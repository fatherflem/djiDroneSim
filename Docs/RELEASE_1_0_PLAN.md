# DJI Drone Simulator 1.0 Release Plan

> Audit date: 2026-09-17  
> Source of truth: repository code at `6012d8b`, including the Hover Box and field-system merges.  
> Product target: a desktop-first, classroom-ready DJI Mini 5 Pro-style training application with optional stationary Quest 3 presentation.

## Guardrails

- **Normal-mode flight behavior is frozen.** Do not change controller gains, mode assets, Rigidbody setup, input curves, benchmark tolerances, or related defaults without an explicit fidelity reason and before/after evidence.
- Prefer a finite training application over a sandbox: guided entry, repeatable drills, clear results, and a reliable return path.
- Desktop and VR are presentations of one training model. They must not become separate drill implementations.
- Runtime-generated objects remain useful for prototyping, but release scenes and build configuration must become explicit, inspectable, and testable.

## Audit Snapshot

| Area | Repository truth |
|---|---|
| Unity | Unity `6000.3.17f1` (Unity 6.3 LTS). |
| Rendering | Universal Render Pipeline `17.0.3`; runtime material fallbacks are centralized by `RuntimeShaderCache`. |
| Input | Input System `1.11.2`, with both old and new backends enabled (`activeInputHandler: 2`). `DroneInputReader` creates actions from configurable binding strings and supports externally supplied benchmark/VR frames. |
| Scenes | Desktop vertical slice, desktop Hover Box, VR pilot, and VR Hover Box scenes exist under `Assets/Scenes`. |
| Build configuration | No committed `ProjectSettings/EditorBuildSettings.asset`; there is no authoritative scene order or proven player build recipe. |
| Runtime bootstrap | `VerticalSliceBootstrap`, `HoverBoxDrillSceneBootstrap`, and `VRPilotBootstrap` create and wire substantial portions of the app at runtime. There is no application-level flow controller. |
| Flight | `DroneInputReader -> DJIStyleFlightController -> DronePhysicsBody`; Cine, Normal, and Sport are ScriptableObject profiles. Normal is evidence-backed and frozen. |
| Camera / feed | Chase/FPV switching, a stabilized gimbal camera, persistent RenderTexture feed, debug overlay, and display-surface bridge exist. |
| Training | A legacy `SimpleTrainingScenario` and the newer five-point `HoverBoxDrill` coexist. Hover Box supplies desktop/VR UI, waypoint feedback, bounds monitoring, restart, and field overrides. |
| Environment | `FieldDefinition` and `FieldLoader` can instantiate a field prefab or a generated placeholder and expose operating-area metadata. Only the placeholder definition is committed. |
| VR | OpenXR, XR Management, Core Utils, and XR Interaction Toolkit packages are present. `VRPilotBootstrap` implements a stationary pilot shell and virtual RC/feed bridges; headset setup and tracked-prop calibration remain incomplete. |
| Persistence | No settings, student profile, calibration, session-history, or results persistence service exists. Telemetry and benchmark CSV/ZIP outputs are developer-facing. |
| Tests | No Unity EditMode/PlayMode test assemblies are committed. Validation is currently benchmark scripts, archived runs, and manual checklists. |
| Editor tooling | No custom Editor folder or release/build tooling is committed. |

## A. Current State

### Flight Core

- Rigidbody-based stabilized flight is separated into input, control, physics, and visual layers.
- Cine, Normal, and Sport profiles expose directional speed/acceleration/braking and yaw parameters.
- Normal mode has extensive AirData comparisons, archived benchmark sessions, acceptance criteria, and an explicit freeze decision.
- The model is intentionally a DJI-style training abstraction, not a per-motor engineering simulation.

### Input

- Keyboard/gamepad/joystick bindings, deadzone, expo, smoothing, inversion, camera mode, gimbal, and mode selection are data-driven through `DroneInputConfig`.
- Raw joystick diagnostics and a debug HUD exist for troubleshooting.
- External input injection supports deterministic benchmark runs and the VR bridge.
- Missing for classroom use: guided device selection, neutral/range calibration, mapping confirmation, disconnect handling, and a student-facing controller check.

### Camera / FPV

- Chase and FPV presentation modes share the same flight model.
- The drone camera has pitch limits, smoothing, and horizon stabilization.
- A live feed can be shown on a world-space/controller surface independently of the active desktop camera.
- Current overlays are diagnostic rather than release UI.

### Training

- Hover Box has ordered 3D markers, configurable hold/stability requirements, field-derived bounds, live feedback, desktop and VR views, and retry support.
- The first reusable training layer now provides `TrainingDrill`, `TrainingDrillDefinition`, `DrillState`, and `TrainingResult`. Both committed Hover Box scenes now explicitly bootstrap it, use the shared Instructions → Countdown → Running → Completed/Failed → Results lifecycle, lock/reset the aircraft between attempts, and expose keyed drill metrics while retaining the existing evaluation rules. Desktop supplies Input System-backed clickable controls; VR uses the physical controller's gamepad South button. Full Play Mode verification remains required on a Unity-capable workstation.
- Hover Box results report elapsed time, a simple score, out-of-bounds time, and interrupted holds.
- `SimpleTrainingScenario` remains a separate legacy vertical-slice exercise and should not be used as the template for new drills.

### Environment

- Field selection is represented by a ScriptableObject with ground height, preferred drill altitude, safe footprint, maximum altitude, metadata, and an optional prefab.
- A generated placeholder provides a functional field before authored environment content is ready.
- There is no environment-selection screen, environment validation, or production field asset yet.

### Desktop UI

- Runtime UGUI and IMGUI surfaces expose flight telemetry, controller diagnostics, camera status, and Hover Box status.
- Hover Box now exposes an instruction/start step, countdown, running metrics, result score, and retry through its existing desktop presenter.
- There is no unified visual language, main menu, drill/environment selection, pause/exit flow, or scalable accessibility pass.

### VR

- A stationary pilot experience, XR camera setup, virtual RC, stick animation, and live controller feed are scaffolded.
- Hover Box consumes the same drill model as desktop rather than copying its evaluation logic.
- Quest 3 feature configuration, physical-controller pose calibration, controller-check UX, performance evidence, and a dependable no-hands UI path are not release-ready.

### Benchmarking

- Scriptable maneuver definitions, deterministic external input, telemetry capture, CSV export, an environment controller, analysis scripts, archived sessions, and acceptance documents exist.
- Evidence is strongest for Normal mode. Cine/Sport and end-to-end classroom drills have less coverage.
- Benchmark tools are developer workflows, not automated CI release gates.

### Persistence

- None at application level. Configuration is stored in project assets only.
- Student results, controller calibration, selected environment, audio/display preferences, and instructor export history do not survive sessions.

### Build / Distribution

- Package dependencies and the Unity version are pinned.
- Build scenes/settings, target presets, version stamping, clean-machine build verification, crash/log collection instructions, and a packaged teacher quick start are absent.
- Android/Quest deployment still depends on local Unity/OpenXR configuration.

### Documentation

- Architecture, handoff, tuning, benchmark, acceptance, VR, Hover Box, field, and AirData documents provide substantial engineering history.
- Some overview documents predate the dedicated Hover Box and field work and still emphasize the prototype/debug vertical slice.
- There is no single release runbook for teachers or controller compatibility matrix.

## B. Release Blockers

1. **Authoritative app entry and build settings:** commit a main-menu scene and `EditorBuildSettings` scene list; prove a Windows desktop player from a clean checkout.
2. **Application flow:** implement Menu → Training/Free Flight → selection → Controller Check → Instructions → Drill → Results → Retry/Next/Menu. Back, pause, and quit must always work.
3. **Controller readiness:** add a student-facing connection/mapping/neutral/range check, clear unsupported-device messaging, reconnection behavior, and at least one documented RC-style controller configuration.
4. **Scene/reference integrity:** open and resave every release scene in the pinned Unity editor, resolve missing-script references, and replace release-critical runtime discovery with explicit references where practical.
5. **Training reliability:** add PlayMode coverage for lifecycle transitions, restart/reset, completion, timeout/failure, results, and shared desktop/VR behavior. Confirm restart also resets the drone to a safe, deterministic pose before repeated student runs.
6. **Safety and recovery:** define ground contact, flyaway/out-of-bounds response, reset, pause, and controller-disconnect behavior. A student must not need the Unity Editor to recover.
7. **Local persistence:** save controller calibration and user settings; store/export a minimal result record without requiring accounts or cloud services.
8. **Release UI pass:** replace overlapping debug surfaces with a readable student HUD and keep diagnostics behind an instructor/developer toggle.
9. **Deployment proof:** document supported OS/controller combinations, package licensing, build/install steps, first-launch steps, log location, and a clean-machine classroom smoke test.
10. **Quest claim discipline:** either complete and test the documented Quest 3 setup/performance path or clearly label VR as preview/optional in 1.0 materials.

## C. Required for 1.0

### Reference training architecture

- Keep `TrainingDrill` small: common lifecycle, timing, transitions, result publication, and restart entry points only.
- Keep definitions authorable: stable ID, display name, student instructions, countdown, optional time limit, and restart policy.
- Keep scoring drill-specific while returning a common result shape.
- Add a single `TrainingDrillRunner`/session coordinator only when application flow needs to select definitions, reset the aircraft, lock input, and navigate. Do not move flight control into training classes.
- Move duplicated desktop/VR text decisions into a shared presenter/view-model before implementing the second production drill.
- Deprecate or migrate `SimpleTrainingScenario`; do not maintain two competing training frameworks.

### Minimum curriculum

Ship a small, tested sequence rather than all candidate drills. Recommended 1.0 minimum:

1. Free Flight / Familiarization.
2. Takeoff and Hover.
3. Hover Box (reference drill).
4. Controlled Takeoff → Hover → Landing.
5. Precision Landing **or** a square pattern, based on instructor testing.

Figure-eight/coordinated yaw can follow once the common runner and scoring model have proven stable.

### Results and instructor utility

- Show pass/fail, elapsed time, key mistakes, a concise coaching message, and retry/menu actions.
- Persist a local timestamped record containing drill ID/version, environment, result metrics, and controller identity where available.
- Provide simple CSV export suitable for a teacher; avoid accounts and student PII by default.
- Version scoring rules so later tuning does not silently invalidate comparisons.

### Quality gates

- Automated EditMode tests for pure lifecycle/scoring logic and PlayMode tests for scene wiring/reset.
- A short desktop release smoke checklist covering launch, controller check, each drill, retry, menu return, settings persistence, and quit.
- Frozen-Normal benchmark comparison whenever input, controller, physics, timing, or Rigidbody code is touched.
- Performance targets measured on the actual classroom desktop baseline; optional Quest targets measured on Quest 3.
- Instructor/student usability session validating that a first-time user can start, complete, interpret, and retry a drill without developer help.

## D. Nice After 1.0

- Additional drills: rectangle, coordinated yaw, figure eight, and expanded precision landing.
- More field definitions after one performant, legible production environment is validated.
- Richer instructor dashboards and longitudinal progress summaries.
- Better virtual RC art, tracked-prop alignment, and optional VR presentation polish.
- Localization, remappable accessibility presets, audio coaching, ghost/reference paths, and replay.
- Broader controller compatibility profiles and automated device recognition.
- Cine/Sport curriculum variants after Normal-mode learning outcomes are established.

These items must not delay a dependable desktop classroom release.

## E. Out of Scope for 1.0

- Multiplayer, networking, online accounts, cloud saves, and leaderboards.
- Open-world or photorealistic world building and dynamic weather.
- Multiple aircraft models, damage/repair simulation, battery chemistry, motor/propeller engineering, or a full DJI software clone.
- FPV racing, acro flight, elaborate VR hand interaction, locomotion, or a general VR interaction showcase.
- Autonomous mission planning, regulatory certification, or claims that simulator completion replaces supervised flight instruction.

## Delivery Sequence

### Milestone 1 — Prove the reference drill

- Finish shared Hover Box lifecycle presentation and deterministic aircraft reset.
- Add lifecycle/result tests and remove hard-coded UI threshold duplication.
- Decide migration/removal path for `SimpleTrainingScenario`.

**Exit:** desktop and VR presenters observe the same run; instruction, countdown, complete/fail, result, and retry paths are repeatable.

### Milestone 2 — Make it an application

- Add the main menu, session coordinator, controller check, selection screens, pause/back/quit, settings, and explicit release scene list.
- Separate student HUD from instructor diagnostics.

**Exit:** a teacher launches a player build and a new student reaches and retries Hover Box without developer knowledge.

### Milestone 3 — Complete the minimum curriculum

- Add drills one at a time through the reference architecture.
- Add common reset/bounds/result tests for every drill and conduct instructor usability trials.

**Exit:** the selected minimum curriculum has consistent instructions, measurable outcomes, and no drill-specific navigation forks.

### Milestone 4 — Release hardening

- Implement local persistence/export, accessibility basics, clean-machine builds, controller matrix, logging/runbook, and licensing review.
- Run frozen-Normal regression checks only if relevant systems changed.
- Validate Quest 3 or label it clearly as preview.

**Exit:** signed/tagged 1.0 build, teacher quick start, known-issues list, test evidence, and rollback artifact are archived.

## Explicit Non-Change in This Milestone

The reusable training work does **not** modify `DJIStyleFlightController`, `DronePhysicsBody`, `DroneInputReader`, any flight-mode asset, any benchmark definition, or any benchmark threshold. Hover Box's waypoint geometry and pass thresholds remain unchanged; only lifecycle ownership and result reporting are added around the existing evaluation.
