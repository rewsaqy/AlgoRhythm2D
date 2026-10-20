# ALGORHYTHM — Plan Arsitektur Demo 3 Hari

> Pendamping `ALGORHYTHM_GAME_CONTEXT.md`. Dokumen ini menentukan **apa yang dibangun, bagaimana strukturnya, dan urutan kerjanya** untuk demo yang selesai dalam 3 hari. Jika bertentangan dengan GDD pada hal yang bertanda `[D]`, GDD menang. Untuk hal di luar scope demo, abaikan dulu.

Label: `[D]` dikonfirmasi GDD · `[U]` usulan · `[V]` perlu validasi · `[A]` asumsi khusus demo (ubah bila tim memutuskan lain).

> **Provenance:** `ALGORHYTHM_GAME_CONTEXT.md` adalah ringkasan turunan GDD; §13.5 `[E]` dan §17 adalah analisis turunan, bukan GDD asli. DOCX asli tidak tersedia. Jangan perlakukan bagian turunan sebagai `[D]`.

---

## 0. Session decisions (2026-10-20)

Keputusan sesi ini mengikat implementasi demo dan melengkapi asumsi `[A]` di §1.3:

- **A1 — auto-shield independen.** Rule `WHEN_PROJECTILE + arah + RAISE_SHIELD` boleh menaikkan shield pada arah target saat impact tanpa input manual untuk arah itu. Biaya Energy dipotong saat **fire**, cooldown berlaku, dan hasil otomatis maksimal **Good** (bukan Perfect). Manual shield tetap jalur utama untuk arah lain dan untuk Perfect.
- **A1 runtime — Set A.** Edge case yang mengikat demo:
  - **Manual precedence:** input manual yang resolve Perfect/Good membatalkan auto-shield untuk threat itu; tidak ada Energy/cooldown. Manual Miss tidak memblokir auto-shield; hanya hasil final yang dihitung.
  - **Energy saat fire:** jika Energy kurang, auto-shield di-suppress, tanpa Energy/cooldown, log `suppressed_no_energy`; rule tetap committed. E004 saat commit hanya berlaku untuk rule baru; runtime tetap mengecek ulang.
  - **Scoring:** auto-shield dihitung **Good**; decoy yang terkena auto-shield hanya memberi **−5**, bukan Good; satu threat satu hasil final.
  - **Decoy:** auto-shield boleh tertipu decoy; memakai 1 Energy, memulai cooldown, memberi −5, tanpa damage.
  - **Cooldown:** cooldown mulai hanya saat fire berhasil; suppress/no-target tidak memulai cooldown.
  - **Tie-break:** bila beberapa threat eligible di beat yang sama, pilih `Id` terkecil; cooldown mencegah fire berikutnya.
- **A2 — Integrity + Energy untuk demo.** Dua resource dipakai untuk demo; GDD secara internal tidak konsisten (menyarankan satu resource utama, tetapi mencantumkan Integrity 100 + Energy 6). Regen +1 per 4 beat adalah tambahan demo; keputusan produksi `D-03`/`D-12` tetap terbuka.
- **Build — Linux lokal dulu.** Linux internal demo build adalah artefak lokal untuk verifikasi. Target `[D]` tetap Windows; Windows clean build adalah release gate yang belum dijalankan.
- **Pack — 3 increment.** I-001 (Hari 1), I-002 (Hari 2), I-003 (Hari 3), dengan V-011 sebagai exit check demo Linux dan V-012 sebagai release gate Windows yang terpisah.

---

## 1. Tujuan & Scope Demo

**Tujuan:** membuktikan bahwa loop inti menyenangkan: *baca telegraph → shield tepat beat → ubah rule 3-slot → lihat efeknya*.

### 1.1 Masuk scope

| Item | Detail |
|---|---|
| Sektor | Adware saja: sub-wave **1A** dan **1B** (1C opsional bila waktu ada) |
| Ritme | BeatClock berbasis DSP, grade Perfect/Good/Miss, shield 4 arah |
| Musuh | `Straight Shooter` dan `Adware Decoy` |
| Rule engine | 3 slot, validasi, preview, error E001–E006, auto-shield rule |
| Narasi | Dialog A.I.D.A. 1.0 (tutorial) → 2.0 (setelah wave clear), subtitle |
| UI | HUD (Integrity, Energy, beat indicator), code panel, layar hasil |
| Aksesibilitas minimum | Cue non-warna, slider calibration offset, pause yang benar |
| Build | Linux internal demo build (V-011) lebih dulu; Windows release build (V-012) terpisah dan pending (§0) |

### 1.2 Di luar scope (jangan dikerjakan)

Boss & betrayal · upgrade & dilema · ending scoring · save/load · tempo map & perubahan BPM · adaptive music · shader glitch/scanline · remapping penuh · telemetry · sektor 2–5 · VContainer/Addressables.

### 1.3 Asumsi demo `[A]`

| # | Asumsi | Alasan |
|---|---|---|
| A1 | **Auto-shield independen (demo).** Rule `WHEN_PROJECTILE + arah + RAISE_SHIELD` menaikkan shield otomatis pada arah target (biaya Energy saat fire, cooldown, maksimal Good). Manual shield tetap jalur utama untuk arah lain dan Perfect. | Celah #1 GDD belum diputuskan; keputusan demo dan **Set A runtime** tercatat di §0. Konfirmasi produksi masih diperlukan. |
| A2 | **Dua resource demo:** **Integrity** (100) + **Energy** (6). Energy pulih +1 per 4 beat selama wave (regen demo), plus +1 saat wave clear. | GDD internal tidak konsisten: menyarankan satu resource utama tetapi mencantumkan Integrity 100 + Energy 6. Dua resource dan regen +1/4 beat adalah pilihan demo; keputusan produksi D-03/D-12 tetap terbuka. |
| A3 | Commit rule diizinkan **kapan saja** saat Combat; efektif di beat berikutnya. | "Safe window" belum didefinisikan (celah #5). |
| A4 | BPM tetap **100**, tanpa tempo map. | Mengurangi risiko sinkronisasi. |
| A5 | Gagal (Integrity 0) = ulang sub-wave saat ini. | Sesuai usulan D-04. |

---

## 2. Keputusan Arsitektur (disederhanakan)

| Hal | Pilihan demo | Catatan migrasi |
|---|---|---|
| Assembly | 2 asmdef: **`AlgoRhythm.Core`** (pure C#, `noEngineReferences: true`) dan **`AlgoRhythm.Game`** (Unity, refer ke Core) | Pecah lebih lanjut setelah demo |
| DI | **Tanpa library.** `GameBootstrap` merakit objek via constructor injection manual, bergantung pada interface | Bisa diganti VContainer tanpa mengubah Core |
| Event | `event Action<T>` biasa | Bisa diganti message bus |
| Konten | ScriptableObject di Game → dikonversi ke **struct/record immutable** di Core | Aset definisi tidak pernah dimutasi saat runtime |
| Flow | Enum FSM: `Tutorial → Combat → WaveClear → Results` (+ `Paused`) | Perluas saat boss ditambah |
| Waktu | Satu sumber: `BeatClock` (DSP). **Dilarang** memakai `Time.time` untuk menilai ritme | — |
| Input | New Input System, satu action map, **satu jalur timestamp** | — |
| Objek berulang | Pooling untuk threat & VFX | Nol alokasi GC di hot path |

**Aturan dependensi:** `Game → Core`. `Core` tidak pernah mereferensi `UnityEngine`. Domain tidak memanggil UI; UI berlangganan event.

---

## 3. Struktur Folder

```
Assets/_Project/
  Scripts/
    Core/                       # AlgoRhythm.Core.asmdef (noEngineReferences)
      Rhythm/    BeatClock.cs, TimingJudge.cs, TimingGrade.cs, IClockSource.cs
      Rules/     RuleDefinition.cs, RuleTokens.cs, RuleValidator.cs,
                 RuleRuntime.cs, RuleError.cs, RuleWhitelist.cs
      Combat/    CombatState.cs, ThreatEvent.cs, Direction.cs, ScoreService.cs
      Waves/     WaveData.cs, WaveRunner.cs
      Flow/      GamePhase.cs, GameFlow.cs
    Game/                       # AlgoRhythm.Game.asmdef
      Bootstrap/ GameBootstrap.cs
      Time/      DspClockSource.cs
      Input/     InputRouter.cs
      Presentation/ ArenaPresenter.cs, ThreatView.cs, CodePanelView.cs,
                    HudView.cs, DialogueView.cs, ResultsView.cs, PauseView.cs
      Data/      WaveDefinitionSO.cs, DialogueEntrySO.cs, WaveMapper.cs
  Data/Waves/    W1A.asset, W1B.asset
  Data/Dialogues/ AIDA_*.asset
  Tests/EditMode/ AlgoRhythm.Tests.asmdef
  Scenes/        Boot, Gameplay
```

---

## 4. Kontrak Utama (stub interface — implementasikan persis ini lebih dulu)

> **C# 9 baseline.** Jangan pakai `record struct` (C# 10+). Stub ini memakai `readonly struct` + constructor; field adalah kontrak demo.

```csharp
using System;
using System.Collections.Generic;

// ---------- Core/Rhythm ----------
public interface IClockSource { double NowDsp { get; } }   // Game: DspClockSource; Test: FakeClock

public enum TimingGrade { Perfect, Good, Miss }

public sealed class BeatClock
{
    public BeatClock(IClockSource src, double bpm);
    public void Start(double dspStart);          // set origin
    public void Pause();                         // simpan pauseStart
    public void Resume();                        // geser origin sebesar durasi pause
    public double BeatTime(int beatIndex);       // dspStart + n * 60/bpm
    public double NowSeconds { get; }
}

public sealed class TimingJudge
{
    public TimingJudge(double perfectMs = 70, double goodMs = 130);
    public double CalibrationOffsetMs { get; set; }       // -150..+150, clamp
    public TimingGrade Grade(double inputDsp, double expectedImpactDsp);
    // err(ms) = (input - expected)*1000 - CalibrationOffsetMs ; pakai |err|
}

// ---------- Core/Combat ----------
public enum Direction { North, East, South, West }
public enum ThreatKind { StraightShooter, AdwareDecoy }

public readonly struct ThreatEvent
{
    public int Id { get; }
    public ThreatKind Kind { get; }
    public Direction Dir { get; }
    public int SpawnBeat { get; }
    public int ImpactBeat { get; }
    public int BaseDamage { get; }

    public ThreatEvent(int id, ThreatKind kind, Direction dir, int spawnBeat, int impactBeat, int baseDamage)
    {
        Id = id; Kind = kind; Dir = dir; SpawnBeat = spawnBeat; ImpactBeat = impactBeat; BaseDamage = baseDamage;
    }
}

public readonly struct ThreatResolved
{
    public int ThreatId { get; }
    public TimingGrade Grade { get; }
    public bool FromRule { get; }
    public int Damage { get; }
    public int ScoreDelta { get; }

    public ThreatResolved(int threatId, TimingGrade grade, bool fromRule, int damage, int scoreDelta)
    {
        ThreatId = threatId; Grade = grade; FromRule = fromRule; Damage = damage; ScoreDelta = scoreDelta;
    }
}

public sealed class CombatState
{
    public int Integrity { get; }  public int Energy { get; }
    public int Score { get; }      public int Perfect, Good, Miss;
    public event Action<ThreatResolved> ThreatResolved;      // satu kali per threat
    public event Action<int> IntegrityChanged;
    // ResolveThreat(...) HARUS idempotent: panggilan kedua untuk Id sama diabaikan.
}

// ---------- Core/Rules ----------
public enum Condition { WhenBeat, WhenProjectile }                 // demo
public enum Target    { North, East, South, West, Nearest }
public enum RuleAction{ RaiseShield }                              // demo (REFLECT/PURGE dst. ditunda)

public readonly struct RuleDefinition
{
    public Condition Cond { get; }
    public Target Tgt { get; }
    public RuleAction Act { get; }
    public int EnergyCost { get; }
    public int CooldownMs { get; }
    public int Priority { get; }

    public RuleDefinition(Condition cond, Target tgt, RuleAction act, int energyCost, int cooldownMs, int priority)
    {
        Cond = cond; Tgt = tgt; Act = act; EnergyCost = energyCost; CooldownMs = cooldownMs; Priority = priority;
    }
}

public readonly struct RuleDraft
{
    public Condition? Cond { get; }
    public Target? Tgt { get; }
    public RuleAction? Act { get; }

    public RuleDraft(Condition? cond, Target? tgt, RuleAction? act)
    {
        Cond = cond; Tgt = tgt; Act = act;
    }
}

public enum RuleError
{
    None, E001_EmptySlot, E002_WrongCategory, E003_Unsupported,
    E004_NotEnoughEnergy, E005_Cooldown, E006_TargetNotFound
}

public readonly struct RuleValidationContext
{
    public int Energy { get; }
    public int CooldownRemainingMs { get; }

    public RuleValidationContext(int energy, int cooldownRemainingMs)
    {
        Energy = energy; CooldownRemainingMs = cooldownRemainingMs;
    }
}

public readonly struct RuleValidationResult
{
    public RuleError Error { get; }
    public string PreviewKey { get; }
    public bool IsValid => Error == RuleError.None;

    public RuleValidationResult(RuleError error, string previewKey)
    {
        Error = error; PreviewKey = previewKey;
    }
}

public sealed class RuleValidator
{
    // E001–E005 dicek di sini; E006 saat target resolution runtime.
    public RuleValidationResult Validate(RuleDraft draft, in RuleValidationContext context);
}

public readonly struct BeatContext
{
    public int BeatIndex { get; }
    public double DspTime { get; }

    public BeatContext(int beatIndex, double dspTime)
    {
        BeatIndex = beatIndex; DspTime = dspTime;
    }
}

public readonly struct RuleFired
{
    public int RuleId { get; }
    public int ThreatId { get; }
    public int BeatIndex { get; }
    public int EnergySpent { get; }
    public bool WasDecoy { get; }

    public RuleFired(int ruleId, int threatId, int beatIndex, int energySpent, bool wasDecoy)
    {
        RuleId = ruleId; ThreatId = threatId; BeatIndex = beatIndex; EnergySpent = energySpent; WasDecoy = wasDecoy;
    }
}

public sealed class RuleRuntime
{
    public void Commit(RuleDefinition rule);                 // aktif di beat berikutnya [A3]
    public void OnBeat(int beatIndex, in BeatContext ctx);   // evaluasi event, hasilkan efek deterministik
    // Log suppressed_no_energy/manual_precedence/cooldown/no_target tanpa efek gameplay.
    public event Action<RuleFired> RuleFired;                // untuk feedback & log
}

// ---------- Core/Waves ----------
public sealed class WaveData
{
    public int TempoBpm { get; }
    public IReadOnlyList<ThreatEvent> Threats { get; }

    public WaveData(int tempoBpm, IReadOnlyList<ThreatEvent> threats)
    {
        TempoBpm = tempoBpm; Threats = threats;
    }
}

public sealed class WaveRunner
{
    public WaveRunner(WaveData data, BeatClock clock);
    public int Due(int currentBeat, Span<ThreatEvent> buffer);   // isi buffer, kembalikan jumlah; tanpa alokasi
    public bool IsCleared { get; }
}
```

### Whitelist demo `[U]`

| Condition | Target valid | Action valid |
|---|---|---|
| WHEN_BEAT | N/E/S/W | RAISE_SHIELD |
| WHEN_PROJECTILE | N/E/S/W/NEAREST | RAISE_SHIELD |

Whitelist disimpan sebagai **data** (`RuleWhitelist`), bukan `if` bertumpuk. Rule tidak pernah dibuat dari string.

### Parameter default `[U]`

BPM 100 · Perfect ±70 ms · Good ±130 ms · input buffer 120 ms · calibration −150…+150 (step 5) · Integrity 100 · Energy 6 · rule cost 1 · cooldown 1500 ms · RuleBonus 0 · baseDamage Straight Shooter 10, Decoy 0 (penalti jika di-shield: lihat §6; auto-shield ke decoy tidak memberi Good).

Semua angka ini **harus berasal dari config/SO**, bukan konstanta tersebar.

---

## 5. Alur Runtime Satu Frame

```
InputRouter ──(arah, inputDsp)──► CombatController.OnShieldInput
                                       │
         WaveRunner.Due(beat, buffer) ─► spawn ThreatEvent ─► ArenaPresenter (telegraph)
                                       │
   TimingJudge.Grade(inputDsp, BeatClock.BeatTime(ImpactBeat))
                                       │
   arah cocok & grade ≠ Miss ? ─► shield sukses : damage
                                       │
        CombatState.ResolveThreat (idempotent) ─► events ─► HUD / Audio cue / Score
RuleRuntime.OnBeat ─► (jika rule memicu) auto-shield arah target ─► RuleFired ─► CodePanel log
```

**Aturan penilaian:**
1. Satu ancaman → satu hasil final. Cegah double resolve (flag per `ThreatEvent.Id`).
2. Threat yang impact-nya lewat melebihi Good window tanpa input = `Miss` (resolve oleh timeout di `CombatController`).
3. Input buffer: input di dalam 120 ms sebelum window disimpan dan dinilai terhadap impact terdekat dengan arah sama `[A]`.
4. Pause: `BeatClock.Pause/Resume` menjaga beat map tidak bergeser.

---

## 6. Perilaku Musuh Demo

| Arketipe | Perilaku | Counterplay | Catatan |
|---|---|---|---|
| Straight Shooter | `Idle → Telegraph → Travel → Impact → Recover`; telegraph panah arah + marker beat | Shield arah & timing benar | Telegraph minimal 2 beat sebelum impact |
| Adware Decoy | `Spawn → Distract → Expire`; popup dengan simbol **umpan** (bentuk berbeda, bukan hanya warna) | Abaikan | Menembak shield ke arah decoy = penalti kecil (−5 score `[U]`); decoy tidak pernah memberi damage |

Implementasi: satu `ThreatEvent` + `ThreatKind` memilih perilaku di Presenter; logika di Core hanya perlu tahu apakah threat berbahaya atau umpan. **Tidak perlu FSM per musuh di Core** untuk demo.

---

## 7. Data Wave Demo

`WaveDefinitionSO` → `WaveMapper` → `WaveData` (immutable, Core).

| Field | 1A | 1B |
|---|---|---|
| `tempo_bpm` | 100 | 100 |
| `difficulty_tier` | Tutorial | Normal |
| `spawn_sequence` | ~12 Straight Shooter, 1 arah per beat renggang, urutan arah bergantian | ~20 threat: Straight Shooter + Adware Decoy, selalu ada gap (min 4 beat, tidak ada note bersamaan — perintah owner) |
| `rule_unlocks` | `WHEN_PROJECTILE`, `RAISE_SHIELD` (slot sudah terisi sebagian) | + `WHEN_BEAT` |
| `dialogue_trigger` | `OnStart` (A.I.D.A. 1.0) | `AfterWaveClear` (A.I.D.A. 2.0) |
| `clear_condition` | `AllThreatsResolved` | `AllThreatsResolved` |

**Prinsip tutorial (1A):** hanya 1 slot yang berubah, 2 lainnya terisi; mekanik baru diajarkan sebelum bisa memberi damage.

---

## 8. Dialog Demo

Gunakan teks dari GDD Modul 2.5 (adegan pembuka + transisi setelah sektor 1). Disimpan sebagai `DialogueEntrySO`:
`dialogue_id`, `speaker`, `trigger`, `text_id`, `version_gate`, `interruptible`, `subtitle_duration`.
Teks diambil via `text_id` dari satu tabel string sederhana (bukan hard-coded di logika). Dialog tidak boleh memotong pola aktif; tampil saat Tutorial/WaveClear.

---

## 9. Jadwal 3 Hari

### Hari 1 — Ritme & Combat

**Output:** satu wave greybox bisa dimainkan ±1 menit dengan grade akurat.

- [ ] Setup proyek Unity 6 (6000.3.8f1), 2 asmdef + test asmdef, New Input System.
- [ ] `IClockSource`, `DspClockSource`, `BeatClock` (+ Pause/Resume).
- [ ] `TimingJudge` + **tes EditMode**: tepat di ±70, ±71, ±130, ±131 ms; offset ±150; error negatif/positif.
- [ ] `InputRouter` → satu titik konversi timestamp input ke skala DSP.
- [ ] `ThreatEvent`, `WaveRunner` (WaveData fixture sementara; ganti SO pada Hari 2).
- [ ] `ArenaPresenter`: 4 arah, telegraph (panah + marker beat), impact marker, shield visual, label grade.
- [ ] `CombatState` + `ResolveThreat` idempotent + damage floor (`max(0, …)`).
- [ ] HUD dasar: Integrity, Energy, beat indicator.
- [ ] Musik/metronom placeholder dijadwalkan dengan `PlayScheduled`.

**DoD Hari 1:** tidak ada double hit; grade deterministik; 1 menit gameplay tanpa crash.

### Hari 2 — Rule Engine & Wave

**Output:** dua sub-wave dimainkan berurutan dengan rule 3-slot yang berfungsi.

- [ ] `RuleTokens`, `RuleWhitelist` (data), `RuleValidator` (E001–E005; E006 runtime) + **tes EditMode** untuk semua kombinasi valid/invalid, slot kosong, energi kurang, cooldown; E006 diuji saat target resolution gagal.
- [ ] `RuleRuntime`: commit → aktif di beat berikutnya; auto-shield; cost & cooldown; log `suppressed_no_energy`/`manual_precedence`/`cooldown`/`no_target` `[U]`.
- [ ] `CodePanelView`: 3 slot, pilih slot (1/2/3), cycle token (Q/E/scroll), preview kalimat pemain, error spesifik di slot yang salah, tombol **Reset ke aman**.
- [ ] `WaveDefinitionSO` + `WaveMapper`; buat `W1A`, `W1B`.
- [ ] Adware Decoy (visual bentuk berbeda, penalti jika di-shield).
- [ ] Energy regen `[A2]`; ringkasan wave clear (Perfect/Good/Miss, damage, score).
- [ ] Transisi `Combat → WaveClear → Combat(1B)`; retry sub-wave saat gagal.

**DoD Hari 2:** rule valid jalan, invalid memberi pesan jelas, tanpa exception; 1A→1B mulus.

### Hari 3 — Narasi, Stabilitas, Build

**Output:** Linux internal demo build stabil dan sudah smoke test; Windows release build (V-012) tetap pending.

- [ ] `DialogueEntrySO` + `DialogueView` (subtitle); A.I.D.A. 1.0 di tutorial, 2.0 setelah clear.
- [ ] Pause/Resume benar (simulasi, musik, beat map; `Esc`).
- [ ] Slider calibration offset (−150…+150) tersimpan di `PlayerPrefs` (cukup untuk demo).
- [ ] Cue non-warna: ikon/bentuk untuk arah & status; tidak hanya warna.
- [ ] `ResultsView` + restart.
- [ ] Uji: alt-tab, pause/resume berulang, retry, selesai wave, resolusi berbeda.
- [ ] **Build Linux internal demo & smoke test** (V-011); Windows release build dan clean-machine test (V-012) adalah gate terpisah, bukan exit Day 3.
- [ ] Catat known issues + versi build (Linux); tandai Windows acceptance pending.

**DoD Hari 3:** Linux build jalan tanpa editor; tidak ada crash/softlock; pemain non-dev bisa menyelesaikan 1A tanpa penjelasan lisan; Windows V-012 tetap pending.

---

## 10. Aturan Potong (jika molor)

| Kondisi | Tindakan |
|---|---|
| Hari 1 molor > ½ hari | Buang 1B; demo = 1A saja |
| Hari 2 molor | Rule engine cukup **1 kombinasi** (`WHEN_PROJECTILE + arah + RAISE_SHIELD`); buang `WHEN_BEAT` |
| Decoy belum jadi | Pakai Straight Shooter saja di 1B, variasikan arah & kepadatan |
| Hari 3 sempit | Buang slider calibration (default 0), pertahankan pause & Linux demo build (V-011) |

**Jangan pernah dipotong:** BeatClock + judge yang akurat, validasi rule + pesan error, Linux internal demo build (V-011), dan Windows release gate (V-012). Jika Windows belum tersedia, 8b tetap pending; demo internal Linux bukan acceptance penuh.

---

## 11. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|---|---|---|
| Timestamp Input System ≠ skala `dspTime` | Penilaian terasa tidak adil | Satu titik konversi di `InputRouter`; uji dengan metronom + ketukan nyata; offset kalibrasi |
| `dspTime` tetap jalan saat pause | Beat bergeser | `BeatClock.Pause/Resume` menggeser origin sebesar durasi pause |
| Double resolve satu threat | Damage/score ganda | `ResolveThreat` idempotent per `Id` + tes |
| Dual-task overload | Pemain gagal baca kode & ritme | 1A hanya 1 slot berubah; rule bisa diedit saat jeda antar-beat padat; mode "tanpa penalti" untuk eksperimen |
| GC spike saat spawn | Stutter merusak timing | Pooling threat/VFX; tanpa LINQ/alokasi di loop |
| Asumsi A1 salah | Rework combat | Rule dibungkus `RuleRuntime`; efeknya terpisah dari `CombatState` sehingga mudah diganti |
| Build berbeda dari Play Mode | Bug audio/timing baru muncul | Tes Linux build Hari 3 wajib; Windows clean build V-012 dicek terpisah |

---

## 12. Definisi Selesai Demo (Acceptance)

1. Tutorial 1A dapat dipahami tanpa instruksi lisan.
2. Satu ancaman: telegraph → respons → grade (Perfect/Good/Miss) → konsekuensi konsisten.
3. Pemain mengedit 3 slot, melihat preview, mendapat error spesifik, dan mengaktifkan rule valid yang **berpengaruh nyata**.
4. 1A selesai → ringkasan → 1B dimulai; gagal → ulang sub-wave.
5. A.I.D.A. 1.0 muncul di awal, 2.0 setelah wave clear.
6. Visual dan audio sinkron; calibration offset berfungsi.
7. Tidak crash saat pause/resume, alt-tab, retry, atau menyelesaikan wave.
8a. Linux internal demo build jalan dan 1A/1B dapat dimainkan (V-011).
8b. Windows clean build jalan di mesin bersih (V-012); pending sampai environment tersedia.

---

## 13. Instruksi Khusus untuk Agent

- Kerjakan **berurutan per hari**; jangan lompat ke fitur di luar scope (§1.2).
- Tulis tes EditMode untuk Core **sebelum/bersamaan** dengan implementasinya.
- Jangan ubah item `[D]`. Jangan memutuskan item `[V]`; gunakan asumsi `[A]` di atas dan catat bila mengubahnya.
- Jangan menaruh logika gameplay di MonoBehaviour; MonoBehaviour hanya adapter ke/dari Core.
- Hindari singleton, `FindObjectOfType`, dan `Time.time` untuk ritme.
- Setiap konstanta gameplay dari config/SO. Setiap teks pemain lewat `text_id`.
- Jika menemukan konflik atau celah baru pada spesifikasi, **tulis di akhir PR/commit note** daripada memutuskan diam-diam.
- Setelah demo, migrasi yang direncanakan: pecah asmdef per layer, VContainer, Addressables (bila perlu), tempo map, boss.
