# Airdata + Simulator Benchmark Comparison

Source CSV (used directly):
- `Apr-8th-2026-08-15AM-Flight-Airdata.csv`
- Sim CSV patterns: `BenchmarkRuns/session_20260408_130703.zip, BenchmarkRuns/session_20260408_130703/**/*.csv`

## Simulator session selection

- Primary protocol runs included: 8
- Runs excluded from primary protocol comparison: 0

| Included run # | Category | Protocol order | Run source | File |
|---:|---|---:|---|---|
| 1 | hover_hold | 1 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_001_hover_hold_hover_hold_Normal_20260408_130717_run001.csv` |
| 2 | forward_step | 2 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_002_forward_step_forward_step_Normal_20260408_130721_run002.csv` |
| 3 | lateral_right | 3 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_003_lateral_right_lateral_right_Normal_20260408_130725_run003.csv` |
| 4 | lateral_left | 4 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_004_lateral_left_lateral_left_Normal_20260408_130931_run004.csv` |
| 5 | climb | 5 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_005_climb_climb_Normal_20260408_130935_run005.csv` |
| 6 | descent | 6 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_006_descent_descent_Normal_20260408_130939_run006.csv` |
| 7 | yaw_right | 7 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_007_yaw_right_yaw_right_Normal_20260408_130943_run007.csv` |
| 8 | yaw_left | 8 | full_protocol | `/home/user/djiDroneSim/BenchmarkRuns/session_20260408_130703.zip:run_008_yaw_left_yaw_left_Normal_20260408_130947_run008.csv` |


## Segmentation confidence overview (real flight)

| Maneuver | Count | High | Medium | Low | Peak mean | Delay mean |
|---|---:|---:|---:|---:|---:|---:|
| backward_step | 3 | 2 | 1 | 0 | 6.38 | 0.47 |
| climb | 4 | 4 | 0 | 0 | 4.33 | 0.30 |
| descent | 4 | 3 | 1 | 0 | 3.67 | 0.28 |
| forward_step | 3 | 2 | 1 | 0 | 2.63 | 0.30 |
| lateral_left | 2 | 2 | 0 | 0 | 10.04 | 0.65 |
| lateral_right | 2 | 2 | 0 | 0 | 7.44 | 0.45 |
| yaw_left | 2 | 2 | 0 | 0 | 82.00 | 0.30 |
| yaw_right | 2 | 2 | 0 | 0 | 82.00 | 0.30 |

## Hover hold

- Runs: 19
- Horizontal RMS mean: 1.003 m/s
- Vertical RMS mean: 0.356 m/s

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
| forward_step | compared | medium | estimated_from_limited_segments | -0.24 | -0.081 | 4.574 | 0.987 | 1.747 | matches_well_provisional_input_amplitude |
| lateral_right | compared | high | directly_measured | -0.33 | -7.323 | -4.535 | 1.28 | -0.066 | too_sluggish |
| lateral_left | compared | low | estimated_from_limited_segments | -0.59 | -7.39 | 3.367 | 1.14 | 2.05 | too_sluggish_provisional_input_amplitude |
| climb | compared | medium | estimated_from_limited_segments | -0.24 | -0.624 | 5.25 | 0.475 | 1.645 | too_sluggish_provisional_input_amplitude |
| descent | compared | medium | estimated_from_limited_segments | -0.195 | 0.506 | 5.75 | 0.88 | 1.962 | too_aggressive_provisional_input_amplitude |
| yaw_right | compared | high | directly_measured | -0.24 | -1.589 | 70.3 | 0.0 | -1.495 | too_sluggish |
| yaw_left | compared | high | directly_measured | -0.24 | -8.9 | -1.549 | 0.0 | -2.614 | too_sluggish |

## Recommended default protocol stick amplitudes (from Airdata RC)

| Maneuver | RC channel | Recommended % | Normalized | Classification | Consistency |
|---|---|---:|---:|---|---|
| forward_step | rc_elevator | 100.0 | 1.000 | estimated_from_noisy_or_limited_segments | high |
| lateral_right | rc_aileron | 100.0 | 1.000 | estimated_from_noisy_or_limited_segments | high |
| lateral_left | rc_aileron | 100.0 | -1.000 | estimated_from_noisy_or_limited_segments | high |
| climb | rc_throttle | 100.0 | 1.000 | directly_measured_from_clean_rc_plateaus | high |
| descent | rc_throttle | 100.0 | -1.000 | directly_measured_from_clean_rc_plateaus | moderate |
| yaw_right | rc_rudder | 100.0 | 1.000 | estimated_from_noisy_or_limited_segments | high |
| yaw_left | rc_rudder | 100.0 | -1.000 | estimated_from_noisy_or_limited_segments | high |

## Strength of comparison categories

- **Strong categories** (directly measured default simulator amplitude): climb, descent
- **Provisional categories** (estimated or assumed default simulator amplitude): forward_step, lateral_right, lateral_left, yaw_right, yaw_left

## Confidence policy

- **directly_measured**: at least 2 high-confidence segments for that target maneuver.
- **estimated_from_limited_segments**: only medium-confidence or single high-confidence support.
- **designer_assumption**: no reliable segments in this log.
- If simulator input amplitude confidence/provenance is provisional, treat mismatch verdicts as directional guidance (not final tuning proof).
