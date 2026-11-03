# Controller Setup and Calibration

Target hardware: **RadioMaster T8L** (USB joystick mode, **Mode 2**). Other USB radios that show up as a
standard joystick should work the same way.

## What the student sees

1. **Plug in the controller.** If this PC has no saved calibration for it, the **Controller Check** starts by itself.
2. **Hands off.** Let go of both sticks for about 1.5 seconds while the sim measures where they rest.
3. **Follow the arrows.** Push each stick to each end and hold it until the bar fills, letting go between
   steps. That's 8 moves: right stick right/left/up/down, then left stick right/left/up/down.
4. **Results.** The check saves on its own after a few seconds. Warnings and problems are listed.

While the check runs, the drone holds position and the drill's Start/Retry are ignored.

Desktop controls: **F4** starts or cancels a check at any time (instructors use this to recalibrate).
The panel also has **Save now**, **Redo** and **Cancel** buttons. In VR the same check shows on a small
panel above the virtual RC. It needs no buttons: it saves on its own, and if something fails it starts
over on its own after 8 seconds. The virtual RC sticks follow the physical ones during the check.

## What it detects

| Result | Meaning | What to do |
|---|---|---|
| Problem: *asked for X but Y moved* | Channels are swapped (wrong stick mode or channel order). Nothing is saved. | Set the radio to Mode 2 and check its channel order. |
| Problem: *moved the same way for both directions* | The student pushed the same way twice. | Redo, following the arrows. |
| Warning: *throttle doesn't spring back to center* | The throttle stick stays where it's left. DJI controllers center both sticks, so this teaches the wrong habit. | Switch this radio's throttle to self-centering. Calibration still works. |
| Warning: *reads reversed* | A channel reports the opposite direction. Calibration corrects it. | None, if the student followed the arrows. Otherwise Redo. |
| Warning: *limited travel* | A stick doesn't reach full range. | Set the radio's endpoints/weights to 100%. |
| Warning: *jitters at rest* | Noisy stick at center. | Raise `stickDeadzone` in `DroneInputConfig` if the drone creeps. |
| Note: *rests off center* | Small trim offset. Calibration corrects it. | None. |

Disconnects: if the USB controller is unplugged, a red banner appears and the sticks read zero, so the drone
holds position. Plugging it back in reloads its calibration.

## Where calibration lives

One JSON file per controller model per PC, under `Application.persistentDataPath/ControllerCalibration/`
(on Windows: `%USERPROFILE%\AppData\LocalLow\<company>\<product>\ControllerCalibration\`). Delete the file
to force a new check.

## How it fits the flight model

- Calibration maps each raw axis to [-1, 1] using the measured center and both ends of travel. It runs
  **before** deadzone, expo and `invertPitch`/`invertThrottle`, and it only applies to physical-joystick input.
- With no saved calibration the reader behaves exactly as before. Benchmarks feed input directly and never
  pass through calibration, so the frozen Normal-mode benchmark numbers are unaffected.

## Code map

| File | Role |
|---|---|
| `Assets/Scripts/Drone/Input/Calibration/Core/` | Pure C# core in its own assembly (`DroneSim.ControllerCalibration`): `AxisCalibration`, `ControllerCalibrationProfile`, `ControllerCheckFlow`. No Unity dependency. |
| `ControllerCalibrationStore.cs` | Saves and loads profiles as JSON. |
| `ControllerSetupService.cs` | Device connect/disconnect, applies calibration to `DroneInputReader`, runs the check. |
| `ControllerSetupDesktopUI.cs` | Desktop panel and status banner. |
| `Assets/Scripts/VR/ControllerSetupVRUI.cs` | VR panel above the virtual RC. |
| `Assets/Tests/EditMode/ControllerCalibrationTests.cs` | Unit tests for the core. |

Both presentations read the same service state. New drills or menus should use `ControllerSetupService`,
not read the joystick directly.

## Not yet verified on hardware

All of this was written without Unity or a T8L attached. Before relying on it, check:
- [ ] The project compiles in Unity 6.3 and the EditMode tests pass in the Test Runner.
- [ ] The T8L's product name is picked up (shown under the panel title) and calibration saves and reloads.
- [ ] The full 9-step check completes on the T8L, and the dots track the sticks in the right directions.
- [ ] Unplugging mid-flight shows the banner and the drone holds position. Replugging resumes control.
- [ ] VR: the panel sits at a readable size and position above the virtual RC (`panelLocalPosition`/`panelLocalEuler` in the inspector).
