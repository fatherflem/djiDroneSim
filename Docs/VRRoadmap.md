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
7. Test-readiness guardrails: bootstrap retries fragile references (feed/input), avoids duplicate rig creation, and adds a minimal floor/light context for first-run headset validation.

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

## Practical OpenXR setup status

Committed in repo:
- `Packages/manifest.json` includes:
  - `com.unity.xr.management`
  - `com.unity.xr.openxr`
  - `com.unity.xr.core-utils`

Not reliably commit-safe in this environment:
- Per-platform XR Plug-in Management toggles and OpenXR interaction profile feature checkboxes are typically stored in additional Unity-generated project setting assets that may vary by Unity install/platform target.

Therefore:
- Treat first open on target hardware as requiring a one-time Project Settings verification (see `Docs/VRTestChecklist.md`).
- Do not assume headset tracking will run until those checks are confirmed locally in Unity.

## Manual setup and playtest checklist
Use `Docs/VRTestChecklist.md` as the authoritative current checklist.
