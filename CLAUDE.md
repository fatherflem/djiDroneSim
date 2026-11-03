# CLAUDE.md

Guidance for Claude Code sessions in this repository. Claude Code loads this file automatically.

## What this is

A Unity 6.3 LTS (`6000.3.17f1`, URP) training simulator for a DJI Mini-class drone: a stabilized,
DJI-style flight model (not per-motor), desktop and stationary-VR (Quest 3 / OpenXR) presentation,
and a training layer (Hover Box drill). The goal is a classroom-ready 1.0.

## Read first, in this order

1. `Docs/RELEASE_1_0_PLAN.md` — current state audit, guardrails, and the 1.0 roadmap. **Start here.**
2. `HANDOFF.md` — flight-tuning history and the Normal-mode freeze decision.
3. `ARCHITECTURE.md`, `TUNING_GUIDE.md`, `BENCHMARKS.md`, `Docs/AcceptanceCriteria.md` as needed.
4. `Docs/HoverBoxDrill.md`, `Docs/FieldEnvironments.md`, `Docs/VRRoadmap.md` for training/VR work.

Some docs mention Codex. Earlier work was done with Codex; this project now uses Claude only.

## Guardrails

- **Normal-mode flight behavior is frozen.** Don't change controller gains, `Assets/Resources/Configs/DroneMode*.asset`,
  Rigidbody setup, input curves, benchmark tolerances, or scene-serialized controller globals unless there's an
  explicit fidelity reason, and keep before/after benchmark evidence when you do.
- Desktop and VR are two presentations of one training model. Never fork drill logic per platform.
- Real-flight reference data: `Apr-8th-2026-08-15AM-Flight-Airdata.csv` (primary), `Apr-10th-...` (exploratory).
  Don't delete or rewrite these or the archived `BenchmarkRuns/*.zip`.

## Flight architecture in one paragraph

`DroneInputReader → DJIStyleFlightController → DronePhysicsBody`. The controller is P-control on body-frame
velocity error. **Per-mode `forwardAcceleration`/`lateralAcceleration`/`verticalAcceleration` are P-gains, not
caps.** The real accel caps (`globalForwardAccelLimit` etc.) are serialized on the controller component in each
scene. Pilot acceleration is jerk-limited by `accelerationSlewRate`. Yaw uses first-order catch-up, so it can't
overshoot by construction.

## Driving Unity from the command line (local sessions only)

Find the editor: Unity Hub installs to `C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe` (Windows)
or `/Applications/Unity/Hub/Editor/6000.3.17f1/Unity.app/Contents/MacOS/Unity` (macOS). Use exactly `6000.3.17f1`.

- **Close the project in the Unity Editor first.** Batch mode fails with a lock error if the project is open.
- Compile check: `Unity -batchmode -nographics -projectPath . -quit -logFile -`, then look for `error CS` in the output.
- Run tests: `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <path>.xml -logFile -`
  (also `PlayMode`). Don't pass `-quit` together with `-runTests`.
- Custom entry points: `-executeMethod Namespace.Class.StaticMethod` (the method must live in an `Editor` folder).
- The first batch run after a pull can take minutes to reimport. Later runs are much faster.

## Verification tools

- **Headless benchmark:** `python Tools/run_headless_benchmark.py --fail-on-drift` runs the default Normal protocol
  in batch mode (`Assets/Editor/Benchmark/HeadlessBenchmarkRunner.cs`), archives it to `BenchmarkRuns/`, and gates it
  with `Tools/check_acceptance.py` against the freeze baseline `session_20260413_142657`. Also available as
  **DroneSim → Benchmarks → Run Default Protocol (unattended)** in the Editor.
- **Acceptance gate alone:** `python3 Tools/check_acceptance.py <session>` scores a session against
  `Docs/AcceptanceCriteria.md`. Exit code 1 means regression, 2 means drift (with `--fail-on-drift`).
- **Unity tests:** EditMode tests live in `Assets/Tests/EditMode` (assembly `DroneSim.Tests.EditMode`).
- **Without Unity (cloud sessions):** `dotnet test Tools/DotnetChecks/CoreTests` runs the pure-C# tests.
  `python3 Tools/DotnetChecks/fetch_unity_refs.py && dotnet build Tools/DotnetChecks/CompileCheck` compile-checks
  selected Unity-facing files against Unity reference assemblies plus stubs. Add new files to its csproj. It's a
  safety net; Unity's own compile is still the real test.

## Controller setup

See `Docs/ControllerSetup.md`. The physical RC (RadioMaster T8L, Mode 2) goes through `ControllerSetupService`,
which loads per-PC calibration, runs the guided controller check (F4), and handles disconnects. Desktop and VR UIs
both read that service. Don't read the joystick directly from new drills or menus.

## Known gaps

- Not yet run in Unity: the headless runner, the controller check, and the EditMode tests were written and checked
  outside Unity. Their first real run is pending (see the checklist in `Docs/ControllerSetup.md`).
- No PlayMode tests yet.
- No committed `ProjectSettings/EditorBuildSettings.asset` and no proven player build recipe.
- Hover Box and VR flows still need real Play Mode and headset verification. The existing Hover Box VR status
  panel sizes a world-space canvas at scale 1 (pixel units as meters), so check its size in the headset.

## Analyzer

```
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session <session_name>
```
Writes `Docs/airdata_mar30_analysis.json` and `Docs/Airdata_Mar30_2026_Benchmark_Summary.md`. Sim metrics are
measured over the input phase only, which keeps the window symmetric with the real Airdata segments.

## Git

- Repo: `fatherflem/djidronesim`. Work happens on topic branches merged by PR into `main`.
- Commit regenerated analysis docs together with the change that motivated them.
- Don't commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or build output.
