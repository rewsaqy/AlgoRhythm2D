# Session Handoff — AlgoRhythm2D

- Date and source snapshot: 2026-10-23; HEAD `54224d9` on `main`, in sync with `origin/main`. The only dirty files are owner tuning: `Assets/RenderFeature/CRT/CRTs/Moon.asset` and `Assets/Scenes/MainMenu.unity`. Leave both untouched. `project-context.md` and `validation-matrix.md` are still pinned at `77f12cf`; every commit after that is stale in the pack until it is refreshed.
- Goal and authorized scope: make the short demo playable end to end as `MainMenu -> Opening_1 -> SampleScene`, then `W1T (Tutorial) -> WS1 (Lagu 1) -> WS2 (Lagu 2) -> outro`. No boss fight. Out of scope: boss/betrayal fight, upgrades beyond the two shop tracks, endings, save/load, tempo map, adaptive music, shaders, sectors 2-5, VContainer, Addressables, and the enemy/AIDA-tower visual revamps.
- Demo canon: A.I.D.A. is the eventual antagonist. The friend-authored outro from `fd9ed3b` is canonical and was restored exactly in `54224d9`: AIDA's blackout line ("Core-nya aman di tanganku"), four black/white flicker cycles, then AIDA's monologue naming the OS account. Do not replace it with a third-party "someone else in the system" reveal. Do not reintroduce the removed `DATA TELAH DIJEBOL` / read-only terminal / siren / MainMenu-load variant unless the owner asks. Keep the GDD motivation for AIDA: optimization and sale value, not random malice (`docs/ALGORHYTHM_GAME_CONTEXT.md` §9.3).

## Current demo content

- Scene flow is wired in `ProjectSettings/EditorBuildSettings.asset`: `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Opening_1.unity`, `Assets/Scenes/SampleScene.unity`, in that order.
- `SampleScene` `GameBootstrap.waveDefinitions` contains exactly three assets: `W1T`, `WS1`, `WS2` (scene lines 9532-9534).
- `W1T` (`Assets/_Project/Data/Waves/W1T.asset`): 100 BPM, one practice threat (kind `StraightShooter`, direction north, impact beat 5), `Tutorial.wav` with loop on. `TutorialDriver` owns the greeting and the Perfect gate; a non-Perfect retries the drill with zero damage.
- `WS1` (`Assets/_Project/Data/Waves/WS1.asset`): Lagu 1, 100 BPM, 27 notes, impacts 10-114 with travel 8, `Wave_1.wav`.
- `WS2` (`Assets/_Project/Data/Waves/WS2.asset`): Lagu 2, 100 BPM, 27 notes with mixed kinds (Heavy, Marcher, Sleeper, Holder, Adware decoy), but `songClip: {fileID: 0}`. `SongPlayer.PlayWaveSong` stops on a null clip, so the metronome carries this wave. This is a placeholder, not the final Lagu 2.
- Shop: popup after `WS1` only, because `WS2` is the final wave. Two tracks: `REGEN` Lv0-5 and `AIDA` Lv1-5. Shared price ladder 30/55/85/120/160 PTS. Starting PTS 10; Perfect +2, Good +1. `AidaLevel` stays a hidden dependency marker for the future boss and only rises through the shop.
- Tutorials for new things: `TutorialDriver.TryConsumeShopContinue` plays a blocking pre-wave intro for `WS1` (song, PTS, shop) and `WS2` (new threat roster) before advancing. Wave-start one-liners come from `DemoDialogue.WaveStartLine`.
- Outro: `GameBootstrap.OnWaveCleared` stops the song when `AllWavesCleared` and calls `OutroDriver.Begin()`. `OutroDriver` handles blackout, flicker, AIDA's monologue, fade-out, and scene restore. It does not load `MainMenu`.

## Key files and decisions

- Run flow: `Assets/_Project/Scripts/Game/Bootstrap/GameBootstrap.cs`, `Assets/_Project/Scripts/Game/Combat/CombatController.cs`, `Assets/_Project/Scripts/Game/Combat/TutorialDriver.cs`, `Assets/_Project/Scripts/Game/Combat/WaveDirector.cs`.
- Content: `Assets/_Project/Data/Waves/{W1T,WS1,WS2}.asset`, `Assets/_Project/Data/DemoConfig.asset`, `Assets/_Project/Audio/{Tutorial,Wave_1}.wav`.
- Presentation: `Assets/_Project/Scripts/Game/Presentation/{DemoDialogue,OutroDialogue,DialogueView,ShopView,HudView,SongPlayer,SfxPlayer}.cs`, plus the friend-authored `OutroDriver.cs` and `BlackoutView.cs`.
- Outro authority: `fd9ed3b` is the owner-approved baseline for `OutroDialogue.cs`, `OutroDriver.cs`, and `OutroDialogueTests.cs`. `54224d9` restored those three exactly. My earlier teaser rewrite (third-party reveal, breach banner, terminal, siren, MainMenu load) was rejected and removed.
- Shop economy: `DemoConfig.cs` holds the price/regen/AIDA tables; `CombatState.DefaultStartingBits = 10`; `AidaLevel` is shop-only and capped at 5.
- Protected areas P-001 through P-008 still apply. Do not touch the vendored CRT/TMP trees or the two dirty owner files as part of gameplay work.

## Blockers and limits

- Lagu 2 audio is still missing. Only `Tutorial.wav` (116 beats) and `Wave_1.wav` (124 beats) exist; `WS2.songClip` is null. Either import the clip or get explicit owner approval to ship the metronome placeholder.
- The charts end at impact beat 114 while the songs run 124 beats. Confirm in the playthrough whether the wave clear cuts the last ~6 seconds of music; do not claim song alignment until observed.
- No manual full playthrough of this exact HEAD has been recorded: `MainMenu -> Opening_1 -> SampleScene -> tutorial -> WS1 -> shop -> WS2 -> outro`. No Windows player run either.
- The live Unity editor is open (lockfile present) and currently reports 0 compilation errors / not compiling. Batchmode clone remains the safe test path. Scene/asset writes, Play Mode control, code execution, and builds require explicit owner authorization plus a post-write inspection.
- `project-context.md` and `validation-matrix.md` are stale at `77f12cf`; they do not cover the song run, the shop revamp, the per-wave tutorials, or the outro.

## Evidence and status

- Status: see validation matrix (`validation-matrix.md`); the evidence record belongs in `project-context.md`. Both are stale at `77f12cf`, so refresh the pack before claiming any check.
- The live editor reported no compilation errors at handoff time, but that is an observation, not a matrix status. A disposable batchmode clone was used for the latest test run; record it in `project-context.md` before treating it as evidence.

## Next executable increment

1. Resolve Lagu 2: import the clip and assign `WS2.songClip`, or get explicit approval for the metronome placeholder.
2. Run the exact manual playthrough on this HEAD in the live editor. Capture MainMenu, Opening_1, tutorial, WS1, shop, WS2, and outro. Confirm 0 console errors, AIDA as the outro reveal, and no third-party line.
3. During that run, check JetBrainsMono rendering and overflow in the enlarged dialogue/shop layouts, and check whether WS1/WS2 clear before the song is cut.
4. Update `project-context.md` evidence and `validation-matrix.md` statuses for the demo flow; only then claim checks.
5. Commit only approved fixes. Keep `Moon.asset` and `MainMenu.unity` out of commits unless the owner says otherwise.

## Resume instructions

- Read root `AGENTS.md`, `docs/ai-workflow/agent.md`, and this file. Run `python docs/ai-workflow/tools/context.py . --pack docs/ai-workflow`; use `--max-chars 32000` for R-002 through R-004 if the default exits 2.
- For Unity work, ping read-only first. The editor instance is on port 7890. Treat scene/asset writes, Play Mode control, code execution, and builds as privileged mutations.
