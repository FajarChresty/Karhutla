using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Sistem Deteksi Kegagalan Usaha & Kebangkrutan Warung (GDD Bab 5.5).
    ///
    /// Aturan GDD:
    ///  - Pada paruh pertama game (Hari 1-59, sebelum bencana kebakaran memuncak),
    ///    pemain harus mengelola keuangan untuk belanja bahan pokok warung.
    ///  - Jika pemain tidak mampu mempertahankan usaha dan tidak mampu membeli bahan pokok penting,
    ///    terdapat kondisi kegagalan (Game Over: Business Failure).
    /// </summary>
    public class BankruptcySystem : MonoBehaviour
    {
        public static BankruptcySystem Instance { get; private set; }

        [Header("Ambang Batas Keuangan (NILAI SEMENTARA)")]
        [Tooltip("Batas modal minimal untuk dapat membeli bahan makanan termurah di pasar.")]
        public int minimumCapitalThreshold = 5000;

        [Tooltip("Berapa hari berturut-turut dalam kondisi kritis sebelum dinyatakan Game Over.")]
        public int allowedCriticalDays = 2;

        public int ConsecutiveCriticalDays { get; private set; }
        public bool IsInFinancialCrisis { get; private set; }

        public event Action OnBankruptcyWarning;
        public event Action OnBankruptcyGameOver;

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
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += EvaluateFinancialHealth;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= EvaluateFinancialHealth;
            }
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Evaluasi kesehatan finansial warung di awal hari baru.
        /// Hanya aktif pada paruh pertama permainan (sebelum Day 60).
        /// </summary>
        public void EvaluateFinancialHealth(int day)
        {
            var time = TimeManager.Instance;
            var disaster = DisasterSystem.Instance;

            // Kebangkrutan warung hanya menjadi penalti di paruh pertama (GDD 5.5: "first-half failure condition")
            int fireStartDay = disaster != null ? disaster.FireStartDay : 60;
            if (day >= fireStartDay)
            {
                IsInFinancialCrisis = false;
                ConsecutiveCriticalDays = 0;
                return;
            }

            var inv = Inventory.Instance;
            if (inv == null) return;

            int money = inv.Money;
            int totalFood = CountAllFreshFoodAndIngredients();

            // Kondisi Kritis: Uang kurang dari modal beli sembako termurah DAN tidak punya stok makanan/bahan layak
            if (money < minimumCapitalThreshold && totalFood <= 0)
            {
                IsInFinancialCrisis = true;
                ConsecutiveCriticalDays++;

                if (ConsecutiveCriticalDays < allowedCriticalDays)
                {
                    // Peringatan Hari Pertama
                    string warn = "PERINGATAN KEBANGKRUTAN (Hari " + ConsecutiveCriticalDays + "/" + allowedCriticalDays + "): "
                        + "Uang warung tersisa " + TextFormat.Rupiah(money) + " dan persediaan bahan makanan habis!\n"
                        + "Dapatkan pemasukan hari ini atau barter barang sebelum besok, atau warung akan gulung tikar.";
                    Notifier.Show(warn);
                    DayReportSystem.LogActivity("Peringatan Finansial: Modal habis & tanpa stok!");
                    OnBankruptcyWarning?.Invoke();
                }
                else
                {
                    // Game Over Kebangkrutan
                    TriggerBankruptcyGameOver();
                }
            }
            else
            {
                if (IsInFinancialCrisis)
                {
                    Notifier.Show("Keuangan warung berangsur aman. Operasional dapat dilanjutkan.");
                }
                IsInFinancialCrisis = false;
                ConsecutiveCriticalDays = 0;
            }
        }

        private void TriggerBankruptcyGameOver()
        {
            OnBankruptcyGameOver?.Invoke();

            string reason = "KEBANGKRUTAN WARUNG (Business Failure)\n"
                + "Kamu kehabisan modal operasional dan tidak lagi memiliki persediaan makanan pokok untuk menjalankan warung keluarga. "
                + "Di tengah teriknya awal musim kemarau, warung terpaksa gulung tikar sebelum sempat bertahan.";

            if (EndingManager.Instance != null)
            {
                EndingManager.Instance.TriggerEndingScreen(reason);
            }
            else
            {
                Notifier.Show(reason);
            }
        }

        /// <summary>Menghitung total seluruh hidangan dan bahan makanan segar di inventaris, kulkas, dan gudang.</summary>
        public int CountAllFreshFoodAndIngredients()
        {
            int count = 0;
            foreach (var item in ItemDatabase.All)
            {
                if (item.Category == ItemCategory.Masakan || item.Category == ItemCategory.Bahan || item.Category == ItemCategory.Minuman)
                {
                    count += WarungStorage.TotalFresh(item.Id);
                }
            }
            return count;
        }

        // Simpan & Muat
        public void RestoreState(int criticalDays, bool inCrisis)
        {
            ConsecutiveCriticalDays = criticalDays;
            IsInFinancialCrisis = inCrisis;
        }
    }
}
