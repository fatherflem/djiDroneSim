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
