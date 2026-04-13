# Closed-Loop Validation (Apr 9–13, 2026)

## Scope and latest evidence

- Real benchmark CSV: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`
- Key session anchors:
  - `session_20260409_145236` (yaw-regressed)
  - `session_20260409_164309` and `session_20260409_170224` (yaw fixed)
  - `session_20260409_180413` (forward amplitude correction)
  - `session_20260409_183817` and `session_20260409_190056` (forward brake-slew fix + confirmation)
  - `session_20260410_120548` (first archived 10-maneuver run with long vertical maneuvers)
  - `session_20260410_135709` (vertical-only correction validated)
  - `session_20260413_142657` (**latest 11-maneuver Normal run; backward added**)

Latest meaningful Normal benchmark evidence is `session_20260413_142657`.

## Journey summary with evidence

1. **Yaw regression discovered (`145236`)**: held yaw collapsed to ~38.8 °/s because active-input damping biased steady-state yaw below command.
2. **Yaw fixed (`164309`, `170224`)**: held yaw restored to ~79.9 °/s; release timing stayed crisp (~0.26 s to <5 °/s).
3. **Forward amplitude corrected (`180413`)**: `forward_step` switched to pitch=1.0; onset improved but carryover spiked (~0.84 m/s).
4. **Forward brake-slew patch (`183817`, `190056`)**: carryover cut to ~0.50 m/s while preserving onset.
5. **Long vertical protocol validated (`120548`)**: `climb_long`/`descent_long` were clearly too strong (6.490 / 5.301 m/s).
6. **Vertical-only correction validated (`135709`)**: long vertical reduced to 4.194 / 3.578 m/s with forward/yaw/lateral unchanged.
7. **Expanded protocol confirmation (`142657`)**: added `backward_step`, producing an 11-maneuver Normal run that confirms the same tuned state.

## Latest run metrics (`session_20260413_142657`)

Input-phase peaks:
- `forward_step`: 2.220 m/s (full-run 2.720; carryover 0.500 m/s)
- `backward_step`: 2.220 m/s (full-run 2.720; carryover 0.500 m/s)
- `lateral_right`: 8.925 m/s
- `lateral_left`: 9.812 m/s
- `yaw_right`: 79.892 °/s
- `yaw_left`: 79.892 °/s
- `climb_long`: 4.194 m/s
- `descent_long`: 3.578 m/s

Interpretation:
- yaw remains solved,
- forward carryover fix remains solved,
- vertical correction remains solved,
- backward now has simulator protocol coverage,
- remaining narrow misses are still forward onset and right-lateral realism.

## Decision

**PATH A — freeze Normal mode**.

`142657` is primarily a confirmation/coverage run (important protocol expansion, no regression), not evidence for another retune cycle.

## Reopen conditions

Only reopen Normal if:
1. training/pilot feedback shows meaningful impact from forward-onset or right-lateral mismatch,
2. target dataset/tolerance policy changes, or
3. non-Normal work introduces measurable Normal regression.
