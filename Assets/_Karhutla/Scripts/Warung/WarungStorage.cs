using System.Text;
using UnityEngine;

namespace Karhutla
{
    public enum StorageKind { Gudang, Kulkas }

    /// <summary>
    /// Penyimpanan warung: GUDANG (suhu ruang) dan KULKAS (memperlambat kedaluwarsa).
    /// Keduanya adalah wadah terpisah dari inventaris yang dibawa pemain, masing-masing dengan batas berat sendiri
    /// (NILAI SEMENTARA). Perpindahan barang dilakukan lewat StorageManager.
    /// </summary>
    public class WarungStorage : MonoBehaviour
    {
        public static WarungStorage Instance { get; private set; }

        [Header("Kapasitas (kg, NILAI SEMENTARA)")]
        public float gudangMaxWeight = 150f;
        public float fridgeMaxWeight = 30f;

        [Header("Stok awal (dummy, mengganti stok V1)")]
        public bool seedInitialStock = true;

        public ItemContainer Gudang { get; private set; }
        public ItemContainer Fridge { get; private set; }

        private void Awake()
        {
            Instance = this;
            Gudang = new ItemContainer("Gudang Warung", gudangMaxWeight);
            Fridge = new ItemContainer("Kulkas", fridgeMaxWeight);

            if (seedInitialStock)
            {
                Gudang.Add(ItemDatabase.Nasi, 4);
                Gudang.Add(ItemDatabase.Air, 4);
                Gudang.Add(ItemDatabase.Kopi, 3);
                Gudang.Add(ItemDatabase.Masker, 2);
                Gudang.Add(ItemDatabase.Beras, 2);
                Gudang.Add(ItemDatabase.Sayur, 2);
                Gudang.Add(ItemDatabase.BubukKopi, 1);
            }
        }

        private void Update()
        {
            // Sinkronkan kapasitas bila diubah lewat Inspector saat Play.
            Gudang.MaxWeight = gudangMaxWeight;
            Fridge.MaxWeight = fridgeMaxWeight;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public ItemContainer Get(StorageKind kind)
        {
            return kind == StorageKind.Kulkas ? Fridge : Gudang;
        }

        /// <summary>Jumlah item layak di inventaris + gudang + kulkas.</summary>
        public static int TotalFresh(string id)
        {
            int n = 0;
            if (Inventory.Instance != null) n += Inventory.Instance.Container.CountFresh(id);
            if (Instance != null) n += Instance.Gudang.CountFresh(id) + Instance.Fridge.CountFresh(id);
            return n;
        }

        /// <summary>Jumlah item layak di gudang + kulkas saja (stok warung).</summary>
        public static int StockFresh(string id)
        {
            if (Instance == null) return 0;
            return Instance.Gudang.CountFresh(id) + Instance.Fridge.CountFresh(id);
        }

        /// <summary>
        /// Pakai bahan masak: diambil dari inventaris, lalu gudang, lalu kulkas (hanya yang masih layak).
        /// Semua atau tidak sama sekali.
        /// </summary>
        public static bool ConsumeFresh(string id, int count)
        {
            if (TotalFresh(id) < count) return false;

            int left = count;
            ItemContainer[] sources =
            {
                Inventory.Instance != null ? Inventory.Instance.Container : null,
                Instance != null ? Instance.Gudang : null,
                Instance != null ? Instance.Fridge : null
            };

            for (int i = 0; i < sources.Length && left > 0; i++)
            {
                var src = sources[i];
                if (src == null) continue;
                int take = Mathf.Min(left, src.CountFresh(id));
                if (take > 0 && src.RemoveFresh(id, take)) left -= take;
            }
            return left == 0;
        }

        /// <summary>Ringkasan isi wadah, mis. "Nasi 4, Air 4 (1 basi)".</summary>
        public static string Summary(ItemContainer container)
        {
            if (container == null) return "-";
            var sb = new StringBuilder();
            foreach (var def in ItemDatabase.All)
            {
                int total = container.Count(def.Id);
                if (total <= 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(def.DisplayName + " " + total);
                int bad = container.CountExpired(def.Id);
                if (bad > 0) sb.Append(" <color=#FF6B5A>(" + bad + " basi)</color>");
            }
            return sb.Length == 0 ? "(kosong)" : sb.ToString();
        }
    }
}
