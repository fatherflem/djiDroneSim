# Hover Box Drill

## Role in the training system
Hover Box is the reference implementation for reusable training drills. `HoverBoxDrill` inherits the common `TrainingDrill` lifecycle and uses `Resources/Training/HoverBoxDrillDefinition` for its student-facing name, instructions, countdown, optional time limit, and restart policy.

The common lifecycle is:

`NotStarted → Instructions → Countdown → Running → Completed/Failed → Results`

Desktop and VR presenters read the same drill instance; neither UI owns waypoint evaluation or scoring. Future drills should reuse the lifecycle and `TrainingResult`, while keeping their small, drill-specific evaluation logic in their own component.

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
A completed run reports elapsed time and a 0–100 coaching score. The score starts at 100 and deducts two points per second outside the safety envelope and two points per interrupted hold. These deductions do not alter waypoint pass criteria or flight-model behavior.

## Difficulty Tuning
For first-time students, start easier:
- `waypointRadius = 1.0`
- `requiredHoldSeconds = 1.0`

Then tighten to default values when students are consistent.

## Desktop vs VR
- Desktop scene (`Assets/Scenes/HoverBoxDrill.unity`) uses top-left overlay UI with waypoint, hold bar, live stability metrics, and completion count.
- VR scene (`Assets/Scenes/VR/HoverBoxDrillVR.unity`) uses spatial feedback: ring progress on the active waypoint and a small RC-mounted status panel.

## VR Notes
In-headset primary feedback channels are:
1. Active waypoint ring fill/color shift (yellow → green) for hold progress.
2. RC-mounted panel for waypoint, progress count, and status (`Stable` / `Drifting` / `Out of bounds`).
3. World-space red out-of-bounds warning near the boundary-crossing location.
