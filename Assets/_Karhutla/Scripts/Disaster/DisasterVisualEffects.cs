using System;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Pengelola Efek Visual Bencana, Cuaca, Asap & Suhu (GDD Bab 8.3 & 8.4).
    ///
    /// Menghadirkan:
    ///  1. Partikel Abu & Bara Api (Wind-Drifting Ash & Embers) yang beterbangan tertiup angin di sekitar pemain.
    ///  2. Partikel Kepulan Asap Tebal (Volumetric Ground Smoke Puff) yang membubung dari arah kebakaran hutan.
    ///  3. Fullscreen Screen Hazard Post-FX:
    ///     - Smoke Vignette: Menggelapkan tepi layar secara organik saat kabut asap pekat.
    ///     - Heat Hazard Vignette: Pijar oranye-merah membara saat dekat dengan kobaran api / suhu tinggi.
    ///     - Choking Pulse: Spasme visual batuk/sesak saat paparan asap tinggi tanpa masker.
    /// </summary>
    public class DisasterVisualEffects : MonoBehaviour
    {
        public static DisasterVisualEffects Instance { get; private set; }

        [Header("Pengaturan Partikel")]
        [Tooltip("Maksimum partikel abu/bara per detik saat asap paling pekat.")]
        public float maxAshEmissionRate = 120f;
        [Tooltip("Maksimum partikel asap tebal per detik.")]
        public float maxSmokeEmissionRate = 40f;

        [Header("Pengaturan Layar (Vignette)")]
        [Tooltip("Kepekatan maksimum vignette asap di tepi layar (0..1).")]
        [Range(0f, 1f)] public float maxSmokeVignetteAlpha = 0.82f;
        [Tooltip("Jarak dari garis api (meter) di mana peringatan panas mulai berpijar.")]
        public float heatWarningDistance = 20f;

        private ParticleSystem ashParticleSystem;
        private ParticleSystem smokeParticleSystem;
        private ParticleSystem.EmissionModule ashEmission;
        private ParticleSystem.EmissionModule smokeEmission;

        private GameObject overlayRoot;
        private Image smokeVignetteImage;
        private Image heatVignetteImage;
        private Image chokingFlashImage;

        private float coughTimer = 4f;
        private float chokingIntensity = 0f;
        private float heatPulseTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetupParticles();
            SetupScreenOverlay();
        }

        private void OnDestroy()
        {
            if (overlayRoot != null) Destroy(overlayRoot);
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            // Posisikan sistem partikel mengikuti pemain
            transform.position = player.transform.position;

            float smokeAtPlayer = 0f;
            float distToFire = 999f;
            bool fireActive = false;

            var disaster = DisasterSystem.Instance;
            if (disaster != null)
            {
                smokeAtPlayer = disaster.SmokeAtPlayer;
                distToFire = disaster.DistanceToFront;
                fireActive = disaster.FireStarted;
            }

            // Juga perhitungkan ketegangan pra-bencana (Tension01)
            var timeline = DisasterTimeline.Instance;
            float tension = timeline != null ? timeline.Tension01 : 0f;

            float effectiveSmoke = Mathf.Max(smokeAtPlayer, tension * 0.25f);

            // 1. Update Emisi Partikel Abu & Bara Api
            if (ashParticleSystem != null)
            {
                float targetAshRate = fireActive ? Mathf.Lerp(15f, maxAshEmissionRate, smokeAtPlayer) : (tension * 8f);
                ashEmission.rateOverTime = targetAshRate;
            }

            // 2. Update Emisi Partikel Kepulan Asap Tebal
            if (smokeParticleSystem != null)
            {
                float targetSmokeRate = fireActive ? Mathf.Lerp(5f, maxSmokeEmissionRate, smokeAtPlayer) : (tension * 3f);
                smokeEmission.rateOverTime = targetSmokeRate;
            }

            // 3. Update Fullscreen Smoke Vignette
            if (smokeVignetteImage != null)
            {
                float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 1.5f);
                float targetAlpha = effectiveSmoke * maxSmokeVignetteAlpha * pulse;
                Color c = smokeVignetteImage.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.deltaTime * 0.8f);
                smokeVignetteImage.color = c;
            }

            // 4. Update Fullscreen Heat / Temperature Danger Vignette
            if (heatVignetteImage != null)
            {
                float heatFactor = 0f;
                if (fireActive)
                {
                    if (distToFire <= 0f) heatFactor = 1f;
                    else if (distToFire < heatWarningDistance) heatFactor = 1f - (distToFire / heatWarningDistance);
                }

                heatPulseTimer += Time.deltaTime * (2.5f + heatFactor * 3.5f);
                float heatPulse = 0.65f + 0.35f * Mathf.Sin(heatPulseTimer);
                float targetHeatAlpha = heatFactor * 0.75f * heatPulse;

                Color hc = heatVignetteImage.color;
                hc.a = Mathf.MoveTowards(hc.a, targetHeatAlpha, Time.deltaTime * 2.5f);
                heatVignetteImage.color = hc;
            }

            // 5. Update Efek Sesak Napas / Batuk (Choking Spasm)
            UpdateChokingEffect(smokeAtPlayer);
        }

        private void UpdateChokingEffect(float smokeAtPlayer)
        {
            var survival = SurvivalSystem.Instance;
            if (survival == null) return;

            bool inDanger = smokeAtPlayer >= 0.35f && !survival.IsMasked;
            bool criticalExposure = survival.SmokeExposure >= 45f && !survival.IsMasked;

            if (inDanger || criticalExposure)
            {
                coughTimer -= Time.deltaTime;
                if (coughTimer <= 0f)
                {
                    coughTimer = criticalExposure ? UnityEngine.Random.Range(2.5f, 4.5f) : UnityEngine.Random.Range(5f, 8f);
                    chokingIntensity = 1f; // Spasme batuk mendadak

                    if (criticalExposure && UnityEngine.Random.value < 0.4f)
                    {
                        Notifier.Show("<color=#FF8A7A>Kamu terbatuk-batuk hebat karena asap tebal! Kenakan masker atau cari tempat aman.</color>");
                    }
                }
            }

            if (chokingFlashImage != null)
            {
                chokingIntensity = Mathf.MoveTowards(chokingIntensity, 0f, Time.deltaTime * 1.8f);
                Color c = chokingFlashImage.color;
                c.a = chokingIntensity * 0.55f;
                chokingFlashImage.color = c;
            }
        }

        private void SetupParticles()
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

            // Mat abu & bara
            var ashMat = new Material(particleShader);
            ashMat.name = "VFX_AshAndEmbers";
            var ashTex = CreateSoftCircleTexture(32);
            ashMat.mainTexture = ashTex;

            // 1. GameObject Partikel Abu & Bara Api
            var ashGo = new GameObject("VFX_AshAndEmbers");
            ashGo.transform.SetParent(transform, false);
            ashGo.transform.localPosition = new Vector3(0f, 6f, 4f);

            ashParticleSystem = ashGo.AddComponent<ParticleSystem>();
            var ashRenderer = ashGo.GetComponent<ParticleSystemRenderer>();
            ashRenderer.material = ashMat;
            ashRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ashParticleSystem.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;

            // Gradien warna abu gelap dan bara api oranye terang
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.18f, 0.16f), 0.35f),
                    new GradientColorKey(new Color(0.08f, 0.08f, 0.08f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.2f),
                    new GradientAlphaKey(0.8f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(grad);

            var shape = ashParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 8f, 25f);

            ashEmission = ashParticleSystem.emission;
            ashEmission.rateOverTime = 0f;

            var vel = ashParticleSystem.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            // Angin berhembus dari arah hutan utara ke arah selatan desa
            vel.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.5f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);
            vel.z = new ParticleSystem.MinMaxCurve(-3.5f, -1.8f);

            // 2. Partikel Kepulan Asap Tebal
            var smokeMat = new Material(particleShader);
            smokeMat.name = "VFX_GroundSmoke";
            smokeMat.mainTexture = ashTex;

            var smokeGo = new GameObject("VFX_GroundSmoke");
            smokeGo.transform.SetParent(transform, false);
            smokeGo.transform.localPosition = new Vector3(0f, 1f, 8f);

            smokeParticleSystem = smokeGo.AddComponent<ParticleSystem>();
            var smokeRenderer = smokeGo.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.material = smokeMat;
            smokeRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            var sMain = smokeParticleSystem.main;
            sMain.loop = true;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
            sMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            sMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sMain.maxParticles = 250;

            var sGrad = new Gradient();
            sGrad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.45f, 0.38f, 0.32f), 0f),
                    new GradientColorKey(new Color(0.22f, 0.20f, 0.18f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.25f, 0.25f),
                    new GradientAlphaKey(0.20f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            sMain.startColor = new ParticleSystem.MinMaxGradient(sGrad);

            var sShape = smokeParticleSystem.shape;
            sShape.shapeType = ParticleSystemShapeType.Box;
            sShape.scale = new Vector3(35f, 2f, 30f);

            smokeEmission = smokeParticleSystem.emission;
            smokeEmission.rateOverTime = 0f;

            var sVel = smokeParticleSystem.velocityOverLifetime;
            sVel.enabled = true;
            sVel.space = ParticleSystemSimulationSpace.World;
            sVel.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.2f);
            sVel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            sVel.z = new ParticleSystem.MinMaxCurve(-2.2f, -1.0f);
        }

        private void SetupScreenOverlay()
        {
            var canvas = UIFactory.Canvas;
            if (canvas == null) return;

            overlayRoot = new GameObject("HazardScreenPostFxOverlay");
            overlayRoot.transform.SetParent(canvas.transform, false);

            var rootRt = overlayRoot.AddComponent<RectTransform>();
            UIFactory.Stretch(rootRt, 0f, 0f, 0f, 0f);

            var vignetteTex = CreateVignetteTexture(256, 256);

            // Layer 1: Smoke Vignette (Hitam/Abu Gelap Peat Smoke)
            var smokeVo = new GameObject("SmokeVignette");
            smokeVo.transform.SetParent(rootRt, false);
            var smokeRt = smokeVo.AddComponent<RectTransform>();
            UIFactory.Stretch(smokeRt, 0f, 0f, 0f, 0f);
            smokeVignetteImage = smokeVo.AddComponent<Image>();
            smokeVignetteImage.sprite = Sprite.Create(vignetteTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            smokeVignetteImage.color = new Color(0.12f, 0.10f, 0.08f, 0f);
            smokeVignetteImage.raycastTarget = false;

            // Layer 2: Heat Hazard Vignette (Merah Oranye Membara)
            var heatVo = new GameObject("HeatHazardVignette");
            heatVo.transform.SetParent(rootRt, false);
            var heatRt = heatVo.AddComponent<RectTransform>();
            UIFactory.Stretch(heatRt, 0f, 0f, 0f, 0f);
            heatVignetteImage = heatVo.AddComponent<Image>();
            heatVignetteImage.sprite = Sprite.Create(vignetteTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            heatVignetteImage.color = new Color(0.95f, 0.32f, 0.05f, 0f);
            heatVignetteImage.raycastTarget = false;

            // Layer 3: Choking Cough Flash (Gelap / Spasme Sesak Napas)
            var chokeVo = new GameObject("ChokingFlashOverlay");
            chokeVo.transform.SetParent(rootRt, false);
            var chokeRt = chokeVo.AddComponent<RectTransform>();
            UIFactory.Stretch(chokeRt, 0f, 0f, 0f, 0f);
            chokingFlashImage = chokeVo.AddComponent<Image>();
            chokingFlashImage.color = new Color(0.02f, 0.02f, 0.03f, 0f);
            chokingFlashImage.raycastTarget = false;
        }

        private static Texture2D CreateSoftCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float maxR = center;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / maxR);
                    float alpha = Mathf.SmoothStep(1f, 0f, t);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateVignetteTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float maxDist = center.magnitude;

            Color[] colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = dist / maxDist;
                    // Pusat transparan (0), pinggir gelap (1)
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((norm - 0.35f) / 0.65f));
                    colors[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
    }
}
