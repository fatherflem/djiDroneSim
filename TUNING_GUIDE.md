# DJI Drone Sim Tuning Guide (Unity 6)

This guide is for practical "feel" tuning of the current stabilized DJI-style controller.

## Start here: highest-impact parameters

Tune these first (in this order):

1. `stickDeadzone` (DroneInputConfig)
2. `stickExpo` (DroneInputConfig)
3. `maxForwardSpeed` (mode config)
4. `maxLateralSpeed` (mode config)
5. `forwardAcceleration` (mode config)
6. `lateralAcceleration` (mode config)
7. `forwardStopStrength` (mode config)
8. `lateralStopStrength` (mode config)
9. `maxClimbSpeed` / `maxDescentSpeed` (mode config)
10. `verticalAcceleration` (mode config)
11. `maxYawRateDegrees` (mode config)
12. `yawCatchUpSpeed` (mode config)
13. `gravityCancelMultiplier` (DJIStyleFlightController)

## What each parameter changes

- **maxForwardSpeed**: Top forward/back speed.
- **maxLateralSpeed**: Top side-slip speed.
- **forwardAcceleration / lateralAcceleration**: How quickly each horizontal axis builds velocity.
- **forwardStopStrength / lateralStopStrength**: Axis-specific braking feel near neutral sticks.
- **maxClimbSpeed / maxDescentSpeed**: Independent vertical limits for up/down behavior.
- **verticalAcceleration**: How quickly vertical velocity catches commanded target.
- **maxYawRateDegrees**: Maximum turn rate.
- **yawCatchUpSpeed**: How quickly yaw reaches commanded rate (higher = snappier).
- **gravityCancelMultiplier**: Baseline upward assist against gravity; affects hover feel.

## CSV benchmark workflow (Airdata)

Use this helper when you ingest large Airdata logs:

```bash
python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv
```

It outputs:
- `Docs/airdata_mar30_analysis.json` (machine-readable)
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md` (human summary)
- confidence-labeled segment metrics for hover/forward/lateral/vertical/yaw windows
- `evidence_classification` for each key target as:
  - `directly_measured`
  - `estimated_from_limited_segments`
  - `designer_assumption`
- optional `sim_vs_real_comparison` if `--sim-root` or `--sim-csv-glob` inputs are supplied

Use those metrics to tune **Normal** mode first, then derive Cine/Sport proportionally.
Do not treat `estimated_from_limited_segments` as hard ground truth.

## Mode targets

Use `DroneModeCine`, `DroneModeNormal`, and `DroneModeSport` assets.

### Cine (smooth teaching mode)
- Lower forward/lateral speeds and accelerations
- Lower yaw rate and catch-up speed
- Gentle stop strengths

### Normal (validated baseline)
- Use Airdata benchmark windows as ground truth for baseline response
- Keep forward/lateral and climb/descent independently tunable

### Sport (aggressive mode)
- Increase speed and acceleration from Normal
- Keep stop strengths high enough to remain teachable
- Treat as extrapolated unless separately benchmarked


## Validation loop (recommended)

1. Run benchmark maneuvers in Unity (F7/F8).
2. Copy the generated `session_*` folder from `Application.persistentDataPath/BenchmarkRuns/` into repo-local `BenchmarkRuns/`.
3. Run:

```bash
python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv --sim-root BenchmarkRuns
```

4. Review `Docs/Airdata_Mar30_2026_Benchmark_Summary.md` and `Docs/airdata_mar30_analysis.json`.
5. Tune only mismatched axes/metrics that are clearly measured (not inferred).
