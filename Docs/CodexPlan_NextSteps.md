# Codex Next Steps Plan (Post Vertical-Fix Audit)

_Last refreshed: April 10, 2026 after auditing `session_20260410_135709`._

## 1) Where the project has been (closed-loop sequence)

1. `session_20260409_145236`: yaw held-input regression discovered (~38.8 °/s).
2. `session_20260409_164309` + `170224`: yaw control fixed (~79.9 °/s) with healthy release timing.
3. `session_20260409_180413`: forward protocol amplitude corrected to `forward_step pitch=1.0`.
4. `session_20260409_183817` + `190056`: forward brake-slew patch reduced carryover to ~0.500 m/s while preserving onset.
5. `session_20260410_120548`: first archived **10-maneuver** Normal run with `climb_long` / `descent_long`.
6. `session_20260410_135709`: validated the vertical-only patch and showed long vertical peaks now in-range without cross-axis regression.

## 2) What newest evidence proved

From `session_20260410_135709` input-phase peaks:

- `forward_step`: ~2.220 m/s (carryover ~0.500 m/s full-run delta)
- `lateral_right`: ~8.925 m/s
- `lateral_left`: ~9.812 m/s
- `yaw_right` / `yaw_left`: ~79.89 °/s
- `climb_long`: ~4.194 m/s
- `descent_long`: ~3.578 m/s

Interpretation:
- Yaw is effectively done for now.
- Forward is improved and stable enough to defer.
- Long-window vertical is now in the target zone by current ±15% criterion.
- Remaining misses are narrow: forward input-phase is slightly below threshold, and right-lateral remains somewhat aggressive.

## 3) Decision: PATH A (freeze Normal tuning for now)

Why freeze:
- Recent one-axis fixes succeeded without reopening solved axes.
- Normal mode now meets most high-value acceptance targets, including yaw and long-window vertical.
- Remaining misses are relatively small (forward onset) or potentially intentional asymmetry tradeoff (right-lateral) and do not currently justify another risk cycle.

## 4) What to do next (evidence, not retune)

1. Archive one full-protocol **Cine** session using `protocolModeOverride`.
2. Archive one full-protocol **Sport** session using `protocolModeOverride`.
3. Record hover-box completion timing for Normal/Cine/Sport.
4. Only reopen Normal tuning if those runs or playtests show a concrete training-impact issue.

## 5) Reopen gates (strict)

Permit one new Normal micro-patch only if:
- a new run shows statistically meaningful regression, or
- instructional/pilot testing confirms the remaining mismatch impairs training outcomes.

If reopened, choose only one axis and keep the patch tiny.

## 6) Analyzer commands

Primary latest-session check:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv --session session_20260410_135709
```

Trend audit command:

```bash
python3 Tools/analyze_airdata.py Apr-8th-2026-08-15AM-Flight-Airdata.csv \
  --session session_20260409_145236 \
  --session session_20260409_164309 \
  --session session_20260409_170224 \
  --session session_20260409_180413 \
  --session session_20260409_183817 \
  --session session_20260409_190056 \
  --session session_20260410_120548 \
  --session session_20260410_135709
```
