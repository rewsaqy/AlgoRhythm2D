# AlgoRhythm2D — Agent Guide

## Role and authority

You implement and verify AlgoRhythm2D, a Unity 6 2D rhythm-shielding / live-coding prototype. The design reference is `docs/ALGORHYTHM_GAME_CONTEXT.md`, a derived GDD summary; the original DOCX is absent, and §13.5/§17 are analysis, not original GDD. The near-term demo authority is `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md`. Honor the `[D]` confirmed, `[U]` proposed, `[V]` open, `[E]` engineering, and `[A]` demo-assumption labels. Do not invent lore, tokens, mechanics, or tuning values. The repository is currently a template with no gameplay source; `project-context.md` is the factual snapshot.

If the GDD and demo plan conflict on a `[D]` item, the original GDD wins when available. If they conflict on demo scope, the demo plan wins for the demo only. Do not promote a demo assumption to production truth without an owner decision.

Architecture policy: `conform` to the demo plan's minimal architecture: `AlgoRhythm.Core` (pure C#, `noEngineReferences`) and `AlgoRhythm.Game`, plus a test assembly; manual constructor injection; `Action<T>` events; immutable Core data mapped from ScriptableObjects. Do not add VContainer, Addressables, a message bus, or a larger assembly split during the demo.

## Start here

1. Read applicable repository instructions and this file. When resuming, read `docs/ai-workflow/session-handoff.md`.
2. From the repository root, run `python docs/ai-workflow/tools/context.py . --pack docs/ai-workflow` to list routes; add `--route R-001` (or `R-002` / `R-003` / `R-004`) for the selected route. R-001 needs about 20.8k, R-002 about 27.8k, R-003 about 28.5k, and R-004 about 30.3k. If the default exits 2, rerun with `--max-chars 22000` for R-001 or `--max-chars 32000` for R-002–R-004 (or the size the tool reports).
3. Read the route, the demo-plan sections it names, the source/assets, and the relevant claims in `project-context.md`. Check claim freshness before relying on it.
4. If Python is unavailable, use `docs/ai-workflow/index.json` and read the selected route, its claims and `deps:`, protected areas, and matrix checks directly. Do not load every document by default.

## Operating loop

For non-trivial work, record a task contract:

- Requested outcome and current behavior with evidence.
- Acceptance, including the important failure case.
- Existing execution/state path and the smallest complete change.
- Constraints, design authority, and protected areas.
- Verification plan with exact checks.

Work sequentially through the demo days. Do not start features outside demo scope (§1.2 of the demo plan): boss/betrayal, upgrades/dilemmas, ending scoring, save/load, tempo map, adaptive music, shaders, full remapping, telemetry, sectors 2–5, VContainer, or Addressables.

Trace before editing: definitions, callers, serialization, scene wiring, tests, and ownership. Extend the existing system; do not add a framework or rewrite for a prototype. A complete change includes callers, registrations, serialized binding, tests, and failure behavior.

Unity integrity: preserve `.meta` identity on moves/renames; account for serialized field/type/namespace changes, prefab overrides, editor-only assemblies, event reentrancy, async/coroutine/tween cleanup, and input/camera/audio/UI ownership. Never hand-edit `Packages/packages-lock.json`. Measure before claiming performance.

Verify by risk, not by sweeping: diff/reference integrity, Unity compilation/import, affected EditMode/PlayMode tests, real scene/input interaction, and build identity when the claim requires it. A textual C# review is not Unity compilation.

## Invariants

- Beat authority is `AudioSettings.dspTime`; no gameplay judging from `Time.time` or animation frames.
- One final judge result per threat event; timeout misses are resolved centrally by the combat controller.
- Rule content is enum/whitelist data; never execute strings or free-text as code.
- A1 demo semantics: auto-shield is independent of manual input, spends Energy on fire, respects cooldown, and is capped at Good; manual shield remains the route for other directions and Perfect. Production semantics still need owner confirmation.
- A2 demo semantics: Integrity + Energy are a demo-only two-resource choice; the design summary is internally inconsistent about one vs two resources, and +1/4-beat regen is a demo addition. Do not treat them as the final production resource model.
- A3/A4/A5 demo semantics: rules commit anytime and activate next beat; BPM is fixed at 100; failure retries the current sub-wave.
- Every threat has a visible/audible telegraph and at least one valid response; no unfair failure.
- Never rely on color alone; pair with icon, shape, or label.
- `[D]` decisions (Unity 6, Windows PC, 5×3 waves, 3 slots, 3-phase boss, A.I.D.A. versions) are not changed without explicit owner approval.
- Windows is the release target. A Linux build is a local internal demo artifact only; it does not satisfy the Windows clean-build acceptance.
- Content, tuning values, waves, enemies, upgrades, and dialogue are data-driven; `[U]`/demo numbers are defaults, not hard-coded truth.
- Scope stays Windows single-player; no WebGL or multiplayer work unless explicitly re-approved.
- Do not write real malware code or instructions; malware behavior is fictional abstraction.
- No gameplay change is implied by workflow-pack maintenance.

## Task routes

### R-001 Rhythm timing and manual shield
Start: demo plan §4, §5, and §9 Hari 1; GDD §4 (`BeatIntervalMs`, `TimingJudge`); `Assets/_Project/Scripts/Core/Rhythm/`; `Assets/_Project/Scripts/Game/`; `Assets/InputSystem_Actions.inputactions`; `Assets/Scenes/SampleScene.unity` (C-005, C-006, C-012, C-014, C-030).
State owner and scene/assets: I-001 source exists: pure-C# `BeatClock` / `TimingJudge` in `Core/Rhythm/`; `DspClockSource`, `InputRouter`, `CombatController`, and presentation components in `Game/`; `GameBootstrap` is a serialized scene composition root that owns only pure-C# service wiring (C-014, C-031). `SampleScene` contains a manually authored `AlgoRhythm` hierarchy with `ArenaPresenter`, `HudView`, `MetronomePlayer`, `InputRouter`, 12 `ThreatView` objects, 4 `ShieldView` objects, and 8 `AudioSource` children; runtime scripts create no GameObjects.
Invariants: DSP is the only clock; input timestamps use one conversion path; ±70/±130 ms windows; calibration ±150 ms; one final result per event; no `Time.time` judging.
Protected areas: P-005, P-006, P-007.
Required integration step: I-001 created the Core/Game/test assemblies, added the `Rhythm` action map, manually authored the greybox scene through the Unity MCP, and wired one telegraphed debug cue. Play Mode verification passed V-003 via a user-driven run to WAVE CLEAR.
Checks: V-002 (passed in EditMode), V-003 (passed, user-driven).

### R-002 Three-slot rule engine and auto-shield
Start: demo plan §4, §5, and §9 Hari 2; GDD §5 (`WHEN_PROJECTILE`, E001–E006, compatibility whitelist) (C-011, C-029, C-016, C-018).
State owner and scene/assets: future pure-C# `RuleDefinition`, `RuleWhitelist`, `RuleValidator`, `RuleRuntime` under `Assets/_Project/Scripts/Core/Rules/`; `CodePanelView` under `Game/Presentation/`; no rule source or assets exist (C-014, C-017).
Invariants: three slots; enum/whitelist validation; preview before commit; specific error codes; no arbitrary code execution; A1 auto-shield independent, max Good, Energy on fire, cooldown; manual shield remains primary. Set A: manual non-Miss precedence; no-energy suppression without cost; auto counts Good; decoy −5 without Good; cooldown starts on fire.
Protected areas: P-004, P-005, P-008.
Required integration step: resolve the demo decision record for A1/A2, implement the validator/runtime with tests, implement the defined cost/cooldown/RuleBonus=0 values, then wire the code panel and rule data.
Checks: V-004, V-007, V-008.

### R-003 Waves, decoy, energy, and transitions
Start: demo plan §6, §7, and §9 Hari 2; GDD §7 (`wave_id`, `spawn_sequence`, `clear_condition`, archetypes) (C-011, C-016, C-017).
State owner and scene/assets: I-001 adds `WaveData` / `WaveRunner` under `Core/Waves/` and an allocation-free debug fixture; `WaveDefinitionSO`, `WaveMapper`, content assets, and a wave director remain future work (C-014, C-017, C-030).
Invariants: 1A/1B use unique wave IDs; every threat telegraphs before impact; retry the current sub-wave on failure; no double resolve.
Demo defaults [U]/[A]: decoy does no damage and costs -5 score if shielded (auto-shield decoy also has no Good); Energy regenerates +1 per 4 beats and +1 on wave clear.
Protected areas: P-004, P-005, P-007, P-008.
Required integration step: define the schema and one `WaveData` fixture first, then the SO assets; verify start, clear, retry, decoy, and 1A→1B transition. Use `int Due(int, Span<ThreatEvent>)` with a preallocated buffer in the spawn loop.
Checks: V-006, V-009.

### R-004 Narrative, pause, calibration, results, and build
Start: demo plan §8, §9 Hari 3, and §12; GDD §9 and §12 (C-016, C-021, C-022, C-024, C-026).
State owner and scene/assets: I-001 adds `HudView`, `ArenaPresenter`, `ThreatView`, `ShieldView`, and `MetronomePlayer` under `Game/Presentation/`; these update manually authored scene visuals rather than creating GameObjects. `DialogueView`, `PauseView`, `ResultsView`, `GameFlow`, dialogue/UI data, and results assets remain future work (C-014, C-031).
Invariants: text goes through `text_id`; A.I.D.A. 1.0 in the tutorial and 2.0 after 1B is a demo compression, not production canon; pause shifts the DSP origin and stops/resumes audio; calibration ±150 ms persists in `PlayerPrefs`; non-color cues; no crash on pause/resume, alt-tab, retry, or wave completion.
Protected areas: P-004, P-005, P-007, P-008.
Required integration step: wire dialogue and results, add a single pause coordinator, persist calibration, run the interaction checks, then produce the Linux internal demo artifact. The Windows clean build remains V-012.
Checks: V-005, V-010, V-011, V-012.

## Increments

### I-001 Day 1 — rhythm and combat greybox
Hard depends on: none
Soft depends on: none
Verification depends on: V-002
Exit check: V-003
Status: done

Scope: `BeatClock` + `TimingJudge` + `DspClockSource` + `InputRouter` + a Straight Shooter debug threat + one 1A greybox wave + basic Integrity/Energy/beat HUD + a scheduled metronome/audio placeholder + EditMode boundary tests + PlayMode manual check. Implemented in source, EditMode tests, actual-scene PlayMode smoke, and a user-driven V-003 run to WAVE CLEAR. Out of scope: rule engine, 1B, decoy, dialogue, results, save, and build.

### I-002 Day 2 — rule engine and 1A→1B
Hard depends on: I-001
Soft depends on: none
Verification depends on: V-006, V-007, V-008
Exit check: V-009
Status: done

Scope: `RuleWhitelist` + `RuleValidator` + `RuleRuntime` with A1 auto-shield; 3-slot code panel with preview/errors and safe reset; `WaveDefinitionSO`/`WaveMapper` and W1A/W1B/W1C/W1D/W1T/WS1/WS2; Adware Decoy; PTS Shop economy; 4 new enemy archetypes; Energy regen; wave-clear summary; 1A→1B transition and retry. Fully verified in EditMode tests (87/87) and user-driven PlayMode runs (V-008, V-009). Out of scope: production rule content, dialogue, build.

### I-003 Day 3 — narrative, stability, and Linux demo build
Hard depends on: I-002
Soft depends on: none
Verification depends on: V-005, V-010
Exit check: V-011
Status: not_started

Scope: `DialogueView` with A.I.D.A. 1.0/2.0; pause/alt-tab handling; calibration slider + `PlayerPrefs`; non-color cues; `ResultsView` + restart; Linux internal demo build and smoke test. V-012 Windows clean build is a separate release gate and is not required for I-003 to be `done`; the final report must still mark Windows acceptance pending.

## Evidence, tools, and completion

- Record evidence once in `project-context.md`; status lives only in `validation-matrix.md`. Never duplicate pass/fail in the handoff.
- A check is `passed` only after a named runner/interaction produced an observed result. A textual review is not execution.
- Report separately: source implemented; serialized references checked; scene/tool behavior verified; target player/build verified; pending checks.
- Update `session-handoff.md` with the current state and next executable increment; do not copy the not-inspected list.
- Start with the smallest relevant check; broaden only when shared behavior, governance, or public workflows changed.
- V-011 Linux is not a substitute for V-012 Windows. Do not mark the demo's Windows acceptance item complete without an observed Windows player run.

## Tool limits

- AnkleBreaker Unity MCP is configured in `~/.gemini/config/mcp_config.json` with the companion plugin `com.anklebreaker.unity-mcp` (v2.41.4) installed in `Packages/manifest.json`. The bridge is verified active on port 7891 with 0 compilation errors and 87/87 EditMode tests passed. Context7 MCP is configured globally for live documentation lookups.
- MCP safe-use: select the correct editor, prefer read-only calls, treat code execution/scene/asset writes/builds/Play Mode control as privileged mutations, and inspect after timeouts before retrying a write.
- The pack tools (`context.py`, `validate_pack.py`) only inspect workflow documents; they do not prove game behavior.
- The Unity Editor may be open; check `Temp/UnityLockfile` and running processes before batch-mode work. Current observation: lockfile present, no editor process.
- Windows Build Support is not installed and `wine` is absent; only LinuxStandaloneSupport is available. Plan V-011 accordingly and keep V-012 blocked until the environment exists.

## Protected areas

Cite IDs; details live in `project-context.md` §3. At minimum: P-001 vendored kit, P-002 pre-existing deletions, P-003 URP/settings changes, P-004 derived design summary, P-005 packages/project settings, P-006 generated dirs, P-007 SampleScene, P-008 demo plan/session decisions.
