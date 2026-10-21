# CRT-Unity — URP Port (Unity 6)

Sumber: https://github.com/cdhanna/CRT-Unity (Built-in RP, Unity 2021.3).
Tujuan: Unity 6000.3.8f1 + URP 17.3 (Renderer2D). Lokasi: `Assets/RenderFeature/CRT`
(mula-mula `Assets/_ThirdParty/CRT`, dipindah via editor dengan GUID utuh).

## Yang disalin mentah (dengan `.meta` asli)

- `Scripts/CRTCameraBehaviour.cs` — hanya 2 perbaikan API Unity 6:
  `DestroyImmediate` → aman edit/play, `RenderTexture.Release()` →
  `ReleaseTemporary`. Plus catatan: `OnRenderImage` tidak dipanggil URP.
- `Scripts/CRTDataObject.cs`, `Scripts/CRTRenderSettingsObject.cs` — tanpa ubahan.
- `Scripts/CRTDemoBehaviour.cs` — `Input.GetKeyDown(Space)` → `Keyboard.current`
  (proyek ini Input System baru saja; input lama nonaktif).
- `Materials/CRTUnlit.shader`, `Materials/CRTMaterial.mat`, `Images/`,
  `CRTs/` (6 preset), `CRTRenderSettings.asset` — tanpa ubahan.
  Catatan: `CRTRenderSettings.asset` upstream memang `crtMaterial: null`.
- `Scenes/` TIDAK disalin (contoh Built-in, tidak relevan untuk URP).

## Yang baru (port URP)

- `BrewedInk.CRT.asmdef` — isolasi assembly; referensi URP Core/Runtime + InputSystem.
- `Materials/CRT_URP.shader` (`Hidden/BrewedInk/CRT_URP`) — port HLSL dari
  `CRTUnlit.shader`: `_MainTex` → `_BlitTexture`, `#include Blit.hlsl`,
  array Bayer `float` (diisi `SetGlobalFloatArray` seperti aslinya).
  Pixelation di-emulasi via quantize UV (`_Pixelation`), bukan 2-target blit.
- `Scripts/URP/CRTMaterialSync.cs` — sinkron parameter CPU (cerminan
  `CRTCameraBehaviour`), dipakai path URP.
- `Scripts/URP/CRTPass.cs` — `ScriptableRenderPass` RenderGraph (template
  resmi Unity 6: baca `cameraColor`, gambar segitiga fullscreen ke texture
  sementara, tukar handle). `requiresIntermediateTexture = true`.
- `Scripts/URP/CRTFeature.cs` — `ScriptableRendererFeature`, clone
  `CRTData` sekali pakai (seperti aslinya, agar aset preset tidak kotor).

## Status wiring (SUDAH di-wire di editor)

1. `Assets/Settings/Renderer2D.asset` → `CRTFeature` aktif (`m_RendererFeatures`),
   `Start Config` + `Render Settings` + `CRT Shader` terisi, event
   `AfterRenderingPostProcessing`.
2. Referensi serial `CRT Shader` ini yang menjaga shader masuk build
   (`Shader.Find` saja tidak cukup di player build).
3. Play — efek muncul setelah post-processing kamera.

Perhatian: wiring menyentuh P-003 (`Settings/Renderer2D.asset`) dan
`SampleScene.unity` (P-007) punya tweak adegan terpisah dari pekerjaan ini —
lihat diff sebelum push.
