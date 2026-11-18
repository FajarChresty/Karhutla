using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Sistem survival: Hunger, Thirst, Fatigue, Smoke Exposure (+ Health sebagai dampaknya).
    ///
    /// ASUMSI PROTOTYPE: semua meter berskala 0-100 dan "makin tinggi makin buruk".
    /// Angka laju dan ambang batas di bawah hanya nilai dummy untuk demonstrasi.
    /// Dihitung per menit SIMULASI (berlangganan TimeManager.OnMinutePassed). Karena waktu berbasis sesi,
    /// menit simulasi hanya berjalan saat pemain memakai jatah waktu atau tidur; berjalan kaki dan percakapan
    /// biasa tidak menambah lapar/haus/lelah (kecuali berlari: Fatigue naik per detik nyata).
    /// NILAI SEMENTARA: satu sesi = TimeManager.simulatedMinutesPerSection menit simulasi (default 480).
    /// </summary>
    public class SurvivalSystem : MonoBehaviour
    {
        public static SurvivalSystem Instance { get; private set; }

        [Header("Laju per jam game")]
        public float hungerPerHour = 5f;
        public float thirstPerHour = 8f;
        public float fatiguePerHour = 4f;
        public float nightFatigueMultiplier = 1.5f;
        [Tooltip("Tambahan Fatigue per detik nyata saat berlari.")]
        public float sprintFatiguePerSecond = 0.6f;

        [Header("Asap")]
        [Tooltip("Seberapa cepat Smoke Exposure mengejar nilai udara sekitar (per menit game).")]
        public float smokeFollowRate = 0.06f;
        [Tooltip("Masker mengalikan paparan asap dengan nilai ini (0.3 = menyaring 70%).")]
        public float maskFilterFactor = 0.3f;

        [Header("Kesehatan")]
        public float smokeDamageThreshold = 60f;
        public float smokeDamageMax = 0.35f;

        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float Fatigue { get; private set; }
        public float SmokeExposure { get; private set; }
        public float Health { get; private set; }
        public int MaskMinutesLeft { get; private set; }
        public bool IsMasked => MaskMinutesLeft > 0;
        public bool IsSleeping { get; set; }

        public bool CanSprint => Fatigue < 90f;

        /// <summary>Pengali kecepatan jalan akibat kondisi tubuh.</summary>
        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                if (Fatigue > 80f) m *= 0.75f;
                if (SmokeExposure > 70f) m *= 0.8f;
                return m;
            }
        }

        public event Action OnCollapsed;

        private Vector3 respawnPosition;

        private void Awake()
        {
            Instance = this;
            Hunger = 10f;
            Thirst = 10f;
            Fatigue = 5f;
            SmokeExposure = 0f;
            Health = 100f;
        }

        private void Start()
        {
            if (PlayerController.Instance != null) respawnPosition = PlayerController.Instance.transform.position;
            if (TimeManager.Instance != null) TimeManager.Instance.OnMinutePassed += Tick;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnMinutePassed -= Tick;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Berlari membuat cepat lelah (dihitung per detik nyata).
            var player = PlayerController.Instance;
            if (player != null && player.IsSprinting && !GameState.InputLocked)
            {
                Fatigue = Mathf.Clamp(Fatigue + sprintFatiguePerSecond * Time.deltaTime, 0f, 100f);
            }
        }

        /// <summary>Dipanggil tiap 1 menit waktu game.</summary>
        private void Tick()
        {
            var time = TimeManager.Instance;

            // 1) Lapar, haus, lelah
            float k = IsSleeping ? 0.5f : 1f;
            Hunger = Mathf.Clamp(Hunger + hungerPerHour / 60f * k, 0f, 100f);
            Thirst = Mathf.Clamp(Thirst + thirstPerHour / 60f * k, 0f, 100f);

            if (IsSleeping)
            {
                Fatigue = Mathf.Clamp(Fatigue - 0.4f, 0f, 100f);
            }
            else
            {
                float mult = (time != null && time.Period == TimeOfDay.Malam) ? nightFatigueMultiplier : 1f;
                Fatigue = Mathf.Clamp(Fatigue + fatiguePerHour / 60f * mult, 0f, 100f);
            }

            // 2) Paparan asap: mengejar kadar asap di posisi pemain (dikurangi masker).
            float ambientSmoke = 0f;
            var disaster = DisasterSystem.Instance;
            var player = PlayerController.Instance;
            if (disaster != null && player != null) ambientSmoke = disaster.GetSmokeIntensityAt(player.transform.position);

            float target = ambientSmoke * 100f * (IsMasked ? maskFilterFactor : 1f);
            SmokeExposure = Mathf.Clamp(SmokeExposure + (target - SmokeExposure) * smokeFollowRate, 0f, 100f);

            if (MaskMinutesLeft > 0) MaskMinutesLeft--;

            // 3) Dampak ke kesehatan
            float damage = 0f;
            if (Hunger >= 100f) damage += 0.15f;
            if (Thirst >= 100f) damage += 0.25f;
            if (Fatigue >= 100f) damage += 0.10f;
            if (SmokeExposure >= smokeDamageThreshold)
            {
                damage += (SmokeExposure - smokeDamageThreshold) / (100f - smokeDamageThreshold) * smokeDamageMax;
            }

            if (damage > 0f)
            {
                Health -= damage;
            }
            else if (Hunger < 60f && Thirst < 60f && Fatigue < 80f && SmokeExposure < 30f)
            {
                Health += 0.1f;
            }
            Health = Mathf.Clamp(Health, 0f, 100f);

            if (Health <= 0f) Collapse();
        }

        /// <summary>Pulihkan kondisi tubuh dari data simpan.</summary>
        public void RestoreState(float hunger, float thirst, float fatigue, float smoke, float health, int maskMinutes)
        {
            Hunger = Mathf.Clamp(hunger, 0f, 100f);
            Thirst = Mathf.Clamp(thirst, 0f, 100f);
            Fatigue = Mathf.Clamp(fatigue, 0f, 100f);
            SmokeExposure = Mathf.Clamp(smoke, 0f, 100f);
            Health = Mathf.Clamp(health, 1f, 100f);
            MaskMinutesLeft = Mathf.Max(0, maskMinutes);
            IsSleeping = false;
        }

        public void ApplyItem(ItemDefinition item)
        {
            Hunger = Mathf.Clamp(Hunger - item.HungerRestore, 0f, 100f);
            Thirst = Mathf.Clamp(Thirst - item.ThirstRestore, 0f, 100f);
            Fatigue = Mathf.Clamp(Fatigue - item.FatigueRestore, 0f, 100f);
            if (item.MaskMinutes > 0) MaskMinutesLeft = item.MaskMinutes;
            if (item.HealthRestore > 0f) Health = Mathf.Clamp(Health + item.HealthRestore, 0f, 100f);
            if (item.SmokeRelief > 0f) SmokeExposure = Mathf.Clamp(SmokeExposure - item.SmokeRelief, 0f, 100f);
        }

        /// <summary>Damage instan ke kesehatan (mis. dari tanah gambut ambles atau air tercemar).</summary>
        public void TakeDamage(float amount, string cause = "")
        {
            if (amount <= 0f) return;
            Health = Mathf.Clamp(Health - amount, 0f, 100f);
            if (Health <= 0f) Collapse();
        }

        public void AddFatigue(float amount)
        {
            if (amount <= 0f) return;
            Fatigue = Mathf.Clamp(Fatigue + amount, 0f, 100f);
        }

        public void RestoreThirst(float amount)
        {
            if (amount <= 0f) return;
            Thirst = Mathf.Clamp(Thirst - amount, 0f, 100f);
        }

        /// <summary>ASUMSI PROTOTYPE: kalau Health habis, pemain pingsan dan dibawa kembali ke titik awal.</summary>
        private void Collapse()
        {
            Notifier.Show("Kamu pingsan! Warga membawamu kembali ke dekat warung.");
            Health = 50f;
            SmokeExposure = 20f;
            Hunger = Mathf.Min(Hunger, 60f);
            Thirst = Mathf.Min(Thirst, 60f);
            Fatigue = Mathf.Min(Fatigue, 60f);

            if (PlayerController.Instance != null) PlayerController.Instance.TeleportTo(respawnPosition);
            OnCollapsed?.Invoke();
        }
    }
}
