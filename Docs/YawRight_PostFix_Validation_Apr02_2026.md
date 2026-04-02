# Yaw-Right Post-Fix Validation — April 2, 2026 (session_20260402_163547)

## Scope and session roles

- **Baseline sim session:** `BenchmarkRuns/session_20260402_133209.zip`
- **Pre-yaw_right-fix reference session:** `BenchmarkRuns/session_20260402_161237.zip`
- **Post-yaw_right-fix rerun under test:** `BenchmarkRuns/session_20260402_163547.zip`
- **Real benchmark source:** `Mar-30th-2026-08-31AM-Flight-Airdata.csv`

Coverage check outcome:
- Baseline included protocol categories: 8/8
- Pre-fix included protocol categories: 8/8
- Post-fix included protocol categories: 8/8
- Missing categories in post-fix rerun vs baseline: none

Conclusion: protocol coverage is complete for the intended Normal-mode comparison, so validation claims below are based on complete-category data.

## Primary yaw_right comparison (shape-focused)

| Metric | Real | Pre-fix sim (161237) | Post-fix sim (163547) | Pre-fix Δ vs real | Post-fix Δ vs real | Direction |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.240 | 0.040 | 0.040 | -0.200 | -0.200 | unchanged |
| peak_rate | 84.600 | 84.358 | 84.340 | -0.242 | -0.260 | slight regression (very small) |
| max_accel | 312.000 | 764.593 | 638.006 | +452.593 | +326.006 | **improved** |
| overshoot | 4.600 | 79.107 | 79.789 | +74.507 | +75.189 | **regression** |
| settle_time_s | 0.000 | 1.360 | 1.380 | +1.360 | +1.380 | slight regression |

### Interpretation for yaw_right response shape

- **Onset / acceleration shape:** improved in the expected direction (max acceleration moved materially closer to real).
- **Release overshoot:** regressed slightly (overshoot increased instead of decreasing).
- **Settle behavior:** regressed slightly (settle tail lengthened by ~0.02s).
- **Stop behavior after release:** still not controlled enough; high overshoot and long settle persist.
- **Peak-rate closeness:** preserved at near-real level, with only a tiny negative movement.

Overall classification for `yaw_right`: **improved-but-still-off** (shape improvement only on onset acceleration; stop/release shape remains poor).

## yaw_left stability check (regression guard)

| Metric | Real | Pre-fix sim (161237) | Post-fix sim (163547) | Pre-fix Δ | Post-fix Δ | Direction |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.250 | 0.040 | 0.040 | -0.210 | -0.210 | unchanged |
| peak_rate | 70.750 | 66.599 | 66.599 | -4.151 | -4.151 | unchanged |
| max_accel | 177.500 | 603.638 | 603.638 | +426.138 | +426.138 | unchanged |
| overshoot | 6.900 | 61.895 | 61.895 | +54.995 | +54.995 | unchanged |
| settle_time_s | 0.000 | 1.320 | 1.320 | +1.320 | +1.320 | unchanged |

`yaw_left` remained stable with no measurable regression in this rerun, consistent with the right-only change intent.

## Secondary watchlist (non-dominant in this pass)

- `lateral_right`, `forward_step`, `climb`, and `descent` remained effectively unchanged vs pre-fix in this rerun.
- Provisional categories remain provisional; this pass does not upgrade their confidence or retune claim scope.

## Direct answers to required validation questions

- Did `yaw_right` improve in the correct direction? **Partially yes** (onset acceleration improved), but not across full response shape.
- Did `yaw_right` overshoot improve? **No** (slight regression).
- Did `yaw_right` settle behavior improve? **No** (slight regression).
- Did `yaw_right` onset/accel shape improve? **Yes** (max acceleration moved closer to real).
- Did the change preserve peak-rate closeness? **Yes, effectively preserved** (tiny regression only).
- Did `yaw_left` remain stable enough? **Yes** (no measurable change/regression).
- Is `yaw_right` now acceptable? **No** (still improved-but-still-off due to stop/release shape).

## Single next recommended step

**one more narrow `yaw_right` pass**

Reason: the target mismatch remains in right-yaw release/settle behavior; onset improved but overshoot/settle did not.
