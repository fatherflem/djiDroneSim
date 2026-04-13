# DJI Mini 4 Pro Flight Simulator — Project Handoff

> Last updated: 2026-04-13 after expanded Normal benchmark audit (`session_20260413_142657`).

## 1. Current truth snapshot

- **Latest meaningful Normal benchmark:** `BenchmarkRuns/session_20260413_142657.zip`
- This run adds `backward_step` to the default Normal protocol (11 maneuvers total).
- Real reference log remains: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`

Observed input-phase peaks from `session_20260413_142657`:

| Category | Value | Interpretation |
|---|---:|---|
| forward_step input peak | 2.220 m/s | still slightly low vs ~2.63 real (borderline miss) |
| forward_step full-run peak | 2.720 m/s | carryover control remains stable |
| forward carryover (full - input) | 0.500 m/s | pass at threshold |
| backward_step input peak | 2.220 m/s | sim-side coverage now present (real-matched acceptance still provisional) |
| backward_step full-run peak | 2.720 m/s | symmetric with forward |
| backward carryover (full - input) | 0.500 m/s | pass on sim-side policy threshold |
| lateral_right input peak | 8.925 m/s | still somewhat high vs ~7.44 real |
| lateral_left input peak | 9.812 m/s | near real (~10.04) |
| yaw_right / yaw_left input peak | ~79.89 °/s | stable and healthy |
| climb_long input peak | 4.194 m/s | in target zone |
| descent_long input peak | 3.578 m/s | in target zone |

## 2. Decision posture

Decision: **PATH A — freeze Normal mode now.**

Why:
- No yaw regression, forward carryover regression, or vertical regression in the newest run.
- `142657` behaves like confirmation-plus-coverage (added backward protocol evidence), not a new tuning inflection point.
- Remaining misses (forward onset, right-lateral) are narrow enough that another retune cycle is higher risk than value right now.

## 3. What changed in this iteration

- Runtime tuning: **no changes** (freeze maintained).
- Documentation/acceptance updates to align repo truth with `session_20260413_142657`:
  - `README.md`
  - `Docs/AcceptanceCriteria.md`
  - `Docs/CodexPlan_NextSteps.md`
  - `Docs/ClosedLoopValidation_Apr09_2026.md`
  - `HANDOFF.md`

## 4. Reopen gates (Normal mode)

Reopen Normal tuning only if one of these occurs:
1. Pilot/training feedback shows the forward-onset or right-lateral mismatch harms instruction outcomes.
2. New baseline data materially changes target values/tolerances.
3. Cine/Sport or controller/physics changes cause measurable Normal regression.

## 5. Immediate next evidence work

- No immediate Normal retune benchmark is required.
- Highest-value next runs are still mode-coverage and scenario evidence:
  1. one archived full-protocol Cine run,
  2. one archived full-protocol Sport run,
  3. hover-box completion timing by mode.
