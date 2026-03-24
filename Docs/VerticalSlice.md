# DJI Drone Simulator Vertical Slice

## Architecture

The first slice is organized into small, focused Unity C# components:

- **Input layer**: `DroneInputReader` reads Unity Input System actions created from `DroneInputConfig` bindings.
- **Flight mode/config layer**: `DroneFlightModeConfig` ScriptableObjects define tunable limits for Cine, Normal, and Sport-style response.
- **Flight controller layer**: `DJIStyleFlightController` converts pilot intent into target horizontal velocity, vertical speed, and yaw rate.
- **Physics plant layer**: `DronePhysicsBody` owns the Rigidbody and applies acceleration / yaw commands.
- **Training/scoring layer**: `SimpleTrainingScenario` tracks hover-box performance and exposes score/progress data.
- **Debug UI layer**: `DroneDebugHUD` renders live telemetry and input data using a lightweight `OnGUI` HUD.
- **Bootstrap layer**: `VerticalSliceBootstrap` builds a runnable desktop training scene at startup so the project can be opened cleanly even from an empty repository baseline.

## Controls

The vertical slice targets a RadioMaster T8L in **Mode 2** through Unity's Input System.

Default bindings in `DroneInputConfig`:

- Roll: `<Joystick>/x`
- Pitch: `<Joystick>/y`
- Throttle: `<Joystick>/z`
- Yaw: `<Joystick>/rx`

Keyboard/gamepad fallbacks are included for editor testing:

- Roll/Pitch: left stick or WASD / arrow style bindings
- Throttle: `R/F` or gamepad triggers
- Yaw: `Q/E` or gamepad shoulder buttons
- Mode select: `1` = Cine, `2` = Normal, `3` = Sport

## Tunable parameters

The most important tuning values are exposed on ScriptableObjects and controller components:

- `maxHorizontalSpeed`
- `horizontalAcceleration`
- `horizontalStopStrength`
- `maxVerticalSpeed`
- `verticalAcceleration`
- `maxYawRateDegrees`
- `tiltLimitDegrees`
- `tiltSmoothing`
- `stickDeadzone`
- `stickExpo`
- `hoverBoxSize` / `targetAltitude`

These values are intentionally inspector-driven so the classroom feel can be tuned without rewriting control logic.

## Scene content

`Assets/Scenes/DroneTrainingVerticalSlice.unity` contains a bootstrap object that builds:

- a ground plane
- simple marker pylons and a hover box
- a follow camera
- the drone prefab
- debug / telemetry systems

## Assumptions

- The repository started effectively empty, so this slice bootstraps a minimal Unity 6 project structure.
- Hover assist is modeled as **velocity-hold plus gravity compensation**, not GPS-grade position lock.
- The rigidbody root stays upright for stable classroom behavior; visible tilt is applied on a child visual transform to match commanded motion.

## Recommended next steps

1. Replace runtime-generated level geometry with authored Unity prefabs.
2. Add an ATTI-like degraded mode and wind disturbances.
3. Expand telemetry into replayable debrief data.
4. Add UI-based calibration for controller deadzones and axis inversion.
5. Add authored training drills beyond the hover-box scenario.
