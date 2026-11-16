using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Tempat tidur (bale-bale). GDD 4.1: pulang dan tidur memungkinkan pemain maju ke hari berikutnya.
    /// Tidur melewati sisa hari menuju Pagi berikutnya (TimeCost.ToNextDay) dan meminta konfirmasi dulu.
    ///
    /// V1 hanya memajukan 4 jam; sekarang mengikuti sistem sesi. Penolakan tidur saat bahaya tinggi
    /// (ASUMSI PROTOTYPE V1) tetap dipertahankan.
    /// </summary>
    public class RestSpot : InteractableBase
    {
        // [Q] Time skip: lewati beberapa hari tanpa event penting (lihat TimeSkipSystem).
        public override string GetSecondaryPrompt()
        {
            var skip = TimeSkipSystem.Instance;
            if (skip == null) return null;
            int target;
            string reason;
            return skip.CanSkip(out target, out reason) ? "Lewati hari sampai Hari " + target + " (time skip)" : "Time skip tidak tersedia: " + reason;
        }

        public override void InteractSecondary()
        {
            if (TimeSkipSystem.Instance != null) TimeSkipSystem.Instance.RequestSkip();
        }

        public override void Interact()
        {
            var time = TimeManager.Instance;
            var survival = SurvivalSystem.Instance;
            var activities = TimeActivityManager.Instance;
            if (time == null || survival == null || activities == null) return;

            var disaster = DisasterSystem.Instance;
            if (disaster != null && disaster.CurrentDanger >= DangerLevel.Bahaya)
            {
                Notifier.Show("Tidak bisa tidur, asap terlalu tebal dan api terlalu dekat!");
                return;
            }

            activities.Request(
                "Tidur sampai besok?",
                TimeCost.ToNextDay,
                afterSpend: () =>
                {
                    survival.IsSleeping = false;
                    if (!time.FinalDayEnded) Notifier.Show("Kamu tidur. Hari ke-" + time.Day + ", " + time.Period + ".");
                },
                beforeSpend: () => { survival.IsSleeping = true; });
        }
    }
}
