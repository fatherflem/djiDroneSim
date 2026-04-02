# Targeted Physics Refinement Pass — April 2, 2026

## Intent
This pass is intentionally narrow and benchmark-driven.

Strong-category drivers from the latest comparison summary:
- `lateral_right`: simulator too aggressive (high-confidence)
- `yaw_right`: simulator too sluggish (high-confidence)
- `yaw_left`: simulator too aggressive (high-confidence)

Provisional categories (small adjustments only):
- `forward_step` too aggressive, medium-confidence amplitude
- `climb` too sluggish, provisional
- `descent` too sluggish, provisional

## Implemented changes

### High-confidence changes
1. Added right-lateral-only shaping knobs to `DroneFlightModeConfig` and applied them in controller logic.
   - `lateralRightSpeedMultiplier`
   - `lateralRightAccelerationMultiplier`
   - `lateralRightStopMultiplier`
2. Added yaw response-shape controls:
   - `yawStopSpeed` for neutral-input yaw braking
   - `yawRightCommandGain` / `yawLeftCommandGain` for directional command correction
3. Updated Normal mode values to reduce right-lateral aggressiveness and reduce yaw asymmetry.

### Provisional changes (kept modest)
1. Slightly reduced Normal `forwardAcceleration`.
2. Slightly increased Normal `maxClimbSpeed`, `maxDescentSpeed`, and `verticalAcceleration`.

## Why these are still pending verification
No claim of final accuracy is made in this document. These edits are hypothesis-driven from the comparison deltas and must be verified with a fresh full protocol run and regenerated sim-vs-real comparison outputs.

## Next benchmark expectations
Expected directional movement in the next comparison:
- `lateral_right`: lower peak/accel and less overshoot/coast; faster settle.
- `yaw_right`: faster turn onset and stronger achieved rate.
- `yaw_left`: less peak/accel and reduced overshoot tendency.
- `yaw_right`/`yaw_left`: closer start/stop symmetry and settle consistency.
- `forward_step`, `climb`, `descent`: only small directional shifts due to provisional confidence.

---

## Follow-up narrow pass: yaw_right-only refinement (April 2, 2026)

### Scope lock
- Single active target: `yaw_right` in **Normal** mode.
- Non-target strong categories (`yaw_left`, `lateral_right`) are explicitly **not** claimed solved in this pass.
- Provisional categories (`forward_step`, `climb`, `descent`) were not retuned in tuning intent.

### Why yaw_right was still materially off
The previous directional yaw command shaping used:
- `shapedYawInput = Clamp(inputYaw * yawDirectionGain, -1, 1)`.

For full-stick `yaw_right` (`inputYaw = +1.0`), `yawRightCommandGain > 1.0` saturated back to `+1.0`, so the right-gain had little/no effect exactly where benchmark runs spend most of the segment. This left right-yaw peak/rate buildup under-corrected.

### Yaw-right-focused fix implemented
1. **Command-path correction (shared logic, right-targeted effect)**
   - Clamp raw input first, then apply directional gain in yaw-rate target space:
   - `targetYawRate = Clamp(inputYaw, -1, 1) * maxYawRateDegrees * yawDirectionGain`.
   - Result: positive yaw gain now affects full-stick right yaw as intended.
2. **Right-only neutral braking support**
   - Added `yawRightStopMultiplier` for neutral-input damping when current yaw-rate is rightward.
   - Keeps release/stop behavior tighter for right turns without changing left stop authority.
3. **Normal mode tuning update (yaw-right only intent)**
   - Increased `yawRightCommandGain` modestly.
   - Added `yawRightStopMultiplier` > 1.0 to counter stop/overshoot side effects from stronger right command.
   - Left-yaw gain retained unchanged.

### Expected outcome and required validation
- Targeted improvement expected in `yaw_right`: onset/buildup/peak with controlled release and shorter settle tail.
- `yaw_left` remains unacceptable from prior closed-loop results and remains a non-target here.
- `lateral_right` remains unacceptable from prior closed-loop results and remains a non-target here.
- Another full benchmark rerun + comparison regeneration is required before making any acceptance claim.

## Post-fix validation outcome (session_20260402_163547)

A fresh closed-loop comparison was run using:
- baseline: `session_20260402_133209`
- pre-fix reference: `session_20260402_161237`
- post-fix rerun: `session_20260402_163547`

Result summary for the narrow right-yaw pass:
- `yaw_right` onset/acceleration moved closer to real.
- `yaw_right` release overshoot and settle did **not** improve (slight regressions).
- `yaw_right` peak-rate closeness was effectively preserved (minor negative drift only).
- `yaw_left` remained stable (no measurable regression), matching right-only protection intent.

Validation classification:
- `yaw_right`: improved-but-still-off (shape mismatch remains concentrated in release/settle control).

Single next step from this result set:
- **one more narrow `yaw_right` pass** focused specifically on right-release stop/settle shaping.

---

## Multi-axis tuning pass: yaw stop fix + lateral/forward/vertical corrections (April 2, 2026)

### Scope
This pass addresses the dominant problems identified in the `ClosedLoopValidation_Apr02_2026.md` comparison (session_20260402_163547 vs real AirData). Changes span both the flight controller architecture (yaw stop control law) and Normal mode gain tuning across all axes.

### Problem summary driving this pass
From the latest closed-loop validation:
- **Yaw overshoot (both directions)**: 60-80 degrees sim vs ~5 degrees real. Settle time 1.3-1.4s sim vs ~0.1s real. Root cause: exponential blend (`Lerp` with `1 - exp(-authority * dt)`) for yaw stop inherently produces a long tail. Real DJI applies near-instant hard brake.
- **Yaw onset too sharp**: Max accel 600+ deg/s^2 sim vs 178-312 real. The exponential onset has maximum acceleration at t=0.
- **Lateral right way too aggressive**: Peak rate 2.29 sim vs 0.2 real (10x). Prior right-multipliers insufficient.
- **Forward step too aggressive** (provisional): Peak rate 3.48 sim vs 1.07 real.
- **Climb/descent peak rates too low but accel hitting ceiling**: verticalAcceleration gain too high, speed limits too low.

### Changes implemented

#### 1. Yaw stop control law change (architectural — `DJIStyleFlightController.cs`)
Replaced the single exponential-blend yaw path with a branched approach:
- **Neutral input (stick released):** `Mathf.MoveTowards(currentYawRate, 0, maxYawDecel)` — linear deceleration that eliminates the exponential tail. With `yawStopSpeed=13` and `maxYawRateDegrees=74`, decel is ~19.2 deg/s per tick, stopping from 80 deg/s in ~0.08s.
- **Active input:** Preserved existing exponential catch-up blend for smooth onset ramp.

This is the single most impactful change — it directly addresses the dominant yaw overshoot/settle mismatch in both directions.

#### 2. Yaw onset slowed (`DroneModeNormal.asset`)
- `yawCatchUpSpeed`: 10 → 7 (slower exponential ramp matches real ~0.2s onset delay)
- `yawRightCatchUpMultiplier`: 0.82 → 1.0 (simplified; base catch-up is now slow enough)
- `yawRightReleaseStopMultiplier`: 1.35 → 1.0 (hard stop makes release-window shaping unnecessary)
- `yawRightReleaseStopDuration`: 0.22 → 0 (disabled; hard stop is sufficient)

#### 3. Lateral right aggressiveness reduced (`DroneModeNormal.asset`)
- `lateralRightSpeedMultiplier`: 0.9 → 0.5 (effective right lateral max: 1.4 m/s)
- `lateralRightAccelerationMultiplier`: 0.82 → 0.4 (effective right accel: 1.12 m/s^2)
- `lateralRightStopMultiplier`: 1.15 → 1.3 (stronger braking for rightward motion)

#### 4. Forward step reduced (provisional — `DroneModeNormal.asset`)
- `maxForwardSpeed`: 4.6 → 4.0
- `forwardAcceleration`: 4.0 → 3.2
- `forwardStopStrength`: 7.2 → 6.5

These are modest changes given the provisional input amplitude (0.77 normalized).

#### 5. Climb/descent rebalanced (provisional — `DroneModeNormal.asset` + controller)
- `maxClimbSpeed`: 2.5 → 3.8 (real peak is 3.63 at benchmark amplitude)
- `maxDescentSpeed`: 3.1 → 4.3 (real peak is 4.07 at benchmark amplitude)
- `verticalAcceleration`: 5.0 → 3.5 (gentler ramp to avoid overshoot-then-brake pattern)
- `globalVerticalAccelLimit`: 10.0 → 7.0 (closer to real max_accel values of 4-6)

### Expected benchmark outcomes
| Category | Metric | Before | Expected after | Confidence |
|---|---|---:|---|---|
| yaw_right | overshoot | 79.8 deg | < 15 deg | high (control law fix) |
| yaw_right | settle_time | 1.38s | < 0.3s | high |
| yaw_left | overshoot | 61.9 deg | < 15 deg | high |
| yaw_left | settle_time | 1.32s | < 0.3s | high |
| yaw_right | max_accel | 638 | ~350-450 | medium (slower onset) |
| yaw_left | max_accel | 603 | ~300-400 | medium |
| lateral_right | peak_rate | 2.29 | < 1.0 | medium-high |
| lateral_right | overshoot | 2.00 | < 0.5 | medium-high |
| forward_step | peak_rate | 3.48 | ~2.5-3.0 | low (provisional amplitude) |
| climb | peak_rate | 2.49 | ~3.0-3.5 | low (provisional) |
| descent | peak_rate | 3.08 | ~3.5-4.0 | low (provisional) |

### What was NOT changed
- All fields preserved in `DroneFlightModeConfig` (multipliers set to 1.0 / durations to 0 rather than removed)
- No changes to benchmark infrastructure, camera systems, input systems, UI, or scene structure
- Cine/Sport modes untouched (Normal first, derived later)
- No new dependencies added

### Required next step
Full benchmark rerun (F9 in Play Mode) + closed-loop comparison to validate these predictions.
