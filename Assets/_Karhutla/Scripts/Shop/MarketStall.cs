using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Lapak Pasar (dummy). Aktivitas BELANJA yang memakai waktu (GDD 4.1).
    ///  E : "Belanja ke pasar" — memakai satu sesi (TimeManager.costs.shoppingSections) dengan konfirmasi, lalu membuka layar belanja.
    ///  Q : beli cepat satu item (tidak memakai sesi).
    /// </summary>
    public class MarketStall : InteractableBase
    {
        public override string GetPrompt()
        {
            var time = TimeManager.Instance;
            string cost = time != null ? time.DescribeCost(time.CostFromSections(time.costs.shoppingSections)) : "";
            return "Belanja ke Pasar (memakai waktu)";
        }

        public override void Interact()
        {
            var time = TimeManager.Instance;
            var activities = TimeActivityManager.Instance;
            if (time == null || activities == null || ShopManager.Instance == null) return;

            activities.Request(
                "Belanja ke pasar",
                time.CostFromSections(time.costs.shoppingSections),
                afterSpend: () => ShopManager.Instance.Open(false));
        }

        public override string GetSecondaryPrompt() { return "Beli cepat 1 item (tanpa memakai sesi)"; }

        public override void InteractSecondary()
        {
            if (ShopManager.Instance != null) ShopManager.Instance.Open(true);
        }
    }
}
