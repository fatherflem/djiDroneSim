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
