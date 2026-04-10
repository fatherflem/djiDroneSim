# Codex Prompt: DJI Drone Simulator — Next Phase Plan

> Paste this into ChatGPT / Codex as a single prompt. It is self-contained.

---

## Context

You are working on a **Unity 6 DJI Mini 4 Pro classroom drone simulator**. The core flight model uses Rigidbody-based stabilized control (velocity-error P + yaw-rate + acceleration slew), tuned against real DJI Mini 4 Pro flight logs.

### Where things stand now

The simulator has gone through ~24 benchmark sessions and ~70 commits of closed-loop tuning against real Airdata CSVs. Here is the current state of each axis in **Normal mode** (the only mode benchmarked so far), comparing simulator peaks to real flight data peaks:

| Axis | Sim Peak | Real Peak | Delta | Status |
|---|---|---|---|---|
| **Forward** (input-phase) | 2.22 m/s | ~2.63 m/s | -15% | Onset gap remains |
| **Forward** (post-release carryover) | 0.50 m/s | ~0.14 m/s | +0.36 m/s | Improved 40% via brake-slew patch, residual remains |
| **Lateral right** | 8.925 m/s | ~7.44 m/s | +20% | Right-trim applied (0.88 speed mult) |
| **Lateral left** | 9.812 m/s | ~10.04 m/s | -2% | Essentially done |
| **Climb** | 2.94 m/s | ~4.33 m/s | **-32%** | **Largest gap — hypothesis untested** |
| **Descent** | 2.93 m/s | ~3.67 m/s | **-20%** | Same issue as climb |
| **Yaw right** | 79.89 °/s | ~82.0 °/s | -3% | Done |
| **Yaw left** | 79.89 °/s | ~82.0 °/s | -3% | Done |

### Key current tuning values (`DroneModeNormal.asset`)

```
maxForwardSpeed: 3.5          forwardAcceleration: 2.8
maxLateralSpeed: 10            lateralAcceleration: 2.8
maxClimbSpeed: 4.35            maxDescentSpeed: 3.7
verticalAcceleration: 5.4
maxYawRateDegrees: 82
forwardAccelerationSlewRate: 6    forwardBrakeSlewRate: 11
lateralRightSpeedMultiplier: 0.88
lateralRightAccelerationMultiplier: 0.92
```

The global `accelerationSlewRate` on the controller is `6.0 m/s³`.

### Current vertical benchmark protocol

The climb and descent maneuvers use **1.0-second input windows** with full-stick amplitude. The hypothesis is that the 32% climb deficit is **slew/protocol-limited** — the commanded acceleration ramps through the slew limiter and doesn't have enough time in 1.0s to reach the configured `maxClimbSpeed` of 4.35 m/s. This hypothesis has never been tested with a longer input window.

### Real flight reference files
- `Apr-8th-2026-08-15AM-Flight-Airdata.csv` — current primary baseline (2904 rows)
- `Mar-30th-2026-08-31AM-Flight-Airdata.csv` — historical calibration reference

### Repository structure (relevant files)
```
Assets/Scripts/Drone/Flight/DJIStyleFlightController.cs   — Core controller
Assets/Scripts/Drone/Flight/DroneFlightModeConfig.cs       — Mode tuning ScriptableObject schema
Assets/Scripts/Drone/Benchmark/BenchmarkRunner.cs          — F7/F8/F9 benchmark harness
Assets/Scripts/Drone/Benchmark/ManeuverDefinition.cs       — Maneuver asset schema
Assets/Scripts/Drone/Benchmark/BenchmarkCsvExporter.cs     — CSV telemetry export
Assets/Scripts/Drone/Training/SimpleTrainingScenario.cs    — Hover-box drill (89 lines)
Assets/Resources/Configs/DroneModeNormal.asset             — Normal mode tuning
Assets/Resources/Configs/DroneModeCine.asset               — Cine mode tuning (unbenchmarked)
Assets/Resources/Configs/DroneModeSport.asset              — Sport mode tuning (unbenchmarked)
Assets/Resources/Benchmarks/Maneuver_*.asset               — 8 protocol maneuvers
Tools/analyze_airdata.py                                   — Real-vs-sim comparison analyzer
Tools/closed_loop_validation.py                            — Validation report generator
Docs/ClosedLoopValidation_Apr09_2026.md                    — Latest validation summary
HANDOFF.md                                                 — Session handoff doc
BENCHMARKS.md                                              — Benchmark harness protocol
TUNING_GUIDE.md                                            — Tuning workflow reference
```

### Maneuver asset structure (example: climb)

Each maneuver is a `ManeuverDefinition` ScriptableObject with:
- `protocolCategory`: string (e.g., "climb")
- `protocolOrder`: int (sort order in F9 full protocol)
- `includeInDefaultProtocol`: bool
- `amplitudeConfidence`: High / Medium / Low
- `amplitudeProvenance`: DirectlyMeasured / EstimatedFromLimitedSegments / DesignerAssumption
- `segments`: list of `InputSegment` (each has `duration`, `roll`, `pitch`, `throttle`, `yaw`)
- Per-maneuver `preRollDuration` and `settleDuration` overrides (defaults: 1.5s each)

Current `Maneuver_Climb.asset`: 1 segment, duration=1.0s, throttle=+1.0.
Current `Maneuver_Descent.asset`: 1 segment, duration=1.0s, throttle=-1.0.

### Cine and Sport mode values (never benchmarked)

**DroneModeCine.asset** (slow, stable — for beginners):
```
maxForwardSpeed: 3.3    maxLateralSpeed: 2.8    maxClimbSpeed: 1.8
forwardAcceleration: 2.8    lateralAcceleration: 2.8
maxYawRateDegrees: 48    tiltLimitDegrees: 14
```

**DroneModeSport.asset** (fast, aggressive):
```
maxForwardSpeed: 9.5    maxLateralSpeed: 7.8    maxClimbSpeed: 4.2
forwardAcceleration: 8.2    lateralAcceleration: 7.2
maxYawRateDegrees: 120    tiltLimitDegrees: 24
```

---

## The Plan: 4 Steps

### Step 1: Test the Vertical Protocol-Limited Hypothesis

**Goal:** Determine whether the 32% climb deficit and 20% descent deficit are caused by the 1.0-second input window being too short for the slew limiter, or whether it's a genuine gain deficit.

**What to do:**

1. **Create two new maneuver assets** in `Assets/Resources/Benchmarks/`:
   - `Maneuver_ClimbLong.asset` — identical to `Maneuver_Climb.asset` but with `duration: 2.5` (matching lateral maneuvers). Set `protocolCategory: "climb_long"`, `protocolOrder: 9`, `includeInDefaultProtocol: true`.
   - `Maneuver_DescentLong.asset` — identical to `Maneuver_Descent.asset` but with `duration: 2.5`. Set `protocolCategory: "descent_long"`, `protocolOrder: 10`, `includeInDefaultProtocol: true`.

2. **Keep the original 1.0s maneuvers in the protocol** so we get both durations in the same session for direct comparison.

3. **Run one F9 full-protocol benchmark session** (this now includes 10 maneuvers).

4. **Analyze results** with `Tools/analyze_airdata.py`. Compare:
   - If `climb_long` peak is significantly higher than `climb` peak (e.g., >3.5 m/s vs 2.94 m/s), the hypothesis is confirmed → the slew limiter is the bottleneck, not the gain. Document this finding. Decide whether to adjust `accelerationSlewRate` for vertical, add vertical-specific slew (like the forward brake-slew patch), or accept the behavior for training purposes.
   - If `climb_long` peak is similar to `climb` peak (~2.94 m/s), the hypothesis is wrong → it's a gain deficit. Proceed to increase `verticalAcceleration` and/or adjust `maxClimbSpeed`/`maxDescentSpeed` in `DroneModeNormal.asset`.

5. **Update `HANDOFF.md` and `Docs/ClosedLoopValidation_Apr09_2026.md`** with the result, replacing the hypothesis with a conclusion.

**Success criteria:** The vertical gap is either closed to within 10% of real, or the root cause is definitively identified with a documented plan to close it.

---

### Step 2: Close the Forward Onset Gap

**Goal:** Bring forward input-phase peak from 2.22 m/s to within 10% of real (~2.63 m/s).

**What to do:**

1. **Diagnose the bottleneck** using current Normal mode values:
   - `maxForwardSpeed: 3.5 m/s` — this is the ceiling. Not the bottleneck (2.22 < 3.5).
   - `forwardAcceleration: 2.8 m/s²` — this is the P-gain clamp.
   - `forwardAccelerationSlewRate: 6 m/s³` — this limits how fast acceleration ramps up.
   - In a 1.0s input window with slew rate 6 m/s³, max acceleration reaches 6.0 m/s² at 1.0s (if unclamped), but it's clamped at 2.8 m/s². So acceleration saturates at `2.8/6 = 0.47s`, then holds at 2.8 m/s² for the remaining 0.53s. Predicted peak speed ≈ `0.5 * 2.8 * 0.47 + 2.8 * 0.53 = 0.66 + 1.48 = 2.14 m/s`. This is close to the observed 2.22 m/s, confirming `forwardAcceleration` is the limiting knob.

2. **Calculate the needed adjustment:**
   - Target: ~2.63 m/s in 1.0s with slewRate=6.
   - Using the same ramp model: need `forwardAcceleration ≈ 3.4 m/s²` (increase from 2.8).
   - This is a ~21% increase. Apply conservatively: try `3.2` first, benchmark, then `3.4` if needed.

3. **Verify carryover doesn't regress:**
   - Higher `forwardAcceleration` means more speed at release, so `forwardBrakeSlewRate` may need a corresponding bump. Monitor the post-release carryover in the same benchmark run.
   - If carryover regresses, increase `forwardBrakeSlewRate` from 11 to ~14-15 proportionally.

4. **Run a benchmark session** after each tuning change. Compare input-phase peak and carryover against the previous `session_20260409_183817` baseline.

5. **Update HANDOFF.md** with new values and benchmark evidence.

**Success criteria:** Forward input-phase peak within 10% of real (~2.37-2.89 m/s) AND carryover does not exceed 0.50 m/s.

---

### Step 3: Define Written Acceptance Criteria

**Goal:** Establish a concrete "done" definition so the tuning loop has a natural stopping point.

**What to do:**

1. **Create `Docs/AcceptanceCriteria.md`** with the following structure:

```markdown
# Simulator Fidelity Acceptance Criteria

## Purpose
This is a classroom training simulator. The goal is "representative enough
that students build correct muscle memory," not telemetry-perfect replication.

## Per-Axis Criteria (Normal Mode)

| Axis | Metric | Threshold | Rationale |
|---|---|---|---|
| Forward | Input-phase peak speed | Within 15% of real | Primary training axis |
| Forward | Post-release carryover | ≤ 0.5 m/s | Affects stop-point learning |
| Lateral (each) | Input-phase peak speed | Within 15% of real | Secondary training axis |
| Climb | Peak speed (2.5s window) | Within 15% of real | Altitude control training |
| Descent | Peak speed (2.5s window) | Within 15% of real | Altitude control training |
| Yaw (each) | Held-input rate | Within 5% of real | Heading control |
| Yaw (each) | Release settle to <5°/s | Within 0.1s of real | Stop crispness |
| Hover | Horizontal drift | ≤ 1.5 m/s RMS | Baseline stability |

## Mode Coverage Criteria

| Mode | Requirement |
|---|---|
| Normal | All per-axis criteria met with benchmark evidence |
| Cine | All per-axis criteria met OR documented rationale for deviation |
| Sport | All per-axis criteria met OR documented rationale for deviation |

## Training Scenario Criteria

| Scenario | Requirement |
|---|---|
| Hover-box drill | Completable by a player within 60s using Normal mode |
| (future scenarios) | TBD as scenarios are added |

## Sign-off
Each criterion must reference a specific benchmark session ID as evidence.
```

2. **Evaluate current state against these criteria** and note which are already met:
   - Yaw: PASS (within 3%)
   - Lateral left: PASS (within 2%)
   - Lateral right: Needs review (20% aggressive — but this is with the trim already applied)
   - Forward onset: FAIL (15% deficit — borderline)
   - Forward carryover: PASS (0.50 m/s)
   - Climb/Descent: BLOCKED on Step 1 outcome

3. **Add a "Current Status" section** to the acceptance doc with pass/fail per criterion and the session ID used as evidence.

**Success criteria:** The document exists, is referenced in HANDOFF.md, and current benchmark evidence is evaluated against it.

---

### Step 4: Validate Cine & Sport Modes + Polish Training

**Goal:** Extend benchmark coverage beyond Normal mode and make the training scenario usable.

**What to do:**

#### 4A: Benchmark Cine and Sport modes

1. **Create mode-specific maneuver variants** OR (simpler) **modify the BenchmarkRunner to accept a mode override** so existing maneuvers can run in each mode without duplicating assets. Check if `ManeuverDefinition.flightMode` already controls this — if each maneuver specifies `flightMode: 1` (Normal), you may need to either:
   - Create parallel maneuver sets for Cine/Sport, OR
   - Add a runtime mode override to BenchmarkRunner (preferred — less asset duplication)

2. **Run F9 protocol in Cine mode.** Expected behavior: all peaks should be lower than Normal (slower speeds, less tilt). Compare against the Cine config values to verify the mode is correctly applied.

3. **Run F9 protocol in Sport mode.** Expected behavior: all peaks should be higher than Normal. Compare against Sport config values.

4. **There is no real-flight Cine/Sport reference data**, so these benchmarks validate internal consistency (does the sim respect mode configs?) rather than real-world fidelity. If Cine/Sport real data becomes available later, use the same closed-loop workflow.

5. **Tune if needed:** The Cine and Sport `.asset` files have never been tuned against real data. If the ratios between modes feel wrong compared to known DJI Mini 4 Pro behavior (published specs: Normal ≈ 10 m/s max horizontal, Sport ≈ 16 m/s, Cine not officially published but typically ~3-6 m/s), adjust the config values proportionally.

#### 4B: Polish the training scenario

1. **Review `SimpleTrainingScenario.cs`** (89 lines). Current state: a single hover-box drill with time-based completion. The structure is minimal but functional.

2. **Playtest the hover-box drill** in each mode:
   - Is 15s required hover time reasonable for a student?
   - Is the 3x3m box + 0.5m altitude tolerance too tight or too loose?
   - Is 0.75 m/s speed threshold appropriate?

3. **Add basic UI feedback** if not present:
   - Visual indicator of the hover box boundaries (already may be in the debug HUD)
   - Progress bar or timer showing accumulated hover time
   - Completion message

4. **Consider one additional training drill** — a simple waypoint navigation exercise would complement the hover drill by testing forward/lateral/yaw control. This is stretch scope; only pursue if Steps 1-3 and 4A are complete.

5. **Update README.md** to reflect the current feature set and training scenario status.

**Success criteria:**
- Cine and Sport benchmark sessions archived with manifests
- Training scenario is playable and completable in Normal mode
- Any mode-specific tuning changes are documented

---

## Execution Order

```
Step 1 (Vertical hypothesis)     ← Do first. One session. Highest information gain.
    ↓
Step 2 (Forward onset gap)       ← Do second. 1-2 sessions. Closes second-largest gap.
    ↓
Step 3 (Acceptance criteria)     ← Do third. Documentation only. Frames the stopping point.
    ↓
Step 4 (Cine/Sport + Training)   ← Do last. Broadens coverage. Polish pass.
```

Steps 1 and 2 are independent and could run in parallel if you have two benchmark windows. Step 3 should happen before Step 4 so you know what "good enough" means for Cine/Sport.

---

## Rules / Guardrails

- **One variable at a time.** Never change more than one tuning knob between benchmark sessions.
- **Archive every session.** Drop zips into `BenchmarkRuns/` with the timestamp naming convention.
- **Update HANDOFF.md** after each meaningful finding — this is the project's institutional memory.
- **Don't reopen yaw.** It's within 3% and frozen unless new real-data evidence appears.
- **Don't touch lateral left.** It's within 2%.
- **Lateral right trim stays** at `0.88`/`0.92` unless new evidence contradicts it.
- **Benchmark protocol is additive** — never remove existing maneuvers, only add new ones (like the long-window variants). This preserves comparability with all prior sessions.
