# Airdata + Simulator Benchmark Comparison

Source CSV (used directly):
- `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Sim CSV patterns: `BenchmarkRuns/**/*.csv, BenchmarkRuns/*.zip`

## Simulator session selection

- Primary protocol runs included: 56
- Runs excluded from primary protocol comparison: 9

| Included run # | Category | Protocol order | Run source | File |
|---:|---|---:|---|---|
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_001_hover_hold_hover_hold_Normal_20260402_140715_run001.csv` |
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_001_hover_hold_hover_hold_Normal_20260402_151212_run001.csv` |
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_001_hover_hold_hover_hold_Normal_20260402_161254_run001.csv` |
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_001_hover_hold_hover_hold_Normal_20260402_163600_run001.csv` |
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_001_hover_hold_hover_hold_Normal_20260402_170100_run001.csv` |
| 1 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_001_hover_hold_hover_hold_Normal_20260402_171311_run001.csv` |
| 2 | hover_hold | 1 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_002_hover_hold_hover_hold_Normal_20260402_133237_run002.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_002_forward_step_forward_step_Normal_20260402_140719_run002.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_002_forward_step_forward_step_Normal_20260402_151216_run002.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_002_forward_step_forward_step_Normal_20260402_161258_run002.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_002_forward_step_forward_step_Normal_20260402_163604_run002.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_002_forward_step_forward_step_Normal_20260402_170104_run002.csv` |
| 2 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_002_forward_step_forward_step_Normal_20260402_171315_run002.csv` |
| 3 | forward_step | 2 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_003_forward_step_forward_step_Normal_20260402_133241_run003.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_003_lateral_right_lateral_right_Normal_20260402_140723_run003.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_003_lateral_right_lateral_right_Normal_20260402_151220_run003.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_003_lateral_right_lateral_right_Normal_20260402_161302_run003.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_003_lateral_right_lateral_right_Normal_20260402_163608_run003.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_003_lateral_right_lateral_right_Normal_20260402_170108_run003.csv` |
| 3 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_003_lateral_right_lateral_right_Normal_20260402_171319_run003.csv` |
| 4 | lateral_right | 3 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_004_lateral_right_lateral_right_Normal_20260402_133245_run004.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_004_lateral_left_lateral_left_Normal_20260402_140728_run004.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_004_lateral_left_lateral_left_Normal_20260402_151224_run004.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_004_lateral_left_lateral_left_Normal_20260402_161306_run004.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_004_lateral_left_lateral_left_Normal_20260402_163612_run004.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_004_lateral_left_lateral_left_Normal_20260402_170112_run004.csv` |
| 4 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_004_lateral_left_lateral_left_Normal_20260402_171323_run004.csv` |
| 5 | lateral_left | 4 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_005_lateral_left_lateral_left_Normal_20260402_133249_run005.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_005_climb_climb_Normal_20260402_140732_run005.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_005_climb_climb_Normal_20260402_151228_run005.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_005_climb_climb_Normal_20260402_161310_run005.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_005_climb_climb_Normal_20260402_163616_run005.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_005_climb_climb_Normal_20260402_170116_run005.csv` |
| 5 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_005_climb_climb_Normal_20260402_171327_run005.csv` |
| 6 | climb | 5 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_006_climb_climb_Normal_20260402_133253_run006.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_006_descent_descent_Normal_20260402_140736_run006.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_006_descent_descent_Normal_20260402_151232_run006.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_006_descent_descent_Normal_20260402_161314_run006.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_006_descent_descent_Normal_20260402_163620_run006.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_006_descent_descent_Normal_20260402_170121_run006.csv` |
| 6 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_006_descent_descent_Normal_20260402_171331_run006.csv` |
| 7 | descent | 6 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_007_descent_descent_Normal_20260402_133257_run007.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_007_yaw_right_yaw_right_Normal_20260402_140740_run007.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_007_yaw_right_yaw_right_Normal_20260402_151237_run007.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_007_yaw_right_yaw_right_Normal_20260402_161318_run007.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_007_yaw_right_yaw_right_Normal_20260402_163625_run007.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_007_yaw_right_yaw_right_Normal_20260402_170125_run007.csv` |
| 7 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_007_yaw_right_yaw_right_Normal_20260402_171335_run007.csv` |
| 8 | yaw_right | 7 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_008_yaw_right_yaw_right_Normal_20260402_133301_run008.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_140700.zip:run_008_yaw_left_yaw_left_Normal_20260402_140744_run008.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_008_yaw_left_yaw_left_Normal_20260402_151241_run008.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_161237.zip:run_008_yaw_left_yaw_left_Normal_20260402_161323_run008.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_163547.zip:run_008_yaw_left_yaw_left_Normal_20260402_163629_run008.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_170046.zip:run_008_yaw_left_yaw_left_Normal_20260402_170129_run008.csv` |
| 8 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_171257.zip:run_008_yaw_left_yaw_left_Normal_20260402_171339_run008.csv` |
| 9 | yaw_left | 8 | full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_009_yaw_left_yaw_left_Normal_20260402_133305_run009.csv` |

| Excluded run # | Category | Reason | File |
|---:|---|---|---|
| 1 | climb | run_source_manual_not_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_133209.zip:run_001_climb_climb_Normal_20260402_133222_run001.csv` |
| 9 | hover_hold | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_009_hover_hold_hover_hold_Normal_20260402_151258_run009.csv` |
| 10 | forward_step | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_010_forward_step_forward_step_Normal_20260402_151302_run010.csv` |
| 11 | lateral_right | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_011_lateral_right_lateral_right_Normal_20260402_151306_run011.csv` |
| 12 | lateral_left | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_012_lateral_left_lateral_left_Normal_20260402_151310_run012.csv` |
| 13 | climb | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_013_climb_climb_Normal_20260402_151314_run013.csv` |
| 14 | descent | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_014_descent_descent_Normal_20260402_151318_run014.csv` |
| 15 | yaw_right | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_015_yaw_right_yaw_right_Normal_20260402_151323_run015.csv` |
| 16 | yaw_left | duplicate_category_in_full_protocol | `/workspace/djiDroneSim/BenchmarkRuns/session_20260402_151147.zip:run_016_yaw_left_yaw_left_Normal_20260402_151327_run016.csv` |


## Segmentation confidence overview (real flight)

| Maneuver | Count | High | Medium | Low | Peak mean | Delay mean |
|---|---:|---:|---:|---:|---:|---:|
| climb | 4 | 1 | 3 | 0 | 3.62 | 0.25 |
| descent | 3 | 2 | 1 | 0 | 4.07 | 0.17 |
| forward_step | 12 | 8 | 4 | 0 | 1.07 | 0.00 |
| lateral_right | 3 | 3 | 0 | 0 | 0.20 | 0.00 |
| yaw_left | 4 | 3 | 1 | 0 | 70.75 | 0.25 |
| yaw_right | 5 | 5 | 0 | 0 | 84.60 | 0.24 |

## Hover hold

- Runs: 15
- Horizontal RMS mean: 0.412 m/s
- Vertical RMS mean: 0.312 m/s

## Measured vs inferred classification (real baseline)

| Target | Classification | Evidence |
|---|---|---|
| forward speed behavior | directly_measured | computed from segment aggregates |
| lateral right behavior | directly_measured | computed from segment aggregates |
| lateral left behavior | designer_assumption | no clean lateral_left segments |
| climb behavior | estimated_from_limited_segments | computed from segment aggregates |
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
| forward_step | compared | medium | estimated_from_limited_segments | 0.074 | 2.275 | 7.268 | 0.519 | 2.62 | too_aggressive_provisional_input_amplitude |
| lateral_right | compared | high | directly_measured | 0.089 | 1.85 | 4.36 | 0.08 | 1.549 | too_aggressive |
| lateral_left | no_real_data | - | - | - | - | - | - | - | insufficient data |
| climb | compared | medium | estimated_from_limited_segments | -0.19 | -0.805 | 4.25 | 1.026 | 1.296 | too_sluggish_provisional_input_amplitude |
| descent | compared | medium | estimated_from_limited_segments | -0.101 | -0.685 | 6.0 | 1.009 | 2.087 | too_sluggish_provisional_input_amplitude |
| yaw_right | compared | high | directly_measured | -0.197 | -3.673 | 316.754 | 1.354 | 71.289 | too_sluggish |
| yaw_left | compared | high | directly_measured | -0.207 | -2.055 | 376.699 | 1.343 | 57.229 | too_sluggish |

## Recommended default protocol stick amplitudes (from Airdata RC)

| Maneuver | RC channel | Recommended % | Normalized | Classification | Consistency |
|---|---|---:|---:|---|---|
| forward_step | rc_elevator | 77.0 | 0.770 | estimated_from_noisy_or_limited_segments | low |
| lateral_right | rc_aileron | 100.0 | 1.000 | directly_measured_from_clean_rc_plateaus | high |
| lateral_left | rc_aileron | 100.0 | -1.000 | estimated_from_noisy_or_limited_segments | inferred |
| climb | rc_throttle | 100.0 | 1.000 | estimated_from_noisy_or_limited_segments | high |
| descent | rc_throttle | 100.0 | -1.000 | estimated_from_noisy_or_limited_segments | high |
| yaw_right | rc_rudder | 100.0 | 1.000 | directly_measured_from_clean_rc_plateaus | high |
| yaw_left | rc_rudder | 100.0 | -1.000 | directly_measured_from_clean_rc_plateaus | moderate |

## Strength of comparison categories

- **Strong categories** (directly measured default simulator amplitude): lateral_right, yaw_right, yaw_left
- **Provisional categories** (estimated or assumed default simulator amplitude): forward_step, lateral_left, climb, descent

## Confidence policy

- **directly_measured**: at least 2 high-confidence segments for that target maneuver.
- **estimated_from_limited_segments**: only medium-confidence or single high-confidence support.
- **designer_assumption**: no reliable segments in this log.
- If simulator input amplitude confidence/provenance is provisional, treat mismatch verdicts as directional guidance (not final tuning proof).
