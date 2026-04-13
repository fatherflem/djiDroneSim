# VR Roadmap and Milestones (Stationary Pilot, Quest 3 Target)

## Hardware target and design stance
- Primary target hardware: **Meta Quest 3** (Quest 3 family devices where equivalent behavior applies).
- Design stance: stationary pilot using a real physical controller, no VR locomotion, no virtual-hand dependency.

## Project-state context
- Normal mode benchmark tuning is currently **frozen for now** (PATH A) as a stable baseline.
- VR implementation/testing is now the active frontier.

## Current delivered slice (Phase 1 prototype baseline)
Scene entry point:
- `Assets/Scenes/VR/VRPilotScene.unity`

Runtime bootstrap:
- `Assets/Scripts/VR/VRPilotBootstrap.cs`

What works now:
1. XR shell with runtime `XROrigin` + headset camera.
2. Stationary pilot setup (no locomotion systems created).
3. No hands/rays/grab interactions by design.
4. Virtual DJI-RC-style controller visible in pilot space.
5. Live drone onboard feed on RC screen (`DroneVideoFeed` -> `DroneFeedDisplaySurface`).
6. Input-reactive RC stick visuals from `DroneInputReader.CurrentInput`.
7. Startup guardrails for first-run testability (retry wiring, duplicate-rig avoidance, simple floor/light context).

## What is not done yet (explicit limitations)
- No true tracked physical-controller alignment pipeline end-to-end.
- No completed user calibration flow for real RC ↔ virtual RC alignment.
- No finalized production RC art/model polish pass.
- No polished one-click Quest 3 deployment workflow; OpenXR setup is still partly manual.

## Pose architecture for tracked physical prop (future)
Interface:
- `IControllerPoseProvider`

Current providers:
- `AnchoredControllerPoseProvider` (active fallback; headset/chest-relative placement)
- `PlaceholderTrackedPropPoseProvider` (future integration hook + offsets)

Resolver behavior in `VirtualRCControllerRig`:
- Prefer tracked provider if available and valid.
- Fall back to anchored provider otherwise.

## Milestone phases

### Phase 1 — Headset-testable prototype (current)
- XR shell works.
- Stationary user.
- No hands.
- Virtual RC visible.
- Live screen visible.
- Input visible/connected.
- Manual Meta Quest 3 testing path documented.

### Phase 2 — Better physical-controller alignment
- Tracked prop strategy clarified for Quest 3-compatible workflow.
- Calibration workflow for real-controller alignment.
- RC pose refinement.
- Comfort/usability improvements on Quest 3.

### Phase 3 — Training-ready VR experience
- Better RC visuals.
- Stable headset workflow.
- Better environment grounding.
- Performance/polish pass for Quest 3.
- Clear operator workflow for training sessions.

## Practical OpenXR setup status
Committed in repo:
- `Packages/manifest.json` includes `com.unity.xr.management`, `com.unity.xr.openxr`, `com.unity.xr.core-utils`.

Still local/manual:
- Per-platform XR Plug-in Management toggles and OpenXR feature checkboxes may require one-time Unity Project Settings confirmation on each target machine.

Use `Docs/VRTestChecklist.md` as the runbook for Quest 3 bring-up and regression checks.

## Intentionally postponed
- Additional Normal-mode tuning unless explicit reopen gates are met.
- Hand interaction UX and locomotion systems (outside current stationary pilot scope).
