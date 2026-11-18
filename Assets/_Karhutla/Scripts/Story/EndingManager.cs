using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Pengelola pemicu akhir permainan (Ending Trigger).
    ///
    /// Mendeteksi saat:
    ///  1. Kalender cerita mencapai Hari ke-120 (akhir Bulan 4).
    ///  2. Total hari gameplay aktif mencapai target ~50-60 hari.
    ///
    /// Menampilkan evaluasi akhir permainan (Ending Screen) dengan ringkasan komprehensif.
    /// </summary>
    public class EndingManager : MonoBehaviour
    {
        public static EndingManager Instance { get; private set; }

        public bool IsEndingActive { get; private set; }
        public string EndingReason { get; private set; }

        public event Action<string> OnEndingShown;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnEndingTriggered += HandleEndingTriggered;
                TimeManager.Instance.OnFinalDayEnded += HandleFinalDayEnded;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnEndingTriggered -= HandleEndingTriggered;
                TimeManager.Instance.OnFinalDayEnded -= HandleFinalDayEnded;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleEndingTriggered(string reason)
        {
            TriggerEndingScreen(reason);
        }

        private void HandleFinalDayEnded()
        {
            if (!IsEndingActive)
            {
                var time = TimeManager.Instance;
                string reason = time != null && time.Day >= time.totalDays
                    ? "Kalender 4 bulan (" + time.totalDays + " hari) telah tuntas."
                    : "Waktu cerita telah mencapai batas akhir.";
                TriggerEndingScreen(reason);
            }
        }

        /// <summary>
        /// Memicu layar ending secara langsung.
        /// </summary>
        public void TriggerEndingScreen(string reason)
        {
            if (IsEndingActive) return;
            IsEndingActive = true;
            EndingReason = reason;

            GameState.SetInputLocked(true);

            if (EndingScreenUI.Instance != null)
            {
                EndingScreenUI.Instance.Show(reason);
            }
            else
            {
                // Pengaman: Buat UI jika belum terpasang
                var ui = gameObject.AddComponent<EndingScreenUI>();
                ui.Show(reason);
            }

            if (OnEndingShown != null) OnEndingShown(reason);
        }
    }
}
