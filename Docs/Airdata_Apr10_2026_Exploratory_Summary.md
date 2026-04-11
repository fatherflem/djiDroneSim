# Airdata + Simulator Benchmark Comparison

Source CSV (used directly):
- `Apr-10th-2026-02-12PM-Flight-Airdata.csv`
- Sim CSV patterns: `BenchmarkRuns/session_20260410_135709.zip, BenchmarkRuns/session_20260410_135709/**/*.csv`

## Simulator session selection

- Primary protocol runs included: 8
- Runs excluded from primary protocol comparison: 2

| Included run # | Category | Protocol order | Run source | File |
|---:|---|---:|---|---|
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_001_hover_hold_hover_hold_Normal_20260410_135721_run001.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_002_forward_step_forward_step_Normal_20260410_135725_run002.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_003_lateral_right_lateral_right_Normal_20260410_135731_run003.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_004_lateral_left_lateral_left_Normal_20260410_135737_run004.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_005_climb_climb_Normal_20260410_135741_run005.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_006_descent_descent_Normal_20260410_135745_run006.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_007_yaw_right_yaw_right_Normal_20260410_135749_run007.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_008_yaw_left_yaw_left_Normal_20260410_135753_run008.csv` |

| Excluded run # | Category | Reason | File |
|---:|---|---|---|
| 9 | climb_long | outside_core_comparison_categories | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_009_climb_long_climb_long_Normal_20260410_135758_run009.csv` |
| 10 | descent_long | outside_core_comparison_categories | `/workspace/djiDroneSim/BenchmarkRuns/session_20260410_135709.zip:run_010_descent_long_descent_long_Normal_20260410_135804_run010.csv` |


## Segmentation confidence overview (real flight)

| Maneuver | Count | High | Medium | Low | Peak mean | Delay mean |
|---|---:|---:|---:|---:|---:|---:|
| backward_step | 17 | 2 | 13 | 2 | 3.72 | 0.68 |
| climb | 8 | 4 | 4 | 0 | 4.01 | 0.19 |
| descent | 15 | 12 | 3 | 0 | 3.34 | 0.17 |
| forward_step | 41 | 26 | 15 | 0 | 3.77 | 0.33 |
| lateral_left | 14 | 7 | 7 | 0 | 3.56 | 0.51 |
| lateral_right | 11 | 8 | 3 | 0 | 4.93 | 0.54 |
| yaw_left | 26 | 18 | 8 | 0 | 78.85 | 0.27 |
| yaw_right | 18 | 15 | 3 | 0 | 70.22 | 0.26 |

## Hover hold

- Runs: 44
- Horizontal RMS mean: 1.200 m/s
- Vertical RMS mean: 0.332 m/s

## Measured vs inferred classification (real baseline)

| Target | Classification | Evidence |
|---|---|---|
| forward speed behavior | directly_measured | computed from segment aggregates |
| lateral right behavior | directly_measured | computed from segment aggregates |
| lateral left behavior | directly_measured | computed from segment aggregates |
| climb behavior | directly_measured | computed from segment aggregates |
| descent behavior | directly_measured | computed from segment aggregates |
| yaw right behavior | directly_measured | computed from segment aggregates |
| yaw left behavior | directly_measured | computed from segment aggregates |
| acceleration | directly_measured | computed from segment aggregates |
| braking | directly_measured | computed from segment aggregates |
| overshoot | directly_measured | computed from segment aggregates |
| settle time | directly_measured | computed from segment aggregates |

## Sim vs real deltas

| Category | Status | Sim input confidence | Sim input provenance | Delay Δ | Peak Δ | Accel Δ | Settle Δ | Overshoot Δ | Verdict |
|---|---|---|---|---:|---:|---:|---:|---:|---|
| hover_hold | compared | high | designer_assumption | - | - | - | - | - | insufficient_data |
| forward_step | compared | medium | estimated_from_limited_segments | -0.027 | -1.554 | -0.105 | 0.0 | -0.205 | too_sluggish_provisional_input_amplitude |
| lateral_right | compared | high | directly_measured | 0.024 | 3.996 | 0.686 | 0.0 | -0.567 | too_aggressive |
| lateral_left | compared | low | estimated_from_limited_segments | 0.093 | 6.257 | 0.976 | 0.0 | -0.344 | too_aggressive_provisional_input_amplitude |
| climb | compared | medium | estimated_from_limited_segments | 0.132 | -1.455 | -1.352 | 0.0 | -0.297 | too_sluggish_provisional_input_amplitude |
| descent | compared | medium | estimated_from_limited_segments | 0.127 | -1.04 | 0.094 | 0.0 | -0.673 | too_sluggish_provisional_input_amplitude |
| yaw_right | compared | high | directly_measured | -0.196 | 9.671 | 66.311 | 0.0 | -5.502 | too_aggressive |
| yaw_left | compared | high | directly_measured | -0.209 | 1.046 | 32.104 | 0.0 | -7.885 | too_aggressive |

## Recommended default protocol stick amplitudes (from Airdata RC)

| Maneuver | RC channel | Recommended % | Normalized | Classification | Consistency |
|---|---|---:|---:|---|---|
| forward_step | rc_elevator | 98.5 | 0.985 | estimated_from_noisy_or_limited_segments | low |
| lateral_right | rc_aileron | 100.0 | 1.000 | directly_measured_from_clean_rc_plateaus | high |
| lateral_left | rc_aileron | 100.0 | -1.000 | estimated_from_noisy_or_limited_segments | moderate |
| climb | rc_throttle | 76.5 | 0.765 | estimated_from_noisy_or_limited_segments | low |
| descent | rc_throttle | 100.0 | -1.000 | directly_measured_from_clean_rc_plateaus | high |
| yaw_right | rc_rudder | 100.0 | 1.000 | directly_measured_from_clean_rc_plateaus | moderate |
| yaw_left | rc_rudder | 100.0 | -1.000 | directly_measured_from_clean_rc_plateaus | high |

## Strength of comparison categories

- **Strong categories** (directly measured default simulator amplitude): lateral_right, descent, yaw_right, yaw_left
- **Provisional categories** (estimated or assumed default simulator amplitude): forward_step, lateral_left, climb

## Confidence policy

- **directly_measured**: at least 2 high-confidence segments for that target maneuver.
- **estimated_from_limited_segments**: only medium-confidence or single high-confidence support.
- **designer_assumption**: no reliable segments in this log.
- If simulator input amplitude confidence/provenance is provisional, treat mismatch verdicts as directional guidance (not final tuning proof).
