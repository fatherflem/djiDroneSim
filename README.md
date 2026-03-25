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

## Expected controller mapping (Mode 2)

Likely current axis mapping in `DroneInputConfig`:
- `throttleAxis = "z"` (`<Joystick>/z`)
- `yawAxis = "rx"` (`<Joystick>/rx`)
- `pitchAxis = "y"` (`<Joystick>/y`)
- `rollAxis = "x"` (`<Joystick>/x`)

Mode switching defaults:
- `1` = Cine
- `2` = Normal
- `3` = Sport

Keyboard/gamepad fallback bindings are included for editor testing.

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
