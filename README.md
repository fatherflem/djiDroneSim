# DJI Drone Simulator (Unity 6 Vertical Slice)

This repository contains a first-pass **Unity 6** vertical slice for a DJI-style classroom drone training simulator using:
- **C#**
- **Unity Input System**
- **Rigidbody-based stabilized flight control**

The current focus is clarity and tunability, not high-fidelity aerodynamics.

## What this project currently is

- A desktop training prototype with DJI-style assisted/stabilized controls.
- Flight modes: **Cine / Normal / Sport**.
- Active braking behavior when stick input returns near center.
- A simple hover-box training drill plus telemetry/debug HUD.
- Runtime bootstrap that can assemble a runnable test setup quickly.

## Quick start (scene-authored default)

1. Open the project in **Unity 6**.
2. Open scene: `Assets/Scenes/DroneTrainingVerticalSlice.unity`.
3. Press Play.
4. The scene now includes authored runtime objects (drone instance, training scenario, HUD), and bootstrap only fills in missing pieces as a fallback.

## Scene + prefab workflow

### What is scene-authored
- `DroneTrainingVerticalSlice` now contains authored gameplay objects in the hierarchy:
  - `DroneTrainerDrone` (the playable drone instance).
  - `TrainingScenario` (`SimpleTrainingScenario`).
  - `DebugHUD` (`DroneDebugHUD`).
- These references are inspector-wired in the scene so Unity developers can inspect and tweak them directly.

### What the prefab contains
- `Assets/Resources/DroneTrainerDrone.prefab` contains the core drone stack:
  - `Rigidbody` + collider
  - `DroneVisualRig`
  - `DronePhysicsBody`
  - `DroneInputReader`
  - `DJIStyleFlightController`
  - `TelemetryRecorder`
- The flight controller now auto-resolves its visual tilt root from `DroneVisualRig`, so dropping this prefab into a scene works without bootstrap-specific initialization.

### When bootstrap is used
- `VerticalSliceBootstrap` is now an **optional fallback/debug helper** with modes:
  - `Disabled`
  - `FallbackOnly` (default; only creates missing objects and wires dependencies)
  - `ForceRuntimeBuild`
- Use it when you want a quick runtime-built test setup, or when authored scene pieces are intentionally absent.

## Camera Modes / FPV Gimbal System

The sim supports two camera modes, toggled with **V**:

- **Chase** (default): third-person follow camera behind/above the drone.
- **FPV**: drone-mounted onboard camera with gimbal pitch control.

### Architecture

| Script | Responsibility |
|--------|---------------|
| `DroneGimbalCameraRig` | Drone-mounted camera with 2-axis gimbal stabilization and configurable pitch |
| `DroneVideoFeed` | Manages a `RenderTexture` that captures the onboard camera output |
| `DroneCameraModeController` | Switches between Chase and FPV, manages active camera |
| `DroneFeedDisplaySurface` | Binds the feed texture to a mesh `Renderer` or UI `RawImage` |

All four scripts live under `Assets/Scripts/Drone/Camera/`.

### Gimbal pitch controls

Camera mode + gimbal controls now use **Unity Input System actions** (configured in `DroneInputConfig`):

- `cameraToggleBinding` (default **`V`**) toggles Chase/FPV
- `gimbalTiltDownBinding` / `gimbalTiltUpBinding` (default **`[` / `]`**) tilt the gimbal
- `gimbalResetBinding` (default **`\`**) resets pitch to 0°

- Pitch range: -90 to +10 degrees (configurable on `DroneGimbalCameraRig`)
- Pitch speed and smoothing are configurable in the Inspector

The gimbal system accepts both absolute (`SetTargetPitch`) and incremental (`AdjustTargetPitch`) input, so it can later be driven by a controller wheel/dial or VR input.

### Horizon stabilization

The onboard camera partially decouples from the drone body's roll and pitch:
- `rollStabilization` (default 0.85): 85% of body roll is removed, keeping the horizon readable
- `pitchStabilization` (default 0.9): 90% of body pitch is removed from the camera view (gimbal pitch input is added on top)

### VR controller screen hookup (future)

The `DroneVideoFeed` component always maintains a live `RenderTexture` of the onboard camera, even in Chase mode. To display this on a VR controller screen:

1. Add a `DroneFeedDisplaySurface` component to the controller screen mesh (Quad/plane).
2. Assign or let it auto-find the `DroneVideoFeed`.
3. The feed texture is automatically applied to the mesh material each frame.
4. For UI-based screens, add a `RawImage` component and the surface will bind to that instead.

Alternatively, access `DroneVideoFeed.FeedTexture` directly and assign it to any material: `material.mainTexture = feed.FeedTexture;`

### Default live world display demo (now included)

The vertical-slice bootstrap now creates a default in-world screen named **`VRControllerScreenPlaceholder`** (under **`DemoDisplays`**) when no display surface is already authored in the scene.

- The object uses `DroneFeedDisplaySurface`.
- It binds to the same `DroneVideoFeed.FeedTexture` used by the onboard camera pipeline.
- It remains live in both Chase and FPV modes.
- This is the intended placeholder path for a future handheld VR controller screen: replace/move this object onto a controller model and keep the same component hookup.

### Camera/feed debug status overlay

A lightweight `DroneCameraFeedDebugOverlay` is now created by bootstrap (if missing) for camera/feed validation.

It displays:
- Camera mode (Chase/FPV)
- Gimbal pitch
- Onboard FOV
- Feed resolution
- Feed live/valid status
- Whether onboard camera is bound to the RenderTexture
- Whether the world display surface is currently bound

You can disable it by unchecking `Show Overlay` on the `DroneCameraFeedDebugOverlay` component.

### Chase vs FPV behavior

- **Chase mode**: main camera runs `SimpleFollowCamera`; onboard camera still renders continuously to `DroneVideoFeed.FeedTexture`.
- **FPV mode**: `SimpleFollowCamera` is disabled, and main camera is positioned/rotated to match onboard camera each frame.
- In both modes, the onboard camera remains bound to the same persistent `RenderTexture`, so feed consumers (mesh screen, RawImage, future VR controller display) keep working.

## Input System / Controller Mapping

- Flight controls use Unity's **Input System** at runtime.
- Runtime stick/mode input is read by `Assets/Scripts/Drone/Input/DroneInputReader.cs`.
- Core flight input is not driven primarily by a generated `.inputactions` asset; `DroneInputReader` creates `InputAction`s in code from `DroneInputConfig`.
- The primary binding/config source is `DroneInputConfig`, typically loaded from `Assets/Resources/Configs/DroneInputConfig.asset`.
- The current intended stick layout is:
  - left stick = throttle / yaw
  - right stick = roll / pitch
- For joystick x/y axes, runtime actions now support both binding path styles for compatibility:
  - `<Joystick>/x` and `<Joystick>/y`
  - `<Joystick>/stick/x` and `<Joystick>/stick/y`
- This compatibility is bidirectional: whichever style is configured, the alternate style is also bound at runtime.
- `Assets/Scripts/Drone/Benchmark/BenchmarkRunner.cs` intentionally uses `LegacyInput` (`UnityEngine.Input`) only for benchmark keyboard hotkeys (for example `F7`/`F8`).

Mode switching defaults:
- `1` = Cine
- `2` = Normal
- `3` = Sport

Keyboard/gamepad fallback bindings are included for editor testing.

### Troubleshooting: stick moves in Input Debugger but drone does not respond

1. Confirm the stick path shown in Input Debugger (for example `/NATIONS RADIOMASTER SIM/stick/x`).
2. Verify the corresponding `DroneInputConfig` roll/pitch/yaw/throttle binding points to the expected axis family (`x/y` vs `stick/x`/`stick/y`).
3. Since `DroneInputReader` now binds both joystick path styles for x/y, mismatch between those two styles should no longer block input.
4. If movement still fails, check deadzone/invert values in `DroneInputConfig` and ensure `DroneInputReader` is assigned on the drone prefab/scene object.


### Input System backend configuration

The project uses **Input System** for flight/camera/gimbal controls. Legacy `UnityEngine.Input` is intentionally retained only for benchmark-only hotkeys in `BenchmarkRunner`. `ProjectSettings/ProjectSettings.asset` sets `activeInputHandler: 2` (Both) so startup warnings remain resolved while benchmark hotkeys continue working.

**If the "new input system package but native platform backends are not enabled" popup reappears:**
1. Check that `ProjectSettings/ProjectSettings.asset` exists and contains `activeInputHandler: 2`.
2. If the file was deleted or overwritten by Unity, re-add the setting or use **Edit > Project Settings > Player > Other Settings > Active Input Handling** and select "Both".

### Namespace collision notes

The `DroneSim.Drone.Camera` namespace conflicts with `UnityEngine.Camera` when both are imported. Scripts that need both use type aliases:

- `VerticalSliceBootstrap.cs`: `using UnityCamera = UnityEngine.Camera;` — all bare `Camera` references use `UnityCamera`
- `BenchmarkRunner.cs`: `using LegacyInput = UnityEngine.Input;` — avoids collision with `DroneSim.Drone.Input` namespace
- Camera scripts in `DroneSim.Drone.Camera` namespace use `UnityEngine.Camera` explicitly where needed

**If a camera namespace collision reappears in a new script:**
Add `using UnityCamera = UnityEngine.Camera;` at the top and use `UnityCamera` instead of bare `Camera`.

## Camera/FPV troubleshooting

- **Black feed texture (mesh/UI screen is black)**
  1. Confirm `DroneVideoFeed` exists and references the same `DroneGimbalCameraRig`.
  2. Confirm `DroneGimbalCameraRig.OnboardCamera` exists and is enabled.
  3. Confirm a valid `RenderTexture` is present in `DroneVideoFeed.FeedTexture`.
  4. Confirm the display material/property or `RawImage` is bound by `DroneFeedDisplaySurface`.

- **FPV works, but controller/world display surface does not**
  1. Ensure the display object has `DroneFeedDisplaySurface`.
  2. Ensure it points to the active `DroneVideoFeed` (or let it auto-find).
  3. For mesh displays, confirm the shader property name (default `_MainTex`) matches your shader.
  4. For UI displays, confirm `RawImage` exists and is assigned.

- **Feed resolution is wrong**
  1. Check `DroneVideoFeed` `feedWidth` / `feedHeight`.
  2. Confirm the debug overlay shows the expected resolution at runtime.
  3. If updated at runtime, call `DroneVideoFeed.SetResolution(width, height)` so the RenderTexture is recreated.

- **Camera mode switches, but gimbal pitch seems stuck**
  1. Check camera/gimbal bindings in `DroneInputConfig` (`gimbalTiltDownBinding`, `gimbalTiltUpBinding`, `gimbalResetBinding`).
  2. Verify `DroneCameraModeController` has `gimbalPitchRate > 0`.
  3. Verify `DroneGimbalCameraRig` pitch limits include your expected range and `pitchSpeed`/`pitchSmoothing` are not overly restrictive.
  4. Watch `Gimbal Pitch` in `DroneCameraFeedDebugOverlay` while pressing tilt inputs.

- **Right stick lights up in Input Debugger, but roll/pitch do not respond**
  1. Check `DroneInputConfig` roll/pitch bindings.
  2. `DroneInputReader` binds both `<Joystick>/x|y` and `<Joystick>/stick/x|y`, so either naming style should work.
  3. Check deadzone/expo/invert settings and that the active scene drone uses the expected `DroneInputConfig`.

- **Input System popup returns on startup**
  1. Open `ProjectSettings/ProjectSettings.asset`.
  2. Verify `activeInputHandler: 2`.
  3. If Unity reset it, set **Player > Active Input Handling** back to **Both**.

- **Camera namespace collision reappears**
  1. Add alias: `using UnityCamera = UnityEngine.Camera;`
  2. Use `UnityCamera` for all camera references in that script.

## Raw joystick diagnostics overlay (temporary input debugging)

A runtime diagnostics overlay is now available to inspect what Unity Input System reports from your radio/joystick without changing flight behavior.

How to use:
1. Open `Assets/Scenes/DroneTrainingVerticalSlice.unity` and press Play.
2. The bootstrap creates `RawJoystickDiagnostics` automatically if it is missing.
3. Toggle the overlay with **BackQuote** ( ` ) or **F1**.
4. Move one stick axis at a time and watch:
   - control name
   - live value
   - exact Input System path (for example `<Joystick>/x`)
5. Axes above the configured threshold are highlighted, making it easier to identify active controls.

Inspector options on `RawJoystickDiagnosticsOverlay`:
- `Activity Threshold`: ignores tiny jitter and controls highlight/log behavior.
- `Show Only Changing Controls`: shows only currently active axes/buttons.
- `Log Moving Axes`: writes active axis path/value to Console with throttling.

Tip for mapping Mode 2 sticks:
- Move only one stick direction at a time and note which control path changes strongly (typically near ±1.0).
- Record the four paths for: left horizontal, left vertical, right horizontal, right vertical.

## Input deadzone note

A practical deadzone is often around **0.12-0.18** depending on radio jitter and calibration quality.

## Where to start reading (new developer)

1. `ARCHITECTURE.md`
2. `Assets/Scripts/Drone/Bootstrap/VerticalSliceBootstrap.cs`
3. `Assets/Scripts/Drone/Flight/DJIStyleFlightController.cs`
4. `Assets/Scripts/Drone/Flight/DroneFlightModeConfig.cs`
5. `TUNING_GUIDE.md`

## Additional docs

- `ARCHITECTURE.md` — plain-English architecture overview and tuning map.
- `TUNING_GUIDE.md` — practical feel tuning workflow for Cine/Normal/Sport.
- `Docs/VerticalSlice.md` — original vertical-slice notes.
