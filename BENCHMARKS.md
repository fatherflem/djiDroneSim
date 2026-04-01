# Benchmark / Validation Harness

## Purpose
This harness closes the loop between:
1. real Airdata benchmark logs,
2. Unity simulator benchmark exports,
3. side-by-side comparison deltas.

## Simulator benchmark export convention
`BenchmarkRunner` now writes benchmark sessions under:

`<Application.persistentDataPath>/BenchmarkRuns/session_<yyyyMMdd_HHmmss>/`

Each session contains:
- `session_manifest.jsonl` (session header + one line per run)
- one CSV per run:
  - `run_<run#>_<protocol_category>_<maneuver>_<mode>_<timestamp_run#>.csv`

CSV columns include session/run metadata plus telemetry:
- `session_id`, `session_dir`, `run_label`, `run_number`
- `maneuver_name`, `protocol_category`, `protocol_order`, `maneuver_mode`, `maneuver_duration_s`
- `sample_index`, `time_s`
- position + velocity vectors
- `horizontal_speed_mps`, `vertical_speed_mps`
- `yaw_deg`, `pitch_deg`, `roll_deg`, `yaw_rate_degps`
- commanded input channels: `input_roll`, `input_pitch`, `input_throttle`, `input_yaw`

## Supported maneuver categories for comparison
Use these protocol categories to align with Airdata analysis:
- `hover_hold`
- `forward_step`
- `lateral_right`
- `lateral_left`
- `climb`
- `descent`
- `yaw_right`
- `yaw_left`

## Unity run steps
1. Open `Assets/Scenes/DroneTrainingVerticalSlice.unity`.
2. Enter Play Mode.
3. Use **F7** to cycle maneuvers.
4. Use **F8** to start/stop the selected maneuver.
5. Exit Play Mode and copy the new `session_*` directory from `Application.persistentDataPath/BenchmarkRuns/` into repo-local `BenchmarkRuns/` (recommended for analysis history).

## Analysis workflow (real + sim)
Real-only pass:

```bash
python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv --sim-root ""
```

Real + simulator comparison pass (recommended):

```bash
python Tools/analyze_airdata.py \
  Mar-30th-2026-08-31AM-Flight-Airdata.csv \
  --sim-root BenchmarkRuns
```

You can also provide explicit globs:

```bash
python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv \
  --sim-csv-glob "BenchmarkRuns/session_*/run_*.csv"
```

## Outputs
- `Docs/airdata_mar30_analysis.json`
  - real maneuver segmentation and confidence labels
  - measured vs inferred classification
  - indexed simulator run inputs
  - `sim_vs_real_comparison` with category-level deltas
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md`
  - concise comparison table and verdicts

## Evidence / trustworthiness policy
Values stay explicitly separated as:
- `directly_measured_from_airdata`
- `directly_measured_from_sim_csv`
- `estimated_from_limited_segments`
- `designer_assumption`

Do **not** collapse these into a single score.
