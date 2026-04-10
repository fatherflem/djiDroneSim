# DJI Mini 4 Pro Flight Simulator — Project Handoff

> Last updated: 2026-04-10 after post-vertical-fix audit (`session_20260410_135709`).

## 1. Current truth snapshot

- **Latest decisive evidence session:** `BenchmarkRuns/session_20260410_135709.zip`
- This confirms the vertical-only patch from the prior iteration.
- Real reference log: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`

Observed input-phase peaks (from protocol run CSVs):

| Category | 164309 | 170224 | 180413 | 183817 | 190056 | 120548 | 135709 | Net story |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| forward_step input peak | 2.112 | 2.112 | 2.220 | 2.220 | 2.220 | 2.220 | 2.220 | forward onset improved vs pre-`pitch=1.0` and stable |
| forward carryover (full-input delta) | 0.288 | 0.288 | 0.840 | 0.500 | 0.500 | 0.500 | 0.500 | brake-slew patch holds |
| lateral_right input peak | 8.925 | 8.925 | 8.925 | 8.925 | 8.925 | 8.925 | 8.925 | still somewhat high vs real |
| lateral_left input peak | 9.812 | 9.812 | 9.812 | 9.812 | 9.812 | 9.812 | 9.812 | near real |
| yaw_right input peak (°/s) | 79.893 | 79.893 | 79.893 | 79.893 | 79.893 | 79.893 | 79.893 | fixed and stable |
| yaw_left input peak (°/s) | 79.892 | 79.892 | 79.892 | 79.894 | 79.894 | 79.894 | 79.892 | fixed and stable |
| climb_long input peak | - | - | - | - | - | 6.490 | 4.194 | improved into target zone |
| descent_long input peak | - | - | - | - | - | 5.301 | 3.578 | improved into target zone |

## 2. What changed in this patch

### Documentation updates (no additional controller retune)
- `README.md`
- `Docs/AcceptanceCriteria.md`
- `Docs/CodexPlan_NextSteps.md`
- `Docs/ClosedLoopValidation_Apr09_2026.md`
- `HANDOFF.md` (this file)

## 3. Current decision posture

Decision: **PATH A — freeze Normal tuning for now.**

Reasoning:
- Vertical no longer blocks acceptance after `135709`.
- Yaw remains healthy and stable.
- Forward shape is much better than pre-brake-slew state, with only a borderline onset miss remaining.
- Right-lateral remains high, but benefit/risk of another micro-patch is not compelling without stronger training-impact evidence.

## 4. Next run checklist (mode coverage, not Normal retune)

1. Run one F9 full protocol in Cine mode (`protocolModeOverride = Cine`).
2. Run one F9 full protocol in Sport mode (`protocolModeOverride = Sport`).
3. Confirm archives include all 10 maneuvers.
4. Analyze with:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session <new_session_id>
```

5. Compare mode runs against Normal anchor `session_20260410_135709`.

## 5. Known open issues

- Forward input-phase peak remains just outside threshold (~15.6% low).
- Right-lateral remains somewhat high relative to real (~20% high).
- Cine/Sport full-protocol evidence remains pending.
- Hover-box completion-time evidence remains pending.
