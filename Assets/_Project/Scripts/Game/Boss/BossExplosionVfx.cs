using System.Collections;
using UnityEngine;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Procedural cyber explosion VFX:
    /// Spawns bursting debris cubes, expanding shockwave ring, and instant core/halo flash.
    /// Cleans itself up automatically when animation completes.
    /// </summary>
    public sealed class BossExplosionVfx : MonoBehaviour
    {
        public static void Spawn(Vector3 position, Color primaryColor, int particleCount = 16, float burstSpeed = 8f)
        {
            GameObject explosionObj = new GameObject("BossExplosion");
            explosionObj.transform.position = position;
            BossExplosionVfx vfx = explosionObj.AddComponent<BossExplosionVfx>();
            vfx.Play(primaryColor, particleCount, burstSpeed);
        }

        public void Play(Color color, int count, float speed)
        {
            // Trigger UI FlashOverlay for 0.2s at each explosion
            AidaDeathCinematic.TriggerFlash(0.2f, 1.0f);

            StartCoroutine(ExplosionRoutine(color, count, speed));
        }

        private IEnumerator ExplosionRoutine(Color color, int count, float speed)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");

            // 0. Create Explosion Flash Elements
            // 0a. Core Blinding Flash (Intense Pure White, snappy decay)
            GameObject flashCore = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flashCore.name = "ExplosionFlashCore";
            flashCore.transform.SetParent(transform, false);
            flashCore.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            float baseFlashScale = Mathf.Lerp(1.4f, 3.6f, speed / 10f);
            flashCore.transform.localScale = Vector3.one * baseFlashScale;
            var flashCol = flashCore.GetComponent<Collider>();
            if (flashCol != null) DestroySafe(flashCol);

            Material flashCoreMat = new Material(unlit) { color = Color.white };
            var coreRend = flashCore.GetComponent<Renderer>();
            if (coreRend != null) coreRend.sharedMaterial = flashCoreMat;

            // 0b. Outer Color Flare Glow (Expands and rotates slightly)
            GameObject flashHalo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flashHalo.name = "ExplosionFlashHalo";
            flashHalo.transform.SetParent(transform, false);
            flashHalo.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            flashHalo.transform.localScale = Vector3.one * (baseFlashScale * 1.6f);
            var haloCol = flashHalo.GetComponent<Collider>();
            if (haloCol != null) DestroySafe(haloCol);

            Material flashHaloMat = new Material(unlit) { color = new Color(color.r, color.g, color.b, 0.9f) };
            var haloRend = flashHalo.GetComponent<Renderer>();
            if (haloRend != null) haloRend.sharedMaterial = flashHaloMat;

            // 1. Create debris shards
            GameObject[] shards = new GameObject[count];
            Vector3[] velocities = new Vector3[count];
            Vector3[] rotVelocities = new Vector3[count];

            Material shardMat = new Material(unlit) { color = color };

            for (int i = 0; i < count; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Shard_{i}";
                shard.transform.SetParent(transform, false);
                shard.transform.localPosition = Vector3.zero;

                float size = UnityEngine.Random.Range(0.18f, 0.42f);
                shard.transform.localScale = Vector3.one * size;

                var rend = shard.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = shardMat;

                var col = shard.GetComponent<Collider>();
                if (col != null) DestroySafe(col);

                shards[i] = shard;

                float angle = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                float shardSpeed = speed * UnityEngine.Random.Range(0.6f, 1.4f);
                velocities[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * shardSpeed;
                rotVelocities[i] = new Vector3(0f, 0f, UnityEngine.Random.Range(-360f, 360f));
            }

            // 2. Create expanding shockwave ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShockwaveRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);

            var ringCol = ring.GetComponent<Collider>();
            if (ringCol != null) DestroySafe(ringCol);

            Material ringMat = new Material(unlit) { color = new Color(color.r, color.g, color.b, 0.8f) };
            var ringRend = ring.GetComponent<Renderer>();
            if (ringRend != null) ringRend.sharedMaterial = ringMat;

            // 3. Animate over duration
            float duration = 0.5f;
            float elapsed = 0f;
            float flashDuration = 0.16f;
            float haloDuration = 0.24f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Animate Core Flash (very snappy, 0.16s)
                if (flashCore != null)
                {
                    float ft = Mathf.Clamp01(elapsed / flashDuration);
                    float curScale = Mathf.Lerp(baseFlashScale, baseFlashScale * 1.35f, ft);
                    flashCore.transform.localScale = Vector3.one * curScale;

                    Color fc = Color.Lerp(Color.white, color, ft * 0.4f);
                    fc.a = Mathf.Lerp(1f, 0f, ft * ft);
                    flashCoreMat.color = fc;

                    if (ft >= 1f && flashCore.activeSelf)
                    {
                        flashCore.SetActive(false);
                    }
                }

                // Animate Outer Halo (0.24s)
                if (flashHalo != null)
                {
                    float ht = Mathf.Clamp01(elapsed / haloDuration);
                    float curHaloScale = Mathf.Lerp(baseFlashScale * 1.6f, baseFlashScale * 2.2f, ht);
                    flashHalo.transform.localScale = Vector3.one * curHaloScale;
                    flashHalo.transform.Rotate(0f, 0f, 150f * Time.deltaTime);

                    Color hc = color;
                    hc.a = Mathf.Lerp(0.9f, 0f, ht);
                    flashHaloMat.color = hc;

                    if (ht >= 1f && flashHalo.activeSelf)
                    {
                        flashHalo.SetActive(false);
                    }
                }

                // Expand ring
                float ringScale = Mathf.Lerp(0.5f, 5.0f, t);
                ring.transform.localScale = new Vector3(ringScale, 0.01f, ringScale);
                Color rc = ringMat.color;
                rc.a = Mathf.Lerp(0.8f, 0f, t);
                ringMat.color = rc;

                // Move shards
                for (int i = 0; i < count; i++)
                {
                    if (shards[i] != null)
                    {
                        shards[i].transform.localPosition += velocities[i] * Time.deltaTime;
                        shards[i].transform.Rotate(rotVelocities[i] * Time.deltaTime);

                        // Shrink shards
                        float shrink = Mathf.Lerp(1f, 0f, t * t);
                        shards[i].transform.localScale = Vector3.one * (0.28f * shrink);
                    }
                }

                yield return null;
            }

            DestroySafe(gameObject);
        }

        private static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }
    }
}
