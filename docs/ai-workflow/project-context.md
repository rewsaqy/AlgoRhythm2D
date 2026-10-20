# Project Context — AlgoRhythm2D

## 1. Provenance and coverage

- Observed: 2026-10-20, source kind `git working tree`, revision `84b82c7`, dirty `true`.
- Repository root: `/home/zxwyvern/AlgoRhythm2D` (one Unity root).
- Inspector: AI agent. The 3-day demo plan was supplied by the user on 2026-10-20 and saved as `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md`. A Unity 6000.3.8f1 batchmode compile, EditMode run, and bootstrap-only PlayMode smoke test were performed on a disposable clone after the I-001 source was added; no input-driven Play Mode, build, profiler, or player run was performed. The main project editor was already open, so direct tests in the live project were not attempted.
- Budget: `balanced` (refresh for the 3-day demo); profile: `compact`. Deep reads were limited to startup, stack, scene, input, design context, and the demo plan. The file inventory covered `Assets/` and top-level project/package/config files; `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and `Builds/` were excluded.
- Design authority: `docs/ALGORHYTHM_GAME_CONTEXT.md` is a derived summary of `AlgoRhythm_CODE_IS_SHIELD_GDD_Full_v1.docx` (v1.0, pre-production); `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` is the near-term demo scope. The original DOCX is absent. Sections §13.5 `[E]` and §17 are derived engineering/gap analysis, not original GDD; do not treat them as `[D]`. If a derived section conflicts with a `[D]` item, the original GDD wins when available; if scope conflicts, the demo plan wins for the demo. Refresh this pack when either authority changes.
- Derived content: §13.5 `[E]`, §17 gap analysis, and the 175-combination count are authored additions, not GDD text. Claims from those sections carry a derived limit.
- Applicable instructions: parent-scope `/home/zxwyvern/AGENTS.md` (agentic-first, human integration authority); root `AGENTS.md` now carries the generated workflow section. This pack adds the detailed rules.
- Snapshot completeness limits are listed in §15.

## 2. Pack profile and tier

- Profile: `compact` (default for a focused prototype). Revisit if routes exceed 8 or gameplay scripts exceed roughly 50.
- Tier: `small` — one Unity root, 20 runtime C# scripts, 4 asmdefs, 1 scene, 0 prefabs, 5 test files (4 EditMode + 1 PlayMode smoke), 4 project-owned assets. The I-001 greybox arena and HUD are created at runtime; they add no prefab or serialized scene content.

## 3. Protected areas

- [P-001] `unity-workflow-kit/**` | reason: vendored workflow-kit checkout with its own `.git`; reference only, do not edit, restage, or commit as project content.
- [P-002] `Assets/Editor.meta`, `Assets/Editor/HubForceResolve.cs`, `Assets/Editor/HubForceResolve.cs.meta` | reason: pre-existing working-tree deletions; preserve unless the task explicitly restores or replaces this editor helper.
- [P-003] `Assets/Settings/**` | reason: URP 2D renderer/settings and template scenes; pre-existing changes; do not normalize without a rendering task.
- [P-004] `docs/ALGORHYTHM_GAME_CONTEXT.md` | reason: derived design summary; §13.5/§17 are analysis, not original GDD; original DOCX absent; edit only with design-owner approval.
- [P-005] `Packages/**`, `ProjectSettings/**` | reason: package and serialized project settings; change only for a task that explicitly targets them, with version/build impact checked.
- [P-006] `Library/**`, `Temp/**`, `Logs/**`, `UserSettings/**`, `Builds/**` | reason: generated/cache/build/user state; do not edit or commit.
- [P-007] `Assets/Scenes/SampleScene.unity` | reason: the only registered scene; serialized edits require task relevance and a verified scene binding.
- [P-008] `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` | reason: near-term demo scope and decision record; edit only with demo-owner approval and preserve the §0 session decisions.

## 4. Intended experience and design conflicts

`docs/ALGORHYTHM_GAME_CONTEXT.md` defines a single-player, Windows-PC, 4-direction rhythm-shielding × live-coding puzzle. The player defends a Core Database across 5 sectors × 3 sub-waves (15 total), composes rules from 3 tokens (condition, target, action), and faces A.I.D.A. v1.0–5.0 in a 3-phase finale. The stated priority is controls/timing → rule engine → vertical slice → content → polish.

The near-term demo scope is the Adware 1A/1B slice in `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md`: DSP timing, manual shield, 3-slot rules, auto-shield, one decoy, dialogue, pause/calibration, and a build. The demo plan's §0 records the session decisions:

- A1: auto-shield is independent of manual input, spends Energy on fire, respects cooldown, and is capped at Good; manual shield remains the route for other directions and Perfect. Set A runtime adds manual precedence, no-energy suppression, auto-Good scoring, decoy −5 without Good, and cooldown-on-fire.
- A2: Integrity + Energy are a demo-only two-resource choice; the summary is internally inconsistent (one main resource vs Integrity 100 + Energy 6), and regen +1 per 4 beats is a demo addition.
- A3/A4/A5: commit anytime and activate next beat; fixed BPM 100; failure retries the current sub-wave.
- Build: Linux internal demo first; Windows clean build remains the `[D]` release gate and is not yet run.

Remaining production conflicts/open decisions (not resolved by the demo):

- Manual shield vs rule `RAISE_SHIELD`: A1 is a demo semantic; production still needs owner confirmation of whether rules automate, modify, or preserve manual shielding (F-001, V-004/V-008).
- Resource model: the demo uses Integrity + Energy; the summary's one-resource guidance conflicts with its own Integrity 100 + Energy 6 values, and the final model/names remain open (D-03/D-12, F-004).
- Input timestamp source and DSP mapping: one consistent path is required; the 120 ms buffer interaction with the Good window still needs implementation validation.
- Safe window (#5), upgrade replacement rules (#6), in-wave energy regeneration (#7), and RU/OV ending metrics (#8) are derived §17 gap findings, not D-01–D-12; they remain production decisions beyond the demo (F-001/F-005).
- The 3-day schedule is much shorter than the design summary's P1–P3 estimates; the demo cut rules are part of the plan (F-005).
- Windows-only is `[D]`; only Linux build support is installed here, so the Linux demo artifact is not a substitute for the Windows acceptance gate (F-006).
- Showing A.I.D.A. 2.0 after 1B is a demo narrative compression; the design summary places 2.0 after sector 1, including 1C (F-010).
- Rule-validation and hot-path contracts in the demo plan were corrected to E001–E006, a cooldown-aware validation context, and a preallocated `Span<ThreatEvent>` due-buffer; implementation and tests remain (F-007, F-008, F-009).

## 5. Stack and version evidence

| Component | Version | Evidence | Status |
|---|---|---|---|
| Unity Editor | 6000.3.8f1 (revision 1c7db571dde0) | C-001 | batchmode compile/EditMode tests on a disposable clone; main editor open; Play Mode not run |
| Universal RP | 17.3.0 | C-002 | declared and locked |
| Input System | 1.18.0; new-only (`activeInputHandler: 1`) | C-003 | declared and configured; I-001 `Rhythm` map added; bindings not exercised |
| Unity Test Framework | 1.6.0 | C-004 | declared; I-001 EditMode + PlayMode assemblies; EditMode 19/19 and smoke 1/1 in a clone |
| Demo scope | Adware 1A/1B (1C optional) | C-016 | design intent; I-001 debug fixture only; no 1A/1B content assets |
| Demo build (internal) | Linux Standalone (local only) | C-022 | planned; not a target change |
| Python (pack tools) | 3.14.8 | tooling observation | available |
| Node.js (external MCP candidate) | 26.11.1 | tooling observation | available; MCP not installed |
| Target platform | Windows PC (design intent) | C-015 | `[D]` release target; not confirmed in repo build settings |

## 6. Entry and startup path

- First and only enabled build scene: `Assets/Scenes/SampleScene.unity` (C-005).
- Scene contents: `Main Camera` (orthographic, size 5, URP camera data) and `Global Light 2D`; no gameplay components or scripts (C-005).
- I-001 adds a runtime `GameBootstrap` under `Assets/_Project/Scripts/Game/Bootstrap/`; it starts through `RuntimeInitializeOnLoadMethod`, so the scene still contains no serialized gameplay MonoBehaviour (C-014, C-030).
- `Assets/_Project/Scripts/Core/**`, `Assets/_Project/Scripts/Game/**`, and `Assets/_Project/Tests/EditMode/**` now exist. `Assets/_Project/Data/**` and the proposed `Boot`/`Gameplay` scenes do not.
- An I-001 execution path exists and the EditMode suite plus a bootstrap-only PlayMode smoke are observed; input-driven Play Mode, the actual game scene, and the full demo loop remain unverified (V-003).

## 7. Subsystem map (deep-mapped)

| Subsystem | Intended responsibility | Current state | Key future symbols |
|---|---|---|---|
| Rhythm/Timing | DSP beat clock, impact schedule, input judge, calibration | I-001 source + EditMode tests + bootstrap PlayMode smoke; input/scene not run | `BeatClock`, `DspClockSource`, `TimingJudge`, `InputRouter` |
| Rule Engine | 3-slot grammar, whitelist, validation, preview, auto-shield | absent | `RuleValidator`, `RuleRuntime`, `RuleDefinition`, `RuleWhitelist` |
| Combat/Waves | Threat events, idempotent resolve, waves, decoy, energy | I-001 `CombatState`, `WaveRunner`, `WaveData`, debug fixture; no content assets | `CombatState`, `ThreatEvent`, `WaveRunner`, `WaveData` |
| Presentation/Flow | Arena, code panel, HUD, dialogue, pause, results | I-001 runtime arena/HUD/metronome; no code panel, dialogue, results, or pause view | `ArenaPresenter`, `CodePanelView`, `HudView`, `DialogueView`, `GameFlow` |

Bootstrap, input, and DSP adapters are foundations for these systems, not separately deep-mapped in this compact pass.

## 8. Representative end-to-end paths (I-001 implemented; demo unverified)

Combat path: `Input (N/E/S/W) → InputRouter (DSP timestamp) → BeatClock expected impact → TimingJudge → CombatController → CombatState.ResolveThreat → runtime HUD/arena feedback → wave completion`.

Rule path: `Code panel → RuleValidator (preview/error) → RuleRuntime.Commit → next beat OnBeat → auto-shield target direction → RuleFired → code-panel log/HUD`.

Current evidence: I-001 source, the `Rhythm` input map, the runtime bootstrap, the EditMode suite, and a bootstrap-only PlayMode smoke exist (C-006, C-014, C-030). The rule, content, narrative, and build links remain absent; input-driven Play Mode and the actual `SampleScene` have not been observed.

## 9. Scene, prefab, and config bindings

- `Assets/Scenes/SampleScene.unity`: `Main Camera` (Camera, AudioListener, URP camera data; orthographic size 5; z = -10) and `Global Light 2D`; no prefab instances, no gameplay MonoBehaviours (C-005).
- `ProjectSettings/EditorBuildSettings.asset`: one enabled scene, `SampleScene`; the Input System settings object is wired to `Assets/InputSystem_Actions.inputactions` (C-005, C-006).
- `Assets/InputSystem_Actions.inputactions`: maps `Player` (Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint), `UI`, and I-001's `Rhythm` map (`ShieldNorth/East/South/West` on WASD/arrows and `Pause` on Escape/P) (C-006).
- `Assets/Settings/UniversalRP.asset` references `Assets/Settings/Renderer2D.asset`; the 2D renderer has no renderer features; SRP batcher on, HDR on, MSAA 1 (C-007, C-008).
- `ProjectSettings/ProjectSettings.asset`: product `AlgoRhythm2D`, 1920×1080 default, linear color space, new Input System only (C-003, C-009).
- The I-001 paths (`Assets/_Project/Scripts/Core/**`, `Assets/_Project/Scripts/Game/**`, `Assets/_Project/Tests/EditMode/**`) exist. `Assets/_Project/Data/**` and `Assets/_Project/Scenes/{Boot,Gameplay}.unity` remain proposed.
- No prefabs, gameplay ScriptableObject assets, Addressables groups, or save schema exist. Four asmdefs and five test files exist (C-004, C-014).

## 10. Feature state split

| Feature | Source | Wiring | Execution |
|---|---|---|---|
| Rhythm/Timing | I-001 Core source | runtime bootstrap | EditMode tests + bootstrap smoke observed; input-driven Play Mode not_run |
| Manual 4-direction shield | I-001 Core/Game source | runtime bootstrap + `Rhythm` map | not_run |
| Rule engine / compile preview | absent | absent | not_run |
| Auto-shield rule (A1) | absent | absent | not_run |
| Combat/waves/telegraphs | I-001 debug fixture + runtime arena | runtime bootstrap | not_run (bootstrap smoke only) |
| 1A/1B wave data | absent | absent | not_run |
| Adware Decoy / energy regen | absent | absent | not_run |
| Dialogue / pause / calibration / results | absent | absent | not_run |
| Linux internal demo build | absent | absent | not_run |
| Windows clean build | absent | absent | not_run |
| A.I.D.A. narrative / boss | absent | absent | not_run |
| URP 2D rendering | package/config present | `SampleScene` camera + 2D light | not_run |
| New Input System | package + default asset + I-001 `Rhythm` map | `activeInputHandler: 1`; build settings config object; runtime router | not_run |

## 11. Claims

- [C-001 | source_verified | ProjectSettings/ProjectVersion.txt#m_EditorVersion | 84b82c7+dirty] Unity Editor version is 6000.3.8f1 (revision 1c7db571dde0). | deps: none | invalidates: package_or_unity_version_changed,toolchain_changed | limit: batchmode compile and EditMode tests ran on a disposable clone; the main editor was already open; Play Mode not run.
- [C-002 | source_verified | Packages/manifest.json#com.unity.render-pipelines.universal | 84b82c7+dirty] Universal RP 17.3.0 is declared; the lockfile resolves 17.3.0. | deps: C-001 | invalidates: package_or_unity_version_changed | limit: package source not inspected.
- [C-003 | source_verified | Packages/manifest.json#com.unity.inputsystem | 84b82c7+dirty] Input System 1.18.0 is declared; the project uses the new Input System only (`activeInputHandler: 1`). | deps: C-001 | invalidates: package_or_unity_version_changed,build_configuration_changed | limit: input behavior not exercised.
- [C-004 | source_verified | Packages/manifest.json#com.unity.test-framework | 84b82c7+dirty] Unity Test Framework 1.6.0 is declared; I-001 adds EditMode and PlayMode test asmdefs with five test files (C-030). | deps: C-001 | invalidates: package_or_unity_version_changed,test_environment_changed | limit: the PlayMode file is a bootstrap-only smoke test; V-003 manual input/scene run not performed.
- [C-005 | serialized_verified | Assets/Scenes/SampleScene.unity#Main Camera | 84b82c7+dirty] SampleScene is the only enabled build scene and contains Main Camera and Global Light 2D, with no serialized gameplay prefabs or MonoBehaviours; I-001 uses a runtime bootstrap instead of editing the scene. | deps: C-001 | invalidates: serialized_dependency_changed,entry_path_changed | limit: actual-scene Play Mode/input not run.
- [C-006 | source_verified | Assets/InputSystem_Actions.inputactions#Rhythm | 84b82c7+dirty] The input asset defines Player, UI, and I-001's Rhythm map; Rhythm has ShieldNorth/East/South/West and Pause actions with WASD/arrow and Escape/P bindings. | deps: C-003 | invalidates: source_changed | limit: bindings not exercised in Play Mode.
- [C-007 | source_verified | Assets/Settings/UniversalRP.asset#m_RendererDataList | 84b82c7+dirty] URP uses a renderer data list with default index 0. | deps: C-002 | invalidates: serialized_dependency_changed | limit: rendering not executed.
- [C-008 | source_verified | Assets/Settings/Renderer2D.asset#m_RendererFeatures | 84b82c7+dirty] The 2D renderer has no renderer features. | deps: C-007 | invalidates: serialized_dependency_changed | limit: rendering not executed.
- [C-009 | source_verified | ProjectSettings/ProjectSettings.asset#productName | 84b82c7+dirty] Product is AlgoRhythm2D; default resolution is 1920x1080; active color space is linear. | deps: C-001 | invalidates: build_configuration_changed | limit: build target not confirmed.
- [C-010 | source_verified | ProjectSettings/EditorSettings.asset#m_EnterPlayModeOptionsEnabled | 84b82c7+dirty] Assets use Force Text serialization; Enter Play Mode Options is enabled, but domain reload and scene reload remain enabled (`m_EnterPlayModeOptions: 0`). | deps: C-001 | invalidates: source_changed,toolchain_changed | limit: serialized assets sampled, not exhaustively parsed.
- [C-011 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#WHEN_PROJECTILE | 84b82c7+dirty] The design summary proposes a 3-slot rule grammar (condition, target, action) and an initial compatibility whitelist. | deps: none | invalidates: design_authority_changed | limit: derived summary; original DOCX absent; design intent only.
- [C-012 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#BeatIntervalMs | 84b82c7+dirty] The design summary requires DSP-based beat scheduling and proposes ±70 ms PERFECT, ±130 ms GOOD, 120 ms input buffer, and ±150 ms calibration. | deps: none | invalidates: design_authority_changed | limit: derived summary; timing values are [U] proposals. I-001 implements the DSP clock, boundaries, calibration clamp, and input buffer configuration; input-driven Play Mode is not observed.
- [C-013 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#D-03 | 84b82c7+dirty] The design summary leaves D-01 through D-12 open, including the A.I.D.A. acronym, Core Database content, HP/Integrity/lives, retry model, keyboard-only requirement, final token count, Over-Reliance ending, minimum hardware, audio licensing, wave duration, WebGL, and final resource/action names. | deps: none | invalidates: design_authority_changed | limit: derived summary; original GDD and owner decisions not inspected.
- [C-014 | source_verified | Assets/_Project/Scripts/Core/AlgoRhythm.Core.asmdef | 84b82c7+dirty] I-001 adds Core/Game/test asmdefs, runtime C# source, and five test files; `SampleScene` remains free of serialized gameplay components, and a runtime `GameBootstrap` owns Unity wiring. | deps: C-005,C-006 | invalidates: source_changed,serialized_dependency_changed | limit: no prefabs or serialized gameplay scene binding; Play Mode input/scene behavior not run.
- [C-015 | unknown | ProjectSettings/ProjectSettings.asset#scriptingBackend | 84b82c7+dirty] No active Windows build target or build profile is recorded in ProjectSettings; Windows-only is design intent, not confirmed build configuration. | deps: C-001 | invalidates: build_configuration_changed | limit: active target may be stored in Library/EditorUserBuildSettings.asset; only LinuxStandaloneSupport was observed in PlaybackEngines.
- [C-016 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Adware | 84b82c7+dirty] The demo scope is Adware sub-waves 1A and 1B (1C optional); boss/betrayal, upgrades/dilemmas, ending scoring, save/load, tempo map, adaptive music, shaders, full remapping, telemetry, sectors 2–5, VContainer, and Addressables are out of scope. | deps: C-011 | invalidates: design_authority_changed | limit: demo scope only; production GDD still covers the full game.
- [C-017 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#AlgoRhythm.Core | 84b82c7+dirty] The demo plan uses AlgoRhythm.Core (pure C#, noEngineReferences) and AlgoRhythm.Game asmdefs plus a test asmdef; manual constructor injection, Action<T> events, immutable Core data, enum FSM, DSP timing, one input action map, and pooling. | deps: C-016 | invalidates: design_authority_changed,source_changed | limit: I-001 implements the asmdef split, immutable Core data, Action<T> events, DSP timing, and the Rhythm map; rule, pooling, and full-demo architecture remain pending.
- [C-018 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#A1 | 84b82c7+dirty] Session decision: A1 auto-shield is independent of manual input, spends Energy on fire, respects cooldown, and is capped at Good. Set A adds manual precedence, no-energy suppression without cost, auto-Good scoring, decoy −5 without Good, and cooldown-on-fire; manual shield remains the route for other directions and Perfect. | deps: C-016 | invalidates: design_authority_changed | limit: demo semantics; production manual-shield/rule interaction still needs owner confirmation.
- [C-019 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#A2 | 84b82c7+dirty] Session decision: Integrity + Energy are a demo-only two-resource choice; the design summary is internally inconsistent (one main resource vs Integrity 100 + Energy 6). In-wave regen +1 per 4 beats is a demo addition; final model and names remain production decisions. | deps: C-016,C-013 | invalidates: design_authority_changed | limit: demo only; UI/economy production decision open.
- [C-020 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#A3 | 84b82c7+dirty] Demo assumptions: rules commit at any time and activate on the next beat; BPM is fixed at 100; failure retries the current sub-wave. | deps: C-016 | invalidates: design_authority_changed | limit: demo only; tempo map and production safe window deferred.
- [C-021 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Hari 1 | 84b82c7+dirty] The demo is scheduled as three increments: Day 1 rhythm/combat, Day 2 rule engine and waves, Day 3 narrative, stability, and Linux demo build. | deps: C-016 | invalidates: design_authority_changed | limit: schedule is a target; cut rules apply.
- [C-022 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Linux | 84b82c7+dirty] Session decision: build a Linux internal demo first; the Windows clean build remains the [D] release gate and has not been run. | deps: C-015 | invalidates: design_authority_changed,build_configuration_changed | limit: Linux build is a local artifact, not a target change.
- [C-023 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Jangan pernah dipotong | 84b82c7+dirty] The demo cut rules prioritize beat clock/judge accuracy, rule validation and errors, the Linux demo build (V-011), and the Windows release gate (V-012); 1B, WHEN_BEAT, decoy, and calibration may be cut. If Windows is unavailable, item 8b stays pending. | deps: C-021 | invalidates: design_authority_changed | limit: schedule risk remains high.
- [C-024 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#2.0 setelah clear | 84b82c7+dirty] The demo shows A.I.D.A. 2.0 after 1B clear; the GDD summary mapping places 2.0 after sector 1 (including 1C). | deps: C-016,C-026 | invalidates: design_authority_changed | limit: narrative compression for demo; not production canon.
- [C-025 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#Safe window | 84b82c7+dirty] The design summary's §17 is a derived gap analysis, not original GDD: #1 manual shield vs `RAISE_SHIELD`, #5 safe window, #6 upgrade replacement, #7 in-wave Energy regen, and #8 RU/OV ending metrics are derived findings. | deps: C-011 | invalidates: design_authority_changed | limit: derived analysis; not original GDD; original DOCX absent.
- [C-026 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#Pemetaan transisi | 84b82c7+dirty] The design summary maps A.I.D.A. 2.0 after sector 1, 3.0 after sector 2, 4.0 after sector 3, and 5.0 after betrayal. | deps: none | invalidates: design_authority_changed | limit: derived summary; original DOCX absent.
- [C-027 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#P1 Greybox | 84b82c7+dirty] The design summary roadmap estimates P1 Greybox 1–2 weeks, P2 Rule MVP 1–2 weeks, and P3 Vertical slice 2–3 weeks (roughly 4–7 weeks). | deps: none | invalidates: design_authority_changed | limit: derived summary estimate; original DOCX absent.
- [C-028 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#RuleBonus | 84b82c7+dirty] The design summary defines Score = Perfect×100 + Good×60 − Miss×40 − DamageTaken×25 + RuleBonus (min 0) [U]; RuleBonus and auto-shield scoring are undefined. | deps: none | invalidates: design_authority_changed | limit: derived summary formula only; no tuning.
- [C-029 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#E006 | 84b82c7+dirty] The design summary defines rule error taxonomy E001 empty slot, E002 wrong category, E003 unsupported combination, E004 insufficient resource, E005 cooldown, and E006 target not found. | deps: C-011 | invalidates: design_authority_changed | limit: derived summary; original DOCX absent.
- [C-030 | execution_verified | Assets/_Project/Tests/EditMode/BeatClockTests.cs | 84b82c7+dirty] I-001's EditMode suite passed 19/19 in a disposable clone with Unity 6000.3.8f1; it covers DSP beat boundaries, pause/resume origin shift, ±70/±130 ms judging with an epsilon at exact boundaries, calibration clamp, idempotent resolve, damage/scoring, and allocation-free `WaveRunner.Due` overflow behavior (V-002). A separate PlayMode bootstrap smoke test also passed 1/1. | deps: C-004,C-014 | invalidates: test_environment_changed,source_changed | limit: disposable clone only; the PlayMode test does not drive input; no build or player observation; scene integration remains unverified.

## 12. Detailed findings

### F-001 Manual shield vs RAISE_SHIELD rule is unresolved in production
Class: design conflict | Confidence: high | Basis: static
Evidence: C-011, C-025, C-018
Impact and trigger conditions: The demo uses A1/Set A (manual precedence, no-energy suppression, auto-Good, decoy −5 without Good), but production still has no confirmed semantics; rule/combat architecture may change after the demo.
Smallest follow-up and acceptance: V-004 records the demo decision; V-008 verifies the A1 Set A integration. Production semantics still need owner confirmation before production rule work.

### F-002 I-001 source exists but Play Mode integration is unverified
Class: missing integration/content | Confidence: high | Basis: static
Evidence: C-005, C-014, C-030
Impact and trigger conditions: I-001 adds the DSP clock, judge, input router, combat state, debug wave, runtime arena/HUD, a passed EditMode suite, and a bootstrap-only PlayMode smoke. The scene still has no serialized gameplay binding; input-driven Play Mode and the actual game scene are unobserved; rule, content, narrative, and build paths remain absent.
Smallest follow-up and acceptance: V-003; run the runtime bootstrap in Play Mode and verify correct/wrong/late/pause behavior. V-002 is already observed.

### F-003 I-001 has tests; input-driven PlayMode, build, and later systems remain unverified
Class: verification gap | Confidence: high | Basis: static
Evidence: C-004, C-014, C-015, C-030
Impact and trigger conditions: The I-001 EditMode suite detects core timing/combat regressions and a bootstrap-only PlayMode smoke test starts the runtime. There is no input-driven PlayMode test, CI/build entry point, or confirmed Windows target. Later rule/wave behavior is untested.
Smallest follow-up and acceptance: V-003 for the manual PlayMode path; V-012 confirms the Windows clean build; V-011 is only the Linux demo artifact.

### F-004 GDD summary's one-resource guidance conflicts with its own Integrity/Energy values
Class: design conflict | Confidence: high | Basis: static
Evidence: C-013, C-019
Impact and trigger conditions: The summary recommends one main resource but lists Integrity 100 + Energy 6; the demo keeps both and adds in-wave regen (+1 per 4 beats), increasing UI/economy complexity. Final model and names D-03/D-12 remain open.
Smallest follow-up and acceptance: V-004 confirms A2 as demo-only; V-008 checks the Energy/cooldown behavior. Production D-03/D-12 remains open.

### F-005 Three-day schedule is much tighter than the design-summary estimates
Class: source-supported risk | Confidence: high | Basis: static
Evidence: C-021, C-023, C-027
Impact and trigger conditions: The design summary estimates P1–P3 at roughly 4–7 weeks; the demo compresses them to 3 days, so the cut path (1A only, one rule combination) is likely.
Smallest follow-up and acceptance: V-009 and V-011; apply the cut rules before adding scope.

### F-006 Windows clean build cannot be verified on the current workstation
Class: verification gap | Confidence: high | Basis: static
Evidence: C-015, C-022
Impact and trigger conditions: Only LinuxStandaloneSupport is installed; no Windows Build Support, Wine, or clean Windows machine was observed. The demo's Windows acceptance item cannot be met locally.
Smallest follow-up and acceptance: V-012; V-011 is an internal Linux artifact only, not a substitute.

### F-007 Rule validation contract must include E006 and a cooldown context
Class: source-supported risk | Confidence: high | Basis: static
Evidence: C-017, C-029
Impact and trigger conditions: The demo plan previously listed E001–E005 and `Validate(draft, energy)` had no cooldown input. The plan now specifies E001–E006, `Validate(draft, in RuleValidationContext)` with Energy/CooldownRemainingMs, and E006 as a runtime target-resolution error; no source or test exists yet.
Smallest follow-up and acceptance: V-007; verify the E001–E006 contract and the context in I-002.

### F-008 `WaveRunner.Due` must avoid hot-path allocation
Class: source-supported risk | Confidence: high | Basis: static
Evidence: C-017
Impact and trigger conditions: The plan specifies `int Due(int, Span<ThreatEvent> buffer)` with a preallocated buffer. I-001 implements it and tests overflow/catch-up behavior, but the spawn loop has not been profiled in Play Mode and later wave content may stress it.
Smallest follow-up and acceptance: V-009 and a profiler check; verify the implementation remains allocation-free in the spawn loop.

### F-009 Auto-shield tuning and ScoreService inputs need implementation tests
Class: verification gap | Confidence: medium | Basis: static
Evidence: C-018, C-028
Impact and trigger conditions: `CombatState` now implements the base score formula, min 0, damage penalty, and RuleBonus=0 for I-001; auto-shield scoring, decoy −5, and cost/cooldown remain Day 2 work and lack integration tests.
Smallest follow-up and acceptance: V-008; verify the ScoreService inputs and keep RuleBonus at 0 unless the owner sets another value.

### F-010 A.I.D.A. 2.0 after 1B is a narrative compression
Class: design conflict | Confidence: medium | Basis: static
Evidence: C-024, C-026
Impact and trigger conditions: The demo shows 2.0 after 1B; the design summary maps 2.0 after sector 1 (including 1C). Treating the demo as canon would create an inconsistency.
Smallest follow-up and acceptance: V-004 records the demo compression; V-010 checks the dialogue sequence. Keep the `[A]` label in dialogue data and docs.

## 12b. Closed findings

- None at this snapshot.

## 13. Overflow finding stubs

- None at this snapshot.

## 14. Available tools and tests

### Project test/build tooling

- Unity Test Framework 1.6.0 is installed; the I-001 EditMode and PlayMode test assemblies exist (C-004). A disposable clone was compiled and tested with Unity 6000.3.8f1: EditMode 19/19 and PlayMode bootstrap smoke 1/1 (C-030, V-002). No CI workflow, build script, or build profile is present.
- Unity Editor 6000.3.8f1 is installed at `/home/zxwyvern/Unity/Hub/Editor/6000.3.8f1/Editor/Unity`. The main project editor was already open, so a disposable clone at `/tmp/opencode/AlgoRhythm2D-test` was used for batchmode compile/tests; no Play Mode player run or build was observed.
- Python 3.14.8 is available for the pack tools.
- Node.js 26.11.1 and npm 12.2.1 are available; the external MCP candidate below is not configured.
- Demo build environment: only `LinuxStandaloneSupport` is installed in `PlaybackEngines`; Windows Build Support is absent, `wine` is absent, and no clean Windows machine was observed. V-011 can build Linux; V-012 remains blocked.

### Demo plan

- `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` is the near-term scope and decision record. Its §0 records the 2026-10-20 session decisions (A1 independent auto-shield plus Set A edge rules, A2 two-resource demo choice, Linux internal build first, 3 increments).
- It is a target plan, not observed gameplay. The demo plan is the source for routes R-001–R-004 and increments I-001–I-003.

### External Unity MCP candidate — AnkleBreaker Unity MCP

- Repository: `https://github.com/AnkleBreaker-Studio/unity-mcp-server.git`
- Inspected checkout: commit `9c84ff9fe22db1202bc003a56910f3add7a18d08`, package version 2.36.0, cloned read-only to `/tmp/opencode/unity-mcp-server` for grounding.
- Purpose: MCP stdio server that connects an AI assistant to one or more running Unity Editors. The README describes 347 named operations and 80 exposed MCP tools, with lazy advanced-tool discovery; core areas include scene/GameObject/component authoring, assets and prefabs, scripts, compilation errors, console, EditMode/PlayMode test jobs, builds, screenshots, profiler/memory, packages, and Unity Hub.
- Requirements: Node.js 18 or newer; the companion Unity plugin `https://github.com/AnkleBreaker-Studio/unity-mcp-plugin.git#main`; a running Unity Editor bridge (default port 7890, discovery range 7890–7899); an MCP client entry that spawns `node <checkout>/src/index.js`.
- Current state: **not configured** in `~/.config/opencode/opencode.json` (only `context7` is present); **not installed** in this project (no package reference); the bridge and any tool call were **not exercised**. The demo does not depend on it.
- Compatibility limits: the inspected manifest declares `win32` and `darwin` platforms, not Linux. The README's live validation is Windows/Unity 6000.6.2f1; this project is Linux/Unity 6000.3.8f1. Treat Linux and Unity 6.3 support as `unknown` until a read-only ping and inspection test succeed.
- License: AnkleBreaker Open License v1.0. Attribution ("Made with AnkleBreaker MCP" or "Powered by AnkleBreaker MCP") is required for distributed products; reselling the software is prohibited. Review with the owner before distributing a build that integrates it.
- Safe-use rules if enabled:
  - Select the intended Unity instance before any editor call; do not rely on an implicit default when multiple editors are open.
  - Start with read-only calls (`unity_editor_ping`, `unity_editor_state`, `unity_project_info`, `unity_get_compilation_errors`, `unity_scene_info`, test discovery) before writes.
  - Treat `unity_execute_code`, scene/asset writes, package changes, builds, and Play Mode control as privileged mutations: require a task contract, an explicit target, and post-action verification.
  - Never use `unity_execute_code` to bypass the rule-engine whitelist or to inject arbitrary gameplay at runtime.
  - After a timeout or failed call, inspect the project before retrying a write; the server documents that an outcome may be unknown.
  - Do not install or configure this MCP as part of an unrelated task.

## 15. Not inspected

- Original GDD DOCX (absent from repository).
- Unity Editor input-driven Play Mode, builds, players, profiler, and screenshots. The EditMode suite and a bootstrap-only PlayMode smoke test were observed in a disposable clone; the live project and actual game scene were not exercised.
- Windows Build Support, a Linux demo player, a clean Windows machine, and Wine behavior.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, and other generated/cache directories.
- Package source and embedded package internals; only declared/locked versions were read.
- Active build target/build profile (not stored in inspected `ProjectSettings`; may be in `Library/`).
- AnkleBreaker Unity MCP source beyond README, package/manifest, license, and selected docs; companion plugin source; live bridge behavior.
- Rule, content, UI, audio, narrative, and wave-asset source. I-001 source and its runtime greybox presentation were inspected; later gameplay systems remain future work.
- Git history beyond HEAD; only the current working tree and `git status`/`git log -5` were observed.
