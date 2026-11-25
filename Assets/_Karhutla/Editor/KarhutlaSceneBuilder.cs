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
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

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
            Material metal = Mat("Logam", new Color(0.35f, 0.35f, 0.38f));
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
            Material npcPejabatMat = Mat("NPC_Pejabat", new Color(0.2f, 0.45f, 0.85f));
            Material npcMedisMat = Mat("NPC_Medis", new Color(0.2f, 0.85f, 0.7f));
            Material npcMekanikMat = Mat("NPC_Mekanik", new Color(0.55f, 0.55f, 0.6f));
            Material npcJurnalisMat = Mat("NPC_Jurnalis", new Color(0.25f, 0.7f, 0.55f));
            Material npcTetuaMat = Mat("NPC_Tetua", new Color(0.55f, 0.35f, 0.2f));
            Material npcPemudaMat = Mat("NPC_Pemuda", new Color(0.95f, 0.8f, 0.2f));
            Material npcMandorMat = Mat("NPC_Mandor", new Color(0.75f, 0.25f, 0.2f));
            Material npcNelayanMat = Mat("NPC_Nelayan", new Color(0.25f, 0.55f, 0.75f));
            Material customerMat = Mat("Pelanggan", Color.white);

            var npcMats = new System.Collections.Generic.Dictionary<string, Material>
            {
                { "warga", npc1Mat },
                { "relawan", npc2Mat },
                { "pejabat", npcPejabatMat },
                { "medis", npcMedisMat },
                { "mekanik", npcMekanikMat },
                { "jurnalis", npcJurnalisMat },
                { "tetua", npcTetuaMat },
                { "pemuda", npcPemudaMat },
                { "mandor", npcMandorMat },
                { "nelayan", npcNelayanMat }
            };

            // ---------- Cahaya ----------
            var lightGo = new GameObject("Directional Light");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

            // ---------- Lingkungan ----------
            var env = new GameObject("Environment").transform;

            // Tanah utama desa & hutan diperbesar (240m x 250m)
            var ground = Prim(PrimitiveType.Plane, "Tanah", env, new Vector3(0f, 0f, 25f), new Vector3(24f, 1f, 25f), grass, true);
            ground.isStatic = true;

            // Hamparan tanah gambut khusus di hutan utara (warna gambut gelap alami)
            Material peatSoil = Mat("Tanah_Gambut_Utara", new Color(0.18f, 0.12f, 0.08f));
            var peatGround = Prim(PrimitiveType.Plane, "Tanah_Gambut_Utara", env, new Vector3(0f, 0.02f, 65f), new Vector3(24f, 1f, 15f), peatSoil, true);
            peatGround.isStatic = true;

            // Dinding tak terlihat di tepi peta (diperluas sesuai area baru)
            Barrier(env, "Batas_Utara", new Vector3(0f, 2f, 135f), new Vector3(245f, 4f, 1f));
            Barrier(env, "Batas_Selatan", new Vector3(0f, 2f, -85f), new Vector3(245f, 4f, 1f));
            Barrier(env, "Batas_Timur", new Vector3(115f, 2f, 25f), new Vector3(1f, 4f, 225f));
            Barrier(env, "Batas_Barat", new Vector3(-115f, 2f, 25f), new Vector3(1f, 4f, 225f));

            // Rumah-rumah & Fasilitas Desa (GDD Bab 6 & 8)
            var houses = new GameObject("Rumah").transform;
            CreateHouse(houses, "Rumah Pak RT", new Vector2(-16f, 16f), new Vector3(7f, 3.2f, 6f), wall, roof,
                "Rumah Pak RT Bambang. Tempat tinggal sesepuh lingkungan warga RT 02.");
            CreateHouse(houses, "Rumah Warga", new Vector2(-30f, 14f), new Vector3(7f, 3.2f, 6f), wall, roof,
                "Rumah warga desa.");
            CreateHouse(houses, "Gudang Desa", new Vector2(6f, -18f), new Vector3(7f, 3.2f, 5.5f), wall, roof,
                "Gudang logistik dan persediaan darurat desa.");
            VillageRosterBuilder.BuildLandmarks(houses, wall, roof, wood, metal);

            // Bale-bale (tempat istirahat)
            var baleRoot = new GameObject("Bale-bale").transform;
            baleRoot.position = new Vector3(-4f, 0f, -14f);
            var bale = Prim(PrimitiveType.Cube, "Bale-bale Alas", baleRoot, new Vector3(0f, 0.3f, 0f), new Vector3(3.5f, 0.6f, 2.4f), wood, true);
            var rest = bale.AddComponent<RestSpot>();
            rest.promptText = "Tidur sampai besok";
            rest.interactRange = 2.5f;
            AddLabel(baleRoot, new Vector3(0f, 1.6f, 0f), "Bale-bale", Color.white, 0.8f);

            // ---------- Warung & Rumah Pemain (GDD Bab 3.2: layout operasional Diner Dash) ----------
            // Area: Dapur/Persiapan (kiri), Gudang + Kulkas (kanan), Area Pelayanan (kasir), Area Pelanggan (3 meja di depan).
            var warungRoot = new GameObject("Warung").transform;
            Material white = Mat("Kulkas", new Color(0.92f, 0.95f, 0.97f));
            Material stove = Mat("Kompor", new Color(0.18f, 0.18f, 0.2f));
            Material chair = Mat("Kursi", new Color(0.6f, 0.4f, 0.25f));

            // Lantai panggung kayu Warung (memberikan batas ruang yang rapi dan tertata)
            Prim(PrimitiveType.Cube, "Warung Lantai Panggung", warungRoot, new Vector3(14f, 0.05f, 6.5f), new Vector3(11f, 0.1f, 9.5f), wood, true);

            // Rumah Pemain (Menyatu di bagian belakang warung sesuai GDD Bab 3.2)
            CreateHouse(houses, "Rumah Pemain", new Vector2(14f, 16f), new Vector3(8f, 3.5f, 6f), warungWall, warungRoof,
                "Rumah tinggal pemain di belakang warung makan.");

            Prim(PrimitiveType.Cube, "Warung Dinding Belakang", warungRoot, new Vector3(14f, 1.5f, 11f), new Vector3(10f, 3f, 0.4f), warungWall, true);
            Prim(PrimitiveType.Cube, "Warung Atap", warungRoot, new Vector3(14f, 3.25f, 7f), new Vector3(11.2f, 0.3f, 8.5f), warungRoof, false);

            // Area pelayanan: kasir (hanya ringkasan; pembayaran terjadi di meja pelanggan)
            var counter = Prim(PrimitiveType.Cube, "Warung Kasir", warungRoot, new Vector3(14f, 0.6f, 6.8f), new Vector3(6f, 1.1f, 0.8f), wood, true);
            var counterInt = counter.AddComponent<WarungInteractable>();
            counterInt.mode = WarungInteractable.Mode.Counter;
            counterInt.interactRange = 3.5f;

            var signPost = Prim(PrimitiveType.Cube, "Papan Warung", warungRoot, new Vector3(10.2f, 0.8f, 6.8f), new Vector3(0.8f, 1.4f, 0.2f), sign, true);
            var signInt = signPost.AddComponent<WarungInteractable>();
            signInt.mode = WarungInteractable.Mode.Sign;
            signInt.interactRange = 2.2f;

            // Area dapur / persiapan makanan
            var kompor = Prim(PrimitiveType.Cube, "Dapur Kompor", warungRoot, new Vector3(10.5f, 0.55f, 9.5f), new Vector3(1.6f, 1f, 1.2f), stove, true);
            var kitchen = kompor.AddComponent<KitchenStation>();
            kitchen.interactRange = 2.4f;
            Prim(PrimitiveType.Cube, "Meja Persiapan", warungRoot, new Vector3(12.8f, 0.5f, 9.8f), new Vector3(1.8f, 0.9f, 0.8f), wood, true);

            // Area penyimpanan: gudang dan kulkas
            var gudang = Prim(PrimitiveType.Cube, "Gudang Warung", warungRoot, new Vector3(17.8f, 0.9f, 9.5f), new Vector3(1.6f, 1.8f, 1.6f), wood, true);
            var gudangInt = gudang.AddComponent<StorageInteractable>();
            gudangInt.kind = StorageKind.Gudang;
            gudangInt.interactRange = 2.4f;

            var kulkas = Prim(PrimitiveType.Cube, "Kulkas", warungRoot, new Vector3(15.5f, 1.1f, 9.8f), new Vector3(1f, 2.2f, 0.9f), white, true);
            var kulkasInt = kulkas.AddComponent<StorageInteractable>();
            kulkasInt.kind = StorageKind.Kulkas;
            kulkasInt.interactRange = 2.4f;

            AddLabel(warungRoot, new Vector3(14f, 4.6f, 7f), "WARUNG", new Color(1f, 0.9f, 0.4f), 1.6f);
            AddLabel(warungRoot, new Vector3(10.2f, 2.1f, 6.8f), "Buka/Tutup", Color.white, 0.7f);
            AddLabel(warungRoot, new Vector3(14f, 1.8f, 6.8f), "Kasir / Area Pelayanan", Color.white, 0.6f);
            AddLabel(warungRoot, new Vector3(10.5f, 1.9f, 9.5f), "Dapur (Area Persiapan)", Color.white, 0.6f);
            AddLabel(warungRoot, new Vector3(12.8f, 1.6f, 9.8f), "Meja Persiapan", Color.white, 0.5f);
            AddLabel(warungRoot, new Vector3(17.8f, 2.4f, 9.5f), "Gudang Bahan", Color.white, 0.7f);
            AddLabel(warungRoot, new Vector3(15.5f, 2.5f, 9.8f), "Kulkas", Color.white, 0.7f);

            // Area pelanggan: tiga meja (Cylinder) dengan kursi (Cube) sebagai titik duduk
            var tablesRoot = new GameObject("Area Pelanggan").transform;
            tablesRoot.SetParent(warungRoot, false);
            float[] tableX = { 10.5f, 14f, 17.5f };
            for (int i = 0; i < tableX.Length; i++)
            {
                var table = Prim(PrimitiveType.Cylinder, "Meja " + (i + 1), tablesRoot, new Vector3(tableX[i], 0.45f, 3.8f), new Vector3(1.5f, 0.4f, 1.5f), wood, true);
                var seat = new GameObject("Titik Duduk " + (i + 1)).transform;
                seat.SetParent(tablesRoot, false);
                seat.position = new Vector3(tableX[i], 0f, 2.5f);
                Prim(PrimitiveType.Cube, "Kursi " + (i + 1), tablesRoot, new Vector3(tableX[i], 0.25f, 2.7f), new Vector3(0.7f, 0.5f, 0.5f), chair, false);

                var ct = table.AddComponent<CustomerTable>();
                ct.tableNumber = i + 1;
                ct.seatPoint = seat;
                ct.interactRange = 2.4f;
                AddLabel(tablesRoot, new Vector3(tableX[i], 1.4f, 3.8f), "Meja " + (i + 1), Color.white, 0.6f);
            }

            var spawnPoint = new GameObject("TitikMunculPelanggan").transform;
            spawnPoint.SetParent(warungRoot, false);
            spawnPoint.position = new Vector3(22f, 0f, 3.8f);

            // ---------- Pasar (belanja bahan, memakai sesi waktu) ----------
            var pasarRoot = new GameObject("Pasar").transform;
            var pasarBase = Prim(PrimitiveType.Cube, "Lapak Pasar", pasarRoot, new Vector3(-34f, 0.6f, -4f), new Vector3(4f, 1.2f, 2f), wood, true);
            pasarBase.AddComponent<MarketStall>().interactRange = 2.6f;
            Prim(PrimitiveType.Cube, "Atap Pasar", pasarRoot, new Vector3(-34f, 3f, -4f), new Vector3(5f, 0.25f, 3.2f), warungRoof, false);
            AddLabel(pasarRoot, new Vector3(-34f, 3.9f, -4f), "PASAR", new Color(1f, 0.9f, 0.4f), 1.2f);

            // ---------- Pohon (Cylinder + Sphere) ----------
            var forest = new GameObject("Hutan").transform;
            var rng = new System.Random(1234);
            int placed = 0;
            while (placed < 160)
            {
                // Hutan utara: arah datangnya api (diperluas hingga z = 95f agar kobaran api membakar pohon di dalam daratan)
                CreateTree(forest, placed, new Vector3(Rnd(rng, -65f, 65f), 0f, Rnd(rng, 18f, 95f)), trunk, crown, Rnd(rng, 0.8f, 1.3f));
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

            // ---------- Item di dunia (Ditata rapi di atas rak/meja/lokasi logistik logis) ----------
            var pickups = new GameObject("Item").transform;
            // Di Warung & Dapur
            CreatePickup(pickups, ItemDatabase.Nasi, new Vector3(12.8f, 1.0f, 9.8f));
            CreatePickup(pickups, ItemDatabase.Air, new Vector3(15.5f, 0.6f, 8.8f));
            CreatePickup(pickups, ItemDatabase.Beras, new Vector3(17.8f, 1.2f, 9.5f));
            CreatePickup(pickups, ItemDatabase.Kopi, new Vector3(14.5f, 1.1f, 6.8f));
            CreatePickup(pickups, ItemDatabase.BubukKopi, new Vector3(11.5f, 1.0f, 9.5f));

            // Di Lapak Pasar Desa
            CreatePickup(pickups, ItemDatabase.Sayur, new Vector3(-31.2f, 1.25f, -2f));
            CreatePickup(pickups, ItemDatabase.Telur, new Vector3(-30.6f, 1.25f, -2f));
            CreatePickup(pickups, ItemDatabase.Pisang, new Vector3(-30.0f, 1.25f, -2f));
            CreatePickup(pickups, ItemDatabase.Mie, new Vector3(-29.4f, 1.25f, -2f));
            CreatePickup(pickups, ItemDatabase.Teh, new Vector3(-28.8f, 1.25f, -2f));

            // Di Posko & Fasilitas Logistik
            CreatePickup(pickups, ItemDatabase.Masker, new Vector3(24f, 1.0f, 24f));
            CreatePickup(pickups, ItemDatabase.Masker, new Vector3(-24f, 1.0f, 22f));
            CreatePickup(pickups, ItemDatabase.Air, new Vector3(36f, 0.8f, 8f));
            CreatePickup(pickups, ItemDatabase.Air, new Vector3(0f, 0.8f, 24f));
            CreatePickup(pickups, ItemDatabase.Nasi, new Vector3(-6f, 0.7f, -10f));

            // ---------- Titik Investigasi & Sekat Bakar (Sesi 4) ----------
            var storyObjects = new GameObject("Story_Objects").transform;

            // 1. Titik Investigasi Berkas Balai Desa
            var clueBalai = Prim(PrimitiveType.Cube, "Meja Arsip Izin Konsesi", storyObjects, new Vector3(0f, 0.5f, 23.5f), new Vector3(1.6f, 0.9f, 0.9f), wood, true);
            var clueSpotBalai = clueBalai.AddComponent<InvestigationClueSpot>();
            clueSpotBalai.clueId = InvestigationSystem.ClueKonsesi;
            clueSpotBalai.interactRange = 2.4f;
            AddLabel(storyObjects, new Vector3(0f, 1.4f, 23.5f), "Arsip Perizinan", Color.yellow, 0.6f);

            // 2. Titik Investigasi Peta di Pondok Ladang Gambut
            var cluePondok = Prim(PrimitiveType.Cube, "Meja Peta Ladang Gambut", storyObjects, new Vector3(-42f, 0.5f, -12f), new Vector3(1.4f, 0.8f, 0.8f), wood, true);
            var clueSpotPondok = cluePondok.AddComponent<InvestigationClueSpot>();
            clueSpotPondok.clueId = InvestigationSystem.CluePetaBakar;
            clueSpotPondok.interactRange = 2.4f;
            AddLabel(storyObjects, new Vector3(-42f, 1.3f, -12f), "Peta Titik Api", Color.yellow, 0.6f);

            // 3. Titik Sekat Bakar Perbatasan Utara (3 Sektor)
            var firebreaks = new GameObject("Sekat_Bakar_Utara").transform;
            firebreaks.SetParent(storyObjects, false);

            CreateFirebreakZone(firebreaks, "Sektor Barat", new Vector3(-24f, 0.05f, 32f), wood);
            CreateFirebreakZone(firebreaks, "Sektor Tengah", new Vector3(0f, 0.05f, 34f), wood);
            CreateFirebreakZone(firebreaks, "Sektor Timur", new Vector3(24f, 0.05f, 32f), wood);

            // ---------- Bahaya Lingkungan Tambahan (GDD Bab 8.4) ----------
            var hazardsRoot = new GameObject("Bahaya_Lingkungan").transform;

            // 1. Zona Tanah Gambut Ambles / Labil
            CreateUnstableGroundZone(hazardsRoot, "Tanah Gambut Labil Utara", new Vector3(8f, 0f, 38f), 4.5f);
            CreateUnstableGroundZone(hazardsRoot, "Tanah Gambut Labil Barat", new Vector3(-35f, 0f, -6f), 4.2f);
            CreateUnstableGroundZone(hazardsRoot, "Tanah Gambut Labil Timur", new Vector3(30f, 0f, 18f), 4.0f);

            // 2. Sumber Air Warga (Sumur Gali Desa & Sumber Air Rawa)
            CreateWaterSource(hazardsRoot, "Sumur Gali Balai Desa", new Vector3(-6f, 0f, 8f));
            CreateWaterSource(hazardsRoot, "Sumber Air Kanal Gambut", new Vector3(36f, 0f, 14f));

            // ---------- NPC Roster (15 Karakter Bernama & Jadwal - GDD Bab 6 & 8) ----------
            var npcs = new GameObject("NPC").transform;
            VillageRosterBuilder.BuildRoster(npcs, npcMats);

            // ---------- Pemain & kamera ----------
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(14f, 1f, 0f);
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

            // Pasang Prefab Karakter 2D & Animator bila tersedia
            var charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Karhutla/Prefabs/character.prefab");
            var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player/Player.controller");
            if (charPrefab != null)
            {
                var visualGo = new GameObject("CharacterVisual");
                visualGo.transform.SetParent(player.transform, false);
                visualGo.transform.localPosition = new Vector3(0f, -1.02f, 0f);
                visualGo.transform.localRotation = Quaternion.identity;

                var charInstance = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab, visualGo.transform);
                charInstance.name = "character"; // Wajib "character" karena track animasi memiliki jalur "character/Front", dsb.
                charInstance.transform.localPosition = new Vector3(0f, 0f, -0.52916f); // Kompensasi offset Z rig Alvin (+0.52916) agar tepat di titik tengah (0,0)
                charInstance.transform.localRotation = Quaternion.identity;

                // Sembunyikan kapsul dummy jika karakter 2D aktif
                var mr = player.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;

                // Blob shadow lembut kontak di bawah kaki pemain
                var sGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sGo.name = "BlobShadow";
                sGo.transform.SetParent(player.transform, false);
                sGo.transform.localPosition = new Vector3(0f, -0.992f, 0f);
                sGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                sGo.transform.localScale = new Vector3(0.95f, 0.65f, 1f);
                var col = sGo.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
                var ren = sGo.GetComponent<Renderer>();
                if (ren != null)
                {
                    ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    ren.receiveShadows = false;
                    Shader shadowShader = null;
                    if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                    {
                        shadowShader = Shader.Find("Universal Render Pipeline/Unlit");
                    }
                    if (shadowShader == null)
                    {
                        shadowShader = Shader.Find("Sprites/Default");
                    }
                    if (shadowShader == null)
                    {
                        shadowShader = Shader.Find("Unlit/Transparent");
                    }

                    if (shadowShader != null)
                    {
                        var shadowMat = new Material(shadowShader);
                        shadowMat.mainTexture = PlayerAnimator.GetOrCreateShadowTexture();
                        shadowMat.color = Color.white;
                        if (shadowMat.HasProperty("_BaseColor")) shadowMat.SetColor("_BaseColor", Color.white);
                        if (shadowMat.HasProperty("_Cull")) shadowMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        ren.sharedMaterial = shadowMat;
                    }
                }

                var anim = visualGo.AddComponent<Animator>();
                if (animController != null) anim.runtimeAnimatorController = animController;

                var pAnim = player.AddComponent<PlayerAnimator>();
                pAnim.visual = visualGo.transform;
                pAnim.controller = playerController;
            }
            else
            {
                Prim(PrimitiveType.Cube, "Penunjuk Arah", player.transform, new Vector3(0f, 0.5f, 0.55f), new Vector3(0.4f, 0.25f, 0.4f), noseMat, false);
            }

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
            var survival = Child(systems, "SurvivalSystem").AddComponent<SurvivalSystem>();
            survival.hungerPerHour = 2.5f;
            survival.thirstPerHour = 3.75f;
            survival.fatiguePerHour = 2.0f;
            survival.sprintFatiguePerSecond = 0.35f;
            survival.smokeFollowRate = 0.045f;
            survival.smokeDamageMax = 0.25f;

            var disasterGo = Child(systems, "DisasterSystem");
            disasterGo.AddComponent<DisasterTimeline>();
            var disaster = disasterGo.AddComponent<DisasterSystem>();
            disaster.fireOrigin = new Vector3(0f, 0f, 48f);
            disaster.maxRadius = 36f;
            disaster.burningMaterial = burning;
            disaster.burntMaterial = burnt;
            disaster.flameMaterial = flame;
            disasterGo.AddComponent<DisasterVisualEffects>();

            // Warung dan sistem pendukungnya (Tahap 2 & Sesi 3)
            var warung = Child(systems, "WarungSystem").AddComponent<WarungSystem>();
            warung.spawnPoint = spawnPoint;
            warung.customerMaterial = customerMat;
            warung.orderPatience = 38f;
            warung.foodPatience = 65f;
            warung.sessionDurationSeconds = 160f;
            warung.baseSpawnInterval = 14f;
            warung.eatSeconds = 5f;
            warung.worstServedSatisfaction = 45f;
            warung.demandInfluence = 0.25f;
            Child(systems, "WarungStorage (Gudang + Kulkas)").AddComponent<WarungStorage>();
            Child(systems, "FoodExpirySystem").AddComponent<FoodExpirySystem>();
            Child(systems, "DayReportSystem").AddComponent<DayReportSystem>();
            Child(systems, "ModalManager").AddComponent<ModalManager>();
            Child(systems, "StorageManager").AddComponent<StorageManager>();
            Child(systems, "ShopManager").AddComponent<ShopManager>();
            Child(systems, "BarterManager").AddComponent<BarterManager>();
            Child(systems, "RelationshipSystem").AddComponent<RelationshipSystem>();
            Child(systems, "CommunityEventManager").AddComponent<CommunityEventManager>();
            Child(systems, "GiftManager").AddComponent<GiftManager>();
            Child(systems, "GameSaveSystem").AddComponent<GameSaveSystem>();
            Child(systems, "TimeSkipSystem").AddComponent<TimeSkipSystem>();

            // Sistem Sesi 3: Resep Baru & Kebangkrutan Warung
            Child(systems, "RecipeManager").AddComponent<RecipeManager>();
            Child(systems, "BankruptcySystem").AddComponent<BankruptcySystem>();

            // Sistem Sesi 4: Penyelidikan Korupsi & Pemadaman Api / Sekat Bakar
            Child(systems, "InvestigationSystem").AddComponent<InvestigationSystem>();
            Child(systems, "FirefightingMission").AddComponent<FirefightingMission>();

            Child(systems, "DialogueManager").AddComponent<DialogueManager>();
            Child(systems, "TimeActivityManager").AddComponent<TimeActivityManager>();
            Child(systems, "PlotPointSkipManager").AddComponent<PlotPointSkipManager>();
            Child(systems, "ScriptedStorylineEventManager").AddComponent<ScriptedStorylineEventManager>();
            Child(systems, "EndingManager").AddComponent<EndingManager>();

            var uiGo = Child(systems, "UI (dibangun saat Play)");
            uiGo.AddComponent<HudUI>();
            uiGo.AddComponent<InteractionPromptUI>();
            uiGo.AddComponent<InventoryUI>();
            uiGo.AddComponent<WarungUI>();
            uiGo.AddComponent<DialogueUI>();
            uiGo.AddComponent<TimeConfirmUI>();
            uiGo.AddComponent<MenuScreenUI>();
            uiGo.AddComponent<RelationshipUI>();
            uiGo.AddComponent<EndingScreenUI>();

            // ---------- Simpan ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;
            Debug.Log("[Karhutla] Scene prototype dibuat: " + ScenePath + ". Tekan Play untuk mencoba.");
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Karhutla Prototype", "Scene berhasil dibuat:\n" + ScenePath + "\n\nTekan tombol Play untuk mencoba.", "OK");
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

        private static void CreateFirebreakZone(Transform parent, string sectorName, Vector3 pos, Material woodMat)
        {
            var zone = new GameObject("SekatBakar_" + sectorName);
            zone.transform.SetParent(parent, false);
            zone.transform.position = pos;

            // Patok kayu penanda sekat bakar
            var post = Prim(PrimitiveType.Cylinder, "Patok Penanda", zone.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 1.2f, 0.3f), woodMat, true);

            var fb = zone.AddComponent<FirebreakSpot>();
            fb.sectorName = sectorName;
            fb.requiredStages = 3;
            fb.interactRange = 3.5f;

            AddLabel(zone.transform, new Vector3(0f, 1.8f, 0f), "Sekat Bakar: " + sectorName, Color.cyan, 0.7f);
        }

        private static void CreateUnstableGroundZone(Transform parent, string name, Vector3 pos, float radius)
        {
            var zoneGo = new GameObject(name);
            zoneGo.transform.SetParent(parent, false);
            zoneGo.transform.position = pos;
            var hazard = zoneGo.AddComponent<UnstableGroundHazard>();
            hazard.zoneName = name;
            hazard.hazardRadius = radius;
        }

        private static void CreateWaterSource(Transform parent, string name, Vector3 pos)
        {
            var wellGo = new GameObject(name);
            wellGo.transform.SetParent(parent, false);
            wellGo.transform.position = pos;
            var water = wellGo.AddComponent<WaterSourceHazard>();
            water.sourceName = name;
        }
    }
}
