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

## Camera system

`DroneCameraModeController -> DroneGimbalCameraRig -> DroneVideoFeed -> DroneFeedDisplaySurface`

8. **DroneGimbalCameraRig (MonoBehaviour)**
   - Drone-mounted onboard camera with gimbal pitch pivot and horizon stabilization.
   - Exposes `SetTargetPitch()` / `AdjustTargetPitch()` for external gimbal control.
9. **DroneVideoFeed (MonoBehaviour)**
   - Manages a RenderTexture that captures the onboard camera output.
   - Feed is always live regardless of active camera mode (for VR controller screen use).
10. **DroneCameraModeController (MonoBehaviour)**
    - Switches between Chase and FPV modes.
    - Uses Unity Input System actions (from `DroneInputConfig` camera/gimbal bindings) for mode toggle and gimbal pitch controls.
    - Only controls player presentation camera state; does not unbind/disable feed generation.
11. **DroneFeedDisplaySurface (MonoBehaviour)**
    - Binds the feed texture to a mesh Renderer or UI RawImage for display.

### Camera scripts

- `Assets/Scripts/Drone/Camera/DroneGimbalCameraRig.cs`
  - Onboard camera, gimbal pitch, stabilization.
- `Assets/Scripts/Drone/Camera/DroneVideoFeed.cs`
  - RenderTexture management for the onboard camera feed.
- `Assets/Scripts/Drone/Camera/DroneCameraModeController.cs`
  - Chase/FPV presentation switching and FPV presentation-camera syncing from onboard camera.
- `Assets/Scripts/Drone/Camera/DroneFeedDisplaySurface.cs`
  - Display surface binding for mesh or UI targets.

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

### 5) Camera and gimbal
Tune in **DroneGimbalCameraRig**:
- `minPitchDegrees` / `maxPitchDegrees`
- `pitchSpeed` / `pitchSmoothing`
- `rollStabilization` / `pitchStabilization`
- `fieldOfView`
- `cameraLocalOffset`

Tune in **DroneCameraModeController**:
- `startupMode` (Chase or FPV)
- `gimbalPitchRate`
- fallback camera/gimbal Input System binding strings (used if `DroneInputConfig` is not assigned)

Tune in **DroneInputConfig** (camera/gimbal actions):
- `cameraToggleBinding` (default `V`)
- `gimbalTiltDownBinding` / `gimbalTiltUpBinding` (default `[` / `]`)
- `gimbalResetBinding` (default `\`)

---

If you are new to this repo, start with:
1. `VerticalSliceBootstrap`
2. `DJIStyleFlightController`
3. `DroneFlightModeConfig` assets under `Assets/Resources/Configs/`
4. `DroneDebugHUD` while testing in Play mode.
