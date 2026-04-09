# DJI Mini 4 Pro Flight Simulator — Project Handoff

> Last updated: 2026-04-09 after auditing benchmark `session_20260409_145236` (latest key evidence) and patching a yaw held-input equilibrium regression.

---

## 1. Current truth snapshot

- **Latest decisive sim session:** `BenchmarkRuns/session_20260409_145236.zip`.
- Previous comparison anchor: `BenchmarkRuns/session_20260409_125031.zip`.
- Real reference log: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`.

Observed `125031 -> 145236` peak changes:

| Category | 125031 peak | 145236 peak | Net |
|---|---:|---:|---:|
| forward_step | 2.112 m/s | 2.112 m/s | 0.000 |
| lateral_right | 9.812 m/s | 8.925 m/s | **-0.887 (improved)** |
| lateral_left | 9.812 m/s | 9.812 m/s | 0.000 |
| climb | 2.940 m/s | 2.940 m/s | 0.000 |
| descent | 2.911 m/s | 2.925 m/s | +0.014 |
| yaw_right | 79.597 °/s | 38.829 °/s | **-40.768 (regressed)** |
| yaw_left | 79.596 °/s | 38.831 °/s | **-40.765 (regressed)** |

Interpretation:
1. Right-lateral trim was successful.
2. Vertical authority increases did not change peak climb/descent in this protocol window.
3. New yaw active-input damping introduced a structural held-input undershoot.

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

### Root cause (pre-patch)

Active-stick yaw used:

- `yawError = targetYawRate - currentYawRate`
- `yawDamping = currentYawRate * yawStopAuthority`
- `rawYawAcceleration = yawError * yawCatchUpAuthority - yawDamping`

This creates a biased held-input equilibrium:

`equilibriumYawRate = targetYawRate * yawCatchUpAuthority / (yawCatchUpAuthority + yawStopAuthority)`

With Normal mode run values (`82`, `3.6`, `4.0`):

`82 * 3.6 / (3.6 + 4.0) = 38.84 °/s`

Exactly what `session_20260409_145236` showed (~38.83 °/s both directions).

### Fix applied

- Removed active-input full-rate damping term from the held-input branch.
- Held-input yaw now uses rate-error catch-up only (`yawError * yawCatchUpAuthority`) with the existing acceleration clamp + overshoot headroom.
- Neutral-stick branch still uses hard-stop braking (`MoveTowards` with `yawStopSpeed`) for crisp DJI-like release behavior.

This restores the correct steady-state target under sustained yaw command while preserving controlled stop on release.

---

## 4. Vertical interpretation (important)

Do **not** treat current climb/descent deltas as pure “needs more gain” evidence.

Why:
- Vertical maneuvers (`climb`, `descent`) are `1.0s` input windows.
- Commanded acceleration is slew-limited (`accelerationSlewRate`) before physics application.
- In short windows, peak speed can remain onset-ramp limited even if vertical P-gain/cap increase.

So the current data is consistent with **slew/protocol interaction** as the dominant factor for peak vertical mismatch.

No vertical tuning code changes were made in this patch.

---

## 5. Right-lateral trim status

Keep these in `DroneModeNormal.asset`:
- `lateralRightSpeedMultiplier: 0.88`
- `lateralRightAccelerationMultiplier: 0.92`

They produced the only clear translational improvement in `145236` (right-lateral peak dropped toward real while left stayed stable).

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

This mixed 2.5s lateral vs 1.0s vertical/yaw structure matters for interpreting “peak reached vs not reached” outcomes.

---

## 7. Analyzer commands

Single latest session (recommended quick check):

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session session_20260409_145236
```

A/B compare prior vs latest session:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv \
  --session session_20260409_125031 --session session_20260409_145236
```

Output files (legacy names, always overwritten):
- `Docs/airdata_mar30_analysis.json`
- `Docs/Airdata_Mar30_2026_Benchmark_Summary.md`

---

## 8. Open issues / uncertainties

1. `forward_step` still undershoots real peak (~2.11 vs 2.63 m/s) and remains partially provisional in amplitude provenance.
2. Lateral asymmetry in real data may include environment/mechanical effects; sim right-only trim is currently pragmatic, not “proven physics truth.”
3. Vertical evaluation depends on objective:
   - if goal is onset profile, current protocol may already be informative;
   - if goal is peak-speed matching, 1.0s step windows may be too short under slew-limited control.

