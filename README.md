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
