#!/usr/bin/env python3
"""Acceptance + regression gate for a Normal-mode benchmark session.

Scores a benchmark session against the per-axis criteria in
Docs/AcceptanceCriteria.md, then compares it with a baseline session (by default
the Normal-mode freeze anchor). It's built to run unattended after the headless
benchmark runner.

Exit codes:
  0  no regressions (criteria that are documented misses can still fail)
  1  regression: a criterion that passed in the baseline now fails
  2  drift: a metric moved more than --drift-tolerance from the baseline
     (only with --fail-on-drift; Normal mode is frozen, so a rerun of the same
     build should reproduce the baseline almost exactly)
  3  input error (missing session, missing maneuvers, unreadable files)

Usage:
  python3 Tools/check_acceptance.py <session>          # id, .zip, or folder
  python3 Tools/check_acceptance.py <session> --baseline session_20260413_142657 --fail-on-drift
  python3 Tools/check_acceptance.py <session> --json-out result.json
"""

import argparse
import csv
import io
import json
import math
import sys
import zipfile
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Dict, List, Optional

sys.path.insert(0, str(Path(__file__).resolve().parent))
from analyze_airdata import load_airdata, metrics_for_segments, segment_maneuvers  # noqa: E402

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_REAL_CSV = REPO_ROOT / "Apr-8th-2026-08-15AM-Flight-Airdata.csv"
DEFAULT_RUNS_ROOT = REPO_ROOT / "BenchmarkRuns"
DEFAULT_BASELINE = "session_20260413_142657"

YAW_SETTLE_THRESHOLD_DEGPS = 5.0
YAW_SETTLE_TOLERANCE_S = 0.1


@dataclass
class Criterion:
    key: str
    description: str
    sim_categories: List[str]  # first category present in the session wins
    signal: str  # forward | lateral | vertical | yaw | hover
    kind: str  # peak_vs_real | carryover_max | yaw_settle | hover_rms_max
    real_category: Optional[str] = None
    tolerance: float = 0.0  # relative for peak_vs_real, absolute otherwise


CRITERIA: List[Criterion] = [
    Criterion("forward_peak", "Forward input-phase peak within 15% of real", ["forward_step"], "forward", "peak_vs_real", "forward_step", 0.15),
    Criterion("forward_carryover", "Forward post-release carryover <= 0.5 m/s", ["forward_step"], "forward", "carryover_max", tolerance=0.5),
    Criterion("backward_peak", "Backward input-phase peak within 15% of real", ["backward_step"], "forward", "peak_vs_real", "backward_step", 0.15),
    Criterion("backward_carryover", "Backward post-release carryover <= 0.5 m/s", ["backward_step"], "forward", "carryover_max", tolerance=0.5),
    Criterion("lateral_right_peak", "Lateral right input-phase peak within 15% of real", ["lateral_right"], "lateral", "peak_vs_real", "lateral_right", 0.15),
    Criterion("lateral_left_peak", "Lateral left input-phase peak within 15% of real", ["lateral_left"], "lateral", "peak_vs_real", "lateral_left", 0.15),
    Criterion("climb_peak", "Climb peak (2.5s window) within 15% of real", ["climb_long", "climb"], "vertical", "peak_vs_real", "climb", 0.15),
    Criterion("descent_peak", "Descent peak (2.5s window) within 15% of real", ["descent_long", "descent"], "vertical", "peak_vs_real", "descent", 0.15),
    Criterion("yaw_right_rate", "Yaw right held-input rate within 5% of real", ["yaw_right"], "yaw", "peak_vs_real", "yaw_right", 0.05),
    Criterion("yaw_left_rate", "Yaw left held-input rate within 5% of real", ["yaw_left"], "yaw", "peak_vs_real", "yaw_left", 0.05),
    Criterion("yaw_right_settle", "Yaw right release settle to <5 deg/s", ["yaw_right"], "yaw", "yaw_settle"),
    Criterion("yaw_left_settle", "Yaw left release settle to <5 deg/s", ["yaw_left"], "yaw", "yaw_settle"),
    Criterion("hover_drift", "Hover horizontal drift <= 1.5 m/s RMS", ["hover_hold"], "hover", "hover_rms_max", tolerance=1.5),
]

SIGNAL_COLUMNS = {
    "forward": "forward_speed_mps",
    "lateral": "lateral_speed_mps",
    "vertical": "vertical_speed_mps",
    "yaw": "yaw_rate_degps",
}


# ---------------------------------------------------------------------------
# Session loading
# ---------------------------------------------------------------------------

def resolve_session(value: str, runs_root: Path) -> Path:
    path = Path(value)
    if path.exists():
        return path
    for candidate in (runs_root / f"{value}.zip", runs_root / value):
        if candidate.exists():
            return candidate
    raise FileNotFoundError(f"Session '{value}' not found as a path or under {runs_root}")


def _f(row: dict, key: str) -> float:
    try:
        return float(row.get(key) or 0.0)
    except ValueError:
        return 0.0


def load_session_runs(session_path: Path) -> Dict[str, List[dict]]:
    """Return {protocol_category: rows} using the last run seen per category."""
    csv_texts: List[str] = []
    if session_path.suffix.lower() == ".zip":
        with zipfile.ZipFile(session_path) as zf:
            for name in sorted(zf.namelist()):
                if name.lower().endswith(".csv"):
                    csv_texts.append(zf.read(name).decode("utf-8-sig"))
    else:
        for csv_path in sorted(session_path.rglob("*.csv")):
            csv_texts.append(csv_path.read_text(encoding="utf-8-sig"))

    runs: Dict[str, List[dict]] = {}
    for text in csv_texts:
        rows = list(csv.DictReader(io.StringIO(text)))
        if not rows:
            continue
        category = (rows[0].get("protocol_category") or rows[0].get("maneuver_name") or "").strip().lower()
        if category:
            runs[category] = rows
    return runs


def _phase_rows(rows: List[dict], *phases: str) -> List[dict]:
    wanted = set(phases)
    return [r for r in rows if (r.get("benchmark_phase") or "").strip().lower() in wanted]


# ---------------------------------------------------------------------------
# Sim metrics
# ---------------------------------------------------------------------------

def sim_metric(criterion: Criterion, rows: List[dict]) -> Optional[float]:
    if criterion.kind == "hover_rms_max":
        window = _phase_rows(rows, "input") or rows
        values = [_f(r, "horizontal_speed_mps") for r in window]
        return math.sqrt(sum(v * v for v in values) / len(values)) if values else None

    column = SIGNAL_COLUMNS[criterion.signal]
    input_rows = _phase_rows(rows, "input")
    if not input_rows:
        return None
    input_peak = max(abs(_f(r, column)) for r in input_rows)

    if criterion.kind == "peak_vs_real":
        return input_peak

    if criterion.kind == "carryover_max":
        full_rows = _phase_rows(rows, "input", "settle")
        full_peak = max(abs(_f(r, column)) for r in full_rows)
        return full_peak - input_peak

    if criterion.kind == "yaw_settle":
        settle_rows = _phase_rows(rows, "settle")
        if not settle_rows:
            return None
        t0 = _f(settle_rows[0], "time_s")
        # Time of the first settle sample after which |yaw rate| stays below threshold.
        settled_at = None
        for r in settle_rows:
            if abs(_f(r, column)) < YAW_SETTLE_THRESHOLD_DEGPS:
                if settled_at is None:
                    settled_at = _f(r, "time_s")
            else:
                settled_at = None
        return (settled_at - t0) if settled_at is not None else None

    raise ValueError(f"Unknown criterion kind {criterion.kind}")


# ---------------------------------------------------------------------------
# Evaluation
# ---------------------------------------------------------------------------

def load_real_peaks(real_csv: Path) -> Dict[str, float]:
    rows = load_airdata(str(real_csv))
    usable, segments, _ = segment_maneuvers(rows)
    metrics = metrics_for_segments(usable, segments)
    return {name: entry["aggregate"]["peak_rate_mean"] for name, entry in metrics.items()}


def evaluate(session_path: Path, real_peaks: Dict[str, float]) -> dict:
    runs = load_session_runs(session_path)
    results = []
    for c in CRITERIA:
        category = next((cat for cat in c.sim_categories if cat in runs), None)
        entry = {"key": c.key, "description": c.description, "sim_category": category,
                 "value": None, "reference": None, "status": "missing", "detail": ""}
        if category is None:
            entry["detail"] = f"no run for {c.sim_categories}"
            results.append(entry)
            continue

        value = sim_metric(c, runs[category])
        entry["value"] = round(value, 3) if value is not None else None

        if value is None:
            entry["detail"] = "metric could not be computed"
        elif c.kind == "peak_vs_real":
            real = real_peaks.get(c.real_category)
            entry["reference"] = real
            if not real:
                entry["status"] = "no_reference"
            else:
                rel = (value - real) / real
                entry["detail"] = f"{rel * 100:+.1f}% vs real {real:.3f}"
                entry["status"] = "pass" if abs(rel) <= c.tolerance + 1e-9 else "fail"
        elif c.kind in ("carryover_max", "hover_rms_max"):
            entry["reference"] = c.tolerance
            entry["status"] = "pass" if round(value, 3) <= c.tolerance + 1e-9 else "fail"
            entry["detail"] = f"limit {c.tolerance}"
        elif c.kind == "yaw_settle":
            # The real log only covers held-stick segments, so there is no real
            # release-settle reference. Judge this one against the baseline.
            entry["status"] = "baseline_only"
            entry["detail"] = f"must stay within {YAW_SETTLE_TOLERANCE_S}s of baseline"
        results.append(entry)

    return {"session": str(session_path), "categories": sorted(runs), "criteria": results}


def compare_to_baseline(current: dict, baseline: dict, drift_tolerance: float) -> List[dict]:
    base_by_key = {c["key"]: c for c in baseline["criteria"]}
    findings = []
    for c in current["criteria"]:
        b = base_by_key.get(c["key"])
        if b is None:
            continue
        cv, bv = c["value"], b["value"]
        if c["status"] == "fail" and b["status"] == "pass":
            findings.append({"key": c["key"], "type": "regression", "detail": f"passed in baseline ({bv}), now fails ({cv})"})
        if c["status"] == "missing" and b["status"] != "missing":
            findings.append({"key": c["key"], "type": "regression", "detail": "metric missing from session"})
        if cv is None or bv is None:
            continue
        if c["key"].endswith("_settle"):
            if abs(cv - bv) > YAW_SETTLE_TOLERANCE_S + 1e-9:
                findings.append({"key": c["key"], "type": "regression", "detail": f"settle {cv}s vs baseline {bv}s"})
            continue
        scale = max(abs(bv), 1.0)
        if abs(cv - bv) / scale > drift_tolerance:
            findings.append({"key": c["key"], "type": "drift", "detail": f"{cv} vs baseline {bv}"})
    return findings


def print_report(current: dict, baseline: Optional[dict], findings: List[dict]) -> None:
    base_by_key = {c["key"]: c for c in baseline["criteria"]} if baseline else {}
    print(f"Session:  {current['session']}")
    if baseline:
        print(f"Baseline: {baseline['session']}")
    print()
    header = f"{'criterion':<20} {'status':<14} {'value':>9} {'baseline':>9}  detail"
    print(header)
    print("-" * len(header))
    for c in current["criteria"]:
        b = base_by_key.get(c["key"], {})
        status = c["status"]
        if status == "fail" and b.get("status") == "fail":
            status = "fail (known)"
        value = "-" if c["value"] is None else f"{c['value']:.3f}"
        bval = "-" if b.get("value") is None else f"{b['value']:.3f}"
        print(f"{c['key']:<20} {status:<14} {value:>9} {bval:>9}  {c['detail']}")
    print()
    if findings:
        for f in findings:
            print(f"[{f['type'].upper()}] {f['key']}: {f['detail']}")
    elif baseline:
        print("No regressions or drift versus baseline.")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("session", help="Session id, .zip, or folder")
    ap.add_argument("--baseline", default=DEFAULT_BASELINE, help="Baseline session (use 'none' to skip)")
    ap.add_argument("--real-csv", default=str(DEFAULT_REAL_CSV))
    ap.add_argument("--runs-root", default=str(DEFAULT_RUNS_ROOT))
    ap.add_argument("--drift-tolerance", type=float, default=0.02,
                    help="Relative change vs baseline reported as drift (default 0.02 = 2%%)")
    ap.add_argument("--fail-on-drift", action="store_true")
    ap.add_argument("--json-out")
    args = ap.parse_args()

    runs_root = Path(args.runs_root)
    try:
        session_path = resolve_session(args.session, runs_root)
        baseline_path = None
        if args.baseline and args.baseline.lower() != "none":
            baseline_path = resolve_session(args.baseline, runs_root)
        real_peaks = load_real_peaks(Path(args.real_csv))
    except (FileNotFoundError, OSError, zipfile.BadZipFile) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 3

    current = evaluate(session_path, real_peaks)
    if not current["categories"]:
        print(f"error: no benchmark CSVs found in {session_path}", file=sys.stderr)
        return 3

    baseline = evaluate(baseline_path, real_peaks) if baseline_path else None
    findings = compare_to_baseline(current, baseline, args.drift_tolerance) if baseline else []
    print_report(current, baseline, findings)

    if args.json_out:
        Path(args.json_out).write_text(json.dumps(
            {"current": current, "baseline": baseline, "findings": findings}, indent=2), encoding="utf-8")

    if any(f["type"] == "regression" for f in findings):
        return 1
    if args.fail_on_drift and any(f["type"] == "drift" for f in findings):
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
