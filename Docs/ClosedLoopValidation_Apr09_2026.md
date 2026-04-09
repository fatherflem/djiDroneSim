# Closed-Loop Validation (Real vs Baseline vs Prior Rerun vs Newest Rerun)

- Real benchmark CSV: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`
- Baseline simulator session: `BenchmarkRuns/session_20260409_115951.zip` (zip)
- Prior post-tuning simulator session: `BenchmarkRuns/session_20260409_122756.zip` (zip)
- Newest simulator rerun (climb-coverage closeout): `BenchmarkRuns/session_20260409_125031.zip` (zip)
- Canonical drop location for benchmark sessions: `BenchmarkRuns/`.
- Workflow note: drop session zip files directly into `BenchmarkRuns/`; no manual per-session folder setup is required.
- Missing run(s) in newest session (vs baseline expected runs): (none)
- Missing category label(s) in newest session: (none)

## Session coverage

- Baseline manifest run count: 8
- Prior rerun manifest run count: 8
- Newest rerun manifest run count: 8
- Baseline primary protocol run count: 8
- Prior rerun primary protocol run count: 8
- Newest rerun primary protocol run count: 8
- Baseline excluded runs: 0
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
| forward_step | present_in_all | response_delay_s | 0.3 | 0.28 | 0.28 | 0.28 | -0.02 | -0.02 | -0.02 | unchanged |
|  |  | peak_rate | 2.633 | 2.112 | 2.112 | 2.112 | -0.521 | -0.521 | -0.521 | unchanged |
|  |  | max_accel | 2.972 | 3.0 | 3.0 | 3.0 | 0.028 | 0.028 | 0.028 | unchanged |
|  |  | settle_time_s | 0.133 | 0.0 | 0.0 | 0.0 | -0.133 | -0.133 | -0.133 | unchanged |
|  |  | overshoot | 0.433 | 0.082 | 0.082 | 0.082 | -0.351 | -0.351 | -0.351 | unchanged |
|  |  | residual_drift | 1.899 | 1.961 | 1.961 | 1.961 | 0.062 | 0.062 | 0.062 | unchanged |
| lateral_right | present_in_all | response_delay_s | 0.45 | 0.6 | 0.6 | 0.6 | 0.15 | 0.15 | 0.15 | unchanged |
|  |  | peak_rate | 7.44 | 9.812 | 9.812 | 9.812 | 2.372 | 2.372 | 2.372 | unchanged |
|  |  | max_accel | 4.676 | 5.0 | 5.0 | 5.0 | 0.324 | 0.324 | 0.324 | unchanged |
|  |  | settle_time_s | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | unchanged |
|  |  | overshoot | 0.174 | 0.104 | 0.104 | 0.104 | -0.07 | -0.07 | -0.07 | unchanged |
|  |  | residual_drift | 7.125 | 9.622 | 9.622 | 9.622 | 2.497 | 2.497 | 2.497 | unchanged |
| lateral_left | present_in_all | response_delay_s | 0.65 | 0.6 | 0.6 | 0.6 | -0.05 | -0.05 | -0.05 | unchanged |
|  |  | peak_rate | 10.042 | 9.812 | 9.812 | 9.812 | -0.23 | -0.23 | -0.23 | unchanged |
|  |  | max_accel | 4.473 | 5.0 | 5.0 | 5.0 | 0.527 | 0.527 | 0.527 | unchanged |
|  |  | settle_time_s | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | unchanged |
|  |  | overshoot | 0.215 | 0.104 | 0.104 | 0.104 | -0.111 | -0.111 | -0.111 | unchanged |
|  |  | residual_drift | 9.656 | 9.622 | 9.622 | 9.622 | -0.034 | -0.034 | -0.034 | unchanged |
| climb | present_in_all | response_delay_s | 0.3 | 0.32 | 0.34 | 0.34 | 0.02 | 0.04 | 0.04 | unchanged |
|  |  | peak_rate | 4.325 | 2.628 | 2.94 | 2.94 | -1.697 | -1.385 | -1.385 | unchanged |
|  |  | max_accel | 4.75 | 4.0 | 5.881 | 5.881 | -0.75 | 1.131 | 1.131 | unchanged |
|  |  | settle_time_s | 0.425 | 0.0 | 0.0 | 0.0 | -0.425 | -0.425 | -0.425 | unchanged |
|  |  | overshoot | 1.285 | 0.16 | 0.23 | 0.23 | -1.125 | -1.055 | -1.055 | unchanged |
|  |  | residual_drift | 3.091 | 2.348 | 2.546 | 2.546 | -0.743 | -0.545 | -0.545 | unchanged |
| descent | present_in_all | response_delay_s | 0.275 | 0.32 | 0.34 | 0.34 | 0.045 | 0.065 | 0.065 | unchanged |
|  |  | peak_rate | 3.675 | 2.625 | 2.911 | 2.911 | -1.05 | -0.764 | -0.764 | unchanged |
|  |  | max_accel | 4.25 | 4.0 | 5.519 | 5.519 | -0.25 | 1.269 | 1.269 | unchanged |
|  |  | settle_time_s | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | unchanged |
|  |  | overshoot | 1.315 | 0.16 | 0.211 | 0.211 | -1.155 | -1.104 | -1.104 | unchanged |
|  |  | residual_drift | 2.281 | 2.345 | 2.539 | 2.539 | 0.064 | 0.258 | 0.258 | unchanged |
| yaw_right | present_in_all | response_delay_s | 0.3 | 0.06 | 0.06 | 0.06 | -0.24 | -0.24 | -0.24 | unchanged |
|  |  | peak_rate | 82.0 | 79.596 | 79.597 | 79.597 | -2.404 | -2.403 | -2.403 | unchanged |
|  |  | max_accel | 280.0 | 284.823 | 284.823 | 284.823 | 4.823 | 4.823 | 4.823 | unchanged |
|  |  | settle_time_s | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | unchanged |
|  |  | overshoot | 1.7 | 0.392 | 0.393 | 0.393 | -1.308 | -1.307 | -1.307 | unchanged |
|  |  | residual_drift | 80.125 | 78.86 | 78.86 | 78.86 | -1.265 | -1.265 | -1.265 | unchanged |
| yaw_left | present_in_all | response_delay_s | 0.3 | 0.06 | 0.06 | 0.06 | -0.24 | -0.24 | -0.24 | unchanged |
|  |  | peak_rate | 82.0 | 79.596 | 79.596 | 79.596 | -2.404 | -2.404 | -2.404 | unchanged |
|  |  | max_accel | 320.0 | 284.805 | 284.805 | 284.805 | -35.195 | -35.195 | -35.195 | unchanged |
|  |  | settle_time_s | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | unchanged |
|  |  | overshoot | 2.8 | 0.392 | 0.392 | 0.392 | -2.408 | -2.408 | -2.408 | unchanged |
|  |  | residual_drift | 79.625 | 78.86 | 78.86 | 78.86 | -0.765 | -0.765 | -0.765 | unchanged |

## Strong-category assessment

- lateral_right: status=still_poor; moved_correct_direction=False; abs_delta_better_metrics=0/5; response_shape=not_improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_right: status=improved_but_still_off; moved_correct_direction=True; abs_delta_better_metrics=2/5; response_shape=improved; notes=Status is based on absolute-delta movement against the real benchmark values.
- yaw_left: status=still_poor; moved_correct_direction=False; abs_delta_better_metrics=0/5; response_shape=not_improved; notes=Status is based on absolute-delta movement against the real benchmark values.

## Provisional-category notes

- forward_step: no_clear_directional_gain; moved_correct_direction=False; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- lateral_left: no_clear_directional_gain; moved_correct_direction=False; real_segmentation_confidence=high; sim_amplitude_confidence=low; sim_amplitude_provenance=estimated_from_limited_segments.
- climb: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.
- descent: directionally_improved; moved_correct_direction=True; real_segmentation_confidence=high; sim_amplitude_confidence=medium; sim_amplitude_provenance=estimated_from_limited_segments.

## Decision summary

- Climb coverage complete: True
- Strongest high-confidence divergence: yaw_left
- Lateral right acceptable: False
- Yaw left acceptable: False
- Normal-mode fidelity good enough to move on: False
- Single next tuning target: yaw_right
