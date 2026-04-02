# Benchmark / Validation Harness

## Purpose
This harness closes the loop between:
1. real Airdata benchmark logs,
2. Unity simulator benchmark exports,
3. side-by-side comparison deltas.

## Simulator benchmark export convention
`BenchmarkRunner` writes benchmark sessions under:

`<Application.persistentDataPath>/BenchmarkRuns/session_<yyyyMMdd_HHmmss>/`

Each session contains:
- `session_manifest.jsonl`
  - first line = session metadata/config snapshot
  - one additional line per run
- one CSV per run:
  - `run_<run#>_<protocol_category>_<maneuver>_<mode>_<timestamp_run#>.csv`

## Run structure (trustworthy capture)
Every benchmark run now uses a fixed structure:
1. **Pre-roll (neutral)**: no commanded stick input, captures baseline hover/drift.
2. **Input phase**: maneuver-defined segment playback.
3. **Settle (neutral)**: no commanded stick input, captures overshoot/coast/braking.
4. **Export**.

Duration controls:
- global defaults on `BenchmarkRunner`:
  - `defaultPreRollDuration` (default 1.5s)
  - `defaultSettleDuration` (default 1.5s)
- optional per-maneuver overrides on `ManeuverDefinition`:
  - `overridePreRollDuration` + `preRollDuration`
  - `overrideSettleDuration` + `settleDuration`

For protocol trustworthiness, non-hover maneuvers should define **input-only segments**.  
Do not add neutral-before or neutral-after segments to those assets; the runner already provides pre-roll/settle neutral windows.

Default protocol timing for direct real-world comparison:
- runner neutral pre-roll: `1.5s` (`BenchmarkRunner.defaultPreRollDuration`)
- non-hover step input window: `1.0s` (single segment in each non-hover protocol maneuver asset)
- runner neutral settle: `1.5s` (`BenchmarkRunner.defaultSettleDuration`)
- hover neutral total hold: `10.0s` via `1.5s` pre-roll + `7.0s` hover maneuver segment + `1.5s` settle

Default protocol stick amplitude:
- benchmark maneuver segment channels are normalized stick commands in `[-1, 1]`
- default protocol step amplitudes are calibrated from `Mar-30th-2026-08-31AM-Flight-Airdata.csv` RC channels using segmented active windows + median plateau magnitude per maneuver family
- current default protocol amplitudes:
  - `forward_step`: `pitch = +0.77` (**estimated_from_noisy_or_limited_segments**, rc_elevator plateau medians varied across repeated runs)
  - `lateral_right`: `roll = +1.00` (**directly_measured_from_clean_rc_plateaus**)
  - `lateral_left`: `roll = -1.00` (**estimated_from_noisy_or_limited_segments**, mirrored from right due missing clean left segment in this log)
  - `climb`: `throttle = +1.00` (**estimated_from_noisy_or_limited_segments**, medium-confidence segmentation but consistent plateaus)
  - `descent`: `throttle = -1.00` (**estimated_from_noisy_or_limited_segments**, mixed confidence but consistent plateaus)
  - `yaw_right`: `yaw = +1.00` (**directly_measured_from_clean_rc_plateaus**)
  - `yaw_left`: `yaw = -1.00` (**directly_measured_from_clean_rc_plateaus**)
- edit `segments` + `inputAmplitudeEvidence`/`inputAmplitudeNotes` on `Assets/Resources/Benchmarks/Maneuver_*.asset` to change benchmark amplitude and evidence labels
- regenerate RC-derived recommendation artifacts with:
  - `python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv --sim-root ""`

## Repeatable reset policy before every run
Before each run begins, `BenchmarkRunner` performs an explicit benchmark reset:
- resets `DroneInputReader` smoothing + external frame state to neutral
- resets `DJIStyleFlightController` transient state (yaw-rate integrator, commanded acceleration, visual tilt)
- repositions drone to benchmark start transform (plus benchmark area offset when enabled)
- reapplies initial yaw
- clears rigidbody linear and angular velocity to zero
- sleeps/wakes rigidbody to avoid momentum carryover

This avoids cross-run contamination from prior inputs or stabilization state.

## CSV fields (analysis-complete)
CSV columns include:
- session/run metadata:
  - `session_id`, `session_dir`, `run_label`, `run_number`
  - `maneuver_name`, `protocol_category`, `protocol_order`, `maneuver_mode`
- phase + timing context:
  - `benchmark_phase` (`preroll`, `input`, `settle`)
  - `maneuver_preroll_s`, `maneuver_duration_s`, `maneuver_settle_s`
  - `sample_index`, `time_s`
- kinematics:
  - position + velocity vectors
  - `forward_speed_mps`, `lateral_speed_mps`, `horizontal_speed_mps`, `vertical_speed_mps`
  - `yaw_deg`, `pitch_deg`, `roll_deg`, `yaw_rate_degps`
- commanded channels:
  - `input_roll`, `input_pitch`, `input_throttle`, `input_yaw`

## Session metadata/config snapshot
The first `session_manifest.jsonl` line now records capture context including:
- `session_id`, UTC timestamp, export directory
- `Application.version`, `Time.fixedDeltaTime`
- benchmark area origin and spawn offset
- benchmark runner settings (pre-roll/settle defaults, maneuver library, protocol ordering)
- default protocol input amplitudes snapshot (`default_protocol_input_amplitudes`) including per-maneuver axis values and evidence labels
- controller global limits
- active mode config values (Cine/Normal/Sport tuning snapshots)

## Benchmark-safe environment isolation
`BenchmarkRunner` coordinates `BenchmarkEnvironmentController` so benchmark maneuvers run in a sterile area:

- On benchmark start, the controller disables colliders and rigidbody collision participation on presentation-only objects, including:
  - `VRUserPlaceholder`
  - `DroneControllerPlaceholder` + stick visuals
  - `DroneFeedDisplaySurface` roots / fallback `VRControllerScreenPlaceholder_Fallback`
  - demo props such as `Marker` and `HoverBoxEdge`
- During the run, drone reset uses a dedicated benchmark offset (`benchmarkSpawnOffset`, default `(0, 0, 40)`) so maneuvers execute away from demo/operator rigs.
- On benchmark stop (or runner disable), original collider/rigidbody settings are restored for normal play/demo mode.

To change the clean benchmark spawn/reset area, edit `benchmarkSpawnOffset` on `BenchmarkEnvironmentController`.
Disable dedicated offset behavior via `useDedicatedBenchmarkArea` if you need authored positions exactly.

## Repeatable execution paths
In Play Mode:
- **F7**: cycle maneuver
- **F8**: run/stop selected maneuver
- **F9**: run full default protocol queue (maneuvers where `includeInDefaultProtocol = true`, ordered by `protocolOrder`, then maneuver name)

The benchmark debug window now shows:
- selected maneuver and queue info
- active phase (`PreRoll`, `Input`, `Settle`)
- elapsed phase/total timing
- effective pre-roll/input/settle durations
- session/run counters

Optional scene debug:
- select `BenchmarkRunner` to show gizmo marker for benchmark start origin + heading.

## Unity run steps
1. Open `Assets/Scenes/DroneTrainingVerticalSlice.unity`.
2. Enter Play Mode.
3. Use **F9** for one clean full-protocol capture (recommended), or **F7/F8** for manual per-maneuver runs.
4. Exit Play Mode and copy the new `session_*` directory from `Application.persistentDataPath/BenchmarkRuns/` into repo-local `BenchmarkRuns/` (recommended for analysis history).

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

## Supported maneuver categories for comparison
Default comparison protocol (`includeInDefaultProtocol = true`) runs this order:
1. `hover_hold`
2. `forward_step`
3. `lateral_right`
4. `lateral_left`
5. `climb`
6. `descent`
7. `yaw_right`
8. `yaw_left`

Use these protocol categories to align with Airdata analysis:
- `hover_hold`
- `forward_step`
- `lateral_right`
- `lateral_left`
- `climb`
- `descent`
- `yaw_right`
- `yaw_left`

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
