using AlgoRhythm.Game.Boss;
using NUnit.Framework;
using UnityEngine;

namespace AlgoRhythm.Tests
{
    public sealed class BossExplosionVfxTests
    {
        [Test]
        public void BossExplosionVfx_SpawnsFlashCoreAndHalo_WithCorrectRenderers()
        {
            var go = new GameObject("TestExplosionHost");
            try
            {
                var vfx = go.AddComponent<BossExplosionVfx>();
                vfx.Play(new Color(0f, 1f, 0.8f), 8, 10f);

                // Verify Flash Core exists
                Transform flashCore = go.transform.Find("ExplosionFlashCore");
                Assert.IsNotNull(flashCore, "ExplosionFlashCore child must be created.");
                var coreRend = flashCore.GetComponent<Renderer>();
                Assert.IsNotNull(coreRend, "ExplosionFlashCore must have a Renderer.");
                Assert.Greater(coreRend.sharedMaterial.color.a, 0.8f);
                Assert.Greater(coreRend.sharedMaterial.color.r, 0.8f);

                // Verify Flash Halo exists
                Transform flashHalo = go.transform.Find("ExplosionFlashHalo");
                Assert.IsNotNull(flashHalo, "ExplosionFlashHalo child must be created.");
                var haloRend = flashHalo.GetComponent<Renderer>();
                Assert.IsNotNull(haloRend, "ExplosionFlashHalo must have a Renderer.");
                Assert.Greater(haloRend.sharedMaterial.color.a, 0.5f);

                // Verify Shockwave Ring exists
                Transform ring = go.transform.Find("ShockwaveRing");
                Assert.IsNotNull(ring, "ShockwaveRing child must be created.");

                // Verify Shards exist
                Transform shard0 = go.transform.Find("Shard_0");
                Assert.IsNotNull(shard0, "Debris shard must be created.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
