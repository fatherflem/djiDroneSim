# Benchmark / Validation Harness

## Why this exists
This benchmark harness runs repeatable scripted stick maneuvers through the existing DJI-style stabilized controller and records time-series output. The goal is objective tuning against real DJI black-box response data (same inputs, same mode, compare measured outputs), instead of relying only on pilot feel.

## What is implemented in this pass
- **ScriptableObject maneuver definitions** (`ManeuverDefinition`) with:
  - maneuver name + description
  - requested flight mode
  - known initial position/yaw
  - timed fixed-input segments
- **Benchmark runner** (`BenchmarkRunner`) that:
  - resets drone state to known initial conditions
  - temporarily overrides live pilot input with scripted input
  - runs selected maneuver on fixed physics timesteps
  - keeps manual flight path intact when benchmark is idle
- **Benchmark telemetry recorder** (`BenchmarkTelemetryRecorder`) capturing per-physics-step samples.
- **CSV exporter** (`BenchmarkCsvExporter`) writing one file per run into `Application.persistentDataPath/BenchmarkRuns`.

## Included maneuvers
Stored in `Assets/Resources/Benchmarks/`:
1. `Hover Hold`
2. `Forward Step Response`
3. `Lateral Step Response`
4. `Vertical Step Response`
5. `Yaw Step Response`
6. `Forward + Yaw Combined Response` (optional combined maneuver)

## How to run a benchmark
1. Open the standard training scene (`Assets/Scenes/DroneTrainingVerticalSlice.unity`).
2. Enter Play Mode.
3. Use **F7** to cycle maneuvers.
4. Press **F8** to start the selected maneuver (press again to stop early).
5. While running, live pilot input is overridden by scripted input.
6. When the run completes, manual input is restored automatically and CSV is exported.

> A small on-screen benchmark panel shows selected maneuver and run state.

## Output format
Each run writes a CSV named like:

`benchmark_<maneuverName>_<yyyyMMdd_HHmmss_runNNN>.csv`

Output folder:

`<Application.persistentDataPath>/BenchmarkRuns/`

CSV columns:
- maneuver metadata: name, mode, duration
- protocol metadata: `protocol_category`, `protocol_order` (for direct Airdata alignment)
- sample time
- position (x,y,z)
- velocity (x,y,z)
- horizontal speed
- vertical speed
- yaw
- yaw rate
- scripted inputs (roll, pitch, throttle, yaw)

## Metrics this raw data supports
From exported data you can compute:
- rise time
- peak horizontal speed
- peak climb/descent rate
- braking distance after stick release
- stopping time
- settling time
- overshoot
- hover drift
- yaw response characteristics

## Notes for validation sessions
For consistency with current validation recommendations:
- Stick deadzone: `0.05`
- Stick expo: `1.0`
- Input smoothing: `8`
- Invert pitch: `true`
- Invert throttle: `false`

These settings ensure manual flying remains aligned with the benchmark baseline when you switch between subjective and objective tuning passes.

## Airdata ingestion helper
For large real-flight Airdata CSVs in repo root, run:

`python Tools/analyze_airdata.py <airdata.csv>`

This performs confidence-labeled RC-channel segmentation, neutral dwell detection, and measured-vs-inferred classification.  
It writes:
- `Docs/airdata_mar30_analysis.json`
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md`

To directly compare simulator telemetry against the real benchmark in one pass:

`python Tools/analyze_airdata.py <airdata.csv> --sim-csv-glob \"<BenchmarkRuns>/*.csv\"`

The JSON output contains a `sim_vs_real_comparison` block with side-by-side metrics and deltas for:
- forward step
- lateral step
- climb / descent
- yaw step
- hover hold
