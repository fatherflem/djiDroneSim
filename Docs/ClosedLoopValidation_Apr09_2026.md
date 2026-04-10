# Closed-Loop Validation (Apr 9, 2026)

## Scope and latest evidence

- Real benchmark CSV: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`
- Session anchors:
  - `BenchmarkRuns/session_20260409_125031.zip` (pre-regression reference)
  - `BenchmarkRuns/session_20260409_145236.zip` (yaw-regressed)
  - `BenchmarkRuns/session_20260409_164309.zip` (post-yaw-fix validation)
  - `BenchmarkRuns/session_20260409_170224.zip` (same tuning as 164309; forward baseline repeat)
  - `BenchmarkRuns/session_20260409_180413.zip` (forward-step amplitude updated to pitch=1.0; exposed carryover)
  - `BenchmarkRuns/session_20260409_183817.zip` (**forward brake-slew validation; latest decisive evidence**)

The newest decisive evidence is `session_20260409_183817`.

## Forward-focused Apr 9 comparison (`164309`/`170224`/`180413`/`183817`)

Values are from protocol run CSVs in each session zip (forward run #2 input phase vs full run):

| Category | `164309` | `170224` | `180413` | `183817` | Story |
|---|---:|---:|---:|---:|---|
| forward_step input-phase peak | 2.112 m/s | 2.112 m/s | 2.220 m/s | 2.220 m/s | onset gain from `pitch=1.0` preserved |
| forward_step full-run peak | 2.400 m/s | 2.400 m/s | 3.060 m/s | 2.720 m/s | brake-slew cut post-release overshoot materially |
| forward extra after release (full - input) | 0.288 m/s | 0.288 m/s | 0.840 m/s | 0.500 m/s | carryover reduced by 0.340 m/s (~40.5%) |
| lateral_right input peak | 8.925 m/s | 8.925 m/s | 8.925 m/s | 8.925 m/s | unchanged |
| lateral_left input peak | 9.812 m/s | 9.812 m/s | 9.812 m/s | 9.812 m/s | unchanged |
| climb input peak | 2.940 m/s | 2.940 m/s | 2.940 m/s | 2.940 m/s | unchanged |
| descent input peak | 2.925 m/s | 2.925 m/s | 2.925 m/s | 2.925 m/s | unchanged |
| yaw_right input peak | 79.893 °/s | 79.893 °/s | 79.893 °/s | 79.893 °/s | unchanged |
| yaw_left input peak | 79.892 °/s | 79.892 °/s | 79.892 °/s | 79.894 °/s | unchanged within noise |

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

## Forward architecture diagnosis from `180413` + `183817`

Current forward math in `DJIStyleFlightController` is effectively:
- target speed from `input.Pitch * maxForwardSpeed`
- forward speed-error P term, accel-clamped to `±globalForwardAccelLimit`
- then global vector slew via `MoveTowards(..., accelerationSlewRate * dt)`

With Normal tuning in `180413` (`maxForwardSpeed=3.5`, `forwardAcceleration=2.8`, `globalForwardAccelLimit=3.0`, `accelerationSlewRate=6.0`):
- Onset reaches +3.0 m/s² in ~0.5 s and predicts ~2.25 m/s gain in a 1.0 s input window, close to observed 2.22 m/s.
- If release starts near +3.0 m/s² and slews to zero at 6.0 m/s³, expected extra speed after release is ~0.75 m/s; observed extra is ~0.84 m/s.

`183817` validated the targeted fix direction:
- input-phase peak stayed at `2.220 m/s` (same as `180413`)
- full-run peak dropped from `3.060` to `2.720 m/s`
- post-release carryover dropped from `0.840` to `0.500 m/s` (`-40.5%`)

Conclusion: forward mismatch is now mostly a manageable residual carryover gap; architecture change worked as intended.

## Implemented forward-only corrective patch (this iteration)

- Added `forwardAccelerationSlewRate` and `forwardBrakeSlewRate` to `DroneFlightModeConfig` with backward-compatible fallback (`<=0` uses global `accelerationSlewRate`).
- In `DJIStyleFlightController`, kept existing global vector slew for all axes, then applied forward-specific local-Z slew using:
  - active pitch input -> `forwardAccelerationSlewRate`
  - pitch neutral/release -> `forwardBrakeSlewRate`
- Normal mode initial values: `forwardAccelerationSlewRate = 6`, `forwardBrakeSlewRate = 11`.

Math target for release carryover with `A=3.0 m/s²` and `brake_slew=11 m/s³`:
- extra carryover ≈ `A² / (2*brake_slew) = 9/22 = 0.41 m/s`
- prior symmetric slew (`6`) predicted ≈ `0.75 m/s`, so this pass targets roughly `0.34 m/s` less coast-up while keeping onset behavior aligned to current `6` slew.

## Decision summary (post-183817 audit)

1. Yaw held-input behavior remains fixed and **done for now**.
2. Keep right-lateral trim (`0.88` speed multiplier, `0.92` acceleration multiplier).
3. Forward-specific slew separation worked (onset preserved, release carryover reduced); current `183817` state is close enough to freeze for now.
4. No further micro-retune is strongly justified without new real-evidence goals; if resumed later, keep it forward-only and tiny.
5. Vertical remains a protocol/onset-shape investigation, not immediate gain inflation.


## Apr 10, 2026 implementation update (pre-benchmark)

A follow-up code/assets pass has been completed to execute the next validation phase:
- Added protocol maneuvers `climb_long` and `descent_long` (2.5s throttle windows, orders 9 and 10) so vertical slew/protocol-limitation can be tested in the same F9 session as the original 1.0s climbs/descents.
- Added `BenchmarkRunner` runtime `protocolModeOverride` (None/Cine/Normal/Sport) to run the same protocol under Cine/Sport without duplicating maneuver assets.
- Added `Docs/AcceptanceCriteria.md` and recorded current pass/fail/blocked state from existing Apr 9 evidence.

No new benchmark session ID is attached yet for these additions, so vertical root-cause status remains **hypothesis pending validation** as of April 10, 2026.
