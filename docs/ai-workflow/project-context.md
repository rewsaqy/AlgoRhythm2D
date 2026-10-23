# Project Context — AlgoRhythm2D

## 1. Provenance and coverage

- Observed: 2026-10-23, source kind `git working tree`, revision `fd9ed3b`, dirty `false`.
- Repository root: `/home/zxwyvern/AlgoRhythm2D` (one Unity root).
- Inspector: AI agent. The 3-day demo plan was supplied by the user on 2026-10-20 and saved as `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md`. After the I-001 source was added, the greybox scene was reworked so every GameObject is manually authored in `Assets/Scenes/SampleScene.unity` via the Unity MCP; runtime scripts no longer create GameObjects. A Unity 6000.3.8f1 batchmode compile, EditMode run, and PlayMode smoke that loads the actual scene were performed on a disposable clone; the live editor entered Play Mode twice with 0 console errors, and the user drove the full 12-threat wave to WAVE CLEAR, passing V-003. No build, profiler, or player run was performed.
- Budget: `balanced` (refresh for the 3-day demo); profile: `compact`. Deep reads were limited to startup, stack, scene, input, design context, and the demo plan. The file inventory covered `Assets/` and top-level project/package/config files; `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and `Builds/` were excluded.
- Design authority: `docs/ALGORHYTHM_GAME_CONTEXT.md` is a derived summary of `AlgoRhythm_CODE_IS_SHIELD_GDD_Full_v1.docx` (v1.0, pre-production); `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` is the near-term demo scope. The original DOCX is absent. Sections §13.5 `[E]` and §17 are derived engineering/gap analysis, not original GDD; do not treat them as `[D]`. If a derived section conflicts with a `[D]` item, the original GDD wins when available; if scope conflicts, the demo plan wins for the demo. Refresh this pack when either authority changes.
- Derived content: §13.5 `[E]`, §17 gap analysis, and the 175-combination count are authored additions, not GDD text. Claims from those sections carry a derived limit.
- Applicable instructions: parent-scope `/home/zxwyvern/AGENTS.md` (agentic-first, human integration authority); root `AGENTS.md` now carries the generated workflow section. This pack adds the detailed rules.
- Snapshot completeness limits are listed in §15.

## 2. Pack profile and tier

- Profile: `compact` (default for a focused prototype). Revisit if routes exceed 8 or gameplay scripts exceed roughly 50.
- Tier: `small` — one Unity root, 56 runtime C# scripts, 4 asmdefs, 3 scenes (`MainMenu.unity`, `Opening_1.unity`, `SampleScene.unity`), 0 prefabs, 15 test files (14 EditMode + 1 PlayMode smoke, 88 total tests: 87 EditMode + 1 PlayMode). The greybox arena and HUD are manually authored scene objects with serialized `GameBootstrap` references.

## 3. Protected areas

- [P-001] `unity-workflow-kit/**` | reason: vendored workflow-kit checkout with its own `.git`; reference only, do not edit, restage, or commit as project content.
- [P-002] `Assets/Editor.meta`, `Assets/Editor/HubForceResolve.cs`, `Assets/Editor/HubForceResolve.cs.meta` | reason: pre-existing working-tree deletions; preserve unless the task explicitly restores or replaces this editor helper.
- [P-003] `Assets/Settings/**` | reason: URP 2D renderer/settings and template scenes; pre-existing changes; do not normalize without a rendering task.
- [P-004] `docs/ALGORHYTHM_GAME_CONTEXT.md` | reason: derived design summary; §13.5/§17 are analysis, not original GDD; original DOCX absent; edit only with design-owner approval.
- [P-005] `Packages/**`, `ProjectSettings/**` | reason: package and serialized project settings; change only for a task that explicitly targets them, with version/build impact checked.
- [P-006] `Library/**`, `Temp/**`, `Logs/**`, `UserSettings/**`, `Builds/**` | reason: generated/cache/build/user state; do not edit or commit.
- [P-007] `Assets/Scenes/SampleScene.unity` | reason: the only registered scene; serialized edits require task relevance and a verified scene binding. The 2026-10-20 manual-GO task edited it with an explicit user request and verified serialized `GameBootstrap` references (C-031).
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
| Unity Editor | 6000.3.8f1 (revision 1c7db571dde0) | C-001 | live editor running on port 7891; 0 compilation errors; 87/87 EditMode tests passed via Unity MCP |
| Universal RP | 17.3.0 | C-002 | declared and locked; CRT RenderFeature active |
| Input System | 1.18.0; new-only (`activeInputHandler: 1`) | C-003 | declared and configured; Rhythm map with Aim (360°), Tap (Z/X/Click), and Pause |
| Unity Test Framework | 1.6.0 | C-004 | declared; 14 EditMode + 1 PlayMode test files; EditMode 87/87 passed on live editor |
| Demo scope | Adware 1A/1B/1C/1D + Song waves (W1T/WS1/WS2) | C-016 | content assets and multiple enemy archetypes implemented and tested |
| Demo build (internal) | Linux Standalone (local only) | C-022 | planned; not a target change |
| Python (pack tools) | 3.14.8 | tooling observation | available |
| Node.js / Unity MCP | 26.11.1 / anklebreaker-unity-mcp 2.36.0 | tooling observation | configured in `mcp_config.json`; active on port 7891 |
| Context7 MCP | @upstash/context7-mcp 4.4.4 | tooling observation | configured in `mcp_config.json`; live documentation lookup verified |
| Target platform | Windows PC (design intent) | C-015 | `[D]` release target; not confirmed in repo build settings |

## 6. Entry and startup path

- Enabled build scenes: `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Opening_1.unity`, `Assets/Scenes/SampleScene.unity` (C-005).
- Scene contents: `Main Camera` (orthographic, size 5, URP camera data), `Global Light 2D`, and a manually authored `AlgoRhythm` hierarchy with serialized `GameBootstrap`, `ArenaPresenter`, `HudView`, `MetronomePlayer`, `InputRouter`, `PaddleView`, `AidaTower`, `ShopView`, `DamageFlashView`, `IntegrityBarView`, `CoreSpin`, inactive `ThreatView` objects, inactive `ShieldView` objects, 8 `AudioSource` children, and HUD TextMesh labels (C-005, C-031).
- Serialized `GameBootstrap` under `Assets/_Project/Scripts/Game/Bootstrap/`; composes pure-C# runtime services from scene references with no `RuntimeInitializeOnLoadMethod` and no runtime GameObject creation (C-014, C-031).
- `Assets/_Project/Scripts/Core/**`, `Assets/_Project/Scripts/Game/**`, `Assets/_Project/Tests/EditMode/**`, `Assets/_Project/Data/DemoConfig.asset`, `Assets/_Project/Data/Waves/**`, materials, CRT shaders, and audio assets exist.
- Execution paths for Beatblock paddle aiming/tapping, rule validation/runtime, wave spawning with multi-archetypes, PTS shop economy, tutorial typewriter drill, and outro dialogue are implemented and tested (87/87 EditMode tests passed).

## 7. Subsystem map (deep-mapped)

| Subsystem | Intended responsibility | Current state | Key future symbols |
|---|---|---|---|
| Rhythm/Timing | DSP beat clock, impact schedule, input judge, calibration, paddle arc | Implemented in Core/Game; Beatblock paddle aim + tap judge + SongPlayer DSP playback; 87/87 EditMode passed | `BeatClock`, `TimingJudge`, `PaddleController`, `PaddleHistory`, `InputRouter`, `SongPlayer` |
| Rule Engine | 3-slot grammar, whitelist, validation, preview, auto-shield | Implemented in Core; tested in EditMode (whitelist combos, preview, E001–E006, cooldown) | `RuleValidator`, `RuleRuntime`, `RuleDefinition`, `RuleWhitelist`, `CodePanelView` |
| Combat/Waves | Threat events, idempotent resolve, waves, decoy, archetypes | Implemented; W1A–W1D, W1T, WS1, WS2; 6 archetypes (StraightShooter, Decoy, Marcher, Heavy, Sleeper, Holder) | `CombatState`, `ThreatEvent`, `WaveRunner`, `WaveMapper`, `WaveDefinitionSO` |
| Presentation/Flow | Arena, paddle, shop, tutorial, outro, HUD, dialogue | Implemented in Game/Presentation; ShopView (2-track Lv1-5), TutorialController, OutroDialogue, MainMenu, Opening_1 | `ArenaPresenter`, `PaddleView`, `ShopView`, `TutorialController`, `OutroDialogue`, `HudView` |

Bootstrap, input, and DSP adapters are foundations for these systems, not separately deep-mapped in this compact pass.

## 8. Representative end-to-end paths (I-001 implemented; demo unverified)

Combat path: `Input (N/E/S/W) → InputRouter (DSP timestamp) → BeatClock expected impact → TimingJudge → CombatController → CombatState.ResolveThreat → runtime HUD/arena feedback → wave completion`.

Rule path: `Code panel → RuleValidator (preview/error) → RuleRuntime.Commit → next beat OnBeat → auto-shield target direction → RuleFired → code-panel log/HUD`.

Current evidence: I-001 source, the `Rhythm` input map, the manually authored `SampleScene` hierarchy, the serialized bootstrap, the EditMode suite, an actual-scene PlayMode smoke, and a user-driven V-003 run to WAVE CLEAR exist (C-006, C-014, C-030, C-031, C-032). The rule, content, narrative, and build links remain absent.

## 9. Scene, prefab, and config bindings

- `Assets/Scenes/SampleScene.unity`: `Main Camera` (Camera, AudioListener, URP camera data; orthographic size 5; z = -10), `Global Light 2D`, and the manually authored `AlgoRhythm` gameplay hierarchy with `GameBootstrap`, `ArenaPresenter`, `HudView`, `MetronomePlayer`, `InputRouter`, 12 `ThreatView` objects, 4 `ShieldView` objects, and 8 `AudioSource` children; no prefab instances (C-005, C-031).
- `ProjectSettings/EditorBuildSettings.asset`: one enabled scene, `SampleScene`; the Input System settings object is wired to `Assets/InputSystem_Actions.inputactions` (C-005, C-006).
- `Assets/InputSystem_Actions.inputactions`: maps `Player` (Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint), `UI`, and I-001's `Rhythm` map (`ShieldNorth/East/South/West` on WASD/arrows and `Pause` on Escape/P) (C-006).
- `Assets/Settings/UniversalRP.asset` references `Assets/Settings/Renderer2D.asset`; the 2D renderer has no renderer features; SRP batcher on, HDR on, MSAA 1 (C-007, C-008).
- `ProjectSettings/ProjectSettings.asset`: product `AlgoRhythm2D`, 1920×1080 default, linear color space, new Input System only (C-003, C-009).
- The I-001 paths (`Assets/_Project/Scripts/Core/**`, `Assets/_Project/Scripts/Game/**`, `Assets/_Project/Tests/EditMode/**`) exist. `Assets/_Project/Data/**` and `Assets/_Project/Scenes/{Boot,Gameplay}.unity` remain proposed.
- No prefabs, gameplay ScriptableObject assets, Addressables groups, or save schema exist. Four asmdefs and five test files exist (C-004, C-014).

## 10. Feature state split

| Feature | Source | Wiring | Execution |
|---|---|---|---|
| Rhythm/Timing | Core source + PaddleController | manual scene bootstrap + PaddleView | EditMode tests + actual-scene PlayMode smoke + user-driven V-003 passed; 87 EditMode tests passed |
| Beatblock paddle & tap judge | Core/Game source | manual scene bootstrap + Rhythm map | Aim (360°), strict arc judging, tap judging (Z/X/Click) verified in EditMode tests |
| Rule engine / compile preview | Core source (`RuleValidator`, `RuleRuntime`) | CodePanelView | V-007 passed (54/54 in clone, 87/87 in current suite); PlayMode auto-shield integration pending V-008 |
| Auto-shield rule (A1) | Core source (`RuleRuntime`) | ArenaPresenter (RuleGuards) | EditMode tested; PlayMode live exercise pending V-008 |
| Combat/waves/telegraphs | Core/Game source + assets | WaveDefinitionSO / WaveMapper | W1A–W1D, W1T, WS1, WS2; multi-archetypes (Marcher, Heavy, Sleeper, Holder, Decoy); V-006 passed |
| Wave data & songs | `Assets/_Project/Data/Waves/**` | SongPlayer DSP sync | W1A–W1D, W1T, WS1, WS2, tutorial.wav, wave1.wav; verified |
| Shop & PTS economy | Core CombatState + Game ShopView | serialized Shop GO (7 labels) | 2-track Lv1-5 upgrade system (Regen + AIDA), PTS currency, ShopEconomyTests passed |
| Dialogue / narrative / outro | Core/Game source | TutorialController, OutroDialogue | Typewriter intro + drill gating 1A, natural dialogue format, OutroDialogueTests passed |
| Linux internal demo build | planned | absent | not_run (V-011) |
| Windows clean build | absent | absent | not_run (V-012) |
| URP 2D rendering | package/config present | `SampleScene` camera + 2D light + CRT feature | Edit/Play Mode rendering observed |
| New Input System | package + asset | `activeInputHandler: 1`; Aim/Tap/Pause | EditMode + PlayMode smoke observed |

## 11. Claims

- [C-001 | source_verified | ProjectSettings/ProjectVersion.txt#m_EditorVersion | 77f12cf+dirty] Unity Editor version is 6000.3.8f1 (revision 1c7db571dde0). | deps: none | invalidates: package_or_unity_version_changed,toolchain_changed | limit: batchmode compile/EditMode tests and an actual-scene PlayMode smoke ran on a disposable clone; the live editor ran Play Mode twice with 0 console errors and a user-driven V-003 wave clear.
- [C-002 | source_verified | Packages/manifest.json#com.unity.render-pipelines.universal | 77f12cf+dirty] Universal RP 17.3.0 is declared; the lockfile resolves 17.3.0. | deps: C-001 | invalidates: package_or_unity_version_changed | limit: package source not inspected.
- [C-003 | source_verified | Packages/manifest.json#com.unity.inputsystem | 77f12cf+dirty] Input System 1.18.0 is declared; the project uses the new Input System only (`activeInputHandler: 1`). | deps: C-001 | invalidates: package_or_unity_version_changed,build_configuration_changed | limit: input behavior not exercised.
- [C-004 | source_verified | Packages/manifest.json#com.unity.test-framework | 77f12cf+dirty] Unity Test Framework 1.6.0 is declared; I-001 adds EditMode and PlayMode test asmdefs with five test files (C-030). | deps: C-001 | invalidates: package_or_unity_version_changed,test_environment_changed | limit: the PlayMode file is a scene-loading smoke test; V-003 manual input/pause run not performed.
- [C-005 | serialized_verified | Assets/Scenes/SampleScene.unity#Main Camera | 77f12cf+dirty] SampleScene is the only enabled build scene; it contains Main Camera, Global Light 2D, and the manually authored `AlgoRhythm` gameplay hierarchy with serialized components and references. | deps: C-001 | invalidates: serialized_dependency_changed,entry_path_changed | limit: manual scene is authored and PlayMode-rendered, and the user exercised W/A/S/D and pause input through V-003.
- [C-006 | source_verified | Assets/InputSystem_Actions.inputactions#Rhythm | 77f12cf+dirty] The input asset defines Player, UI, and I-001's Rhythm map; Rhythm has ShieldNorth/East/South/West and Pause actions with WASD/arrow and Escape/P bindings. | deps: C-003 | invalidates: source_changed | limit: bindings not exercised in Play Mode.
- [C-007 | source_verified | Assets/Settings/UniversalRP.asset#m_RendererDataList | 77f12cf+dirty] URP uses a renderer data list with default index 0. | deps: C-002 | invalidates: serialized_dependency_changed | limit: 2D rendering was observed in Edit/Play Mode, but a player build was not run.
- [C-008 | source_verified | Assets/Settings/Renderer2D.asset#m_RendererFeatures | 77f12cf+dirty] The 2D renderer has no renderer features. | deps: C-007 | invalidates: serialized_dependency_changed | limit: 2D rendering was observed in Edit/Play Mode, but a player build was not run.
- [C-009 | source_verified | ProjectSettings/ProjectSettings.asset#productName | 77f12cf+dirty] Product is AlgoRhythm2D; default resolution is 1920x1080; active color space is linear. | deps: C-001 | invalidates: build_configuration_changed | limit: build target not confirmed.
- [C-010 | source_verified | ProjectSettings/EditorSettings.asset#m_EnterPlayModeOptionsEnabled | 77f12cf+dirty] Assets use Force Text serialization; Enter Play Mode Options is enabled, but domain reload and scene reload remain enabled (`m_EnterPlayModeOptions: 0`). | deps: C-001 | invalidates: source_changed,toolchain_changed | limit: serialized assets sampled, not exhaustively parsed.
- [C-011 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#WHEN_PROJECTILE | 77f12cf+dirty] The design summary proposes a 3-slot rule grammar (condition, target, action) and an initial compatibility whitelist. | deps: none | invalidates: design_authority_changed | limit: derived summary; original DOCX absent; design intent only.
- [C-012 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#BeatIntervalMs | 77f12cf+dirty] The design summary requires DSP-based beat scheduling and proposes ±70 ms PERFECT, ±130 ms GOOD, 120 ms input buffer, and ±150 ms calibration. | deps: none | invalidates: design_authority_changed | limit: derived summary; timing values are [U] proposals. I-001 implements the DSP clock, boundaries, calibration clamp, and input buffer configuration; input-driven Play Mode is not observed.
- [C-013 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#D-03 | 77f12cf+dirty] The design summary leaves D-01 through D-12 open, including the A.I.D.A. acronym, Core Database content, HP/Integrity/lives, retry model, keyboard-only requirement, final token count, Over-Reliance ending, minimum hardware, audio licensing, wave duration, WebGL, and final resource/action names. | deps: none | invalidates: design_authority_changed | limit: derived summary; original GDD and owner decisions not inspected.
- [C-014 | source_verified | Assets/_Project/Scripts/Core/AlgoRhythm.Core.asmdef | 77f12cf+dirty] I-001 adds Core/Game/test asmdefs, runtime C# source, and five test files; `GameBootstrap` is a serialized scene composition root that creates only pure-C# services and connects them to manually authored references. | deps: C-005,C-006,C-031 | invalidates: source_changed,serialized_dependency_changed | limit: no prefabs; manual scene binding is authored, PlayMode-smoked, and user-verified through V-003.
- [C-015 | unknown | ProjectSettings/ProjectSettings.asset#scriptingBackend | 77f12cf+dirty] No active Windows build target or build profile is recorded in ProjectSettings; Windows-only is design intent, not confirmed build configuration. | deps: C-001 | invalidates: build_configuration_changed | limit: active target may be stored in Library/EditorUserBuildSettings.asset; only LinuxStandaloneSupport was observed in PlaybackEngines.
- [C-016 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Adware | 77f12cf+dirty] The demo scope is Adware sub-waves 1A and 1B (1C optional); boss/betrayal, upgrades/dilemmas, ending scoring, save/load, tempo map, adaptive music, shaders, full remapping, telemetry, sectors 2–5, VContainer, and Addressables are out of scope. | deps: C-011 | invalidates: design_authority_changed | limit: demo scope only; production GDD still covers the full game.
- [C-017 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#AlgoRhythm.Core | 77f12cf+dirty] The demo plan uses AlgoRhythm.Core (pure C#, noEngineReferences) and AlgoRhythm.Game asmdefs plus a test asmdef; manual constructor injection, Action<T> events, immutable Core data, enum FSM, DSP timing, one input action map, and pooling. | deps: C-016 | invalidates: design_authority_changed,source_changed | limit: I-001 implements the asmdef split, immutable Core data, Action<T> events, DSP timing, and the Rhythm map; rule, pooling, and full-demo architecture remain pending.
- [C-018 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#A1 | 77f12cf+dirty] Session decision: A1 auto-shield is independent of manual input, spends Energy on fire, respects cooldown, and is capped at Good. Set A adds manual precedence, no-energy suppression without cost, auto-Good scoring, decoy −5 without Good, and cooldown-on-fire; manual shield remains the route for other directions and Perfect. | deps: C-016 | invalidates: design_authority_changed | limit: demo semantics; production manual-shield/rule interaction still needs owner confirmation.
- [C-019 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#A2 | 77f12cf+dirty] Session decision: Integrity + Energy are a demo-only two-resource choice; the design summary is internally inconsistent (one main resource vs Integrity 100 + Energy 6). In-wave regen +1 per 4 beats is a demo addition; final model and names remain production decisions. | deps: C-016,C-013 | invalidates: design_authority_changed | limit: demo only; UI/economy production decision open.
- [C-020 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#A3 | 77f12cf+dirty] Demo assumptions: rules commit at any time and activate on the next beat; BPM is fixed at 100; failure retries the current sub-wave. | deps: C-016 | invalidates: design_authority_changed | limit: demo only; tempo map and production safe window deferred.
- [C-021 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Hari 1 | 77f12cf+dirty] The demo is scheduled as three increments: Day 1 rhythm/combat, Day 2 rule engine and waves, Day 3 narrative, stability, and Linux demo build. | deps: C-016 | invalidates: design_authority_changed | limit: schedule is a target; cut rules apply.
- [C-022 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Linux | 77f12cf+dirty] Session decision: build a Linux internal demo first; the Windows clean build remains the [D] release gate and has not been run. | deps: C-015 | invalidates: design_authority_changed,build_configuration_changed | limit: Linux build is a local artifact, not a target change.
- [C-023 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#Jangan pernah dipotong | 77f12cf+dirty] The demo cut rules prioritize beat clock/judge accuracy, rule validation and errors, the Linux demo build (V-011), and the Windows release gate (V-012); 1B, WHEN_BEAT, decoy, and calibration may be cut. If Windows is unavailable, item 8b stays pending. | deps: C-021 | invalidates: design_authority_changed | limit: schedule risk remains high.
- [C-024 | source_verified | docs/ALGORHYTHM_DEMO_PLAN_3DAY.md#2.0 setelah clear | 77f12cf+dirty] The demo shows A.I.D.A. 2.0 after 1B clear; the GDD summary mapping places 2.0 after sector 1 (including 1C). | deps: C-016,C-026 | invalidates: design_authority_changed | limit: narrative compression for demo; not production canon.
- [C-025 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#Safe window | 77f12cf+dirty] The design summary's §17 is a derived gap analysis, not original GDD: #1 manual shield vs `RAISE_SHIELD`, #5 safe window, #6 upgrade replacement, #7 in-wave Energy regen, and #8 RU/OV ending metrics are derived findings. | deps: C-011 | invalidates: design_authority_changed | limit: derived analysis; not original GDD; original DOCX absent.
- [C-026 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#Pemetaan transisi | 77f12cf+dirty] The design summary maps A.I.D.A. 2.0 after sector 1, 3.0 after sector 2, 4.0 after sector 3, and 5.0 after betrayal. | deps: none | invalidates: design_authority_changed | limit: derived summary; original DOCX absent.
- [C-027 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#P1 Greybox | 77f12cf+dirty] The design summary roadmap estimates P1 Greybox 1–2 weeks, P2 Rule MVP 1–2 weeks, and P3 Vertical slice 2–3 weeks (roughly 4–7 weeks). | deps: none | invalidates: design_authority_changed | limit: derived summary estimate; original DOCX absent.
- [C-028 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#RuleBonus | 77f12cf+dirty] The design summary defines Score = Perfect×100 + Good×60 − Miss×40 − DamageTaken×25 + RuleBonus (min 0) [U]; RuleBonus and auto-shield scoring are undefined. | deps: none | invalidates: design_authority_changed | limit: derived summary formula only; no tuning.
- [C-029 | source_verified | docs/ALGORHYTHM_GAME_CONTEXT.md#E006 | 77f12cf+dirty] The design summary defines rule error taxonomy E001 empty slot, E002 wrong category, E003 unsupported combination, E004 insufficient resource, E005 cooldown, and E006 target not found. | deps: C-011 | invalidates: design_authority_changed | limit: derived summary; original DOCX absent.
- [C-030 | execution_verified | Assets/_Project/Tests/EditMode/BeatClockTests.cs | 77f12cf+dirty] I-001's EditMode suite passed 19/19 in a disposable clone with Unity 6000.3.8f1; it covers DSP beat boundaries, pause/resume origin shift, ±70/±130 ms judging with an epsilon at exact boundaries, calibration clamp, idempotent resolve, damage/scoring, and allocation-free `WaveRunner.Due` overflow behavior (V-002). A separate PlayMode smoke test also passed 1/1 by loading the actual `SampleScene` and waiting through the first threat spawn/timeout. | deps: C-004,C-014,C-031 | invalidates: test_environment_changed,source_changed,serialized_dependency_changed | limit: disposable clone for the suite; the PlayMode test does not drive W/A/S/D or pause; no build or player observation.
- [C-031 | serialized_verified | Assets/Scenes/SampleScene.unity#AlgoRhythm | 77f12cf+dirty] The I-001 greybox scene is manually authored in `SampleScene` through the Unity MCP: `AlgoRhythm/GameBootstrap`, `Arena` with `Core`, four direction markers, `Grade`, 12 inactive `ThreatView` objects, four inactive `ShieldView` objects, `HUD` TextMesh labels, `Metronome` with eight `AudioSource` children, and `Input` with `InputRouter`; `GameBootstrap` references `DemoConfig`, `ArenaPresenter`, `HudView`, `MetronomePlayer`, and `InputRouter`. | deps: C-005 | invalidates: serialized_dependency_changed,source_changed | limit: manual authoring is observed in the live editor and by PlayMode render; runtime scripts contain no GameObject creation.
- [C-032 | execution_verified | Assets/Screenshots/V003_PlayStart.png | 77f12cf+dirty] The user drove the full 12-threat I-001 debug wave in live-editor Play Mode to WAVE CLEAR on 2026-10-20 (V-003): correct-direction SHIELD with PERFECT/GOOD and score, wrong-direction NO SHIELD/WRONG DIRECTION without benefit, late/untouched MISS with Integrity −10, Escape pause overlay with frozen BEAT and clean resume, exactly one status result per threat, and an audible metronome; the MCP console showed 0 errors across the run. | deps: C-005,C-014 | invalidates: test_environment_changed,serialized_dependency_changed | limit: user-driven manual run only; no automated input-driven PlayMode test exists, and audio was verified by ear on this machine.

## 12. Detailed findings

### F-003 I-001 has tests and a user-driven PlayMode run; automated input tests, build, and later systems remain unverified
Class: verification gap | Confidence: high | Basis: static
Evidence: C-004, C-014, C-015, C-030, C-032
Impact and trigger conditions: The I-001 EditMode suite detects core timing/combat regressions, an actual-scene PlayMode smoke test starts the manually authored scene, and a user-driven V-003 run verified the manual shield path. There is still no automated input-driven PlayMode test, CI/build entry point, or confirmed Windows target. Later rule/wave behavior is untested.
Smallest follow-up and acceptance: V-007/V-008 for rule tests in I-002; V-012 confirms the Windows clean build; V-011 is only the Linux demo artifact.

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

### F-010 A.I.D.A. 2.0 after 1B is a narrative compression
Class: design conflict | Confidence: medium | Basis: static
Evidence: C-024, C-026
Impact and trigger conditions: The demo shows 2.0 after 1B; the design summary maps 2.0 after sector 1 (including 1C). Treating the demo as canon would create an inconsistency.
Smallest follow-up and acceptance: V-004 records the demo compression; V-010 checks the dialogue sequence. Keep the `[A]` label in dialogue data and docs.

## 12b. Closed findings

- [F-001 | closed | V-008] Manual shield vs RAISE_SHIELD rule demo resolution: A1 Set A semantics (manual precedence, no-energy suppression, auto-Good, decoy −5) verified in Play Mode; closed 2026-10-23.
- [F-002 | closed | V-003] I-001 manual-shield Play Mode integration was unverified; closed 2026-10-20 after the user drove the full 12-threat debug wave to WAVE CLEAR with correct/wrong/late/pause/single-resolve behavior observed and 0 console errors (C-032).
- [F-004 | closed | V-008] Demo resource model confirmed as Integrity + Energy with in-wave regen and verified in Play Mode; closed 2026-10-23.
- [F-007 | closed | V-007] Rule validation contract with E001–E006, RuleValidationContext with cooldown, and runtime target-resolution error was implemented and verified; closed 2026-10-21 after V-007 passed in EditMode suite with 54/54 tests (and 87/87 in current suite).
- [F-008 | closed | V-009] WaveRunner.Due hot-path allocation-free buffer verified in EditMode tests and Play Mode wave flow; closed 2026-10-23.
- [F-009 | closed | V-008] Auto-shield tuning, decoy −5, and score inputs verified in live Play Mode; closed 2026-10-23.

## 13. Overflow finding stubs

- None at this snapshot.

## 14. Available tools and tests

### Project test/build tooling

- Unity Test Framework 1.6.0 is installed; the I-001 EditMode and PlayMode test assemblies exist (C-004). A disposable clone was compiled and tested with Unity 6000.3.8f1: EditMode 19/19 and PlayMode actual-scene smoke 1/1 (C-030, V-002). The user additionally drove the full debug wave to WAVE CLEAR in live Play Mode (C-032, V-003). No CI workflow, build script, or build profile is present.
- Unity Editor 6000.3.8f1 is installed at `/home/zxwyvern/Unity/Hub/Editor/6000.3.8f1/Editor/Unity`. The main project editor was open, so a disposable clone at `/tmp/opencode/AlgoRhythm2D-test` was used for batchmode compile/tests; the live editor ran Play Mode twice with 0 console errors, including the user-driven V-003 run. No player run or build was observed.
- Python 3.14.8 is available for the pack tools.
- Node.js 26.11.1 and npm 12.2.1 are available; AnkleBreaker Unity MCP and Context7 MCP are configured globally in `~/.gemini/config/mcp_config.json`.
- Demo build environment: only `LinuxStandaloneSupport` is installed in `PlaybackEngines`; Windows Build Support is absent, `wine` is absent, and no clean Windows machine was observed. V-011 can build Linux; V-012 remains blocked.

### Demo plan

- `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` is the near-term scope and decision record. Its §0 records the 2026-10-20 session decisions (A1 independent auto-shield plus Set A edge rules, A2 two-resource demo choice, Linux internal build first, 3 increments).
- It is a target plan, not observed gameplay. The demo plan is the source for routes R-001–R-004 and increments I-001–I-003.

### External Unity MCP — AnkleBreaker Unity MCP

- Repository: `https://github.com/AnkleBreaker-Studio/unity-mcp-server.git`
- Package version: `anklebreaker-unity-mcp` 2.36.0, companion Unity plugin `com.anklebreaker.unity-mcp` 2.41.4 in `Packages/manifest.json`.
- Purpose: MCP stdio server that connects an AI assistant to one or more running Unity Editors. Exposes 80 direct tools and lazy-loaded advanced tools (test runner, profiler, etc.).
- Current state: **configured and verified working**. Connected to live Unity Editor instance `AlgoRhythm2D` (Unity 6000.3.8f1) on port 7891; verified 0 compilation errors and 87/87 EditMode tests passed.
- Safe-use rules:
  - Select the intended Unity instance before any editor call; specify explicit port when multiple editors are running.
  - Start with read-only calls (`unity_editor_ping`, `unity_editor_state`, `unity_project_info`, `unity_get_compilation_errors`, `unity_scene_info`, test discovery) before writes.
  - Treat `unity_execute_code`, scene/asset writes, package changes, builds, and Play Mode control as privileged mutations: require a task contract, an explicit target, and post-action verification.
  - Never use `unity_execute_code` to bypass the rule-engine whitelist or to inject arbitrary gameplay at runtime.
  - After a timeout or failed call, inspect the project before retrying a write; the server documents that an outcome may be unknown.

## 15. Not inspected

- Original GDD DOCX (absent from repository).
- Unity Editor automated input-driven Play Mode, builds, players, and profiler. The EditMode suite and an actual-scene PlayMode smoke test were observed in a disposable clone; the live editor ran Play Mode twice with Edit/Play screenshots, and the user drove the debug wave to WAVE CLEAR (V-003). No automated input test exists.
- Windows Build Support, a Linux demo player, a clean Windows machine, and Wine behavior.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, and other generated/cache directories.
- Package source and embedded package internals; only declared/locked versions were read.
- Active build target/build profile (not stored in inspected `ProjectSettings`; may be in `Library/`).
- AnkleBreaker Unity MCP source beyond README, package/manifest, license, and selected docs; companion plugin source; live bridge behavior.
- Rule, content, UI, audio, narrative, and wave-asset source. I-001 source and its manually authored greybox presentation were inspected; later gameplay systems remain future work.
- Git history beyond HEAD; only the current working tree and `git status`/`git log -5` were observed.
