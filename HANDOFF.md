# DJI Mini 4 Pro Flight Simulator — Project Handoff

> Last updated: 2026-04-10 after vertical-focused audit + vertical-only tuning patch.

## 1. Current truth snapshot

- **Latest decisive evidence session:** `BenchmarkRuns/session_20260410_120548.zip`
- This is the first archived **10-maneuver** Normal-mode run.
- Real reference log: `Apr-8th-2026-08-15AM-Flight-Airdata.csv`

Observed input-phase peaks (from protocol run CSVs):

| Category | 164309 | 170224 | 180413 | 183817 | 190056 | 120548 | Net story |
|---|---:|---:|---:|---:|---:|---:|---|
| forward_step input peak | 2.112 | 2.112 | 2.220 | 2.220 | 2.220 | 2.220 | forward onset improved vs pre-`pitch=1.0` and stable |
| forward carryover (full-input delta) | 0.288 | 0.288 | 0.840 | 0.500 | 0.500 | 0.500 | brake-slew patch holds |
| lateral_right input peak | 8.925 | 8.925 | 8.925 | 8.925 | 8.925 | 8.925 | still somewhat high vs real |
| lateral_left input peak | 9.812 | 9.812 | 9.812 | 9.812 | 9.812 | 9.812 | near real |
| yaw_right input peak (°/s) | 79.893 | 79.893 | 79.893 | 79.893 | 79.893 | 79.893 | fixed and stable |
| yaw_left input peak (°/s) | 79.892 | 79.892 | 79.892 | 79.894 | 79.894 | 79.894 | fixed and stable |
| climb_long input peak | - | - | - | - | - | 6.490 | newly measured, too high |
| descent_long input peak | - | - | - | - | - | 5.301 | newly measured, too high |

## 2. What changed in this patch

### Code/config
- **Vertical-only tuning:** `Assets/Resources/Configs/DroneModeNormal.asset`
  - `verticalAcceleration` reduced from `5.4` to `1.6`.

### Documentation updates
- `README.md`
- `Docs/AcceptanceCriteria.md`
- `Docs/CodexPlan_NextSteps.md`
- `Docs/ClosedLoopValidation_Apr09_2026.md`
- `HANDOFF.md` (this file)

## 3. Why this patch is vertical-only

Long-window vertical maneuvers (`climb_long`, `descent_long`, each 2.5s hold) are now measured in-archive and show large overshoot vs real. That makes vertical the clearest next axis.

This patch intentionally does **not** retune yaw, forward, or lateral.

## 4. Next run checklist

1. Run one F9 full protocol in Normal mode.
2. Confirm session archive has 10 runs including `climb_long` and `descent_long`.
3. Analyze with:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session <new_session_id>
```

4. Compare to `session_20260410_120548`:
   - `climb_long` should decrease from ~6.49 toward ~4.33 m/s.
   - `descent_long` should decrease from ~5.30 toward ~3.67 m/s.
   - yaw/forward/lateral should remain approximately unchanged.

## 5. Known open issues (after this patch, before new benchmark)

- Vertical patch is implemented but **not yet validated by a new Unity benchmark session in this environment**.
- Right-lateral remains somewhat high relative to real, but is deferred while vertical is the active focus.
- Cine/Sport full-protocol evidence remains pending.
