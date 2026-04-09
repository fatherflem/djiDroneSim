# DJI Mini 4 Pro Flight Simulator — Project Handoff

> Last updated: 2026-04-09, reviewed benchmark `session_20260409_125031` (no additional gain vs `122756`) and queued a yaw PD + vertical/lateral refinement pass.
> Written by Claude (Opus) for continuity with ChatGPT Codex or any future agent.

---

## 1. What This Project Is

A Unity flight simulator for the DJI Mini 4 Pro drone. The goal is to replicate real-world flight dynamics so the sim can be used for training and testing. We compare sim output against real flight telemetry captured via DJI's Airdata service.

The sim is **not** a motor-level or acro-rate simulation. It's a **stabilized flight controller** that maps pilot stick inputs to acceleration commands, matching how the real DJI firmware presents itself to the pilot (smooth, assisted, GPS-stabilized flight).

---

## 2. Architecture Overview

### Flight Controller Pipeline

```
DroneInputReader (stick input)
    → DJIStyleFlightController (P-control on velocity error → acceleration)
        → DronePhysicsBody (applies acceleration + yaw to Rigidbody)
```

### Key Files

| File | Purpose |
|---|---|
| `Assets/Scripts/Drone/Flight/DJIStyleFlightController.cs` | Central controller. P-control loop, jerk limit, yaw smoothing. |
| `Assets/Scripts/Drone/Physics/DronePhysicsBody.cs` | Applies world-space acceleration and yaw rotation to Rigidbody. |
| `Assets/Scripts/Drone/Input/DroneInputReader.cs` | Reads stick input (live or benchmark-injected). |
| `Assets/Resources/Configs/DroneModeNormal.asset` | **Primary tuning file.** ScriptableObject with per-mode speed/accel/yaw parameters. |
| `Assets/Resources/Configs/DroneModeCine.asset` | Cine mode tuning (not actively tuned yet). |
| `Assets/Resources/Configs/DroneModeNormal.asset` | Sport mode tuning (not actively tuned yet). |
| `Assets/Scenes/DroneTrainingVerticalSlice.unity` | Main scene. **Scene-serialized globals** on the controller live here. |
| `Assets/Resources/Benchmarks/*.asset` | Benchmark protocol ScriptableObjects (maneuver definitions). |
| `Tools/analyze_airdata.py` | Comparison tool: real Airdata CSV vs sim benchmark CSV. |
| `Apr-8th-2026-08-15AM-Flight-Airdata.csv` | **Current real-world reference data** (April 8, 2026 flight). |
| `BenchmarkRuns/` | Sim benchmark output zips (one per F9 run). |

### Critical Architectural Detail: P-gains vs Accel Caps

The `DroneFlightModeConfig` fields named `forwardAcceleration`, `lateralAcceleration`, `verticalAcceleration` are **proportional gains** (P-gains) in a velocity-error controller, NOT acceleration limits. They control how aggressively the controller chases the target speed.

**Actual acceleration limits** are scene-serialized globals on `DJIStyleFlightController`:

| Global (scene-serialized) | Current Value | Where |
|---|---|---|
| `globalForwardAccelLimit` | **3** m/s² | Scene line 323 |
| `globalLateralAccelLimit` | **5** m/s² | Scene line 324 |
| `globalVerticalAccelLimit` | **7** m/s² | Scene line 325 |
| `gravityCancelMultiplier` | 1.0 | Scene line 322 |
| `brakingInputDeadband` | 0.08 | Scene line 326 |
| `accelerationSlewRate` | **6** m/s³ (code default) | `DJIStyleFlightController.cs:55` |

The `accelerationSlewRate` is a **jerk limit** — it caps how fast the commanded acceleration vector can change per physics tick via `Vector3.MoveTowards`. This is the main source of onset delay in the sim.

### The Control Loop (FixedUpdate)

```
1. Compute target speed from stick input × maxSpeed
2. Compute velocity error = target - current
3. Multiply error by P-gain to get desired acceleration
4. Clamp to global accel limits
5. Slew (jerk-limit) the pilot acceleration via MoveTowards
6. Add gravity compensation
7. Apply to Rigidbody
```

Yaw command is now a **PD-on-rate loop during active yaw input** (P chases target yaw rate, D damps with current yaw rate), with capped overshoot headroom. Neutral stick still uses hard-stop `MoveTowards` braking.

---

## 3. Current Tuning State (DroneModeNormal.asset)

```yaml
maxForwardSpeed: 3.5        # m/s target at full stick
maxLateralSpeed: 10         # m/s target at full stick
forwardAcceleration: 2.8    # P-gain (not a cap!)
lateralAcceleration: 2.8    # P-gain (not a cap!)
forwardStopStrength: 4      # braking P-gain
lateralStopStrength: 7.1    # braking P-gain
lateralRightSpeedMultiplier: 0.88   # targeted right-lateral trim from Apr 9 rerun deltas
lateralRightAccelerationMultiplier: 0.92  # targeted right-lateral onset trim
lateralRightStopMultiplier: 1       # was 1.3 (hack, removed)
maxClimbSpeed: 4.35
maxDescentSpeed: 3.7
verticalAcceleration: 5.4   # P-gain
maxYawRateDegrees: 82
yawCatchUpSpeed: 3.6
yawLeftCatchUpMultiplier: 1  # was 0.55 (hack, reverted)
yawRightCatchUpMultiplier: 1
yawStopSpeed: 4
yawRightCommandGain: 1      # was 1.1 (hack, removed)
yawLeftCommandGain: 1
yawRightStopMultiplier: 1   # was 1.1 (hack, removed)
tiltLimitDegrees: 30
tiltSmoothing: 12
```

The original asymmetry "hack" multipliers were removed after the body-frame bug fix (`a3d9243`). Current right-lateral multipliers are intentional, benchmark-driven trims from the April 9 rerun and should be validated with the next F9 session before further adjustment.

---

## 4. Benchmark Protocol

Press **F9** in Play mode to run the full benchmark protocol. It plays each maneuver sequentially:

1. `hover_hold` — neutral hold segment **7.0s** (10.0s total including runner pre-roll + settle)
2. `forward_step` — +pitch step, **1.0s** hold
3. `lateral_right` — +roll step, **2.5s** hold
4. `lateral_left` — -roll step, **2.5s** hold
5. `climb` — +throttle step, **1.0s** hold
6. `descent` — -throttle step, **1.0s** hold
7. `yaw_right` — +yaw step, **1.0s** hold
8. `yaw_left` — -yaw step, **1.0s** hold

Each maneuver has a pre-roll (1.5s calm), input phase, and settle phase (1.5s release). Output goes to `BenchmarkRuns/session_YYYYMMDD_HHMMSS/`.

Benchmark maneuver assets in `Assets/Resources/Benchmarks/` are the source of truth for input-phase timing (`ManeuverDefinition.segments[].duration`). Currently only the lateral step maneuvers were extended from 1.0 to 2.5 seconds (commit `f609a76`); the other non-hover step maneuvers remain 1.0 seconds.

### Running the Analyzer

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session session_20260409_125031
```

`Apr-8th-2026-08-15AM-Flight-Airdata.csv` is the authoritative real-flight benchmark reference for this tuning pass. The Mar 30 log remains useful for stick-amplitude calibration history but is not the primary benchmark baseline.

This produces:
- `Docs/airdata_mar30_analysis.json` — full structured comparison
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md` — human-readable summary table

---

## 5. Where We Are Now — Latest Deltas (session_20260409_125031)

| Category | Real Peak | Sim Peak | Peak Δ | Overshoot Δ | Delay Δ | Verdict |
|---|---|---|---|---|---|---|
| forward_step | 2.63 m/s | 2.11 m/s | **−0.52** | −0.35 | −0.02 | Sluggish (small gap) |
| lateral_right | 7.44 m/s | 9.81 m/s | **+2.37** | −0.07 | +0.15 | **Too aggressive** |
| lateral_left | 10.04 m/s | 9.81 m/s | **−0.23** | −0.11 | −0.05 | Nearly matched |
| climb | 4.33 m/s | 2.94 m/s | **−1.39** | −1.06 | +0.04 | Improved but still sluggish |
| descent | 3.68 m/s | 2.91 m/s | **−0.76** | −1.10 | +0.07 | Improved but still sluggish |
| yaw_right | 82.0 °/s | 79.6 °/s | **−2.40** | −1.31 | −0.24 | Sluggish (structural) |
| yaw_left | 82.0 °/s | 79.6 °/s | **−2.40** | −2.41 | −0.24 | Sluggish (structural) |

### Key observations:
1. **Lateral is close!** Left is nearly matched. Right overshoots by 2.37 — may need `maxLateralSpeed` reduced from 10 or lateral P-gain (`lateralAcceleration`) tweaked. The real asymmetry (right=7.4, left=10.0) may be wind or mechanical; the sim gives symmetric 9.8.
2. **Climb/descent remain the biggest gaps.** Newest session remained at 2.94/2.91 vs real 4.33/3.68. Vertical authority was increased after this run (`globalVerticalAccelLimit: 7`, `verticalAcceleration: 5.4`) and now needs benchmark confirmation.
3. **Yaw undershoot was structural in prior runs.** The active-yaw path has now been rewritten to PD-on-rate to permit realistic overshoot; next benchmark should verify overshoot and settle shape improvements.
4. **Forward is close but still under target.** The 0.52 m/s peak gap is now the smallest non-yaw translational miss after climb/descent.

---

## 6. What Was Already Done (History)

### Phase 1: Body-frame fix (commit a3d9243)
The flight controller was computing velocity error in world-frame instead of body-frame. This meant yaw rotation corrupted lateral/forward channels. Fixed by projecting velocity into `Quaternion.Inverse(transform.rotation) * velocity`. This invalidated all the left/right asymmetry hack multipliers.

### Phase 2: First-pass tuning against Apr 8 data (commits 7d0c0b9 through e4a36f3)
- Removed all asymmetry hack multipliers (set to 1.0)
- Tuned `maxClimbSpeed` (3.8→4.35), `maxDescentSpeed` (4.3→3.7)
- Tuned `maxYawRateDegrees` (74→82), `yawCatchUpSpeed` (4.5→3.6)
- Lowered global accel caps from stale values (12→3 forward, 12→5 lateral, 10→4 vertical)
- Reverted `yawLeftCatchUpMultiplier` from 0.55 to 1.0

### Phase 3: Jerk limit (commit a66ee96)
Added `accelerationSlewRate` (jerk limit) of 6 m/s³ to model real-drone onset delay. This uses `Vector3.MoveTowards` on the commanded acceleration vector, giving linear ramp-up instead of instant step.

### Phase 4: Extended lateral benchmark hold + vertical cap (commit f609a76)
- Extended lateral maneuver stick hold from 1.0s to 2.5s (sim couldn't reach peak in 1s)
- Lowered `globalVerticalAccelLimit` from 5 to 4

### Phase 5: Analyzer symmetry fix (commit 792ae5b)
Fixed `Tools/analyze_airdata.py` — the sim side was computing "steady" from settle-phase rows (decaying signal after stick release) while the real side used active-phase tail. This inflated sim overshoot and caused climb/descent to appear "too aggressive" when they were actually sluggish.

---

## 7. What Needs To Be Done Next

### Priority 1: Validate vertical authority increase (climb/descent)
- **Problem**: Sim peaks were 2.94 m/s (climb) and 2.91 m/s (descent), below real 4.33 / 3.68 m/s.
- **Change already applied**: `globalVerticalAccelLimit` raised to 7 and `verticalAcceleration` raised to 5.4.
- **Next action**: run full F9 benchmark and confirm climb/descent peak and overshoot movement is in the correct direction.

### Priority 2: Validate right-lateral trim
- **Problem**: `lateral_right` overshoots by +2.37 m/s while `lateral_left` is already near target.
- **Change already applied**: `lateralRightSpeedMultiplier` set to 0.88 and `lateralRightAccelerationMultiplier` set to 0.92.
- **Next action**: rerun benchmark and check whether right peak drops without materially degrading left lateral fidelity.

### Priority 3: Validate new yaw PD behavior
- **Problem**: Yaw was undershooting with insufficient overshoot/settle shape match.
- **Change already applied**: active-yaw dynamics now use PD-on-rate with acceleration clamp and overshoot headroom.
- **Next action**: benchmark yaw-left/yaw-right and retune `yawCatchUpSpeed`, `yawStopSpeed`, `yawOvershootHeadroom`, and `yawAccelerationLimitMultiplier` from measured deltas.

### Priority 4: Forward overshoot
- **Problem**: Sim undershoots real by 0.52 m/s peak and 0.35 overshoot.
- **Possible fix**: Lower `forwardStopStrength` from 6.5 to ~4.0. This braking gain may be suppressing the natural overshoot the real drone shows. Small change, low risk.

### Deferred / Low Priority
- **Visual tilt cleanup**: `UpdateVisualTilt` now receives the Y component of `slewedPilotAcceleration`. Should either strip Y or revert to horizontal-only. Cosmetic only.
- **Sampling rate asymmetry**: Real Airdata is 10 Hz, sim is 50 Hz (FixedUpdate). The `max_accel` metric is computed from consecutive sample deltas, so the sim sees finer granularity. Could downsample sim to 10 Hz for fairer comparison, but this is an analyzer issue, not a sim issue.
- **Cine/Sport mode tuning**: Only Normal mode has been tuned. Cine and Sport configs exist but are untouched.
- **Hover hold wind/drift**: Real hover shows 1.0 m/s horizontal RMS (wind), sim shows 0.0. Adding a wind disturbance model would close this gap.

---

## 8. Conventions and Workflow

### Latest handoff update (2026-04-09)
- Logged new benchmark session artifact: `BenchmarkRuns/session_20260409_125031.zip`.
- Confirmed `session_20260409_125031` is numerically unchanged vs `session_20260409_122756` on all primary maneuver deltas.
- Started a new refinement pass: yaw PD-on-rate control while stick-held, higher vertical authority (`globalVerticalAccelLimit: 7`, `verticalAcceleration: 5.4`), and moderated right-lateral multipliers (`0.88` speed, `0.92` acceleration).

- **Work on `main` branch** directly (no feature branches unless asked).
- **Don't create PRs** unless the user explicitly asks.
- **Benchmark loop**: Edit tuning → Build → Press F9 → Upload zip → Run analyzer → Compare → Repeat.
- **Real data reference**: Always compare against `Apr-8th-2026-08-15AM-Flight-Airdata.csv`. An older Mar 30 CSV also exists but the Apr 8 data is more recent and authoritative.
- **Commit style**: Descriptive messages explaining what changed and why. Keep regenerated docs (the analysis JSON and summary MD) in the same commit as the code change that motivated them.
- **Authorized repo**: `fatherflem/djidronesim` (GitHub). Only interact with this repo.

---

## 9. Quick Reference: How to Tune

To change **how fast** the drone reaches a speed: adjust the **P-gain** (`forwardAcceleration`, `lateralAcceleration`, `verticalAcceleration`) in `DroneModeNormal.asset`.

To change **the ceiling** on acceleration: adjust the **global accel limit** (`globalForwardAccelLimit`, `globalLateralAccelLimit`, `globalVerticalAccelLimit`) in the **scene file** (`DroneTrainingVerticalSlice.unity`, lines 322-326).

To change **onset smoothness/delay**: adjust `accelerationSlewRate` in `DJIStyleFlightController.cs:55` (code default, not scene-serialized). Lower = smoother/slower onset. Higher = snappier/faster.

To change **target speed at full stick**: adjust `maxForwardSpeed`, `maxLateralSpeed`, `maxClimbSpeed`, `maxDescentSpeed` in `DroneModeNormal.asset`.

To change **yaw speed**: adjust `maxYawRateDegrees` in the config. Yaw dynamics are controlled by `yawCatchUpSpeed` (onset rate) and `yawStopSpeed` (braking rate).

---

## 10. File Map for Quick Navigation

```
djiDroneSim/
├── Assets/
│   ├── Scripts/Drone/
│   │   ├── Flight/DJIStyleFlightController.cs    ← THE controller
│   │   ├── Flight/DroneFlightModeConfig.cs        ← ScriptableObject definition
│   │   ├── Physics/DronePhysicsBody.cs            ← Rigidbody wrapper
│   │   ├── Input/DroneInputReader.cs              ← Input abstraction
│   │   └── Benchmark/BenchmarkRunner.cs           ← F9 protocol runner
│   ├── Resources/
│   │   ├── Configs/DroneModeNormal.asset          ← TUNE THIS (Normal mode)
│   │   ├── Configs/DroneModeCine.asset
│   │   ├── Configs/DroneModeSport.asset
│   │   └── Benchmarks/*.asset                     ← Maneuver definitions
│   └── Scenes/DroneTrainingVerticalSlice.unity    ← Scene (global caps here)
├── Tools/
│   └── analyze_airdata.py                         ← Comparison analyzer
├── BenchmarkRuns/                                 ← Sim output zips
├── Docs/
│   ├── airdata_mar30_analysis.json                ← Latest comparison JSON
│   └── Airdata_Mar30_2026_Benchmark_Summary.md    ← Latest comparison summary
├── Apr-8th-2026-08-15AM-Flight-Airdata.csv        ← REAL DATA REFERENCE
├── ARCHITECTURE.md                                ← High-level architecture doc
├── TUNING_GUIDE.md                                ← Tuning reference
└── HANDOFF.md                                     ← THIS FILE
```
