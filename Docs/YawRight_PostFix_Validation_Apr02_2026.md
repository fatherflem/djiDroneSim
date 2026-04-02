# Yaw Asymmetry Validation Update — April 2, 2026 (session_20260402_182438)

## Scope and session roles

- **Baseline sim session:** `BenchmarkRuns/session_20260402_133209.zip`
- **Pre-latest comparison session (repo-grounded):** `BenchmarkRuns/session_20260402_175500.zip`
- **Newest post-yaw-asymmetry rerun under test:** `BenchmarkRuns/session_20260402_182438.zip`
- **Real benchmark source:** `Mar-30th-2026-08-31AM-Flight-Airdata.csv`

Why `175500 -> 182438` is used for pre/post:
- `175500` is the last complete full-protocol run immediately before the yaw-asymmetry follow-up rerun.
- `182438` is the explicitly requested post-change rerun.
- The regenerated closed-loop comparison confirms both sessions contain the same 8/8 protocol categories, enabling a like-for-like yaw asymmetry check.

Coverage check outcome for newest session (`182438`):
- Included protocol categories: 8/8
- Included order: `hover_hold`, `forward_step`, `lateral_right`, `lateral_left`, `climb`, `descent`, `yaw_right`, `yaw_left`
- Missing categories in newest rerun vs baseline: none
- Missing full-protocol runs in newest rerun vs baseline: none

Conclusion: protocol coverage is complete for the intended Normal-mode validation pass.

## Primary yaw asymmetry comparison (pre-latest vs newest)

### yaw_left (priority target in latest asymmetry pass)

| Metric | Real | Pre-latest sim (175500) | Newest sim (182438) | Pre-latest Δ vs real | Newest Δ vs real | Direction (175500→182438) |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.250 | 0.060 | 0.060 | -0.190 | -0.190 | unchanged |
| peak_rate | 70.750 | 65.924 | 65.924 | -4.826 | -4.826 | unchanged |
| max_accel | 177.500 | 286.636 | 286.636 | +109.136 | +109.136 | unchanged |
| overshoot | 6.900 | 62.668 | 62.668 | +55.768 | +55.768 | unchanged |
| settle_time_s | 0.000 | 1.420 | 1.420 | +1.420 | +1.420 | unchanged |

Interpretation:
- **Did yaw_left improve?** No measurable improvement versus `175500`.
- **Yaw_left overshoot:** unchanged and still strongly high vs real.
- **Yaw_left settle behavior:** unchanged and still too long vs real.
- **Yaw_left onset/accel shape:** unchanged (same delay and same accel mismatch).

### yaw_right (must remain stable while yaw_left is targeted)

| Metric | Real | Pre-latest sim (175500) | Newest sim (182438) | Pre-latest Δ vs real | Newest Δ vs real | Direction (175500→182438) |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.240 | 0.060 | 0.060 | -0.180 | -0.180 | unchanged |
| peak_rate | 84.600 | 83.503 | 83.503 | -1.097 | -1.097 | unchanged |
| max_accel | 312.000 | 363.038 | 363.038 | +51.038 | +51.038 | unchanged |
| overshoot | 4.600 | 79.247 | 79.247 | +74.647 | +74.647 | unchanged |
| settle_time_s | 0.000 | 1.400 | 1.400 | +1.400 | +1.400 | unchanged |

Interpretation:
- **Did yaw_right remain stable enough?** Yes in repeatability terms (no measurable drift/regression from `175500`).
- **Did yaw_right improve?** No measurable improvement in the newest rerun.
- `yaw_right` remains materially off on overshoot and settle shape vs real.

## Yaw consistency and priority conclusions

- **Overall left/right yaw consistency improved?** No measurable change; both sides are effectively identical to `175500`.
- **Is yaw_left still the strongest high-confidence mismatch?** Yes. In regenerated divergence ranking, `yaw_left` remains highest among strong-confidence categories.
- **Is yaw_right still a major issue?** Yes. It remains high-divergence on overshoot/settle, even though `yaw_left` is worse overall.

## Secondary categories (kept secondary in this pass)

- `lateral_right`: unchanged versus `175500`; still improved-but-still-off and not yet acceptable.
- `forward_step`, `climb`, `descent`: unchanged versus `175500` in this rerun.
- `lateral_left`: unchanged and still provisional due confidence/provenance constraints.

## Provisional confidence handling

- Provisional categories (`forward_step`, `lateral_left`, `climb`, `descent`) remain provisional in this pass.
- Newest rerun does not provide new evidence strong enough to upgrade those confidence/provenance classifications.

## Direct answers

- Was `session_20260402_182438.zip` used directly? **Yes, from `BenchmarkRuns/session_20260402_182438.zip`.**
- Is the newest protocol coverage complete? **Yes (8/8, full intended order present).**
- Did `yaw_left` improve? **No measurable improvement vs `175500`.**
- Did `yaw_right` remain stable enough? **Yes for stability/repeatability, but still not acceptable vs real shape.**
- Is `yaw_left` still the strongest high-confidence mismatch? **Yes.**
- Is `lateral_right` now the better next target? **No; yaw remains the stronger high-confidence problem.**
- Are provisional categories still provisional? **Yes.**

## Single next recommended action

**one more narrow yaw pass**

Reason: the `175500 -> 182438` post-change comparison is effectively flat for both `yaw_left` and `yaw_right`, so one tightly scoped yaw-only iteration is still the most evidence-aligned next step before shifting focus.
