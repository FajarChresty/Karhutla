using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Pintu masuk semua aktivitas yang memakai jatah waktu.
    /// GDD 4.1: aktivitas yang memakai waktu harus memberi tahu pemain SEBELUM konfirmasi.
    ///
    /// Cara pakai dari script aktivitas:
    ///   TimeActivityManager.Instance.Request("Judul", cost, afterSpend, beforeSpend);
    ///  - cost gratis (0)  : langsung dijalankan tanpa dialog konfirmasi.
    ///  - cost berbayar    : dialog konfirmasi tampil (E = ya, Esc/Q = batal). Jika ya:
    ///                       beforeSpend() -> TimeManager.Spend(cost) -> afterSpend().
    /// Selama dialog tampil, input dikunci (GameState.InputLocked).
    /// TimeConfirmUI hanya membaca properti di class ini.
    /// </summary>
    public class TimeActivityManager : MonoBehaviour
    {
        public static TimeActivityManager Instance { get; private set; }

        public bool IsPending { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }

        private TimeCost pendingCost;
        private Action pendingBefore;
        private Action pendingAfter;

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (IsPending) GameState.SetInputLocked(false);
            if (Instance == this) Instance = null;
        }

        /// <summary>Minta pemain mengonfirmasi aktivitas. Mengembalikan false jika tidak bisa dimulai.</summary>
        public bool Request(string title, TimeCost cost, Action afterSpend = null, Action beforeSpend = null)
        {
            var time = TimeManager.Instance;
            if (time == null || IsPending) return false;

            if (cost.IsFree)
            {
                if (beforeSpend != null) beforeSpend();
                if (afterSpend != null) afterSpend();
                return true;
            }

            if (time.FinalDayEnded)
            {
                Notifier.Show("Hari ke-" + time.totalDays + " sudah berakhir. Tidak ada waktu tersisa.");
                return false;
            }

            Title = title;
            Description = time.DescribeCost(cost);
            pendingCost = cost;
            pendingBefore = beforeSpend;
            pendingAfter = afterSpend;
            IsPending = true;
            GameState.SetInputLocked(true);
            return true;
        }

        private void Update()
        {
            if (!IsPending) return;
            if (Time.frameCount == GameState.LockChangedFrame) return; // abaikan tombol yang baru membuka dialog

            if (GameInput.ConfirmPressed) Confirm();
            else if (GameInput.CancelPressed) Cancel();
        }

        private void Confirm()
        {
            var before = pendingBefore;
            var after = pendingAfter;
            var cost = pendingCost;
            string title = Title;
            Clear();

            // Catat untuk laporan akhir hari (sebelum waktu berpindah, supaya masuk ke hari yang benar).
            if (!string.IsNullOrEmpty(title)) DayReportSystem.LogActivity(title.TrimEnd('?', ' '));

            if (before != null) before();
            if (TimeManager.Instance != null) TimeManager.Instance.Spend(cost);
            if (after != null) after();
        }

        private void Cancel()
        {
            Clear();
            Notifier.Show("Dibatalkan.");
        }

        private void Clear()
        {
            IsPending = false;
            pendingBefore = null;
            pendingAfter = null;
            GameState.SetInputLocked(false);
        }
    }
}
