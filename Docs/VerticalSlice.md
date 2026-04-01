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

- Roll: `<Joystick>/x` (right stick horizontal)
- Pitch: `<Joystick>/y` (right stick vertical)
- Throttle: `<Joystick>/z` (left stick vertical)
- Yaw: `<Joystick>/rx` (left stick horizontal)

`DroneInputReader` builds runtime `InputAction`s directly from `DroneInputConfig` bindings (instead of a generated `.inputactions` asset), and now applies joystick x/y compatibility bindings for both:

- `<Joystick>/x` and `<Joystick>/y`
- `<Joystick>/stick/x` and `<Joystick>/stick/y`

This addresses controllers (including RadioMaster variants) that surface right-stick axes under `/stick/x` and `/stick/y` in Unity Input Debugger.

Keyboard/gamepad fallbacks are included for editor testing:

- Roll/Pitch: left stick or WASD / arrow style bindings
- Throttle: `R/F` or gamepad triggers
- Yaw: `Q/E` or gamepad shoulder buttons
- Mode select: `1` = Cine, `2` = Normal, `3` = Sport
- Benchmark hotkeys (run/cycle maneuvers) remain on legacy `UnityEngine.Input` in `BenchmarkRunner` (`F8`/`F7` by default).

Camera/gimbal bindings (also from `DroneInputConfig`) use Input System actions:

- `cameraToggleBinding` (default `<Keyboard>/v`)
- `gimbalTiltDownBinding` / `gimbalTiltUpBinding` (default `<Keyboard>/leftBracket` / `<Keyboard>/rightBracket`)
- `gimbalResetBinding` (default `<Keyboard>/backslash`)

FPV architecture note:

- The onboard camera continuously renders into `DroneVideoFeed.FeedTexture` in both Chase and FPV.
- FPV is presented by syncing the player camera to onboard view; feed generation remains independent and always live.
- A default world display (`VRControllerScreenPlaceholder`) is auto-created by bootstrap when missing, using `DroneFeedDisplaySurface` bound to that same feed.
- `DroneCameraFeedDebugOverlay` is available for quick verification of mode, gimbal, feed resolution, and feed binding status.


## HUD polish pass (desktop-style debug windows)

The debug overlays are now IMGUI windows with runtime drag/collapse behavior:

- `DroneDebugHUD`
- `DroneCameraFeedDebugOverlay`
- `RawJoystickDiagnosticsOverlay`
- `BenchmarkRunner` status window

Each window supports:
- drag by header
- collapse/restore using header +/- button
- per-play-session rect retention with screen clamping

Defaults were adjusted to reduce center-screen obstruction:
- Main HUD: top-left
- Camera/feed status: top-right
- Benchmark: lower-left
- Raw joystick diagnostics: hidden + collapsed by default

`VerticalSliceBootstrap` helper hotkeys:
- `F2` toggle HUD + joystick windows visibility
- `F3` reset HUD/joystick/camera debug windows to default layout

## Operator / VR anchor placeholder

`VerticalSliceBootstrap` creates/uses `VRUserPlaceholder` as an in-world operator stand-in and now mounts a dedicated `DroneControllerPlaceholder` under that rig.

Anchor hierarchy for future VR integration:
- `BodyRoot`
  - `ChestAnchor`
    - `HeadAnchor`
      - `VRCameraAnchor`
    - `ControllerPropAnchor`
      - `DroneControllerPlaceholder`
        - `Body` / `LeftGrip` / `RightGrip` / `ScreenBezel` / `ScreenSurface`
      - `ControllerAnchor_Left`
      - `ControllerAnchor_Right`
      - `ControllerScreenAnchor`

The handheld controller is still primitive/prototype quality, but intentionally reads as a radio/controller body with a mounted screen.

`DroneFeedDisplaySurface` is attached to `DroneControllerPlaceholder/ScreenSurface` so the same always-live `DroneVideoFeed.FeedTexture` appears on the controller in both Chase and FPV modes. No duplicate camera feed path is created.

For future VR work:
- Replace only `DroneControllerPlaceholder` visuals with an authored asset.
- Keep `ControllerPropAnchor`, `ControllerAnchor_Left/Right`, and `ControllerScreenAnchor` to preserve head/controller mapping.
- Add VR hand/controller prefabs by parenting them to the existing controller anchors.

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
