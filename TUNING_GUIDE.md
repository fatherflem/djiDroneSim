# DJI Drone Sim Tuning Guide (Unity 6)

This guide is for practical "feel" tuning of the current stabilized DJI-style controller.

## Start here: highest-impact parameters

Tune these first (in this order):

1. `stickDeadzone` (DroneInputConfig)
2. `stickExpo` (DroneInputConfig)
3. `maxHorizontalSpeed` (mode config)
4. `horizontalAcceleration` (mode config)
5. `horizontalStopStrength` (mode config)
6. `maxYawRateDegrees` (mode config)
7. `yawCatchUpSpeed` (mode config)
8. `maxVerticalSpeed` (mode config)
9. `verticalAcceleration` (mode config)
10. `gravityCancelMultiplier` (DJIStyleFlightController)

## What each parameter changes

- **stickDeadzone**: How much stick movement is ignored around center. Increase if noisy radio centers cause drift.
- **stickExpo**: Softens center stick response while keeping full deflection authority.
- **maxHorizontalSpeed**: Top lateral/forward speed.
- **horizontalAcceleration**: How quickly the drone reaches the requested horizontal speed.
- **horizontalStopStrength**: How aggressively it brakes when sticks return near center.
- **maxYawRateDegrees**: Maximum turn rate.
- **yawCatchUpSpeed**: How quickly yaw reaches commanded rate (higher = snappier).
- **maxVerticalSpeed**: Maximum climb/descent speed.
- **verticalAcceleration**: How quickly climb/descent rate changes.
- **gravityCancelMultiplier**: Baseline upward assist against gravity; affects hover feel.

## Mode targets

Use `DroneModeCine`, `DroneModeNormal`, and `DroneModeSport` assets.

### Cine (smooth teaching mode)
- Lower `maxHorizontalSpeed`
- Lower `maxYawRateDegrees`
- Lower `horizontalAcceleration`
- Medium `horizontalStopStrength` (don’t let it feel mushy)
- Slightly higher `stickExpo`

### Normal (default training mode)
- Balanced speed and braking
- Moderate yaw rate
- Predictable stop behavior
- Good first baseline before touching Cine/Sport

### Sport (aggressive mode)
- Higher `maxHorizontalSpeed`
- Higher `horizontalAcceleration`
- Higher `maxYawRateDegrees`
- Keep `horizontalStopStrength` high enough to remain teachable

## Symptom -> likely fix

- **Drone drifts too much at center**
  - Increase `stickDeadzone` (common range: **0.12-0.18**).
  - Verify controller calibration and axis center quality.

- **Drone feels too floaty**
  - Increase `verticalAcceleration`.
  - Slightly increase `gravityCancelMultiplier` if hover is sagging.

- **Drone stops too abruptly**
  - Decrease `horizontalStopStrength`.
  - Increase `brakingInputDeadband` only slightly if micro-inputs trigger braking too early.

- **Drone takes too long to stop**
  - Increase `horizontalStopStrength`.
  - Optionally reduce `maxHorizontalSpeed` for that mode.

- **Yaw feels too twitchy**
  - Decrease `maxYawRateDegrees` first.
  - Then lower `yawCatchUpSpeed` if still snappy.

- **Yaw feels sluggish / delayed**
  - Increase `yawCatchUpSpeed`.
  - If needed, increase `maxYawRateDegrees` modestly.

- **Throttle does not feel hover-like**
  - Tune `gravityCancelMultiplier` near 1.0.
  - Then refine with `verticalAcceleration` and `maxVerticalSpeed`.

## Practical workflow

1. Tune **Normal** mode first until it feels right.
2. Derive **Cine** by reducing speed/rates and softening response.
3. Derive **Sport** by increasing speed/rates and preserving control authority.
4. Make one change at a time and test in the hover box drill with HUD visible.
5. Record short telemetry runs so changes can be compared objectively.
