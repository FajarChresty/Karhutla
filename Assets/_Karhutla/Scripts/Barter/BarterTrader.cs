using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Satu barang yang bisa didapat dari warga lewat barter.</summary>
    [Serializable]
    public class BarterOffer
    {
        public string itemId;
        [Tooltip("Sisa persediaan warga untuk barang ini saat ini.")]
        public int stock = 3;
        [Tooltip("Batas persediaan warga untuk barang ini. Stok tidak pernah melebihi batas ini.")]
        public int maxStock = 5;
        [Tooltip("Pengali nilai barang (1 = nilai pasar barang dari ItemDefinition).")]
        public float valueMultiplier = 1f;
        [Tooltip("Tingkat hubungan minimal agar barang ini ditawarkan. Dingin = selalu tersedia.")]
        public RelationshipTier minTier = RelationshipTier.Dingin;
    }

    /// <summary>Data simpan stok satu warga (dipakai GameSaveSystem).</summary>
    [Serializable]
    public class BarterTraderSave
    {
        public string npcId;
        public int daysSinceRestock;
        public List<string> itemIds = new List<string>();
        public List<int> stocks = new List<int>();
    }

    /// <summary>
    /// Profil barter seorang warga. Pasang di GameObject yang sama dengan NpcController.
    /// Warga TIDAK menerima semua barang: hanya acceptedItemIds. Isi tabel (barang yang diterima/ditawarkan, stok, tingkat hubungan)
    /// diatur di scene builder / Inspector dan harus dikonfirmasi Game Designer.
    ///
    /// NILAI BARANG (satu rumus untuk semua, semua dari ItemDefinition):
    ///   nilai pasar = BarterValue x Scarcity x (1 + DisasterDemand x faktorBencana)
    ///   faktorBencana: 0 sebelum api, naik menurut tingkat bahaya (Aman 0.15 ... Kritis 1).
    /// Barang yang DIMINTA pemain juga dikali: pengali barang x kelangkaan stok (makin sedikit stok makin mahal)
    /// x pengali tingkat hubungan (RelationshipSystem). Barang yang DITAWARKAN pemain dikali acceptMultiplier.
    /// Stok pulih bertahap (restockEveryDays) dan bertambah bila pemain memberikan barang yang sama dengan barang tawaran warga.
    /// </summary>
    public class BarterTrader : MonoBehaviour
    {
        [Tooltip("Barang dari pemain yang mau diterima warga ini.")]
        public List<string> acceptedItemIds = new List<string>();
        [Tooltip("Pengali nilai barang pemain yang diterima warga (>1 = warga menghargai lebih tinggi).")]
        public float acceptMultiplier = 1f;
        [Tooltip("Barang yang bisa didapat pemain dari warga.")]
        public List<BarterOffer> offers = new List<BarterOffer>();

        [Header("Kelangkaan stok & pemulihan (NILAI SEMENTARA)")]
        [Tooltip("Tambahan harga saat stok hampir habis. 0.5 = harga naik sampai +50% ketika stok tinggal sedikit.")]
        public float lowStockPremium = 0.5f;
        [Tooltip("Stok barang yang kurang dari batas bertambah 1 setiap sekian hari. 0 = tidak pulih.")]
        public int restockEveryDays = 3;

        private int lastDay = -1;
        private int daysSinceRestock;

        public BarterTraderSave ExportState()
        {
            var save = new BarterTraderSave { npcId = NpcId, daysSinceRestock = daysSinceRestock };
            if (offers != null)
            {
                foreach (var o in offers)
                {
                    if (o == null) continue;
                    save.itemIds.Add(o.itemId);
                    save.stocks.Add(o.stock);
                }
            }
            return save;
        }

        /// <summary>Pulihkan stok dari data simpan. Barang dicocokkan lewat itemId; stok dibatasi maxStock.</summary>
        public void ImportState(BarterTraderSave save)
        {
            if (save == null || offers == null) return;
            daysSinceRestock = Mathf.Max(0, save.daysSinceRestock);
            for (int i = 0; i < save.itemIds.Count && i < save.stocks.Count; i++)
            {
                foreach (var o in offers)
                {
                    if (o != null && o.itemId == save.itemIds[i]) o.stock = Mathf.Clamp(save.stocks[i], 0, o.maxStock);
                }
            }
            var time = TimeManager.Instance;
            if (time != null) lastDay = time.Day;
        }

        public string DisplayName
        {
            get
            {
                var npc = GetComponent<NpcController>();
                return npc != null ? npc.npcName : name;
            }
        }

        public string NpcId
        {
            get
            {
                var npc = GetComponent<NpcController>();
                return npc != null ? npc.NpcId : name;
            }
        }

        private void Awake()
        {
            // Pastikan batas stok tidak lebih kecil dari stok awal.
            if (offers == null) return;
            foreach (var o in offers)
            {
                if (o == null) continue;
                o.maxStock = Mathf.Max(o.maxStock, o.stock, 1);
                o.stock = Mathf.Clamp(o.stock, 0, o.maxStock);
            }
        }

        private void Start()
        {
            var time = TimeManager.Instance;
            if (time == null) return;
            lastDay = time.Day;
            time.OnDayChanged += HandleDayChanged;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int day)
        {
            int passed = lastDay < 0 ? 1 : Mathf.Max(1, day - lastDay);
            lastDay = day;
            if (restockEveryDays <= 0 || offers == null) return;

            daysSinceRestock += passed;
            while (daysSinceRestock >= restockEveryDays)
            {
                daysSinceRestock -= restockEveryDays;
                foreach (var o in offers)
                {
                    if (o != null && o.stock < o.maxStock) o.stock++;
                }
            }
        }

        // ================= Nilai barang =================

        /// <summary>0..1: seberapa parah kondisi bencana. Dipakai untuk menaikkan nilai barang kebutuhan.</summary>
        public static float DisasterFactor()
        {
            var d = DisasterSystem.Instance;
            if (d == null || !d.FireStarted) return 0f;
            switch (d.CurrentDanger)
            {
                case DangerLevel.Waspada: return 0.4f;
                case DangerLevel.Bahaya: return 0.75f;
                case DangerLevel.Kritis: return 1f;
                default: return 0.15f;
            }
        }

        /// <summary>Nilai pasar satu unit barang saat ini (jenis x kelangkaan x kebutuhan bencana).</summary>
        public static float MarketValue(ItemDefinition def)
        {
            if (def == null) return 0f;
            return def.BarterValue * def.Scarcity * (1f + def.DisasterDemand * DisasterFactor());
        }

        public static float MarketValue(string itemId) { return MarketValue(ItemDatabase.Get(itemId)); }

        public bool Accepts(string itemId) { return acceptedItemIds != null && acceptedItemIds.Contains(itemId); }

        /// <summary>Nilai barang pemain yang ditawarkan ke warga ini.</summary>
        public float ValueOfOffered(string itemId, int count)
        {
            return MarketValue(itemId) * acceptMultiplier * count;
        }

        // ================= Hubungan sosial =================

        public RelationshipTier Tier
        {
            get
            {
                var rel = RelationshipSystem.Instance;
                return rel != null ? rel.GetTier(NpcId) : RelationshipTier.Kenalan;
            }
        }

        public float RelationshipPriceMultiplier
        {
            get
            {
                var rel = RelationshipSystem.Instance;
                return rel != null ? rel.BarterPriceMultiplier(NpcId) : 1f;
            }
        }

        /// <summary>Apakah barang ini sudah ditawarkan pada tingkat hubungan sekarang?</summary>
        public bool IsUnlocked(BarterOffer offer)
        {
            return offer != null && Tier >= offer.minTier;
        }

        // ================= Barang yang diminta pemain =================

        /// <summary>Pengali harga karena stok menipis: 1 saat stok penuh, sampai 1 + lowStockPremium saat stok hampir habis.</summary>
        public float StockPriceMultiplier(BarterOffer offer)
        {
            if (offer == null || offer.maxStock <= 0) return 1f;
            float missing = 1f - Mathf.Clamp01(offer.stock / (float)offer.maxStock);
            return 1f + lowStockPremium * missing;
        }

        /// <summary>Nilai yang harus dibayar untuk SATU unit barang ini (sudah termasuk kelangkaan stok dan hubungan).</summary>
        public float RequiredValue(BarterOffer offer)
        {
            return RequiredValue(offer, 1);
        }

        public float RequiredValue(BarterOffer offer, int count)
        {
            if (offer == null) return 0f;
            var def = ItemDatabase.Get(offer.itemId);
            if (def == null) return 0f;
            return MarketValue(def) * offer.valueMultiplier * StockPriceMultiplier(offer) * RelationshipPriceMultiplier * Mathf.Max(1, count);
        }

        /// <summary>Pemain memberikan barang ke warga: bila barangnya sama dengan barang tawaran warga, stok warga bertambah (dibatasi maxStock).</summary>
        public void ReceiveGoods(string itemId, int count)
        {
            if (offers == null || count <= 0) return;
            foreach (var o in offers)
            {
                if (o != null && o.itemId == itemId) o.stock = Mathf.Min(o.maxStock, o.stock + count);
            }
        }
    }
}
