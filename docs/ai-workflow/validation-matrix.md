# Validation Matrix — AlgoRhythm2D

### V-001 Pack mechanics validation
Covers: R-001, R-002, R-003, R-004
Mode: static
Preconditions and environment: Python 3.14.8; pack at `docs/ai-workflow`; tools copied from the workflow kit.
Runner or exact manual input path: `python docs/ai-workflow/tools/validate_pack.py . docs/ai-workflow`
Expected observable result: exit 0; no validator errors; any warnings reviewed and explained.
Status: passed
Observed result and evidence location: `python docs/ai-workflow/tools/validate_pack.py . docs/ai-workflow` exited 0 with `0 error(s), 0 warning(s)` after V-003 passed and I-001 closed; `context.py` listed R-001 through R-004. R-001 needs about 20.8k, R-002 about 27.8k, R-003 about 28.5k, and R-004 about 30.3k, so use `--max-chars 22000` for R-001 or `--max-chars 32000` for R-002–R-004. Terminal output, 2026-10-20.
Remaining limit or next action: validator checks mechanics, not claim truth or gameplay behavior.

### V-002 EditMode timing boundaries
Covers: R-001, F-002
Mode: EditMode
Preconditions and environment: I-001 test assembly at `Assets/_Project/Tests/EditMode/AlgoRhythm.Tests.EditMode.asmdef`; Unity 6000.3.8f1; manually authored `SampleScene`; the main project editor was open, so the run used a disposable clone at `/tmp/opencode/AlgoRhythm2D-test` with identical project source and a symlinked package cache.
Runner or exact manual input path: `Unity -batchmode -nographics -projectPath /tmp/opencode/AlgoRhythm2D-test -runTests -testPlatform EditMode -testResults /tmp/opencode/manual-editmode-results.xml -logFile /tmp/opencode/manual-editmode.log`.
Expected observable result: `|error| <= 70` is PERFECT at the boundary, 71–130 is GOOD, >130 is MISS; calibration ±150 shifts the expected impact; one final result per event; pause/resume preserves the beat map; wave due buffers do not duplicate or lose threats.
Status: passed
Observed result and evidence location: `/tmp/opencode/manual-editmode-results.xml` records 19/19 tests passed (0 failed) on 2026-10-20 after the manual-scene rework; test files: `BeatClockTests.cs`, `TimingJudgeTests.cs`, `CombatStateTests.cs`, `WaveRunnerTests.cs`. An epsilon (`1e-6` ms) was added after two exact-boundary tests exposed binary floating-point error.
Remaining limit or next action: the run used a disposable clone, not the open live project; no input-device/build observation. Keep this check green when timing code changes.

### V-003 PlayMode manual shield integration
Covers: R-001, R-003, F-002
Mode: PlayMode
Preconditions and environment: manually authored I-001 greybox in `SampleScene`; new Input System with the `Rhythm` map; one telegraphed debug cue; editor not in a compile/reload loop.
Runner or exact manual input path: Open `Assets/Scenes/SampleScene.unity`, enter Play Mode, press W/A/S/D (or arrows) at the telegraphed cues; test correct direction, wrong direction, late input, and pause/resume with Escape/P.
Expected observable result: Correct direction inside the window yields PERFECT or GOOD; wrong direction yields no shield benefit with an explicit reason; late input yields MISS; pause/resume does not shift the DSP beat origin; no double resolve.
Status: passed
Observed result and evidence location: User drove the full 12-threat debug wave in live-editor Play Mode to WAVE CLEAR on 2026-10-20 and reported all five checks good: correct-direction key gave SHIELD plus PERFECT/GOOD with score; wrong-direction key gave NO SHIELD / WRONG DIRECTION with no benefit and an explicit status reason; late/untouched threats gave MISS with Integrity −10; Escape showed the PAUSED overlay with a frozen BEAT counter and resumed with beats continuing in sequence; each threat produced exactly one status result. The metronome was audible. MCP observed Play Mode entry, on-schedule threat spawn/timeout behavior before input, 0 console errors across the run, and live gameplay rendering (`Assets/Screenshots/V003_PlayStart.png`, `Assets/Screenshots/ManualGreybox_Play.png`). The batchmode smoke (`/tmp/opencode/manual-playmode-results.xml`, 1/1) remains a supplement, not the basis.
Remaining limit or next action: this was a user-driven manual run; no automated input-driven PlayMode test exists yet, and audio was verified by ear on this machine only.

### V-004 Demo assumptions review
Covers: R-002, F-001, F-004, F-010
Mode: static
Preconditions and environment: demo plan §0 and §1.3 current; design owner available; `docs/ALGORHYTHM_GAME_CONTEXT.md` current.
Runner or exact manual input path: Review A1–A5 and the 3-day scope against the design summary's open decisions; confirm that A1 Set A, A2 Integrity + Energy, A3 commit-next-beat, A4 fixed BPM, A5 retry, and the A.I.D.A. 2.0 compression are recorded as demo-only.
Expected observable result: The demo decisions are unambiguous and recorded in the demo plan, including A1 Set A edge cases, A2 as a demo-only choice over the summary's internal one-vs-two-resource ambiguity, and the 8a Linux/8b Windows acceptance split; remaining production decisions are named rather than silently resolved.
Status: passed
Observed result and evidence location: User approved A1 Set A, A2 as a demo-only two-resource choice, all review corrections, and derived marking on 2026-10-20; recorded in `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` §0. Production semantics and D-01–D-12 remain open.
Remaining limit or next action: This is a demo decision review only; production rule/combat semantics still require owner confirmation before R-002 production work.

### V-005 Build identity and platform
Covers: R-004, F-006
Mode: editor-interactive
Preconditions and environment: Unity Editor 6000.3.8f1 and access to the active target file; only LinuxStandaloneSupport is currently installed.
Runner or exact manual input path: Open Unity Build Settings / Build Profiles or inspect `Library/EditorUserBuildSettings.asset` and `Assets/Settings/Build Profiles/` if present; record target, development/release, scripting backend, architecture, and installed build-support modules.
Expected observable result: The release target is explicitly recorded as Windows; the missing Windows Build Support is identified; Linux is recorded as a local demo artifact, not a target change.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: active target is not in inspected `ProjectSettings`; `Assets/Settings/Build Profiles/` is absent or has no Windows profile; do not claim Windows support until checked.

### V-006 Wave data schema and telegraph review
Covers: R-003, F-005
Mode: static
Preconditions and environment: `WaveData` / `WaveDefinitionSO` and telegraph data exist after I-002 content work; demo plan §6/§7 is current.
Runner or exact manual input path: Inspect the 1A/1B data assets and validate every wave ID, event beat, telegraph lead time, decoy marker, and clear condition.
Expected observable result: 1A/1B IDs are unique; every dangerous threat has a telegraph and at least one valid response; the decoy is distinguishable by shape/icon, not color alone; no event references an invalid beat or missing target.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: no wave assets exist; define the schema in I-002 before content production.

### V-007 EditMode rule validation
Covers: R-002, F-007
Mode: EditMode
Preconditions and environment: I-002 creates the rule test assembly; `RuleWhitelist`, `RuleValidator`, and `RuleRuntime` exist; A1 Set A semantics recorded.
Runner or exact manual input path: Run the I-002 EditMode rules suite covering valid/invalid whitelist combinations, empty slots, category mismatch, unsupported actions, insufficient Energy, cooldown, preview keys, commit-next-beat activation, and E006 target resolution failure.
Expected observable result: Valid rules produce a preview and commit; invalid rules return the specific E001–E005 validation error; no string is executed; E005 uses the cooldown-aware `RuleValidationContext`; E006 is returned/logged by the runtime when target resolution fails; no duplicate fire per event.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: no rule source or test assembly exists; verify the E001–E006 contract in I-002.

### V-008 PlayMode auto-shield integration
Covers: R-002, F-001, F-009
Mode: PlayMode
Preconditions and environment: I-002 rule wiring; a `WHEN_PROJECTILE + direction + RAISE_SHIELD` rule; Energy/cooldown values; manual shield path still active; A1 Set A semantics recorded.
Runner or exact manual input path: Play the 1B scenario, commit the auto-shield rule, and observe target-direction auto-shield, manual overlap, Energy deduction on fire, cooldown, max-Good behavior, a decoy, and multiple eligible threats on one beat. Test a wrong-direction threat, a manual Perfect, insufficient Energy at fire, and manual precedence.
Expected observable result: Auto-shield fires independently on the target direction, spends Energy only on fire, respects cooldown, and yields at most Good; a manual Perfect/Good suppresses auto-shield for that threat without cost; insufficient Energy suppresses without cost; decoy auto-shield spends Energy and applies −5 without Good; single-threat-per-beat spacing is authored (no simultaneous notes per owner order — the lowest-Id tie-break stays in code and EditMode tests only); manual shield still works for other directions and can earn Perfect; no double resolve; the code panel logs fire/suppression.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: no rule or scene wiring exists; verify the explicit cost/cooldown, RuleBonus=0 default, and ScoreService inputs in I-002.

### V-009 PlayMode wave flow, decoy, energy, and retry
Covers: R-003, F-005, F-008
Mode: PlayMode
Preconditions and environment: I-002 wave assets and scene wiring; 1A/1B; Adware Decoy; Energy regen; a preallocated due-threat buffer.
Runner or exact manual input path: Play 1A to clear, observe the summary, enter 1B, shield the decoy, fail a sub-wave, retry, and complete the run. Check the spawn loop for allocations with the Profiler if available.
Expected observable result: 1A/1B start, clear, and retry correctly; the decoy does no damage and applies the -5 score penalty if shielded; Energy regenerates +1 per 4 beats and +1 on clear; no double resolve; no observed GC spike in the spawn loop.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: no wave assets or source exist; implement `int Due(int, Span<ThreatEvent>)` with a preallocated buffer.

### V-010 PlayMode pause, calibration, and results
Covers: R-004, F-005, F-010
Mode: PlayMode
Preconditions and environment: I-003 dialogue, pause, calibration, and results wiring; metronome/audio placeholder; Unity Editor or player.
Runner or exact manual input path: Pause/resume repeatedly, alt-tab, change calibration across −150…+150 in 5 ms steps, complete 1B, and restart from Results. Observe the beat origin, audio, subtitles, and persisted calibration.
Expected observable result: Pause/resume and alt-tab do not shift the beat map or desync audio; calibration persists via PlayerPrefs and shifts judging as expected; A.I.D.A. 1.0 appears in the tutorial and 2.0 after 1B clear; Results and restart work without crash or softlock.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: no UI/dialogue/audio source exists; run after I-003.

### V-011 Linux internal demo build
Covers: R-004, F-006
Mode: player-build
Preconditions and environment: V-005 build identity recorded; LinuxStandaloneSupport installed; I-003 source and scene wiring; a Linux build target. This check depends on V-005.
Runner or exact manual input path: Build a Linux Standalone player from Unity Build Settings, launch it outside the editor on this machine, and play 1A/1B with the metronome and UI.
Expected observable result: The player launches without the editor, no crash/softlock, and the core loop (telegraph → manual/auto shield → grade → wave transition) works.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: Linux is a local internal demo artifact; Linux audio/timing feel is not representative of Windows, and it does not satisfy the Windows clean-build acceptance (V-012 must recheck).

### V-012 Windows clean build
Covers: R-004, F-006
Mode: player-build
Preconditions and environment: V-005 build identity recorded; Windows Build Support installed; a clean Windows machine or VM; I-003 source and scene wiring.
Runner or exact manual input path: Build a StandaloneWindows64 player, copy it to the clean Windows machine, launch it outside the editor, and play 1A/1B with the metronome and UI.
Expected observable result: The Windows player launches without editor dependencies, no crash/softlock, and the core loop works on the clean machine.
Status: not_run
Observed result and evidence location: none.
Remaining limit or next action: Windows Build Support and a clean Windows machine are absent; V-011 is not a substitute. This remains the demo's Windows acceptance gate.
