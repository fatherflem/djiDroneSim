#!/usr/bin/env python3
"""Airdata + sim benchmark analysis helper.

Focuses on:
1) confidence-labeled maneuver segmentation from Airdata RC + motion traces
2) explicit measured vs inferred evidence tagging
3) optional sim-vs-real side-by-side metric comparison when benchmark CSVs are supplied
"""

import argparse
import csv
import glob
import json
import math
import os
import statistics
from collections import defaultdict
from dataclasses import asdict, dataclass
from typing import Dict, List, Optional, Tuple

MPS_PER_MPH = 0.44704
AXIS_TO_RC = {
    "forward": "rc_elevator",
    "lateral": "rc_aileron",
    "vertical": "rc_throttle",
    "yaw": "rc_rudder",
}
MANEUVER_MAP = {
    ("forward", 1): "forward_step",
    ("forward", -1): "backward_step",
    ("lateral", 1): "lateral_right",
    ("lateral", -1): "lateral_left",
    ("vertical", 1): "climb",
    ("vertical", -1): "descent",
    ("yaw", 1): "yaw_right",
    ("yaw", -1): "yaw_left",
}
EXPECTED_ORDER = [
    "hover_hold",
    "forward_step",
    "lateral_right",
    "lateral_left",
    "climb",
    "descent",
    "yaw_right",
    "yaw_left",
]


@dataclass
class Segment:
    maneuver: str
    axis: str
    sign: int
    start: int
    end: int
    start_s: float
    end_s: float
    duration_s: float
    confidence_score: float
    confidence_label: str
    confidence_reasons: List[str]
    cluster_id: int
    sequence_index: int


@dataclass
class RunMetric:
    start_s: float
    end_s: float
    duration_s: float
    response_delay_s: Optional[float]
    peak_rate: float
    settle_time_s: float
    max_accel: float
    overshoot: float
    residual_drift: float
    command_mean_abs: float


def _safe_float(row: dict, key: str) -> float:
    value = row.get(key, "")
    value = value.strip() if isinstance(value, str) else value
    if value in ("", None):
        return 0.0
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def load_airdata(path: str) -> List[dict]:
    rows = []
    with open(path, newline="", encoding="utf-8-sig") as fp:
        reader = csv.DictReader(fp)
        for raw in reader:
            rows.append(
                {
                    "t_ms": int(float(raw["time(millisecond)"])),
                    "mode": raw.get("flycState", "").strip(),
                    "vx_mph": _safe_float(raw, " xSpeed(mph)"),
                    "vy_mph": _safe_float(raw, " ySpeed(mph)"),
                    "vz_mph": _safe_float(raw, " zSpeed(mph)"),
                    "alt_ft": _safe_float(raw, "height_above_takeoff(feet)")
                    or _safe_float(raw, "altitude(feet)"),
                    "heading_deg": _safe_float(raw, " compass_heading(degrees)")
                    if " compass_heading(degrees)" in raw
                    else _safe_float(raw, "compass_heading(degrees)"),
                    "pitch_deg": _safe_float(raw, " pitch(degrees)"),
                    "roll_deg": _safe_float(raw, " roll(degrees)"),
                    "rc_elevator": _safe_float(raw, "rc_elevator(percent)"),
                    "rc_aileron": _safe_float(raw, "rc_aileron(percent)"),
                    "rc_throttle": _safe_float(raw, "rc_throttle(percent)"),
                    "rc_rudder": _safe_float(raw, "rc_rudder(percent)"),
                }
            )
    if not rows:
        return []
    t0 = rows[0]["t_ms"]
    for row in rows:
        row["t"] = (row["t_ms"] - t0) / 1000.0
    return rows


def detect_neutral_windows(rows: List[dict], neutral_th: float = 8.0, min_s: float = 1.2):
    windows = []
    i = 0
    while i < len(rows):
        if max(abs(rows[i][k]) for k in AXIS_TO_RC.values()) > neutral_th:
            i += 1
            continue
        j = i
        while j < len(rows) and max(abs(rows[j][k]) for k in AXIS_TO_RC.values()) <= neutral_th:
            j += 1
        duration = rows[j - 1]["t"] - rows[i]["t"]
        if duration >= min_s:
            windows.append((i, j - 1))
        i = j
    return windows


def _dominant_axis(row: dict) -> Tuple[str, float, float]:
    values = {axis: row[field] for axis, field in AXIS_TO_RC.items()}
    axis = max(values, key=lambda a: abs(values[a]))
    dom = values[axis]
    runner_up = max(abs(v) for k, v in values.items() if k != axis)
    return axis, dom, runner_up


def segment_maneuvers(rows: List[dict], active_th: float = 16.0, cross_th: float = 14.0, min_len: float = 0.7):
    usable = [r for r in rows if r["mode"] == "P-GPS"]
    neutral_windows = detect_neutral_windows(usable)
    segments: List[Segment] = []
    i = 0

    while i < len(usable):
        axis, dom, runner = _dominant_axis(usable[i])
        if abs(dom) < active_th:
            i += 1
            continue

        sign = 1 if dom >= 0 else -1
        start = i
        j = i
        while j < len(usable):
            curr_axis, curr_dom, curr_runner = _dominant_axis(usable[j])
            curr_sign = 1 if curr_dom >= 0 else -1
            if curr_axis != axis or curr_sign != sign:
                break
            if abs(curr_dom) < active_th * 0.78:
                break
            if curr_runner > cross_th:
                break
            j += 1

        end = j - 1
        duration_s = usable[end]["t"] - usable[start]["t"] if end >= start else 0.0
        if duration_s < min_len:
            i = max(i + 1, j)
            continue

        maneuver = MANEUVER_MAP[(axis, sign)]
        cluster_key = (maneuver, round(duration_s, 1), round(abs(dom) / 10.0) * 10)

        reasons = []
        score = 0.35
        if duration_s >= 1.0:
            score += 0.14
            reasons.append("duration>=1.0s")
        if runner <= cross_th * 0.55:
            score += 0.14
            reasons.append("low cross-axis input")

        pre_neutral = any(win_end >= start - 4 and win_end < start for _, win_end in neutral_windows)
        post_neutral = any(win_start <= end + 4 and win_start > end for win_start, _ in neutral_windows)
        if pre_neutral and post_neutral:
            score += 0.16
            reasons.append("neutral dwell before+after")
        elif pre_neutral or post_neutral:
            score += 0.08
            reasons.append("neutral dwell on one side")

        # Motion + attitude sign consistency checks.
        motion_ok = False
        attitude_ok = False
        window = usable[start : end + 1]
        if axis == "forward":
            rate = [abs(r["vx_mph"]) * MPS_PER_MPH for r in window]
            attitude_ok = statistics.fmean(r["pitch_deg"] for r in window) < -0.8
            motion_ok = max(rate) > 0.6
        elif axis == "lateral":
            rate = [abs(r["vy_mph"]) * MPS_PER_MPH for r in window]
            mean_roll = statistics.fmean(r["roll_deg"] for r in window)
            attitude_ok = mean_roll > 0.8 if sign > 0 else mean_roll < -0.8
            motion_ok = max(rate) > 0.5
        elif axis == "vertical":
            rate = [max(0.0, sign * r["vz_mph"] * MPS_PER_MPH) for r in window]
            attitude_ok = True
            motion_ok = max(rate) > 0.5
        else:
            rates = []
            for idx in range(1, len(window)):
                d = ((window[idx]["heading_deg"] - window[idx - 1]["heading_deg"] + 540.0) % 360.0) - 180.0
                rates.append(sign * d / 0.1)
            rate = [max(0.0, r) for r in rates] if rates else [0.0]
            attitude_ok = True
            motion_ok = max(rate) > 18.0

        if motion_ok:
            score += 0.11
            reasons.append("motion sign agrees")
        if attitude_ok:
            score += 0.10
            reasons.append("attitude response agrees")

        score = min(1.0, score)
        label = "high" if score >= 0.78 else "medium" if score >= 0.56 else "low"

        segments.append(
            Segment(
                maneuver=maneuver,
                axis=axis,
                sign=sign,
                start=start,
                end=end,
                start_s=usable[start]["t"],
                end_s=usable[end]["t"],
                duration_s=duration_s,
                confidence_score=round(score, 3),
                confidence_label=label,
                confidence_reasons=reasons,
                cluster_id=abs(hash(cluster_key)) % 100000,
                sequence_index=0,
            )
        )
        i = max(j, i + 1)

    # Sequence hints based on expected benchmark ordering (informational only).
    expected_idx = 0
    for seg in segments:
        if seg.maneuver in EXPECTED_ORDER:
            idx = EXPECTED_ORDER.index(seg.maneuver)
            if idx >= expected_idx:
                expected_idx = idx
            else:
                seg.confidence_reasons.append("out-of-order vs expected protocol")
        seg.sequence_index = expected_idx

    # Repeated-run cluster support (confidence boost if repeated and similar).
    counts = defaultdict(int)
    for seg in segments:
        counts[(seg.maneuver, seg.cluster_id)] += 1
    for seg in segments:
        if counts[(seg.maneuver, seg.cluster_id)] >= 2:
            seg.confidence_score = min(1.0, seg.confidence_score + 0.05)
            seg.confidence_reasons.append("repeated-run cluster support")
        seg.confidence_label = "high" if seg.confidence_score >= 0.78 else "medium" if seg.confidence_score >= 0.56 else "low"

    return usable, segments, neutral_windows


def _extract_signal(window: List[dict], axis: str, sign: int) -> List[float]:
    if axis == "forward":
        values = [sign * r["vx_mph"] * MPS_PER_MPH for r in window]
    elif axis == "lateral":
        values = [sign * r["vy_mph"] * MPS_PER_MPH for r in window]
    elif axis == "vertical":
        values = [abs(r["vz_mph"]) * MPS_PER_MPH for r in window]
    else:
        values = []
        for idx in range(1, len(window)):
            d = ((window[idx]["heading_deg"] - window[idx - 1]["heading_deg"] + 540.0) % 360.0) - 180.0
            values.append(sign * d / 0.1)
        values = ([values[0]] if values else [0.0]) + values
    return [max(0.0, v) for v in values]


def metrics_for_segments(usable: List[dict], segments: List[Segment]) -> Dict[str, dict]:
    grouped: Dict[str, List[Segment]] = defaultdict(list)
    for seg in segments:
        grouped[seg.maneuver].append(seg)

    out: Dict[str, dict] = {}

    for maneuver, runs in grouped.items():
        run_metrics: List[RunMetric] = []
        used_segments: List[Segment] = []
        for seg in runs:
            window = usable[seg.start : seg.end + 1]
            signal = _extract_signal(window, seg.axis, seg.sign)
            if not signal:
                continue
            peak = max(signal)
            if peak <= 1e-4:
                continue
            idx10 = next((i for i, v in enumerate(signal) if v >= 0.1 * peak), None)
            settle_idx = next((i for i in range(len(signal) - 1, -1, -1) if signal[i] > 0.12 * peak), 0)
            steady = statistics.fmean(signal[-min(5, len(signal)) :])
            overshoot = max(0.0, peak - steady)
            residual_drift = statistics.fmean(signal[-min(8, len(signal)) :])
            dt = 0.1
            accel = max(((signal[i + 1] - signal[i]) / dt) for i in range(len(signal) - 1)) if len(signal) > 1 else 0.0
            cmd = [abs(r[AXIS_TO_RC[seg.axis]]) for r in window]
            run_metrics.append(
                RunMetric(
                    start_s=window[0]["t"],
                    end_s=window[-1]["t"],
                    duration_s=window[-1]["t"] - window[0]["t"],
                    response_delay_s=(idx10 * dt) if idx10 is not None else None,
                    peak_rate=peak,
                    settle_time_s=max(0.0, (len(signal) - 1 - settle_idx) * dt),
                    max_accel=accel,
                    overshoot=overshoot,
                    residual_drift=residual_drift,
                    command_mean_abs=statistics.fmean(cmd),
                )
            )
            used_segments.append(seg)

        if not run_metrics:
            continue

        confidences = [s.confidence_score for s in used_segments]
        out[maneuver] = {
            "count": len(run_metrics),
            "confidence": {
                "mean_score": round(statistics.fmean(confidences), 3),
                "high_count": sum(1 for s in used_segments if s.confidence_label == "high"),
                "medium_count": sum(1 for s in used_segments if s.confidence_label == "medium"),
                "low_count": sum(1 for s in used_segments if s.confidence_label == "low"),
            },
            "aggregate": {
                "response_delay_s_mean": round(statistics.fmean([m.response_delay_s for m in run_metrics if m.response_delay_s is not None]), 3),
                "peak_rate_mean": round(statistics.fmean([m.peak_rate for m in run_metrics]), 3),
                "max_accel_mean": round(statistics.fmean([m.max_accel for m in run_metrics]), 3),
                "settle_time_s_mean": round(statistics.fmean([m.settle_time_s for m in run_metrics]), 3),
                "overshoot_mean": round(statistics.fmean([m.overshoot for m in run_metrics]), 3),
                "residual_drift_mean": round(statistics.fmean([m.residual_drift for m in run_metrics]), 3),
            },
            "runs": [asdict(m) for m in run_metrics],
        }

    return out


def hover_metrics(usable: List[dict], neutral_windows) -> dict:
    runs = []
    for i, j in neutral_windows:
        window = usable[i : j + 1]
        duration = window[-1]["t"] - window[0]["t"]
        if duration < 2.0:
            continue
        hs = [math.hypot(r["vx_mph"], r["vy_mph"]) * MPS_PER_MPH for r in window]
        vs = [r["vz_mph"] * MPS_PER_MPH for r in window]
        alt = [r["alt_ft"] * 0.3048 for r in window]
        runs.append(
            {
                "start_s": window[0]["t"],
                "end_s": window[-1]["t"],
                "duration_s": duration,
                "horizontal_rms_mps": math.sqrt(statistics.fmean(v * v for v in hs)),
                "vertical_rms_mps": math.sqrt(statistics.fmean(v * v for v in vs)),
                "alt_std_m": statistics.pstdev(alt) if len(alt) > 1 else 0.0,
            }
        )
    if not runs:
        return {}
    return {
        "count": len(runs),
        "aggregate": {
            "horizontal_rms_mps_mean": round(statistics.fmean(r["horizontal_rms_mps"] for r in runs), 3),
            "vertical_rms_mps_mean": round(statistics.fmean(r["vertical_rms_mps"] for r in runs), 3),
            "alt_std_m_mean": round(statistics.fmean(r["alt_std_m"] for r in runs), 3),
        },
        "runs": runs,
    }


def evidence_table(metrics: Dict[str, dict]) -> Dict[str, dict]:
    table = {}
    mappings = {
        "forward speed behavior": "forward_step",
        "lateral speed behavior": "lateral_right",
        "climb behavior": "climb",
        "descent behavior": "descent",
        "yaw behavior": "yaw_right",
        "acceleration": "forward_step",
        "braking": "forward_step",
        "overshoot": "forward_step",
        "settle time": "forward_step",
    }
    for item, key in mappings.items():
        m = metrics.get(key)
        if not m:
            table[item] = {"classification": "designer_assumption", "reason": f"no clean {key} segments"}
            continue
        conf = m["confidence"]
        if conf["high_count"] >= 2:
            cls = "directly_measured"
        elif conf["high_count"] + conf["medium_count"] >= 1:
            cls = "estimated_from_limited_segments"
        else:
            cls = "designer_assumption"
        table[item] = {
            "classification": cls,
            "source_maneuver": key,
            "confidence": conf,
            "notes": "computed from segment aggregates" if cls != "designer_assumption" else "insufficient clean data",
        }
    return table


def read_sim_runs(patterns: List[str]) -> Dict[str, List[dict]]:
    by_maneuver = defaultdict(list)
    for pattern in patterns:
        for path in glob.glob(pattern):
            with open(path, newline="", encoding="utf-8") as fp:
                reader = csv.DictReader(fp)
                rows = list(reader)
            if not rows:
                continue
            name = rows[0].get("maneuver_name", os.path.basename(path)).strip().lower().replace(" ", "_")
            by_maneuver[name].append({"path": path, "rows": rows})
    return by_maneuver


def maneuver_aliases() -> Dict[str, List[str]]:
    return {
        "forward_step": ["forward_step_response", "forward_step", "maneuver_forwardstep"],
        "lateral_right": ["lateral_step_response", "lateral_step", "maneuver_lateralstep"],
        "climb": ["vertical_step_response", "vertical_step", "maneuver_verticalstep"],
        "descent": ["vertical_step_response", "vertical_step", "maneuver_verticalstep"],
        "yaw_right": ["yaw_step_response", "yaw_step", "maneuver_yawstep"],
        "hover_hold": ["hover_hold", "maneuver_hoverhold"],
    }


def summarize_sim_run(run: dict, category: str) -> dict:
    rows = run["rows"]
    times = [float(r.get("time_s", 0) or 0) for r in rows]
    if category in ("forward_step", "lateral_right"):
        signal = [float(r.get("horizontal_speed_mps", 0) or 0) for r in rows]
    elif category in ("climb", "descent"):
        signal = [abs(float(r.get("vertical_speed_mps", 0) or 0)) for r in rows]
    elif category == "yaw_right":
        signal = [abs(float(r.get("yaw_rate_degps", 0) or 0)) for r in rows]
    else:
        signal = [float(r.get("horizontal_speed_mps", 0) or 0) for r in rows]

    if not signal:
        return {}
    peak = max(signal)
    idx10 = next((i for i, v in enumerate(signal) if v >= 0.1 * peak), None)
    settle_idx = next((i for i in range(len(signal) - 1, -1, -1) if signal[i] > 0.12 * peak), 0)
    dt = (times[1] - times[0]) if len(times) > 1 else 0.02
    accel = max(((signal[i + 1] - signal[i]) / dt) for i in range(len(signal) - 1)) if len(signal) > 1 and dt > 0 else 0.0
    steady = statistics.fmean(signal[-min(5, len(signal)) :])
    return {
        "path": run["path"],
        "response_delay_s": (idx10 * dt) if idx10 is not None else None,
        "peak_rate": peak,
        "settle_time_s": max(0.0, (len(signal) - 1 - settle_idx) * dt),
        "max_accel": accel,
        "overshoot": max(0.0, peak - steady),
        "residual_drift": statistics.fmean(signal[-min(8, len(signal)) :]),
    }


def compare_real_vs_sim(real_metrics: Dict[str, dict], sim_runs: Dict[str, List[dict]]) -> Dict[str, dict]:
    aliases = maneuver_aliases()
    out = {}
    for category in ["forward_step", "lateral_right", "climb", "descent", "yaw_right", "hover_hold"]:
        real = real_metrics.get(category)
        matching = []
        for alias in aliases.get(category, []):
            matching.extend(sim_runs.get(alias, []))
        sim_summaries = [summarize_sim_run(run, category) for run in matching]
        sim_summaries = [s for s in sim_summaries if s]

        if not real:
            out[category] = {"status": "no_real_data", "sim_runs": len(sim_summaries)}
            continue
        if not sim_summaries:
            out[category] = {
                "status": "real_only",
                "real": real.get("aggregate", {}),
                "note": "no simulator benchmark CSVs supplied to analysis",
            }
            continue

        sim_agg = {
            "response_delay_s_mean": statistics.fmean([s["response_delay_s"] for s in sim_summaries if s["response_delay_s"] is not None]),
            "peak_rate_mean": statistics.fmean([s["peak_rate"] for s in sim_summaries]),
            "max_accel_mean": statistics.fmean([s["max_accel"] for s in sim_summaries]),
            "settle_time_s_mean": statistics.fmean([s["settle_time_s"] for s in sim_summaries]),
            "overshoot_mean": statistics.fmean([s["overshoot"] for s in sim_summaries]),
            "residual_drift_mean": statistics.fmean([s["residual_drift"] for s in sim_summaries]),
        }

        deltas = {}
        for k, real_value in real.get("aggregate", {}).items():
            sim_key = k
            if sim_key in sim_agg:
                deltas[k] = round(sim_agg[sim_key] - real_value, 3)

        out[category] = {
            "status": "compared",
            "real": real.get("aggregate", {}),
            "sim": {k: round(v, 3) for k, v in sim_agg.items()},
            "delta_sim_minus_real": deltas,
            "sim_sources": [s["path"] for s in sim_summaries],
        }
    return out


def write_markdown(path: str, payload: dict):
    metrics = payload["metrics"]
    evidence = payload["evidence_classification"]
    comp = payload["sim_vs_real_comparison"]

    lines = [
        "# Airdata Benchmark Summary (Mar 30, 2026 08:31 UTC)",
        "",
        "Source CSV (used directly):",
        f"- `{payload['source_csv']}`",
        "",
        "## Segmentation confidence overview",
        "",
        "| Maneuver | Count | High | Medium | Low | Peak mean | Delay mean |",
        "|---|---:|---:|---:|---:|---:|---:|",
    ]

    for name in sorted(k for k in metrics.keys() if k != "hover_hold"):
        info = metrics[name]
        conf = info["confidence"]
        agg = info["aggregate"]
        lines.append(
            f"| {name} | {info['count']} | {conf['high_count']} | {conf['medium_count']} | {conf['low_count']} | {agg['peak_rate_mean']:.2f} | {agg['response_delay_s_mean']:.2f} |"
        )

    hover = metrics.get("hover_hold")
    if hover:
        lines += ["", "## Hover hold", "", f"- Runs: {hover['count']}", f"- Horizontal RMS mean: {hover['aggregate']['horizontal_rms_mps_mean']:.3f} m/s", f"- Vertical RMS mean: {hover['aggregate']['vertical_rms_mps_mean']:.3f} m/s"]

    lines += ["", "## Measured vs inferred classification", "", "| Target | Classification | Evidence |", "|---|---|---|"]
    for target, row in evidence.items():
        lines.append(f"| {target} | {row['classification']} | {row.get('reason', row.get('notes', ''))} |")

    lines += ["", "## Sim vs real comparison", "", "| Category | Status | Notes |", "|---|---|---|"]
    for cat, row in comp.items():
        note = row.get("note", "")
        if row.get("status") == "compared":
            note = "has side-by-side metric deltas"
        lines.append(f"| {cat} | {row.get('status')} | {note} |")

    lines += [
        "",
        "## Confidence policy",
        "",
        "- **directly_measured**: at least 2 high-confidence segments for that target maneuver.",
        "- **estimated_from_limited_segments**: only medium-confidence or single high-confidence support.",
        "- **designer_assumption**: no reliable segments in this log.",
    ]

    with open(path, "w", encoding="utf-8") as fp:
        fp.write("\n".join(lines) + "\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv")
    ap.add_argument("--json-out", default="Docs/airdata_mar30_analysis.json")
    ap.add_argument("--summary-out", default="Docs/Airdata_Mar30_2026_Benchmark_Summary.md")
    ap.add_argument("--sim-csv-glob", action="append", default=[])
    args = ap.parse_args()

    rows = load_airdata(args.csv)
    usable, segments, neutral_windows = segment_maneuvers(rows)
    metrics = metrics_for_segments(usable, segments)
    hover = hover_metrics(usable, neutral_windows)
    if hover:
        metrics["hover_hold"] = hover

    evidence = evidence_table(metrics)
    sim_runs = read_sim_runs(args.sim_csv_glob)
    comparison = compare_real_vs_sim(metrics, sim_runs)

    payload = {
        "source_csv": args.csv,
        "total_rows": len(rows),
        "usable_rows": len(usable),
        "duration_s": round((usable[-1]["t"] - usable[0]["t"]) if usable else 0.0, 3),
        "segmentation": {
            "active_threshold_percent": 16.0,
            "cross_axis_threshold_percent": 14.0,
            "min_segment_seconds": 0.7,
            "neutral_threshold_percent": 8.0,
            "expected_order": EXPECTED_ORDER,
            "confidence_labels": {
                "high": ">=0.78",
                "medium": ">=0.56 and <0.78",
                "low": "<0.56",
            },
        },
        "segments": [asdict(s) for s in segments],
        "metrics": metrics,
        "evidence_classification": evidence,
        "sim_vs_real_comparison": comparison,
        "sim_csv_inputs": args.sim_csv_glob,
    }

    with open(args.json_out, "w", encoding="utf-8") as fp:
        json.dump(payload, fp, indent=2)
    write_markdown(args.summary_out, payload)

    print(
        json.dumps(
            {
                "segments": len(segments),
                "maneuvers": sorted({s.maneuver for s in segments}),
                "json_out": args.json_out,
                "summary_out": args.summary_out,
                "sim_inputs": args.sim_csv_glob,
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
