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

- **`[` / `]`** keys: tilt gimbal down / up
- **Mouse scroll wheel**: tilt gimbal
- **`\`** key: reset gimbal to 0 degrees (forward)
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

The project uses **both** the new Input System (for flight controls) and legacy `UnityEngine.Input` (for benchmark hotkeys and camera gimbal keyboard controls). The `ProjectSettings/ProjectSettings.asset` file sets `activeInputHandler: 2` (Both) so that Unity enables both backends on startup.

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
