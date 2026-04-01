# Airdata Benchmark Summary (Mar 30, 2026 08:31 UTC)

Source CSV (used directly):
- `Mar-30th-2026-08-31AM-Flight-Airdata.csv`

## Segmentation confidence overview

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

## Measured vs inferred classification

| Target | Classification | Evidence |
|---|---|---|
| forward speed behavior | directly_measured | computed from segment aggregates |
| lateral speed behavior | directly_measured | computed from segment aggregates |
| climb behavior | estimated_from_limited_segments | computed from segment aggregates |
| descent behavior | directly_measured | computed from segment aggregates |
| yaw behavior | directly_measured | computed from segment aggregates |
| acceleration | directly_measured | computed from segment aggregates |
| braking | directly_measured | computed from segment aggregates |
| overshoot | directly_measured | computed from segment aggregates |
| settle time | directly_measured | computed from segment aggregates |

## Sim vs real comparison

| Category | Status | Notes |
|---|---|---|
| forward_step | real_only | no simulator benchmark CSVs supplied to analysis |
| lateral_right | real_only | no simulator benchmark CSVs supplied to analysis |
| climb | real_only | no simulator benchmark CSVs supplied to analysis |
| descent | real_only | no simulator benchmark CSVs supplied to analysis |
| yaw_right | real_only | no simulator benchmark CSVs supplied to analysis |
| hover_hold | real_only | no simulator benchmark CSVs supplied to analysis |

## Confidence policy

- **directly_measured**: at least 2 high-confidence segments for that target maneuver.
- **estimated_from_limited_segments**: only medium-confidence or single high-confidence support.
- **designer_assumption**: no reliable segments in this log.
