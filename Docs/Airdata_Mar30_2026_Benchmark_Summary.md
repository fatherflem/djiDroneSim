# Airdata + Simulator Benchmark Comparison

Source CSV (used directly):
- `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Sim CSV patterns: `(none)`

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
| hover_hold | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |
| forward_step | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |
| lateral_right | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |
| lateral_left | no_real_data | - | - | - | - | - | - | - | insufficient data |
| climb | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |
| descent | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |
| yaw_right | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |
| yaw_left | real_only | - | - | - | - | - | - | - | no simulator benchmark CSVs supplied for this category |

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
