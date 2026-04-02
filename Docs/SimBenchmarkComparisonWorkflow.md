# Simulator ↔ Real Benchmark Comparison Workflow

## Scope
This is the focused validation loop for comparing Unity benchmark maneuvers against the real Airdata benchmark log:

- Real source: `Mar-30th-2026-08-31AM-Flight-Airdata.csv`
- Sim source: benchmark CSV exports from `BenchmarkRunner`
- Output: side-by-side category deltas in JSON + Markdown

## 1) Run simulator benchmark maneuvers in Unity
1. Open `Assets/Scenes/DroneTrainingVerticalSlice.unity`.
2. Enter Play Mode.
3. Press **F9** once to run the default full protocol end-to-end.
4. Optional manual path: **F7** chooses maneuver and **F8** runs selected maneuver.

Default full protocol order:
1. `hover_hold`
2. `forward_step`
3. `lateral_right`
4. `lateral_left`
5. `climb`
6. `descent`
7. `yaw_right`
8. `yaw_left`

Protocol assets are input-only for non-hover categories. `BenchmarkRunner` owns neutral pre-roll and settle timing.
Default benchmark timing for protocol maneuvers:
- non-hover steps: `1.0s` active input segments
- runner neutral windows: `1.5s` pre-roll + `1.5s` settle
- hover total neutral hold: `10.0s` (`1.5 + 7.0 + 1.5`)

Default benchmark amplitude:
- maneuver segment channels are normalized stick values in `[-1, 1]`
- the default protocol uses full-scale step amplitudes (`±1`) on the active axis
- inspect or change amplitudes directly in `Assets/Resources/Benchmarks/Maneuver_*.asset`

## 2) Collect simulator exports
Unity exports to:

`<Application.persistentDataPath>/BenchmarkRuns/session_<yyyyMMdd_HHmmss>/`

Copy that whole `session_*` folder into repo-local:

`BenchmarkRuns/session_<yyyyMMdd_HHmmss>/`

Each session includes:
- `session_manifest.jsonl`
- `run_###_<category>_<maneuver>_<mode>_<label>.csv`

Run manifest entries include `maneuver_name`, `protocol_category`, `protocol_order`, timing, and `run_source` (`manual` or `full_protocol`).

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
