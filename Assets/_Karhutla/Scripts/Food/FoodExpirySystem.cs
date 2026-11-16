using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Masa kedaluwarsa bahan makanan dan masakan.
    ///
    /// Umur item dihitung dalam SESI waktu (1 periode = 1 sesi), bukan menit: setiap kali periode berganti
    /// (termasuk saat tidur melewati beberapa periode) semua item bertambah umur. Item basi bila umur >= masa simpan.
    /// Kulkas memperlambat penuaan dengan mengalikan penambahan umur dengan fridgeAgingFactor.
    ///
    /// SEMUA ANGKA DI BAWAH ADALAH NILAI SEMENTARA (GDD belum menentukan durasi kedaluwarsa maupun efek pendinginan).
    /// Ubah lewat Inspector komponen ini.
    /// </summary>
    public class FoodExpirySystem : MonoBehaviour
    {
        public static FoodExpirySystem Instance { get; private set; }

        public const float DefaultIngredientShelfLife = 9f;
        public const float DefaultCookedShelfLife = 3f;

        [Header("Masa simpan (dalam sesi/periode, NILAI SEMENTARA)")]
        [Tooltip("Bahan mentah (beras, sayur, bubuk kopi). 9 sesi = 3 hari.")]
        public float ingredientShelfLifeSections = DefaultIngredientShelfLife;
        [Tooltip("Makanan/minuman yang sudah dimasak. 3 sesi = 1 hari.")]
        public float cookedShelfLifeSections = DefaultCookedShelfLife;

        [Header("Laju penuaan (NILAI SEMENTARA)")]
        [Tooltip("Pengali umur di inventaris dan gudang (suhu ruang).")]
        public float ambientAgingFactor = 1f;
        [Tooltip("Pengali umur di kulkas. 0.25 = membusuk 4x lebih lambat. 0 = tidak pernah basi di kulkas.")]
        public float fridgeAgingFactor = 0.25f;

        private int lastSectionIndex;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            var time = TimeManager.Instance;
            if (time == null) return;
            lastSectionIndex = SectionIndex(time);
            time.OnPeriodChanged += HandlePeriodChanged;
            time.OnTimeJumped += HandleTimeJumped;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPeriodChanged -= HandlePeriodChanged;
                TimeManager.Instance.OnTimeJumped -= HandleTimeJumped;
            }
            if (Instance == this) Instance = null;
        }

        private static int SectionIndex(TimeManager time)
        {
            return (time.Day - 1) * 3 + (int)time.Period;
        }

        /// <summary>Sinkronkan penanda sesi tanpa menua makanan (dipakai setelah memuat data simpan; umur sudah tersimpan di tiap batch).</summary>
        public void ResyncWithoutAging()
        {
            var time = TimeManager.Instance;
            if (time != null) lastSectionIndex = SectionIndex(time);
        }

        private void HandlePeriodChanged(TimeOfDay period) { SyncAging(); }
        private void HandleTimeJumped() { SyncAging(); }

        private void SyncAging()
        {
            var time = TimeManager.Instance;
            if (time == null) return;

            int now = SectionIndex(time);
            int delta = now - lastSectionIndex;
            lastSectionIndex = now;
            if (delta <= 0) return;

            int inventoryExpired = 0, gudangExpired = 0, fridgeExpired = 0;

            if (Inventory.Instance != null)
                inventoryExpired = Inventory.Instance.Container.AgeAll(delta * ambientAgingFactor);

            var storage = WarungStorage.Instance;
            if (storage != null)
            {
                gudangExpired = storage.Gudang.AgeAll(delta * ambientAgingFactor);
                fridgeExpired = storage.Fridge.AgeAll(delta * fridgeAgingFactor);
            }

            int total = inventoryExpired + gudangExpired + fridgeExpired;
            if (total > 0)
            {
                DayReportSystem.LogExpired(total);
                string where = "";
                if (inventoryExpired > 0) where += " inventaris " + inventoryExpired;
                if (gudangExpired > 0) where += " gudang " + gudangExpired;
                if (fridgeExpired > 0) where += " kulkas " + fridgeExpired;
                if (!time.IsSkipping)
                    Notifier.Show(total + " item basi (" + where.Trim() + "). Buang lewat tombol G (inventaris) atau menu gudang/kulkas.");
            }
        }

        // ================= Query (statis, aman dipanggil tanpa instance) =================

        public static float ShelfLife(ItemDefinition def)
        {
            if (def == null) return 0f;
            switch (def.Perish)
            {
                case Perishability.Bahan:
                    return Instance != null ? Instance.ingredientShelfLifeSections : DefaultIngredientShelfLife;
                case Perishability.Masakan:
                    return Instance != null ? Instance.cookedShelfLifeSections : DefaultCookedShelfLife;
                default:
                    return 0f; // tidak kedaluwarsa
            }
        }

        public static bool IsExpired(string id, float age)
        {
            float life = ShelfLife(ItemDatabase.Get(id));
            return life > 0f && age >= life;
        }

        /// <summary>Teks singkat kesegaran untuk UI, mis. "BASI", "hampir basi", "awet 2.5 sesi". Kosong bila tidak kedaluwarsa.</summary>
        public static string FreshnessText(string id, float age)
        {
            float life = ShelfLife(ItemDatabase.Get(id));
            if (life <= 0f) return "";
            float remaining = life - age;
            if (remaining <= 0f) return "<color=#FF6B5A>BASI</color>";
            if (remaining <= 1f) return "<color=#FFD166>hampir basi</color>";
            return "awet " + remaining.ToString("0.#") + " sesi";
        }
    }
}
