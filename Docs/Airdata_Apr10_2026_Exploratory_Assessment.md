# Exploratory Assessment: Apr-10th-2026-02-12PM Free-Fly AirData

## Scope and evidence tier

This file (`Apr-10th-2026-02-12PM-Flight-Airdata.csv`) is a **messy, non-protocol free-fly log** and is treated as **exploratory evidence only**.
It is not used as acceptance-grade proof and does not replace structured benchmark sessions such as `session_20260410_135709`.

Primary extraction artifacts:
- `Docs/airdata_apr10_2026_1412_exploratory_maneuvers.json`
- `Docs/Airdata_Apr10_2026_Exploratory_Maneuver_Mining.md`
- Tool: `Tools/extract_exploratory_maneuvers.py`

## Data quality and usability

- Row count: **10,898**.
- Sampling: mostly **100 ms** cadence (one duplicate timestamp row).
- Flight mode mix: mostly `P-GPS`, with smaller `Go_Home` / `AutoLanding` tails.
- RC traces are present and usable (`rc_elevator`, `rc_aileron`, `rc_throttle`, `rc_rudder`, each reaching ±100%).
- Active-control fraction (RC max axis ≥ 22% while `P-GPS`): about **47.2%**.
- Neutral/passive fraction: about **52.8%**.

Overall usefulness rating: **Moderate (exploratory-strong, acceptance-weak)**.

## Candidate segmentation method (heuristic, explainable)

`Tools/extract_exploratory_maneuvers.py` uses these rules:
1. Detect active windows from RC activity (`max(abs(axis)) >= 22%`) with short gap stitching.
2. Classify by dominant stick axis and sign.
3. Mark a window `mixed-input window` when dominant-axis ratio is below 1.25.
4. Compute cleanliness score from:
   - hold duration,
   - dominant-axis separation ratio,
   - pre/post neutral dwell,
   - measurable response signal.
5. Mark very weak windows as `discard/noisy window`.
6. Compute per-window metrics: input peak rate, full-run peak rate, carryover after release, settle timing (when detectable).

## Extraction totals

From this log:
- Candidate windows: **194**
- Usable windows (non-mixed, non-discard, medium/high confidence): **167**

Counts by class:
- forward-dominant: 30
- backward-dominant: 12
- right-strafe-dominant: 14
- left-strafe-dominant: 13
- climb-dominant: 22
- descent-dominant: 20
- yaw-right-dominant: 31
- yaw-left-dominant: 25
- mixed-input: 11
- discard/noisy: 16

## Cleanest objective windows (examples)

Representative high-confidence examples (row ranges):
- Forward: `1612-1644`, `1684-1714`, `6202-6225`
- Right strafe: `2090-2123`, `2154-2181`, `2209-2236`
- Left strafe: `2868-2894`, `2927-2958`, `3010-3036`
- Climb: `4167-4186`, `3938-3956`, `8778-8794`
- Descent: `4237-4253`, `4278-4309`, `9663-9681`
- Yaw right: `877-915`, `3293-3321`, `3442-3479`
- Yaw left: `3593-3646`, `6693-6722`, `7449-7470`

For full metric tables (peak stick, hold duration, input/full peaks, carryover, settle), see:
`Docs/Airdata_Apr10_2026_Exploratory_Maneuver_Mining.md`.

## What this file suggests about current Normal mode

Comparisons below are directional and uncertainty-limited.
Structured reference remains `session_20260410_135709`.

1. **Forward onset**
   - Clean free-fly forward windows include some segments near/above benchmark forward speeds, but they are highly variable because hold duration and mixed intent vary.
   - Net read: this does **not** clearly contradict the current “forward slightly low but usable” structured finding.

2. **Right vs left lateral tendency**
   - In selected cleaner windows, right-strafe peaks often appear somewhat higher than left, consistent with the existing structured asymmetry concern.
   - Net read: exploratory evidence **leans consistent** with “right can feel stronger,” but this is not a precise acceptance quantification.

3. **Vertical behavior**
   - Climb/descent windows exist in useful quantity, but free-fly vertical events vary from short jabs to longer holds and include mixed periods.
   - Net read: this does not weaken the structured long-window vertical conclusion from `session_20260410_135709`; it mostly supports that vertical behavior is now in a plausible range for casual maneuvering.

4. **Missing real-drone-feel signs**
   - No single glaring, high-confidence pattern in this log shows a must-fix Normal-mode deficiency beyond already-known narrow gaps.
   - Mixed/noisy windows and operator variability dominate uncertainty.

## What this file can and cannot prove

Useful for:
- sanity-checking that modeled behavior appears broadly plausible under messy real pilot behavior,
- mining candidate windows for exploratory trend checks,
- spotting obvious outliers/regressions quickly.

Not reliable enough for:
- acceptance sign-off,
- strict retune decisions,
- fine-grained axis-error quantification at protocol-level confidence.

## Best next objective data-collection move

Keep this free-fly log as exploratory context, but collect one additional **structured real-flight capture** that mirrors protocol timing (clean pre-neutral, controlled hold durations, clean release) for forward/lateral/vertical/yaw in Normal mode.
That provides high-confidence, apples-to-apples validation while preserving current freeze-first posture.
