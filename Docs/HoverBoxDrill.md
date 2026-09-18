# Hover Box Drill

## Role in the training system
Hover Box is the reference implementation for reusable training drills. `HoverBoxDrill` inherits the common `TrainingDrill` lifecycle and uses `Resources/Training/HoverBoxDrillDefinition` for its student-facing name, instructions, countdown, optional time limit, and restart policy.

The common lifecycle is:

`NotStarted → Instructions → Countdown → Running → Completed/Failed → Results`

Desktop and VR presenters read the same drill instance; neither UI owns waypoint evaluation or scoring. Future drills should reuse the lifecycle and `TrainingResult`, while keeping their small, drill-specific evaluation logic in their own component.

`Completed` and `Failed` are observable immediate terminal transitions. The drill publishes its result and then enters `Results`; result presentation and retry are driven only from `Results`, so the state is not a dead-end or presenter-specific convention.

## Runtime wiring and attempt reset
Both committed Hover Box scenes contain `HoverBoxDrillSceneBootstrap`. It runs after the desktop or VR flight-stack bootstrap, loads the shared definition from `Resources`, creates the drill and appropriate presenter once, and reports a single clear error if the definition or initialized aircraft is missing. Desktop also creates exactly one `EventSystem` with an `InputSystemUIInputModule` and default UI actions when the scene does not already provide one.

`TrainingAircraftReset` owns the scene-configured start pose. Instructions and countdown lock the existing `DroneInputReader` to a neutral external frame and make the Rigidbody kinematic. Motion is cleared before that transition; pose-only resets never write velocity while kinematic. Entering `Running` restores the pose, original dynamic/collision state, and zero velocity before releasing input. Results stop motion before locking the aircraft, and Retry clears common/drill state, restores the aircraft, and returns to Instructions.

Desktop Start/Retry remain normal clickable UI buttons. Enter or Space also starts, and R retries. In VR the RC workflow uses gamepad button South (A/Cross) for Start/Retry; keyboard controls remain available for development. No hand, ray, or locomotion interaction system is required.

## Purpose
The Hover Box drill teaches controlled, precise hovering in Normal mode. Completion requires stability, not speed.

## Waypoint Layout
Square path at 2m altitude, 5m edges:

A (0,0) ---- B (5,0)
  |            |
  |            |
D (0,5) ---- C (5,5)

Path: A → B → C → D → A

## Hold Requirements
To complete each waypoint hit, the drone must remain inside the waypoint cylinder and hold for 2.0 continuous seconds while meeting:
- Horizontal speed ≤ 0.3 m/s
- Vertical speed ≤ 0.3 m/s
- Yaw rate ≤ 10°/s

Leaving the cylinder or exceeding any threshold resets the hold timer.

## Result Metrics
A completed run reports elapsed time and a 0–100 coaching score. The score starts at 100 and deducts two points per second outside the safety envelope and two points per interrupted hold. These deductions do not alter waypoint pass criteria or flight-model behavior. `TrainingResult.metrics` also exposes `waypointsCompleted`, `outOfBoundsSeconds`, and `interruptedHolds` as keyed numeric records with labels/units; consumers never need to parse the prose summary.

## Difficulty Tuning
For first-time students, start easier:
- `waypointRadius = 1.0`
- `requiredHoldSeconds = 1.0`

Then tighten to default values when students are consistent.

## Desktop vs VR
- Desktop scene (`Assets/Scenes/HoverBoxDrill.unity`) uses top-left overlay UI with waypoint, hold bar, live stability metrics, and completion count.
- VR scene (`Assets/Scenes/VR/HoverBoxDrillVR.unity`) uses spatial feedback: ring progress on the active waypoint and a small RC-mounted status panel.
- Both presentations show the authored `TrainingDrillDefinition.instructions` and suppress stability/timing observations until the drill is actually running.

## Manual Play Mode verification
Unity 6.3 (`6000.3.17f1`) smoke testing exposed and prompted repairs for a missing configured runtime URP shader, an invalid angular-velocity write while the aircraft was kinematic, the unreadable desktop HUD, and waypoint visibility/URP material handling. The shader cache now self-repairs package shader references, while the desktop panel and unlit markers are intentionally high contrast.

1. Open `Assets/Scenes/HoverBoxDrill.unity`, enter Play Mode, and confirm one drill, one canvas, and one EventSystem exist with no repeated console errors.
2. Confirm the authored instructions are readable. Attempt flight input and verify the aircraft remains at `(0, 1.25, -4)`.
3. Click **Start Drill**. Verify the three-second countdown, neutral controls, and zero motion; verify controls release only when `Running` begins.
4. Enter a waypoint, interrupt one hold by leaving/re-entering, and complete the hold. Confirm progression and the interrupted-hold count.
5. Leave/re-enter the safety envelope and confirm the warning and accumulated out-of-bounds duration.
6. Complete A → B → C → D → A. Confirm `Completed → Results`, summary, score, and all three structured metrics in the Inspector/debugger.
7. Click **Retry Drill**. Confirm Instructions, cleared result/timers/metrics, restored transform, and zero linear/angular velocity; start a second attempt without reloading.
8. Repeat the presentation checks in `Assets/Scenes/VR/HoverBoxDrillVR.unity`; confirm authored instructions and the configured controller Start/Retry action.

## Tomorrow's First Run

Open `Assets/Scenes/HoverBoxDrill.unity` first. In Play Mode, expect one placeholder field, one five-point Hover Box path, authored Instructions, a locked three-second Countdown, Running telemetry, and automatic Results. Start with Enter, Space, or the desktop button; retry with R or the desktop button. Gamepad South is also bound for Start/Retry. A generic RadioMaster/Joystick button is deliberately **not guessed**: set the scene bootstrap's `genericJoystickButtonControlPath` to the verified Input System path (for example, `<Joystick>/buttonN`) after identifying the transmitter button, and the same button will provide edge-triggered Start/Retry without touching flight axes. If startup fails, read the first Hover Box error in the Console and verify the scene's initialized drone, drill definition, and field asset; do not add a fallback drone. The release remains at the vertical-slice hardening stage described in `Docs/RELEASE_1_0_PLAN.md`, with Unity Play Mode and physical-controller verification still outstanding.

## VR Notes
In-headset primary feedback channels are:
1. Active waypoint ring fill/color shift (yellow → green) for hold progress.
2. RC-mounted panel for waypoint, progress count, and status (`Stable` / `Drifting` / `Out of bounds`).
3. World-space red out-of-bounds warning near the boundary-crossing location.
