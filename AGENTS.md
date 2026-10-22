# Repository Instructions — AlgoRhythm2D

<!-- ai-workflow:begin -->
## Scope

This repository is a Unity 6.3 project for AlgoRhythm: CODE IS SHIELD. Workflow pack: `docs/ai-workflow/`. Design reference: `docs/ALGORHYTHM_GAME_CONTEXT.md`, a derived GDD summary; the original DOCX is absent, and §13.5/§17 are analysis, not original GDD. Near-term demo authority: `docs/ALGORHYTHM_DEMO_PLAN_3DAY.md` (Adware 1A/1B, three increments).

## Before work

1. Read applicable repository instructions and `docs/ai-workflow/agent.md`.
2. On resume, read `docs/ai-workflow/session-handoff.md`.
3. List routes with `python docs/ai-workflow/tools/context.py . --pack docs/ai-workflow`; load only the selected route and its dependency closure. R-001 needs about 20.8k, so rerun with `--max-chars 22000`; R-002–R-003 need about 28–29k and R-004 about 30.3k, so rerun with `--max-chars 32000` (or the reported size).
4. Check claim freshness in `project-context.md`; the excerpt is navigation, not proof.

## Non-negotiables

- Honor `[D]`/`[U]`/`[V]`/`[E]`/`[A]` labels; do not invent lore, mechanics, tokens, or tuning.
- Rhythm authority is DSP time; no judging from animation or frames.
- Rule engine is enum/whitelist only; never execute free-text as code.
- A1 demo semantics: auto-shield is independent, capped at Good, spends Energy on fire, and respects cooldown; manual shield remains primary for other directions and Perfect. Set A edge rules are in demo plan §0: manual non-Miss precedence, no-energy suppression without cost, auto counts Good, decoy −5 without Good, cooldown starts on fire. Production semantics still need owner confirmation.
- A2 is a demo-only Integrity + Energy choice; the GDD summary is internally inconsistent about one vs two resources. Do not treat it, or the +1/4-beat regen, as the production resource model.
- Work sequentially through demo Day 1–3; do not start boss, upgrades, endings, save/load, tempo map, adaptive music, shaders, sectors 2–5, VContainer, or Addressables.
- Every threat is telegraphed with at least one valid response; no color-only information.
- Preserve protected areas P-001 through P-008 from `project-context.md`; unrelated dirty files stay untouched.
- Unity 6.3 / Windows single-player is the release scope; Linux is an internal demo artifact only. No WebGL or multiplayer without explicit approval.
- Do not claim the Windows clean-build acceptance without an observed Windows player run (V-012).
- Pack generation changes workflow docs only; gameplay implementation is a separate request.
- Unity MCP is mandatory whenever the task needs the live editor (scene/asset inspection or edits, Play Mode, screenshots, console): ping read-only first and never claim bridge access without an observed read-only success; treat scene/asset writes, code execution, builds, and Play Mode control as privileged mutations that need explicit user authorization and a post-write inspection.
- Validation honesty: a check is `passed` only with an observed result; report game checks separately from pack validation.

## Verification

Use the checks in `docs/ai-workflow/validation-matrix.md`; run the smallest relevant check first. Report source, serialized wiring, scene behavior, and build evidence separately.
<!-- ai-workflow:end -->

<!-- antislop:start -->
## UI copy (antislop)

Player-facing UI text follows the antislop rules. Before writing or editing it,
load the `antislop` core and `antislop-copywriting` skills.

Scope: visible player-facing text in C#, scenes, and data assets. This includes
button labels, HUD lines, dialogue, prompts, shop rows, and status banners. It
does not include code comments, Inspector `[Header]`/`[Tooltip]` text, debug
logs, rule tokens, or text ids.

- No em dash or double hyphen used as a dash. Use a period, comma, colon, or
  parentheses (R-02).
- No fabricated numbers, names, testimonials, dates, or claims. Keep game
  fiction within existing `[D]`/`[U]` content; do not invent lore or tuning
  (R-17, R-18, R-36, R-38).
- No buzzwords, filler, fake-candid openers, signposting, chatbot closers, or
  empty social proof (R-16, R-36).
- CTAs name the real action. Keep `LANJUT` for the next wave and make
  buy/continue wording say what ENTER does (R-15).
- Keep A.I.D.A.'s voice and the existing Indonesian/English mix. Do not flatten
  it into generic UI English (R-37).
- The uppercase HUD labels, `!!` warnings, `II PAUSED` glyph, `▶` cursor, and
  `LANJUT →` arrow are established terminal identity or directional cues. Keep
  them; they are not caps-as-emphasis or decorative arrows (R-08, R-37).
- Scene-serialized text counts. Edit it through Unity MCP, never by hand-editing
  YAML. Ping read-only first, write, then read back and inspect.
- Report UI copy checks separately from gameplay/pack validation.
<!-- antislop:end -->
