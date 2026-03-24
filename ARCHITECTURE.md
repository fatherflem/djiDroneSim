# DJI Drone Simulator Architecture (Clarity Pass)

This project is a **Unity 6, Rigidbody-based, DJI-style stabilized drone simulator** for desktop training.

It is intentionally **not** a per-propeller flight model and **not** an FPV acro simulator.

## Core data flow

`InputReader -> FlightController -> PhysicsBody -> VisualRig -> Training/UI`

1. **DroneInputConfig (ScriptableObject)**
   - Stores input bindings and filtering settings (deadzone, expo, smoothing, inversion).
2. **DroneInputReader (MonoBehaviour)**
   - Reads input each frame from Unity Input System and builds a normalized input frame.
3. **DJIStyleFlightController (MonoBehaviour)**
   - Converts pilot input + selected mode (Cine/Normal/Sport) into acceleration and yaw commands.
4. **DronePhysicsBody (MonoBehaviour)**
   - Applies those commands to the Rigidbody and reports telemetry-friendly state.
5. **DroneVisualRig (MonoBehaviour)**
   - Provides a tilt root and simple generated visuals for quick iteration.
6. **SimpleTrainingScenario + TelemetryRecorder + DroneDebugHUD**
   - Tracks drill progress, stores telemetry samples, and displays live debug info.
7. **VerticalSliceBootstrap (MonoBehaviour)**
   - Wires everything together at startup so the vertical slice runs quickly in a clean scene.

## Main scripts and responsibilities

- `Assets/Scripts/Drone/Input/DroneInputConfig.cs`
  - Input device bindings and stick filtering parameters.
- `Assets/Scripts/Drone/Input/DroneInputReader.cs`
  - Builds a clean, smoothed input frame and mode requests.
- `Assets/Scripts/Drone/Flight/DroneFlightModeConfig.cs`
  - Mode-specific tuning limits for movement, yaw, and visual tilt.
- `Assets/Scripts/Drone/Flight/DJIStyleFlightController.cs`
  - Stabilized controller logic (velocity-hold style + active braking + yaw response).
- `Assets/Scripts/Drone/Physics/DronePhysicsBody.cs`
  - Rigidbody wrapper: apply acceleration/yaw + expose readable speed/altitude values.
- `Assets/Scripts/Drone/Flight/DroneVisualRig.cs`
  - Creates simple placeholder drone body/arms/rotors and tilt hierarchy.
- `Assets/Scripts/Drone/Training/SimpleTrainingScenario.cs`
  - Hover-box drill rules and completion progress.
- `Assets/Scripts/Drone/Training/TelemetryRecorder.cs`
  - Periodically records position/velocity/yaw/mode samples.
- `Assets/Scripts/Drone/UI/DroneDebugHUD.cs`
  - OnGUI debug overlay for inputs, flight state, and training metrics.
- `Assets/Scripts/Drone/Bootstrap/VerticalSliceBootstrap.cs`
  - Runtime setup for environment + drone + camera + HUD.

## Where to tune what

### 1) Stick feel / input noise
Tune in **DroneInputConfig**:
- `stickDeadzone`
- `stickExpo`
- `inputSmoothing`
- `invertPitch`, `invertThrottle`

### 2) Flight feel per mode (Cine / Normal / Sport)
Tune in each **DroneFlightModeConfig** asset:
- `maxHorizontalSpeed`
- `horizontalAcceleration`
- `horizontalStopStrength`
- `maxVerticalSpeed`
- `verticalAcceleration`
- `maxYawRateDegrees`
- `yawCatchUpSpeed`
- `tiltLimitDegrees`
- `tiltSmoothing`

### 3) Global assist feel
Tune in **DJIStyleFlightController**:
- `gravityCancelMultiplier`
- `globalHorizontalAccelLimit`
- `globalVerticalAccelLimit`
- `brakingInputDeadband`

### 4) Training drill behavior
Tune in **SimpleTrainingScenario**:
- hover box center/size
- target altitude / tolerance
- required hover time
- speed threshold to count as stable hover

---

If you are new to this repo, start with:
1. `VerticalSliceBootstrap`
2. `DJIStyleFlightController`
3. `DroneFlightModeConfig` assets under `Assets/Resources/Configs/`
4. `DroneDebugHUD` while testing in Play mode.
