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

## Quick start (test scene / bootstrap)

1. Open the project in **Unity 6**.
2. Open scene: `Assets/Scenes/DroneTrainingVerticalSlice.unity`.
3. Press Play.
4. The `VerticalSliceBootstrap` script creates/initializes the training environment, drone systems, and debug HUD.

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
