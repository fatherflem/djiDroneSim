#!/usr/bin/env python3
"""Airdata + simulator benchmark comparison helper.

Focus:
1) confidence-labeled maneuver segmentation from Airdata RC + motion traces
2) explicit measured vs inferred evidence tagging
3) simulator benchmark ingestion and side-by-side deltas vs real data
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
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

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
COMPARISON_CATEGORIES = [
    "hover_hold",
    "forward_step",
    "lateral_right",
    "lateral_left",
    "climb",
    "descent",
    "yaw_right",
    "yaw_left",
]
AMPLITUDE_TARGETS = [
    "forward_step",
    "lateral_right",
    "lateral_left",
    "climb",
    "descent",
    "yaw_right",
    "yaw_left",
]


def _confidence_rank(label: str) -> int:
    mapping = {"high": 3, "medium": 2, "low": 1}
    return mapping.get((label or "").strip().lower(), 0)


def _is_provisional_confidence(label: str, provenance: str) -> bool:
    normalized_prov = (provenance or "").strip().lower()
    return _confidence_rank(label) < _confidence_rank("high") or normalized_prov != "directly_measured"


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

    expected_idx = 0
    for seg in segments:
        if seg.maneuver in EXPECTED_ORDER:
            idx = EXPECTED_ORDER.index(seg.maneuver)
            if idx >= expected_idx:
                expected_idx = idx
            else:
                seg.confidence_reasons.append("out-of-order vs expected protocol")
        seg.sequence_index = expected_idx

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
        delays = [m.response_delay_s for m in run_metrics if m.response_delay_s is not None]
        out[maneuver] = {
            "count": len(run_metrics),
            "confidence": {
                "mean_score": round(statistics.fmean(confidences), 3),
                "high_count": sum(1 for s in used_segments if s.confidence_label == "high"),
                "medium_count": sum(1 for s in used_segments if s.confidence_label == "medium"),
                "low_count": sum(1 for s in used_segments if s.confidence_label == "low"),
            },
            "aggregate": {
                "response_delay_s_mean": round(statistics.fmean(delays), 3) if delays else None,
                "peak_rate_mean": round(statistics.fmean([m.peak_rate for m in run_metrics]), 3),
                "max_accel_mean": round(statistics.fmean([m.max_accel for m in run_metrics]), 3),
                "settle_time_s_mean": round(statistics.fmean([m.settle_time_s for m in run_metrics]), 3),
                "overshoot_mean": round(statistics.fmean([m.overshoot for m in run_metrics]), 3),
                "residual_drift_mean": round(statistics.fmean([m.residual_drift for m in run_metrics]), 3),
            },
            "runs": [asdict(m) for m in run_metrics],
        }

    return out


def _channel_for_maneuver(maneuver: str) -> Tuple[str, int]:
    if maneuver == "forward_step":
        return "rc_elevator", 1
    if maneuver == "lateral_right":
        return "rc_aileron", 1
    if maneuver == "lateral_left":
        return "rc_aileron", -1
    if maneuver == "climb":
        return "rc_throttle", 1
    if maneuver == "descent":
        return "rc_throttle", -1
    if maneuver == "yaw_right":
        return "rc_rudder", 1
    if maneuver == "yaw_left":
        return "rc_rudder", -1
    return "", 1


def _plateau_magnitude(window: List[dict], channel: str, sign: int) -> Optional[float]:
    aligned = [sign * row[channel] for row in window]
    active = [v for v in aligned if v > 0]
    if not active:
        return None
    peak = max(active)
    plateau_gate = peak * 0.7
    plateau = [v for v in active if v >= plateau_gate]
    samples = plateau if plateau else active
    return statistics.median(samples)


def derive_input_amplitudes(usable: List[dict], segments: List[Segment]) -> Dict[str, dict]:
    by_maneuver: Dict[str, List[dict]] = defaultdict(list)
    for seg in segments:
        channel, sign = _channel_for_maneuver(seg.maneuver)
        if not channel:
            continue
        window = usable[seg.start : seg.end + 1]
        plateau = _plateau_magnitude(window, channel, sign)
        if plateau is None:
            continue
        by_maneuver[seg.maneuver].append(
            {
                "start_s": seg.start_s,
                "end_s": seg.end_s,
                "confidence_label": seg.confidence_label,
                "confidence_score": seg.confidence_score,
                "plateau_percent": round(plateau, 3),
            }
        )

    summary = {}
    for maneuver in AMPLITUDE_TARGETS:
        runs = by_maneuver.get(maneuver, [])
        if not runs:
            summary[maneuver] = {
                "channel": _channel_for_maneuver(maneuver)[0],
                "classification": "uncertain",
                "reason": "no segmented RC plateau windows for this maneuver in this log",
                "derived_from": "none",
                "recommended_percent": None,
                "recommended_normalized": None,
                "consistency": "insufficient_data",
                "runs_used": [],
            }
            continue

        high = [r for r in runs if r["confidence_label"] == "high"]
        medium = [r for r in runs if r["confidence_label"] == "medium"]
        selected = high if high else (high + medium)
        if not selected:
            selected = runs
        values = [r["plateau_percent"] for r in selected]
        rec_percent = statistics.median(values)
        spread = statistics.pstdev(values) if len(values) > 1 else 0.0
        cv = spread / rec_percent if rec_percent > 1e-4 else 1.0

        if len(high) >= 3 and cv <= 0.18:
            classification = "directly_measured_from_clean_rc_plateaus"
        elif len(high) >= 1 or len(medium) >= 2:
            classification = "estimated_from_noisy_or_limited_segments"
        else:
            classification = "uncertain"

        consistency = "high" if cv <= 0.1 else "moderate" if cv <= 0.2 else "low"
        _, maneuver_sign = _channel_for_maneuver(maneuver)
        summary[maneuver] = {
            "channel": _channel_for_maneuver(maneuver)[0],
            "classification": classification,
            "derived_from": "segmented_rc_plateaus",
            "recommended_percent": round(rec_percent, 1),
            "recommended_normalized": round(max(-1.0, min(1.0, maneuver_sign * rec_percent / 100.0)), 3),
            "consistency": consistency,
            "spread_percent_stddev": round(spread, 2),
            "runs_used": selected,
        }

    lat_left = summary.get("lateral_left")
    if lat_left and lat_left.get("recommended_normalized") is None:
        right = summary.get("lateral_right", {})
        right_norm = right.get("recommended_normalized")
        if right_norm is not None:
            lat_left.update(
                {
                    "classification": "estimated_from_noisy_or_limited_segments",
                    "reason": "no clean lateral_left RC plateau windows; mirrored from lateral_right median magnitude",
                    "derived_from": "symmetry_from_lateral_right",
                    "recommended_percent": round(abs(right_norm) * 100.0, 1),
                    "recommended_normalized": round(-abs(right_norm), 3),
                    "consistency": "inferred",
                    "runs_used": [],
                }
            )

    return summary


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
        "lateral right behavior": "lateral_right",
        "lateral left behavior": "lateral_left",
        "climb behavior": "climb",
        "descent behavior": "descent",
        "yaw right behavior": "yaw_right",
        "yaw left behavior": "yaw_left",
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


def normalize_protocol_category(raw: str) -> str:
    n = (raw or "").strip().lower().replace(" ", "_")
    aliases = {
        "forward": "forward_step",
        "lateral": "lateral_right",
        "vertical": "climb",
        "yaw": "yaw_right",
        "hover": "hover_hold",
    }
    return aliases.get(n, n)


def infer_category_from_row(row: dict) -> str:
    from_protocol = normalize_protocol_category(row.get("protocol_category", ""))
    if from_protocol:
        return from_protocol

    mname = normalize_protocol_category(row.get("maneuver_name", ""))
    if "lateral" in mname and "left" in mname:
        return "lateral_left"
    if "lateral" in mname:
        return "lateral_right"
    if "yaw" in mname and "left" in mname:
        return "yaw_left"
    if "yaw" in mname:
        return "yaw_right"
    if "descent" in mname:
        return "descent"
    if "climb" in mname or "vertical" in mname:
        return "climb"
    if "forward" in mname:
        return "forward_step"
    if "hover" in mname:
        return "hover_hold"

    pitch = _safe_float(row, "input_pitch")
    roll = _safe_float(row, "input_roll")
    throttle = _safe_float(row, "input_throttle")
    yaw = _safe_float(row, "input_yaw")
    axis_values = {
        "forward_step": abs(pitch),
        "lateral_right": abs(roll),
        "climb": abs(throttle),
        "yaw_right": abs(yaw),
    }
    category = max(axis_values, key=axis_values.get)
    if axis_values[category] < 1e-4:
        return "hover_hold"

    if category == "lateral_right" and roll < 0:
        return "lateral_left"
    if category == "yaw_right" and yaw < 0:
        return "yaw_left"
    if category == "climb" and throttle < 0:
        return "descent"
    return category


def _provenance_from_legacy_evidence(evidence: str) -> str:
    normalized = (evidence or "").strip().lower()
    if "directly_measured" in normalized:
        return "directly_measured"
    if "estimated" in normalized:
        return "estimated_from_limited_segments"
    if "assumption" in normalized:
        return "designer_assumption"
    return "designer_assumption"


def read_session_amplitude_metadata(manifest_path: str) -> Dict[str, dict]:
    category_meta: Dict[str, dict] = {}
    with open(manifest_path, encoding="utf-8") as fp:
        for line in fp:
            line = line.strip()
            if not line:
                continue
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue

            if entry.get("type") != "session_metadata":
                continue
            amps = (
                entry.get("benchmark_settings", {}).get("default_protocol_input_amplitudes", [])
                if isinstance(entry.get("benchmark_settings", {}), dict)
                else []
            )
            for amp in amps:
                category = normalize_protocol_category(amp.get("protocol_category", ""))
                if not category:
                    continue
                confidence = (amp.get("confidence_label") or "").strip().lower()
                provenance = (amp.get("provenance_classification") or "").strip().lower()
                if not confidence:
                    confidence = "medium"
                if not provenance:
                    provenance = _provenance_from_legacy_evidence(amp.get("evidence_classification", ""))
                category_meta[category] = {
                    "confidence_label": confidence,
                    "provenance": provenance,
                    "provisional": amp.get("provisional")
                    if isinstance(amp.get("provisional"), bool)
                    else _is_provisional_confidence(confidence, provenance),
                    "active_axis_magnitude": amp.get("active_axis_magnitude"),
                    "notes": amp.get("notes", ""),
                }
            break
    return category_meta


def parse_session_manifest(manifest_path: str) -> dict:
    session = {"session_metadata": None, "runs": []}
    if not os.path.exists(manifest_path):
        return session

    with open(manifest_path, encoding="utf-8") as fp:
        for line in fp:
            line = line.strip()
            if not line:
                continue
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue
            et = entry.get("type")
            if et == "session_metadata":
                session["session_metadata"] = entry
            elif et == "run":
                session["runs"].append(entry)
    return session


def identify_primary_protocol_runs(manifest: dict) -> dict:
    expected_order = {name: idx + 1 for idx, name in enumerate(COMPARISON_CATEGORIES)}
    primary = []
    excluded = []
    seen_categories: Set[str] = set()

    all_runs = sorted(manifest.get("runs", []), key=lambda r: int(r.get("run_number", 0)))
    for run in all_runs:
        category = normalize_protocol_category(run.get("protocol_category", ""))
        run_number = int(run.get("run_number", 0))
        run_source = (run.get("run_source") or "").strip().lower()
        protocol_order = int(run.get("protocol_order", -1))

        if category not in expected_order:
            excluded.append(
                {
                    "run_number": run_number,
                    "category": category,
                    "reason": "outside_core_comparison_categories",
                }
            )
            continue

        if run_source != "full_protocol":
            excluded.append(
                {
                    "run_number": run_number,
                    "category": category,
                    "reason": f"run_source_{run_source or 'unknown'}_not_full_protocol",
                }
            )
            continue

        expected = expected_order[category]
        if protocol_order != expected:
            excluded.append(
                {
                    "run_number": run_number,
                    "category": category,
                    "reason": f"protocol_order_{protocol_order}_expected_{expected}",
                }
            )
            continue

        if category in seen_categories:
            excluded.append(
                {
                    "run_number": run_number,
                    "category": category,
                    "reason": "duplicate_category_in_full_protocol",
                }
            )
            continue

        seen_categories.add(category)
        primary.append(
            {
                "run_number": run_number,
                "category": category,
                "maneuver_name": run.get("maneuver_name", ""),
                "protocol_order": protocol_order,
                "run_source": run_source,
                "manifest_entry": run,
            }
        )

    return {"primary_runs": primary, "excluded_runs": excluded}


def read_sim_runs(patterns: List[str]) -> List[dict]:
    runs = []
    seen = set()
    session_meta_cache: Dict[str, dict] = {}
    amp_cache: Dict[str, Dict[str, dict]] = {}
    protocol_cache: Dict[str, dict] = {}
    for pattern in patterns:
        for path in sorted(glob.glob(pattern, recursive=True)):
            if not path.endswith(".csv"):
                continue
            full = str(Path(path).resolve())
            if full in seen:
                continue
            seen.add(full)
            with open(path, newline="", encoding="utf-8") as fp:
                reader = csv.DictReader(fp)
                rows = list(reader)
            if not rows:
                continue
            category = infer_category_from_row(rows[0])
            parent = Path(path).resolve().parent
            manifest_path = parent / "session_manifest.jsonl"
            amplitude_meta = None
            run_manifest_entry = None
            in_primary_protocol = None
            exclusion_reason = None
            if manifest_path.exists():
                manifest_key = str(manifest_path)
                if manifest_key not in amp_cache:
                    amp_cache[manifest_key] = read_session_amplitude_metadata(manifest_key)
                if manifest_key not in session_meta_cache:
                    session_meta_cache[manifest_key] = parse_session_manifest(manifest_key)
                    protocol_cache[manifest_key] = identify_primary_protocol_runs(session_meta_cache[manifest_key])
                amplitude_meta = amp_cache[manifest_key].get(category)

                stem = Path(path).stem
                for entry in session_meta_cache[manifest_key].get("runs", []):
                    if int(entry.get("run_number", 0)) == int(rows[0].get("run_number", 0)) or stem.startswith(
                        f"run_{int(entry.get('run_number', 0)):03d}_"
                    ):
                        run_manifest_entry = entry
                        break

                selected = {
                    item["run_number"]: item
                    for item in protocol_cache[manifest_key].get("primary_runs", [])
                }
                excluded = {
                    item["run_number"]: item["reason"]
                    for item in protocol_cache[manifest_key].get("excluded_runs", [])
                }
                run_num = int(rows[0].get("run_number", 0))
                in_primary_protocol = run_num in selected
                if run_num in excluded:
                    exclusion_reason = excluded[run_num]

            runs.append(
                {
                    "path": path,
                    "rows": rows,
                    "category": category,
                    "amplitude_metadata": amplitude_meta,
                    "run_manifest_entry": run_manifest_entry,
                    "in_primary_protocol": in_primary_protocol,
                    "exclusion_reason": exclusion_reason,
                }
            )
    return runs


def _get_times(rows: List[dict]) -> List[float]:
    if rows and "time_s" in rows[0]:
        return [_safe_float(r, "time_s") for r in rows]
    return list(range(len(rows)))


def summarize_sim_run(run: dict, category: str) -> dict:
    rows = run["rows"]
    analysis_rows = [r for r in rows if (r.get("benchmark_phase") or "").strip().lower() in ("input", "settle")]
    if not analysis_rows:
        analysis_rows = rows
    times = _get_times(analysis_rows)
    mode = rows[0].get("maneuver_mode", "Unknown") if rows else "Unknown"
    input_only_rows = [r for r in analysis_rows if (r.get("benchmark_phase") or "").strip().lower() == "input"]
    settle_only_rows = [r for r in analysis_rows if (r.get("benchmark_phase") or "").strip().lower() == "settle"]

    if category in ("forward_step", "lateral_right", "lateral_left"):
        signal = [abs(_safe_float(r, "forward_speed_mps")) for r in analysis_rows] if category == "forward_step" else [
            abs(_safe_float(r, "lateral_speed_mps")) for r in analysis_rows
        ]
    elif category in ("climb", "descent"):
        signal = [abs(_safe_float(r, "vertical_speed_mps")) for r in analysis_rows]
    elif category in ("yaw_right", "yaw_left"):
        signal = [abs(_safe_float(r, "yaw_rate_degps")) for r in analysis_rows]
    else:
        hs = [_safe_float(r, "horizontal_speed_mps") for r in analysis_rows]
        vs = [_safe_float(r, "vertical_speed_mps") for r in analysis_rows]
        alt = [_safe_float(r, "pos_y_m") for r in analysis_rows]
        if not hs:
            return {}
        return {
            "path": run["path"],
            "mode": mode,
            "response_delay_s": 0.0,
            "peak_rate": max(hs) if hs else 0.0,
            "settle_time_s": 0.0,
            "max_accel": 0.0,
            "overshoot": 0.0,
            "residual_drift": statistics.fmean(abs(v) for v in hs + vs),
            "vertical_rms_mps": math.sqrt(statistics.fmean(v * v for v in vs)) if vs else 0.0,
            "horizontal_rms_mps": math.sqrt(statistics.fmean(v * v for v in hs)) if hs else 0.0,
            "alt_std_m": statistics.pstdev(alt) if len(alt) > 1 else 0.0,
            "measured_from": "sim_csv_direct",
        }

    if not signal:
        return {}

    peak = max(signal)
    idx10 = next((i for i, v in enumerate(signal) if peak > 0 and v >= 0.1 * peak), None)
    settle_idx = next((i for i in range(len(signal) - 1, -1, -1) if signal[i] > 0.12 * peak), 0)
    dt = (times[1] - times[0]) if len(times) > 1 else 0.02
    if dt <= 0:
        dt = 0.02

    accel = max(((signal[i + 1] - signal[i]) / dt) for i in range(len(signal) - 1)) if len(signal) > 1 else 0.0
    steady_source = settle_only_rows if settle_only_rows else analysis_rows[-min(8, len(analysis_rows)) :]
    if category == "forward_step":
        steady_values = [abs(_safe_float(r, "forward_speed_mps")) for r in steady_source]
    elif category in ("lateral_right", "lateral_left"):
        steady_values = [abs(_safe_float(r, "lateral_speed_mps")) for r in steady_source]
    elif category in ("climb", "descent"):
        steady_values = [abs(_safe_float(r, "vertical_speed_mps")) for r in steady_source]
    else:
        steady_values = [abs(_safe_float(r, "yaw_rate_degps")) for r in steady_source]
    steady = statistics.fmean(steady_values) if steady_values else statistics.fmean(signal[-min(5, len(signal)) :])
    return {
        "path": run["path"],
        "mode": mode,
        "response_delay_s": (idx10 * dt) if idx10 is not None else None,
        "peak_rate": peak,
        "settle_time_s": max(0.0, (len(signal) - 1 - settle_idx) * dt),
        "max_accel": accel,
        "overshoot": max(0.0, peak - steady),
        "residual_drift": statistics.fmean(signal[-min(8, len(signal)) :]),
        "measured_from": "sim_csv_direct",
    }


def _agg_metric(samples: List[dict], key: str) -> Optional[float]:
    values = [s[key] for s in samples if s.get(key) is not None]
    return statistics.fmean(values) if values else None


def comparison_note(delta: Optional[float], magnitude_threshold: float = 0.15) -> str:
    if delta is None:
        return "insufficient_data"
    if abs(delta) <= magnitude_threshold:
        return "matches_well"
    return "too_aggressive" if delta > 0 else "too_sluggish"


def compare_real_vs_sim(real_metrics: Dict[str, dict], sim_runs: List[dict]) -> Dict[str, dict]:
    by_cat = defaultdict(list)
    for run in sim_runs:
        if run.get("in_primary_protocol") is False:
            continue
        by_cat[run["category"]].append(run)

    out = {}
    for category in COMPARISON_CATEGORIES:
        real = real_metrics.get(category)
        matching = by_cat.get(category, [])
        sim_summaries = [summarize_sim_run(run, category) for run in matching]
        sim_summaries = [s for s in sim_summaries if s]
        amplitude_metadata = [run.get("amplitude_metadata") for run in matching if run.get("amplitude_metadata")]

        if not real:
            out[category] = {
                "status": "no_real_data",
                "sim_runs": len(sim_summaries),
                "real_evidence": "designer_assumption",
            }
            continue

        if not sim_summaries:
            out[category] = {
                "status": "real_only",
                "real": real.get("aggregate", {}),
                "real_evidence": "directly_measured_from_airdata",
                "note": "no simulator benchmark CSVs supplied for this category",
            }
            continue

        sim_agg = {
            "response_delay_s_mean": _agg_metric(sim_summaries, "response_delay_s"),
            "peak_rate_mean": _agg_metric(sim_summaries, "peak_rate"),
            "max_accel_mean": _agg_metric(sim_summaries, "max_accel"),
            "settle_time_s_mean": _agg_metric(sim_summaries, "settle_time_s"),
            "overshoot_mean": _agg_metric(sim_summaries, "overshoot"),
            "residual_drift_mean": _agg_metric(sim_summaries, "residual_drift"),
            "horizontal_rms_mps_mean": _agg_metric(sim_summaries, "horizontal_rms_mps"),
            "vertical_rms_mps_mean": _agg_metric(sim_summaries, "vertical_rms_mps"),
            "alt_std_m_mean": _agg_metric(sim_summaries, "alt_std_m"),
        }

        deltas = {}
        assessment = {}
        for k, real_value in real.get("aggregate", {}).items():
            sim_value = sim_agg.get(k)
            if real_value is None or sim_value is None:
                deltas[k] = None
                assessment[k] = "insufficient_data"
                continue
            delta = sim_value - real_value
            deltas[k] = round(delta, 3)
            threshold = 0.03 if "delay" in k else 0.1 if "overshoot" in k else 0.15
            assessment[k] = comparison_note(delta, threshold)

        amplitude_confidence = "unknown"
        amplitude_provenance = "unknown"
        amplitude_provisional = False
        if amplitude_metadata:
            amplitude_confidence = min(
                (m.get("confidence_label", "unknown") for m in amplitude_metadata),
                key=_confidence_rank,
            )
            provenance_values = [m.get("provenance", "unknown") for m in amplitude_metadata]
            amplitude_provenance = provenance_values[0] if len(set(provenance_values)) == 1 else "mixed"
            amplitude_provisional = any(bool(m.get("provisional")) for m in amplitude_metadata) or _is_provisional_confidence(
                amplitude_confidence, amplitude_provenance
            )

        if amplitude_provisional:
            for key, verdict in list(assessment.items()):
                if verdict not in ("insufficient_data",):
                    assessment[key] = f"{verdict}_provisional_input_amplitude"

        out[category] = {
            "status": "compared",
            "real": real.get("aggregate", {}),
            "sim": {k: (round(v, 3) if v is not None else None) for k, v in sim_agg.items()},
            "delta_sim_minus_real": deltas,
            "delta_assessment": assessment,
            "real_evidence": "directly_measured_from_airdata",
            "sim_evidence": "directly_measured_from_sim_csv",
            "sim_input_amplitude_confidence": amplitude_confidence,
            "sim_input_amplitude_provenance": amplitude_provenance,
            "sim_input_amplitude_provisional": amplitude_provisional,
            "sim_sources": [s["path"] for s in sim_summaries],
            "sim_modes": sorted({s["mode"] for s in sim_summaries}),
        }
    return out


def discover_sim_inputs(explicit_globs: List[str], sim_root: Optional[str]) -> List[str]:
    patterns = list(explicit_globs)
    if sim_root:
        patterns.append(os.path.join(sim_root, "**", "*.csv"))
    return patterns


def write_markdown(path: str, payload: dict):
    metrics = payload["metrics"]
    evidence = payload["evidence_classification"]
    comp = payload["sim_vs_real_comparison"]
    amplitudes = payload.get("recommended_protocol_amplitudes", {})

    lines = [
        "# Airdata + Simulator Benchmark Comparison",
        "",
        "Source CSV (used directly):",
        f"- `{payload['source_csv']}`",
        f"- Sim CSV patterns: `{', '.join(payload['sim_csv_inputs']) if payload['sim_csv_inputs'] else '(none)'}`",
        "",
        "## Simulator session selection",
        "",
        f"- Primary protocol runs included: {len(payload.get('sim_primary_protocol_runs', []))}",
        f"- Runs excluded from primary protocol comparison: {len(payload.get('sim_excluded_runs', []))}",
        "",
    ]

    if payload.get("sim_primary_protocol_runs"):
        lines += [
            "| Included run # | Category | Protocol order | Run source | File |",
            "|---:|---|---:|---|---|",
        ]
        for row in payload["sim_primary_protocol_runs"]:
            lines.append(
                f"| {row.get('run_number')} | {row.get('category')} | {row.get('protocol_order')} | {row.get('run_source')} | `{row.get('path')}` |"
            )
        lines.append("")

    if payload.get("sim_excluded_runs"):
        lines += [
            "| Excluded run # | Category | Reason | File |",
            "|---:|---|---|---|",
        ]
        for row in payload["sim_excluded_runs"]:
            lines.append(
                f"| {row.get('run_number')} | {row.get('category')} | {row.get('reason')} | `{row.get('path', '-')}` |"
            )
        lines.append("")

    lines += [
        "",
        "## Segmentation confidence overview (real flight)",
        "",
        "| Maneuver | Count | High | Medium | Low | Peak mean | Delay mean |",
        "|---|---:|---:|---:|---:|---:|---:|",
    ]

    for name in sorted(k for k in metrics.keys() if k != "hover_hold"):
        info = metrics[name]
        conf = info["confidence"]
        agg = info["aggregate"]
        delay = agg["response_delay_s_mean"] if agg["response_delay_s_mean"] is not None else 0.0
        lines.append(
            f"| {name} | {info['count']} | {conf['high_count']} | {conf['medium_count']} | {conf['low_count']} | {agg['peak_rate_mean']:.2f} | {delay:.2f} |"
        )

    hover = metrics.get("hover_hold")
    if hover:
        lines += [
            "",
            "## Hover hold",
            "",
            f"- Runs: {hover['count']}",
            f"- Horizontal RMS mean: {hover['aggregate']['horizontal_rms_mps_mean']:.3f} m/s",
            f"- Vertical RMS mean: {hover['aggregate']['vertical_rms_mps_mean']:.3f} m/s",
        ]

    lines += ["", "## Measured vs inferred classification (real baseline)", "", "| Target | Classification | Evidence |", "|---|---|---|"]
    for target, row in evidence.items():
        lines.append(f"| {target} | {row['classification']} | {row.get('reason', row.get('notes', ''))} |")

    lines += [
        "",
        "## Sim vs real deltas",
        "",
        "| Category | Status | Sim input confidence | Sim input provenance | Delay Δ | Peak Δ | Accel Δ | Settle Δ | Overshoot Δ | Verdict |",
        "|---|---|---|---|---:|---:|---:|---:|---:|---|",
    ]
    for cat in COMPARISON_CATEGORIES:
        row = comp.get(cat, {})
        if row.get("status") != "compared":
            lines.append(
                f"| {cat} | {row.get('status', 'missing')} | - | - | - | - | - | - | - | {row.get('note', 'insufficient data')} |"
            )
            continue
        d = row["delta_sim_minus_real"]
        verdict = row["delta_assessment"].get("peak_rate_mean", "insufficient_data")
        def fmt_delta(key: str) -> str:
            value = d.get(key)
            return "-" if value is None else f"{value}"
        lines.append(
            f"| {cat} | compared | {row.get('sim_input_amplitude_confidence', 'unknown')} | {row.get('sim_input_amplitude_provenance', 'unknown')} | {fmt_delta('response_delay_s_mean')} | {fmt_delta('peak_rate_mean')} | {fmt_delta('max_accel_mean')} | {fmt_delta('settle_time_s_mean')} | {fmt_delta('overshoot_mean')} | {verdict} |"
        )

    lines += [
        "",
        "## Recommended default protocol stick amplitudes (from Airdata RC)",
        "",
        "| Maneuver | RC channel | Recommended % | Normalized | Classification | Consistency |",
        "|---|---|---:|---:|---|---|",
    ]
    for maneuver in AMPLITUDE_TARGETS:
        row = amplitudes.get(maneuver, {})
        rec_pct = "-" if row.get("recommended_percent") is None else f"{row['recommended_percent']:.1f}"
        rec_norm = "-" if row.get("recommended_normalized") is None else f"{row['recommended_normalized']:.3f}"
        lines.append(
            f"| {maneuver} | {row.get('channel', '-')} | {rec_pct} | {rec_norm} | {row.get('classification', 'unknown')} | {row.get('consistency', 'unknown')} |"
        )

    strong = []
    provisional = []
    for maneuver in AMPLITUDE_TARGETS:
        cls = (amplitudes.get(maneuver, {}).get("classification", "") or "").strip().lower()
        if cls == "directly_measured_from_clean_rc_plateaus":
            strong.append(maneuver)
        else:
            provisional.append(maneuver)

    lines += [
        "",
        "## Strength of comparison categories",
        "",
        f"- **Strong categories** (directly measured default simulator amplitude): {', '.join(strong) if strong else '(none)'}",
        f"- **Provisional categories** (estimated or assumed default simulator amplitude): {', '.join(provisional) if provisional else '(none)'}",
        "",
        "## Confidence policy",
        "",
        "- **directly_measured**: at least 2 high-confidence segments for that target maneuver.",
        "- **estimated_from_limited_segments**: only medium-confidence or single high-confidence support.",
        "- **designer_assumption**: no reliable segments in this log.",
        "- If simulator input amplitude confidence/provenance is provisional, treat mismatch verdicts as directional guidance (not final tuning proof).",
    ]

    with open(path, "w", encoding="utf-8") as fp:
        fp.write("\n".join(lines) + "\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv")
    ap.add_argument("--json-out", default="Docs/airdata_mar30_analysis.json")
    ap.add_argument("--summary-out", default="Docs/Airdata_Mar30_2026_Benchmark_Summary.md")
    ap.add_argument("--sim-csv-glob", action="append", default=[])
    ap.add_argument("--sim-root", default="BenchmarkRuns")
    args = ap.parse_args()

    sim_patterns = discover_sim_inputs(args.sim_csv_glob, args.sim_root)

    rows = load_airdata(args.csv)
    usable, segments, neutral_windows = segment_maneuvers(rows)
    metrics = metrics_for_segments(usable, segments)
    hover = hover_metrics(usable, neutral_windows)
    if hover:
        metrics["hover_hold"] = hover

    evidence = evidence_table(metrics)
    amplitudes = derive_input_amplitudes(usable, segments)
    sim_runs = read_sim_runs(sim_patterns)
    comparison = compare_real_vs_sim(metrics, sim_runs)
    primary_runs = []
    excluded_runs = []
    for run in sim_runs:
        row = run["rows"][0] if run.get("rows") else {}
        run_number = int(row.get("run_number", 0))
        entry = run.get("run_manifest_entry") or {}
        info = {
            "run_number": run_number,
            "category": run.get("category"),
            "protocol_order": int(entry.get("protocol_order", row.get("protocol_order", -1))),
            "run_source": entry.get("run_source", row.get("run_source", "unknown")),
            "path": run.get("path"),
        }
        if run.get("in_primary_protocol") is True:
            primary_runs.append(info)
        elif run.get("in_primary_protocol") is False:
            info["reason"] = run.get("exclusion_reason", "not_in_primary_protocol")
            excluded_runs.append(info)

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
        "recommended_protocol_amplitudes": amplitudes,
        "sim_vs_real_comparison": comparison,
        "sim_csv_inputs": sim_patterns,
        "sim_runs_index": [
            {
                "path": r["path"],
                "category": r["category"],
                "rows": len(r["rows"]),
                "in_primary_protocol": r.get("in_primary_protocol"),
                "exclusion_reason": r.get("exclusion_reason"),
                "amplitude_metadata": r.get("amplitude_metadata"),
            }
            for r in sim_runs
        ],
        "sim_primary_protocol_runs": sorted(primary_runs, key=lambda row: row["run_number"]),
        "sim_excluded_runs": sorted(excluded_runs, key=lambda row: row["run_number"]),
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
                "sim_patterns": sim_patterns,
                "sim_run_count": len(sim_runs),
            },
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
