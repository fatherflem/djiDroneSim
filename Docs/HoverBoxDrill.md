# Hover Box Drill

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
