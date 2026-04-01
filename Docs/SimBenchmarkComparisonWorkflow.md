# Simulator ↔ Real Benchmark Comparison Workflow

## Scope
This is the focused validation loop for comparing Unity benchmark maneuvers against the real Airdata benchmark log:

- Real source: `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Sim source: benchmark CSV exports from `BenchmarkRunner`
- Output: side-by-side category deltas in JSON + Markdown

## 1) Run simulator benchmark maneuvers in Unity
1. Open `Assets/Scenes/DroneTrainingVerticalSlice.unity`.
2. Enter Play Mode.
3. Press **F7** to choose maneuver.
4. Press **F8** to run selected maneuver.
5. Repeat runs for each protocol category (`hover_hold`, `forward_step`, `lateral_right`, `lateral_left`, `climb`, `descent`, `yaw_right`, `yaw_left`).

## 2) Collect simulator exports
Unity exports to:

`<Application.persistentDataPath>/BenchmarkRuns/session_<yyyyMMdd_HHmmss>/`

Copy that whole `session_*` folder into repo-local:

`BenchmarkRuns/session_<yyyyMMdd_HHmmss>/`

Each session includes:
- `session_manifest.jsonl`
- `run_###_<category>_<maneuver>_<mode>_<label>.csv`

## 3) Run analysis
From repo root:

```bash
python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv --sim-root BenchmarkRuns
```

Optional explicit input control:

```bash
python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv \
  --sim-csv-glob "BenchmarkRuns/session_*/run_*.csv"
```

## 4) Read outputs
- `Docs/airdata_mar30_analysis.json`
  - `metrics`: real-flight segmented metrics
  - `sim_runs_index`: discovered simulator runs
  - `sim_vs_real_comparison`: delta per category and metric
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md`
  - quick comparison table and verdicts

## Measured vs inferred policy
- Real baseline metrics: segmented from Airdata with confidence labels.
- Sim metrics: directly measured from benchmark CSV exports.
- Any missing/weak category is explicitly marked as `insufficient_data` or `designer_assumption`.

No combined “single score” is used.
