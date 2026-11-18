using System;
using UnityEngine;

namespace Karhutla
{
    public enum DangerLevel { Aman, Waspada, Bahaya, Kritis }

    /// <summary>
    /// Simulasi kebakaran sederhana, dijadwalkan berdasarkan HARI (GDD 3.3 dan 8.3).
    ///
    /// Aturan jadwal:
    ///  - TIDAK ada api aktif sebelum DisasterTimeline.fireStartDay (sekitar Hari 60).
    ///  - Api mulai di awal hari itu dari satu titik (fireOrigin) dengan radius kecil.
    ///  - Dampak meningkat BERTAHAP: radius tumbuh linear dari initialRadius sampai maxRadius selama
    ///    spreadDaysToMaxRadius hari (agar desa tidak langsung tak layak huni di Hari 60).
    ///  - Radius dihitung dari menit simulasi sejak api mulai, jadi konsisten walau waktu diloncati (debug).
    ///
    /// ASUMSI PROTOTYPE (bukan desain final): bentuk lingkaran, asap menjangkau smokeReach di luar garis api,
    /// pohon menyala lalu hangus, ambang bahaya. Belum ada mekanik memadamkan api.
    /// </summary>
    public class DisasterSystem : MonoBehaviour
    {
        public static DisasterSystem Instance { get; private set; }

        [Header("Lokasi & penyebaran (NILAI SEMENTARA)")]
        [Tooltip("Titik awal api berada di dalam hutan utara (di dalam batas daratan).")]
        public Vector3 fireOrigin = new Vector3(0f, 0f, 48f);
        public float initialRadius = 3f;
        [Tooltip("Radius maksimum api agar kobaran api tetap berada di dalam area daratan.")]
        public float maxRadius = 36f;
        [Tooltip("Berapa hari sejak api mulai sampai radius mencapai maksimum. 45 = dari Hari 75 sampai akhir Bulan 4 (Hari 120).")]
        public float spreadDaysToMaxRadius = 45f;
        [Tooltip("Jarak asap menjangkau melewati garis api.")]
        public float smokeReach = 45f;
        [Tooltip("Lama pohon menyala (menit simulasi) sebelum hangus.")]
        public float treeBurnMinutes = 240f;

        [Header("Debug (hanya untuk pengujian)")]
        [Tooltip("Tombol F memulai api sekarang / memperbesar api. Matikan untuk memastikan api hanya mengikuti timeline.")]
        public bool allowDebugFire = true;

        [Header("Material (diisi otomatis oleh Scene Builder)")]
        public Material burningMaterial;
        public Material burntMaterial;
        public Material flameMaterial;

        public bool FireStarted { get; private set; }
        /// <summary>True jika api dimulai lewat tombol F (debug), bukan lewat timeline.</summary>
        public bool FireStartedByDebug { get; private set; }
        public float FireRadius { get; private set; }
        public float FireProgress01 { get { return maxRadius > 0f ? Mathf.Clamp01(FireRadius / maxRadius) : 0f; } }
        public DangerLevel CurrentDanger { get; private set; }
        public float SmokeAtPlayer { get; private set; }
        public float DistanceToFront { get; private set; }
        public string DirectionToFire { get; private set; } = "-";

        /// <summary>Hari mulai api menurut timeline (default 60).</summary>
        public int FireStartDay { get { return timeline != null ? timeline.fireStartDay : 60; } }

        public event Action<DangerLevel> OnDangerChanged;

        private static readonly string[] Compass =
            { "Utara", "Timur Laut", "Timur", "Tenggara", "Selatan", "Barat Daya", "Barat", "Barat Laut" };

        private DisasterTimeline timeline;
        private float refreshTimer;
        private int fireStartMinute;
        private float debugRadiusBonus;

        private void Awake()
        {
            Instance = this;
            timeline = FindFirstObjectByType<DisasterTimeline>();
            if (timeline == null) timeline = gameObject.AddComponent<DisasterTimeline>();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMinutePassed += Tick;
                TimeManager.Instance.OnTimeJumped += HandleTimeJumped;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMinutePassed -= Tick;
                TimeManager.Instance.OnTimeJumped -= HandleTimeJumped;
            }
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Tombol F = DEBUG: mulai api sekarang / percepat penyebaran. Bukan mekanik normal.
            if (allowDebugFire && GameInput.DebugFirePressed)
            {
                if (!FireStarted) StartFire(true);
                else debugRadiusBonus += 8f;
                RefreshFire();
            }

            TryStartByTimeline();

            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.2f;
                RefreshPlayerStatus();
            }
        }

        private void HandleTimeJumped()
        {
            TryStartByTimeline();
            if (FireStarted) RefreshFire();
        }

        /// <summary>Api hanya boleh mulai dari timeline pada atau setelah fireStartDay.</summary>
        private void TryStartByTimeline()
        {
            var time = TimeManager.Instance;
            if (FireStarted || time == null) return;
            if (time.Day < FireStartDay) return;
            StartFire(false);
            RefreshFire();
        }

        private void Tick()
        {
            if (!FireStarted) return;
            RefreshFire();
            foreach (var tree in BurnableTree.All) tree.TickMinute(burntMaterial);
        }

        private void StartFire(bool byDebug)
        {
            var time = TimeManager.Instance;
            FireStarted = true;
            FireStartedByDebug = byDebug;
            fireStartMinute = byDebug && time != null ? time.TotalMinutes : (FireStartDay - 1) * (time != null ? time.SimulatedMinutesPerDay : 1440);
            FireRadius = initialRadius;

            if (byDebug) Notifier.Show("[DEBUG] Api dimulai paksa lewat tombol F (bukan timeline).");
            else Notifier.Show("Hari ke-" + FireStartDay + ": terlihat asap dan api di arah utara. Kebakaran hutan dimulai.");
        }

        // ----- Simpan / muat -----
        public int FireStartMinute { get { return fireStartMinute; } }
        public float DebugRadiusBonus { get { return debugRadiusBonus; } }

        /// <summary>
        /// Pulihkan keadaan api dari data simpan (dipanggil SETELAH waktu dipulihkan). Pohon dikembalikan sehat dulu,
        /// lalu pohon di dalam radius dinyalakan lagi (pohon yang sudah hangus ikut terbakar ulang: batasan visual saja).
        /// </summary>
        public void RestoreState(bool started, bool byDebug, int startMinute, float radiusBonus)
        {
            foreach (var tree in BurnableTree.All) tree.ResetToHealthy();

            FireStarted = started;
            FireStartedByDebug = started && byDebug;
            fireStartMinute = startMinute;
            debugRadiusBonus = radiusBonus;
            FireRadius = started ? initialRadius : 0f;
            if (started) RefreshFire();
            refreshTimer = 0f;
        }

        /// <summary>Hitung radius dari waktu simulasi lalu nyalakan pohon yang masuk radius.</summary>
        private void RefreshFire()
        {
            var time = TimeManager.Instance;
            if (!FireStarted || time == null) return;

            float elapsed = Mathf.Max(0, time.TotalMinutes - fireStartMinute);
            // "Angin": variasi halus laju penyebaran (integral dari 1 + 0.4*sin(0.01 t)), tetap monoton naik.
            float effective = elapsed + 40f * (1f - Mathf.Cos(0.01f * elapsed));
            float totalMinutesToMax = Mathf.Max(1f, spreadDaysToMaxRadius * time.SimulatedMinutesPerDay);
            float perMinute = (maxRadius - initialRadius) / totalMinutesToMax;

            FireRadius = Mathf.Min(maxRadius, initialRadius + perMinute * effective + debugRadiusBonus);

            Vector2 origin = new Vector2(fireOrigin.x, fireOrigin.z);
            foreach (var tree in BurnableTree.All)
            {
                if (tree.State != TreeState.Healthy) continue;
                Vector2 p = new Vector2(tree.transform.position.x, tree.transform.position.z);
                if (Vector2.Distance(p, origin) <= FireRadius)
                {
                    tree.Ignite(burningMaterial, flameMaterial, treeBurnMinutes);
                }
            }
        }

        /// <summary>Kadar asap 0..1 di sebuah posisi (tanpa memperhitungkan masker).</summary>
        public float GetSmokeIntensityAt(Vector3 position)
        {
            if (!FireStarted) return 0f;

            float distToOrigin = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(fireOrigin.x, fireOrigin.z));
            float beyondFront = distToOrigin - FireRadius;

            float baseValue = beyondFront <= 0f ? 1f : Mathf.Clamp01(1f - beyondFront / smokeReach);
            float intensity = Mathf.Lerp(0.35f, 1f, FireProgress01); // api besar = asap lebih pekat
            return baseValue * intensity;
        }

        private void RefreshPlayerStatus()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 p = player.transform.position;
            Vector2 toFire = new Vector2(fireOrigin.x - p.x, fireOrigin.z - p.z);

            DistanceToFront = toFire.magnitude - FireRadius;
            SmokeAtPlayer = GetSmokeIntensityAt(p);

            float angle = (Mathf.Atan2(toFire.x, toFire.y) * Mathf.Rad2Deg + 360f) % 360f;
            DirectionToFire = Compass[Mathf.RoundToInt(angle / 45f) % 8];

            DangerLevel level;
            if (!FireStarted) level = DangerLevel.Aman;
            else if (SmokeAtPlayer >= 0.7f || DistanceToFront <= 0f) level = DangerLevel.Kritis;
            else if (SmokeAtPlayer >= 0.4f || DistanceToFront < 15f) level = DangerLevel.Bahaya;
            else if (SmokeAtPlayer >= 0.1f || DistanceToFront < 40f) level = DangerLevel.Waspada;
            else level = DangerLevel.Aman;

            if (level != CurrentDanger)
            {
                CurrentDanger = level;
                if (OnDangerChanged != null) OnDangerChanged(level);
                if (level != DangerLevel.Aman) Notifier.Show("Tingkat bahaya: " + level.ToString().ToUpper());
            }
        }
    }
}
