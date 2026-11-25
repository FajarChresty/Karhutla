using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Karhutla.EditorTools
{
    /// <summary>
    /// Pembangun Roster 15 Karakter Penting & Fasilitas Desa (GDD Bab 6 & 8).
    /// Mengonfigurasi seluruh 15 NPC lengkap dengan watak, dialog bertingkat, selera hadiah,
    /// sistem barter, serta rutinitas jadwal harian (Pagi, Siang, Malam, Darurat).
    /// </summary>
    public static class VillageRosterBuilder
    {
        public static void BuildLandmarks(Transform houses, Material wall, Material roof, Material wood, Material metal)
        {
            // Balai Desa & Kantor Kades (Sisi Utara Lapangan Desa)
            CreateStructure(houses, "Balai Desa", new Vector2(0f, 28f), new Vector3(11f, 3.8f, 8f), wall, roof,
                "Balai Desa dan Kantor Kepala Desa. Pusat koordinasi administrasi, musyawarah warga, dan posko bencana utama.");

            // Pustu (Puskesmas Pembantu - Sudut Barat Laut)
            CreateStructure(houses, "Puskesmas Pembantu (Pustu)", new Vector2(-26f, 26f), new Vector3(8f, 3.2f, 6.5f), wall, roof,
                "Pustu Desa. Fasilitas kesehatan yang menangani pencegahan ISPA, pembagian masker medis, dan pertolongan pertama.");

            // Bengkel Mesin & Pompa Kang Dedi (Sisi Tenggara)
            CreateStructure(houses, "Bengkel Pompa Kang Dedi", new Vector2(28f, -14f), new Vector3(8f, 3f, 6f), wood, metal,
                "Bengkel mesin motor dan pompa air rawa gambut milik Kang Dedi.");

            // Toko Kelontong Grosir Bu Linda (Sisi Barat Daya - Area Luas dan Mandiri)
            CreateStructure(houses, "Toko Kelontong Bu Linda", new Vector2(-24f, -16f), new Vector3(8f, 3.2f, 6f), wall, roof,
                "Toko kelontong grosir sembako desa. Menyediakan kebutuhan pangan kering dan logistik harian.");

            // Posko Relawan MPA & Menara Pantau (Sudut Timur Laut)
            CreateStructure(houses, "Posko Relawan Pemadam", new Vector2(26f, 28f), new Vector3(8f, 3.2f, 6f), wood, roof,
                "Posko siaga Masyarakat Peduli Api (MPA) dan menara pantau karhutla perbatasan utara.");

            // Pondok Ladang Gambut (Barat Daya Jauh)
            CreateStructure(houses, "Pondok Ladang Gambut", new Vector2(-44f, -16f), new Vector3(7f, 2.8f, 5.5f), wood, roof,
                "Pondok istirahat petani di perbatasan ladang gambut dan konsesi perkebunan sawit.");

            // Dermaga Kanal Rawa Pak Yanto (Sisi Timur)
            CreateStructure(houses, "Dermaga Kanal Rawa", new Vector2(38f, 10f), new Vector3(7f, 2.4f, 5f), wood, wood,
                "Dermaga perahu rawa gambut dan titik pemantauan sekat kanal air tradisional.");

            // Pondok Adat Tetua Salim (Utara Jauh Hutan Adat)
            CreateStructure(houses, "Pondok Adat Gambut", new Vector2(-36f, 38f), new Vector3(7f, 3f, 5.5f), wood, roof,
                "Pondok Tetua Adat Salim, tempat merawat pohon sialang dan kearifan kuno lahan basah gambut.");

            // Pojok Baca & Belajar Desa (Taman Terbuka Sisi Barat Lapangan)
            CreateStructure(houses, "Pojok Baca Siti", new Vector2(-14f, 4f), new Vector3(5f, 2.6f, 4f), wood, roof,
                "Pojok baca anak-anak desa yang dikelola oleh Siti.");
        }

        private static void CreateStructure(Transform parent, string name, Vector2 xz, Vector3 size, Material wall, Material roof, string message)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Dinding";
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(xz.x, size.y / 2f, xz.y);
            body.transform.localScale = size;
            body.GetComponent<Renderer>().sharedMaterial = wall;

            var interact = body.AddComponent<SimpleInteractable>();
            interact.promptText = "Periksa " + name;
            interact.message = message;
            interact.interactRange = 2.4f;

            var roofObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofObj.name = "Atap";
            roofObj.transform.SetParent(root, false);
            roofObj.transform.localPosition = new Vector3(xz.x, size.y + 0.15f, xz.y);
            roofObj.transform.localScale = new Vector3(size.x + 0.8f, 0.35f, size.z + 0.8f);
            roofObj.GetComponent<Renderer>().sharedMaterial = roof;
            Object.DestroyImmediate(roofObj.GetComponent<Collider>());

            var labelGo = new GameObject("Label " + name);
            labelGo.transform.SetParent(root, false);
            labelGo.transform.localPosition = new Vector3(xz.x, size.y + 1.4f, xz.y);
            var label = labelGo.AddComponent<WorldLabel>();
            label.text = name;
            label.color = Color.white;
            label.scale = 0.9f;
        }

        public static void BuildRoster(Transform npcs, Dictionary<string, Material> mats)
        {
            // 1. Bu Sari
            BuildBuSari(npcs, mats["warga"]);

            // 2. Pak Joko
            BuildPakJoko(npcs, mats["relawan"]);

            // 3. Bu Warti
            BuildBuWarti(npcs, mats["warga"]);

            // 4. Pak Kades Surya
            BuildPakKades(npcs, mats["pejabat"]);

            // 5. Pak RT Bambang
            BuildPakRT(npcs, mats["pejabat"]);

            // 6. Bidan Ratna
            BuildBidanRatna(npcs, mats["medis"]);

            // 7. Pak Udin
            BuildPakUdin(npcs, mats["warga"]);

            // 8. Kang Dedi
            BuildKangDedi(npcs, mats["mekanik"]);

            // 9. Mas Hendra
            BuildMasHendra(npcs, mats["jurnalis"]);

            // 10. Pak Salim
            BuildPakSalim(npcs, mats["tetua"]);

            // 11. Rian
            BuildRian(npcs, mats["pemuda"]);

            // 12. Bu Linda
            BuildBuLinda(npcs, mats["warga"]);

            // 13. Pak Tejo
            BuildPakTejo(npcs, mats["mandor"]);

            // 14. Siti
            BuildSiti(npcs, mats["pemuda"]);

            // 15. Pak Yanto
            BuildPakYanto(npcs, mats["nelayan"]);
        }

        private static GameObject SpawnNpcCapsule(Transform parent, string name, Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // =========================================================================
        // 1. BU SARI (Tetangga Warung)
        // =========================================================================
        private static void BuildBuSari(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Bu Sari", new Vector3(-4f, 1f, 10f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Bu Sari";
            npc.npcId = "Bu Sari";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = 5f, gainMultiplier = 1.1f, neglectMultiplier = 1f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Sayur };
            npc.sharesStockInfo = true;
            npc.friendGiftItemId = ItemDatabase.Air;
            npc.closeGiftItemId = ItemDatabase.Masker;
            npc.helpItemId = ItemDatabase.Oralit;
            npc.helpItemCount = 1;
            npc.giftItemId = ItemDatabase.Masker;

            npc.normalLines = new[]
            {
                "Halo! Aku Bu Sari, tetangga warung.",
                "Musim kemarau begini tanah gambut jadi kering sekali.",
                "Kalau lihat asap dari hutan di utara, segera kabari warga ya."
            };
            npc.nightLines = new[]
            {
                "Sudah malam begini masih berkeliaran?",
                "Istirahatlah di bale-bale atau warung. Besok masih banyak pekerjaan."
            };
            npc.dangerLines = new[]
            {
                "Kamu lihat asap itu?! Apinya makin dekat dari utara!",
                "Ini, pakai masker. Jangan sampai sesak napas.",
                "Jauhi arah api dan jangan terlalu lelah."
            };
            npc.friendlyLines = new[]
            {
                "Eh, kamu! Sudah seperti keluarga sendiri di desa ini.",
                "Kalau butuh apa-apa, bilang saja. Kita tetangga saling jaga."
            };
            npc.coldLines = new[] { "Oh, kamu. Ada perlu apa?", "Maaf, aku lagi sibuk menjahit." };
            npc.closeLines = new[]
            {
                "Nah, ini dia! Duduk dulu, kamu sudah kuanggap seperti anak sendiri.",
                "Hati-hati ya kalau keluar desa saat asap tebal."
            };
            npc.annoyedLines = new[] { "Aduh, kita baru ngobrol tadi, Nak. Aku harus menjemur dulu.", "Nanti saja kita sambung lagi ya." };
            npc.bondingLines = new[]
            {
                "Kita duduk-duduk begini jarang sekali ya. Enak juga.",
                "Dulu desa ini hijau semua. Sekarang gambutnya kering, makanya gampang terbakar."
            };
            npc.closeBondingLines = new[]
            {
                "Kamu tahu, aku simpan beberapa obat untuk keadaan darurat. Kalau kamu sakit, bilang padaku.",
                "Aku senang kamu ada di desa ini."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Sayur });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 4, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 2, maxStock = 3, minTier = RelationshipTier.Teman });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 1, maxStock = 2, minTier = RelationshipTier.Dekat });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Warung Makan", new Vector3(14f, 1f, 3.8f), 2.5f, "Belanja sarapan & ngobrol pagi");
            sched.SetSlot(TimeOfDay.Siang, "Rumah Bu Sari", new Vector3(-4f, 1f, 10f), 3f, "Menjahit baju dan menjemur kain");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Bu Sari", new Vector3(-4f, 1f, 10f), 1f, "Istirahat malam di rumah");
            sched.emergencyLocationName = "Dapur Umum Balai Desa";
            sched.emergencyDestination = new Vector3(0f, 1f, 24f);
            sched.emergencyActivity = "Menyiapkan konsumsi pengungsi";
        }

        // =========================================================================
        // 2. PAK JOKO (Relawan Pemadam / MPA)
        // =========================================================================
        private static void BuildPakJoko(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak Joko", new Vector3(26f, 1f, 28f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak Joko (Relawan)";
            npc.npcId = "Pak Joko";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1f, neglectMultiplier = 0.8f };
            npc.likedItemIds = new[] { ItemDatabase.Air, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Kopi };
            npc.sharesFireInfo = true;
            npc.friendGiftItemId = ItemDatabase.Air;
            npc.closeGiftItemId = ItemDatabase.Masker;
            npc.helpItemId = ItemDatabase.Obat;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Saya relawan pemantau api di desa ini.",
                "Titik panas biasanya muncul di lahan kering saat kemarau.",
                "Selalu pastikan persediaan air dan masker siap sedia."
            };
            npc.nightLines = new[] { "Malam hari saya tetap berjaga di posko. Api gambut bisa merambat di bawah tanah tanpa terlihat." };
            npc.dangerLines = new[]
            {
                "Api menjalar dari utara! Menjauhlah ke arah selatan, asap di sana lebih tipis.",
                "Pakai masker dan minum yang cukup. Jangan panik tapi tetap waspada."
            };
            npc.friendlyLines = new[]
            {
                "Senang ada warga yang peduli dengan pencegahan api.",
                "Kalau ada kepulan asap mencurigakan di batas hutan, segera beri sinyal."
            };
            npc.coldLines = new[] { "Saya sedang bertugas. Mohon jangan mengganggu pemantauan." };
            npc.closeLines = new[]
            {
                "Kamu sudah seperti rekan relawan sendiri bagiku.",
                "Aku percaya padamu. Jika garis pertahanan api terancam, kamu yang pertama kuberi tahu."
            };
            npc.annoyedLines = new[] { "Saya harus mencatat koordinat titik api. Nanti kita lanjut ngobrol, ya." };
            npc.bondingLines = new[]
            {
                "Titik panas di lahan gambut bisa menyala berminggu-minggu di bawah permukaan tanah.",
                "Karena itu pemadaman butuh suntikan air dalam jumlah besar ke dalam rongga gambut."
            };
            npc.closeBondingLines = new[]
            {
                "Sini kutunjukkan cara membaca pergerakan angin. Kalau asap mengarah ke pemukiman, segera evakuasi anak-anak dan lansia."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Beras, ItemDatabase.Nasi, ItemDatabase.Air });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 2, maxStock = 4 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 2, maxStock = 4 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 3, minTier = RelationshipTier.Teman });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Posko Relawan", new Vector3(26f, 1f, 28f), 3f, "Pengecekan selang & menara pantau");
            sched.SetSlot(TimeOfDay.Siang, "Bengkel Pompa", new Vector3(28f, 1f, -14f), 2.5f, "Uji coba mesin pompa air gambut");
            sched.SetSlot(TimeOfDay.Malam, "Pos Ronda", new Vector3(26f, 1f, 28f), 2f, "Patroli malam & siaga radio HT");
            sched.emergencyLocationName = "Garis Depan Pemadam";
            sched.emergencyDestination = new Vector3(24f, 1f, 32f);
            sched.emergencyActivity = "Memadamkan kepala api bersama tim MPA";
        }

        // =========================================================================
        // 3. BU WARTI (Petani Sayur Gambut)
        // =========================================================================
        private static void BuildBuWarti(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Bu Warti", new Vector3(-34f, 1f, -4f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Bu Warti (Petani)";
            npc.npcId = "Bu Warti";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = -10f, gainMultiplier = 0.9f, neglectMultiplier = 1.2f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Air };
            npc.dislikedItemIds = new[] { ItemDatabase.Obat };
            npc.sharesStockInfo = true;
            npc.friendGiftItemId = ItemDatabase.Sayur;
            npc.closeGiftItemId = ItemDatabase.Beras;
            npc.helpItemId = ItemDatabase.Beras;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Saya petani sayur dan padi di pinggir desa.",
                "Musim kemarau membuat parit gambut menyusut dan tanaman layu.",
                "Hasil panen kami yang menghidupi warga desa."
            };
            npc.nightLines = new[] { "Malam begini saya mau pulang beristirahat. Badan remuk sehabis mencangkul." };
            npc.dangerLines = new[]
            {
                "Ladang saya terancam terbakar! Kerja keras berbulan-bulan bisa musnah dalam hitungan jam.",
                "Jika kamu punya makanan atau air, kita bisa saling tukar."
            };
            npc.coldLines = new[] { "Hm. Ada perlu apa?", "Saya tidak kenal kamu. Jangan ganggu orang sedang bekerja." };
            npc.friendlyLines = new[]
            {
                "Kamu ternyata orang yang tulus. Maaf jika awalnya aku ketus.",
                "Hasil panen terbatas, tapi untuk sahabat aku selalu sediakan."
            };
            npc.closeLines = new[]
            {
                "Kamu sudah kuanggap keluarga sendiri di desa ini. Jangan pernah sungkan ya.",
                "Jika lumbungku masih ada beras, dapurmu tidak akan kelaparan."
            };
            npc.annoyedLines = new[] { "Saya sedang sibuk menanam. Jangan diajak bicara terus, ya." };
            npc.bondingLines = new[]
            {
                "Tanah gambut itu istimewa. Kalau dirawat dengan air yang cukup, sayuran tumbuh sangat subur.",
                "Tapi kalau dikeringkan demi perkebunan sawit raksasa, tanahnya berubah menjadi mesiu."
            };
            npc.closeBondingLines = new[]
            {
                "Dulu almarhum suamiku membuka petak ladang ini dengan cangkul tanpa membakar sedikitpun.",
                "Melihat ada anak muda sepertimu yang peduli membuat hatiku lega."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Air });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 4, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Sayur, stock = 4, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Pasar Desa", new Vector3(-34f, 1f, -4f), 2.5f, "Menjual hasil panen sayur");
            sched.SetSlot(TimeOfDay.Siang, "Ladang Gambut", new Vector3(-44f, 1f, -16f), 3.5f, "Menyiram bedengan sayur & parit");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Bu Warti", new Vector3(-30f, 1f, 14f), 1.5f, "Mengemas benih & istirahat");
            sched.emergencyLocationName = "Posko Balai Desa";
            sched.emergencyDestination = new Vector3(0f, 1f, 24f);
            sched.emergencyActivity = "Menyelamatkan bibit & pasokan pangan";
        }

        // =========================================================================
        // 4. PAK KADES SURYA (Kepala Desa)
        // =========================================================================
        private static void BuildPakKades(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak Kades", new Vector3(0f, 1f, 28f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak Kades Surya";
            npc.npcId = "Pak Kades";
            npc.wanderRadius = 2.5f;
            npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 0.8f, neglectMultiplier = 0.9f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Sayur };
            npc.friendGiftItemId = ItemDatabase.Masker;
            npc.closeGiftItemId = ItemDatabase.Beras;
            npc.helpItemId = ItemDatabase.Masker;
            npc.helpItemCount = 2;

            npc.normalLines = new[]
            {
                "Selamat datang. Selaku Kepala Desa, saya bertanggung jawab atas ketertiban desa.",
                "Musim kemarau ini membawa banyak tekanan, baik dari tuntutan investasi maupun keselamatan warga.",
                "Semoga warung keluargamu bisa berkembang baik di sini."
            };
            npc.nightLines = new[] { "Malam hari di kantor desa biasanya tenang untuk membaca arsip perizinan dan surat dinas." };
            npc.dangerLines = new[]
            {
                "Kondisi darurat telah diumumkan! Kami sedang berkoordinasi dengan BPBD dan satgas karhutla kabupaten.",
                "Semua warga harap mematuhi instruksi evakuasi jika asap melewati batas aman."
            };
            npc.friendlyLines = new[]
            {
                "Pandanganmu sangat obyektif dan membantu pemerintah desa.",
                "Saya menghargai peran warungmu sebagai simpul informasi masyarakat."
            };
            npc.coldLines = new[] { "Ada urusan birokrasi apa? Mohon isi buku tamu terlebih dahulu." };
            npc.closeLines = new[]
            {
                "Secara pribadi, saya merasa lega bisa berbicara jujur padamu tanpa tedeng aling-aling.",
                "Menjadi kades di tengah sengketa lahan gambut ini seperti berdiri di atas bara api."
            };
            npc.annoyedLines = new[] { "Banyak disposisi surat yang harus saya tanda tangani. Nanti kita bicara lagi." };
            npc.bondingLines = new[]
            {
                "Investor perkebunan menjanjikan PAD dan lapangan kerja, tapi mengeringkan kanal berisiko bencana.",
                "Pilihan seorang pemimpin tidak pernah hitam putih, Nak."
            };
            npc.closeBondingLines = new[]
            {
                "Ini rahasia antara kita: ada dokumen izin konsesi yang dipaksakan dari tingkat atas tanpa persetujuan warga.",
                "Aku butuh dukungan masyarakat jika harus menolak tekanan tersebut."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Beras, ItemDatabase.Nasi, ItemDatabase.Sayur });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Kantor Balai Desa", new Vector3(0f, 1f, 28f), 2.5f, "Memeriksa administrasi & arsip desa");
            sched.SetSlot(TimeOfDay.Siang, "Warung Makan", new Vector3(14f, 1f, 3.8f), 2f, "Makan siang & serap aspirasi warga");
            sched.SetSlot(TimeOfDay.Malam, "Balai Desa", new Vector3(0f, 1f, 28f), 1.5f, "Rapat koordinasi pimpinan");
            sched.emergencyLocationName = "Posko Komando Bencana";
            sched.emergencyDestination = new Vector3(0f, 1f, 28f);
            sched.emergencyActivity = "Memimpin koordinasi tanggap darurat";
        }

        // =========================================================================
        // 5. PAK RT BAMBANG (Ketua RT 02)
        // =========================================================================
        private static void BuildPakRT(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak RT", new Vector3(-16f, 1f, 16f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak RT Bambang";
            npc.npcId = "Pak RT";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = 10f, gainMultiplier = 1.2f, neglectMultiplier = 0.9f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
            npc.friendGiftItemId = ItemDatabase.Kopi;
            npc.closeGiftItemId = ItemDatabase.Masker;
            npc.helpItemId = ItemDatabase.Nasi;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Halo Nak! RT 02 ini warganya guyub rukun.",
                "Kalau ada masalah di lingkungan sekitar warung, kabari Pak RT saja ya.",
                "Yang penting jangan buang puntung rokok sembarangan di semak kering."
            };
            npc.nightLines = new[] { "Ronda malam menjaga keamanan desa dari pencuri dan percikan api liar." };
            npc.dangerLines = new[]
            {
                "Waspada! Asap mulai masuk ke pemukiman warga RT 02.",
                "Basahi kain atau pakai masker, tutup ventilasi rumah dari abu terbang."
            };
            npc.friendlyLines = new[]
            {
                "Kamu ini warga teladan. Warungmu selalu jadi tempat warga saling tukar kabar gembira."
            };
            npc.coldLines = new[] { "Ada laporan warga yang perlu disampaikan?" };
            npc.closeLines = new[]
            {
                "Bagi Pak RT, kamu sudah seperti keponakan sendiri. Sehat-sehat ya, Nak."
            };
            npc.annoyedLines = new[] { "Sebentar ya, Pak RT mau mencatat iuran sampah warga dulu." };
            npc.bondingLines = new[]
            {
                "Tradisi gotong royong warga desa ini sudah ada sejak kakek buyut kita.",
                "Bencana api pernah melanda puluhan tahun lalu, dan hanya kebersamaan yang menyelamatkan kita."
            };
            npc.closeBondingLines = new[]
            {
                "Selama warga bersatu dan saling peduli, desa kita pasti bisa melewati kemarau terik ini."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Sayur, ItemDatabase.Air });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Kopi, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Nasi, stock = 2, maxStock = 4 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Lingkungan Warga RT 02", new Vector3(-16f, 1f, 10f), 3.5f, "Patroli kebersihan parit & sapa warga");
            sched.SetSlot(TimeOfDay.Siang, "Balai Desa", new Vector3(0f, 1f, 28f), 2f, "Musyawarah aparatur lingkungan");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Pak RT", new Vector3(-16f, 1f, 16f), 2.5f, "Jaga pos ronda & ngopi bersama warga");
            sched.emergencyLocationName = "Posko Evakuasi Warga";
            sched.emergencyDestination = new Vector3(0f, 1f, 24f);
            sched.emergencyActivity = "Mendata lansia dan keluarga terdampak";
        }

        // =========================================================================
        // 6. BIDAN RATNA (Tenaga Medis Pustu)
        // =========================================================================
        private static void BuildBidanRatna(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Bidan Ratna", new Vector3(-26f, 1f, 26f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Bidan Ratna";
            npc.npcId = "Bidan Ratna";
            npc.wanderRadius = 2.5f;
            npc.social = new SocialProfile { startingValue = 5f, gainMultiplier = 1.1f, neglectMultiplier = 0.9f };
            npc.likedItemIds = new[] { ItemDatabase.Air, ItemDatabase.Sayur };
            npc.dislikedItemIds = new[] { ItemDatabase.Kopi };
            npc.friendGiftItemId = ItemDatabase.Oralit;
            npc.closeGiftItemId = ItemDatabase.Obat;
            npc.helpItemId = ItemDatabase.Obat;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Halo! Saya bertugas di Puskesmas Pembantu desa.",
                "Kemarau kering begini partikel debu tanah gambut mudah terhirup saluran napas.",
                "Perbanyak minum air putih hangat dan konsumsi sayuran hijau ya."
            };
            npc.nightLines = new[] { "Klinik tetap buka untuk persalinan dan panggilan darurat medis warga." };
            npc.dangerLines = new[]
            {
                "Paparan asap karhutla sangat berbahaya bagi paru-paru! Gunakan masker kain berlapis atau N95.",
                "Jika dada terasa sesak atau mata perih membakar, segera datang ke posko Pustu untuk nebulizer."
            };
            npc.friendlyLines = new[]
            {
                "Terima kasih selalu menjaga kebersihan makanan di warungmu. Itu sangat membantu mencegah wabah."
            };
            npc.coldLines = new[] { "Sedang ada antrean pasien balita. Ada keluhan darurat?" };
            npc.closeLines = new[]
            {
                "Aku sangat bersyukur punya teman yang peduli seperti kamu. Tugas medis di sini sering terasa sepi."
            };
            npc.annoyedLines = new[] { "Saya sedang mencatat rekam medis pasien. Tunggu sebentar ya." };
            npc.bondingLines = new[]
            {
                "ISPA bukan sekadar batuk biasa. Partikel PM2.5 dari gambut terbakar membawa racun karbon monoksida.",
                "Kita harus mengedukasi para orang tua agar tidak membiarkan anak-anak bermain di luar saat berasap."
            };
            npc.closeBondingLines = new[]
            {
                "Aku menyisihkan beberapa botol obat pereda sesak dan tabung oksigen cadangan untuk keluargamu jika keadaan memburuk."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Sayur, ItemDatabase.Nasi, ItemDatabase.Air });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 3, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Pustu Desa", new Vector3(-26f, 1f, 26f), 2f, "Pemeriksaan kesehatan balita & warga");
            sched.SetSlot(TimeOfDay.Siang, "Posko Relawan", new Vector3(26f, 1f, 28f), 2f, "Persiapan obat ISPA & tabung oksigen");
            sched.SetSlot(TimeOfDay.Malam, "Pustu Desa", new Vector3(-26f, 1f, 26f), 1f, "Siaga panggilan darurat medis");
            sched.emergencyLocationName = "Posko Medis Darurat";
            sched.emergencyDestination = new Vector3(-26f, 1f, 26f);
            sched.emergencyActivity = "Menangani korban sesak napas & nebulizer";
        }

        // =========================================================================
        // 7. PAK UDIN (Pedagang Pasar Tradisional)
        // =========================================================================
        private static void BuildPakUdin(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak Udin", new Vector3(-34f, 1f, -4f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak Udin (Pedagang)";
            npc.npcId = "Pak Udin";
            npc.wanderRadius = 2.5f;
            npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1.3f, neglectMultiplier = 1.3f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Air };
            npc.friendGiftItemId = ItemDatabase.BubukKopi;
            npc.closeGiftItemId = ItemDatabase.Sayur;
            npc.helpItemId = ItemDatabase.Sayur;
            npc.helpItemCount = 2;

            npc.normalLines = new[]
            {
                "Mari mampir ke lapak Udin! Sayur segar, bumbu dapur, cabai rawit komplit!",
                "Di pasar inilah urat nadi ekonomi desa berdenyut, bos!",
                "Kalau butuh pasokan bumbu buat warung, bisik-bisik ke Pak Udin saja."
            };
            npc.nightLines = new[] { "Tutup lapak, hitung laba, lalu tidur nyenyak. Besok subuh harus kulakan lagi." };
            npc.dangerLines = new[]
            {
                "Aduh kacau, kabut asap bikin jalan lintas tertutup! Truk pengangkut bawang tertahan di pelabuhan!",
                "Harga-harga bakal naik kalau stok barang tidak masuk minggu ini."
            };
            npc.friendlyLines = new[]
            {
                "Pelanggan setia datang lagi! Khusus buatmu, selalu ada potongan harga spesial!"
            };
            npc.coldLines = new[] { "Lihat-lihat dulu boleh, tapi jangan diraba-raba sayurnya nanti layu!" };
            npc.closeLines = new[]
            {
                "Kamu ini mitra dagang terbaik. Informasi apa yang ingin kamu tahu tentang kondisi desa?"
            };
            npc.annoyedLines = new[] { "Sedang melayani ibu-ibu nawar cabai nih, nanti dulu ngobrolnya ya!" };
            npc.bondingLines = new[]
            {
                "Pedagang itu harus tahu arah angin, bukan cuma ramalan cuaca tapi arah pasar.",
                "Kalau desa tetangga gagal panen karena api, kita di sini yang harus siap menopang pasokan sembako."
            };
            npc.closeBondingLines = new[]
            {
                "Jujur ya, aku tahu siapa makelar yang sering mainkan harga beras di kecamatan. Nanti kubocorkan infonya padamu."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Kopi, ItemDatabase.Air, ItemDatabase.Nasi });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Sayur, stock = 4, maxStock = 7 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 3, maxStock = 5 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Pasar Desa", new Vector3(-34f, 1f, -4f), 2f, "Berdagang sembako di lapak pasar");
            sched.SetSlot(TimeOfDay.Siang, "Toko Kelontong", new Vector3(-24f, 1f, -16f), 2f, "Kulakan grosir & ngobrol stok dagang");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Warga", new Vector3(-30f, 1f, 14f), 1f, "Menghitung omset & rekap pembukuan");
            sched.emergencyLocationName = "Gudang Logistik Desa";
            sched.emergencyDestination = new Vector3(4f, 1f, -16f);
            sched.emergencyActivity = "Membantu inventaris bantuan pangan";
        }

        // =========================================================================
        // 8. KANG DEDI (Mekanik Mesin & Ahli Pompa Air Gambut)
        // =========================================================================
        private static void BuildKangDedi(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Kang Dedi", new Vector3(28f, 1f, -14f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Kang Dedi (Bengkel)";
            npc.npcId = "Kang Dedi";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1.0f, neglectMultiplier = 0.8f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Sayur };
            npc.friendGiftItemId = ItemDatabase.Air;
            npc.closeGiftItemId = ItemDatabase.Masker;
            npc.helpItemId = ItemDatabase.Masker;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Ada yang rusak? Bawa ke bengkel Dedi, dari karburator motor sampai mesin diesel sawah!",
                "Sekarang lagi musim servis pompa air. Banyak petani butuh sedot air dari kanal gambut."
            };
            npc.nightLines = new[] { "Tangan belepotan oli begini enaknya cuci tangan lalu seruput kopi hitam panas di bale-bale." };
            npc.dangerLines = new[]
            {
                "Mesin pompa pemadam MPA jebol tersumbat lumpur! Aku harus ganti seal dan pasang kawat saring darurat!",
                "Tanpa pompa air tekanan tinggi, mustahil menembus gambut yang terbakar di kedalaman 2 meter!"
            };
            npc.friendlyLines = new[]
            {
                "Kopimu di warung itu bahan bakar terbaik buat kerja bengkel, Sobat!"
            };
            npc.coldLines = new[] { "Sedang setel klep mesin. Jangan banyak tanya dulu ya." };
            npc.closeLines = new[]
            {
                "Kalau butuh perkakas, baut, atau selang tekanan tinggi, ambil saja di bengkelku. Kamu kawan sejati."
            };
            npc.annoyedLines = new[] { "Aduh, obengku terpental gara-gara kaget. Tunggu beres ya." };
            npc.bondingLines = new[]
            {
                "Memadamkan api gambut itu ilmunya beda dengan kebakaran rumah. Air harus disuntikkan langsung ke dalam rongga gambut basah.",
                "Aku sedang merancang nozel pipa semprot jinjing agar relawan bisa memadamkan bara dari jarak aman."
            };
            npc.closeBondingLines = new[]
            {
                "Nozel purwarupa buatanku sudah jadi dan bekerja luar biasa! Ini berkat kamu yang selalu menyemangatiku."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Air });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 3, maxStock = 5 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Bengkel Mesin", new Vector3(28f, 1f, -14f), 3f, "Servis motor warga & mesin diesel");
            sched.SetSlot(TimeOfDay.Siang, "Dermaga Pompa", new Vector3(38f, 1f, 10f), 2.5f, "Merakit mesin pompa air jinjing");
            sched.SetSlot(TimeOfDay.Malam, "Bale-bale Desa", new Vector3(-4f, 1f, -14f), 2f, "Minum kopi hitam & santai malam");
            sched.emergencyLocationName = "Posko Damkar Relawan";
            sched.emergencyDestination = new Vector3(26f, 1f, 28f);
            sched.emergencyActivity = "Perawatan cepat pompa air pemadam";
        }

        // =========================================================================
        // 9. MAS HENDRA (Jurnalis Lingkungan & Peneliti)
        // =========================================================================
        private static void BuildMasHendra(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Mas Hendra", new Vector3(0f, 1f, 20f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Mas Hendra (Jurnalis)";
            npc.npcId = "Mas Hendra";
            npc.wanderRadius = 3.5f;
            npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 1.1f, neglectMultiplier = 1.1f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Air };
            npc.sharesFireInfo = true;
            npc.friendGiftItemId = ItemDatabase.Masker;
            npc.closeGiftItemId = ItemDatabase.Obat;
            npc.helpItemId = ItemDatabase.Oralit;
            npc.helpItemCount = 2;

            npc.normalLines = new[]
            {
                "Saya jurnalis investigasi independen. Sedang meneliti alih fungsi lahan gambut di wilayah ini.",
                "Karhutla bukan bencana alam murni. 99% kebakaran lahan di Indonesia dipicu aktivitas manusia.",
                "Banyak konsesi perkebunan sengaja mengeringkan kanal gambut agar lahan mudah dibersihkan dengan api murah."
            };
            npc.nightLines = new[] { "Mengetik laporan dan mengunggah data satelit hotspot butuh sinyal yang lumayan kuat di balai desa." };
            npc.dangerLines = new[]
            {
                "Titik api baru terpantau serentak di batas konsesi! Ini pola terencana, bukan sekadar ketidaksengajaan!",
                "Gunakan masker pelindung respirator. Partikel asap ini membawa sulfur dan tar beracun."
            };
            npc.friendlyLines = new[]
            {
                "Warungmu tempat strategis mendengarkan suara riil warga desa tanpa sensor kekuasaan."
            };
            npc.coldLines = new[] { "Maaf, saya sedang memverifikasi koordinat GPS foto udara. Mohon jangan mengganggu." };
            npc.closeLines = new[]
            {
                "Aku percaya padamu. Jika hasil investigasi ini dipublikasikan, dalang pembakar lahan bisa diseret ke meja hijau."
            };
            npc.annoyedLines = new[] { "Baterai kameraku kritis, harus segera mengecas data laporan." };
            npc.bondingLines = new[]
            {
                "Satelit pemantau MODIS dan VIIRS menunjukkan lonjakan titik api setiap musim tanam baru korporasi.",
                "Masyarakat adat dan warga desa kecil yang selalu menjadi korban sesak napas dan kehilangan tanah."
            };
            npc.closeBondingLines = new[]
            {
                "Aku sudah mengamankan salinan dokumen izin analisis dampak lingkungan (AMDAL) palsu. Bukti ini sangat kuat!"
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Air, ItemDatabase.Kopi, ItemDatabase.Nasi });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Batas Hutan Gambut", new Vector3(0f, 1f, 32f), 4f, "Dokumentasi titik panas & kanal air");
            sched.SetSlot(TimeOfDay.Siang, "Warung Makan", new Vector3(14f, 1f, 3.8f), 2f, "Makan siang & menulis catatan liputan");
            sched.SetSlot(TimeOfDay.Malam, "Pojok Baca Siti", new Vector3(-14f, 1f, 4f), 2.5f, "Wawancara narasumber investigasi");
            sched.emergencyLocationName = "Pusat Media Informasi";
            sched.emergencyDestination = new Vector3(0f, 1f, 28f);
            sched.emergencyActivity = "Rilis berita darurat kabut asap";
        }

        // =========================================================================
        // 10. PAK SALIM (Tetua Adat Gambut)
        // =========================================================================
        private static void BuildPakSalim(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak Salim", new Vector3(-36f, 1f, 38f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak Salim (Tetua Adat)";
            npc.npcId = "Pak Salim";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 0.7f, neglectMultiplier = 0.7f };
            npc.likedItemIds = new[] { ItemDatabase.Air, ItemDatabase.Beras };
            npc.dislikedItemIds = new[] { ItemDatabase.BubukKopi };
            npc.friendGiftItemId = ItemDatabase.Obat;
            npc.closeGiftItemId = ItemDatabase.Beras;
            npc.helpItemId = ItemDatabase.Obat;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Gambut bukan sekadar tanah hitam, Nak. Gambut adalah spons raksasa penjaga napas bumi.",
                "Pohon sialang tempat lebah bersarang tidak boleh ditebang, tanah rawa tidak boleh dikeringkan.",
                "Nenek moyang kita hidup berdampingan dengan hutan rawa ratusan tahun tanpa pernah membakarnya."
            };
            npc.nightLines = new[] { "Mendengarkan suara jangkrik dan gemerisik daun rawa gambut menenangkan jiwa." };
            npc.dangerLines = new[]
            {
                "Ketika bumi pertiwi menangis dalam kobaran api, kabut asap adalah kutukan akibat keserakahan manusia.",
                "Minum rebusan akar kayu bajakah dan tetaplah tenang. Jaga anak cucu di tempat terlindung."
            };
            npc.friendlyLines = new[]
            {
                "Hatimu jernih bagaikan air kanal alami yang mengalir tenang. Tetaplah rendah hati."
            };
            npc.coldLines = new[] { "Angin membawa aroma kegelisahan. Jangan ganggu doa dan renungan tetua." };
            npc.closeLines = new[]
            {
                "Kearifan tanah gambut ini kutitipkan kepadamu, anak muda. Jadilah pelindung bagi yang lemah."
            };
            npc.annoyedLines = new[] { "Kata-kata yang terlalu banyak akan mengusir kedamaian hutan." };
            npc.bondingLines = new[]
            {
                "Di dalam kubah gambut, tersimpan air purba jutaan kubik. Jika kanal dibuka tanpa sekat, air mengalir habis ke laut.",
                "Saat kering, gambut akan terbakar dari bawah hingga pohon-pohon besar tumbang tanpa disadari."
            };
            npc.closeBondingLines = new[]
            {
                "Ini ramuan tradisional pereda dada sesak peninggalan leluhur. Simpan baik-baik untuk saat tergenting."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Air, ItemDatabase.Beras, ItemDatabase.Nasi });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 4 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Pondok Hutan Adat", new Vector3(-36f, 1f, 38f), 3f, "Meracik jamu herbal akar gambut");
            sched.SetSlot(TimeOfDay.Siang, "Dermaga Kanal Rawa", new Vector3(38f, 1f, 10f), 3f, "Memeriksa kelembapan kubah rawa");
            sched.SetSlot(TimeOfDay.Malam, "Pondok Hutan Adat", new Vector3(-36f, 1f, 38f), 1f, "Doa keselamatan alam & istirahat");
            sched.emergencyLocationName = "Balai Adat Desa";
            sched.emergencyDestination = new Vector3(-36f, 1f, 38f);
            sched.emergencyActivity = "Membimbing warga doa bersama & mitigasi";
        }

        // =========================================================================
        // 11. RIAN (Pemuda Desa)
        // =========================================================================
        private static void BuildRian(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Rian", new Vector3(-4f, 1f, -14f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Rian (Pemuda Desa)";
            npc.npcId = "Rian";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1.2f, neglectMultiplier = 1.2f };
            npc.likedItemIds = new[] { ItemDatabase.Nasi, ItemDatabase.Kopi };
            npc.dislikedItemIds = new[] { ItemDatabase.Sayur };
            npc.friendGiftItemId = ItemDatabase.Air;
            npc.closeGiftItemId = ItemDatabase.BubukKopi;
            npc.helpItemId = ItemDatabase.Nasi;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Nongkrong di bale-bale sambil menunggu lowongan buruh harian.",
                "Cari kerja di desa sekarang susah, semuanya serba modal dan orang dalam.",
                "Kadang ada orang asing nawari uang cepat buat babat semak belukar."
            };
            npc.nightLines = new[] { "Gitaran di pos ronda sambil begadang bareng anak-anak muda." };
            npc.dangerLines = new[]
            {
                "Aduh... asapnya bikin mata perih banget! Aku menyesal kalau ingat omongan orang yang nawari bakar lahan kemarin.",
                "Untung aku tidak tergiur uang haram itu!"
            };
            npc.friendlyLines = new[]
            {
                "Kamu asyik diajak ngobrol. Tidak seperti orang tua lain yang sukanya cuma menceramahi."
            };
            npc.coldLines = new[] { "Lagi suntuk nih. Jangan diajak ribut dulu." };
            npc.closeLines = new[]
            {
                "Terima kasih sudah membimbingku, Bos. Kalau bukan karena kamu, mungkin aku sudah tersesat jalan."
            };
            npc.annoyedLines = new[] { "Lagi mikir jalan keluar hidup nih, nanti aja ngobrolnya." };
            npc.bondingLines = new[]
            {
                "Banyak teman seusiaku yang merantau ke kota atau terjebak jadi buruh liar tanpa masa depan.",
                "Aku sebenarnya ingin belajar berkebun organik atau punya usaha sendiri di kampung halaman."
            };
            npc.closeBondingLines = new[]
            {
                "Mulai besok aku ikut Pak Joko daftar jadi relawan pemadam api. Aku ingin berguna buat desa kita!"
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Sayur });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 2, maxStock = 4 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Bale-bale Desa", new Vector3(-4f, 1f, -14f), 3f, "Duduk mencari peluang kerja harian");
            sched.SetSlot(TimeOfDay.Siang, "Warung Kopi", new Vector3(14f, 1f, 3.8f), 2f, "Bantu angkat galon air di warung");
            sched.SetSlot(TimeOfDay.Malam, "Posko Relawan", new Vector3(26f, 1f, 28f), 2.5f, "Main gitar & kumpul pemuda");
            sched.emergencyLocationName = "Posko Relawan Pemadam";
            sched.emergencyDestination = new Vector3(26f, 1f, 28f);
            sched.emergencyActivity = "Membantu angkut selang pemadam api";
        }

        // =========================================================================
        // 12. BU LINDA (Pemilik Toko Kelontong Grosir)
        // =========================================================================
        private static void BuildBuLinda(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Bu Linda", new Vector3(-24f, 1f, -16f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Bu Linda (Toko Sembako)";
            npc.npcId = "Bu Linda";
            npc.wanderRadius = 2.5f;
            npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 0.9f, neglectMultiplier = 1.1f };
            npc.likedItemIds = new[] { ItemDatabase.Beras, ItemDatabase.BubukKopi };
            npc.dislikedItemIds = new[] { ItemDatabase.Sayur };
            npc.sharesStockInfo = true;
            npc.friendGiftItemId = ItemDatabase.Oralit;
            npc.closeGiftItemId = ItemDatabase.Beras;
            npc.helpItemId = ItemDatabase.Beras;
            npc.helpItemCount = 2;

            npc.normalLines = new[]
            {
                "Toko Kelontong Linda menyediakan sembako grosir dan eceran.",
                "Penyimpanan bahan pokok harus kering dan terlindung dari rayap tanah gambut.",
                "Kalau butuh karungan beras atau gula pasir buat stok warung, langsung order ke sini."
            };
            npc.nightLines = new[] { "Menutup rolling door toko dan menghitung pembukuan inventaris gudang." };
            npc.dangerLines = new[]
            {
                "Distribusi logistik darat lumpuh total akibat jarak pandang kabut asap di bawah 50 meter!",
                "Stok beras dan mie instan di gudangku tinggal cadangan darurat untuk warga desa."
            };
            npc.friendlyLines = new[]
            {
                "Kamu mitra dagang yang amanah. Pembayaran selalu lancar dan teratur."
            };
            npc.coldLines = new[] { "Beli apa? Jangan berlama-lama di depan kasir kalau tidak jadi belanja." };
            npc.closeLines = new[]
            {
                "Aku simpan cadangan beras khusus untuk warungmu. Apapun yang terjadi, kita jaga pasokan warga bersama."
            };
            npc.annoyedLines = new[] { "Sedang memeriksa faktur pengiriman barang grosir. Nanti ya." };
            npc.bondingLines = new[]
            {
                "Membuka toko kelontong di daerah terpencil ini resikonya tinggi sekali.",
                "Tapi kalau bukan kita yang berani menyetok sembako, warga desa mau beli garam dan beras ke mana?"
            };
            npc.closeBondingLines = new[]
            {
                "Aku sudah memutuskan: jika bencana asap memburuk, gudangku dibuka untuk lumbung pangan dapur umum cuma-cuma."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Sayur });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 5, maxStock = 8 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 4, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Toko Kelontong", new Vector3(-24f, 1f, -16f), 2f, "Mengecek stok beras & minyak grosir");
            sched.SetSlot(TimeOfDay.Siang, "Toko Kelontong", new Vector3(-24f, 1f, -16f), 2f, "Melayani pembeli sembako desa");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Toko", new Vector3(-24f, 1f, -16f), 1f, "Rekap pembukuan kas & tutup toko");
            sched.emergencyLocationName = "Gudang Sembako Darurat";
            sched.emergencyDestination = new Vector3(-24f, 1f, -16f);
            sched.emergencyActivity = "Membagikan ransum darurat beras";
        }

        // =========================================================================
        // 13. PAK TEJO (Mandor Perkebunan Sawit Perbatasan)
        // =========================================================================
        private static void BuildPakTejo(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak Tejo", new Vector3(-44f, 1f, -16f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak Tejo (Mandor Perkebunan)";
            npc.npcId = "Pak Tejo";
            npc.wanderRadius = 3.5f;
            npc.social = new SocialProfile { startingValue = -15f, gainMultiplier = 0.7f, neglectMultiplier = 1.0f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Obat };
            npc.friendGiftItemId = ItemDatabase.Air;
            npc.closeGiftItemId = ItemDatabase.Beras;
            npc.helpItemId = ItemDatabase.Beras;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Saya mandor lapangan perkebunan sawit. Tugas saya memastikan target panen dan jalur angkut lancar.",
                "Orang-orang sering menyalahkan perusahaan setiap ada asap, padahal tidak semua api berasal dari kebun kami.",
                "Kami juga mempekerjakan puluhan buruh harian dari desa ini."
            };
            npc.nightLines = new[] { "Malam hari saya mengawasi pos keamanan traktor dan gudang pupuk perkebunan." };
            npc.dangerLines = new[]
            {
                "Api liar dari semak belukar mulai merembet ke blok tanaman sawit produktif!",
                "Manajemen sudah kerahkan water cannon dan tangki tanggap darurat perusahaan."
            };
            npc.friendlyLines = new[]
            {
                "Kamu mau mendengar penjelasan kami secara adil, tidak seperti aktivis luar yang cuma menghakimi."
            };
            npc.coldLines = new[] { "Dilarang mengambil foto di area batas konsesi! Ada urusan apa?" };
            npc.closeLines = new[]
            {
                "Keluarga saya tinggal di desa ini juga. Saya tidak pernah ingin melihat tanah kelahiran saya terbakar habis."
            };
            npc.annoyedLines = new[] { "Saya harus mengisi absensi kerja puluhan buruh sawit. Jangan diganggu." };
            npc.bondingLines = new[]
            {
                "Pimpinan di Jakarta hanya menuntut target tonase tandan buah segar, mereka tidak tahu panasnya berada di lahan gambut.",
                "Terkadang instruksi pembuatan kanal baru dari kantor pusat bertabrakan dengan kepentingan air desa."
            };
            npc.closeBondingLines = new[]
            {
                "Aku sudah putuskan: sekat kanal air di batas desa tidak akan kubongkar, apapun ancaman dari manajemen!"
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Kopi, ItemDatabase.Nasi });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 4, maxStock = 6 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Batas Lahan Sawit", new Vector3(-44f, 1f, -16f), 3.5f, "Mengawasi kerja buruh & traktor kebun");
            sched.SetSlot(TimeOfDay.Siang, "Balai Desa", new Vector3(0f, 1f, 28f), 2f, "Koordinasi batas lahan dengan Kades");
            sched.SetSlot(TimeOfDay.Malam, "Pondok Ladang Gambut", new Vector3(-44f, 1f, -16f), 1f, "Istirahat malam bersama keluarga");
            sched.emergencyLocationName = "Posko Evakuasi Balai Desa";
            sched.emergencyDestination = new Vector3(0f, 1f, 28f);
            sched.emergencyActivity = "Menyalurkan bantuan tangki air kebun";
        }

        // =========================================================================
        // 14. SITI (Siswi Cerdas & Relawan Anak Posko)
        // =========================================================================
        private static void BuildSiti(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Siti", new Vector3(-14f, 1f, 4f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Siti (Pelajar)";
            npc.npcId = "Siti";
            npc.wanderRadius = 2.5f;
            npc.social = new SocialProfile { startingValue = 10f, gainMultiplier = 1.3f, neglectMultiplier = 1.0f };
            npc.likedItemIds = new[] { ItemDatabase.Air, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Kopi };
            npc.friendGiftItemId = ItemDatabase.Masker;
            npc.closeGiftItemId = ItemDatabase.Oralit;
            npc.helpItemId = ItemDatabase.Masker;
            npc.helpItemCount = 1;

            npc.normalLines = new[]
            {
                "Halo Kak! Aku Siti, lagi merapikan buku di pojok baca desa.",
                "Aku suka membaca tentang sains, alam, dan ekosistem lahan basah.",
                "Semoga tahun depan aku bisa dapat beasiswa kuliah pertanian di kota!"
            };
            npc.nightLines = new[] { "Malam hari di rumah biasanya aku belajar dengan lampu minyak sambil mendengarkan radio." };
            npc.dangerLines = new[]
            {
                "Sekolah kami terpaksa diliburkan karena indeks standar pencemar udara (ISPU) masuk zona hitam berbahaya!",
                "Adik-adik kecil di posko batuk-batuk. Aku bacakan dongeng agar mereka tidak takut pada sirene pemadam."
            };
            npc.friendlyLines = new[]
            {
                "Kakak ramah sekali! Warung Kakak tempat ternyaman buat baca buku sore hari."
            };
            npc.coldLines = new[] { "Maaf, aku sedang mengerjakan PR matematika sekolah." };
            npc.closeLines = new[]
            {
                "Kakak seperti sosok panutan bagiku. Aku ingin punya warung dan usaha mandiri seperti Kakak kelak."
            };
            npc.annoyedLines = new[] { "Sedang menghafal bab biologi nih, nanti dulu ya Kak!" };
            npc.bondingLines = new[]
            {
                "Tahukah Kakak? Lahan gambut Indonesia menyimpan karbon dua kali lipat lebih banyak daripada seluruh hutan hujan dunia!",
                "Kalau gambut terbakar, karbon jutaan ton terlepas ke atmosfer dan mempercepat pemanasan global."
            };
            npc.closeBondingLines = new[]
            {
                "Aku membuat poster edukasi bahaya kebakaran lahan bareng anak-anak posko. Ayo Kak kita tempel di warung!"
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Sayur });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 2, maxStock = 4 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 2, maxStock = 4 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Pojok Baca", new Vector3(-14f, 1f, 4f), 2f, "Merapikan perpustakaan & belajar");
            sched.SetSlot(TimeOfDay.Siang, "Warung Makan", new Vector3(14f, 1f, 3.8f), 2f, "Makan siang & bantu sapu teras");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Warga", new Vector3(-30f, 1f, 14f), 1f, "Mengerjakan tugas sekolah di rumah");
            sched.emergencyLocationName = "Ruang Anak Posko";
            sched.emergencyDestination = new Vector3(0f, 1f, 24f);
            sched.emergencyActivity = "Pendampingan psikososial anak-anak";
        }

        // =========================================================================
        // 15. PAK YANTO (Nelayan Rawa & Pawang Perahu Kanal)
        // =========================================================================
        private static void BuildPakYanto(Transform root, Material mat)
        {
            var go = SpawnNpcCapsule(root, "NPC Pak Yanto", new Vector3(38f, 1f, 10f), mat);
            var npc = go.AddComponent<NpcController>();
            npc.npcName = "Pak Yanto (Nelayan Rawa)";
            npc.npcId = "Pak Yanto";
            npc.wanderRadius = 3f;
            npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 0.9f, neglectMultiplier = 0.8f };
            npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
            npc.dislikedItemIds = new[] { ItemDatabase.Oralit };
            npc.friendGiftItemId = ItemDatabase.Air;
            npc.closeGiftItemId = ItemDatabase.Masker;
            npc.helpItemId = ItemDatabase.Air;
            npc.helpItemCount = 2;

            npc.normalLines = new[]
            {
                "Saya mencari ikan gabus, tapah, dan lele lembat di aliran kanal rawa gambut.",
                "Air rawa gambut warnanya cokelat pekat seperti teh pekat karena asam organik alami.",
                "Kanal-kanal ini jalur transportasi perahu klotok terbaik saat jalan darat becek atau tertutup semak."
            };
            npc.nightLines = new[] { "Malam hari ikan rawa lebih aktif mencari makan di bawah akar bakung." };
            npc.dangerLines = new[]
            {
                "Abu pembakaran hutan membuat air rawa tercemar dan ikan-ikan mati mengambang!",
                "Tapi perahu kayu klotok saya siap siaga. Jika jalan darat tertutup api, jalur air adalah satu-satunya jalan keluar."
            };
            npc.friendlyLines = new[]
            {
                "Secangkir kopi panas dari warungmu sangat nikmat sebelum mendayung perahu ke hulu rawa."
            };
            npc.coldLines = new[] { "Saya sedang menambal jaring perahu yang robek. Jangan menghalangi." };
            npc.closeLines = new[]
            {
                "Jalur labirin rawa ini rumit, tapi dengan saya, kamu dijamin aman menjelajahi setiap sudut kanal rawa."
            };
            npc.annoyedLines = new[] { "Nanti dulu ya, getah damar buat menambal perahu ini cepat mengering kalau tidak diratakan." };
            npc.bondingLines = new[]
            {
                "Sekat kanal kayu yang dipasang warga menahan air agar tidak terkuras ke sungai besar.",
                "Jika sekat kanal dirusak, rawa gambut menjadi kering kerontang dan ikan air tawar punah."
            };
            npc.closeBondingLines = new[]
            {
                "Sini kutunjukkan peta rahasia titik-titik sumur air alami di tengah kubah gambut yang tidak pernah kering walau kemarau panjang."
            };

            var barter = go.AddComponent<BarterTrader>();
            barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Kopi, ItemDatabase.Nasi });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 4, maxStock = 6 });
            barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 2, maxStock = 4 });

            var sched = go.AddComponent<NpcSchedule>();
            sched.SetSlot(TimeOfDay.Pagi, "Dermaga Rawa Gambut", new Vector3(38f, 1f, 10f), 3f, "Memeriksa jaring & perahu klotok");
            sched.SetSlot(TimeOfDay.Siang, "Aliran Sekat Kanal", new Vector3(38f, 1f, 14f), 2.5f, "Menjaga debit air sekat kanal rawa");
            sched.SetSlot(TimeOfDay.Malam, "Rumah Dermaga", new Vector3(38f, 1f, 10f), 1f, "Menambal jaring & istirahat");
            sched.emergencyLocationName = "Dermaga Evakuasi Jalur Air";
            sched.emergencyDestination = new Vector3(38f, 1f, 10f);
            sched.emergencyActivity = "Menyiapkan perahu evakuasi jalur rawa";
        }
    }
}
