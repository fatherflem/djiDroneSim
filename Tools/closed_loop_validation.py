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


def discover_session_path(sim_root: Path, session_id: str) -> Path:
    candidates = sorted(sim_root.glob(f"{session_id}*"))
    if not candidates:
        raise RuntimeError(f"Could not find session '{session_id}' under {sim_root}")
    zips = [c for c in candidates if c.suffix.lower() == ".zip"]
    if zips:
        return zips[0]
    dirs = [c for c in candidates if c.is_dir()]
    if dirs:
        return dirs[0]
    return candidates[0]


def _parse_manifest_lines(manifest_lines: List[str]):
    session_meta = None
    runs: List[dict] = []
    for line in manifest_lines:
        line = line.strip()
        if not line:
            continue
        entry = json.loads(line)
        if entry.get("type") == "session_metadata":
            session_meta = entry
        elif entry.get("type") == "run":
            runs.append(entry)
    return session_meta, runs


def _parse_session_core(session_path: Path, manifest_lines: List[str], run_csv_names: List[str], open_csv, source_type: str) -> dict:
    session_meta, runs = _parse_manifest_lines(manifest_lines)
    expected_order = {name: idx + 1 for idx, name in enumerate(COMPARISON_CATEGORIES)}
    included = []
    excluded = []
    by_run = {int(r.get("run_number", 0)): r for r in runs}
    all_runs = []

    for run in sorted(runs, key=lambda r: int(r.get("run_number", 0))):
        category = normalize_category(run.get("protocol_category", ""))
        run_number = int(run.get("run_number", 0))
        order = int(run.get("protocol_order", -1))
        source = (run.get("run_source") or "").strip().lower()
        base_row = {
            "run_number": run_number,
            "category": category,
            "protocol_order": order,
            "run_source": source,
        }
        all_runs.append(base_row)

        if category not in expected_order:
            excluded.append({**base_row, "reason": "outside_core_categories"})
            continue
        if source != "full_protocol":
            excluded.append({**base_row, "reason": f"run_source_{source or 'unknown'}"})
            continue
        if order != expected_order[category]:
            excluded.append({**base_row, "reason": f"protocol_order_{order}_expected_{expected_order[category]}"})
            continue
        included.append(base_row)

    run_metrics: Dict[str, dict] = {}
    for row in included:
        run_number = row["run_number"]
        expected_prefix = f"run_{run_number:03d}_"
        csv_name = next((n for n in run_csv_names if Path(n).name.startswith(expected_prefix)), None)
        if not csv_name:
            continue
        reader = csv.DictReader(open_csv(csv_name))
        csv_rows = list(reader)
        summary = summarize_sim_run({"path": f"{session_path.name}:{Path(csv_name).name}", "rows": csv_rows}, row["category"])
        run_metrics[row["category"]] = {
            **summary,
            "run_number": run_number,
            "protocol_order": row["protocol_order"],
            "csv_name": Path(csv_name).name,
        }

    return {
        "session_path": str(session_path),
        "session_source_type": source_type,
        "session_id": session_path.stem,
        "session_metadata": session_meta,
        "included_runs": included,
        "excluded_runs": excluded,
        "all_manifest_runs": all_runs,
        "category_metrics": run_metrics,
    }


def parse_session(session_path: Path) -> dict:
    if session_path.suffix.lower() == ".zip":
        with zipfile.ZipFile(session_path, "r") as zf:
            manifest_name = next((name for name in zf.namelist() if name.endswith("session_manifest.jsonl")), None)
            if not manifest_name:
                raise RuntimeError(f"No session_manifest.jsonl in {session_path}")
            manifest_lines = zf.read(manifest_name).decode("utf-8").splitlines()
            run_csv_names = [name for name in zf.namelist() if name.endswith(".csv")]

            def open_csv(name: str):
                return zf.read(name).decode("utf-8-sig").splitlines()

            return _parse_session_core(session_path, manifest_lines, run_csv_names, open_csv, "zip")

    manifest = next(session_path.glob("**/session_manifest.jsonl"), None)
    if manifest is None:
        raise RuntimeError(f"No session_manifest.jsonl in {session_path}")
    manifest_lines = manifest.read_text(encoding="utf-8").splitlines()
    run_csv_paths = [p for p in session_path.glob("**/*.csv")]
    run_csv_names = [str(p.relative_to(session_path)) for p in run_csv_paths]
    csv_lookup = {str(p.relative_to(session_path)): p for p in run_csv_paths}

    def open_csv(name: str):
        return csv_lookup[name].read_text(encoding="utf-8-sig").splitlines()

    return _parse_session_core(session_path, manifest_lines, run_csv_names, open_csv, "directory")


def calculate_comparison(real_metrics: dict, baseline: dict, prior: dict, newest: dict) -> dict:
    out = {}
    for cat in COMPARISON_CATEGORIES:
        real = (real_metrics.get(cat) or {}).get("aggregate", {})
        old = baseline["category_metrics"].get(cat)
        prior_row = prior["category_metrics"].get(cat)
        new = newest["category_metrics"].get(cat)
        status = (
            "present_in_all"
            if old and prior_row and new
            else "missing_in_newest"
            if old and prior_row and not new
            else "missing_in_prior_or_newest"
            if old and not prior_row
            else "missing_in_baseline_or_multiple"
        )

        metric_rows = {}
        for sim_key, real_key in METRIC_ORDER:
            real_value = real.get(real_key)
            old_value = old.get(sim_key) if old else None
            prior_value = prior_row.get(sim_key) if prior_row else None
            new_value = new.get(sim_key) if new else None
            old_delta = (old_value - real_value) if old_value is not None and real_value is not None else None
            prior_delta = (prior_value - real_value) if prior_value is not None and real_value is not None else None
            new_delta = (new_value - real_value) if new_value is not None and real_value is not None else None
            old_abs = abs(old_delta) if old_delta is not None else None
            prior_abs = abs(prior_delta) if prior_delta is not None else None
            new_abs = abs(new_delta) if new_delta is not None else None
            prior_improvement = (old_abs - prior_abs) if old_abs is not None and prior_abs is not None else None
            newest_improvement = (prior_abs - new_abs) if prior_abs is not None and new_abs is not None else None
            total_improvement = (old_abs - new_abs) if old_abs is not None and new_abs is not None else None
            metric_rows[sim_key] = {
                "real": round(real_value, 3) if real_value is not None else None,
                "old": round(old_value, 3) if old_value is not None else None,
                "prior": round(prior_value, 3) if prior_value is not None else None,
                "new": round(new_value, 3) if new_value is not None else None,
                "old_delta": round(old_delta, 3) if old_delta is not None else None,
                "prior_delta": round(prior_delta, 3) if prior_delta is not None else None,
                "new_delta": round(new_delta, 3) if new_delta is not None else None,
                "prior_abs_delta_improvement": round(prior_improvement, 3) if prior_improvement is not None else None,
                "newest_abs_delta_improvement": round(newest_improvement, 3) if newest_improvement is not None else None,
                "total_abs_delta_improvement": round(total_improvement, 3) if total_improvement is not None else None,
            }

        out[cat] = {
            "availability": status,
            "baseline_present": old is not None,
            "prior_present": prior_row is not None,
            "newest_present": new is not None,
            "real_present": bool(real),
            "metrics": metric_rows,
        }
    return out


def compare_metric_improvements(category_row: dict, metric_keys: List[str], improvement_key: str = "total_abs_delta_improvement"):
    comparable = 0
    improved = 0
    for key in metric_keys:
        imp = category_row["metrics"][key].get(improvement_key)
        if imp is None:
            continue
        comparable += 1
        if imp > 0:
            improved += 1
    return improved, comparable


def segmentation_confidence_label(metrics_entry: dict) -> str:
    confidence = (metrics_entry or {}).get("confidence") or {}
    score = confidence.get("mean_score")
    if score is None:
        return "unknown"
    if score >= 0.78:
        return "high"
    if score >= 0.56:
        return "medium"
    return "low"


def category_divergence_score(category_row: dict, metric_keys: List[str]) -> float:
    total = 0.0
    for key in metric_keys:
        metric = category_row["metrics"].get(key, {})
        delta = metric.get("new_delta")
        if delta is None:
            continue
        total += abs(delta)
    return round(total, 3)


def _trend_label(value):
    if value is None:
        return "n/a"
    if value > 0.02:
        return "improvement"
    if value < -0.02:
        return "regression"
    return "unchanged"


def write_markdown(path: Path, payload: dict):
    comp = payload["comparison_by_category"]
    baseline = payload["baseline_session"]
    prior = payload["prior_post_tuning_session"]
    newest = payload["newest_rerun_session"]

    lines = [
        "# Closed-Loop Validation (Real vs Baseline vs Prior Rerun vs Newest Rerun)",
        "",
        f"- Real benchmark CSV: `{payload['real_csv']}`",
        f"- Baseline simulator session: `{baseline['session_path']}` ({baseline['session_source_type']})",
        f"- Prior post-tuning simulator session: `{prior['session_path']}` ({prior['session_source_type']})",
        f"- Newest simulator rerun (climb-coverage closeout): `{newest['session_path']}` ({newest['session_source_type']})",
        "- Canonical drop location for benchmark sessions: `BenchmarkRuns/`.",
        "- Workflow note: drop session zip files directly into `BenchmarkRuns/`; no manual per-session folder setup is required.",
        f"- Missing run(s) in newest session (vs baseline expected runs): {', '.join(payload['missing_vs_baseline_runs']) if payload['missing_vs_baseline_runs'] else '(none)'}",
        f"- Missing category label(s) in newest session: {', '.join(payload['missing_vs_baseline_categories']) if payload['missing_vs_baseline_categories'] else '(none)'}",
        "",
        "## Session coverage",
        "",
        f"- Baseline manifest run count: {payload['baseline_manifest_run_count']}",
        f"- Prior rerun manifest run count: {payload['prior_manifest_run_count']}",
        f"- Newest rerun manifest run count: {payload['newest_manifest_run_count']}",
        f"- Baseline primary protocol run count: {len(baseline['included_runs'])}",
        f"- Prior rerun primary protocol run count: {len(prior['included_runs'])}",
        f"- Newest rerun primary protocol run count: {len(newest['included_runs'])}",
        f"- Baseline excluded runs: {len(baseline['excluded_runs'])}",
        f"- Prior rerun excluded runs: {len(prior['excluded_runs'])}",
        f"- Newest rerun excluded runs: {len(newest['excluded_runs'])}",
        f"- Climb counterpart run present in newest rerun: {payload['climb_counterpart_present_in_newest']}",
        "",
        "## Category comparison (baseline vs prior vs newest vs real)",
        "",
        "| Category | Availability | Metric | Real | Baseline | Prior | Newest | Baseline Δ | Prior Δ | Newest Δ | Trend (prior→newest) |",
        "|---|---|---|---:|---:|---:|---:|---:|---:|---:|---|",
    ]

    for cat in COMPARISON_CATEGORIES:
        row = comp[cat]
        first = True
        for metric in [m[0] for m in METRIC_ORDER]:
            values = row["metrics"][metric]
            lines.append(
                "| {} | {} | {} | {} | {} | {} | {} | {} | {} | {} | {} |".format(
                    cat if first else "",
                    row["availability"] if first else "",
                    metric,
                    "-" if values["real"] is None else values["real"],
                    "-" if values["old"] is None else values["old"],
                    "-" if values["prior"] is None else values["prior"],
                    "-" if values["new"] is None else values["new"],
                    "-" if values["old_delta"] is None else values["old_delta"],
                    "-" if values["prior_delta"] is None else values["prior_delta"],
                    "-" if values["new_delta"] is None else values["new_delta"],
                    _trend_label(values["newest_abs_delta_improvement"]),
                )
            )
            first = False

    lines += ["", "## Strong-category assessment", ""]
    for cat in STRONG_CATEGORIES:
        item = payload["strong_category_assessment"][cat]
        lines.append(
            f"- {cat}: status={item['status']}; moved_correct_direction={item['moved_correct_direction']}; "
            f"abs_delta_better_metrics={item['metrics_with_smaller_abs_delta']}/{item['comparable_metric_count']}; "
            f"response_shape={item['response_shape']}; notes={item['note']}"
        )

    lines += ["", "## Provisional-category notes", ""]
    for cat in PROVISIONAL_CATEGORIES:
        item = payload["provisional_category_assessment"][cat]
        lines.append(
            f"- {cat}: {item['status']}; moved_correct_direction={item['moved_correct_direction']}; "
            f"real_segmentation_confidence={item['real_segmentation_confidence']}; "
            f"sim_amplitude_confidence={item['sim_amplitude_confidence']}; "
            f"sim_amplitude_provenance={item['sim_amplitude_provenance']}."
        )

    decision = payload["decision_summary"]
    lines += [
        "",
        "## Decision summary",
        "",
        f"- Climb coverage complete: {decision['is_climb_coverage_complete']}",
        f"- Strongest high-confidence divergence: {decision['strongest_high_confidence_divergence']}",
        f"- Lateral right acceptable: {decision['is_lateral_right_acceptable']}",
        f"- Yaw left acceptable: {decision['is_yaw_left_acceptable']}",
        f"- Normal-mode fidelity good enough to move on: {decision['normal_mode_good_enough_to_move_on']}",
        f"- Single next tuning target: {decision['single_next_tuning_target']}",
    ]

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("real_csv")
    ap.add_argument("--benchmark-runs-root", default="BenchmarkRuns")
    ap.add_argument("--baseline-session-id", default="session_20260402_133209")
    ap.add_argument("--prior-session-id", default="session_20260402_140700")
    ap.add_argument("--newest-session-id", default="session_20260402_151147")
    ap.add_argument("--baseline-zip")
    ap.add_argument("--prior-zip")
    ap.add_argument("--newest-zip")
    ap.add_argument("--json-out", default="Docs/ClosedLoopValidation_Apr02_2026.json")
    ap.add_argument("--summary-out", default="Docs/ClosedLoopValidation_Apr02_2026.md")
    args = ap.parse_args()

    rows = load_airdata(args.real_csv)
    usable, segments, neutral_windows = segment_maneuvers(rows)
    metrics = metrics_for_segments(usable, segments)
    hover = hover_metrics(usable, neutral_windows)
    if hover:
        metrics["hover_hold"] = hover

    sim_root = Path(args.benchmark_runs_root)
    baseline_path = Path(args.baseline_zip) if args.baseline_zip else discover_session_path(sim_root, args.baseline_session_id)
    prior_path = Path(args.prior_zip) if args.prior_zip else discover_session_path(sim_root, args.prior_session_id)
    newest_path = Path(args.newest_zip) if args.newest_zip else discover_session_path(sim_root, args.newest_session_id)
    baseline = parse_session(baseline_path)
    prior = parse_session(prior_path)
    newest = parse_session(newest_path)

    baseline_categories = {r["category"] for r in baseline["included_runs"]}
    newest_categories = {r["category"] for r in newest["included_runs"]}
    missing_vs_baseline_categories = sorted(baseline_categories - newest_categories)

    baseline_all = baseline["all_manifest_runs"]
    prior_all = prior["all_manifest_runs"]
    newest_all = newest["all_manifest_runs"]
    baseline_run_count = len(baseline_all)
    prior_run_count = len(prior_all)
    newest_run_count = len(newest_all)

    newest_keys = {(r.get("category"), r.get("protocol_order"), r.get("run_source")) for r in newest_all}
    missing_vs_baseline_runs = []
    missing_category_labels = []
    for r in baseline_all:
        if r.get("run_source") != "full_protocol":
            continue
        key = (r.get("category"), r.get("protocol_order"), r.get("run_source"))
        if key in newest_keys:
            continue
        missing_vs_baseline_runs.append(
            f"run_{r['run_number']:03d}:{r.get('category')} source={r.get('run_source','unknown')} order={r.get('protocol_order','-')}"
        )
        if r.get("category"):
            missing_category_labels.append(r.get("category"))
    if not missing_vs_baseline_runs and missing_vs_baseline_categories:
        missing_vs_baseline_runs = [f"category:{cat}" for cat in missing_vs_baseline_categories]
    if missing_category_labels:
        missing_vs_baseline_categories = sorted(set(missing_category_labels))

    comparison = calculate_comparison(metrics, baseline, prior, newest)
    climb_counterpart_present = "climb" in newest_categories

    strong_assessment = {}
    for cat in STRONG_CATEGORIES:
        row = comparison[cat]
        if not row["newest_present"]:
            strong_assessment[cat] = {
                "status": "missing_in_newest",
                "moved_correct_direction": None,
                "metrics_with_smaller_abs_delta": 0,
                "comparable_metric_count": 0,
                "response_shape": "unknown_missing_rerun",
                "note": "Category missing from newest rerun; rerun needed.",
            }
            continue

        improved, comparable = compare_metric_improvements(
            row, ["response_delay_s", "peak_rate", "max_accel", "settle_time_s", "overshoot"]
        )
        shape_improved, shape_comparable = compare_metric_improvements(row, ["peak_rate", "max_accel", "overshoot"])
        moved = improved > 0
        if comparable and improved == comparable:
            status = "acceptable"
        elif moved:
            status = "improved_but_still_off"
        else:
            status = "still_poor"
        strong_assessment[cat] = {
            "status": status,
            "moved_correct_direction": moved,
            "metrics_with_smaller_abs_delta": improved,
            "comparable_metric_count": comparable,
            "response_shape": "improved" if shape_comparable and shape_improved >= 2 else "not_improved",
            "note": "Status is based on absolute-delta movement against the real benchmark values.",
        }

    airdata_payload = json.loads(Path("Docs/airdata_mar30_analysis.json").read_text(encoding="utf-8"))
    real_metrics_index = airdata_payload.get("metrics", {})
    comparison_rows = airdata_payload.get("sim_vs_real_comparison", {})
    cat_to_amp = comparison_rows if isinstance(comparison_rows, dict) else {}

    provisional_assessment = {}
    for cat in PROVISIONAL_CATEGORIES:
        row = comparison[cat]
        amp = cat_to_amp.get(cat, {})
        real_conf = segmentation_confidence_label(real_metrics_index.get(cat, {}))
        if not row["newest_present"]:
            provisional_assessment[cat] = {
                "status": "not_rerun",
                "moved_correct_direction": None,
                "real_segmentation_confidence": real_conf,
                "sim_amplitude_confidence": amp.get("sim_input_amplitude_confidence", "unknown"),
                "sim_amplitude_provenance": amp.get("sim_input_amplitude_provenance", "unknown"),
            }
            continue

        improved, comparable = compare_metric_improvements(
            row, ["response_delay_s", "peak_rate", "max_accel", "settle_time_s", "overshoot"]
        )
        provisional_assessment[cat] = {
            "status": "directionally_improved" if improved > 0 else "no_clear_directional_gain",
            "moved_correct_direction": improved > 0,
            "real_segmentation_confidence": real_conf,
            "sim_amplitude_confidence": amp.get("sim_input_amplitude_confidence", "unknown"),
            "sim_amplitude_provenance": amp.get("sim_input_amplitude_provenance", "unknown"),
            "comparable_metrics": comparable,
        }

    divergence_metric_keys = ["response_delay_s", "peak_rate", "max_accel", "settle_time_s", "overshoot"]
    divergence_scores = {
        "yaw_right": category_divergence_score(comparison["yaw_right"], divergence_metric_keys),
        "yaw_left": category_divergence_score(comparison["yaw_left"], divergence_metric_keys),
        "lateral_right": category_divergence_score(comparison["lateral_right"], divergence_metric_keys),
        "climb/descent": round(
            (
                category_divergence_score(comparison["climb"], divergence_metric_keys)
                + category_divergence_score(comparison["descent"], divergence_metric_keys)
            )
            / 2.0,
            3,
        ),
        "forward_step": category_divergence_score(comparison["forward_step"], divergence_metric_keys),
    }
    strongest_high_confidence = max(
        {"yaw_right": divergence_scores["yaw_right"], "yaw_left": divergence_scores["yaw_left"], "lateral_right": divergence_scores["lateral_right"]},
        key=lambda k: divergence_scores[k],
    )
    strongest_option = max(
        {
            "yaw_right": divergence_scores["yaw_right"],
            "lateral_right": divergence_scores["lateral_right"],
            "climb/descent": divergence_scores["climb/descent"],
            "forward_step": divergence_scores["forward_step"],
        },
        key=lambda k: divergence_scores[k],
    )
    no_further_needed = all(v < 1.0 for v in divergence_scores.values())
    decision_summary = {
        "is_climb_coverage_complete": climb_counterpart_present and len(missing_vs_baseline_categories) == 0,
        "strongest_high_confidence_divergence": strongest_high_confidence,
        "is_lateral_right_acceptable": strong_assessment["lateral_right"]["status"] == "acceptable",
        "is_yaw_left_acceptable": strong_assessment["yaw_left"]["status"] == "acceptable",
        "forward_step_status": provisional_assessment["forward_step"]["status"],
        "climb_status": provisional_assessment["climb"]["status"],
        "descent_status": provisional_assessment["descent"]["status"],
        "normal_mode_good_enough_to_move_on": no_further_needed,
        "single_next_tuning_target": "no further Normal-mode tuning needed yet" if no_further_needed else strongest_option,
        "divergence_scores": divergence_scores,
    }

    payload = {
        "real_csv": args.real_csv,
        "baseline_session": baseline,
        "prior_post_tuning_session": prior,
        "newest_rerun_session": newest,
        "missing_vs_baseline_categories": missing_vs_baseline_categories,
        "missing_vs_baseline_runs": missing_vs_baseline_runs,
        "baseline_manifest_run_count": baseline_run_count,
        "prior_manifest_run_count": prior_run_count,
        "newest_manifest_run_count": newest_run_count,
        "climb_counterpart_present_in_newest": climb_counterpart_present,
        "comparison_by_category": comparison,
        "strong_category_assessment": strong_assessment,
        "provisional_category_assessment": provisional_assessment,
        "decision_summary": decision_summary,
        "confidence_notes": {
            "high_confidence": ["hover_hold", "lateral_right", "yaw_right", "yaw_left"],
            "provisional": PROVISIONAL_CATEGORIES,
            "note": "Provisional categories remain directional unless rerun confidence evidence is upgraded.",
        },
    }

    Path(args.json_out).write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(Path(args.summary_out), payload)

    print(
        json.dumps(
            {
                "json_out": args.json_out,
                "summary_out": args.summary_out,
                "missing_vs_baseline_categories": missing_vs_baseline_categories,
                "baseline_manifest_run_count": baseline_run_count,
                "prior_manifest_run_count": prior_run_count,
                "newest_manifest_run_count": newest_run_count,
                "baseline_included": len(baseline["included_runs"]),
                "prior_included": len(prior["included_runs"]),
                "newest_included": len(newest["included_runs"]),
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
