# DJI Mini 4 Pro Flight Simulator — Project Handoff

> Last updated: 2026-04-10 after implementing next-phase scaffolding (long vertical maneuvers, protocol mode override support, and acceptance criteria doc).

---

## 1. Current truth snapshot

- **Latest decisive sim session:** `BenchmarkRuns/session_20260409_183817.zip`.
- Comparison anchors: `BenchmarkRuns/session_20260409_125031.zip` (pre-regression reference), `BenchmarkRuns/session_20260409_145236.zip` (yaw-regressed).
- Real reference log: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`.

Observed protocol peaks across key sessions (forward includes both input-phase and full-run values to expose carryover):

| Category | 164309 | 170224 | 180413 | 183817 | Net story |
|---|---:|---:|---:|---:|---|
| forward_step input-phase peak | 2.112 m/s | 2.112 m/s | 2.220 m/s | 2.220 m/s | onset gain from pitch=1.0 held |
| forward_step full-run peak | 2.400 m/s | 2.400 m/s | 3.060 m/s | 2.720 m/s | brake-slew patch reduced release carryover materially |
| lateral_right input peak | 8.925 m/s | 8.925 m/s | 8.925 m/s | 8.925 m/s | unchanged |
| lateral_left input peak | 9.812 m/s | 9.812 m/s | 9.812 m/s | 9.812 m/s | unchanged |
| climb input peak | 2.940 m/s | 2.940 m/s | 2.940 m/s | 2.940 m/s | unchanged |
| descent input peak | 2.925 m/s | 2.925 m/s | 2.925 m/s | 2.925 m/s | unchanged |
| yaw_right input peak | 79.893 °/s | 79.893 °/s | 79.893 °/s | 79.893 °/s | unchanged (done for now) |
| yaw_left input peak | 79.892 °/s | 79.892 °/s | 79.892 °/s | 79.894 °/s | unchanged (done for now) |

Yaw release behavior (from yaw-run settle windows):
- `125031`: `|yaw_rate| < 5°/s` in `0.26s`, `<1°/s` in `0.28s`
- `145236`: `|yaw_rate| < 5°/s` in `0.14s`, `<1°/s` in `0.14s` (faster only because held rate was incorrectly much lower)
- `164309`: `|yaw_rate| < 5°/s` in `0.26s`, `<1°/s` in `0.28s` (matches healthy pre-regression feel)

Interpretation:
1. The forward protocol input update (`forward_step` pitch `1.0`) worked: input-phase forward rose from ~2.112 to ~2.220 m/s.
2. Forward-specific brake slew improved release shape: carryover dropped from `0.84 m/s` (`180413`) to `0.50 m/s` (`183817`), while preserving onset.
3. Yaw behavior remains stable and should stay frozen.
4. Lateral and vertical traces are unchanged; remaining gap is primarily forward residual carryover and vertical protocol/slew interpretation.

---

## 2. Architecture and files you will touch most

| File | Purpose |
|---|---|
| `Assets/Scripts/Drone/Flight/DJIStyleFlightController.cs` | Core stabilized controller (velocity-error translation + yaw-rate control + acceleration slew). |
| `Assets/Scripts/Drone/Flight/DroneFlightModeConfig.cs` | Mode tuning schema consumed by the controller. |
| `Assets/Resources/Configs/DroneModeNormal.asset` | Main tuning values used by current benchmark loop. |
| `Assets/Resources/Benchmarks/*.asset` | Protocol timing + stick amplitudes per maneuver. |
| `Tools/analyze_airdata.py` | Analyzer for real-vs-sim metrics and reports. |
| `BENCHMARKS.md` | Benchmark harness protocol rules and analyzer usage. |
| `Docs/ClosedLoopValidation_Apr09_2026.md` | Apr 9 evidence/diagnosis summary. |

---

## 3. Yaw regression: exact root cause and fix

### Root cause (regressed path)

Active-stick yaw used:

- `yawError = targetYawRate - currentYawRate`
- `yawDamping = currentYawRate * yawStopAuthority`
- `rawYawAcceleration = yawError * yawCatchUpAuthority - yawDamping`

This creates a biased held-input equilibrium:

`equilibriumYawRate = targetYawRate * yawCatchUpAuthority / (yawCatchUpAuthority + yawStopAuthority)`

With Normal mode run values (`82`, `3.6`, `4.0`):

`82 * 3.6 / (3.6 + 4.0) = 38.84 °/s`

Exactly what `session_20260409_145236` showed (~38.83 °/s both directions).

### Fix (validated in 164309)

- Removed active-input full-rate damping term from the held-input branch.
- Held-input yaw now uses rate-error catch-up only (`yawError * yawCatchUpAuthority`) with the existing acceleration clamp + overshoot headroom.
- Neutral-stick branch still uses hard-stop braking (`MoveTowards` with `yawStopSpeed`) for DJI-like snap stop.

Result in benchmark evidence:
- held yaw restored to ~`79.9 °/s` both directions in `164309`
- release decay timing returned to the pre-regression profile (`~0.26s` to <5°/s, `~0.28s` to <1°/s)

---

## 4. Vertical interpretation (important)

Do **not** treat current climb/descent deltas as pure “needs more gain” evidence.

Why:
- Vertical maneuvers (`climb`, `descent`) are `1.0s` input windows.
- Commanded acceleration is slew-limited (`accelerationSlewRate`) before physics application.
- In short windows, peak speed can remain onset-ramp limited even if vertical P-gain/cap increase.

So current data remains consistent with **slew/protocol interaction** as the dominant factor for peak vertical mismatch.

---

## 5. Right-lateral trim status

Keep these in `DroneModeNormal.asset`:
- `lateralRightSpeedMultiplier: 0.88`
- `lateralRightAccelerationMultiplier: 0.92`

The improvement remained preserved from `145236` to `164309` with no new left-side regression.

---

## 6. Benchmark protocol facts to remember

Default F9 protocol (relevant durations):
1. `hover_hold` (7.0s segment + runner pre/settle)
2. `forward_step` (1.0s input)
3. `lateral_right` (2.5s input)
4. `lateral_left` (2.5s input)
5. `climb` (1.0s input)
6. `descent` (1.0s input)
7. `yaw_right` (1.0s input)
8. `yaw_left` (1.0s input)
9. `climb_long` (2.5s input)
10. `descent_long` (2.5s input)

This mixed 2.5s lateral vs 1.0s forward/vertical/yaw structure matters for interpreting “peak reached vs not reached” outcomes.

---

## 7. Analyzer commands

Single latest session (recommended quick check):

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session session_20260409_183817
```

Three-session comparison sweep:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv \
  --session session_20260409_125031 --session session_20260409_145236 --session session_20260409_164309
```

Output files (legacy names, always overwritten):
- `Docs/airdata_mar30_analysis.json`
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md`

---

Forward protocol provenance update (Apr 9 follow-up):
- Historical Mar 30 segmented calibration produced a legacy `forward_step` value of `+0.77`.
- Current analyzer outputs for the Apr 8 real baseline recommend `forward_step = +1.00` normalized.
- `Assets/Resources/Benchmarks/Maneuver_ForwardStep.asset` now uses `pitch: 1.0`; old sessions with `0.77` should be treated as legacy-protocol evidence.


Forward slew patch now in code/config (validated in 183817):
- `DroneFlightModeConfig` now exposes `forwardAccelerationSlewRate` and `forwardBrakeSlewRate` (<=0 falls back to global slew).
- `DJIStyleFlightController` now applies a forward-axis local-Z slew using those fields while preserving global slew behavior for lateral/vertical coupling.
- `DroneModeNormal.asset` sets `forwardAccelerationSlewRate: 6` and `forwardBrakeSlewRate: 11`; `session_20260409_183817` is the validating run that reduced carryover.

## 8. Open issues / uncertainties

1. `session_20260409_183817` validated the forward brake-slew patch and is a reasonable **done-for-now** stopping point.
2. Yaw is done for now; do not reopen yaw tuning unless new evidence appears.
3. Lateral asymmetry in real data may include environment/mechanical effects; sim right-only trim remains pragmatic.
4. Vertical mismatch remains likely onset-slew/protocol-limited under current 1.0s windows; needs protocol-aware diagnosis before raw-gain changes.


## 9. Next-phase scaffolding implemented on Apr 10, 2026

Implemented code/assets (no new benchmark session yet):
- Added `Maneuver_ClimbLong.asset` (`climb_long`, 2.5s throttle +1.0, protocol order 9).
- Added `Maneuver_DescentLong.asset` (`descent_long`, 2.5s throttle -1.0, protocol order 10).
- Kept original 1.0s `climb` and `descent` maneuvers in default protocol for direct same-session comparison.
- Added `BenchmarkRunner` runtime `protocolModeOverride` (None/Cine/Normal/Sport) so full protocol can be forced to Cine/Sport without duplicating maneuver assets.
- Added run-manifest `effective_mode` field to distinguish authored maneuver mode from runtime override mode.
- Added `Docs/AcceptanceCriteria.md` with pass/fail/blocked status snapshot and evidence anchors.

Immediate next run recommendation:
1. Run one F9 full protocol in Normal with new long vertical maneuvers and compare `climb` vs `climb_long`, `descent` vs `descent_long`.
2. Run one F9 in Cine and one F9 in Sport using `protocolModeOverride` and archive session zips/manifests.
3. Update `Docs/ClosedLoopValidation_Apr09_2026.md` with those outcomes and convert vertical hypothesis to a conclusion.
