using UnityEngine;

namespace BrewedInk.CRT
{
	/// <summary>
	/// Shared CPU-side parameter sync for the CRT effect (Unity 6 / URP port).
	/// Mirrors the per-frame sync in <see cref="CRTCameraBehaviour"/> (Built-in RP
	/// path) so both paths stay visually identical. Used by the URP
	/// <c>CRTFeature</c>/<c>CRTPass</c>.
	/// </summary>
	public static class CRTMaterialSync
	{
		public static readonly int PropMaxColorsRed = Shader.PropertyToID("_MaxColorsRed");
		public static readonly int PropMaxColorsGreen = Shader.PropertyToID("_MaxColorsGreen");
		public static readonly int PropMaxColorsBlue = Shader.PropertyToID("_MaxColorsBlue");
		public static readonly int PropDitheringAmount = Shader.PropertyToID("_Spread");
		public static readonly int PropDitheringAmount8 = Shader.PropertyToID("_Spread8");
		public static readonly int PropVignette = Shader.PropertyToID("_VigSize");
		public static readonly int PropMonitorRoundness = Shader.PropertyToID("_BorderOutterRound");
		public static readonly int PropMonitorTexture = Shader.PropertyToID("_BorderTex");
		public static readonly int PropMonitorColor = Shader.PropertyToID("_BorderTint");
		public static readonly int PropInnerDarkness = Shader.PropertyToID("_BorderInnerDarkerAmount");
		public static readonly int PropInnerGlow = Shader.PropertyToID("_CrtGlowAmount");
		public static readonly int PropInnerReflectionRadius = Shader.PropertyToID("_CrtReflectionRadius");
		public static readonly int PropInnerReflectionCurve = Shader.PropertyToID("_CrtReflectionCurve");
		public static readonly int PropMonitorCurve = Shader.PropertyToID("_Curvature2");
		public static readonly int PropInnerCurve = Shader.PropertyToID("_Curvature");
		public static readonly int PropZoom = Shader.PropertyToID("_BorderZoom");
		public static readonly int PropInnerSizeX = Shader.PropertyToID("_BorderInnerSizeX");
		public static readonly int PropInnerSizeY = Shader.PropertyToID("_BorderInnerSizeY");
		public static readonly int PropOutterSizeX = Shader.PropertyToID("_BorderOutterSizeX");
		public static readonly int PropOutterSizeY = Shader.PropertyToID("_BorderOutterSizeY");
		public static readonly int PropColorScans = Shader.PropertyToID("_ColorScans");
		public static readonly int PropDesaturation = Shader.PropertyToID("_Desaturation");
		public static readonly int PropBrewedInkBayer4 = Shader.PropertyToID("_BrewedInk_Bayer4");
		public static readonly int PropBrewedInkBayer8 = Shader.PropertyToID("_BrewedInk_Bayer8");
		public static readonly int PropPixelation = Shader.PropertyToID("_Pixelation");

		private static readonly float[] Bayer4 = new float[]
		{
			0, 8, 2, 10,
			12, 4, 14, 6,
			3, 11, 1, 9,
			15, 7, 13, 5
		};

		private static readonly float[] Bayer8 = new float[]
		{
			0, 32, 8, 40, 2, 34, 10, 42,
			48, 16, 56, 24, 50, 18, 58, 26,
			12, 44, 4, 36, 14, 46, 6, 38,
			60, 28, 52, 20, 62, 30, 54, 22,
			3, 35, 11, 43, 1, 33, 9, 41,
			51, 19, 59, 27, 49, 17, 57, 25,
			15, 47, 7, 39, 13, 45, 5, 37,
			63, 31, 55, 23, 61, 29, 53, 21
		};

		public static void SetGlobalBayerTables()
		{
			Shader.SetGlobalFloatArray(PropBrewedInkBayer4, Bayer4);
			Shader.SetGlobalFloatArray(PropBrewedInkBayer8, Bayer8);
		}

		public static void Apply(Material material, CRTData data)
		{
			if (material == null || data == null)
				return;

			material.SetFloat(PropMaxColorsRed, data.maxColorChannels.red);
			material.SetFloat(PropMaxColorsGreen, data.maxColorChannels.green);
			material.SetFloat(PropMaxColorsBlue, data.maxColorChannels.blue);
			material.SetFloat(PropDitheringAmount, data.dithering4);
			material.SetFloat(PropDitheringAmount8, data.dithering8);
			material.SetFloat(PropVignette, data.vignette);
			material.SetFloat(PropMonitorRoundness, data.monitorRoundness);
			material.SetFloat(PropInnerDarkness, 1f - data.innerMonitorDarkness);
			material.SetFloat(PropInnerGlow, data.innerMonitorShine);
			material.SetFloat(PropInnerReflectionRadius, data.innerMonitorShineRadius);
			material.SetFloat(PropInnerReflectionCurve, data.innerMonitorShineCurve);
			material.SetFloat(PropMonitorCurve, data.monitorCurve);
			material.SetFloat(PropInnerCurve, data.innerCurve);
			material.SetFloat(PropZoom, data.zoom);
			material.SetFloat(PropInnerSizeX, data.monitorInnerSize.width);
			material.SetFloat(PropInnerSizeY, data.monitorInnerSize.height);
			material.SetFloat(PropDesaturation, data.maxColorChannels.greyScale);
			material.SetFloat(PropOutterSizeX, data.monitorOutterSize.width);
			material.SetFloat(PropOutterSizeY, data.monitorOutterSize.height);
			material.SetVector(PropColorScans, new Vector4
			{
				x = data.colorScans.greenChannelMultiplier,
				y = data.colorScans.redBlueChannelMultiplier,
				z = data.colorScans.sizeMultiplier
			});
			material.SetTexture(PropMonitorTexture, data.monitorTexture);
			material.SetColor(PropMonitorColor, data.monitorColor);

			// URP port: the RenderGraph blit path uses a single in-shader UV
			// quantize step instead of the Built-in two-target downsample.
			// 0/1 keeps the effect at full resolution.
			material.SetFloat(PropPixelation, Mathf.Max(0f, data.pixelationAmount));
		}
	}
}
