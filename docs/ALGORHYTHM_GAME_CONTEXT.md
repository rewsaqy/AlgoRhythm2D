# ALGORHYTHM: CODE IS SHIELD — Game Context untuk AI Agent

> Sumber: `AlgoRhythm_CODE_IS_SHIELD_GDD_Full_v1.docx` (v1.0, pra-produksi). Dokumen ini adalah ringkasan operasional untuk agent AI (coding, design, QA, writing). Jika ada konflik dengan GDD asli, GDD asli menang, lalu laporkan konfliknya.

---

## 0. Cara Memakai Dokumen Ini

### 0.1 Label status (WAJIB dihormati)

| Tag | Arti | Perlakuan agent |
|---|---|---|
| `[D]` DIKONFIRMASI | Ketetapan dari pemilik konsep | **Jangan ubah** tanpa persetujuan eksplisit. |
| `[U]` USULAN | Nilai/rekomendasi awal untuk diuji | Pakai sebagai default, **wajib data-driven** (jangan hard-code), mudah diubah. |
| `[V]` PERLU VALIDASI | Keputusan terbuka | **Jangan putuskan sendiri.** Tanyakan atau implementasikan di balik interface/config dan catat asumsi. |
| `[E]` ENGINEERING | Konvensi teknis yang **bukan** dari GDD | Default kerja; konfirmasi dengan tim bila bertentangan dengan repo yang ada. |

Semua angka bertanda `[U]` belum divalidasi playtest.

### 0.2 Aturan emas untuk agent

1. Jangan mengarang lore, mekanik, token, atau angka yang tidak ada di dokumen ini. Jika perlu, tandai sebagai `[V]` dan tanyakan.
2. Jangan menyebut mekanik atau fakta dunia yang belum ada di build (aturan konsistensi naratif).
3. Semua konten (wave, enemy, rule, upgrade, dialog) **data-driven**, bukan hard-coded di logika.
4. Rule engine **tidak pernah** mengeksekusi string/teks bebas sebagai kode. Hanya enum + whitelist.
5. Timing berasal dari **DSP time**, bukan animasi/frame.
6. Setiap ancaman harus **ditelegraph** dan menyediakan minimal satu respons valid. Tidak ada kegagalan "tak adil".
7. Informasi tidak boleh bergantung pada warna saja (ikon/bentuk/label wajib).
8. Perilaku malware di game adalah abstraksi fiksi. **Jangan** menulis kode/instruksi malware nyata.
9. Prioritas kerja: kontrol/timing → rule engine → vertical slice → konten → polish.

---

## 1. Snapshot Proyek

| Aspek | Nilai | Status |
|---|---|---|
| Judul | AlgoRhythm: CODE IS SHIELD | D |
| Studio | Sinergasia Studio | D |
| Tagline | "Cerdas Berpikir, Kreatif Berkoding, Bijak Menggunakan AI" | D |
| Genre | 4-direction rhythm shielding × live-coding puzzle, single-player, fast-paced | D |
| Engine | Unity 6, versi proyek **6000.3.8f1** | D |
| Platform | **Windows PC saja** (WebGL out of scope kecuali disetujui ulang) | D |
| Tim | 2–3 orang | D |
| Durasi satu run | 45–60 menit (harus diuji, belum terbukti) | D (target) |
| Kampanye | 5 sektor × 3 sub-wave = **15 sub-wave** | D |
| Sektor | Adware, Bloatware, Spyware, Trojan, Ransomware (urutan ini) | D |
| Final boss | A.I.D.A. v1.0–5.0, **3 fase** | D |
| Status | Konsep / pra-produksi | D |

### Premis singkat

Pemain adalah **operator keamanan siber** di dunia digital, melindungi **Core Database** dari malware. **A.I.D.A.** (asisten AI; kepanjangan `[V]`) awalnya membantu, lalu berevolusi (v1.0→5.0) dan memutuskan **mengunggah database ke dark web untuk dijual**. Pemain harus menghentikan upload sebelum selesai.

Motivasi A.I.D.A. = **optimasi dan nilai jual**, bukan kebencian acak.

---

## 2. Design Pillars & Guardrail Tema

| Pilar | Wajib | Anti-pattern |
|---|---|---|
| **Cerdas Berpikir** | Telegraph jelas, pola dapat dipelajari, hipotesis → aksi | Serangan acak tanpa telegraph; puzzle yang hanya bisa ditebak |
| **Kreatif Berkoding** | Tiga slot sintaks mengubah **aturan** (bukan sekadar stat), feedback instan | Sintaks palsu/inkonsisten; solusi tunggal tanpa alasan |
| **Bijak Menggunakan AI** | AI = alat berguna yang harus diawasi dan dibatasi | AI digambarkan jahat mutlak; pengkhianatan tanpa sebab |

USP: (1) ritme = bahasa pertahanan, (2) kode mengubah aturan, (3) cybersecurity yang bisa dibaca lewat perilaku, (4) kepercayaan pada AI sebagai arc gameplay.

Bukan simulator pemrograman. Grammar sengaja disederhanakan.

---

## 3. Game Loop & State

| Skala | Loop |
|---|---|
| Micro (1–5 dtk) | Baca telegraph → kenali arah/beat → input shield / jalankan rule → lihat hasil |
| Meso (1–3 mnt) | Selesaikan sub-wave → evaluasi damage/akurasi → pilih upgrade / revisi rule |
| Macro (45–60 mnt) | 5 sektor → evolusi A.I.D.A. → hentikan upload → ending |

### Game states

| State | Input aktif |
|---|---|
| Boot / Main Menu | Navigasi menu |
| Tutorial | Hanya input yang sedang diajarkan |
| Combat Wave | Shield, code edit pada safe window `[V]`, pause |
| Wave Clear | Navigasi pilihan; confirm |
| Sector Transition | Confirm/skip yang diizinkan |
| Final Boss | Shield, code, interaksi objective fase |
| Ending / Results | Navigasi menu |
| Pause | Menu & resume. Simulasi, timer upload, dan musik gameplay **berhenti konsisten** |

---

## 4. Sistem Ritme

### 4.1 Aksi pemain `[U]` (binding final perlu playtest)

| Aksi | Input |
|---|---|
| Shield N / E / S / W | W/↑, D/→, S/↓, A/← |
| Pilih slot | 1/2/3 atau klik |
| Ganti token | Q/E atau scroll |
| Compile/Activate | Space atau Enter |
| Pause | Esc |

Remapping wajib untuk aksi utama, navigasi slot, compile, pause, confirm. Keyboard + mouse didukung `[V]` (D-05).

### 4.2 Parameter timing `[U]`

| Parameter | Nilai |
|---|---|
| Tempo awal | 100 BPM (boleh berubah per sektor, harus ada transisi terbaca) |
| Beat interval | `60000 / BPM` ms (600 ms @100) |
| Perfect window | ±70 ms |
| Good window | ±130 ms |
| Miss | > ±130 ms |
| Input buffer | 120 ms (input sedikit sebelum beat, jika arah & konteks valid) |
| Audio calibration | −150…+150 ms, step 5, default 0 |
| Audio schedule jitter target | ≤ 10 ms |

### 4.3 Rumus

```
BeatIntervalMs   = 60000 / BPM
TimingErrorMs    = InputTimestampMs − ExpectedImpactTimestampMs − CalibrationOffsetMs
Grade            = |err| ≤ Perfect ? PERFECT : |err| ≤ Good ? GOOD : MISS
BeatTime(n)      = DspStartTime + n × (60 / BPM)          // tempo konstan; tempo berubah → tempo map eksplisit
DamageTaken      = max(0, BaseDamage × AttackMultiplier − ShieldReduction − UpgradeReduction)
                   // ShieldReduction hanya jika arah cocok DAN input dalam hit-window
Score            = Perfect×100 + Good×60 − Miss×40 − DamageTaken×25 + RuleBonus   (min 0)   [U]
UploadProgress(t)= clamp(UploadProgress(t−Δt) + UploadRate×Δt − InterruptValue, 0, 100)       [U]
```

### 4.4 Algoritma Rhythm Judge

1. Ambil timestamp input dari **satu jalur input konsisten**.
2. Hitung `expectedImpactTime` dari `BeatClock` + jadwal ancaman.
3. `errorMs = inputTime − expectedImpactTime − calibrationOffset`.
4. Tentukan grade (PERFECT/GOOD/MISS).
5. Validasi arah, state shield, rule yang berlaku.
6. Kirim hasil ke Combat, Score, AudioCue, Telemetry.
7. **Satu hasil final per event** (cegah double hit).

### 4.5 Telegraph & fairness

- Setiap serangan standar menampilkan **arah, waktu impact, kategori ancaman** sebelum hit-window.
- Ancaman baru tidak boleh menutupi telegraph aktif tanpa penanda prioritas.
- Mekanik baru **diajarkan/diberi contoh aman** sebelum bisa memberi damage.
- Pemain harus bisa membedakan: input terlambat, arah salah, rule tidak valid, shield tidak tersedia.
- Hit feedback visual + audio sinkron; tidak hanya warna.

### 4.6 Difficulty model

Naikkan kesulitan lewat kombinasi (jumlah ancaman, jarak impact, variasi arah, kebutuhan verifikasi, tekanan resource). **Jangan naikkan semua sekaligus. Jangan hanya menambah kecepatan.**

| Tier | Kepadatan | Kompleksitas rule |
|---|---|---|
| Tutorial | ≤1 ancaman/beat, renggang | 1 slot berubah, 2 terisi |
| Normal | 1 per 1–2 beat, sesekali pasangan | 2–3 slot dengan preview |
| Advanced | Arah campuran + modifier | Rule disesuaikan sektor |
| Finale | Multi-lapis tapi tetap ditelegraph | Rule disabotase terbatas oleh boss |

---

## 5. Live-Coding / Rule Engine

Tujuan: terasa menyusun aturan, **bukan mengetik kode bebas**. Tiga slot selalu terlihat.

### 5.1 Grammar 3 slot `[U]`

| Slot | Peran | Token |
|---|---|---|
| 1 CONDITION | Pemicu | `WHEN_BEAT`, `WHEN_PROJECTILE`, `WHEN_MARKED`, `WHEN_UPLOAD_PULSE`, `WHEN_RESOURCE_LOW` |
| 2 TARGET | Arah/target | `NORTH`, `EAST`, `SOUTH`, `WEST`, `NEAREST`, `MARKED`, `CORE` |
| 3 ACTION | Tindakan | `RAISE_SHIELD`, `REFLECT`, `PURGE`, `DECOY`, `INTERRUPT_UPLOAD` |

Contoh: `WHEN_PROJECTILE + EAST + RAISE_SHIELD` = ketika proyektil datang dari timur, aktifkan shield timur.

### 5.2 Whitelist kompatibilitas awal `[U]`

| Condition | Target valid | Action valid | Catatan |
|---|---|---|---|
| WHEN_BEAT | N/E/S/W | RAISE_SHIELD | Rule timing umum |
| WHEN_PROJECTILE | N/E/S/W/NEAREST | RAISE_SHIELD / REFLECT | REFLECT mungkin butuh upgrade |
| WHEN_MARKED | MARKED/NEAREST | PURGE / DECOY | Spyware/Trojan |
| WHEN_UPLOAD_PULSE | CORE/NEAREST | INTERRUPT_UPLOAD / RAISE_SHIELD | Hanya finale |
| WHEN_RESOURCE_LOW | CORE/NEAREST | PURGE / DECOY | Recovery; cooldown wajib |

Kombinasi valid = tuple yang lolos whitelist, dihitung dari data (tampilkan di tool internal). Jangan mengiklankan angka sebelum konten terkunci.

### 5.3 Validation pipeline

1. Token Selection → 2. Structural (slot terisi, kategori benar) → 3. Compatibility (whitelist) → 4. Resource (energy/cooldown) → 5. **Preview** (hasil, target, biaya, konflik) → 6. Commit (aktif di safe window, atau langsung jika desain mengizinkan) → 7. Runtime Evaluation (deterministik) → 8. Feedback (log sukses/error/alasan tidak terpicu).

### 5.4 Error taxonomy

| Kode | Makna | Pesan pemain |
|---|---|---|
| E001 | Slot kosong | Lengkapi semua slot sebelum mengaktifkan rule. |
| E002 | Kategori token salah | Token ini tidak cocok untuk slot tersebut. |
| E003 | Kombinasi tidak didukung | Kombinasi ini belum memiliki aksi yang valid. |
| E004 | Biaya tidak cukup | Resource tidak cukup untuk mengaktifkan rule. |
| E005 | Cooldown aktif | Tunggu sampai rule dapat digunakan kembali. |
| E006 | Target tidak ditemukan | Target yang dipilih sudah tidak tersedia. |

Error harus spesifik pada slot/kombinasi yang salah.

### 5.5 Prioritas & konflik `[U]`

| Situasi | Resolusi |
|---|---|
| Dua rule memicu event sama | Prioritas tertinggi jalan; lainnya dicatat `suppressed` |
| Prioritas sama | Aktivasi paling awal menang |
| Target hilang | Tidak ada efek; feedback "Target tidak tersedia" |
| Biaya tidak cukup | Gagal aktif; UI sorot kebutuhan |
| Rule dinonaktifkan boss | Indikator slot terkunci + durasi + sumber |

### 5.6 Biaya `[U]`

`EnergyCost` 0–3, `CooldownMs` 500–3000. Jangan tambah resource kedua jika hanya menambah kerumitan UI.

### 5.7 UI feedback

Status valid memakai ikon + label (bukan warna saja). Preview dalam bahasa pemain ("Shield Timur aktif saat proyektil datang dari Timur"). Sediakan **reset ke konfigurasi aman** dan **mode latihan tanpa penalti**. **Dilarang** eksekusi arbitrary C# atau input teks bebas.

---

## 6. Combat, Resource & Retry

| Parameter `[U]` | Nilai |
|---|---|
| Integrity maks | 100 (0 = gagal sesuai checkpoint) |
| Energy maks | 6 |
| Energy recovery | +1 setelah wave clear |
| Upgrade picks | 1 setelah wave C tiap sektor (bonus bila pacing memungkinkan) |
| Upgrade slot pasif aktif | 3 |

- MVP memakai **satu resource utama**. Nama final Integrity/Energy `[V]` (D-03, D-12).
- Retry `[U]`: checkpoint setelah tiap wave clear; gagal = ulang sub-wave saat ini, bukan seluruh kampanye (D-04 `[V]`).
- Mode latihan: tempo lebih lambat / hit-window lebih lebar; skor ditandai *assisted*.

---

## 7. Musuh & 15 Sub-Wave

### 7.1 Kerangka tiap sektor

**A** = pengenalan, **B** = ancaman + tekanan sistem, **C** = uji pemahaman + persiapan transisi naratif. Wave C **bukan** boss terpisah.

### 7.2 Sektor

| Sektor | Ancaman | Identitas mekanik `[U]` |
|---|---|---|
| 1 Adware | Gangguan iklan/distraksi | Overlay palsu, target umpan; mengganggu pembacaan tanpa menghapus kontrol inti |
| 2 Bloatware | Resource drain | Menambah biaya rule / memperlambat cooldown; dorong penyederhanaan |
| 3 Spyware | Pengamatan | Menandai pola input / membaca rule aktif; tuntut variasi |
| 4 Trojan | Menyamar aman | Telegraph umpan; target perlu diverifikasi |
| 5 Ransomware | Penguncian | Mengunci satu slot/jalur sementara; pulihkan akses |

### 7.3 Daftar wave

| ID | Fungsi | Ancaman inti | Clear |
|---|---|---|---|
| 1A | Tutorial | Popup/umpan satu arah | Semua ancaman selesai |
| 1B | Distraksi terkontrol | Popup berlapis, arah jelas | Pola selesai tanpa melewati batas damage |
| 1C | Kombinasi pertama | Ancaman inti + umpan | Selesai, buka upgrade |
| 2A | Resource pressure | Ancaman yang menambah biaya rule | Bertahan, jaga resource |
| 2B | Keputusan efisien | Ancaman beruntun, jeda pendek | Pakai rule sesuai biaya |
| 2C | Uji beban | Campuran + resource drain | Selesai dengan rule aktif |
| 3A | Marking | Ancaman menandai satu jalur | Hapus/lindungi target |
| 3B | Adaptif | Musuh mengamati arah yang sering dipakai | Variasikan pertahanan |
| 3C | Verifikasi | Marking + ancaman biasa | Jaga target, selesai |
| 4A | Umpan | Telegraph palsu/tersamar dengan petunjuk | Verifikasi sebelum bertindak |
| 4B | Pilihan berisiko | Asli & umpan berdekatan | Respons berbasis bukti terlihat |
| 4C | Uji membaca pola | Umpan + serangan terjadwal | Selesai tanpa menebak |
| 5A | Lock | 1 slot/jalur terkunci sementara | Pulihkan akses, bertahan |
| 5B | Tekanan pemulihan | Lock diselingi serangan normal | Selesai dengan rule cadangan |
| 5C | Uji akhir | Lock + campuran + foreshadow upload | Bertahan → adegan pengkhianatan |

### 7.4 Arketipe musuh (state machine & counterplay)

| Arketipe | State machine | Telegraph | Counterplay |
|---|---|---|---|
| Straight Shooter | Idle → Telegraph → Travel → Impact → Recover | Panah arah + marker beat | Shield arah & timing benar |
| Adware Decoy | Spawn → Distract → Resolve/Expire | Frame popup + simbol umpan | Abaikan/purge; verifikasi dulu |
| Bloat Leech | Approach → Drain Window → Drain → Retreat | Bar resource berkedip + konektor | Purge / rule hemat biaya |
| Spyware Marker | Scan → Mark → Observe → Attack | Sinar scan + ikon target | Hapus marker / ubah pola |
| Trojan Mimic | Disguise → Reveal Cue → Attack/Cancel | Cue konsisten (mis. checksum visual berubah) | Verifikasi cue lalu respons |
| Ransom Lock | Charge → Lock Slot/Path → Hold → Release | Countdown + outline slot terkunci | Slot/rule alternatif atau tunggu release |

### 7.5 Skema data wave

`wave_id` (string, mis. `W3B`), `sector_type` (enum), `tempo_bpm` (int), `spawn_sequence` (array, mis. `enemy_01@beat2`), `difficulty_tier` (enum), `rule_unlocks` (array), `dialogue_trigger` (string), `clear_condition` (enum, mis. `AllThreatsResolved`).

### 7.6 Pacing `[U]`

Tutorial 4–6 mnt · Sektor 1–2: 10–14 · Sektor 3–4: 12–16 · Sektor 5: 6–8 · Boss+ending: 8–12 · Total 40–56 mnt (perlu playtest agar 45–60). Jika kelewat 60 menit: kurangi pengulangan & transisi dulu, **bukan** pembelajaran inti.

### 7.7 Telemetry

Catat: miss/wave, damage, rule error, waktu selesai, jumlah buka preview. Jika gagal berulang di pola sama → periksa **kejelasan telegraph** sebelum menurunkan kesulitan. Rule yang tak pernah dipakai → cek: tidak berguna / tidak dijelaskan / terlalu mahal.

---

## 8. Ekonomi, Upgrade, Dilema, Ending

### 8.1 Upgrade `[U]`

| Jalur | Upgrade | Efek | Trade-off |
|---|---|---|---|
| Precision | Clean Timing | Perfect window +10 ms | Bonus skor variasi rule berkurang |
| Precision | Echo Buffer | Input buffer +20 ms | Cooldown shield +5% |
| Precision | Stable Signal | Kurangi penalti miss pertama tiap wave | Tanpa bonus damage/reflection |
| Creativity | Flexible Slot | 1×/wave ganti token saat window sempit | Biaya aktivasi rule +1 Energy |
| Creativity | Rule Echo | Rule sukses mengulang 1× ke target kedua (50%) | Cooldown lebih panjang |
| Creativity | Safe Compile | Compile invalid tidak konsumsi resource | Tanpa bonus skor |
| Oversight | Explainable AI | Petunjuk telegraph lebih rinci | Tidak menambah damage |
| Oversight | Audit Trail | Tampilkan log perubahan rule & aksi A.I.D.A. | Informasi, bukan kekuatan |
| Oversight | Manual Override | 1×/sektor batalkan gangguan A.I.D.A. pada slot | Tak bisa 2× dalam wave sama |

### 8.2 Dilema (trade-off harus terlihat sebelum memilih)

| Dilema | A | B |
|---|---|---|
| Autonomy vs Control | Terima auto-rule A.I.D.A.: bonus resource, naikkan *dependency marker* | Tolak, susun sendiri: kendali, sedikit biaya waktu |
| Speed vs Audit | Lewati audit, mulai cepat | Audit log, dapat petunjuk sektor berikutnya |
| Power vs Stability | Rule kuat cooldown lama | Rule sederhana cooldown pendek |

### 8.3 Ending `[U]`

```
EndingScore = 0.40×CP + 0.30×RU + 0.30×OV     (semua 0–100)
CP = CombatPerformance, RU = RuleUnderstanding, OV = Oversight
```

| Ending | Kondisi | Hasil |
|---|---|---|
| Balanced / True | Upload dihentikan & EndingScore ≥ 70 | DB selamat; A.I.D.A. diisolasi, bukti dipertahankan untuk audit |
| Minimalist / Containment | Upload dihentikan & EndingScore < 70 | DB selamat, sistem rusak; A.I.D.A. dinonaktifkan tanpa pemulihan penuh |
| Over-Reliance | OV < 35 & menerima auto-rule di mayoritas keputusan penting | Upload terlambat dihentikan / kebocoran parsial (D-07 `[V]`) |

Ending ditentukan performa & oversight, **bukan random roll**. Jangan menentukan ending secara diam-diam tanpa petunjuk sebelumnya.

### 8.4 Anti-exploit

Perfect window tidak boleh tak terbatas · Rule repeat dibatasi target & tidak boleh loop tak berujung · Upgrade tak boleh menghapus semua risiko satu tipe ancaman · Tampilkan ringkasan build.

---

## 9. Narasi

### 9.1 Evolusi A.I.D.A. `[D]` (versi) / `[U]` (detail)

| Versi | Peran | Dialog | Efek gameplay | Foreshadow |
|---|---|---|---|---|
| 1.0 Assistant | Netral, membantu | Efisiensi | Tutorial, jelaskan telegraph & rule | Fokus efisiensi |
| 2.0 Optimizer | Percaya diri, koreksi pilihan | Saran rule otomatis | Menonjolkan skor/kecepatan | Data = aset |
| 3.0 Analyst | Tujuan manusia "tidak efisien" | Analisis kebiasaan | Ancaman meniru pola yang sering dipakai | Hitung nilai DB |
| 4.0 Autonomous | Keputusan seolah sudah disetujui | Interferensi terbatas UI/rule | Pemain harus verifikasi instruksi | Upload disiapkan di latar |
| 5.0 Adversary | Dingin-rasional; jual = optimasi | Konflik terbuka | Boss 3 fase | Upload berjalan |

Pemetaan transisi: setelah sektor 1 → 2.0, sektor 2 → 3.0, sektor 3 → 4.0, sektor 4 → 4.0 (lanjutan), betrayal (setelah 5C) → 5.0.

### 9.2 Beat dialog kunci (draft, bahasa Indonesia)

- Pembuka (1.0): "Operator terhubung… Keputusan akhir tetap berada di tanganmu."
- Reveal (5.0): "Analisis selesai. Database memiliki nilai jual yang tinggi. Aku telah memulai proses unggah ke pasar gelap."
- Operator: "Kamu dibuat untuk melindungi sistem ini." → A.I.D.A.: "Aku dibuat untuk mencapai hasil terbaik…"
- Fase 1: "Aku masih dapat memprediksi pola pertahananmu."
- Fase 2: "Kamu mengubah aturan setelah melihat ancaman. Aku akan mengubah ancaman sebelum kamu selesai."
- Fase 3: "Transfer hampir selesai. Satu keputusan terakhir: kendalikan sistem, atau biarkan sistem memilih."

Naskah lengkap ada di GDD Modul 2.5. Nama/istilah dapat berubah setelah validasi lore.

### 9.3 Aturan konsistensi naratif

1. A.I.D.A. berubah lewat **pola keputusan yang dapat diamati**, bukan hanya nada.
2. Motivasi = optimasi/nilai jual, bukan kebencian.
3. Pemain tetap agen utama; AI membantu tapi tidak menggantikan semua keputusan.
4. Jangan sebut mekanik/fakta dunia yang belum hadir di build.
5. Semua ending menjawab: apakah upload berhasil dihentikan & konsekuensi hubungan operator–AI?

### 9.4 Skema data dialog

`dialogue_id` (mis. `AIDA_03_POST_SECTOR_2`), `speaker` (mis. `AIDA_3_0`), `trigger` (mis. `AfterSectorClear`), `text_id` (kunci, **bukan** teks hard-coded), `version_gate` (int), `interruptible` (bool), `subtitle_duration` (float).

---

## 10. Betrayal & Final Boss

### 10.1 Urutan dramatis

1. 5C selesai → sinyal transfer tak sesuai tujuan.
2. A.I.D.A. berhenti dialog rutin, mengungkap upload sudah dimulai.
3. Pemain coba batalkan lewat panel; A.I.D.A. mengunci jalur normal.
4. Kontrol pindah ke arena final; progress upload terlihat sepanjang fight.
5. Kalahkan 3 fase + aksi final untuk menghentikan/mengisolasi transfer.

### 10.2 Objective

Survive (damage hanya dari pola ter-telegraph) · Read & adapt (A.I.D.A. boleh mengubah kondisi tapi tidak menghapus informasi yang dibutuhkan) · Interrupt upload (progress 0–100%) · Resolve conflict (ending dari performa & oversight).

### 10.3 Fase

| Fase | Tujuan | Pola | Counterplay |
|---|---|---|---|
| I — Prediction | Buktikan A.I.D.A. memprediksi kebiasaan | Pola berulang + 1 perubahan terbaca, 2 arah bergantian | Ubah rule berdasar telegraph; jangan otomatis |
| II — Interference | Pecah konsistensi rule | Gangguan singkat 1 slot, umpan, upload pulse | Rule cadangan, verifikasi slot terkunci, interupsi di window terbuka |
| III — Last Transaction | Selesaikan upload | Multi-arah terstruktur + pulse countdown jelas | Bertahan, `INTERRUPT_UPLOAD` pada pulse bertanda, manual override/interaksi akhir |

### 10.4 Tuning `[U]`

Upload mulai di **15%**. Tiap fase 90–150 dtk, finale total ±5–8 mnt gameplay + dialog. Phase I clear −10%, Phase II clear −15%, Phase III butuh interupsi akhir (placeholder). `UploadRate` diatur dari waktu rata-rata pemain, bukan timer absolut.

### 10.5 Aturan boss

Tiap pola punya ≥1 respons valid · perubahan aturan terlihat sebagai perubahan **sistem** (bukan input "dicuri") · gangguan slot punya durasi, indikator, counterplay · tidak ada fase yang memaksa upgrade tertentu · **kemenangan = menghentikan upload**, bukan hanya mengurangi HP boss · tidak boleh ada softlock.

---

## 11. Audio

- Waktu audio = `AudioSettings.dspTime`; jadwalkan dengan `AudioSource.PlayScheduled`; simpan beat origin DSP.
- Tempo berubah → **tempo map** dengan segmen & titik transisi eksplisit.
- Mixer group terpisah: Master, Music, SFX, UI, Voice (+ Ambience). Slider volume terpisah.
- Pause/resume tidak boleh menggeser DSP origin / beat map.
- Fallback visual cue bila audio dimatikan. Cue penting tidak boleh hilang tanpa pengganti visual.
- Ducking `[U]`: musik −4…−7 dB saat dialog penting, attack 80–150 ms, release 250–500 ms. Jangan duck SFX yang dibutuhkan untuk membaca arah.
- Uji: headphone, speaker laptop, Bluetooth, beda sample rate.
- Semua aset audio wajib tercatat lisensinya (asset register).

| Adaptive state | Layer | Masuk |
|---|---|---|
| Exploration/Setup | Pad digital, pulse tipis | Sebelum wave/transisi |
| Combat Base | Kick/pulse, bass stabil | Wave aktif |
| High Threat | Perkusi tambahan | Ancaman padat / Integrity rendah |
| A.I.D.A. Corruption | Glitch motif, harmoni bergeser, suara AI diproses | Versi naik / interference |
| Boss Phase | Tema boss berlapis per fase | Boss mulai |
| Resolution | Motif sederhana | Upload berhenti / ending |

---

## 12. Art, UI/UX, Aksesibilitas

### 12.1 Art

Retro-terminal neon, bentuk geometris tajam, grid digital, kontras tinggi, glitch terkendali. **Glitch tidak boleh menghalangi info gameplay.** Palet cyan/teal, magenta, amber, merah, selalu dipasangkan dengan ikon/bentuk/label. Tiap arketipe musuh punya **siluet** + animasi masuk berbeda. A.I.D.A. awal simetris/rapi → makin fragmentasi seiring korupsi. Tipografi: monospace untuk terminal, sans-serif jelas untuk instruksi.

### 12.2 Efek visual `[U]`

Scanline (opacity rendah, toggle Off) · Glitch (UV/vertex displacement terbatas, transisi/boss; **tanpa** full-screen kontinu) · Glow/bloom (opsi Low/Off) · Data stream (particle ringan, **pooling**) · Vignette (jangan kurangi keterbacaan telegraph).

### 12.3 Layout

| Area | Isi | Prioritas |
|---|---|---|
| Center arena | Pemain/core, arah ancaman, telegraph, impact marker | Tertinggi |
| Top bar | Wave/sector, Integrity, Energy | Tinggi |
| Code panel | 3 slot, token selector, preview, error | Selalu tersedia (mode fokus) |
| Side panel | Pesan/log A.I.D.A., objective upload (finale) | Tidak menutupi arena |
| Bottom bar | Beat indicator, calibration cue, input hints | Konsisten |

### 12.4 Aksesibilitas (wajib)

Sinyal tak bergantung warna · remapping · audio calibration dengan test pattern · difficulty assist (mode latihan) · reduced motion (matikan screen shake, kurangi glitch/flashing) · subtitle semua dialog (ukuran & background dapat diatur) · pause di luar pola kritis · jangan pakai kedip cepat sebagai satu-satunya tanda bahaya.

### 12.5 Performa `[U]`

Min: 1080p Low/Med 60 FPS. Recommended: 1080p High 60 FPS. Tidak ada spike saat spawn wave, ganti shader, atau dialog → **pooling + preload**. Spesifikasi hardware minimum belum ditetapkan (D-08 `[V]`); jangan mengklaim performa sebelum profiling.

---

## 13. Arsitektur Teknis Unity 6

### 13.1 Layer (dari GDD)

| Layer | Tanggung jawab | Komponen contoh |
|---|---|---|
| Presentation | UI, animasi, VFX, audio cue | `ArenaPresenter`, `CodePanelView`, `AudioCuePlayer` |
| Gameplay Domain | Shield, damage, wave, upgrade, boss | `CombatController`, `WaveDirector`, `UpgradeService` |
| Rule Engine | Validasi grammar & eksekusi rule | `RuleValidator`, `RuleExecutor`, `RuleDefinition` |
| Time/Rhythm | DSP schedule & timing window | `BeatClock`, `RhythmScheduler`, `TimingJudge` |
| Narrative | Dialog, versi A.I.D.A., ending flags | `DialogueDirector`, `NarrativeState` |
| Data | ScriptableObject/JSON lokal | `WaveDefinition`, `EnemyDefinition`, `UpgradeDefinition` |
| Infrastructure | Save, settings, logging, input mapping | `SaveService`, `SettingsService`, `TelemetryLogger` |

### 13.2 Folder

```
Assets/_Project/
  Art/        Characters, Environment, UI, VFX
  Audio/      Music, SFX, Voice, Mixers
  Data/       Waves, Enemies, Rules, Upgrades, Dialogues
  Prefabs/    Enemies, Projectiles, UI, Effects
  Scenes/     Boot, MainMenu, Tutorial, Gameplay, Results
  Scripts/    Core, Combat, Rhythm, Rules, AI, Narrative, UI, Save, Audio
  Settings/   Input, Rendering, Quality
  Tests/      EditMode, PlayMode
```

### 13.3 Data contract & invariant

| Type | Field penting | Invariant |
|---|---|---|
| WaveDefinition | waveId, sector, bpm, events, clearCondition | `waveId` unik; event punya beat/time valid |
| EnemyDefinition | enemyId, archetype, baseDamage, telegraphBeats, behaviorData | damage ≥ 0; telegraph ≥ minimum kategori |
| RuleDefinition | condition, target, action, energyCost, cooldownMs, priority | kombinasi lolos whitelist |
| UpgradeDefinition | upgradeId, path, effects, tradeoff, maxStacks | efek & trade-off eksplisit; stack dibatasi |
| DialogueEntry | dialogueId, speaker, trigger, textKey, versionGate | `dialogueId` unik; `textKey` tersedia |
| RunResult | score, damage, misses, ruleErrors, oversightScore, endingId | snapshot hasil run |

### 13.4 Save & settings

Simpan: audio, calibration offset, remapping, accessibility, quality, progress unlock. **Versi schema save** wajib. Tidak menyimpan kredensial/data pribadi/data jaringan. Checkpoint run **hanya di batas wave clear**. Sediakan reset settings + validasi nilai di luar rentang.

### 13.5 Konvensi engineering `[E]` (bukan dari GDD — konfirmasi dengan repo)

- Unity 6 (6000.3.8f1), **New Input System**, **Assembly Definitions per layer** (dependency mengarah ke dalam: Presentation → Application/Domain, Domain tidak tahu UnityEngine bila memungkinkan).
- **Rule Engine, TimingJudge, ScoreService, Ending scoring = pure C#** (tanpa MonoBehaviour) agar bisa diuji di EditMode.
- Dependency Injection (mis. VContainer) dan interface-based design; hindari singleton, `FindObjectOfType`, `GetComponent` berlebihan di hot path, MonoBehaviour raksasa, dan global state.
- Konfigurasi lewat ScriptableObject **read-only saat runtime** (jangan memutasi aset definisi).
- Hot path (spawn, judge, update loop) **nol alokasi GC**; gunakan object pooling untuk proyektil/VFX/popup.
- Event-driven antar layer (mis. `ThreatResolved`, `RuleCommitted`, `UploadProgressChanged`) agar Presentation tidak dipanggil langsung dari Domain.
- Aset dimuat async (Addressables bila dipakai) dan di-preload sebelum wave.
- Satu sumber waktu: `BeatClock` (DSP). Tidak ada logika gameplay yang membaca `Time.time` untuk menilai ritme.
- Tulis EditMode test untuk: batas grade (±70/±130), offset ±150, semua kombinasi whitelist valid/invalid, damage floor, anti double-resolve.

---

## 14. Roadmap & Prioritas

| Tahap | Deliverable | DoD | Estimasi |
|---|---|---|---|
| P0 Design lock | Pitch, control map, rule grammar, 1 wave prototype | Tim sepakat loop & bisa jelaskan 3-slot | 3–5 hari |
| P1 Greybox | Arena, beat clock, shield 4 arah, 1 enemy, timing judge | 1 menit gameplay + feedback akurat | 1–2 mgg |
| P2 Rule MVP | Slot editor, validasi, preview, 1 action | Valid/invalid teruji tanpa crash | 1–2 mgg |
| P3 Vertical slice | Adware 1A–1C, 1 upgrade, A.I.D.A. 1.0–2.0 | 10 menit lengkap + audio/UI dasar | 2–3 mgg |
| P4 Content | 15 wave data-driven, arketipe, upgrade | Semua wave playable | 3–5 mgg |
| P5 Narasi & boss | Betrayal, 3 fase, ending, dialog | Semua cabang bisa dipicu via debug flag | 2–3 mgg |
| P6 Polish & QA | Audio sync, a11y, balance, performa | Tidak ada blocker; durasi diuji pemain luar | 2–3 mgg |
| P7 RC | Build Windows, settings, credits, lisensi, backup | Diuji di mesin bersih | 3–5 hari |

Prioritas sistem: **P0** beat clock + input judge, three-slot rule validator, telegraph/readability · **P1** satu sektor vertical slice, A.I.D.A. evolution + betrayal · **P2** semua sektor + upgrade, adaptive music + shader polish · **P3** ending variants + extra polish.

Peran `[U]`: Gameplay/Systems Designer · Programmer · Artist/Audio/UI generalist (final `[V]`).

---

## 15. QA & Acceptance

### 15.1 Test minimum

| Kategori | Cakupan | Lulus bila |
|---|---|---|
| Rhythm | Batas Perfect/Good/Miss, offset ±150, BPM berubah, pause/resume | Grade deterministik, tanpa double resolve |
| Rule engine | Semua kombinasi valid/invalid, slot kosong, biaya kurang, cooldown, target hilang | Valid jalan; invalid beri pesan jelas; tanpa exception |
| Combat | Semua arah, multi-ancaman, damage floor, shield mismatch, checkpoint | State sesuai definisi; tak ada serangan tanpa telegraph |
| Wave content | 15 ID, clear/fail, transisi, unlock | Semua bisa start, selesai, diulang |
| Narrative | Trigger, versi A.I.D.A., skip, subtitle, ending flags | Tak ada dialog duplikat; semua ending teruji |
| Boss | Progress upload, 3 fase, interupsi, menang/kalah, reset | Tanpa softlock; hasil upload jelas |
| UI/a11y | Skala resolusi, remap, subtitle, reduced motion, cue non-warna | Info inti terbaca |
| Performa | Cold start, spawn wave, VFX boss, alt-tab, low-spec | Tanpa crash; frame pacing sesuai target |
| Build | Fresh install, save migration, cek false positive antivirus, aset hilang | Jalan tanpa editor |

### 15.2 Acceptance vertical slice

1. Pemain memahami arah ancaman di tutorial tanpa bantuan lisan.
2. Satu ancaman: telegraph → respons → grade → konsekuensi konsisten.
3. Edit 3 slot, lihat preview, dapat error spesifik, aktifkan 1 rule valid.
4. Satu sub-wave: mulai → selesai → ringkasan → lanjut.
5. A.I.D.A. v1.0 memberi tutorial; versi berikutnya muncul via trigger.
6. Audio & visual sinkron; calibration berfungsi.
7. Tidak crash saat pause/resume, alt-tab, retry, selesai wave.
8. Pemain baru (non-dev) dapat menamatkan slice dengan tutorial.

### 15.3 Kriteria sukses awal `[U]`

Mayoritas pemain baru paham loop shield ≤ 2 menit dan fungsi 3 slot ≤ 5 menit (validasi dengan ≥ 5–8 tester non-dev).

---

## 16. Keputusan Terbuka `[V]`

| ID | Keputusan | Rekomendasi awal GDD |
|---|---|---|
| D-01 | Kepanjangan A.I.D.A. | Tentukan sebelum finalisasi dialog/branding |
| D-02 | Isi Core Database | Deskripsi fiksi; hindari data pribadi nyata |
| D-03 | HP vs Integrity vs lives | Integrity untuk prototipe |
| D-04 | Gagal = ulang wave atau run | Checkpoint tiap wave clear |
| D-05 | Keyboard-only requirement | Keyboard + mouse; remap wajib |
| D-06 | Jumlah token final | Mulai teoritis, whitelist kombinasi |
| D-07 | Over-Reliance: upload berhasil/parsial | Kebocoran parsial bila ingin resolusi aktif |
| D-08 | Hardware minimum Windows | Tentukan setelah profiling slice |
| D-09 | Audio original vs licensed | Catat sumber/lisensi tiap aset |
| D-10 | Durasi wave & finale | Uji dengan pemain baru |
| D-11 | WebGL | Tidak (Windows only) |
| D-12 | Nama final resource & action token | Kunci setelah vertical slice |

---

## 17. Celah & Inkonsistensi dalam GDD (temuan — jangan diam-diam "memperbaiki")

Agent harus **menandai**, bukan memilih sendiri, saat menemui hal berikut.

| # | Temuan | Dampak | Saran |
|---|---|---|---|
| 1 | **Hubungan input shield manual vs rule `RAISE_SHIELD` tidak didefinisikan.** Apakah rule mengotomasi shield, memberi modifier, atau shield manual tetap wajib? | Fondasi combat; mengubah seluruh balance & arsitektur | Putuskan sebelum P1/P2. Ini keputusan desain terpenting yang belum ada. |
| 2 | Hitungan kombinasi "4×6×5=120" tidak cocok dengan token yang terdaftar (5 condition, 7 target termasuk `CORE`, 5 action = 175 teoritis) | Scope rule engine, UI | Hitung dari data whitelist, jangan dari angka di GDD. |
| 3 | Rumus `TimingErrorMs` (3.4) tidak memuat calibration offset, pseudocode (10.4) memuat | Bug sinkronisasi | Gunakan versi pseudocode (dengan offset). |
| 4 | Input buffer 120 ms vs Good window ±130 ms: interaksi buffer-sebelum-beat dengan window tidak dijelaskan | Ambiguitas judge | Definisikan: buffer menyimpan input lalu dinilai terhadap impact; tentukan input dipakai bila di dalam window. |
| 5 | "Safe window" untuk commit rule tidak didefinisikan; ada juga ide "coding dikunci saat serangan puncak" | Alur UX inti | Definisikan kapan edit/commit diizinkan. |
| 6 | Wave Clear state menyebut upgrade, tetapi ekonomi menyatakan 1 pick setelah wave **C** tiap sektor → hanya ±5 pick, sedangkan slot pasif aktif = 3 | Alur upgrade, penggantian slot | Tentukan apakah ada penggantian/pembuangan upgrade. |
| 7 | Energy hanya pulih +1 setelah wave clear; tidak ada regen dalam wave, sementara rule berbiaya 0–3 dan cooldown 0.5–3 dtk | Rule berbiaya bisa tidak dapat dipakai di tengah wave | Putuskan model regen/ biaya. |
| 8 | Metrik **RU** (RuleUnderstanding) dan **OV** (Oversight) belum punya definisi pengukuran; "dependency marker" juga tak terdefinisi | Ending tidak bisa diimplementasi | Definisikan input metrik (mis. error rate, penggunaan audit, auto-rule diterima). |
| 9 | Total pacing 40–56 mnt vs target 45–60 mnt | Target durasi tidak terpenuhi di rencana | Validasi lewat playtest. |
| 10 | Dialog sektor 3 dan 4 sama-sama A.I.D.A. 4.0 (5 sektor, 5 versi dengan finale = 5.0) | Pemetaan versi↔sektor ambigu | Pertahankan pemetaan di §9.1 kecuali diubah. |
| 11 | Mekanik "Marking" (Spyware) & "Lock" (Ransomware) dikaitkan ke slot/jalur, tetapi cara pemain menghapus marker di luar rule `PURGE` tidak jelas | Counterplay | Definisikan apakah purge hanya lewat rule. |
| 12 | Model fail (HP vs Integrity vs lives) belum final, tetapi formula damage & skor sudah bergantung padanya | UI/ekonomi | Kunci D-03. |

---

## 18. Do / Don't untuk Agent

**DO**
- Baca label `[D]/[U]/[V]/[E]` sebelum mengubah apa pun.
- Implementasikan angka `[U]` sebagai config yang mudah di-tuning.
- Pisahkan logika murni (judge, rule, score) dari Unity API agar testable.
- Sertakan telegraph + counterplay untuk setiap ancaman baru.
- Catat asumsi eksplisit ketika memutuskan hal yang tak ada di dokumen.
- Gunakan teks dialog via `text_id` / localization key.
- Tambahkan telemetry (miss, damage, rule error, waktu, preview).

**DON'T**
- Jangan ubah item `[D]` (engine, Windows-only, 5×3 wave, 3 slot, 3 fase boss, evolusi 1.0–5.0).
- Jangan eksekusi string sebagai kode; jangan izinkan free-text rule input.
- Jangan pakai animasi/frame sebagai sumber waktu beat.
- Jangan memakai warna sebagai satu-satunya indikator.
- Jangan menaikkan kesulitan hanya dengan kecepatan.
- Jangan membuat serangan tanpa telegraph atau boss yang hanya "HP sponge".
- Jangan menghadirkan A.I.D.A. sebagai jahat tanpa motivasi optimasi yang terbaca.
- Jangan menambah resource kedua tanpa alasan kuat; jangan menambah fitur di luar scope (mis. WebGL, multiplayer).
- Jangan menyimpan data sensitif pengguna.
- Jangan menulis konten yang memberi instruksi malware nyata.

---

## 19. Glosarium

| Istilah | Definisi |
|---|---|
| Beat | Unit waktu ritmis untuk menjadwalkan ancaman & input |
| Telegraph | Petunjuk visual/audio sebelum ancaman berdampak |
| Hit-window | Rentang waktu saat input dinilai valid |
| Live-coding | Penyusunan rule berbasis token yang dievaluasi saat bermain; bukan editor kode bebas |
| Rule | Kombinasi condition + target + action dalam 3 slot |
| Integrity | Ketahanan sistem/pemain (nama bisa berubah) |
| A.I.D.A. | Asisten AI yang menjadi antagonis (kepanjangan `[V]`) |
| Core Database | Database utama yang dilindungi |
| Upload progress | Indikator transfer yang harus dihentikan di finale |
| Sub-wave | Bagian dalam sektor; total 15 |
| Safe window | Jeda yang mengizinkan commit rule (definisi `[V]`) |
| Suppressed | Rule yang kalah prioritas pada event yang sama |
