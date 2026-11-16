using UnityEngine;
using UnityEngine.Rendering;

namespace Karhutla
{
    /// <summary>
    /// Mengatur cahaya matahari, warna langit, ambient, dan kabut sesuai jam game.
    /// Juga menampilkan efek asap kebakaran: makin tebal asap di sekitar pemain,
    /// makin redup cahaya dan makin pekat kabutnya.
    ///
    /// Waktu tidak berjalan otomatis, jadi "jam visual" dihitung dari PERIODE yang tampil di HUD (Pagi/Siang/Malam)
    /// ditambah kemajuan jatah waktu di periode itu (TimeManager.SectionProgress01), lalu digerakkan halus
    /// agar perpindahan periode tidak terlihat patah.
    ///
    /// PERBAIKAN: sebelumnya Pagi dimulai di jam visual 6.0, tepat saat matahari di horizon, sehingga Hari 1 Pagi
    /// terlihat gelap seperti malam. Sekarang jam visual tiap periode dipilih agar: Pagi terang hangat, Siang paling terang,
    /// Malam gelap. Nilainya ada di field di bawah (nama baru, jadi scene lama otomatis memakai nilai baru).
    /// Sebelum kebakaran, suasana berangsur muram sesuai DisasterTimeline.Tension01 (GDD 9.5 'Transition').
    /// Ini hanya visual: tidak ada api atau asap sebelum Hari 60.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public Light sun;
        public Camera targetCamera;
        public Color smokeColor = new Color(0.45f, 0.36f, 0.30f);

        [Header("Jam visual tiap periode (x = awal, y = akhir; jam 0-24, NILAI SEMENTARA)")]
        [Tooltip("Pagi: matahari sudah cukup tinggi (terang, cahaya hangat).")]
        public Vector2 pagiHours = new Vector2(7.5f, 10.5f);
        [Tooltip("Siang: sekitar tengah hari, paling terang.")]
        public Vector2 siangHours = new Vector2(11.5f, 15.5f);
        [Tooltip("Malam: gelap.")]
        public Vector2 malamHours = new Vector2(20.5f, 23.5f);

        [Header("Transisi langit (NILAI SEMENTARA)")]
        [Tooltip("Kecepatan langit berpindah ke jam visual target, dalam jam visual per detik nyata.")]
        public float hourSmoothSpeed = 4f;

        [Header("Suasana muram sebelum kebakaran (NILAI SEMENTARA, visual saja)")]
        [Tooltip("Seberapa pudar warna langit pada Tension01 = 1 (0 = tidak berubah).")]
        [Range(0f, 1f)] public float tensionMute = 0.35f;
        [Tooltip("Seberapa redup matahari pada Tension01 = 1.")]
        [Range(0f, 1f)] public float tensionDim = 0.2f;
        [Tooltip("Tambahan kepekatan kabut pada Tension01 = 1.")]
        public float tensionFog = 0.004f;

        private static readonly float[] SkyHours = { 0f, 5f, 6.5f, 8f, 12f, 16.5f, 18f, 19.5f, 24f };
        private static readonly Color[] SkyColors =
        {
            new Color(0.02f, 0.03f, 0.08f),
            new Color(0.04f, 0.05f, 0.12f),
            new Color(0.95f, 0.62f, 0.45f),
            new Color(0.55f, 0.75f, 0.95f),
            new Color(0.45f, 0.72f, 1.00f),
            new Color(0.60f, 0.75f, 0.90f),
            new Color(0.98f, 0.50f, 0.30f),
            new Color(0.05f, 0.06f, 0.14f),
            new Color(0.02f, 0.03f, 0.08f)
        };

        private float smoothSmoke;
        private float smoothTension;
        private float displayHour = 6f;

        private void Start()
        {
            if (TimeManager.Instance != null) displayHour = TargetHour(TimeManager.Instance);
            var timeline = DisasterTimeline.Instance;
            if (timeline != null) smoothTension = timeline.Tension01;

            if (sun == null) sun = FindFirstObjectByType<Light>();
            if (targetCamera == null) targetCamera = Camera.main;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }

        private void Update()
        {
            var time = TimeManager.Instance;
            if (time == null) return;

            // Gerakkan jam visual menuju target (memperhitungkan lintas tengah malam).
            float diff = Mathf.Repeat(TargetHour(time) - displayHour + 12f, 24f) - 12f;
            float step = Mathf.Clamp(diff, -hourSmoothSpeed * Time.deltaTime, hourSmoothSpeed * Time.deltaTime);
            displayHour = Mathf.Repeat(displayHour + step, 24f);
            float hour = displayHour;

            float targetTension = DisasterTimeline.Instance != null ? DisasterTimeline.Instance.Tension01 : 0f;
            smoothTension = Mathf.MoveTowards(smoothTension, targetTension, Time.deltaTime * 0.3f);

            float targetSmoke = 0f;
            if (DisasterSystem.Instance != null) targetSmoke = DisasterSystem.Instance.SmokeAtPlayer;
            smoothSmoke = Mathf.MoveTowards(smoothSmoke, targetSmoke, Time.deltaTime * 0.3f);

            // Posisi matahari: terbit jam 6, puncak jam 12, terbenam jam 18.
            float angle = hour / 24f * 360f - 90f;
            float height = Mathf.Sin(angle * Mathf.Deg2Rad);
            float dayAmount = Mathf.Clamp01(height * 1.5f + 0.25f);

            if (sun != null)
            {
                if (height > 0f)
                {
                    sun.transform.rotation = Quaternion.Euler(angle, 30f, 0f);
                    sun.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.96f, 0.88f), Mathf.Clamp01(height * 2.5f));
                    sun.intensity = Mathf.Lerp(0.15f, 1.1f, Mathf.Clamp01(height * 2f));
                }
                else
                {
                    sun.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
                    sun.color = new Color(0.45f, 0.55f, 0.9f);
                    sun.intensity = 0.2f;
                }
                sun.intensity *= 1f - 0.55f * smoothSmoke;
                sun.intensity *= 1f - tensionDim * smoothTension;
            }

            Color sky = EvaluateSky(hour);
            float grey = sky.grayscale;
            sky = Color.Lerp(sky, new Color(grey, grey, grey), tensionMute * smoothTension);
            Color smokeTint = smokeColor * Mathf.Lerp(0.15f, 1f, dayAmount);
            sky = Color.Lerp(sky, smokeTint, smoothSmoke * 0.8f);

            Color ambient = Color.Lerp(new Color(0.07f, 0.08f, 0.16f), new Color(0.5f, 0.55f, 0.65f), dayAmount);
            RenderSettings.ambientLight = ambient * (1f - 0.4f * smoothSmoke);

            RenderSettings.fogColor = sky;
            RenderSettings.fogDensity = Mathf.Lerp(0.004f, 0.03f, smoothSmoke) + tensionFog * smoothTension;

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = sky;
            }
        }

        /// <summary>Jam visual target untuk periode yang sedang tampil di HUD.</summary>
        private float TargetHour(TimeManager time)
        {
            Vector2 range;
            switch (time.Period)
            {
                case TimeOfDay.Siang: range = siangHours; break;
                case TimeOfDay.Malam: range = malamHours; break;
                default: range = pagiHours; break;
            }
            return Mathf.Lerp(range.x, range.y, time.SectionProgress01);
        }

        private static Color EvaluateSky(float hour)
        {
            for (int i = 0; i < SkyHours.Length - 1; i++)
            {
                if (hour >= SkyHours[i] && hour <= SkyHours[i + 1])
                {
                    float t = Mathf.InverseLerp(SkyHours[i], SkyHours[i + 1], hour);
                    return Color.Lerp(SkyColors[i], SkyColors[i + 1], t);
                }
            }
            return SkyColors[0];
        }
    }
}
