# Simulator Fidelity Acceptance Criteria

## Purpose
This is a classroom training simulator. The goal is "representative enough that students build correct muscle memory," not telemetry-perfect replication.

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

## Current Status (as of April 10, 2026)

Evidence anchor: `session_20260410_120548` (latest decisive 10-maneuver Normal run), with trend context from `164309`/`170224`/`180413`/`183817`/`190056`.

| Criterion | Status | Evidence / Note |
|---|---|---|
| Forward input-phase peak within 15% | ❌ Fail (borderline) | Sim peak 2.220 m/s vs real ~2.63 m/s (~15.6% low), just outside threshold. |
| Forward post-release carryover ≤ 0.5 m/s | ✅ Pass | Carryover stabilized at ~0.500 m/s in `session_20260409_183817`, `190056`, and `20260410_120548`. |
| Lateral right within 15% | ❌ Fail | Sim 8.925 m/s vs real ~7.44 m/s (~20% high). |
| Lateral left within 15% | ✅ Pass | Sim 9.812 m/s vs real ~10.04 m/s (~2% low). |
| Climb within 15% (2.5s window) | ❌ Fail | `climb_long` now measured: sim 6.490 m/s vs real ~4.33 m/s (~50% high). |
| Descent within 15% (2.5s window) | ❌ Fail | `descent_long` now measured: sim 5.301 m/s vs real ~3.67 m/s (~44% high). |
| Yaw held-input within 5% | ✅ Pass | ~79.9°/s sim vs ~82°/s real (~3% low) both directions. |
| Yaw release settle timing | ✅ Pass | ~0.26s to <5°/s matches healthy reference profile. |
| Hover horizontal drift ≤ 1.5 m/s RMS | 🚧 Pending | Metric not yet exported as a dedicated acceptance KPI. |
| Cine mode coverage | 🚧 Pending | Benchmark runner now supports runtime mode override; Cine protocol session not yet archived. |
| Sport mode coverage | 🚧 Pending | Benchmark runner now supports runtime mode override; Sport protocol session not yet archived. |
| Hover-box drill completable within 60s | 🚧 Pending | Playtest pass still needed in Normal/Cine/Sport. |

## Sign-off
Each criterion must reference a specific benchmark session ID as evidence.

## Immediate next benchmark target (vertical-only patch)

- Patch hypothesis: long-window vertical overshoot is primarily caused by excessive vertical authority from `verticalAcceleration` in Normal mode, interacting with slew-limited acceleration handoff during a 2.5s hold.
- Patch change: `DroneModeNormal.asset` `verticalAcceleration` reduced `5.4 -> 1.6` (no yaw/forward/lateral changes in this pass).
- Expected outcome on next F9 Normal run:
  - `climb_long` should move down from ~6.49 toward ~4.33 m/s.
  - `descent_long` should move down from ~5.30 toward ~3.67 m/s.
  - `forward_step`, `yaw_left/right`, and lateral peaks should remain near current values.
- Success gate for this patch: both long vertical peaks improve materially (directionally correct and clearly lower) without regressions in yaw/forward behavior.
