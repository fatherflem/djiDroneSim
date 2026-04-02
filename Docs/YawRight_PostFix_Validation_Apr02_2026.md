# Yaw / Lateral Validation Update — April 2, 2026 (session_20260402_171257)

## Scope and session roles

- **Baseline sim session:** `BenchmarkRuns/session_20260402_133209.zip`
- **Most relevant pre-latest comparison session:** `BenchmarkRuns/session_20260402_163547.zip`
- **Newest validation session under test:** `BenchmarkRuns/session_20260402_171257.zip`
- **Real benchmark source:** `Mar-30th-2026-08-31AM-Flight-Airdata.csv`

Coverage check outcome for newest session (`171257`):
- Included protocol categories: 8/8
- Included order: `hover_hold`, `forward_step`, `lateral_right`, `lateral_left`, `climb`, `descent`, `yaw_right`, `yaw_left`
- Missing categories in newest rerun vs baseline: none

Conclusion: protocol coverage is complete for the intended Normal-mode comparison, so the claims below are based on complete-category data.

## Primary yaw_right comparison (prior vs newest)

| Metric | Real | Prior sim (163547) | Newest sim (171257) | Prior Δ vs real | Newest Δ vs real | Direction (prior→newest) |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.240 | 0.040 | 0.060 | -0.200 | -0.180 | slight improvement |
| peak_rate | 84.600 | 84.340 | 83.503 | -0.260 | -1.097 | regression |
| max_accel | 312.000 | 638.006 | 363.038 | +326.006 | +51.038 | **strong improvement** |
| overshoot | 4.600 | 79.789 | 79.247 | +75.189 | +74.647 | slight improvement |
| settle_time_s | 0.000 | 1.380 | 1.400 | +1.380 | +1.400 | slight regression |

### Interpretation for yaw_right response shape

- **Onset / acceleration shape:** improved materially (`max_accel` moved much closer to real).
- **Release overshoot:** improved slightly vs `163547`, but remains far from real.
- **Settle behavior:** slightly worse (`1.40s` vs `1.38s`), still clearly off.
- **Peak-rate closeness:** regressed in this rerun.

Overall classification for `yaw_right`: **improved-but-still-off**.

## Secondary checks requested for this pass

### yaw_left stability / acceptability

| Metric | Real | Prior sim (163547) | Newest sim (171257) | Prior Δ | Newest Δ | Direction |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.250 | 0.040 | 0.060 | -0.210 | -0.190 | slight improvement |
| peak_rate | 70.750 | 66.599 | 65.924 | -4.151 | -4.826 | regression |
| max_accel | 177.500 | 603.638 | 286.636 | +426.138 | +109.136 | **strong improvement** |
| overshoot | 6.900 | 61.895 | 62.668 | +54.995 | +55.768 | slight regression |
| settle_time_s | 0.000 | 1.320 | 1.420 | +1.320 | +1.420 | regression |

`yaw_left` is **not yet acceptable**. It improved on onset shape, but settle/overshoot remain poor.

### lateral_right acceptability

| Metric | Real | Prior sim (163547) | Newest sim (171257) | Prior Δ | Newest Δ | Direction |
|---|---:|---:|---:|---:|---:|---|
| response_delay_s | 0.000 | 0.080 | 0.120 | +0.080 | +0.120 | regression |
| peak_rate | 0.200 | 2.291 | 0.117 | +2.091 | -0.083 | **strong improvement** |
| max_accel | 0.333 | 5.786 | 0.141 | +5.453 | -0.192 | **strong improvement** |
| overshoot | 0.200 | 1.997 | 0.108 | +1.797 | -0.092 | **strong improvement** |
| settle_time_s | 1.100 | 1.180 | 1.280 | +0.080 | +0.180 | regression |

`lateral_right` is **improved-but-still-off** and **not yet acceptable** due to delay/settle regressions despite major shape improvements.

## Strong vs provisional category synthesis

- Strong categories (`lateral_right`, `yaw_right`, `yaw_left`) all remain **improved-but-still-off** in the regenerated closed-loop output.
- Provisional categories (`forward_step`, `lateral_left`, `climb`, `descent`) remain **provisional**; confidence/provenance did not upgrade in this pass.
- High-confidence strongest mismatch is now **`yaw_left`**, not `yaw_right`, in the regenerated `171257` comparison.

## Direct answers

- Did `yaw_right` improve? **Yes, partially** (especially onset/accel and slight overshoot gain).
- Did `yaw_right` overshoot improve? **Yes, slightly**.
- Did `yaw_right` settle behavior improve? **No** (slight regression).
- Did `yaw_right` onset/accel shape improve? **Yes, materially**.
- Did `yaw_left` remain stable enough? **Borderline stable directionally, but not acceptable yet**.
- Is `lateral_right` now acceptable? **No**.
- Is `yaw_left` now acceptable? **No**.
- Is `yaw_right` still the strongest high-confidence mismatch? **No** (`yaw_left` is now strongest).
- Did provisional categories strengthen or remain provisional? **Remain provisional**.
- Is Normal-mode benchmark fidelity now good enough to move on? **No**.

## Single next recommended action

**one more narrow `yaw_right` pass**

Reason: despite some gains, `yaw_right` still has large residual overshoot/settle mismatch and is the next actionable non-provisional right-yaw target from this pass's trajectory analysis.
