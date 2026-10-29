using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BrewedInk.CRT.URP
{
	/// <summary>
	/// URP render pass for the CRT effect (Unity 6, RenderGraph path).
	/// Follows the official Unity 6 fullscreen post-processing template:
	/// the camera color is read as input, the effect is drawn into a
	/// temporary texture, and the camera color handle is swapped so the next
	/// pass consumes the result. Material parameters are synced on the main
	/// thread via <see cref="CRTMaterialSync"/> before the pass is enqueued.
	/// </summary>
	public sealed class CRTPass : ScriptableRenderPass
	{
		private const string PassName = "CRT Effect";

		private static readonly int KBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
		private static readonly int KBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");
		private static readonly MaterialPropertyBlock SharedPropertyBlock = new MaterialPropertyBlock();

		private Material m_Material;
		private CRTDataObject m_Config;
		private CRTData m_RuntimeData;
		private CRTDataObject m_SyncedConfig;
		private string m_SyncedValidationId;

		public CRTPass()
		{
			profilingSampler = new ProfilingSampler(PassName);

			// The pass samples the active color buffer, so URP must render
			// through an intermediate texture instead of the backbuffer.
			requiresIntermediateTexture = true;
		}

		public void Setup(Material material, CRTDataObject config)
		{
			m_Material = material;
			m_Config = config;
		}

		public bool IsActive()
		{
			return m_Material != null
				&& m_Material.shader != null
				&& m_Material.shader.isSupported
				&& m_Config != null;
		}

		/// <summary>
		/// Clone the config on first use (mirrors CRTCameraBehaviour, which
		/// clones to avoid mutating the shared asset at runtime), then push
		/// all parameters plus the bayer tables to the runtime material.
		/// Call this from AddRenderPasses, once per camera per frame.
		/// </summary>
		public void SyncMaterial()
		{
			if (m_Material == null || m_Config == null)
				return;

			if (m_RuntimeData == null
				|| !ReferenceEquals(m_SyncedConfig, m_Config)
				|| m_SyncedValidationId != m_Config.validationId)
			{
				m_RuntimeData = m_Config.data.Clone();
				m_SyncedConfig = m_Config;
				m_SyncedValidationId = m_Config.validationId;
			}

			CRTMaterialSync.SetGlobalBayerTables();
			CRTMaterialSync.Apply(m_Material, m_RuntimeData);
		}

		private class MainPassData
		{
			public Material material;
			public TextureHandle inputTexture;
		}

		private static void ExecuteMainPass(MainPassData data, RasterGraphContext context)
		{
			if (data == null || data.material == null || context.cmd == null)
				return;

			SharedPropertyBlock.Clear();
			if (data.inputTexture.IsValid())
				SharedPropertyBlock.SetTexture(KBlitTexturePropertyId, data.inputTexture);
			SharedPropertyBlock.SetVector(KBlitScaleBiasPropertyId, new Vector4(1f, 1f, 0f, 0f));

			context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0,
				MeshTopology.Triangles, 3, 1, SharedPropertyBlock);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (!IsActive())
				return;

			if (renderGraph == null || frameData == null)
				return;

			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			if (resourceData == null || !resourceData.cameraColor.IsValid())
				return;

			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			if (cameraData != null && (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection))
				return;

			TextureDesc cameraColorDesc;
			try
			{
				cameraColorDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[CRTPass] Skipped frame: GetTextureDesc failed ({ex.Message})");
				return;
			}

			cameraColorDesc.name = "CRT_Effect";
			cameraColorDesc.clearBuffer = false;
			TextureHandle destination = renderGraph.CreateTexture(cameraColorDesc);
			if (!destination.IsValid())
				return;

			using (var builder = renderGraph.AddRasterRenderPass<MainPassData>(
				PassName, out var passData, profilingSampler))
			{
				passData.material = m_Material;
				passData.inputTexture = resourceData.cameraColor;

				builder.UseTexture(passData.inputTexture, AccessFlags.Read);
				builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
				builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
					ExecuteMainPass(data, context));

				resourceData.cameraColor = destination;
			}
		}
	}
}
