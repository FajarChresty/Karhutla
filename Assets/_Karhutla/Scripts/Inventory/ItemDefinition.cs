using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Kelompok item (dipakai untuk tampilan dan aturan kedaluwarsa).</summary>
    public enum ItemCategory { Masakan, Minuman, Bahan, Perlengkapan, Obat }

    /// <summary>
    /// Jenis masa kedaluwarsa. Lama sebenarnya (dalam sesi) diatur di FoodExpirySystem (Inspector),
    /// bukan di sini, karena GDD belum menentukan durasinya.
    /// </summary>
    public enum Perishability { Tidak, Bahan, Masakan }

    /// <summary>
    /// Data satu jenis item. Sengaja dibuat class biasa (bukan ScriptableObject) agar mudah dibaca pemula.
    /// Kalau nanti item sudah banyak, ubah menjadi ScriptableObject.
    /// Semua nilai (efek, harga, BERAT, nilai barter) bersifat dummy (ASUMSI PROTOTYPE) dan mudah diubah di ItemDatabase.
    /// </summary>
    public class ItemDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Price;
        public readonly Color PickupColor;
        public readonly float HungerRestore;
        public readonly float ThirstRestore;
        public readonly float FatigueRestore;
        public readonly int MaskMinutes;

        /// <summary>Berat satu unit dalam kg (NILAI SEMENTARA).</summary>
        public readonly float Weight;
        public readonly ItemCategory Category;
        public readonly Perishability Perish;
        /// <summary>Nilai tukar dasar untuk barter (poin, NILAI SEMENTARA).</summary>
        public readonly float BarterValue;
        /// <summary>Kelangkaan barang (1 = mudah didapat, makin besar makin langka). Mengalikan nilai barter. NILAI SEMENTARA.</summary>
        public readonly float Scarcity;
        /// <summary>
        /// Seberapa besar kebutuhan barang ini naik saat bencana (0 = tidak berubah, 1 = nilainya bisa naik 2x saat kondisi paling parah).
        /// Dipakai BarterTrader. NILAI SEMENTARA.
        /// </summary>
        public readonly float DisasterDemand;
        /// <summary>Pemulihan Health saat dipakai (obat).</summary>
        public readonly float HealthRestore;
        /// <summary>Penurunan Smoke Exposure saat dipakai (obat).</summary>
        public readonly float SmokeRelief;
        /// <summary>Boleh dipesan pelanggan warung.</summary>
        public readonly bool Orderable;

        public bool Usable => HungerRestore > 0f || ThirstRestore > 0f || FatigueRestore > 0f || MaskMinutes > 0 || HealthRestore > 0f || SmokeRelief > 0f;

        public ItemDefinition(string id, string displayName, string description, int price, Color color,
            float hunger = 0f, float thirst = 0f, float fatigue = 0f, int maskMinutes = 0,
            float weight = 0.5f, ItemCategory category = ItemCategory.Perlengkapan,
            Perishability perish = Perishability.Tidak, float barterValue = 1f, bool orderable = false,
            float health = 0f, float smokeRelief = 0f, float scarcity = 1f, float disasterDemand = 0f)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Price = price;
            PickupColor = color;
            HungerRestore = hunger;
            ThirstRestore = thirst;
            FatigueRestore = fatigue;
            MaskMinutes = maskMinutes;
            Weight = weight;
            Category = category;
            Perish = perish;
            BarterValue = barterValue;
            Orderable = orderable;
            HealthRestore = health;
            SmokeRelief = smokeRelief;
            Scarcity = Mathf.Max(0.1f, scarcity);
            DisasterDemand = Mathf.Max(0f, disasterDemand);
        }

        public string EffectText
        {
            get
            {
                var sb = new StringBuilder();
                if (HungerRestore > 0f) sb.Append("Lapar -" + HungerRestore + "  ");
                if (ThirstRestore > 0f) sb.Append("Haus -" + ThirstRestore + "  ");
                if (FatigueRestore > 0f) sb.Append("Lelah -" + FatigueRestore + "  ");
                if (HealthRestore > 0f) sb.Append("Kesehatan +" + HealthRestore + "  ");
                if (SmokeRelief > 0f) sb.Append("Asap -" + SmokeRelief + "  ");
                if (MaskMinutes > 0) sb.Append("Filter asap " + (MaskMinutes / 60) + " jam");
                if (sb.Length == 0) sb.Append(Category == ItemCategory.Bahan ? "Bahan masak" : Description);
                return sb.ToString().Trim();
            }
        }
    }

    /// <summary>Daftar semua item di game (dummy).</summary>
    public static class ItemDatabase
    {
        public const string Nasi = "nasi";
        public const string Air = "air";
        public const string Kopi = "kopi";
        public const string Masker = "masker";
        public const string Beras = "beras";
        public const string Sayur = "sayur";
        public const string BubukKopi = "bubuk_kopi";
        public const string Obat = "obat_ispa";
        public const string Oralit = "oralit";

        private static readonly List<ItemDefinition> all = new List<ItemDefinition>
        {
            // Masakan / minuman / perlengkapan yang dijual warung (harga = harga jual, ASUMSI V1 dipertahankan)
            new ItemDefinition(Nasi, "Nasi Bungkus", "Mengurangi rasa lapar.", 10000, new Color(0.95f, 0.85f, 0.3f), hunger: 40f,
                weight: 0.4f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 3f, orderable: true, scarcity: 1f, disasterDemand: 0.4f),
            new ItemDefinition(Air, "Air Mineral", "Mengurangi rasa haus.", 5000, new Color(0.3f, 0.6f, 0.95f), thirst: 45f,
                weight: 0.5f, category: ItemCategory.Minuman, perish: Perishability.Tidak, barterValue: 2f, orderable: true, scarcity: 1f, disasterDemand: 0.6f),
            new ItemDefinition(Kopi, "Kopi Hitam", "Mengurangi lelah.", 6000, new Color(0.35f, 0.2f, 0.1f), thirst: 5f, fatigue: 25f,
                weight: 0.3f, category: ItemCategory.Minuman, perish: Perishability.Masakan, barterValue: 2f, orderable: true, scarcity: 1f, disasterDemand: 0.1f),
            new ItemDefinition(Masker, "Masker", "Menyaring asap beberapa jam.", 8000, new Color(0.9f, 0.9f, 0.95f), maskMinutes: 180,
                weight: 0.05f, category: ItemCategory.Perlengkapan, perish: Perishability.Tidak, barterValue: 4f, orderable: true, scarcity: 1.2f, disasterDemand: 1f),

            // Bahan makanan (harga = harga beli di pasar, NILAI SEMENTARA)
            new ItemDefinition(Beras, "Beras", "Bahan nasi bungkus.", 8000, new Color(0.96f, 0.95f, 0.88f),
                weight: 2f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 5f, scarcity: 1.1f, disasterDemand: 0.5f),
            new ItemDefinition(Sayur, "Sayuran", "Bahan lauk nasi bungkus.", 5000, new Color(0.3f, 0.75f, 0.3f),
                weight: 1f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1f, disasterDemand: 0.3f),
            new ItemDefinition(BubukKopi, "Bubuk Kopi", "Bahan kopi hitam.", 6000, new Color(0.25f, 0.15f, 0.08f),
                weight: 0.5f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1f, disasterDemand: 0.1f),

            // Obat dan kebutuhan darurat. Tidak dijual di pasar dan tidak ada di peta: hanya bisa didapat lewat barter/bantuan warga
            // (itu sebabnya langka). Angka efek, berat, nilai = NILAI SEMENTARA.
            new ItemDefinition(Obat, "Obat ISPA", "Meredakan sesak akibat asap.", 15000, new Color(0.85f, 0.35f, 0.45f), weight: 0.1f,
                category: ItemCategory.Obat, perish: Perishability.Tidak, barterValue: 6f, health: 20f, smokeRelief: 25f, scarcity: 1.5f, disasterDemand: 1f),
            new ItemDefinition(Oralit, "Oralit", "Mengganti cairan dan tenaga.", 4000, new Color(0.95f, 0.75f, 0.45f), thirst: 25f, weight: 0.1f,
                category: ItemCategory.Obat, perish: Perishability.Tidak, barterValue: 3f, health: 10f, scarcity: 1.3f, disasterDemand: 0.7f)
        };

        private static readonly Dictionary<string, ItemDefinition> byId = BuildLookup();

        private static Dictionary<string, ItemDefinition> BuildLookup()
        {
            var dict = new Dictionary<string, ItemDefinition>();
            foreach (var item in all) dict[item.Id] = item;
            return dict;
        }

        public static IReadOnlyList<ItemDefinition> All => all;

        public static ItemDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            ItemDefinition def;
            return byId.TryGetValue(id, out def) ? def : null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < all.Count; i++) if (all[i].Id == id) return i;
            return int.MaxValue;
        }
    }

    /// <summary>Resep masakan di dapur (dummy, NILAI SEMENTARA).</summary>
    public class Recipe
    {
        public readonly string Name;
        public readonly string OutputId;
        public readonly int OutputCount;
        public readonly string[] IngredientIds;
        public readonly int[] IngredientCounts;

        public Recipe(string name, string outputId, int outputCount, string[] ingredientIds, int[] ingredientCounts)
        {
            Name = name;
            OutputId = outputId;
            OutputCount = outputCount;
            IngredientIds = ingredientIds;
            IngredientCounts = ingredientCounts;
        }

        public string IngredientText
        {
            get
            {
                var sb = new StringBuilder();
                for (int i = 0; i < IngredientIds.Length; i++)
                {
                    if (i > 0) sb.Append(" + ");
                    var def = ItemDatabase.Get(IngredientIds[i]);
                    sb.Append(IngredientCounts[i] + " " + (def != null ? def.DisplayName : IngredientIds[i]));
                }
                return sb.ToString();
            }
        }
    }

    public static class RecipeDatabase
    {
        public static readonly Recipe[] All =
        {
            new Recipe("Nasi Bungkus", ItemDatabase.Nasi, 3, new[] { ItemDatabase.Beras, ItemDatabase.Sayur }, new[] { 1, 1 }),
            new Recipe("Kopi Hitam", ItemDatabase.Kopi, 3, new[] { ItemDatabase.BubukKopi, ItemDatabase.Air }, new[] { 1, 1 })
        };
    }
}
