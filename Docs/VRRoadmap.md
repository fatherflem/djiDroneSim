# VR Roadmap and Milestones (Stationary Pilot, Quest 3 Target)

## Hardware target and design stance
- Primary target hardware: **Meta Quest 3** (Quest 3 family devices where equivalent behavior applies).
- Drone: **DJI Mini 5 Pro**.
- Design stance: stationary pilot using a real physical controller, no VR locomotion, no virtual-hand dependency.

## Project-state context
- Normal mode benchmark tuning is currently **frozen for now** (PATH A) as a stable baseline.
- VR implementation/testing is now the active frontier.

## Current delivered slice (VR integration pass)
Scene entry point:
- `Assets/Scenes/VR/VRPilotScene.unity`

Runtime bootstrap:
- `Assets/Scripts/VR/VRPilotBootstrap.cs`

XR/operator/controller bridge scripts:
- `Assets/Scripts/Drone/Bootstrap/VRUserPlaceholder.cs`
- `Assets/Scripts/VR/AnchoredControllerPoseProvider.cs`
- `Assets/Scripts/VR/PlaceholderTrackedPropPoseProvider.cs`
- `Assets/Scripts/VR/VirtualRCControllerRig.cs`
- `Assets/Scripts/VR/DroneScreenFeedBridge.cs`

What works now:
1. OpenXR-capable XR shell with runtime `XROrigin` + headset camera.
2. Stationary pilot setup (no locomotion systems created).
3. Operator placeholder remains source-of-truth for pilot staging and controller anchor context.
4. VR bootstrap aligns XR origin to operator placeholder orientation/head space.
5. Virtual DJI-RC-style controller remains visible in pilot space and follows operator placeholder controller anchor when available.
6. Controller pose has VR readability bias (small presentation offset + tilt) while keeping believable chest-relative placement.
7. Live drone onboard feed is shown on controller screen (`DroneVideoFeed` -> `DroneFeedDisplaySurface`).
8. Input-reactive RC stick visuals from `DroneInputReader.CurrentInput`.

## What is intentionally out of scope (still deferred)
- Full hand interaction UX (grabbing, finger pose, object manipulation).
- Locomotion systems (teleport, smooth move, snap-turn).
- Multiplayer/operator networking.
- Full physical-controller calibration UX pipeline.
- Production-grade RC art/model polish.

## Pose architecture for controller presentation
Interface:
- `IControllerPoseProvider`

Current providers:
- `PlaceholderTrackedPropPoseProvider` (preferred when `VRUserPlaceholder.ControllerPropAnchor` exists)
- `AnchoredControllerPoseProvider` (fallback chest/head-relative pose with readability bias)

Resolver behavior in `VirtualRCControllerRig`:
- Prefer tracked provider if available and valid.
- Fall back to anchored provider otherwise.

## Practical OpenXR setup status
Committed in repo:
- `Packages/manifest.json` includes `com.unity.xr.management`, `com.unity.xr.openxr`, `com.unity.xr.core-utils`, and `com.unity.xr.interaction.toolkit`.

Still local/manual:
- Per-platform XR Plug-in Management toggles and OpenXR feature checkboxes may require one-time Unity Project Settings confirmation on each target machine.

Use `Docs/VRTestChecklist.md` as the runbook for Quest 3 bring-up and regression checks.
