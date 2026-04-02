#!/usr/bin/env python3
"""Closed-loop old-vs-new-vs-real benchmark validation."""

import argparse
import csv
import json
import zipfile
from pathlib import Path
from typing import Dict, List

from analyze_airdata import (
    COMPARISON_CATEGORIES,
    load_airdata,
    metrics_for_segments,
    segment_maneuvers,
    hover_metrics,
    summarize_sim_run,
)

METRIC_ORDER = [
    ("response_delay_s", "response_delay_s_mean"),
    ("peak_rate", "peak_rate_mean"),
    ("max_accel", "max_accel_mean"),
    ("settle_time_s", "settle_time_s_mean"),
    ("overshoot", "overshoot_mean"),
    ("residual_drift", "residual_drift_mean"),
]

STRONG_CATEGORIES = ["lateral_right", "yaw_right", "yaw_left"]
PROVISIONAL_CATEGORIES = ["forward_step", "lateral_left", "climb", "descent"]


def normalize_category(value: str) -> str:
    return (value or "").strip().lower().replace(" ", "_")


def parse_session(zip_path: Path) -> dict:
    with zipfile.ZipFile(zip_path, "r") as zf:
        manifest_name = next((name for name in zf.namelist() if name.endswith("session_manifest.jsonl")), None)
        if not manifest_name:
            raise RuntimeError(f"No session_manifest.jsonl in {zip_path}")

        session_meta = None
        runs: List[dict] = []
        for line in zf.read(manifest_name).decode("utf-8").splitlines():
            line = line.strip()
            if not line:
                continue
            entry = json.loads(line)
            if entry.get("type") == "session_metadata":
                session_meta = entry
            elif entry.get("type") == "run":
                runs.append(entry)

        expected_order = {name: idx + 1 for idx, name in enumerate(COMPARISON_CATEGORIES)}
        included = []
        excluded = []
        by_run = {int(r.get("run_number", 0)): r for r in runs}
        for run in sorted(runs, key=lambda r: int(r.get("run_number", 0))):
            category = normalize_category(run.get("protocol_category", ""))
            run_number = int(run.get("run_number", 0))
            order = int(run.get("protocol_order", -1))
            source = (run.get("run_source") or "").strip().lower()
            if category not in expected_order:
                excluded.append(
                    {
                        "run_number": run_number,
                        "category": category,
                        "protocol_order": order,
                        "run_source": source,
                        "reason": "outside_core_categories",
                    }
                )
                continue
            if source != "full_protocol":
                excluded.append(
                    {
                        "run_number": run_number,
                        "category": category,
                        "protocol_order": order,
                        "run_source": source,
                        "reason": f"run_source_{source or 'unknown'}",
                    }
                )
                continue
            if order != expected_order[category]:
                excluded.append(
                    {
                        "run_number": run_number,
                        "category": category,
                        "protocol_order": order,
                        "run_source": source,
                        "reason": f"protocol_order_{order}_expected_{expected_order[category]}",
                    }
                )
                continue
            included.append({"run_number": run_number, "category": category, "protocol_order": order, "run_source": source})

        # read run csvs directly from zip
        run_csv_names = [name for name in zf.namelist() if name.endswith(".csv")]
        run_metrics: Dict[str, dict] = {}
        for row in included:
            run_number = row["run_number"]
            run_entry = by_run[run_number]
            expected_prefix = f"run_{run_number:03d}_"
            csv_name = next((n for n in run_csv_names if Path(n).name.startswith(expected_prefix)), None)
            if not csv_name:
                continue
            decoded = zf.read(csv_name).decode("utf-8-sig").splitlines()
            reader = csv.DictReader(decoded)
            csv_rows = list(reader)
            summary = summarize_sim_run({"path": f"{zip_path.name}:{Path(csv_name).name}", "rows": csv_rows}, row["category"])
            run_metrics[row["category"]] = {
                **summary,
                "run_number": run_number,
                "protocol_order": row["protocol_order"],
                "csv_name": Path(csv_name).name,
            }

    return {
        "zip_path": str(zip_path),
        "session_id": zip_path.stem,
        "session_metadata": session_meta,
        "included_runs": included,
        "excluded_runs": excluded,
        "category_metrics": run_metrics,
    }


def calculate_comparison(real_metrics: dict, baseline: dict, tuned: dict) -> dict:
    out = {}
    for cat in COMPARISON_CATEGORIES:
        real = (real_metrics.get(cat) or {}).get("aggregate", {})
        old = baseline["category_metrics"].get(cat)
        new = tuned["category_metrics"].get(cat)
        status = "present_in_all" if old and new else "missing_in_new" if old and not new else "missing_in_baseline_or_both"

        metric_rows = {}
        for sim_key, real_key in METRIC_ORDER:
            real_value = real.get(real_key)
            old_value = old.get(sim_key) if old else None
            new_value = new.get(sim_key) if new else None
            old_delta = (old_value - real_value) if old_value is not None and real_value is not None else None
            new_delta = (new_value - real_value) if new_value is not None and real_value is not None else None
            old_abs = abs(old_delta) if old_delta is not None else None
            new_abs = abs(new_delta) if new_delta is not None else None
            improvement = (old_abs - new_abs) if old_abs is not None and new_abs is not None else None
            metric_rows[sim_key] = {
                "real": round(real_value, 3) if real_value is not None else None,
                "old": round(old_value, 3) if old_value is not None else None,
                "new": round(new_value, 3) if new_value is not None else None,
                "old_delta": round(old_delta, 3) if old_delta is not None else None,
                "new_delta": round(new_delta, 3) if new_delta is not None else None,
                "abs_delta_improvement": round(improvement, 3) if improvement is not None else None,
            }

        out[cat] = {
            "availability": status,
            "baseline_present": old is not None,
            "post_tuning_present": new is not None,
            "real_present": bool(real),
            "metrics": metric_rows,
        }
    return out


def write_markdown(path: Path, payload: dict):
    comp = payload["comparison_by_category"]
    baseline = payload["baseline_session"]
    tuned = payload["post_tuning_session"]

    lines = [
        "# Closed-Loop Validation (Real vs Baseline Sim vs Post-Tuning Sim)",
        "",
        f"- Real benchmark CSV: `{payload['real_csv']}`",
        f"- Baseline simulator session: `{baseline['zip_path']}`",
        f"- Post-tuning simulator session: `{tuned['zip_path']}`",
        f"- Missing run(s) in post-tuning session (vs baseline protocol coverage): {', '.join(payload['missing_vs_baseline_runs']) if payload['missing_vs_baseline_runs'] else '(none)'}",
        "",
        "## Session coverage",
        "",
        f"- Baseline primary protocol run count: {len(baseline['included_runs'])}",
        f"- Post-tuning primary protocol run count: {len(tuned['included_runs'])}",
        f"- Baseline excluded runs: {len(baseline['excluded_runs'])}",
        f"- Post-tuning excluded runs: {len(tuned['excluded_runs'])}",
        "",
        "## Category comparison (old vs new vs real)",
        "",
        "| Category | Availability | Metric | Real | Old | New | Old Δ | New Δ | Abs Δ improvement |",
        "|---|---|---|---:|---:|---:|---:|---:|---:|",
    ]

    for cat in COMPARISON_CATEGORIES:
        row = comp[cat]
        first = True
        for metric in [m[0] for m in METRIC_ORDER]:
            values = row["metrics"][metric]
            lines.append(
                "| {} | {} | {} | {} | {} | {} | {} | {} | {} |".format(
                    cat if first else "",
                    row["availability"] if first else "",
                    metric,
                    "-" if values["real"] is None else values["real"],
                    "-" if values["old"] is None else values["old"],
                    "-" if values["new"] is None else values["new"],
                    "-" if values["old_delta"] is None else values["old_delta"],
                    "-" if values["new_delta"] is None else values["new_delta"],
                    "-" if values["abs_delta_improvement"] is None else values["abs_delta_improvement"],
                )
            )
            first = False

    lines += ["", "## Strong-category assessment", ""]
    for cat in STRONG_CATEGORIES:
        row = comp[cat]
        if not row["post_tuning_present"]:
            lines.append(f"- {cat}: **missing in post-tuning session**; rerun required before judging improvement.")
            continue
        peak = row["metrics"]["peak_rate"]["abs_delta_improvement"]
        delay = row["metrics"]["response_delay_s"]["abs_delta_improvement"]
        settle = row["metrics"]["settle_time_s"]["abs_delta_improvement"]
        direction = "improved" if any(v is not None and v > 0 for v in [peak, delay, settle]) else "not_improved"
        lines.append(
            f"- {cat}: {direction}; abs delta improvement (peak/delay/settle) = {peak}, {delay}, {settle}."
        )

    lines += ["", "## Provisional-category notes", ""]
    for cat in PROVISIONAL_CATEGORIES:
        row = comp[cat]
        peak = row["metrics"]["peak_rate"]["abs_delta_improvement"]
        lines.append(f"- {cat}: peak-rate abs delta improvement = {peak}; treat as provisional directionality.")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("real_csv")
    ap.add_argument("--baseline-zip", required=True)
    ap.add_argument("--post-zip", required=True)
    ap.add_argument("--json-out", default="Docs/ClosedLoopValidation_Apr02_2026.json")
    ap.add_argument("--summary-out", default="Docs/ClosedLoopValidation_Apr02_2026.md")
    args = ap.parse_args()

    rows = load_airdata(args.real_csv)
    usable, segments, neutral_windows = segment_maneuvers(rows)
    metrics = metrics_for_segments(usable, segments)
    hover = hover_metrics(usable, neutral_windows)
    if hover:
        metrics["hover_hold"] = hover

    baseline = parse_session(Path(args.baseline_zip))
    post = parse_session(Path(args.post_zip))

    baseline_categories = {r["category"] for r in baseline["included_runs"]}
    post_categories = {r["category"] for r in post["included_runs"]}
    missing_vs_baseline_categories = sorted(baseline_categories - post_categories)
    baseline_all = baseline["included_runs"] + baseline["excluded_runs"]
    post_all = post["included_runs"] + post["excluded_runs"]
    post_keys = {(r.get("category"), r.get("protocol_order"), r.get("run_source")) for r in post_all}
    missing_vs_baseline_runs = []
    for r in baseline_all:
        key = (r.get("category"), r.get("protocol_order"), r.get("run_source"))
        if key in post_keys:
            continue
        missing_vs_baseline_runs.append(
            f"run_{r['run_number']:03d}:{r.get('category')} source={r.get('run_source','unknown')} order={r.get('protocol_order','-')}"
        )
    if not missing_vs_baseline_runs and missing_vs_baseline_categories:
        missing_vs_baseline_runs = [f"category:{cat}" for cat in missing_vs_baseline_categories]

    comparison = calculate_comparison(metrics, baseline, post)

    payload = {
        "real_csv": args.real_csv,
        "baseline_session": baseline,
        "post_tuning_session": post,
        "missing_vs_baseline_categories": missing_vs_baseline_categories,
        "missing_vs_baseline_runs": missing_vs_baseline_runs,
        "comparison_by_category": comparison,
        "confidence_notes": {
            "high_confidence": ["hover_hold", "lateral_right", "yaw_right", "yaw_left"],
            "provisional": PROVISIONAL_CATEGORIES,
            "note": "Provisional categories remain directional unless rerun confidence evidence is upgraded.",
        },
    }

    Path(args.json_out).write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(Path(args.summary_out), payload)

    print(json.dumps({
        "json_out": args.json_out,
        "summary_out": args.summary_out,
        "missing_vs_baseline_categories": missing_vs_baseline_categories,
        "baseline_included": len(baseline["included_runs"]),
        "post_included": len(post["included_runs"]),
    }, indent=2))


if __name__ == "__main__":
    main()
