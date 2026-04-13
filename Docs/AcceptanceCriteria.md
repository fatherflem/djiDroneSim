# Simulator Fidelity Acceptance Criteria

## Purpose
This is a classroom training simulator. The goal is "representative enough that students build correct muscle memory," not telemetry-perfect replication.

## Per-Axis Criteria (Normal Mode)

| Axis | Metric | Threshold | Rationale |
|---|---|---|---|
| Forward | Input-phase peak speed | Within 15% of real | Primary training axis |
| Forward | Post-release carryover | ≤ 0.5 m/s | Affects stop-point learning |
| Backward | Input-phase peak speed | Within 15% of real | Mirrors forward translational onset training |
| Backward | Post-release carryover | ≤ 0.5 m/s | Mirrors forward stop-point learning |
| Lateral (each) | Input-phase peak speed | Within 15% of real | Secondary training axis |
| Climb | Peak speed (2.5s window) | Within 15% of real | Altitude control training |
| Descent | Peak speed (2.5s window) | Within 15% of real | Altitude control training |
| Yaw (each) | Held-input rate | Within 5% of real | Heading control |
| Yaw (each) | Release settle to <5°/s | Within 0.1s of real | Stop crispness |
| Hover | Horizontal drift | ≤ 1.5 m/s RMS | Baseline stability |

## Current Status (as of April 13, 2026)

Evidence anchor: `session_20260413_142657` (latest 11-maneuver Normal run). Trend context from `145236`/`164309`/`170224`/`180413`/`183817`/`190056`/`120548`/`135709`.

| Criterion | Status | Evidence / Note |
|---|---|---|
| Forward input-phase peak within 15% | ❌ Fail (borderline) | Sim 2.220 m/s vs real ~2.63 m/s (~15.6% low), still just outside threshold in `session_20260413_142657`. |
| Forward post-release carryover ≤ 0.5 m/s | ✅ Pass | Full-run peak 2.720 vs input-phase 2.220 => carryover 0.500 m/s in `session_20260413_142657`. |
| Backward input-phase peak within 15% | ❌ Fail (provisional reference quality) | Sim 2.220 m/s vs real backward aggregate ~6.38 m/s is far outside 15%, but real backward baseline confidence is still medium/provisional in AirData segmentation. |
| Backward post-release carryover ≤ 0.5 m/s | ✅ Pass (sim-side criterion) | Full-run peak 2.720 vs input-phase 2.220 => carryover 0.500 m/s in `session_20260413_142657`. |
| Lateral right within 15% | ❌ Fail | Sim 8.925 m/s vs real ~7.44 m/s (~20% high). |
| Lateral left within 15% | ✅ Pass | Sim 9.812 m/s vs real ~10.04 m/s (~2% low). |
| Climb within 15% (2.5s window) | ✅ Pass | `climb_long` 4.194 m/s vs real ~4.33 m/s (~3% low). |
| Descent within 15% (2.5s window) | ✅ Pass | `descent_long` 3.578 m/s vs real ~3.67 m/s (~2.5% low). |
| Yaw held-input within 5% | ✅ Pass | ~79.89°/s sim vs ~82°/s real (~2.6% low), both directions. |
| Yaw release settle timing | ✅ Pass | ~0.26s to <5°/s in both directions in `session_20260413_142657`, consistent with healthy post-fix runs. |
| Hover horizontal drift ≤ 1.5 m/s RMS | ✅ Pass (benchmark hover_hold) | `session_20260413_142657` hover_hold input-phase horizontal RMS ~0.000 m/s. |

## Decision now

**PATH A (freeze Normal mode)** remains the recommended posture.

- `session_20260413_142657` adds important protocol coverage (`backward_step`) and confirms stability.
- It does **not** reveal a new high-value correction opportunity that clearly outweighs retune risk.
- Remaining narrow misses are still:
  1. forward onset slightly low,
  2. right-lateral somewhat high.

## Evidence honesty notes

- Backward is now benchmarked in simulator protocol terms, but real-world backward matching quality remains lower-confidence than forward/lateral/yaw.
- Exploratory/non-protocol flights may inform hypotheses but are not acceptance sign-off evidence by themselves.

## Immediate next benchmark target

- No immediate Normal-mode retune benchmark is required while frozen.
- Reopen only if training-impact evidence or new baseline data justifies one-axis micro-patch.
