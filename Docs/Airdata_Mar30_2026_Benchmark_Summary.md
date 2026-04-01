# Airdata Benchmark Summary (Mar 30, 2026 08:31 UTC)

Source file parsed directly from repository root:
- `/workspace/djiDroneSim/Mar-30th-2026-08-31AM-Flight-Airdata.csv`

Generated with:
- `python Tools/analyze_airdata.py Mar-30th-2026-08-31AM-Flight-Airdata.csv`

## Segmentation method (structured benchmark assumption)

1. Loaded full-resolution CSV (2,321 rows at ~10 Hz) and filtered benchmark window to `flycState == P-GPS` (2,196 rows, ~225 s).
2. Used RC percent channels (`elevator`, `aileron`, `throttle`, `rudder`) as primary maneuver markers.
3. Detected one-axis step windows when dominant axis exceeded ±18% with non-dominant axes below ±12%.
4. Required minimum active window length of 0.8 s and merged near-contiguous windows of same type.
5. Mapped maneuver classes by dominant axis/sign:
   - elevator+: `forward_step`
   - aileron+: `lateral_right`
   - throttle+: `climb`
   - throttle-: `descent`
   - rudder+: `yaw_right`
   - rudder-: `yaw_left`
6. Neutral dwell windows (all channels near center) longer than 2 s were tagged as `hover_hold` windows.

## Identified maneuver blocks

Cleanly identified:
- Hover hold: 15 windows
- Forward step: 12 windows
- Lateral right: 4 windows
- Climb: 1 window
- Descent: 3 windows
- Yaw right: 5 windows
- Yaw left: 4 windows

Not cleanly identified in this log:
- Lateral left: not present as a clean one-axis block

## Directly extracted aggregate metrics

### Hover hold
- Horizontal speed RMS mean: **0.539 m/s**
- Vertical speed RMS mean: **0.294 m/s**
- Altitude std-dev mean: **0.139 m**

### Forward step
- Mean peak speed: **2.06 m/s** (max 3.30 m/s)
- Mean response delay (~10% of peak): **0.23 s**
- Mean max acceleration: **2.25 m/s²**

### Lateral right
- Mean peak speed: **2.30 m/s** (max 2.50 m/s)
- Mean response delay: **0.40 s**
- Mean max acceleration: **3.50 m/s²**

### Vertical
- Climb: 1 usable block, peak **~1.61 m/s** (low confidence, single sample)
- Descent: mean peak **4.07 m/s** (max 5.80 m/s), mean response delay **0.17 s**

### Yaw
- Yaw left peak rate mean: **70.75 deg/s**
- Yaw right peak rate mean: **84.60 deg/s**
- Yaw right appears more aggressive than left in this session.

### Attitude coupling (direct observations)
- Forward segments carried consistent negative pitch magnitude.
- Lateral-right segments carried strong positive roll magnitude.
- This supports tilt-to-translation coupling in sim tuning.

## Inferred/estimated (not directly validated by this single file)

- Exact steady-state top speed limits for full-stick commands in all axes.
- Symmetric lateral-left behavior (no clean left-step blocks were found).
- Robust climb dynamics (only one short climb block met clean criteria).
- Cine/Sport mode validation (log mode is dominated by `P-GPS`; Cine/Sport values were derived from Normal baseline, not directly validated here).

## Output artifacts

- Machine-readable summary: `Docs/airdata_mar30_analysis.json`
- Analysis helper script: `Tools/analyze_airdata.py`
