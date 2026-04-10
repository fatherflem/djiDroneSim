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

Evidence anchor: `session_20260410_135709` (latest decisive 10-maneuver Normal run), with trend context from `145236`/`164309`/`170224`/`180413`/`183817`/`190056`/`120548`.

| Criterion | Status | Evidence / Note |
|---|---|---|
| Forward input-phase peak within 15% | ❌ Fail (borderline) | Sim peak 2.220 m/s vs real ~2.63 m/s (~15.6% low), just outside threshold in `session_20260410_135709`. |
| Forward post-release carryover ≤ 0.5 m/s | ✅ Pass | Carryover remains ~0.500 m/s in `session_20260409_183817`, `190056`, `20260410_120548`, and `20260410_135709`. |
| Lateral right within 15% | ❌ Fail | Sim 8.925 m/s vs real ~7.44 m/s (~20% high). |
| Lateral left within 15% | ✅ Pass | Sim 9.812 m/s vs real ~10.04 m/s (~2% low). |
| Climb within 15% (2.5s window) | ✅ Pass | `climb_long` improved to 4.194 m/s vs real ~4.33 m/s (~3% low) in `session_20260410_135709`. |
| Descent within 15% (2.5s window) | ✅ Pass | `descent_long` improved to 3.578 m/s vs real ~3.67 m/s (~2.5% low) in `session_20260410_135709`. |
| Yaw held-input within 5% | ✅ Pass | ~79.9°/s sim vs ~82°/s real (~3% low) both directions. |
| Yaw release settle timing | ✅ Pass | ~0.26s to <5°/s in `session_20260410_135709`, matching healthy reference profile. |
| Hover horizontal drift ≤ 1.5 m/s RMS | ✅ Pass (for benchmark hover_hold) | `session_20260410_135709` hover_hold input-phase horizontal RMS is ~0.000 m/s (well under 1.5). |
| Cine mode coverage | 🚧 Pending | Benchmark runner now supports runtime mode override; Cine protocol session not yet archived. |
| Sport mode coverage | 🚧 Pending | Benchmark runner now supports runtime mode override; Sport protocol session not yet archived. |
| Hover-box drill completable within 60s | 🚧 Pending | Playtest pass still needed in Normal/Cine/Sport. |

## Sign-off
Each criterion must reference a specific benchmark session ID as evidence.

## Immediate next benchmark target (freeze posture)

- No immediate Normal-mode retune benchmark is required.
- Reopen Normal tuning only if one of these happens:
  1. New pilot feedback shows the remaining gaps (forward onset or right-lateral) produce meaningful training issues.
  2. A new baseline dataset revises target values or tolerance policy.
  3. A non-Normal change (Cine/Sport or physics/core refactor) causes measurable Normal regression.
- Highest-value next evidence is now mode coverage:
  - one archived full-protocol Cine run,
  - one archived full-protocol Sport run,
  - and hover-box completion timing by mode.
