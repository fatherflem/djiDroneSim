# VR Roadmap and Milestone V1 (Stationary Pilot)

## North-star
Build a stationary VR drone-training setup where the user holds a real physical controller and sees a matching virtual DJI-RC with live drone camera feed.

## V1 delivered in this repo
Scene entry point:
- `Assets/Scenes/VR/VRPilotScene.unity`

Runtime bootstrap:
- `Assets/Scripts/VR/VRPilotBootstrap.cs`

Key behaviors:
1. XR shell: runtime `XROrigin` + headset camera only.
2. Stationary experience: no locomotion systems are created.
3. Hands-free presentation: no virtual hand models, rays, or grab interactions.
4. Virtual RC presence: procedural DJI-RC-style body always visible in pilot space.
5. Live screen: drone onboard feed via `DroneVideoFeed` -> `DroneFeedDisplaySurface` on RC screen.
6. Input-reactive RC sticks: left/right stick transforms animate from `DroneInputReader.CurrentInput`.

## Pose architecture for future tracked physical prop
Interface:
- `IControllerPoseProvider`

Current providers:
- `AnchoredControllerPoseProvider` (active fallback): positions RC relative to headset/chest with offset.
- `PlaceholderTrackedPropPoseProvider` (not fully integrated hardware tracking): returns pose from a future tracked prop transform + calibration offsets.

Resolver behavior in `VirtualRCControllerRig`:
- Prefer tracked provider if available and valid.
- Fall back to anchored provider otherwise.

## Calibration path (future)
Planned flow:
1. Acquire tracked prop pose source from target hardware SDK/OpenXR extension.
2. Capture user-confirmed alignment pose while holding real RC.
3. Compute and persist calibration offsets (position + rotation) into tracked provider.
4. Apply offsets every frame before driving virtual RC transform.

## What V1 explicitly does not do
- No full tracked physical-controller alignment workflow.
- No hand-interaction model (grab/press).
- No locomotion / teleport / snap turn.
- No Cine or Sport VR-specific behavior work.
- No retuning of Normal flight model.

## Manual setup and playtest checklist
1. Open `Assets/Scenes/VR/VRPilotScene.unity`.
2. Confirm XR packages are resolved (`com.unity.xr.management`, `com.unity.xr.openxr`, `com.unity.xr.core-utils`).
3. Enter Play mode with headset active.
4. Verify HMD head tracking drives camera pose.
5. Verify there is no teleport/snap-turn/hand rendering.
6. Look down: confirm virtual RC is visible and stable.
7. Confirm RC screen shows live drone feed.
8. Move physical controller sticks: confirm virtual RC sticks animate.
9. Fly drone: confirm existing Normal-mode behavior remains active.
