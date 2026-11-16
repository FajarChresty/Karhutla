using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Karhutla.EditorTools
{
    /// <summary>
    /// Membuat scene prototype lengkap secara otomatis (objek, material, sistem, referensi).
    ///   Menu: Karhutla > Build Prototype Scene
    /// Saat project pertama kali dibuka dan scene belum ada, script ini akan menawarkan untuk membuatnya.
    /// Semua objek dummy: Capsule, Plane, Cube, Cylinder, Sphere. Tidak butuh aset eksternal.
    /// </summary>
    [InitializeOnLoad]
    public static class KarhutlaSceneBuilder
    {
        private const string Root = "Assets/_Karhutla";
        private const string ScenePath = Root + "/Scenes/KarhutlaPrototype.unity";
        private const string MaterialFolder = Root + "/Materials";

        static KarhutlaSceneBuilder()
        {
            EditorApplication.delayCall += AutoPrompt;
        }

        private static void AutoPrompt()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath)) return;
            if (SessionState.GetBool("Karhutla_AskedBuild", false)) return;
            SessionState.SetBool("Karhutla_AskedBuild", true);

            bool yes = EditorUtility.DisplayDialog(
                "Karhutla Prototype",
                "Scene prototype belum dibuat.\n\nBuat otomatis sekarang? (bisa juga lewat menu Karhutla > Build Prototype Scene)",
                "Buat Sekarang", "Nanti");
            if (yes) BuildScene();
        }

        [MenuItem("Karhutla/Reset Saved Relationship Data")]
        public static void ResetSavedData()
        {
            GameSaveSystem.ClearSavedData(); // juga menghapus data hubungan sosial
            EditorUtility.DisplayDialog("Karhutla", "Data simpan permainan dan hubungan sosial (PlayerPrefs) sudah dihapus.", "OK");
        }

        [MenuItem("Karhutla/Open Prototype Scene")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                if (EditorUtility.DisplayDialog("Karhutla", "Scene belum ada. Buat sekarang?", "Buat", "Batal")) BuildScene();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Karhutla/Build Prototype Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Scene baru = permainan baru: data simpan lama (permainan + hubungan sosial) tidak cocok lagi dengan isi scene.
            GameSaveSystem.ClearSavedData();

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Karhutla/Scenes"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Karhutla/Materials"));
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- Material ----------
            Material grass = Mat("Tanah_Rumput", new Color(0.35f, 0.58f, 0.25f));
            Material wall = Mat("Bangunan_Dinding", new Color(0.85f, 0.8f, 0.7f));
            Material roof = Mat("Bangunan_Atap", new Color(0.55f, 0.25f, 0.2f));
            Material warungWall = Mat("Warung_Dinding", new Color(0.9f, 0.75f, 0.45f));
            Material warungRoof = Mat("Warung_Atap", new Color(0.75f, 0.15f, 0.1f));
            Material wood = Mat("Kayu", new Color(0.5f, 0.33f, 0.2f));
            Material sign = Mat("Papan", new Color(0.95f, 0.9f, 0.5f));
            Material trunk = Mat("Pohon_Batang", new Color(0.4f, 0.27f, 0.15f));
            Material crown = Mat("Pohon_Daun", new Color(0.13f, 0.48f, 0.15f));
            Material burning = Mat("Pohon_Terbakar", new Color(0.95f, 0.35f, 0.05f), true);
            Material burnt = Mat("Pohon_Hangus", new Color(0.07f, 0.07f, 0.07f));
            Material flame = Mat("Api", new Color(1f, 0.7f, 0.1f), true);
            Material playerMat = Mat("Pemain", new Color(0.2f, 0.5f, 0.95f));
            Material noseMat = Mat("Pemain_Arah", Color.white);
            Material npc1Mat = Mat("NPC_Warga", new Color(0.9f, 0.4f, 0.6f));
            Material npc2Mat = Mat("NPC_Relawan", new Color(0.95f, 0.6f, 0.1f));
            Material customerMat = Mat("Pelanggan", Color.white);

            // ---------- Cahaya ----------
            var lightGo = new GameObject("Directional Light");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

            // ---------- Lingkungan ----------
            var env = new GameObject("Environment").transform;

            var ground = Prim(PrimitiveType.Plane, "Tanah", env, Vector3.zero, new Vector3(12f, 1f, 12f), grass, true);
            ground.isStatic = true;

            // Dinding tak terlihat di tepi peta
            Barrier(env, "Batas_Utara", new Vector3(0f, 2f, 60.5f), new Vector3(122f, 4f, 1f));
            Barrier(env, "Batas_Selatan", new Vector3(0f, 2f, -60.5f), new Vector3(122f, 4f, 1f));
            Barrier(env, "Batas_Timur", new Vector3(60.5f, 2f, 0f), new Vector3(1f, 4f, 122f));
            Barrier(env, "Batas_Barat", new Vector3(-60.5f, 2f, 0f), new Vector3(1f, 4f, 122f));

            // Rumah-rumah (Cube)
            var houses = new GameObject("Rumah").transform;
            CreateHouse(houses, "Rumah Pak RT", new Vector2(-12f, 6f), new Vector3(6f, 3f, 5f), wall, roof,
                "Rumah Pak RT. Pintunya terkunci, beliau sedang rapat di balai desa.");
            CreateHouse(houses, "Rumah Warga", new Vector2(-14f, -9f), new Vector3(6f, 3f, 5f), wall, roof,
                "Rumah warga. Sepi, penghuninya sedang bekerja di ladang.");
            CreateHouse(houses, "Gudang Desa", new Vector2(3f, -14f), new Vector3(7f, 3f, 5f), wall, roof,
                "Gudang logistik desa. Terkunci.");

            // Bale-bale (tempat istirahat)
            var baleRoot = new GameObject("Bale-bale").transform;
            baleRoot.position = new Vector3(-6f, 0f, -6f);
            var bale = Prim(PrimitiveType.Cube, "Bale-bale Alas", baleRoot, new Vector3(0f, 0.3f, 0f), new Vector3(3f, 0.6f, 2f), wood, true);
            var rest = bale.AddComponent<RestSpot>();
            rest.promptText = "Tidur sampai besok";
            rest.interactRange = 2.5f;
            AddLabel(baleRoot, new Vector3(0f, 1.6f, 0f), "Bale-bale", Color.white, 0.8f);

            // ---------- Warung (Tahap 2: layout operasional Diner Dash) ----------
            // Area: Dapur/Persiapan (kiri), Gudang + Kulkas (kanan), Area Pelayanan (kasir), Area Pelanggan (3 meja di depan).
            var warungRoot = new GameObject("Warung").transform;
            Material metal = Mat("Logam", new Color(0.35f, 0.35f, 0.38f));
            Material white = Mat("Kulkas", new Color(0.92f, 0.95f, 0.97f));
            Material stove = Mat("Kompor", new Color(0.18f, 0.18f, 0.2f));
            Material chair = Mat("Kursi", new Color(0.6f, 0.4f, 0.25f));

            Prim(PrimitiveType.Cube, "Warung Dinding Belakang", warungRoot, new Vector3(12f, 1.5f, 10.5f), new Vector3(8f, 3f, 1f), warungWall, true);
            Prim(PrimitiveType.Cube, "Warung Atap", warungRoot, new Vector3(12f, 3.15f, 9f), new Vector3(9f, 0.3f, 5f), warungRoof, false);

            // Area pelayanan: kasir (hanya ringkasan; pembayaran terjadi di meja pelanggan)
            var counter = Prim(PrimitiveType.Cube, "Warung Kasir", warungRoot, new Vector3(12f, 0.55f, 6.4f), new Vector3(5.5f, 1.1f, 0.9f), wood, true);
            var counterInt = counter.AddComponent<WarungInteractable>();
            counterInt.mode = WarungInteractable.Mode.Counter;
            counterInt.interactRange = 3.5f;

            var signPost = Prim(PrimitiveType.Cube, "Papan Warung", warungRoot, new Vector3(7.3f, 0.7f, 4.8f), new Vector3(1.4f, 1.4f, 0.3f), sign, true);
            var signInt = signPost.AddComponent<WarungInteractable>();
            signInt.mode = WarungInteractable.Mode.Sign;
            signInt.interactRange = 2.2f;

            // Area dapur / persiapan makanan
            var kompor = Prim(PrimitiveType.Cube, "Dapur Kompor", warungRoot, new Vector3(8.8f, 0.5f, 8.8f), new Vector3(1.6f, 1f, 1.4f), stove, true);
            var kitchen = kompor.AddComponent<KitchenStation>();
            kitchen.interactRange = 2.4f;
            Prim(PrimitiveType.Cube, "Meja Persiapan", warungRoot, new Vector3(10.7f, 0.45f, 9.6f), new Vector3(1.8f, 0.9f, 0.8f), wood, true);

            // Area penyimpanan: gudang dan kulkas
            var gudang = Prim(PrimitiveType.Cube, "Gudang Warung", warungRoot, new Vector3(16.9f, 0.8f, 8.2f), new Vector3(1.2f, 1.6f, 2f), wood, true);
            var gudangInt = gudang.AddComponent<StorageInteractable>();
            gudangInt.kind = StorageKind.Gudang;
            gudangInt.interactRange = 2.4f;

            var kulkas = Prim(PrimitiveType.Cube, "Kulkas", warungRoot, new Vector3(14.9f, 1f, 9.8f), new Vector3(1f, 2f, 0.9f), white, true);
            var kulkasInt = kulkas.AddComponent<StorageInteractable>();
            kulkasInt.kind = StorageKind.Kulkas;
            kulkasInt.interactRange = 2.4f;

            AddLabel(warungRoot, new Vector3(12f, 4.6f, 7f), "WARUNG", new Color(1f, 0.9f, 0.4f), 1.6f);
            AddLabel(warungRoot, new Vector3(7.3f, 2.1f, 4.8f), "Buka/Tutup", Color.white, 0.7f);
            AddLabel(warungRoot, new Vector3(12f, 1.8f, 6.4f), "Kasir / Area Pelayanan", Color.white, 0.6f);
            AddLabel(warungRoot, new Vector3(8.8f, 1.9f, 8.8f), "Dapur (Area Persiapan)", Color.white, 0.6f);
            AddLabel(warungRoot, new Vector3(10.7f, 1.6f, 9.6f), "Meja Persiapan", Color.white, 0.5f);
            AddLabel(warungRoot, new Vector3(16.9f, 2.4f, 8.2f), "Gudang", Color.white, 0.7f);
            AddLabel(warungRoot, new Vector3(14.9f, 2.5f, 9.8f), "Kulkas", Color.white, 0.7f);

            // Area pelanggan: tiga meja (Cylinder) dengan kursi (Cube) sebagai titik duduk
            var tablesRoot = new GameObject("Area Pelanggan").transform;
            tablesRoot.SetParent(warungRoot, false);
            float[] tableX = { 6.5f, 12f, 17.5f };
            for (int i = 0; i < tableX.Length; i++)
            {
                var table = Prim(PrimitiveType.Cylinder, "Meja " + (i + 1), tablesRoot, new Vector3(tableX[i], 0.4f, 1.2f), new Vector3(1.5f, 0.4f, 1.5f), wood, true);
                var seat = new GameObject("Titik Duduk " + (i + 1)).transform;
                seat.SetParent(tablesRoot, false);
                seat.position = new Vector3(tableX[i], 0f, -0.9f);
                Prim(PrimitiveType.Cube, "Kursi " + (i + 1), tablesRoot, new Vector3(tableX[i], 0.25f, -0.4f), new Vector3(0.7f, 0.5f, 0.5f), chair, false);

                var ct = table.AddComponent<CustomerTable>();
                ct.tableNumber = i + 1;
                ct.seatPoint = seat;
                ct.interactRange = 2.4f;
                AddLabel(tablesRoot, new Vector3(tableX[i], 1.4f, 1.2f), "Meja " + (i + 1), Color.white, 0.6f);
            }

            var spawnPoint = new GameObject("TitikMunculPelanggan").transform;
            spawnPoint.SetParent(warungRoot, false);
            spawnPoint.position = new Vector3(24f, 0f, 0f);

            // ---------- Pasar (belanja bahan, memakai sesi waktu) ----------
            var pasarRoot = new GameObject("Pasar").transform;
            var pasarBase = Prim(PrimitiveType.Cube, "Lapak Pasar", pasarRoot, new Vector3(-18f, 0.6f, 2f), new Vector3(3.5f, 1.2f, 1.6f), wood, true);
            pasarBase.AddComponent<MarketStall>().interactRange = 2.6f;
            Prim(PrimitiveType.Cube, "Atap Pasar", pasarRoot, new Vector3(-18f, 3f, 2f), new Vector3(4.4f, 0.25f, 3f), warungRoof, false);
            AddLabel(pasarRoot, new Vector3(-18f, 3.9f, 2f), "PASAR", new Color(1f, 0.9f, 0.4f), 1.2f);

            // ---------- Pohon (Cylinder + Sphere) ----------
            var forest = new GameObject("Hutan").transform;
            var rng = new System.Random(1234);
            int placed = 0;
            while (placed < 110)
            {
                // Hutan utara: arah datangnya api
                CreateTree(forest, placed, new Vector3(Rnd(rng, -56f, 56f), 0f, Rnd(rng, 18f, 57f)), trunk, crown, Rnd(rng, 0.8f, 1.3f));
                placed++;
            }
            int extra = 0;
            while (extra < 35)
            {
                float x = Rnd(rng, -56f, 56f);
                float z = Rnd(rng, -56f, 56f);
                bool inVillage = x > -24f && x < 32f && z > -18f && z < 16f;
                if (inVillage || z >= 16f) continue;
                CreateTree(forest, placed + extra, new Vector3(x, 0f, z), trunk, crown, Rnd(rng, 0.8f, 1.3f));
                extra++;
            }

            // ---------- Item di dunia ----------
            var pickups = new GameObject("Item").transform;
            CreatePickup(pickups, ItemDatabase.Nasi, new Vector3(3f, 0.8f, 3f));
            CreatePickup(pickups, ItemDatabase.Nasi, new Vector3(-2f, 0.8f, -8f));
            CreatePickup(pickups, ItemDatabase.Nasi, new Vector3(9f, 0.8f, -7f));
            CreatePickup(pickups, ItemDatabase.Air, new Vector3(1f, 0.8f, 6f));
            CreatePickup(pickups, ItemDatabase.Air, new Vector3(-8f, 0.8f, 1f));
            CreatePickup(pickups, ItemDatabase.Air, new Vector3(10f, 0.8f, -4f));
            CreatePickup(pickups, ItemDatabase.Masker, new Vector3(14f, 0.8f, -3f));
            CreatePickup(pickups, ItemDatabase.Masker, new Vector3(-6f, 0.8f, -12f));
            CreatePickup(pickups, ItemDatabase.Kopi, new Vector3(5f, 0.8f, -10f));
            CreatePickup(pickups, ItemDatabase.Kopi, new Vector3(-3f, 0.8f, 3f));
            CreatePickup(pickups, ItemDatabase.Beras, new Vector3(8f, 0.8f, -2f));
            CreatePickup(pickups, ItemDatabase.Sayur, new Vector3(-5f, 0.8f, -4f));
            CreatePickup(pickups, ItemDatabase.BubukKopi, new Vector3(13f, 0.8f, -8f));

            // ---------- NPC (Capsule) ----------
            var npcs = new GameObject("NPC").transform;
            var sari = Prim(PrimitiveType.Capsule, "NPC Bu Sari", npcs, new Vector3(-4f, 1f, 10f), Vector3.one, npc1Mat, true);
            var sariNpc = sari.AddComponent<NpcController>();
            sariNpc.npcName = "Bu Sari";
            sariNpc.wanderRadius = 5f;
            sariNpc.giftItemId = ItemDatabase.Masker;
            sariNpc.normalLines = new[]
            {
                "Halo! Aku Bu Sari, tetangga warung.",
                "Musim kemarau begini tanah gambut jadi kering sekali.",
                "Kalau lihat asap dari hutan di utara, segera kabari warga ya."
            };
            sariNpc.nightLines = new[]
            {
                "Sudah malam begini masih berkeliaran?",
                "Istirahatlah di bale-bale dekat rumah warga. Besok masih banyak pekerjaan."
            };
            sariNpc.dangerLines = new[]
            {
                "Kamu lihat asap itu?! Apinya makin dekat dari utara!",
                "Ini, pakai masker. Jangan sampai sesak napas.",
                "Jauhi arah api dan jangan terlalu lelah."
            };

            sariNpc.friendlyLines = new[]
            {
                "Eh, kamu! Sudah seperti keluarga sendiri di desa ini.",
                "Kalau butuh apa-apa, bilang saja. Kita saling jaga."
            };
            // Hubungan sosial Bu Sari: tetangga ramah, sudah kenal sedikit di awal, mudah akrab. (NILAI SEMENTARA)
            sariNpc.social = new SocialProfile { startingValue = 5f, gainMultiplier = 1.1f, neglectMultiplier = 1f };
            sariNpc.coldLines = new[] { "Oh, kamu. Ada perlu apa?", "Maaf, aku lagi sibuk." };
            sariNpc.closeLines = new[]
            {
                "Nah, ini dia! Duduk dulu, aku sudah bilang kamu sudah seperti anakku sendiri.",
                "Hati-hati ya kalau keluar desa. Aku selalu cemas kalau asapnya tebal."
            };
            sariNpc.annoyedLines = new[] { "Aduh, kita baru ngobrol tadi, Nak. Aku harus menjemur dulu.", "Nanti saja kita sambung lagi." };
            sariNpc.bondingLines = new[]
            {
                "Kita duduk-duduk begini jarang sekali ya. Enak juga.",
                "Dulu desa ini hijau semua. Sekarang gambutnya kering, makanya gampang terbakar."
            };
            sariNpc.closeBondingLines = new[]
            {
                "Kamu tahu, aku simpan beberapa obat untuk keadaan darurat. Kalau kamu sampai sakit, bilang padaku.",
                "Aku senang kamu ada di desa ini."
            };
            sariNpc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Sayur };
            sariNpc.dislikedItemIds = new string[0];
            sariNpc.sharesStockInfo = true;
            sariNpc.friendGiftItemId = ItemDatabase.Air;
            sariNpc.closeGiftItemId = ItemDatabase.Masker;
            sariNpc.helpItemId = ItemDatabase.Oralit;
            sariNpc.helpItemCount = 1;
            var sariBarter = sari.AddComponent<BarterTrader>();
            sariBarter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Sayur });
            sariBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 5 });
            sariBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 4, maxStock = 6 });
            sariBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 2, maxStock = 3, minTier = RelationshipTier.Teman });
            sariBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 1, maxStock = 2, minTier = RelationshipTier.Dekat });

            var joko = Prim(PrimitiveType.Capsule, "NPC Pak Joko", npcs, new Vector3(-9f, 1f, -3f), Vector3.one, npc2Mat, true);
            var jokoNpc = joko.AddComponent<NpcController>();
            jokoNpc.npcName = "Pak Joko (Relawan)";
            jokoNpc.wanderRadius = 4f;
            jokoNpc.normalLines = new[]
            {
                "Saya relawan pemantau api di desa ini.",
                "Titik panas biasanya muncul di lahan kering saat kemarau.",
                "Warungmu ramai tidak hari ini?"
            };
            jokoNpc.nightLines = new[]
            {
                "Malam hari saya tetap berjaga. Api bisa menyebar kapan saja."
            };
            jokoNpc.dangerLines = new[]
            {
                "Api menjalar dari utara! Menjauhlah ke arah selatan, asap di sana lebih tipis.",
                "Pakai masker dan minum yang cukup. Jangan lari terus, nanti cepat lelah."
            };

            // Hubungan sosial Pak Joko: profesional, awalnya netral, hubungan lebih stabil (tidak mudah luntur). (NILAI SEMENTARA)
            jokoNpc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1f, neglectMultiplier = 0.8f };
            jokoNpc.coldLines = new[] { "Saya sedang bertugas. Mohon jangan mengganggu pemantauan." };
            jokoNpc.friendlyLines = new[]
            {
                "Oh, kamu. Senang ada warga yang peduli dengan api di sini.",
                "Kalau ada yang mencurigakan, kita saling kabari."
            };
            jokoNpc.closeLines = new[]
            {
                "Kamu sudah seperti rekan relawan bagiku.",
                "Aku percaya padamu. Kalau ada sesuatu di lapangan, kamu orang pertama yang kuhubungi."
            };
            jokoNpc.annoyedLines = new[] { "Saya harus mencatat data. Nanti kita lanjut ngobrol, ya." };
            jokoNpc.bondingLines = new[]
            {
                "Ikut aku sebentar. Titik panas biasanya muncul dari lahan gambut yang kering dan terus menyala di bawah tanah.",
                "Karena itu asap bisa muncul walau apinya tidak terlihat."
            };
            jokoNpc.closeBondingLines = new[]
            {
                "Sini kutunjukkan caranya membaca arah angin. Kalau angin dari utara, asap akan sampai ke desa lebih cepat.",
                "Kalau sudah terasa perih di mata, segera menjauh dan pakai masker."
            };
            jokoNpc.likedItemIds = new[] { ItemDatabase.Air, ItemDatabase.Nasi };
            jokoNpc.dislikedItemIds = new[] { ItemDatabase.Kopi };
            jokoNpc.sharesFireInfo = true;
            jokoNpc.friendGiftItemId = ItemDatabase.Air;
            jokoNpc.closeGiftItemId = ItemDatabase.Masker;
            jokoNpc.helpItemId = ItemDatabase.Obat;
            jokoNpc.helpItemCount = 1;
            var jokoBarter = joko.AddComponent<BarterTrader>();
            jokoBarter.acceptedItemIds.AddRange(new[] { ItemDatabase.Beras, ItemDatabase.Nasi, ItemDatabase.Air });
            jokoBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 2, maxStock = 4 });
            jokoBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 3, maxStock = 5 });
            jokoBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 2, maxStock = 4 });
            jokoBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 3, minTier = RelationshipTier.Teman });

            var warti = Prim(PrimitiveType.Capsule, "NPC Bu Warti", npcs, new Vector3(-16f, 1f, 6f), Vector3.one, npc1Mat, true);
            var wartiNpc = warti.AddComponent<NpcController>();
            wartiNpc.npcName = "Bu Warti (Petani)";
            wartiNpc.npcId = "Bu Warti";
            wartiNpc.wanderRadius = 4f;
            wartiNpc.normalLines = new[]
            {
                "Saya petani sayur dan padi di pinggir desa.",
                "Kalau musim kering, hasil panen jadi susut."
            };
            wartiNpc.nightLines = new[] { "Malam begini saya sudah mau pulang. Hati-hati di jalan." };
            wartiNpc.dangerLines = new[]
            {
                "Ladang saya terancam asap. Persediaan beras dan sayur makin menipis.",
                "Kalau kamu punya makanan atau kopi, kita bisa tukar barang."
            };
            // Hubungan sosial Bu Warti: tertutup dan hati-hati pada orang baru, jadi MULAI DINGIN (-10). Pemain harus
            // mendekatinya bertahap lewat ngobrol. Mudah merasa dilupakan. (NILAI SEMENTARA)
            wartiNpc.social = new SocialProfile { startingValue = -10f, gainMultiplier = 0.9f, neglectMultiplier = 1.2f };
            wartiNpc.coldLines = new[] { "Hm. Ada perlu apa?", "Saya tidak terlalu kenal kamu. Jangan lama-lama, ya." };
            wartiNpc.friendlyLines = new[]
            {
                "Kamu ternyata baik juga. Maaf tadi aku agak ketus.",
                "Hasil ladang makin sedikit, tapi untuk teman aku selalu sisihkan."
            };
            wartiNpc.closeLines = new[]
            {
                "Kamu keluarga di desa ini sekarang. Jangan sungkan, ya.",
                "Kalau panen berikutnya bagus, kamu yang pertama kubagi."
            };
            wartiNpc.annoyedLines = new[] { "Saya sedang kerja. Jangan terlalu sering diajak bicara, ya." };
            wartiNpc.bondingLines = new[]
            {
                "Sayur ini harus disiram pagi dan sore. Kalau asap tebal, daunnya cepat layu.",
                "Aku harap musim kering ini cepat lewat."
            };
            wartiNpc.closeBondingLines = new[]
            {
                "Dulu aku sempat kehilangan satu petak ladang karena api. Sejak itu aku lebih hati-hati.",
                "Terima kasih sudah mau mendengarkan."
            };
            wartiNpc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Air };
            wartiNpc.dislikedItemIds = new[] { ItemDatabase.Obat };
            wartiNpc.sharesStockInfo = true;
            wartiNpc.friendGiftItemId = ItemDatabase.Sayur;
            wartiNpc.closeGiftItemId = ItemDatabase.Beras;
            wartiNpc.helpItemId = ItemDatabase.Beras;
            wartiNpc.helpItemCount = 1;
            var wartiBarter = warti.AddComponent<BarterTrader>();
            wartiBarter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Air });
            wartiBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 4, maxStock = 6 });
            wartiBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.Sayur, stock = 4, maxStock = 6 });
            wartiBarter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

            // ---------- Pemain & kamera ----------
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(6f, 1f, -2f);
            player.GetComponent<Renderer>().sharedMaterial = playerMat;
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.05f;
            var playerController = player.AddComponent<PlayerController>();
            player.AddComponent<Interactor>();
            Prim(PrimitiveType.Cube, "Penunjuk Arah", player.transform, new Vector3(0f, 0.5f, 0.55f), new Vector3(0.4f, 0.25f, 0.4f), noseMat, false);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.75f, 1f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            camGo.transform.position = player.transform.position + follow.InitialOffset;
            camGo.transform.rotation = Quaternion.LookRotation(-follow.InitialOffset.normalized, Vector3.up);
            playerController.cameraTransform = camGo.transform;

            // ---------- Sistem (satu GameObject per sistem) ----------
            var systems = new GameObject("_Systems").transform;

            var timeGo = Child(systems, "TimeManager");
            timeGo.AddComponent<TimeManager>();
            var dayNight = timeGo.AddComponent<DayNightCycle>();
            dayNight.sun = sun;
            dayNight.targetCamera = cam;

            Child(systems, "Inventory").AddComponent<Inventory>();
            Child(systems, "SurvivalSystem").AddComponent<SurvivalSystem>();

            var disasterGo = Child(systems, "DisasterSystem");
            disasterGo.AddComponent<DisasterTimeline>();
            var disaster = disasterGo.AddComponent<DisasterSystem>();
            disaster.burningMaterial = burning;
            disaster.burntMaterial = burnt;
            disaster.flameMaterial = flame;

            // Warung dan sistem pendukungnya (Tahap 2)
            var warung = Child(systems, "WarungSystem").AddComponent<WarungSystem>();
            warung.spawnPoint = spawnPoint;
            warung.customerMaterial = customerMat;
            Child(systems, "WarungStorage (Gudang + Kulkas)").AddComponent<WarungStorage>();
            Child(systems, "FoodExpirySystem").AddComponent<FoodExpirySystem>();
            Child(systems, "DayReportSystem").AddComponent<DayReportSystem>();
            Child(systems, "ModalManager").AddComponent<ModalManager>();
            Child(systems, "StorageManager").AddComponent<StorageManager>();
            Child(systems, "ShopManager").AddComponent<ShopManager>();
            Child(systems, "BarterManager").AddComponent<BarterManager>();
            Child(systems, "RelationshipSystem").AddComponent<RelationshipSystem>();
            Child(systems, "GiftManager").AddComponent<GiftManager>();
            Child(systems, "GameSaveSystem").AddComponent<GameSaveSystem>();
            Child(systems, "TimeSkipSystem").AddComponent<TimeSkipSystem>();

            Child(systems, "DialogueManager").AddComponent<DialogueManager>();
            Child(systems, "TimeActivityManager").AddComponent<TimeActivityManager>();

            var uiGo = Child(systems, "UI (dibangun saat Play)");
            uiGo.AddComponent<HudUI>();
            uiGo.AddComponent<InteractionPromptUI>();
            uiGo.AddComponent<InventoryUI>();
            uiGo.AddComponent<WarungUI>();
            uiGo.AddComponent<DialogueUI>();
            uiGo.AddComponent<TimeConfirmUI>();
            uiGo.AddComponent<MenuScreenUI>();
            uiGo.AddComponent<RelationshipUI>();

            // ---------- Simpan ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;
            Debug.Log("[Karhutla] Scene prototype dibuat: " + ScenePath + ". Tekan Play untuk mencoba.");
            EditorUtility.DisplayDialog("Karhutla Prototype", "Scene berhasil dibuat:\n" + ScenePath + "\n\nTekan tombol Play untuk mencoba.", "OK");
        }

        // ================= Helper =================

        private static float Rnd(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Shader FindLitShader()
        {
            Shader s = null;
            if (GraphicsSettings.defaultRenderPipeline != null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        private static Material Mat(string name, Color color, bool emissive = false)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(FindLitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 1.5f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Barrier(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            var go = Prim(PrimitiveType.Cube, name, parent, pos, scale, null, true);
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        }

        private static void AddLabel(Transform parent, Vector3 localPos, string text, Color color, float scale)
        {
            var go = new GameObject("Label " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var label = go.AddComponent<WorldLabel>();
            label.text = text;
            label.color = color;
            label.scale = scale;
        }

        private static void CreateHouse(Transform parent, string name, Vector2 xz, Vector3 size, Material wall, Material roof, string message)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);

            var body = Prim(PrimitiveType.Cube, "Dinding", root, new Vector3(xz.x, size.y / 2f, xz.y), size, wall, true);
            var interact = body.AddComponent<SimpleInteractable>();
            interact.promptText = "Periksa " + name;
            interact.message = message;
            interact.interactRange = 2.2f;

            Prim(PrimitiveType.Cube, "Atap", root, new Vector3(xz.x, size.y + 0.15f, xz.y), new Vector3(size.x + 0.8f, 0.3f, size.z + 0.8f), roof, false);
            AddLabel(root, new Vector3(xz.x, size.y + 1.4f, xz.y), name, Color.white, 1f);
        }

        private static void CreateTree(Transform parent, int index, Vector3 pos, Material trunk, Material crown, float scale)
        {
            var root = new GameObject("Pohon " + index);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.localScale = Vector3.one * scale;

            Prim(PrimitiveType.Cylinder, "Batang", root.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.45f, 1.5f, 0.45f), trunk, true);
            Prim(PrimitiveType.Sphere, "Daun", root.transform, new Vector3(0f, 3.6f, 0f), Vector3.one * 2.8f, crown, false);

            root.AddComponent<BurnableTree>();
        }

        private static void CreatePickup(Transform parent, string itemId, Vector3 pos)
        {
            var def = ItemDatabase.Get(itemId);
            var mat = Mat("Item_" + itemId, def.PickupColor);

            var root = new GameObject("Item " + def.DisplayName);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var pickup = root.AddComponent<ItemPickup>();
            pickup.itemId = itemId;
            pickup.amount = 1;
            pickup.interactRange = 2.5f;

            GameObject visual;
            switch (itemId)
            {
                case ItemDatabase.Air:
                    visual = Prim(PrimitiveType.Cylinder, "Visual", root.transform, Vector3.zero, new Vector3(0.3f, 0.4f, 0.3f), mat, true);
                    break;
                case ItemDatabase.Masker:
                    visual = Prim(PrimitiveType.Cube, "Visual", root.transform, Vector3.zero, new Vector3(0.55f, 0.35f, 0.15f), mat, true);
                    break;
                case ItemDatabase.Kopi:
                    visual = Prim(PrimitiveType.Sphere, "Visual", root.transform, Vector3.zero, Vector3.one * 0.45f, mat, true);
                    break;
                default:
                    visual = Prim(PrimitiveType.Cube, "Visual", root.transform, Vector3.zero, Vector3.one * 0.5f, mat, true);
                    break;
            }
            var col = visual.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            AddLabel(root.transform, new Vector3(0f, 0.9f, 0f), def.DisplayName, Color.white, 0.5f);
        }
    }
}
