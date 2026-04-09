# Closed-Loop Validation (Apr 9, 2026)

## Scope and latest evidence

- Real benchmark CSV: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`
- Session anchors:
  - `BenchmarkRuns/session_20260409_125031.zip` (pre-regression reference)
  - `BenchmarkRuns/session_20260409_145236.zip` (yaw-regressed)
  - `BenchmarkRuns/session_20260409_164309.zip` (**post-patch validation, latest decisive evidence**)

The newest decisive evidence is `session_20260409_164309`.

## Key three-session comparison (`125031` vs `145236` vs `164309`)

Values are from `Tools/analyze_airdata.py` against the Apr 8 real benchmark log.

| Category | `125031` sim peak | `145236` sim peak | `164309` sim peak | Story |
|---|---:|---:|---:|---|
| forward_step | 2.112 m/s | 2.112 m/s | 2.112 m/s | unchanged (still unresolved) |
| lateral_right | 9.812 m/s | 8.925 m/s | 8.925 m/s | right-side improvement preserved |
| lateral_left | 9.812 m/s | 9.812 m/s | 9.812 m/s | unchanged |
| climb | 2.940 m/s | 2.940 m/s | 2.940 m/s | unchanged |
| descent | 2.911 m/s | 2.925 m/s | 2.925 m/s | effectively unchanged |
| yaw_right | 79.597 °/s | 38.829 °/s | 79.893 °/s | fixed back to expected range |
| yaw_left | 79.596 °/s | 38.831 °/s | 79.892 °/s | fixed back to expected range |

## Yaw regression diagnosis (code + math + behavior)

### Regressed active-input law

In `DJIStyleFlightController`, the regressed active-stick branch used:

- `yawError = targetYawRate - currentYawRate`
- `yawDamping = currentYawRate * yawStopAuthority`
- `rawYawAcceleration = yawError * yawCatchUpAuthority - yawDamping`

That produces held-input equilibrium:

`equilibriumYawRate = targetYawRate * yawCatchUpAuthority / (yawCatchUpAuthority + yawStopAuthority)`

Using Normal values (`maxYawRateDegrees=82`, `yawCatchUpSpeed=3.6`, `yawStopSpeed=4.0`):

`82 * 3.6 / (3.6 + 4.0) = 38.84 °/s`

This matches `145236` (~38.83 °/s right/left), proving a structural bias.

### Patched law

Active-stick branch now uses catch-up only:

- `rawYawAcceleration = (targetYawRate - currentYawRate) * yawCatchUpAuthority`

Neutral stick still uses hard-stop braking (`MoveTowards`) with `yawStopAuthority`.

### Behavioral validation in `164309`

- **Held input restored:**
  - yaw_right peak `79.893 °/s`
  - yaw_left peak `79.892 °/s`
- **Release remains crisp:** from settle-start yaw rate to near-zero:
  - `125031`: `<5°/s` in `0.26s`, `<1°/s` in `0.28s`
  - `164309`: `<5°/s` in `0.26s`, `<1°/s` in `0.28s`

No obvious long tail appeared; post-patch stop profile matches the healthy reference session.

## Isolation check (did yaw patch disturb other axes?)

`145236 -> 164309` shows:
- `forward_step`: `2.112 -> 2.112` (no change)
- `lateral_right`: `8.925 -> 8.925` (right-trim preserved)
- `lateral_left`: `9.812 -> 9.812` (no change)
- `climb`: `2.940 -> 2.940` (no change)
- `descent`: `2.925 -> 2.925` (no change)
- `yaw_right`: `38.829 -> 79.893` (fixed)
- `yaw_left`: `38.831 -> 79.892` (fixed)

Conclusion: observed post-patch change is isolated to yaw held-input behavior.

## Vertical interpretation update

Vertical remains essentially flat despite authority increases (`~2.94` climb / `~2.93` descent). Under current protocol this still fits a slew/protocol-limited explanation:

- `climb` / `descent` holds are `1.0s`
- commanded acceleration is slew-limited (`accelerationSlewRate`) before application
- short window peak can stay ramp-limited

So this is still not strong evidence for a simple raw-gain deficit.

## Decision summary (post-164309 audit)

1. Yaw held-input regression is fixed and can be considered **done for now**.
2. Keep right-lateral trim (`0.88` speed multiplier, `0.92` acceleration multiplier).
3. Before any forward controller retune, rerun the benchmark with `forward_step` protocol input updated to `pitch = 1.0` (legacy `0.77` was Mar 30-era calibration).
4. After that rerun, reassess forward mismatch using the corrected input definition.
5. Treat vertical as a protocol/onset-shape investigation next, not immediate gain inflation.
