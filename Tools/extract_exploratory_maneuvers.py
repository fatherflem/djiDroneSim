#!/usr/bin/env python3
"""Extract candidate maneuvers from messy free-fly AirData logs.

This tool is intentionally exploratory (not acceptance-grade protocol analysis).
It detects active RC windows, classifies dominant-axis intent, and computes
simple response metrics for ranking clean candidates.
"""

import argparse
import csv
import json
import math
import statistics
from dataclasses import asdict, dataclass
from typing import Dict, List, Optional, Tuple

MPS_PER_MPH = 0.44704
AXES = ["forward", "lateral", "vertical", "yaw"]
RC_COLS = {
    "forward": "rc_elevator(percent)",
    "lateral": "rc_aileron(percent)",
    "vertical": "rc_throttle(percent)",
    "yaw": "rc_rudder(percent)",
}
CLASS_MAP = {
    ("forward", 1): "forward-dominant",
    ("forward", -1): "backward-dominant",
    ("lateral", 1): "right-strafe-dominant",
    ("lateral", -1): "left-strafe-dominant",
    ("vertical", 1): "climb-dominant",
    ("vertical", -1): "descent-dominant",
    ("yaw", 1): "yaw-right-dominant",
    ("yaw", -1): "yaw-left-dominant",
}


@dataclass
class Candidate:
    classification: str
    row_start: int
    row_end: int
    t_start_s: float
    t_end_s: float
    hold_duration_s: float
    dominant_axis: str
    dominant_sign: int
    peak_stick_pct: float
    dominant_ratio: float
    pre_neutral_s: float
    post_neutral_s: float
    input_peak_rate: float
    full_run_peak_rate: float
    carryover_after_release: float
    settle_time_s: Optional[float]
    confidence: str
    cleanliness_score: float
    notes: List[str]


def _safe_float(v: str) -> float:
    try:
        return float(v)
    except Exception:
        return 0.0


def load_rows(path: str) -> List[dict]:
    rows: List[dict] = []
    with open(path, newline="", encoding="utf-8-sig") as fp:
        reader = csv.DictReader(fp)
        for i, raw in enumerate(reader):
            rows.append(
                {
                    "idx": i,
                    "t_ms": int(_safe_float(raw.get("time(millisecond)", "0"))),
                    "mode": (raw.get("flycState") or "").strip(),
                    "vx_mph": _safe_float(raw.get(" xSpeed(mph)", "0")),
                    "vy_mph": _safe_float(raw.get(" ySpeed(mph)", "0")),
                    "vz_mph": _safe_float(raw.get(" zSpeed(mph)", "0")),
                    "heading_deg": _safe_float(raw.get(" compass_heading(degrees)") or raw.get("compass_heading(degrees)") or "0"),
                    "pitch_deg": _safe_float(raw.get(" pitch(degrees)", "0")),
                    "roll_deg": _safe_float(raw.get(" roll(degrees)", "0")),
                    "forward": _safe_float(raw.get(RC_COLS["forward"], "0")),
                    "lateral": _safe_float(raw.get(RC_COLS["lateral"], "0")),
                    "vertical": _safe_float(raw.get(RC_COLS["vertical"], "0")),
                    "yaw": _safe_float(raw.get(RC_COLS["yaw"], "0")),
                }
            )
    if not rows:
        return rows
    t0 = rows[0]["t_ms"]
    for r in rows:
        r["t_s"] = (r["t_ms"] - t0) / 1000.0
    return rows


def _body_forward_right(r: dict) -> Tuple[float, float]:
    theta = math.radians(r["heading_deg"])
    v_n = r["vx_mph"] * MPS_PER_MPH
    v_e = r["vy_mph"] * MPS_PER_MPH
    fwd = v_e * math.sin(theta) + v_n * math.cos(theta)
    right = v_e * math.cos(theta) - v_n * math.sin(theta)
    return fwd, right


def _rate_signal(rows: List[dict], axis: str, sign: int) -> List[float]:
    if not rows:
        return []
    if axis == "forward":
        vals = [sign * _body_forward_right(r)[0] for r in rows]
    elif axis == "lateral":
        vals = [sign * _body_forward_right(r)[1] for r in rows]
    elif axis == "vertical":
        # AirData zSpeed is typically negative while climbing and positive while descending.
        vals = [sign * (-r["vz_mph"] * MPS_PER_MPH) for r in rows]
    else:
        vals = []
        for i in range(1, len(rows)):
            d = ((rows[i]["heading_deg"] - rows[i - 1]["heading_deg"] + 540.0) % 360.0) - 180.0
            vals.append(sign * d / 0.1)
        if vals:
            vals = [vals[0]] + vals
        else:
            vals = [0.0]
    return [max(0.0, v) for v in vals]


def _neutral_run(rows: List[dict], start: int, step: int, neutral_th: float = 8.0) -> float:
    duration = 0.0
    i = start
    while 0 <= i < len(rows):
        if max(abs(rows[i][a]) for a in AXES) > neutral_th:
            break
        j = i + step
        if not (0 <= j < len(rows)):
            break
        duration += abs(rows[j]["t_s"] - rows[i]["t_s"])
        i = j
    return duration


def detect_windows(rows: List[dict], active_th: float = 22.0, gap_allow: int = 2) -> List[Tuple[int, int]]:
    usable_idx = [i for i, r in enumerate(rows) if r["mode"] == "P-GPS"]
    windows = []
    in_window = False
    start = -1
    gap = 0
    end = -1
    for i in usable_idx:
        active = max(abs(rows[i][a]) for a in AXES) >= active_th
        if active and not in_window:
            in_window = True
            start = i
            end = i
            gap = 0
        elif active and in_window:
            end = i
            gap = 0
        elif (not active) and in_window:
            gap += 1
            if gap > gap_allow:
                windows.append((start, end))
                in_window = False
    if in_window:
        windows.append((start, end))
    return windows


def classify_window(rows: List[dict], start: int, end: int) -> Candidate:
    window = rows[start : end + 1]
    abs_peak = {a: max(abs(r[a]) for r in window) for a in AXES}
    abs_mean = {a: statistics.fmean(abs(r[a]) for r in window) for a in AXES}
    dom_axis = max(abs_mean, key=abs_mean.get)
    sorted_vals = sorted(abs_mean.values(), reverse=True)
    runner = sorted_vals[1] if len(sorted_vals) > 1 else 0.0
    ratio = abs_mean[dom_axis] / max(1e-6, runner)
    dom_sign = 1 if statistics.fmean(r[dom_axis] for r in window) >= 0 else -1

    classification = "mixed-input window" if ratio < 1.25 else CLASS_MAP[(dom_axis, dom_sign)]
    notes = []
    if ratio < 1.25:
        notes.append("dominant-axis ratio below 1.25")

    hold = window[-1]["t_s"] - window[0]["t_s"]
    pre_neutral = _neutral_run(rows, start - 1, -1)
    post_neutral = _neutral_run(rows, end + 1, 1)

    signal_input = _rate_signal(window, dom_axis, dom_sign)
    input_peak = max(signal_input) if signal_input else 0.0

    # extend full-run into post-release settling period up to 2.5s / before next strong input
    ext_end = end
    for j in range(end + 1, min(len(rows), end + 1 + 30)):
        if rows[j]["mode"] != "P-GPS":
            break
        if max(abs(rows[j][a]) for a in AXES) > 20.0:
            break
        ext_end = j
        if rows[j]["t_s"] - rows[end]["t_s"] > 2.5:
            break
    full_window = rows[start : ext_end + 1]
    signal_full = _rate_signal(full_window, dom_axis, dom_sign)
    full_peak = max(signal_full) if signal_full else input_peak
    carryover = max(0.0, full_peak - input_peak)

    settle_time = None
    if input_peak > 0.0 and len(signal_full) > len(signal_input):
        thresh = max(0.15 * input_peak, 0.05)
        release_idx = len(signal_input) - 1
        for k in range(release_idx + 1, len(signal_full) - 3):
            if all(signal_full[m] <= thresh for m in range(k, min(len(signal_full), k + 4))):
                settle_time = round((k - release_idx) * 0.1, 3)
                break

    score = 0.25
    if hold >= 0.5:
        score += 0.15
    if hold >= 0.9:
        score += 0.10
    if ratio >= 1.6:
        score += 0.18
    elif ratio >= 1.35:
        score += 0.10
    if pre_neutral >= 0.4:
        score += 0.12
    if post_neutral >= 0.5:
        score += 0.12
    if input_peak > 0.5:
        score += 0.08

    if classification == "mixed-input window":
        score -= 0.18
    if hold < 0.35:
        notes.append("very short hold")
        score -= 0.10
    if pre_neutral < 0.2:
        notes.append("no clean pre-input hover")
    if post_neutral < 0.3:
        notes.append("no clean post-release settle")

    if score < 0.45:
        confidence = "low"
    elif score < 0.72:
        confidence = "medium"
    else:
        confidence = "high"

    if classification != "mixed-input window" and score < 0.42:
        classification = "discard/noisy window"

    return Candidate(
        classification=classification,
        row_start=start,
        row_end=end,
        t_start_s=round(window[0]["t_s"], 3),
        t_end_s=round(window[-1]["t_s"], 3),
        hold_duration_s=round(hold, 3),
        dominant_axis=dom_axis,
        dominant_sign=dom_sign,
        peak_stick_pct=round(abs_peak[dom_axis], 1),
        dominant_ratio=round(ratio, 3),
        pre_neutral_s=round(pre_neutral, 3),
        post_neutral_s=round(post_neutral, 3),
        input_peak_rate=round(input_peak, 3),
        full_run_peak_rate=round(full_peak, 3),
        carryover_after_release=round(carryover, 3),
        settle_time_s=settle_time,
        confidence=confidence,
        cleanliness_score=round(max(0.0, min(score, 1.0)), 3),
        notes=notes,
    )


def analyze(path: str, top_n: int) -> Dict[str, object]:
    rows = load_rows(path)
    if not rows:
        raise RuntimeError("No rows loaded")

    dt = [rows[i + 1]["t_ms"] - rows[i]["t_ms"] for i in range(len(rows) - 1)]
    dt_nonzero = [d for d in dt if d > 0]
    active_mask = [max(abs(r[a]) for a in AXES) >= 22.0 and r["mode"] == "P-GPS" for r in rows]
    active_rows = sum(1 for v in active_mask if v)

    windows = detect_windows(rows)
    candidates = [classify_window(rows, s, e) for s, e in windows]

    by_class: Dict[str, List[Candidate]] = {}
    for c in candidates:
        by_class.setdefault(c.classification, []).append(c)

    selected = {}
    for k, vals in by_class.items():
        ranked = sorted(vals, key=lambda x: (x.cleanliness_score, x.hold_duration_s), reverse=True)
        selected[k] = [asdict(v) for v in ranked[:top_n]]

    useful = [c for c in candidates if c.classification not in ("discard/noisy window", "mixed-input window") and c.confidence in ("high", "medium")]

    return {
        "source_csv": path,
        "row_count": len(rows),
        "columns_used": [
            "time(millisecond)",
            "flycState",
            " xSpeed(mph)",
            " ySpeed(mph)",
            " zSpeed(mph)",
            " compass_heading(degrees)",
            " pitch(degrees)",
            " roll(degrees)",
            "rc_elevator(percent)",
            "rc_aileron(percent)",
            "rc_throttle(percent)",
            "rc_rudder(percent)",
        ],
        "timing": {
            "dt_ms_mode": statistics.mode(dt_nonzero) if dt_nonzero else None,
            "dt_ms_min": min(dt_nonzero) if dt_nonzero else None,
            "dt_ms_max": max(dt_nonzero) if dt_nonzero else None,
            "dt_ms_mean": round(statistics.fmean(dt), 3) if dt else None,
            "zero_dt_count": sum(1 for d in dt if d == 0),
        },
        "mode_counts": {m: sum(1 for r in rows if r["mode"] == m) for m in sorted(set(r["mode"] for r in rows))},
        "activity": {
            "active_rows": active_rows,
            "active_fraction": round(active_rows / len(rows), 3),
            "passive_rows": len(rows) - active_rows,
            "passive_fraction": round((len(rows) - active_rows) / len(rows), 3),
        },
        "candidate_window_count": len(candidates),
        "usable_window_count": len(useful),
        "counts_by_classification": {k: len(v) for k, v in by_class.items()},
        "top_candidates_by_classification": selected,
        "all_candidates": [asdict(c) for c in candidates],
    }


def render_markdown(report: Dict[str, object]) -> str:
    lines = []
    lines.append("# Exploratory AirData Maneuver Mining Report")
    lines.append("")
    lines.append(f"Source: `{report['source_csv']}`")
    lines.append("")
    lines.append("## Data quality snapshot")
    timing = report["timing"]
    activity = report["activity"]
    lines.append(f"- Rows: {report['row_count']}")
    lines.append(f"- Timing: mode {timing['dt_ms_mode']} ms, min/max {timing['dt_ms_min']}/{timing['dt_ms_max']} ms, zero-dt rows {timing['zero_dt_count']}")
    lines.append(f"- Active maneuvering fraction: {activity['active_fraction']:.3f}")
    lines.append(f"- Passive/neutral fraction: {activity['passive_fraction']:.3f}")
    lines.append(f"- Candidate windows detected: {report['candidate_window_count']}, usable windows: {report['usable_window_count']}")
    lines.append("")
    lines.append("## Candidate counts by class")
    for cls, count in sorted(report["counts_by_classification"].items()):
        lines.append(f"- {cls}: {count}")
    lines.append("")
    lines.append("## Best candidates by class (top ranked)")
    lines.append("")
    lines.append("| Class | Rows | Time (s) | Hold (s) | Peak stick % | Input peak | Full peak | Carryover | Settle (s) | Confidence |")
    lines.append("|---|---:|---:|---:|---:|---:|---:|---:|---:|---|")
    for cls, entries in sorted(report["top_candidates_by_classification"].items()):
        for e in entries[:10]:
            lines.append(
                f"| {cls} | {e['row_start']}-{e['row_end']} | {e['t_start_s']:.1f}-{e['t_end_s']:.1f} | {e['hold_duration_s']:.2f} | {e['peak_stick_pct']:.1f} | {e['input_peak_rate']:.3f} | {e['full_run_peak_rate']:.3f} | {e['carryover_after_release']:.3f} | {e['settle_time_s'] if e['settle_time_s'] is not None else 'n/a'} | {e['confidence']} |"
            )
    lines.append("")
    lines.append("## Interpretation guardrails")
    lines.append("- These windows are exploratory and should not be used as acceptance-grade protocol evidence.")
    lines.append("- Mixed/discard windows indicate overlapping sticks and/or insufficient settle structure.")
    return "\n".join(lines) + "\n"


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("csv")
    ap.add_argument("--json-out", required=True)
    ap.add_argument("--md-out", required=True)
    ap.add_argument("--top-n", type=int, default=10)
    args = ap.parse_args()

    report = analyze(args.csv, args.top_n)
    with open(args.json_out, "w", encoding="utf-8") as fp:
        json.dump(report, fp, indent=2)
    with open(args.md_out, "w", encoding="utf-8") as fp:
        fp.write(render_markdown(report))
    print(json.dumps({"json_out": args.json_out, "md_out": args.md_out, "candidates": report["candidate_window_count"], "usable": report["usable_window_count"]}, indent=2))


if __name__ == "__main__":
    main()
