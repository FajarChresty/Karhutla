using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Satu slot kegiatan jadwal harian NPC untuk satu periode waktu (Pagi, Siang, Malam).
    /// </summary>
    [Serializable]
    public class ScheduleSlot
    {
        public TimeOfDay period;
        public string locationName;
        public Vector3 destination;
        public float wanderRadius = 3f;
        public string activityDescription;

        public ScheduleSlot() { }

        public ScheduleSlot(TimeOfDay p, string loc, Vector3 dest, float radius, string act)
        {
            period = p;
            locationName = loc;
            destination = dest;
            wanderRadius = radius;
            activityDescription = act;
        }
    }

    /// <summary>
    /// Jadwal & Rutinitas Harian Warga (GDD Bab 6.3 & 8.2).
    /// Mengatur pergerakan warga antar lokasi sesuai periode waktu (Pagi, Siang, Malam),
    /// serta adaptasi saat kondisi bencana (Waspada/Bahaya/Kritis).
    /// </summary>
    [RequireComponent(typeof(NpcController))]
    public class NpcSchedule : MonoBehaviour
    {
        [Header("Jadwal Periode (Pagi, Siang, Malam)")]
        public List<ScheduleSlot> dailySlots = new List<ScheduleSlot>();

        [Header("Jadwal Darurat / Bencana (Opsional)")]
        [Tooltip("Jika dicentang, saat bencana masuk tingkat Bahaya/Kritis, NPC pindah ke lokasi darurat.")]
        public bool hasEmergencySchedule = true;
        public string emergencyLocationName = "Posko Balai Desa";
        public Vector3 emergencyDestination = new Vector3(-2f, 1f, 12f);
        public float emergencyWanderRadius = 2.5f;
        public string emergencyActivity = "Siaga di Posko Darurat";

        private NpcController controller;
        private ScheduleSlot activeSlot;
        private bool inEmergency;

        public string CurrentLocationName => inEmergency ? emergencyLocationName : (activeSlot != null ? activeSlot.locationName : "Desa");
        public string CurrentActivity => inEmergency ? emergencyActivity : (activeSlot != null ? activeSlot.activityDescription : "Beraktivitas");

        private void Awake()
        {
            controller = GetComponent<NpcController>();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPeriodChanged += HandlePeriodChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
                TimeManager.Instance.OnTimeSkipped += HandleTimeSkipped;
                TimeManager.Instance.OnTimeJumped += HandleTimeJumped;
            }

            if (DisasterSystem.Instance != null)
            {
                DisasterSystem.Instance.OnDangerChanged += HandleDangerChanged;
            }

            // Inisialisasi awal posisi jadwal
            ApplySchedule(immediate: true);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPeriodChanged -= HandlePeriodChanged;
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnTimeSkipped -= HandleTimeSkipped;
                TimeManager.Instance.OnTimeJumped -= HandleTimeJumped;
            }

            if (DisasterSystem.Instance != null)
            {
                DisasterSystem.Instance.OnDangerChanged -= HandleDangerChanged;
            }
        }

        private void HandlePeriodChanged(TimeOfDay newPeriod)
        {
            ApplySchedule(immediate: false);
        }

        private void HandleDayChanged(int day)
        {
            ApplySchedule(immediate: false);
        }

        private void HandleTimeSkipped(int fromDay, int toDay)
        {
            ApplySchedule(immediate: true);
        }

        private void HandleTimeJumped()
        {
            ApplySchedule(immediate: true);
        }

        private void HandleDangerChanged(DangerLevel danger)
        {
            ApplySchedule(immediate: false);
        }

        /// <summary>
        /// Menerapkan jadwal saat ini ke NPC Controller.
        /// immediate = true jika waktu melompat/skip atau saat load game (teleport instan).
        /// </summary>
        public void ApplySchedule(bool immediate = false)
        {
            if (controller == null) controller = GetComponent<NpcController>();
            if (controller == null) return;

            var disaster = DisasterSystem.Instance;
            bool dangerState = disaster != null && disaster.CurrentDanger >= DangerLevel.Bahaya;

            if (hasEmergencySchedule && dangerState)
            {
                inEmergency = true;
                controller.SetScheduleTarget(emergencyDestination, emergencyWanderRadius, emergencyLocationName, emergencyActivity, immediate);
                return;
            }

            inEmergency = false;
            TimeOfDay currentPeriod = TimeManager.Instance != null ? TimeManager.Instance.Period : TimeOfDay.Pagi;
            activeSlot = FindSlotForPeriod(currentPeriod);

            if (activeSlot != null)
            {
                controller.SetScheduleTarget(activeSlot.destination, activeSlot.wanderRadius, activeSlot.locationName, activeSlot.activityDescription, immediate);
            }
        }

        private ScheduleSlot FindSlotForPeriod(TimeOfDay period)
        {
            for (int i = 0; i < dailySlots.Count; i++)
            {
                if (dailySlots[i].period == period) return dailySlots[i];
            }

            // Fallback: slot pertama jika tidak ditemukan
            return dailySlots.Count > 0 ? dailySlots[0] : null;
        }

        /// <summary>
        /// Menambahkan atau memperbarui slot jadwal untuk periode tertentu.
        /// </summary>
        public void SetSlot(TimeOfDay period, string locName, Vector3 dest, float radius, string activity)
        {
            for (int i = 0; i < dailySlots.Count; i++)
            {
                if (dailySlots[i].period == period)
                {
                    dailySlots[i].locationName = locName;
                    dailySlots[i].destination = dest;
                    dailySlots[i].wanderRadius = radius;
                    dailySlots[i].activityDescription = activity;
                    return;
                }
            }
            dailySlots.Add(new ScheduleSlot(period, locName, dest, radius, activity));
        }
    }
}
