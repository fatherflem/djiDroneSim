# Codex Next Steps Plan (Post Expanded Normal Benchmark)

_Last refreshed: April 13, 2026 after auditing `session_20260413_142657`._

## 1) Closed-loop journey (verified)

1. `session_20260409_145236`: yaw held-input regression discovered (~38.8 °/s).
2. `session_20260409_164309` + `170224`: yaw fixed (~79.9 °/s) with crisp release.
3. `session_20260409_180413`: forward protocol amplitude corrected to `forward_step pitch=1.0`; onset improved but carryover worsened.
4. `session_20260409_183817` + `190056`: forward brake-slew patch reduced carryover to ~0.500 m/s and held that gain.
5. `session_20260410_120548`: first archived 10-maneuver Normal run; long vertical mismatch became explicit (`climb_long`/`descent_long` too strong).
6. `session_20260410_135709`: vertical-only correction validated; long vertical moved into range without cross-axis regression.
7. `session_20260413_142657`: expanded to an 11-maneuver Normal protocol by adding `backward_step`; confirms the same tuning state.

## 2) What the newest run added

From `session_20260413_142657` input-phase peaks:
- `forward_step`: 2.220 m/s (full-run 2.720; carryover 0.500)
- `backward_step`: 2.220 m/s (full-run 2.720; carryover 0.500)
- `lateral_right`: 8.925 m/s
- `lateral_left`: 9.812 m/s
- `yaw_right` / `yaw_left`: ~79.89 °/s
- `climb_long`: 4.194 m/s
- `descent_long`: 3.578 m/s

Interpretation: coverage expanded, behavior unchanged; this is a confirmation-plus-coverage session, not a new tuning breakthrough.

## 3) Decision: PATH A (freeze Normal mode)

Why freeze now:
- Solved axes remain solved (yaw and long vertical hold).
- Forward carryover fix remains intact.
- Remaining misses are narrow and known (forward onset slightly low, right-lateral somewhat high).
- Another Normal retune cycle currently offers limited upside with non-trivial regression risk.

## 4) Reopen criteria (strict)

Allow one future Normal micro-patch only if:
- pilot/training evidence shows one remaining mismatch affects instruction outcomes, or
- new baseline data materially shifts targets/tolerances, or
- another code change causes measurable Normal regression.

If reopened, modify one axis only and benchmark immediately.

## 5) Next evidence work (non-Normal retune)

1. Archive one full-protocol Cine run (`protocolModeOverride = Cine`).
2. Archive one full-protocol Sport run (`protocolModeOverride = Sport`).
3. Capture hover-box completion times by mode.

## 6) Analyzer commands

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session session_20260413_142657
```

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv \
  --session session_20260410_135709 \
  --session session_20260413_142657
```
