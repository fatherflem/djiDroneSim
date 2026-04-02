# Closed-Loop Validation (Real vs Baseline Sim vs Post-Tuning Sim)

- Real benchmark CSV: `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Baseline simulator session: `BenchmarkRuns/session_20260402_133209.zip` (zip)
- Post-tuning simulator session: `BenchmarkRuns/session_20260402_140700.zip` (zip)
- Canonical drop location for benchmark sessions: `BenchmarkRuns/`.
- Workflow note: drop session zip files directly into `BenchmarkRuns/`; no manual per-session folder setup is required.
- Missing run(s) in post-tuning session (vs baseline expected runs): run_001:climb source=manual order=5
- Missing category label(s) in post-tuning session: climb

## Session coverage

- Baseline manifest run count (expected for this comparison): 9
- Post-tuning manifest run count: 8
- Baseline primary protocol run count: 8
- Post-tuning primary protocol run count: 8
- Baseline excluded runs: 1
- Post-tuning excluded runs: 0

## Category comparison (old vs new vs real)

| Category | Availability | Metric | Real | Old | New | Old Δ | New Δ | Abs Δ improvement |
|---|---|---|---:|---:|---:|---:|---:|---:|
| hover_hold | present_in_all | response_delay_s | - | 0.0 | 0.0 | - | - | - |
|  |  | peak_rate | - | 0.0 | 0.0 | - | - | - |
|  |  | max_accel | - | 0.0 | 0.0 | - | - | - |
|  |  | settle_time_s | - | 0.0 | 0.0 | - | - | - |
|  |  | overshoot | - | 0.0 | 0.0 | - | - | - |
|  |  | residual_drift | - | 0.0 | 0.0 | - | - | - |
| forward_step | present_in_all | response_delay_s | 0.0 | 0.08 | 0.08 | 0.08 | 0.08 | 0.0 |
|  |  | peak_rate | 1.067 | 3.954 | 3.478 | 2.887 | 2.411 | 0.476 |
|  |  | max_accel | 0.667 | 8.0 | 8.0 | 7.333 | 7.333 | 0.0 |
|  |  | settle_time_s | 0.55 | 1.02 | 1.06 | 0.47 | 0.51 | -0.04 |
|  |  | overshoot | 0.142 | 3.184 | 2.869 | 3.042 | 2.727 | 0.315 |
|  |  | residual_drift | 0.913 | 0.001 | 0.0 | -0.912 | -0.913 | -0.0 |
| lateral_right | present_in_all | response_delay_s | 0.0 | 0.08 | 0.08 | 0.08 | 0.08 | 0.0 |
|  |  | peak_rate | 0.2 | 4.11 | 2.291 | 3.91 | 2.091 | 1.82 |
|  |  | max_accel | 0.333 | 8.0 | 5.786 | 7.667 | 5.453 | 2.214 |
|  |  | settle_time_s | 1.1 | 0.98 | 1.18 | -0.12 | 0.08 | 0.04 |
|  |  | overshoot | 0.2 | 3.268 | 1.997 | 3.068 | 1.797 | 1.271 |
|  |  | residual_drift | 0.0 | 0.002 | 0.0 | 0.002 | 0.0 | 0.002 |
| lateral_left | present_in_all | response_delay_s | - | 0.08 | 0.06 | - | - | - |
|  |  | peak_rate | - | 4.11 | 2.652 | - | - | - |
|  |  | max_accel | - | 8.0 | 7.84 | - | - | - |
|  |  | settle_time_s | - | 0.98 | 1.14 | - | - | - |
|  |  | overshoot | - | 3.268 | 2.265 | - | - | - |
|  |  | residual_drift | - | 0.002 | 0.0 | - | - | - |
| climb | present_in_all | response_delay_s | 0.25 | 0.06 | 0.06 | -0.19 | -0.19 | 0.0 |
|  |  | peak_rate | 3.625 | 2.386 | 2.488 | -1.239 | -1.137 | 0.102 |
|  |  | max_accel | 5.75 | 10.0 | 10.0 | 4.25 | 4.25 | 0.0 |
|  |  | settle_time_s | 0.0 | 1.06 | 1.08 | 1.06 | 1.08 | -0.02 |
|  |  | overshoot | 1.04 | 2.022 | 2.117 | 0.982 | 1.077 | -0.094 |
|  |  | residual_drift | 1.847 | 0.002 | 0.002 | -1.845 | -1.845 | -0.0 |
| descent | present_in_all | response_delay_s | 0.167 | 0.06 | 0.06 | -0.107 | -0.107 | 0.0 |
|  |  | peak_rate | 4.067 | 2.981 | 3.083 | -1.086 | -0.984 | 0.103 |
|  |  | max_accel | 4.0 | 10.0 | 10.0 | 6.0 | 6.0 | -0.0 |
|  |  | settle_time_s | 0.0 | 1.06 | 1.06 | 1.06 | 1.06 | 0.0 |
|  |  | overshoot | 0.687 | 2.501 | 2.591 | 1.814 | 1.904 | -0.091 |
|  |  | residual_drift | 3.233 | 0.003 | 0.002 | -3.23 | -3.231 | -0.001 |
| yaw_right | present_in_all | response_delay_s | 0.24 | 0.04 | 0.04 | -0.2 | -0.2 | 0.0 |
|  |  | peak_rate | 84.6 | 81.998 | 73.998 | -2.602 | -10.602 | -8.001 |
|  |  | max_accel | 312.0 | 743.203 | 670.696 | 431.203 | 358.696 | 72.507 |
|  |  | settle_time_s | 0.0 | 1.28 | 1.32 | 1.28 | 1.32 | -0.04 |
|  |  | overshoot | 4.6 | 74.967 | 68.771 | 70.367 | 64.171 | 6.196 |
|  |  | residual_drift | 79.225 | 0.0 | 0.0 | -79.225 | -79.225 | 0.0 |
| yaw_left | present_in_all | response_delay_s | 0.25 | 0.04 | 0.04 | -0.21 | -0.21 | 0.0 |
|  |  | peak_rate | 70.75 | 81.999 | 66.599 | 11.249 | -4.151 | 7.098 |
|  |  | max_accel | 177.5 | 743.179 | 603.638 | 565.679 | 426.138 | 139.541 |
|  |  | settle_time_s | 0.0 | 1.28 | 1.32 | 1.28 | 1.32 | -0.04 |
|  |  | overshoot | 6.9 | 74.968 | 61.895 | 68.068 | 54.995 | 13.073 |
|  |  | residual_drift | 63.312 | 0.0 | 0.0 | -63.312 | -63.312 | 0.0 |

## Strong-category assessment

- lateral_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=4/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=2/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_left: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=3/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.

## Provisional-category notes

- forward_step: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- lateral_left: no_clear_directional_gain; moved_correct_direction=False; real_segmentation_confidence=unknown; sim_amplitude_confidence=unknown; sim_amplitude_provenance=unknown.
- climb: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- descent: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
