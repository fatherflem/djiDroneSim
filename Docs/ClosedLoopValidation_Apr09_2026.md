# Closed-Loop Validation (Apr 9, 2026)

## Scope and latest evidence

- Real benchmark CSV: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`
- Baseline simulator session: `BenchmarkRuns/session_20260409_115951.zip`
- Prior rerun (pre-refinement): `BenchmarkRuns/session_20260409_125031.zip`
- **Latest key rerun (post-refinement): `BenchmarkRuns/session_20260409_145236.zip`**

The newest decisive evidence is `session_20260409_145236`.

## What changed from `125031` -> `145236`

Values below are from the analyzer (`Tools/analyze_airdata.py`) against the Apr 8 real benchmark log.

| Category | `125031` sim peak | `145236` sim peak | Change | Interpretation |
|---|---:|---:|---:|---|
| forward_step | 2.112 m/s | 2.112 m/s | 0.000 | unchanged |
| lateral_right | 9.812 m/s | 8.925 m/s | -0.887 | **improved toward real 7.44** |
| lateral_left | 9.812 m/s | 9.812 m/s | 0.000 | unchanged (still close to real 10.04) |
| climb | 2.940 m/s | 2.940 m/s | 0.000 | unchanged |
| descent | 2.911 m/s | 2.925 m/s | +0.014 | effectively unchanged |
| yaw_right | 79.597 °/s | 38.829 °/s | -40.768 | **major regression** |
| yaw_left | 79.596 °/s | 38.831 °/s | -40.765 | **major regression** |

## Yaw regression diagnosis (confirmed in controller code)

In `DJIStyleFlightController`, the active-stick yaw path used:

- `yawError = targetYawRate - currentYawRate`
- `yawDamping = currentYawRate * yawStopAuthority`
- `rawYawAcceleration = yawError * yawCatchUpAuthority - yawDamping`

That structure gives a non-zero-input equilibrium at:

`equilibriumYawRate = targetYawRate * yawCatchUpAuthority / (yawCatchUpAuthority + yawStopAuthority)`

With Normal mode values from the run snapshot (`maxYawRateDegrees=82`, `yawCatchUpSpeed=3.6`, `yawStopSpeed=4`):

`82 * 3.6 / (3.6 + 4.0) = 38.84 °/s`

This matches the observed collapse to ~38.8 °/s in `session_20260409_145236` and confirms a structural controller bias, not random noise.

## Vertical interpretation update

`climb`/`descent` remain near ~2.94/~2.93 m/s despite increased vertical authority values. Under the current benchmark protocol, this is likely dominated by controller slew/protocol interaction, not simply insufficient vertical gain:

- benchmark vertical input hold is only `1.0s` (`Maneuver_VerticalStep.asset`, `Maneuver_Descent.asset`)
- commanded acceleration is slew-limited by `accelerationSlewRate` before application
- therefore short-window peak speed can remain ramp-limited even when vertical gain/caps increase

Conclusion: do not auto-prescribe additional vertical gain from this evidence alone; first separate onset-shape targets from peak-speed targets in protocol interpretation.

## Decision summary (post-145236 audit)

1. Keep the right-lateral trim (`lateralRightSpeedMultiplier=0.88`, `lateralRightAccelerationMultiplier=0.92`) because it improved the known right-only overshoot problem.
2. Revert/fix the active-yaw damping structure so held yaw converges to command while neutral stick still brakes hard.
3. Reframe vertical as likely slew/protocol-limited in current 1.0s step windows; avoid blind gain-only retuning.
4. Keep forward provenance caveat active (`forward_step` amplitude confidence remains provisional/medium).
