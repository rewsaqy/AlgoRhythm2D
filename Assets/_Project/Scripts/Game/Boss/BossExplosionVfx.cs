using System.Collections;
using UnityEngine;

namespace AlgoRhythm.Game.Boss
{
    /// <summary>
    /// Procedural cyber explosion VFX:
    /// Spawns bursting debris cubes, expanding shockwave ring, and flash light.
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

        private void Play(Color color, int count, float speed)
        {
            StartCoroutine(ExplosionRoutine(color, count, speed));
        }

        private IEnumerator ExplosionRoutine(Color color, int count, float speed)
        {
            // 1. Create debris shards
            GameObject[] shards = new GameObject[count];
            Vector3[] velocities = new Vector3[count];
            Vector3[] rotVelocities = new Vector3[count];

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material shardMat = new Material(unlit)
            {
                color = color
            };

            for (int i = 0; i < count; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Shard_{i}";
                shard.transform.SetParent(transform, false);
                shard.transform.localPosition = Vector3.zero;

                float size = UnityEngine.Random.Range(0.18f, 0.42f);
                shard.transform.localScale = Vector3.one * size;

                var rend = shard.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = shardMat;
                }

                var col = shard.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

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
            if (ringCol != null) Destroy(ringCol);

            Material ringMat = new Material(unlit)
            {
                color = new Color(color.r, color.g, color.b, 0.8f)
            };
            var ringRend = ring.GetComponent<Renderer>();
            if (ringRend != null) ringRend.sharedMaterial = ringMat;

            // 3. Animate over 0.5 seconds
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

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

            Destroy(gameObject);
        }
    }
}
