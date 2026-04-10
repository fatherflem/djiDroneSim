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

Evidence anchor: `session_20260409_183817` plus Apr 9 validation analysis.

| Criterion | Status | Evidence / Note |
|---|---|---|
| Forward input-phase peak within 15% | ❌ Fail | Sim peak 2.22 m/s vs real ~2.63 m/s (~15.6% low), just outside threshold. |
| Forward post-release carryover ≤ 0.5 m/s | ✅ Pass | Carryover reduced to 0.50 m/s in `session_20260409_183817`. |
| Lateral right within 15% | ❌ Fail | Sim 8.925 m/s vs real ~7.44 m/s (~20% high). |
| Lateral left within 15% | ✅ Pass | Sim 9.812 m/s vs real ~10.04 m/s (~2% low). |
| Climb within 15% (2.5s window) | 🚧 Blocked | `climb_long` maneuver added; run pending to gather 2.5s-window evidence. |
| Descent within 15% (2.5s window) | 🚧 Blocked | `descent_long` maneuver added; run pending to gather 2.5s-window evidence. |
| Yaw held-input within 5% | ✅ Pass | ~79.9°/s sim vs ~82°/s real (~3% low) both directions. |
| Yaw release settle timing | ✅ Pass | ~0.26s to <5°/s matches healthy reference profile. |
| Hover horizontal drift ≤ 1.5 m/s RMS | 🚧 Pending | Metric not yet exported as a dedicated acceptance KPI. |
| Cine mode coverage | 🚧 Pending | Benchmark runner now supports runtime mode override; Cine protocol session not yet archived. |
| Sport mode coverage | 🚧 Pending | Benchmark runner now supports runtime mode override; Sport protocol session not yet archived. |
| Hover-box drill completable within 60s | 🚧 Pending | Playtest pass still needed in Normal/Cine/Sport. |

## Sign-off
Each criterion must reference a specific benchmark session ID as evidence.
