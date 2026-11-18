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
        // 5 Hidangan Awal (Menu Pembuka GDD 5.4)
        public const string Nasi = "nasi";
        public const string Kopi = "kopi";
        public const string TehHangat = "teh_hangat";
        public const string PisangGoreng = "pisang_goreng";
        public const string SayurAsem = "sayur_asem";

        // 7 Hidangan Lanjutan (Unlock Bertahap hingga 12 hidangan)
        public const string NasiGoreng = "nasi_goreng";
        public const string MieRebus = "mie_rebus";
        public const string EsJeruk = "es_jeruk";
        public const string IkanBakar = "ikan_bakar";
        public const string AyamGoreng = "ayam_goreng";
        public const string BuburKacang = "bubur_kacang";
        public const string WedangJahe = "wedang_jahe";

        // Minuman & Perlengkapan Umum
        public const string Air = "air";
        public const string AirTercemar = "air_tercemar";
        public const string Masker = "masker";

        // Bahan Baku Masakan di Pasar
        public const string Beras = "beras";
        public const string Sayur = "sayur";
        public const string BubukKopi = "bubuk_kopi";
        public const string Teh = "teh";
        public const string Pisang = "pisang";
        public const string Tepung = "tepung";
        public const string Telur = "telur";
        public const string Mie = "mie";
        public const string Jeruk = "jeruk";
        public const string Ikan = "ikan";
        public const string Ayam = "ayam";
        public const string KacangHijau = "kacang_hijau";
        public const string Gula = "gula";
        public const string Jahe = "jahe";

        // Obat dan Kebutuhan Medis Darurat
        public const string Obat = "obat_ispa";
        public const string Oralit = "oralit";

        private static readonly List<ItemDefinition> all = new List<ItemDefinition>
        {
            // === 1. LIMA HIDANGAN PEMBUKA WARUNG (GDD 5.4) ===
            new ItemDefinition(Nasi, "Nasi Bungkus", "Nasi putih lauk sayur sederhana, mengenyangkan.", 10000, new Color(0.95f, 0.85f, 0.3f),
                hunger: 40f, weight: 0.4f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 3f, orderable: true, scarcity: 1f, disasterDemand: 0.4f),
            new ItemDefinition(Kopi, "Kopi Hitam", "Seduhan kopi hitam pekat, menyegarkan lelah.", 6000, new Color(0.35f, 0.2f, 0.1f),
                thirst: 5f, fatigue: 25f, weight: 0.3f, category: ItemCategory.Minuman, perish: Perishability.Masakan, barterValue: 2f, orderable: true, scarcity: 1f, disasterDemand: 0.1f),
            new ItemDefinition(TehHangat, "Teh Hangat Manis", "Teh manis hangat meredakan haus dan penat.", 5000, new Color(0.85f, 0.5f, 0.2f),
                thirst: 30f, fatigue: 15f, weight: 0.3f, category: ItemCategory.Minuman, perish: Perishability.Masakan, barterValue: 2f, orderable: true, scarcity: 1f, disasterDemand: 0.2f),
            new ItemDefinition(PisangGoreng, "Pisang Goreng", "Gorengan pisang renyah, camilan favorit warga.", 7000, new Color(0.9f, 0.7f, 0.15f),
                hunger: 25f, fatigue: 10f, weight: 0.3f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 2.5f, orderable: true, scarcity: 1f, disasterDemand: 0.2f),
            new ItemDefinition(SayurAsem, "Sayur Asem Segar", "Kuah asam gurih segar membangkitkan selera makan.", 9000, new Color(0.6f, 0.8f, 0.3f),
                hunger: 30f, thirst: 25f, weight: 0.5f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 3f, orderable: true, scarcity: 1.1f, disasterDemand: 0.3f),

            // === 2. TUJUH HIDANGAN LANJUTAN (UNLOCK BERTAHAP) ===
            new ItemDefinition(NasiGoreng, "Nasi Goreng Kampung", "Nasi goreng bumbu desa dengan telur dadar.", 13000, new Color(0.85f, 0.6f, 0.2f),
                hunger: 55f, fatigue: 15f, weight: 0.45f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 4f, orderable: true, scarcity: 1.2f, disasterDemand: 0.4f),
            new ItemDefinition(MieRebus, "Mie Rebus Telur", "Mie kuah hangat dengan telur dan sayuran segar.", 11000, new Color(0.95f, 0.8f, 0.4f),
                hunger: 45f, thirst: 15f, weight: 0.4f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 3.5f, orderable: true, scarcity: 1.2f, disasterDemand: 0.4f),
            new ItemDefinition(EsJeruk, "Es Jeruk Segar", "Perasan jeruk asli segar melegakan dahaga terik kemarau.", 8000, new Color(1f, 0.55f, 0.1f),
                thirst: 50f, fatigue: 20f, weight: 0.35f, category: ItemCategory.Minuman, perish: Perishability.Masakan, barterValue: 2.5f, orderable: true, scarcity: 1.2f, disasterDemand: 0.5f),
            new ItemDefinition(IkanBakar, "Ikan Bakar Sambal", "Ikan rawa air tawar bakar dengan cocolan sambal terasi pedas.", 16000, new Color(0.7f, 0.35f, 0.15f),
                hunger: 65f, fatigue: 20f, weight: 0.5f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 5f, orderable: true, scarcity: 1.4f, disasterDemand: 0.5f),
            new ItemDefinition(AyamGoreng, "Ayam Goreng Lalapan", "Ayam kampung goreng gurih dengan lalapan segar.", 18000, new Color(0.8f, 0.45f, 0.2f),
                hunger: 75f, fatigue: 25f, weight: 0.55f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 5.5f, orderable: true, scarcity: 1.5f, disasterDemand: 0.6f),
            new ItemDefinition(BuburKacang, "Bubur Kacang Hijau", "Bubur manis bernutrisi tinggi penambah tenaga warga.", 8000, new Color(0.45f, 0.65f, 0.35f),
                hunger: 35f, fatigue: 30f, health: 5f, weight: 0.4f, category: ItemCategory.Masakan, perish: Perishability.Masakan, barterValue: 3f, orderable: true, scarcity: 1.2f, disasterDemand: 0.5f),
            new ItemDefinition(WedangJahe, "Wedang Jahe Herbal", "Rebusan jahe dan rempah, meredakan sesak nafas akibat kabut asap.", 10000, new Color(0.9f, 0.55f, 0.25f),
                thirst: 20f, fatigue: 35f, health: 15f, smokeRelief: 25f, weight: 0.3f, category: ItemCategory.Minuman, perish: Perishability.Masakan, barterValue: 4f, orderable: true, scarcity: 1.5f, disasterDemand: 1f),

            // === 3. LOGISTIK DAN MINUMAN UMUM ===
            new ItemDefinition(Air, "Air Mineral", "Mengurangi rasa haus.", 5000, new Color(0.3f, 0.6f, 0.95f),
                thirst: 45f, weight: 0.5f, category: ItemCategory.Minuman, perish: Perishability.Tidak, barterValue: 2f, orderable: true, scarcity: 1f, disasterDemand: 0.6f),
            new ItemDefinition(AirTercemar, "Air Tercemar Abu", "Air sumur tercemar jelaga dan abu gambut. Harus disaring/direbus di dapur warung sebelum diminum.", 1000, new Color(0.25f, 0.22f, 0.18f),
                thirst: 5f, fatigue: 15f, health: -15f, weight: 0.5f, category: ItemCategory.Minuman, perish: Perishability.Tidak, barterValue: 0.2f, orderable: false, scarcity: 1f, disasterDemand: 0f),
            new ItemDefinition(Masker, "Masker", "Menyaring asap beberapa jam.", 8000, new Color(0.9f, 0.9f, 0.95f),
                maskMinutes: 180, weight: 0.05f, category: ItemCategory.Perlengkapan, perish: Perishability.Tidak, barterValue: 4f, orderable: true, scarcity: 1.2f, disasterDemand: 1f),

            // === 4. BAHAN BAKU MASAKAN (DIJUAL DI PASAR) ===
            new ItemDefinition(Beras, "Beras", "Bahan pokok nasi bungkus & nasi goreng.", 8000, new Color(0.96f, 0.95f, 0.88f),
                weight: 2f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 5f, scarcity: 1.1f, disasterDemand: 0.5f),
            new ItemDefinition(Sayur, "Sayuran", "Bahan sayur mayur segar lokal.", 5000, new Color(0.3f, 0.75f, 0.3f),
                weight: 1f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1f, disasterDemand: 0.3f),
            new ItemDefinition(BubukKopi, "Bubuk Kopi", "Bahan kopi hitam wangi.", 6000, new Color(0.25f, 0.15f, 0.08f),
                weight: 0.5f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1f, disasterDemand: 0.1f),
            new ItemDefinition(Teh, "Teh Celup", "Bahan teh seduh hangat.", 3000, new Color(0.4f, 0.3f, 0.15f),
                weight: 0.2f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 2f, scarcity: 1f, disasterDemand: 0.2f),
            new ItemDefinition(Pisang, "Pisang Desa", "Bahan pisang goreng manis.", 4000, new Color(0.95f, 0.85f, 0.2f),
                weight: 0.8f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 2.5f, scarcity: 1f, disasterDemand: 0.2f),
            new ItemDefinition(Tepung, "Tepung Terigu", "Bahan adonan gorengan.", 4000, new Color(0.98f, 0.98f, 0.95f),
                weight: 1f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 2.5f, scarcity: 1f, disasterDemand: 0.2f),
            new ItemDefinition(Telur, "Telur Ayam", "Bahan pelengkap nasi goreng & mie rebus.", 5000, new Color(0.85f, 0.65f, 0.45f),
                weight: 0.5f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1.2f, disasterDemand: 0.3f),
            new ItemDefinition(Mie, "Mie Instan", "Bahan olahan mie rebus gurih.", 4000, new Color(0.95f, 0.9f, 0.6f),
                weight: 0.3f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 2.5f, scarcity: 1.1f, disasterDemand: 0.4f),
            new ItemDefinition(Jeruk, "Jeruk Segar", "Bahan minuman es jeruk segar.", 5000, new Color(1f, 0.6f, 0.1f),
                weight: 0.6f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1.1f, disasterDemand: 0.3f),
            new ItemDefinition(Ikan, "Ikan Rawa", "Bahan ikan rawa tangkapan nelayan.", 9000, new Color(0.4f, 0.6f, 0.7f),
                weight: 1f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 4.5f, scarcity: 1.3f, disasterDemand: 0.5f),
            new ItemDefinition(Ayam, "Daging Ayam", "Bahan ayam kampung segar.", 11000, new Color(0.9f, 0.75f, 0.7f),
                weight: 1.2f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 5f, scarcity: 1.4f, disasterDemand: 0.5f),
            new ItemDefinition(KacangHijau, "Kacang Hijau", "Bahan bubuk kacang hijau bergizi.", 5000, new Color(0.35f, 0.55f, 0.3f),
                weight: 0.8f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3f, scarcity: 1.1f, disasterDemand: 0.4f),
            new ItemDefinition(Gula, "Gula Pasir", "Bahan pemanis minuman dan bubur.", 4000, new Color(0.95f, 0.95f, 0.9f),
                weight: 0.5f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 2.5f, scarcity: 1f, disasterDemand: 0.3f),
            new ItemDefinition(Jahe, "Jahe Herbal", "Bahan rempah tradisional penghangat badan.", 6000, new Color(0.75f, 0.55f, 0.35f),
                weight: 0.4f, category: ItemCategory.Bahan, perish: Perishability.Bahan, barterValue: 3.5f, scarcity: 1.3f, disasterDemand: 0.8f),

            // === 5. OBAT MEDIS DARURAT ===
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

    /// <summary>Resep masakan di dapur (GDD 5.4: menu pembuka ~5 hingga ~12 menu bertahap).</summary>
    public class Recipe
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string OutputId;
        public readonly int OutputCount;
        public readonly string[] IngredientIds;
        public readonly int[] IngredientCounts;
        public readonly int UnlockDay; // 0 = dibuka sejak awal
        public readonly string UnlockRequirement;

        public Recipe(string id, string name, string outputId, int outputCount, string[] ingredientIds, int[] ingredientCounts, int unlockDay = 0, string unlockRequirement = "Menu Pembuka Awal")
        {
            Id = id;
            Name = name;
            OutputId = outputId;
            OutputCount = outputCount;
            IngredientIds = ingredientIds;
            IngredientCounts = ingredientCounts;
            UnlockDay = unlockDay;
            UnlockRequirement = unlockRequirement;
        }

        public Recipe(string name, string outputId, int outputCount, string[] ingredientIds, int[] ingredientCounts)
            : this("recipe_" + outputId, name, outputId, outputCount, ingredientIds, ingredientCounts, 0, "Menu Pembuka Awal")
        {
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
            // === 5 MENU PEMBUKA AWAL (GDD Bab 5.4) ===
            new Recipe("recipe_nasi", "Nasi Bungkus", ItemDatabase.Nasi, 3,
                new[] { ItemDatabase.Beras, ItemDatabase.Sayur }, new[] { 1, 1 }, 0, "Menu Pokok Awal"),

            new Recipe("recipe_kopi", "Kopi Hitam", ItemDatabase.Kopi, 3,
                new[] { ItemDatabase.BubukKopi, ItemDatabase.Air }, new[] { 1, 1 }, 0, "Minuman Pokok Awal"),

            new Recipe("recipe_teh", "Teh Hangat", ItemDatabase.TehHangat, 3,
                new[] { ItemDatabase.Teh, ItemDatabase.Air }, new[] { 1, 1 }, 0, "Minuman Segar Awal"),

            new Recipe("recipe_pisang_goreng", "Pisang Goreng", ItemDatabase.PisangGoreng, 4,
                new[] { ItemDatabase.Pisang, ItemDatabase.Tepung }, new[] { 1, 1 }, 0, "Camilan Favorit Awal"),

            new Recipe("recipe_sayur_asem", "Sayur Asem Segar", ItemDatabase.SayurAsem, 3,
                new[] { ItemDatabase.Sayur, ItemDatabase.Air }, new[] { 2, 1 }, 0, "Kuah Sayur Segar Awal"),

            // === 7 MENU LANJUTAN (BERTAMBAH HINGGA 12 MENU) ===
            new Recipe("recipe_nasi_goreng", "Nasi Goreng Kampung", ItemDatabase.NasiGoreng, 3,
                new[] { ItemDatabase.Beras, ItemDatabase.Telur }, new[] { 1, 1 }, 5, "Terbuka Hari ke-5 / Layani 5 pelanggan"),

            new Recipe("recipe_mie_rebus", "Mie Rebus Telur", ItemDatabase.MieRebus, 2,
                new[] { ItemDatabase.Mie, ItemDatabase.Telur }, new[] { 1, 1 }, 10, "Terbuka Hari ke-10 / Layani 10 pelanggan"),

            new Recipe("recipe_es_jeruk", "Es Jeruk Segar", ItemDatabase.EsJeruk, 3,
                new[] { ItemDatabase.Jeruk, ItemDatabase.Air }, new[] { 1, 1 }, 16, "Terbuka Hari ke-16 / Resep segar pasar"),

            new Recipe("recipe_ikan_bakar", "Ikan Bakar Sambal", ItemDatabase.IkanBakar, 2,
                new[] { ItemDatabase.Ikan, ItemDatabase.Sayur }, new[] { 1, 1 }, 24, "Terbuka Hari ke-24 / Hubungan baik Pak Yanto"),

            new Recipe("recipe_ayam_goreng", "Ayam Goreng Lalapan", ItemDatabase.AyamGoreng, 2,
                new[] { ItemDatabase.Ayam, ItemDatabase.Sayur }, new[] { 1, 1 }, 32, "Terbuka Hari ke-32 / Resep keluarga Bu Linda"),

            new Recipe("recipe_bubur_kacang", "Bubur Kacang Hijau", ItemDatabase.BuburKacang, 3,
                new[] { ItemDatabase.KacangHijau, ItemDatabase.Gula }, new[] { 1, 1 }, 42, "Terbuka Hari ke-42 / Kearifan Tetua Salim"),

            new Recipe("recipe_wedang_jahe", "Wedang Jahe Herbal", ItemDatabase.WedangJahe, 3,
                new[] { ItemDatabase.Jahe, ItemDatabase.Air }, new[] { 1, 1 }, 52, "Terbuka Hari ke-52 / Ramuan pemulih kabut asap"),

            // === RESEP PEMURNIAN AIR TERCEMAR (GDD Bab 8.4) ===
            new Recipe("recipe_saring_air", "Saring & Rebus Air Bersih", ItemDatabase.Air, 1,
                new[] { ItemDatabase.AirTercemar }, new[] { 1 }, 0, "Pemurnian Air Bencana")
        };

        private static readonly Dictionary<string, Recipe> byId = BuildLookup();

        private static Dictionary<string, Recipe> BuildLookup()
        {
            var dict = new Dictionary<string, Recipe>();
            foreach (var r in All) dict[r.Id] = r;
            return dict;
        }

        public static Recipe Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Recipe r;
            return byId.TryGetValue(id, out r) ? r : null;
        }
    }
}
