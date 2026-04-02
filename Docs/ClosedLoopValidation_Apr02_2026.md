# Closed-Loop Validation (Real vs Baseline vs Prior Rerun vs Newest Rerun)

- Real benchmark CSV: `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Baseline simulator session: `BenchmarkRuns/session_20260402_133209.zip` (zip)
- Prior post-tuning simulator session: `BenchmarkRuns/session_20260402_151147.zip` (zip)
- Newest simulator rerun (climb-coverage closeout): `BenchmarkRuns/session_20260402_161237.zip` (zip)
- Canonical drop location for benchmark sessions: `BenchmarkRuns/`.
- Workflow note: drop session zip files directly into `BenchmarkRuns/`; no manual per-session folder setup is required.
- Missing run(s) in newest session (vs baseline expected runs): (none)
- Missing category label(s) in newest session: (none)

## Session coverage

- Baseline manifest run count: 9
- Prior rerun manifest run count: 16
- Newest rerun manifest run count: 8
- Baseline primary protocol run count: 8
- Prior rerun primary protocol run count: 16
- Newest rerun primary protocol run count: 8
- Baseline excluded runs: 1
- Prior rerun excluded runs: 0
- Newest rerun excluded runs: 0
- Climb counterpart run present in newest rerun: True

## Category comparison (baseline vs prior vs newest vs real)

| Category | Availability | Metric | Real | Baseline | Prior | Newest | Baseline Δ | Prior Δ | Newest Δ | Trend (prior→newest) |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| hover_hold | present_in_all | response_delay_s | - | 0.0 | 0.0 | 0.0 | - | - | - | n/a |
|  |  | peak_rate | - | 0.0 | 0.0 | 0.0 | - | - | - | n/a |
|  |  | max_accel | - | 0.0 | 0.0 | 0.0 | - | - | - | n/a |
|  |  | settle_time_s | - | 0.0 | 0.0 | 0.0 | - | - | - | n/a |
|  |  | overshoot | - | 0.0 | 0.0 | 0.0 | - | - | - | n/a |
|  |  | residual_drift | - | 0.0 | 0.0 | 0.0 | - | - | - | n/a |
| forward_step | present_in_all | response_delay_s | 0.0 | 0.08 | 0.08 | 0.08 | 0.08 | 0.08 | 0.08 | unchanged |
|  |  | peak_rate | 1.067 | 3.954 | 3.478 | 3.478 | 2.887 | 2.411 | 2.411 | unchanged |
|  |  | max_accel | 0.667 | 8.0 | 8.0 | 8.0 | 7.333 | 7.333 | 7.333 | unchanged |
|  |  | settle_time_s | 0.55 | 1.02 | 1.06 | 1.06 | 0.47 | 0.51 | 0.51 | unchanged |
|  |  | overshoot | 0.142 | 3.184 | 2.869 | 2.869 | 3.042 | 2.727 | 2.727 | unchanged |
|  |  | residual_drift | 0.913 | 0.001 | 0.0 | 0.0 | -0.912 | -0.913 | -0.913 | unchanged |
| lateral_right | present_in_all | response_delay_s | 0.0 | 0.08 | 0.08 | 0.08 | 0.08 | 0.08 | 0.08 | unchanged |
|  |  | peak_rate | 0.2 | 4.11 | 2.291 | 2.291 | 3.91 | 2.091 | 2.091 | unchanged |
|  |  | max_accel | 0.333 | 8.0 | 5.786 | 5.786 | 7.667 | 5.453 | 5.453 | unchanged |
|  |  | settle_time_s | 1.1 | 0.98 | 1.18 | 1.18 | -0.12 | 0.08 | 0.08 | unchanged |
|  |  | overshoot | 0.2 | 3.268 | 1.997 | 1.997 | 3.068 | 1.797 | 1.797 | unchanged |
|  |  | residual_drift | 0.0 | 0.002 | 0.0 | 0.0 | 0.002 | 0.0 | 0.0 | unchanged |
| lateral_left | present_in_all | response_delay_s | - | 0.08 | 0.06 | 0.06 | - | - | - | n/a |
|  |  | peak_rate | - | 4.11 | 2.652 | 2.652 | - | - | - | n/a |
|  |  | max_accel | - | 8.0 | 7.84 | 7.84 | - | - | - | n/a |
|  |  | settle_time_s | - | 0.98 | 1.14 | 1.14 | - | - | - | n/a |
|  |  | overshoot | - | 3.268 | 2.265 | 2.265 | - | - | - | n/a |
|  |  | residual_drift | - | 0.002 | 0.0 | 0.0 | - | - | - | n/a |
| climb | present_in_all | response_delay_s | 0.25 | 0.06 | 0.06 | 0.06 | -0.19 | -0.19 | -0.19 | unchanged |
|  |  | peak_rate | 3.625 | 2.386 | 2.488 | 2.488 | -1.239 | -1.137 | -1.137 | unchanged |
|  |  | max_accel | 5.75 | 10.0 | 10.0 | 10.0 | 4.25 | 4.25 | 4.25 | unchanged |
|  |  | settle_time_s | 0.0 | 1.06 | 1.08 | 1.08 | 1.06 | 1.08 | 1.08 | unchanged |
|  |  | overshoot | 1.04 | 2.022 | 2.117 | 2.117 | 0.982 | 1.077 | 1.077 | unchanged |
|  |  | residual_drift | 1.847 | 0.002 | 0.002 | 0.002 | -1.845 | -1.845 | -1.845 | unchanged |
| descent | present_in_all | response_delay_s | 0.167 | 0.06 | 0.06 | 0.06 | -0.107 | -0.107 | -0.107 | unchanged |
|  |  | peak_rate | 4.067 | 2.981 | 3.083 | 3.083 | -1.086 | -0.984 | -0.984 | unchanged |
|  |  | max_accel | 4.0 | 10.0 | 10.0 | 10.0 | 6.0 | 6.0 | 6.0 | unchanged |
|  |  | settle_time_s | 0.0 | 1.06 | 1.06 | 1.06 | 1.06 | 1.06 | 1.06 | unchanged |
|  |  | overshoot | 0.687 | 2.501 | 2.591 | 2.591 | 1.814 | 1.904 | 1.904 | unchanged |
|  |  | residual_drift | 3.233 | 0.003 | 0.002 | 0.002 | -3.23 | -3.231 | -3.231 | unchanged |
| yaw_right | present_in_all | response_delay_s | 0.24 | 0.04 | 0.04 | 0.04 | -0.2 | -0.2 | -0.2 | unchanged |
|  |  | peak_rate | 84.6 | 81.998 | 73.998 | 84.358 | -2.602 | -10.602 | -0.242 | improvement |
|  |  | max_accel | 312.0 | 743.203 | 670.696 | 764.593 | 431.203 | 358.696 | 452.593 | regression |
|  |  | settle_time_s | 0.0 | 1.28 | 1.32 | 1.36 | 1.28 | 1.32 | 1.36 | regression |
|  |  | overshoot | 4.6 | 74.967 | 68.771 | 79.107 | 70.367 | 64.171 | 74.507 | regression |
|  |  | residual_drift | 79.225 | 0.0 | 0.0 | 0.0 | -79.225 | -79.225 | -79.225 | unchanged |
| yaw_left | present_in_all | response_delay_s | 0.25 | 0.04 | 0.04 | 0.04 | -0.21 | -0.21 | -0.21 | unchanged |
|  |  | peak_rate | 70.75 | 81.999 | 66.599 | 66.599 | 11.249 | -4.151 | -4.151 | unchanged |
|  |  | max_accel | 177.5 | 743.179 | 603.638 | 603.638 | 565.679 | 426.138 | 426.138 | unchanged |
|  |  | settle_time_s | 0.0 | 1.28 | 1.32 | 1.32 | 1.28 | 1.32 | 1.32 | unchanged |
|  |  | overshoot | 6.9 | 74.968 | 61.895 | 61.895 | 68.068 | 54.995 | 54.995 | unchanged |
|  |  | residual_drift | 63.312 | 0.0 | 0.0 | 0.0 | -63.312 | -63.312 | -63.312 | unchanged |

## Strong-category assessment

- lateral_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=4/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=1/5; response_shape=not_improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_left: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=3/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.

## Provisional-category notes

- forward_step: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- lateral_left: no_clear_directional_gain; moved_correct_direction=False; real_segmentation_confidence=unknown; sim_amplitude_confidence=unknown; sim_amplitude_provenance=unknown.
- climb: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- descent: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.

## Decision summary

- Climb coverage complete: True
- Strongest high-confidence divergence: yaw_right
- Lateral right acceptable: False
- Yaw left acceptable: False
- Normal-mode fidelity good enough to move on: False
- Single next tuning target: yaw_right

## Historical rerun reference (session_20260402_140700)

- Explicit session roles:
  - Baseline: `BenchmarkRuns/session_20260402_133209.zip`
  - Prior post-tuning rerun: `BenchmarkRuns/session_20260402_140700.zip`
  - Previous coverage-closing rerun: `BenchmarkRuns/session_20260402_151147.zip`
  - Newest rerun under validation in this pass: `BenchmarkRuns/session_20260402_161237.zip`
- Coverage check against the full intended protocol (`hover_hold`, `forward_step`, `lateral_right`, `lateral_left`, `climb`, `descent`, `yaw_right`, `yaw_left`): complete in `session_20260402_161237.zip` with 8/8 categories present.
- Trend note: `session_20260402_161237.zip` is effectively unchanged from `session_20260402_151147.zip` for all core categories except `yaw_right` (peak rate moved closer to real; max accel, settle time, and overshoot regressed).
- Decision quality update:
  - `climb` is now fully covered and no longer missing.
  - `yaw_right` remains the strongest high-confidence divergence.
  - `lateral_right` is improved but still not acceptable.
  - `yaw_left` is improved but still not acceptable.
  - `forward_step`, `climb`, and `descent` remain directional/provisional (no additional confidence upgrade in this rerun).
- Single highest-priority next tuning target: **`yaw_right`**.
