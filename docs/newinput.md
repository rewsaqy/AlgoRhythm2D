# Plan: Konversi Gerak & Note ke Gaya Beatblock

Project: `AlgoRhythm` (Unity 6, assembly `AlgoRhythm.Core` / `AlgoRhythm.Game`)
Dasar analisis: isi `_Project.zip` + wiki Beatblock (https://beatblock.wiki.gg/wiki/Notes)

---

## 1. Tujuan dan batasan

**Tujuan:** pemain mengendalikan paddle berputar di sekitar Core dengan mouse. Note datang dari sudut bebas (bukan hanya 4 lane). Pemain **tetap men-tap setiap note** seperti sekarang.

**Keputusan yang sudah final:**

| # | Keputusan |
|---|---|
| D1 | Semua note di-tap (perilaku sekarang dipertahankan). Beatblock hanya mengubah gerak paddle dan datangnya note |
| D2 | Tombol tap: **Z, X, dan klik kiri** |
| D3 | Grade tetap dari `TimingJudge` (Perfect ≤ 70 ms, Good ≤ 130 ms) |
| D4 | Aim memakai **posisi kursor yang diubah menjadi sudut** terhadap Core (bukan delta mouse) |
| D5 | Kalibrasi offset pindah dari Z/X ke `[` dan `]` |
| D6 | Sistem Rules/Code Panel dibekukan (sudah mati lewat `enableCodePanel = false`), tidak diporting di iterasi ini |

**Di luar scope (additive, bisa menyusul):** note Inverse dan Side, Hold bersudut melengkung, level editor.

---

## 2. Acuan Beatblock

Terverifikasi dari wiki:
- Block biasa dikenai dengan menghadapkan paddle ke arahnya.
- Tap dipicu klik kiri, Z, atau X; window 75 / 150 / 300 ms sesuai opsi aksesibilitas.
- Hold harus dijaga kontaknya sepanjang durasi; ada leniency timer.
- Mine harus dihindari; Bounce harus dipukul tiap menyentuh paddle.
- Ukuran paddle dalam derajat (contoh di wiki: 70°).
- Review pemain menyebut mouse sebagai kontrol utama.

**Belum terverifikasi (dijadikan parameter tuning, bukan keputusan keras):**
- Apakah note bergerak halus atau per-langkah.
- Apakah paddle mengikuti kursor langsung atau dengan smoothing.
- Apakah Beatblock memakai posisi absolut atau delta mouse. Cara paling pasti: coba langsung.

---

## 3. Kondisi sekarang (dari kode)

| Aspek | Implementasi sekarang | File |
|---|---|---|
| Shield | 4 slot, transit smoothstep 320 ms, cover ±30° hanya saat terparkir | `Core/Combat/ShieldController.cs` |
| Input | 4 action tombol `ShieldNorth/East/South/West` (event `performed`/`canceled`), tidak ada mouse/axis | `Game/Input/InputRouter.cs` |
| Arah note | `Direction` N/E/S/W di `ThreatEvent` | `Core/Combat/ThreatEvent.cs` |
| Spawn | Jarak beda per sumbu (E/W 8.2, N/S 4.6); `SpawnBeat` ditulis manual per note | `ArenaPresenter.cs`, `WaveDefinitionSO.cs` |
| Gerak note | Step per beat (`noteStepAttack`), lalu `Lerp` spawn→ring | `CombatController.BeatGlideProgress` (private) |
| Judgement | Input di-timestamp DSP, dinilai terhadap `ImpactBeat`; buffer `inputBufferMs`; `TOO EARLY/LATE` | `CombatController.HandleShieldPressed` |
| Kandidat note | Dipilih per lane: `FindNearestThreat(direction, inputDsp)` | `CombatController` |
| Kalibrasi | Z/X langsung dari `Keyboard.current`, langkah 5 ms, disimpan di `PlayerPrefs` | `CombatController.PollCalibration` |

Bagian Core yang dipakai ulang tanpa perubahan: `BeatClock`, `TimingJudge`, `WaveRunner`, `WaveData`, `CombatState`, `DspClockSource`.

File `.inputactions` **tidak ada di zip**, jadi binding sebenarnya tidak bisa diperiksa.

### Temuan yang memengaruhi plan

1. **Note tak terlihat tapi tetap dinilai (High).** `CombatController.SpawnDue` memasukkan threat ke `_active` sebelum `ArenaPresenter.SpawnThreat` dipanggil. `SpawnThreat` diam-diam `return` bila pool `ThreatView` habis. Chart padat bisa menghasilkan note tak terlihat yang tetap memberi damage.
2. **`CombatController` terlalu besar (High bila tidak diekstrak).** 36 KB, 15 dependency di constructor; memuat input, judgement, AIDA, kalibrasi, alur wave, dan teks HUD. Mengubah judgement di sini tanpa ekstraksi membuat regresi sulit dilacak.
3. **Kecepatan note bergantung authoring manual.** Di `W1B`, travel note North = 5 beat sedangkan East = 10 beat, dengan jarak spawn berbeda. Kecepatan hanya tampak seragam karena `SpawnBeat` ditulis tangan.
4. **`Direction` tertanam luas:** `ThreatEvent`, `ArenaPresenter` (`_blocks`, `_ruleGuards`), `InputRouter`, AIDA (`ThreatLocalPosition`), `RuleRuntime` (`Target` mengikuti urutan `Direction`).

---

## 4. Desain target

### 4.1 Perbedaan inti

| Aspek | Sekarang | Target |
|---|---|---|
| Shield | 4 slot, WASD | Paddle sudut kontinu, mouse |
| Arah note | `Direction` | `AngleDeg` bebas (0–360) + `HalfWidthDeg` |
| Hit | Tombol arah ditekan | **Tap (Z/X/klik) + paddle menutupi note saat tap** |
| Grade | `TimingJudge` | `TimingJudge` (tidak berubah) |
| Spawn | Jarak per sumbu, `SpawnBeat` manual | Radius konstan, `SpawnBeat = ImpactBeat − approachBeats` |
| Gerak note | Step per beat | Mode `Linear` (default) atau `Stepped` (yang sekarang) |

### 4.2 Alur tap yang baru

1. `Tap` masuk dengan timestamp DSP (jalur `InputRouter` yang sudah ada).
2. `paddleAngle = PaddleHistory.AngleAt(inputDsp)`; untuk klik kiri dipakai `AngleAt(inputDsp − clickAimLookbackMs)` agar kursor yang bergeser saat klik tidak merusak bidikan.
3. Kandidat = note aktif yang `Overlaps(noteAngle, halfWidth, paddleAngle, paddleHalfArc)`, dipilih yang |`inputDsp − impactDsp`| paling kecil. Hanya filter ini yang menggantikan `FindNearestThreat(direction, ...)`.
4. Logika lama berjalan tanpa perubahan: `TOO LATE`, `TOO EARLY`, buffer `inputBufferMs`, `TimingJudge.Grade`, `ApplyHit`, `StartHold`.
5. Tap yang tidak menutupi note apa pun: diam, tanpa penalti (sama seperti "pure repositioning move" sekarang).
6. Note yang tidak pernah di-tap tetap berakhir Miss lewat timeout yang sudah ada.

### 4.3 Pemetaan archetype

| Sekarang | Perilaku di model baru |
|---|---|
| StraightShooter | Tap + paddle menutupi |
| Marcher | Sama; telegraf garis tetap presentasi |
| AdwareDecoy | Tap saat paddle menutupi → −5; dibiarkan → netral (sama dengan sekarang) |
| Heavy | Tap berulang sampai `heavyArmorHits` (logika `ApplyHit` tetap) |
| Holder | Tap memulai hold; lepas tombol **yang sama** sebelum tail → Miss. (Opsional, bukan fase awal: paddle harus tetap menutupi selama hold) |
| Sleeper | Tidak berubah; profil gerak dipindah ke `NoteMotion` |

### 4.4 Arsitektur

**Core (C# murni, tanpa Unity, di-test EditMode):**

| Tipe baru | Tanggung jawab |
|---|---|
| `AngleMath` | `ShortestDelta`, `Overlaps(centerA, halfA, centerB, halfB)`; menggantikan `ShortestDelta` private di `ShieldController` |
| `PaddleHistory` | Ring buffer berukuran tetap berisi `(dsp, angle)`, tanpa alokasi; `AngleAt(dsp)` dengan interpolasi lewat arc terpendek |
| `NoteMotion` | `Progress(spawnDsp, impactDsp, nowDsp, mode, attack)`; `BeatGlideProgress` dipindah keluar dari controller |
| `PaddleController` | Sudut target, batas kecepatan sudut opsional, ukuran paddle dalam derajat |

`ThreatEvent`: tambah `AngleDeg` dan `HalfWidthDeg`. Constructor lama (`Direction`) tetap ada dan memetakan N/E/S/W → 0/90/180/270 (identik dengan `ShieldController.DirectionToAngle`), jadi asset W1A–W1D berjalan tanpa di-author ulang.

**Game:**

| Perubahan | Keterangan |
|---|---|
| `InputRouter` | Tambah `Aim`; ganti 4 event `Shield*` dengan `TapPressed(TapSource, double)` / `TapReleased(TapSource, double)` |
| `PaddleView` | Menggantikan pose orbit `ShieldView`; busur dengan lebar sesuai derajat |
| `WaveThreatEntry` | Tambah `angleDeg` dan `angleMode` (`Lane` / `Free`); asset lama otomatis `Lane`, tidak perlu migrasi |
| `WaveMapper` | Untuk `Free`: `spawnBeat = impactBeat − approachBeats` |
| `DemoConfig` | Tambah parameter di §7 dan flag `controlScheme` (Legacy / Paddle) |
| `ArenaPresenter` | Spawn radial radius konstan; `ThreatLocalPosition(angle, progress)` |

### 4.5 Spesifikasi input

Action map `Rhythm` (file `.inputactions` diedit manual di project):

| Action | Tipe | Binding |
|---|---|---|
| `Aim` | Value / Vector2 | `<Pointer>/position` |
| `TapZ` | Button | `<Keyboard>/z` |
| `TapX` | Button | `<Keyboard>/x` |
| `TapClick` | Button | `<Mouse>/leftButton` |
| `Pause` | Button | tetap |

- Tiga action tap **terpisah**, bukan satu action berbinding banyak. Pada satu action, Input System melakukan conflict resolution: bila Z masih ditahan lalu X ditekan, press X bisa tidak memicu `performed` baru, dan itu merusak tap bergantian Z-X. Uji sendiri: tekan Z lalu X bergantian cepat. Alternatifnya satu action Pass-Through yang disaring manual.
- `ShieldNorth/East/South/West` dihapus di Fase 6, bukan sebelumnya, agar mode Legacy tetap berjalan selama transisi.
- Sudut dihitung dari posisi Core di layar (`Camera.WorldToScreenPoint`): `angle = atan2(cursor.x − core.x, cursor.y − core.y)` dalam derajat kompas (0° = Utara). Ini sama dengan konvensi `ShieldController` sekarang.
- **Dead zone** di sekitar Core: di dalam radius itu sudut terakhir ditahan (mencegah jitter `atan2`).
- `Cursor.lockState = Confined` saat bermain.
- `HandleShieldPressed` sekarang mengabaikan input saat `_paused`; guard yang sama dipertahankan (klik di overlay pause tidak dihitung tap).
- `TapReleased` hanya mengakhiri hold bila `TapSource` sama dengan yang memulainya (kalau tidak, melepas Z sambil X masih ditahan menggagalkan hold). `ActiveThreat` menyimpan `HoldSource`.

### 4.6 Kalibrasi

`PollCalibration` sekarang memakai `keyboard.zKey` / `xKey`. Dipindah ke `[` dan `]` (langkah 5 ms, clamp ±150, tetap di `PlayerPrefs`). Ini **harus dikerjakan bersamaan dengan binding Z/X baru** di Fase 2; bila tidak, setiap tap akan menggeser offset kalibrasi.

Kalibrasi juga harus menggeser sampel paddle: `AngleAt(inputDsp + offset)` agar konsisten dengan `TimingJudge.ErrorMs`.

---

## 5. Fase pengerjaan

Estimasi kasar untuk 1 developer: **9,5–16,5 hari kerja**.

| Fase | Isi | Estimasi |
|---|---|---|
| 0 | Pengaman, tanpa perubahan perilaku | 1–2 hari |
| 1 | Model Core | 1–2 hari |
| 2 | Input + paddle | 2,5–3,5 hari |
| 3 | Switch judgement | 1–2 hari |
| 4 | Presentasi | 2–3 hari |
| 5 | Konten, tuning, cleanup | 2–4 hari |

### Fase 0: Pengaman (tanpa perubahan perilaku)

- Ekstrak `NoteMotion` (dari `BeatGlideProgress`) dan `AngleMath` ke Core.
- Pisahkan blok judgement dari `CombatController` menjadi kelas tersendiri (masih satu alur, hanya terpisah).
- Validasi pool `ThreatView`: hitung maksimum note aktif bersamaan dari `WaveData` saat wave dimuat; **fail keras di Editor** bila pool kurang, dan log peringatan di build. Perbaiki urutan spawn agar tidak ada note masuk `_active` tanpa view.
- **Exit:** semua test EditMode lama hijau, perilaku game identik.

### Fase 1: Model Core

- `ThreatEvent` additive (`AngleDeg`, `HalfWidthDeg`, constructor lama dipertahankan).
- `PaddleHistory`, `PaddleController`.
- `WaveThreatEntry` + `WaveMapper` untuk mode `Free`.
- **Exit:** test baru hijau untuk wrap-around 359°→1°, interpolasi arc terpendek, batas lebar tepat di tepi, kapasitas ring buffer penuh; `WaveMapperTests` diperluas untuk mode `Free` dan kompatibilitas asset lama.

### Fase 2: Input + paddle

- Edit `.inputactions` sesuai §4.5.
- `InputRouter`: `Aim`, `TapPressed/Released` dengan `TapSource`; sudut direkam ke `PaddleHistory` tiap frame dengan timestamp DSP.
- Pindahkan kalibrasi ke `[` / `]`.
- `PaddleView` + dead zone + `Cursor.lockState`.
- **Exit:** paddle mengikuti kursor di scene, `PaddleHistory` terisi, kalibrasi tidak terpicu oleh tap. Judgement belum disentuh.

### Fase 3: Switch judgement

- `HandleShieldPressed` → `HandleTap(TapSource, double)`: filter kandidat memakai `Overlaps`, selebihnya logika lama.
- `HandleShieldReleased` → `HandleTapReleased` dengan pencocokan `HoldSource`.
- Jalur Legacy tetap hidup di belakang `controlScheme`.
- **Exit:** wave W1A–W1D bisa diselesaikan di kedua mode.

### Fase 4: Presentasi

- Spawn radial radius konstan (≥ setengah diagonal viewport); kecepatan sama di semua sudut.
- Efek hit di sudut kontak menggantikan `FlashShieldBlock` per lane.
- AIDA: `ThreatLocalPosition` berbasis sudut; aim dan `TryAidaFire` disesuaikan.
- Pop beat dipertahankan.
- **Exit:** tidak ada visual yang masih bergantung pada `Direction` di jalur Paddle.

### Fase 5: Konten, tuning, cleanup

- Re-author wave dengan sudut bebas.
- Tuning parameter §7.
- Pertimbangkan importer JSON/CSV atau gizmo editor sudut; mengisi sudut lewat array inspector akan melelahkan.
- Cleanup: hapus `ShieldController`, `ShieldView` per-lane, 4 action WASD, dan flag `controlScheme`; port 6 test `ShieldControllerTests` ke `PaddleController`; putuskan nasib sistem Rules.
- **Exit:** tidak ada referensi ke jalur Legacy; semua test hijau.

---

## 6. Pengujian

**EditMode (Core, baru):** `AngleMathTests`, `PaddleHistoryTests`, `NoteMotionTests`, `PaddleControllerTests`; perluasan `WaveMapperTests`.

**EditMode (tetap, tidak boleh rusak):** `BeatClockTests`, `TimingJudgeTests`, `WaveRunnerTests`, `CombatStateTests`, `RuleValidatorTests`, `RuleRuntimeTests`.

**Port:** `ShieldControllerTests` (6 test: parkir awal, transit, retarget mid-transit, shortest path, same-slot no-op) → `PaddleControllerTests`.

**PlayMode:** perbarui `BootstrapSmokeTests` untuk memastikan input `Aim`/`Tap` terikat.

**Manual:**
- Tekan Z dan X bergantian cepat (verifikasi tiga action tidak saling memblokir).
- Tap dengan klik kiri di touchpad (verifikasi kursor tidak menggeser bidikan).
- Chart padat (verifikasi tidak ada note tak terlihat).

---

## 7. Parameter tuning (`DemoConfig`, semua `[U]`)

| Parameter | Nilai awal | Catatan |
|---|---|---|
| `paddleHalfArcDeg` | 35 (paddle 70°) | Contoh wiki Beatblock |
| `noteHalfWidthDeg` | 10 | Lebar sudut note |
| `approachBeats` | 4 | `SpawnBeat = ImpactBeat − approachBeats` |
| `noteMotionMode` | Linear | `Stepped` = perilaku sekarang |
| `aimDeadZonePx` | 40 | Radius tahan sudut di sekitar Core |
| `clickAimLookbackMs` | 10 | Hanya untuk `TapClick` |
| `paddleMaxDegPerSec` | ∞ | Smoothing opsional |
| `controlScheme` | Paddle | Legacy dihapus di Fase 5 |

Window timing tetap `perfectWindowMs = 70`, `goodWindowMs = 130`, `inputBufferMs = 120` (sebagai pembanding, Beatblock memakai 75/150/300 ms).

---

## 8. Risiko dan tradeoff

| # | Risiko | Level | Mitigasi |
|---|---|---|---|
| R1 | Note tak terlihat tapi tetap dinilai (pool habis) | High | Validasi pool saat wave dimuat, fail di Editor (Fase 0) |
| R2 | `CombatController` terlalu besar untuk diubah langsung | High | Ekstraksi di Fase 0 dengan test lama sebagai pengaman |
| R3 | Kalibrasi terpicu tap Z/X | High | Pindah ke `[` `]` bersamaan dengan binding baru (Fase 2) |
| R4 | Presisi input terbatas per frame (callback Input System umumnya diproses sekali per frame) | Medium | Interpolasi `PaddleHistory`; periksa `InputSystem.settings.updateMode` dan vsync/frame rate |
| R5 | Z lalu X bergantian tidak memicu karena conflict resolution | Medium | Tiga action terpisah; uji manual |
| R6 | Klik menggeser kursor | Medium | `clickAimLookbackMs`, hanya untuk klik |
| R7 | `atan2` jitter dekat Core, kursor keluar jendela | Medium | Dead zone + `Cursor.lockState = Confined` |
| R8 | Rules/AIDA terikat `Direction`; auto-shield tak punya padanan di model paddle | Medium | Bekukan Rules (D6); AIDA hanya adaptasi aim di Fase 4 |
| R9 | Dua mode bersamaan menggandakan permukaan test | Low–Medium | Umur flag dibatasi; hapus di Fase 5 |
| R10 | Hold gagal karena melepas tombol yang salah | Low | `HoldSource` per `ActiveThreat` |

**Tradeoff yang disengaja:**
- Pemain mengetuk di tiap note (D1) membuat Fase 3 jauh lebih kecil dan menjaga skor/grade yang sudah dituning, dengan harga: gameplay lebih dekat ke "aim + tap" daripada Beatblock murni yang block-nya otomatis.
- `TimingGrade` dipakai ulang apa adanya; tidak ada rename agar `CombatState` dan skornya tidak tersentuh.

---

## 9. Dampak per file

| File | Perubahan |
|---|---|
| `Core/Combat/ThreatEvent.cs` | Tambah `AngleDeg`, `HalfWidthDeg`; constructor lama dipertahankan |
| `Core/Combat/ShieldController.cs` | Digantikan `PaddleController`; dihapus di Fase 5 |
| `Core/…` (baru) | `AngleMath`, `PaddleHistory`, `NoteMotion`, `PaddleController` |
| `Game/Input/InputRouter.cs` | `Aim`, `TapPressed/Released` + `TapSource` |
| `Game/Combat/CombatController.cs` | Ekstraksi judgement; `HandleTap`; kalibrasi `[` `]`; filter kandidat via `Overlaps` |
| `Game/Presentation/ArenaPresenter.cs` | Spawn radial; `ThreatLocalPosition` berbasis sudut; blok shield per-lane diganti efek di sudut kontak |
| `Game/Presentation/ShieldView.cs` | Diganti `PaddleView` |
| `Game/Presentation/AidaTower.cs`, `AidaShotView.cs` | Aim berbasis sudut |
| `Game/Data/WaveDefinitionSO.cs`, `WaveMapper.cs` | `angleDeg`, `angleMode`, derivasi `SpawnBeat` |
| `Game/Config/DemoConfig.cs` (+ asset) | Parameter §7 |
| `Game/Bootstrap/GameBootstrap.cs` | Wiring `PaddleController`/`PaddleView`/`controlScheme` |
| `Tests/EditMode/*`, `Tests/PlayMode/*` | Sesuai §6 |
| `.inputactions` (tidak ada di zip) | Edit manual, §4.5 |

Catatan: sebagian file (`AidaTower`, `HudView`, `CodePanelView`, `RealMapBootstrap`, `GameBootstrap`) belum dibaca penuh. Dampak ke file-file itu diperkirakan dari referensi yang terlihat di `CombatController` dan `ArenaPresenter`, dan harus diverifikasi saat Fase 0.

---

## 10. Definition of Done

- Pemain mengarahkan paddle dengan mouse dan men-tap dengan Z / X / klik kiri.
- Note datang dari sudut bebas dengan kecepatan konstan di semua sudut.
- Grade Perfect / Good / Miss tetap dari `TimingJudge`; skor tidak berubah semantiknya.
- Semua archetype (StraightShooter, Marcher, AdwareDecoy, Heavy, Holder, Sleeper) berfungsi di model baru.
- Tidak ada note tak terlihat yang dinilai (pool tervalidasi).
- Kalibrasi berfungsi dan tidak terpicu oleh tap.
- Semua test EditMode lulus; tidak ada referensi ke jalur Legacy.