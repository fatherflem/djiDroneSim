# Yaw / Lateral Validation Update — April 2, 2026 (session_20260402_175500)

## Scope and session roles

- **Baseline sim session:** `BenchmarkRuns/session_20260402_133209.zip`
- **Most relevant pre-latest comparison session:** `BenchmarkRuns/session_20260402_171257.zip`
- **Newest validation session under test:** `BenchmarkRuns/session_20260402_175500.zip`
- **Real benchmark source:** `Mar-30th-2026-08-31AM-Flight-Airdata.csv`

Coverage check outcome for newest session (`175500`):
- Included protocol categories: 8/8
- Included order: `hover_hold`, `forward_step`, `lateral_right`, `lateral_left`, `climb`, `descent`, `yaw_right`, `yaw_left`
- Missing categories in newest rerun vs baseline: none

Conclusion: protocol coverage is complete for the intended Normal-mode comparison.

## Primary yaw_right comparison (pre-latest vs newest)

| Metric | Real | Pre-latest sim (171257) | Newest sim (175500) | Pre-latest Δ vs real | Newest Δ vs real | Direction (171257→175500) |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.240 | 0.060 | 0.060 | -0.180 | -0.180 | unchanged |
| peak_rate | 84.600 | 83.503 | 83.503 | -1.097 | -1.097 | unchanged |
| max_accel | 312.000 | 363.038 | 363.038 | +51.038 | +51.038 | unchanged |
| overshoot | 4.600 | 79.247 | 79.247 | +74.647 | +74.647 | unchanged |
| settle_time_s | 0.000 | 1.400 | 1.400 | +1.400 | +1.400 | unchanged |

### Interpretation for yaw_right response shape

- **Onset / acceleration shape:** no measurable change in this rerun.
- **Release overshoot:** no measurable change in this rerun; still far from real.
- **Settle behavior:** no measurable change in this rerun; still clearly off.
- **Peak-rate closeness:** no measurable change in this rerun.

Overall classification for `yaw_right`: **unchanged vs pre-latest, and still improved-but-still-off vs baseline**.

## Secondary checks requested for this pass

### yaw_left stability / acceptability

| Metric | Real | Pre-latest sim (171257) | Newest sim (175500) | Pre-latest Δ | Newest Δ | Direction |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.250 | 0.060 | 0.060 | -0.190 | -0.190 | unchanged |
| peak_rate | 70.750 | 65.924 | 65.924 | -4.826 | -4.826 | unchanged |
| max_accel | 177.500 | 286.636 | 286.636 | +109.136 | +109.136 | unchanged |
| overshoot | 6.900 | 62.668 | 62.668 | +55.768 | +55.768 | unchanged |
| settle_time_s | 0.000 | 1.420 | 1.420 | +1.420 | +1.420 | unchanged |

`yaw_left` remains **not acceptable**.

### lateral_right acceptability

| Metric | Real | Pre-latest sim (171257) | Newest sim (175500) | Pre-latest Δ | Newest Δ | Direction |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.000 | 0.120 | 0.120 | +0.120 | +0.120 | unchanged |
| peak_rate | 0.200 | 0.117 | 0.117 | -0.083 | -0.083 | unchanged |
| max_accel | 0.333 | 0.141 | 0.141 | -0.192 | -0.192 | unchanged |
| overshoot | 0.200 | 0.108 | 0.108 | -0.092 | -0.092 | unchanged |
| settle_time_s | 1.100 | 1.280 | 1.280 | +0.180 | +0.180 | unchanged |

`lateral_right` remains **improved-but-still-off** and **not acceptable**.

## Strong vs provisional category synthesis

- Strong categories (`lateral_right`, `yaw_right`, `yaw_left`) remain **improved-but-still-off**.
- Provisional categories (`forward_step`, `lateral_left`, `climb`, `descent`) **remain provisional** in this pass.
- In the regenerated closed-loop output for `175500`, the highest high-confidence divergence score remains **`yaw_left`**.

## Direct answers

- Did `yaw_right` improve? **No measurable improvement vs `171257`; unchanged in `175500`.**
- Did `yaw_right` overshoot improve? **No; unchanged.**
- Did `yaw_right` settle behavior improve? **No; unchanged and still off.**
- Did `yaw_right` onset/accel shape improve? **No; unchanged.**
- Did `yaw_left` remain stable enough? **Stable in the sense of repeatability, but not acceptable vs real benchmark.**
- Is `lateral_right` now acceptable? **No.**
- Is `yaw_left` now acceptable? **No.**
- Is `yaw_right` still the strongest high-confidence mismatch? **No (`yaw_left` remains strongest in this regenerated pass).**
- Did provisional categories strengthen or remain provisional? **Remain provisional.**
- Is Normal-mode benchmark fidelity now good enough to move on? **No.**

## Single next recommended action

**one more narrow `yaw_right` pass**

Reason: this latest rerun is effectively flat relative to `171257`; a tightly scoped right-yaw pass is still the most direct way to reduce the remaining non-provisional yaw divergence without broad retuning.
