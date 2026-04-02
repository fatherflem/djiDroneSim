# Closed-Loop Validation (Real vs Baseline vs Prior Rerun vs Newest Rerun)

- Real benchmark CSV: `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Baseline simulator session: `BenchmarkRuns/session_20260402_133209.zip` (zip)
- Prior post-tuning simulator session: `BenchmarkRuns/session_20260402_171257.zip` (zip)
- Newest simulator rerun (climb-coverage closeout): `BenchmarkRuns/session_20260402_175500.zip` (zip)
- Canonical drop location for benchmark sessions: `BenchmarkRuns/`.
- Workflow note: drop session zip files directly into `BenchmarkRuns/`; no manual per-session folder setup is required.
- Missing run(s) in newest session (vs baseline expected runs): (none)
- Missing category label(s) in newest session: (none)

## Session coverage

- Baseline manifest run count: 9
- Prior rerun manifest run count: 8
- Newest rerun manifest run count: 8
- Baseline primary protocol run count: 8
- Prior rerun primary protocol run count: 8
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
| forward_step | present_in_all | response_delay_s | 0.0 | 0.08 | 0.06 | 0.06 | 0.08 | 0.06 | 0.06 | unchanged |
|  |  | peak_rate | 1.067 | 3.954 | 2.552 | 2.552 | 2.887 | 1.485 | 1.485 | unchanged |
|  |  | max_accel | 0.667 | 8.0 | 7.546 | 7.546 | 7.333 | 6.879 | 6.879 | unchanged |
|  |  | settle_time_s | 0.55 | 1.02 | 1.12 | 1.12 | 0.47 | 0.57 | 0.57 | unchanged |
|  |  | overshoot | 0.142 | 3.184 | 2.18 | 2.18 | 3.042 | 2.038 | 2.038 | unchanged |
|  |  | residual_drift | 0.913 | 0.001 | 0.0 | 0.0 | -0.912 | -0.913 | -0.913 | unchanged |
| lateral_right | present_in_all | response_delay_s | 0.0 | 0.08 | 0.12 | 0.12 | 0.08 | 0.12 | 0.12 | unchanged |
|  |  | peak_rate | 0.2 | 4.11 | 0.117 | 0.117 | 3.91 | -0.083 | -0.083 | unchanged |
|  |  | max_accel | 0.333 | 8.0 | 0.141 | 0.141 | 7.667 | -0.192 | -0.192 | unchanged |
|  |  | settle_time_s | 1.1 | 0.98 | 1.28 | 1.28 | -0.12 | 0.18 | 0.18 | unchanged |
|  |  | overshoot | 0.2 | 3.268 | 0.108 | 0.108 | 3.068 | -0.092 | -0.092 | unchanged |
|  |  | residual_drift | 0.0 | 0.002 | 0.0 | 0.0 | 0.002 | 0.0 | 0.0 | unchanged |
| lateral_left | present_in_all | response_delay_s | - | 0.08 | 0.06 | 0.06 | - | - | - | n/a |
|  |  | peak_rate | - | 4.11 | 2.652 | 2.652 | - | - | - | n/a |
|  |  | max_accel | - | 8.0 | 7.84 | 7.84 | - | - | - | n/a |
|  |  | settle_time_s | - | 0.98 | 1.14 | 1.14 | - | - | - | n/a |
|  |  | overshoot | - | 3.268 | 2.265 | 2.265 | - | - | - | n/a |
|  |  | residual_drift | - | 0.002 | 0.0 | 0.0 | - | - | - | n/a |
| climb | present_in_all | response_delay_s | 0.25 | 0.06 | 0.06 | 0.06 | -0.19 | -0.19 | -0.19 | unchanged |
|  |  | peak_rate | 3.625 | 2.386 | 3.701 | 3.701 | -1.239 | 0.076 | 0.076 | unchanged |
|  |  | max_accel | 5.75 | 10.0 | 10.0 | 10.0 | 4.25 | 4.25 | 4.25 | unchanged |
|  |  | settle_time_s | 0.0 | 1.06 | 0.9 | 0.9 | 1.06 | 0.9 | 0.9 | unchanged |
|  |  | overshoot | 1.04 | 2.022 | 2.93 | 2.93 | 0.982 | 1.89 | 1.89 | unchanged |
|  |  | residual_drift | 1.847 | 0.002 | 0.024 | 0.024 | -1.845 | -1.823 | -1.823 | unchanged |
| descent | present_in_all | response_delay_s | 0.167 | 0.06 | 0.08 | 0.08 | -0.107 | -0.087 | -0.087 | unchanged |
|  |  | peak_rate | 4.067 | 2.981 | 4.181 | 4.181 | -1.086 | 0.114 | 0.114 | unchanged |
|  |  | max_accel | 4.0 | 10.0 | 10.0 | 10.0 | 6.0 | 6.0 | 6.0 | unchanged |
|  |  | settle_time_s | 0.0 | 1.06 | 0.88 | 0.88 | 1.06 | 0.88 | 0.88 | unchanged |
|  |  | overshoot | 0.687 | 2.501 | 3.277 | 3.277 | 1.814 | 2.59 | 2.59 | unchanged |
|  |  | residual_drift | 3.233 | 0.003 | 0.028 | 0.028 | -3.23 | -3.205 | -3.205 | unchanged |
| yaw_right | present_in_all | response_delay_s | 0.24 | 0.04 | 0.06 | 0.06 | -0.2 | -0.18 | -0.18 | unchanged |
|  |  | peak_rate | 84.6 | 81.998 | 83.503 | 83.503 | -2.602 | -1.097 | -1.097 | unchanged |
|  |  | max_accel | 312.0 | 743.203 | 363.038 | 363.038 | 431.203 | 51.038 | 51.038 | unchanged |
|  |  | settle_time_s | 0.0 | 1.28 | 1.4 | 1.4 | 1.28 | 1.4 | 1.4 | unchanged |
|  |  | overshoot | 4.6 | 74.967 | 79.247 | 79.247 | 70.367 | 74.647 | 74.647 | unchanged |
|  |  | residual_drift | 79.225 | 0.0 | 0.001 | 0.001 | -79.225 | -79.224 | -79.224 | unchanged |
| yaw_left | present_in_all | response_delay_s | 0.25 | 0.04 | 0.06 | 0.06 | -0.21 | -0.19 | -0.19 | unchanged |
|  |  | peak_rate | 70.75 | 81.999 | 65.924 | 65.924 | 11.249 | -4.826 | -4.826 | unchanged |
|  |  | max_accel | 177.5 | 743.179 | 286.636 | 286.636 | 565.679 | 109.136 | 109.136 | unchanged |
|  |  | settle_time_s | 0.0 | 1.28 | 1.42 | 1.42 | 1.28 | 1.42 | 1.42 | unchanged |
|  |  | overshoot | 6.9 | 74.968 | 62.668 | 62.668 | 68.068 | 55.768 | 55.768 | unchanged |
|  |  | residual_drift | 63.312 | 0.0 | 0.0 | 0.0 | -63.312 | -63.312 | -63.312 | unchanged |

## Strong-category assessment

- lateral_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=3/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=3/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_left: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=4/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.

## Provisional-category notes

- forward_step: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- lateral_left: no_clear_directional_gain; moved_correct_direction=False; real_segmentation_confidence=unknown; sim_amplitude_confidence=unknown; sim_amplitude_provenance=unknown.
- climb: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- descent: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.

## Decision summary

- Climb coverage complete: True
- Strongest high-confidence divergence: yaw_left
- Lateral right acceptable: False
- Yaw left acceptable: False
- Normal-mode fidelity good enough to move on: False
- Single next tuning target: yaw_left (yaw asymmetry simplification pass; supersedes earlier yaw_right-only recommendation after latest `session_20260402_182438` comparison)
