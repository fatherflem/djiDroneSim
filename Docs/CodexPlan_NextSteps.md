# Codex Next Steps Plan (Vertical-Focused Pass)

_Last refreshed: April 10, 2026 after auditing `session_20260410_120548`._

## 1) Where the project has been (closed-loop sequence)

1. `session_20260409_145236`: yaw held-input regression discovered (~38.8 °/s).
2. `session_20260409_164309` + `170224`: yaw control fixed (~79.9 °/s) with healthy release timing.
3. `session_20260409_180413`: forward protocol amplitude corrected to `forward_step pitch=1.0`.
4. `session_20260409_183817` + `190056`: forward brake-slew patch reduced carryover to ~0.500 m/s while preserving onset.
5. `session_20260410_120548`: first archived **10-maneuver** Normal run with `climb_long` / `descent_long`.

## 2) What newest evidence proved

From `session_20260410_120548` input-phase peaks:

- `forward_step`: ~2.220 m/s (carryover ~0.500 m/s full-run delta)
- `lateral_right`: ~8.925 m/s
- `lateral_left`: ~9.812 m/s
- `yaw_right` / `yaw_left`: ~79.89 °/s
- `climb_long`: ~6.490 m/s
- `descent_long`: ~5.301 m/s

Interpretation:
- Yaw is effectively done for now.
- Forward is improved and stable enough to defer.
- Right-lateral remains somewhat aggressive, but is not the strongest blocker.
- Long-window vertical is now a measured mismatch (no longer “blocked pending evidence”).

## 3) Why vertical is the next one-axis target

Short-window (`1.0s`) vertical maneuvers were ambiguous because they mixed onset ramp effects with steady-state behavior. The new `2.5s` holds reveal sustained response, and both climb/descent now overshoot real peaks by ~44–50%.

This makes vertical the clearest next focused pass, with yaw/forward/lateral frozen unless a new regression appears.

## 4) Current patch hypothesis (this repo state)

Hypothesis: Normal-mode `verticalAcceleration` was high enough to drive overly strong sustained vertical response in long holds; reducing vertical authority should lower both `climb_long` and `descent_long` peaks without touching other axes.

Implemented in this pass:
- `Assets/Resources/Configs/DroneModeNormal.asset`
  - `verticalAcceleration: 5.4 -> 1.6`

Intentionally not changed:
- Yaw tuning
- Forward tuning (including forward-specific slew)
- Lateral tuning
- Benchmark timing/protocol assets

## 5) Exact next benchmark to run

Run one Normal-mode F9 full protocol session and compare against `session_20260410_120548`.

Recommended command for post-run analysis:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session <new_session_id>
```

Optional trend command:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv \
  --session session_20260409_164309 \
  --session session_20260409_170224 \
  --session session_20260409_180413 \
  --session session_20260409_183817 \
  --session session_20260409_190056 \
  --session session_20260410_120548 \
  --session <new_session_id>
```

## 6) Success / failure gates for the next run

### Success
- `climb_long` and `descent_long` both move materially downward toward real values (~4.33 / ~3.67 m/s).
- Yaw peaks stay near ~79.9 °/s.
- Forward input-phase (~2.22 m/s) and carryover (~0.50 m/s) remain approximately stable.

### Failure / follow-up
- If vertical remains too high: apply one more vertical-only reduction (likely `verticalAcceleration` or vertical cap path), then re-run.
- If vertical undershoots strongly: partially restore `verticalAcceleration` while preserving the same one-axis approach.
- If another axis regresses: stop and investigate before additional tuning.
