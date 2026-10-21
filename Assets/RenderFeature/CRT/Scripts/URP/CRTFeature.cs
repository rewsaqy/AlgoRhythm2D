using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BrewedInk.CRT.URP
{
	/// <summary>
	/// URP RendererFeature wiring for the CRT effect (Unity 6 port).
	/// Reuses the original <see cref="CRTDataObject"/> presets and the
	/// optional <see cref="CRTRenderSettingsObject"/> template, but renders
	/// through <see cref="CRTPass"/> (RenderGraph) instead of
	/// <c>OnRenderImage</c>, which URP never calls.
	///
	/// Wiring (needs explicit owner approval, touches P-003):
	/// add this feature to the active 2D Renderer asset
	/// (Assets/Settings/Renderer2D.asset) and assign a CRT preset.
	/// </summary>
	public sealed class CRTFeature : ScriptableRendererFeature
	{
		[Header("CRT Source (data original, dipakai ulang)")]
		[SerializeField] private CRTDataObject m_StartConfig;
		[SerializeField] private CRTRenderSettingsObject m_RenderSettings;
		[SerializeField] private Shader m_CRTShader;

		[Header("Injection")]
		[SerializeField] private RenderPassEvent m_RenderPassEvent =
			RenderPassEvent.AfterRenderingPostProcessing;

		private Material m_RuntimeMaterial;
		private CRTPass m_Pass;

		/// <inheritdoc/>
		public override void Create()
		{
			Shader shader = m_CRTShader;
			if (shader == null
				&& m_RenderSettings != null
				&& m_RenderSettings.crtMaterial != null)
			{
				shader = m_RenderSettings.crtMaterial.shader;
			}

#if UNITY_EDITOR
			if (shader == null)
			{
				shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
					"Assets/RenderFeature/CRT/Materials/CRT_URP.shader");
			}
#endif

			if (shader == null)
				shader = Shader.Find("Hidden/BrewedInk/CRT_URP");

			if (shader == null)
				return;

			if (m_RuntimeMaterial == null || m_RuntimeMaterial.shader != shader)
			{
				DestroyRuntimeMaterial();
				m_RuntimeMaterial = new Material(shader);
			}

			if (m_Pass == null)
				m_Pass = new CRTPass();
		}

		/// <inheritdoc/>
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (m_RuntimeMaterial == null || m_Pass == null || m_StartConfig == null)
				return;

			if (renderingData.cameraData.cameraType == CameraType.Preview
				|| renderingData.cameraData.cameraType == CameraType.Reflection)
			{
				return;
			}

			m_Pass.renderPassEvent = m_RenderPassEvent;
			m_Pass.Setup(m_RuntimeMaterial, m_StartConfig);
			m_Pass.SyncMaterial();
			renderer.EnqueuePass(m_Pass);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			DestroyRuntimeMaterial();
		}

		private void DestroyRuntimeMaterial()
		{
			if (m_RuntimeMaterial == null)
				return;

#if UNITY_EDITOR
			if (!Application.isPlaying)
				Object.DestroyImmediate(m_RuntimeMaterial);
			else
#endif
				Object.Destroy(m_RuntimeMaterial);

			m_RuntimeMaterial = null;
		}
	}
}
