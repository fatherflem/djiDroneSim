# VR Test Checklist (Stationary Normal-Mode Prototype, Quest 3 Target)

## Milestone scope (what this build is)
Target headset: **Meta Quest 3** (Quest 3 family devices where behavior is equivalent).
- Drone: **DJI Mini 5 Pro**.

This milestone is a **practical in-headset test slice** for the Normal-mode training prototype:
- stationary pilot stance,
- no virtual hands,
- virtual RC visible in pilot space,
- live onboard drone feed on RC screen,
- RC stick visuals react to live input,
- drone still flies with existing Normal-mode behavior.

Not included yet:
- tracked physical-controller calibration UI,
- hand interaction UX,
- locomotion/teleport/snap-turn,
- Cine/Sport VR-specific tuning passes.

## One-time Unity setup checks (target machine)
1. Open **Project Settings > XR Plug-in Management**.
2. For your active build target, ensure **OpenXR** loader is enabled.
3. Open **Project Settings > XR Plug-in Management > OpenXR** and enable the required interaction/runtime features for **Meta Quest 3** runtime usage.
4. Confirm Input System backend remains enabled (project currently uses `activeInputHandler: Both`).
5. Confirm XR Interaction Toolkit package is present in Package Manager.
6. Reopen Unity once if package/XR backend prompts appear.

## Manual in-headset validation steps
1. Open scene: `Assets/Scenes/VR/VRPilotScene.unity`.
2. Press Play while headset runtime is active.
3. Confirm head tracking works (camera follows HMD).
4. Confirm you remain stationary (no locomotion providers present).
5. Confirm no hand meshes/rays are visible.
6. Look down and verify virtual RC appears in front of chest space.
7. Verify RC screen shows live drone camera feed.
8. Move physical controller sticks and verify RC stick meshes animate.
9. Fly drone and confirm Normal-mode behavior is unchanged.
10. Confirm the pilot feels aligned to the operator placeholder stance (no obvious offset between expected chest/controller relationship and headset view).

## Success criteria
A run is considered successful if all ten checks above pass in one play session without manual scene rewiring.

## Quick troubleshooting (check these 6 first)
1. **No headset tracking**
   - Check XR Plug-in Management/OpenXR loader enabled for current build target.
2. **RC not visible**
   - Confirm `VirtualDJIRC` exists in Play mode and either `PlaceholderTrackedPropPoseProvider` or `AnchoredControllerPoseProvider` is active.
3. **RC appears detached from pilot body concept**
   - Confirm `VRUserPlaceholder` exists and has `ControllerPropAnchor`; verify `VRPilotBootstrap` alignment is enabled.
4. **RC screen dark**
   - Check Console for `DroneScreenFeedBridge` warning, then verify `DroneVideoFeed` exists on drone camera rig.
5. **Sticks not animating**
   - Confirm `DroneInputReader` is present and receiving input from `DroneInputConfig` bindings.
6. **Drone not flying as expected**
   - Verify scene is VRPilotScene (not another test scene) and mode remains Normal.

## Long-term direction (unchanged)
- Preserve stationary pilot philosophy with real physical controller.
- Keep virtual RC + live feed as core in-headset reference.
- Add tracked physical-controller calibration later.
- Add hardware-specific pose integration and polish only after this test loop is stable.

## Physical-space and comfort assumptions (stationary Quest 3 use)
- User is expected to remain stationary (seated or standing in-place).
- Ensure a clear local area around the user even though locomotion is disabled.
- Prefer short validation sessions first to check comfort and RC readability.
- Treat RC placement/pose comfort issues as tuning tasks, not invitations to add full locomotion/interaction systems.
