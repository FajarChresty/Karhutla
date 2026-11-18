using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Pengisi Otomatis Roster 15 Karakter & Fasilitas Desa saat Runtime (GDD Bab 6 & 8).
    ///
    /// Memastikan bahwa jika pemain menjalankan scene prototype versi lama (yang sebelumnya hanya berisi 3 NPC),
    /// seluruh 15 karakter penting, landmark desa, titik investigasi korupsi, dan sekat bakar
    /// akan otomatis dilengkapi saat game dimulai (Play Mode) tanpa harus rebuild manual.
    /// </summary>
    public static class RuntimeVillagePopulator
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureFullVillagePopulated()
        {
            // Pastikan peta diperbesar dan area kobaran api berada sepenuhnya di dalam daratan
            EnsureExpandedMapAndFireArea();

            var existingNpcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
            var existingIds = new HashSet<string>();
            foreach (var npc in existingNpcs)
            {
                if (npc != null && !string.IsNullOrEmpty(npc.NpcId)) existingIds.Add(npc.NpcId);
            }

            // Jika sudah ada 15 NPC atau lebih, tidak perlu melengkapi
            if (existingNpcs.Length >= 15) return;

            Debug.Log("[Karhutla] Mendeteksi scene lama dengan " + existingNpcs.Length + " NPC. Melengkapi 15 NPC dan fasilitas desa...");

            // Cari atau buat parent NPC
            Transform npcParent = null;
            var npcGo = GameObject.Find("NPC");
            if (npcGo != null) npcParent = npcGo.transform;
            else
            {
                npcParent = new GameObject("NPC").transform;
            }

            // Material warna penanda peran (dummy)
            var matPejabat = CreateSimpleMat("NPC_Pejabat", new Color(0.2f, 0.45f, 0.85f));
            var matMedis = CreateSimpleMat("NPC_Medis", new Color(0.2f, 0.85f, 0.7f));
            var matMekanik = CreateSimpleMat("NPC_Mekanik", new Color(0.55f, 0.55f, 0.6f));
            var matJurnalis = CreateSimpleMat("NPC_Jurnalis", new Color(0.25f, 0.7f, 0.55f));
            var matTetua = CreateSimpleMat("NPC_Tetua", new Color(0.55f, 0.35f, 0.2f));
            var matPemuda = CreateSimpleMat("NPC_Pemuda", new Color(0.95f, 0.8f, 0.2f));
            var matMandor = CreateSimpleMat("NPC_Mandor", new Color(0.75f, 0.25f, 0.2f));
            var matNelayan = CreateSimpleMat("NPC_Nelayan", new Color(0.25f, 0.55f, 0.75f));
            var matWarga = CreateSimpleMat("NPC_Warga", new Color(0.9f, 0.4f, 0.6f));

            // 4. Pak Kades Surya
            if (!existingIds.Contains("Pak Kades"))
            {
                var go = SpawnCapsule(npcParent, "NPC Pak Kades", new Vector3(-2f, 1f, 14f), matPejabat);
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
                    "Kondisi darurat telah diumumkan! Kami sedang berkoordinasi dengan satgas karhutla.",
                    "Semua warga harap mematuhi instruksi evakuasi jika asap melewati batas aman."
                };
                npc.friendlyLines = new[] { "Saya menghargai peran warungmu sebagai simpul informasi masyarakat." };

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Kantor Balai Desa", new Vector3(-2f, 1f, 14f), 2.5f, "Memeriksa arsip desa");
                sched.SetSlot(TimeOfDay.Siang, "Warung Makan", new Vector3(8f, 1f, 3f), 2f, "Makan siang");
                sched.SetSlot(TimeOfDay.Malam, "Balai Desa", new Vector3(-2f, 1f, 16f), 1.5f, "Rapat koordinasi");
            }

            // 5. Pak RT Bambang
            if (!existingIds.Contains("Pak RT"))
            {
                var go = SpawnCapsule(npcParent, "NPC Pak RT", new Vector3(-12f, 1f, 6f), matPejabat);
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
                npc.dangerLines = new[] { "Waspada! Asap mulai masuk ke pemukiman warga RT 02." };

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Lingkungan RT 02", new Vector3(-10f, 1f, 4f), 3.5f, "Patroli parit & sapa warga");
                sched.SetSlot(TimeOfDay.Siang, "Balai Desa", new Vector3(-3f, 1f, 12f), 2f, "Musyawarah aparatur");
                sched.SetSlot(TimeOfDay.Malam, "Pos Ronda", new Vector3(-5f, 1f, -5f), 2.5f, "Jaga pos ronda");
            }

            // 6. Bidan Ratna
            if (!existingIds.Contains("Bidan Ratna"))
            {
                var go = SpawnCapsule(npcParent, "NPC Bidan Ratna", new Vector3(-22f, 1f, 14f), matMedis);
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
                    "Kemarau kering begini debu tanah gambut mudah terhirup saluran napas.",
                    "Perbanyak minum air putih hangat dan konsumsi sayuran hijau ya."
                };
                npc.nightLines = new[] { "Klinik tetap buka untuk persalinan dan panggilan darurat medis warga." };
                npc.dangerLines = new[] { "Paparan asap karhutla sangat berbahaya bagi paru-paru! Gunakan masker N95 atau kain basah." };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Sayur, ItemDatabase.Nasi, ItemDatabase.Air });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 3, maxStock = 6 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 6 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Pustu Desa", new Vector3(-22f, 1f, 14f), 2f, "Pemeriksaan kesehatan");
                sched.SetSlot(TimeOfDay.Siang, "Apotek Pustu", new Vector3(-22f, 1f, 14f), 2f, "Persiapan obat ISPA");
                sched.SetSlot(TimeOfDay.Malam, "Rumah Medis", new Vector3(-24f, 1f, 14f), 1f, "Siaga panggilan darurat");
            }

            // 7. Pak Udin
            if (!existingIds.Contains("Pak Udin"))
            {
                var go = SpawnCapsule(npcParent, "NPC Pak Udin", new Vector3(-18f, 1f, 1f), matWarga);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Pak Udin (Pedagang)";
                npc.npcId = "Pak Udin";
                npc.wanderRadius = 2.5f;
                npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1.3f, neglectMultiplier = 1.3f };
                npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
                npc.friendGiftItemId = ItemDatabase.BubukKopi;
                npc.closeGiftItemId = ItemDatabase.Sayur;
                npc.helpItemId = ItemDatabase.Sayur;
                npc.helpItemCount = 2;
                npc.normalLines = new[]
                {
                    "Mari mampir ke lapak Udin! Sayur segar, bumbu dapur komplit!",
                    "Kalau butuh pasokan bumbu buat warung, hubungi Pak Udin saja."
                };
                npc.nightLines = new[] { "Tutup lapak, hitung laba, lalu tidur nyenyak." };
                npc.dangerLines = new[] { "Aduh kacau, kabut asap bikin jalan lintas tertutup! Pengiriman bahan pangan tertunda!" };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Kopi, ItemDatabase.Air, ItemDatabase.Nasi });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Sayur, stock = 4, maxStock = 7 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Pasar Desa", new Vector3(-18f, 1f, 1f), 2f, "Berdagang di lapak pasar");
                sched.SetSlot(TimeOfDay.Siang, "Toko Kelontong", new Vector3(-10f, 1f, -2f), 2f, "Kulakan grosir");
                sched.SetSlot(TimeOfDay.Malam, "Rumah Pasar", new Vector3(-20f, 1f, 3f), 1f, "Rekap pembukuan");
            }

            // 8. Kang Dedi
            if (!existingIds.Contains("Kang Dedi"))
            {
                var go = SpawnCapsule(npcParent, "NPC Kang Dedi", new Vector3(22f, 1f, -8f), matMekanik);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Kang Dedi (Bengkel)";
                npc.npcId = "Kang Dedi";
                npc.wanderRadius = 3f;
                npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1.0f, neglectMultiplier = 0.8f };
                npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
                npc.friendGiftItemId = ItemDatabase.Air;
                npc.closeGiftItemId = ItemDatabase.Masker;
                npc.helpItemId = ItemDatabase.Masker;
                npc.helpItemCount = 1;
                npc.normalLines = new[]
                {
                    "Ada yang rusak? Bawa ke bengkel Dedi, dari motor sampai mesin pompa sawah!",
                    "Sekarang lagi musim servis pompa air untuk sedot air kanal gambut."
                };
                npc.nightLines = new[] { "Tangan belepotan oli begini enaknya ngopi panas di bale-bale." };
                npc.dangerLines = new[] { "Pompa pemadam relawan harus terus diservis agar kuat semprot lumpur gambut!" };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Air });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 5 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 3, maxStock = 5 });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Bengkel Mesin", new Vector3(22f, 1f, -8f), 3f, "Servis mesin motor & diesel");
                sched.SetSlot(TimeOfDay.Siang, "Bengkel Pompa", new Vector3(22f, 1f, -8f), 2.5f, "Merakit mesin pompa air");
                sched.SetSlot(TimeOfDay.Malam, "Bale-bale Desa", new Vector3(-6f, 1f, -6f), 2f, "Minum kopi malam");
            }

            // 9. Mas Hendra (Jurnalis Investigasi)
            if (!existingIds.Contains("Mas Hendra"))
            {
                var go = SpawnCapsule(npcParent, "NPC Mas Hendra", new Vector3(0f, 1f, 28f), matJurnalis);
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
                    "Banyak konsesi perkebunan sengaja mengeringkan kanal gambut agar lahan mudah dibersihkan dengan api murah."
                };
                npc.nightLines = new[] { "Mengetik laporan dan mengunggah data satelit hotspot butuh sinyal yang stabil di balai desa." };
                npc.dangerLines = new[]
                {
                    "Titik api baru terpantau serentak di batas konsesi! Ini pola terencana, bukan sekadar ketidaksengajaan!",
                    "Gunakan masker respirator. Partikel asap ini membawa sulfur dan tar beracun."
                };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Air, ItemDatabase.Kopi, ItemDatabase.Nasi });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 3, maxStock = 5 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 4, minTier = RelationshipTier.Teman });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Batas Gambut", new Vector3(0f, 1f, 28f), 4f, "Dokumentasi titik panas");
                sched.SetSlot(TimeOfDay.Siang, "Warung Makan", new Vector3(10f, 1f, 5f), 2f, "Makan siang & menulis catatan");
                sched.SetSlot(TimeOfDay.Malam, "Pojok Baca", new Vector3(-5f, 1f, -4f), 2.5f, "Wawancara narasumber");
            }

            // 10. Pak Salim (Tetua Adat)
            if (!existingIds.Contains("Pak Salim"))
            {
                var go = SpawnCapsule(npcParent, "NPC Pak Salim", new Vector3(-30f, 1f, 26f), matTetua);
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
                    "Nenek moyang kita hidup berdampingan dengan hutan rawa ratusan tahun tanpa pernah membakarnya."
                };
                npc.nightLines = new[] { "Mendengarkan suara jangkrik dan gemerisik daun rawa gambut menenangkan jiwa." };
                npc.dangerLines = new[] { "Ketika bumi terbakar dalam keserakahan, kabut asap adalah peringatan keras bagi kita semua." };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Air, ItemDatabase.Beras, ItemDatabase.Nasi });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Obat, stock = 2, maxStock = 4 });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Pondok Adat", new Vector3(-30f, 1f, 26f), 3f, "Meracik jamu herbal gambut");
                sched.SetSlot(TimeOfDay.Siang, "Kanal Rawa", new Vector3(26f, 1f, 16f), 3f, "Memeriksa kelembapan kubah rawa");
                sched.SetSlot(TimeOfDay.Malam, "Pondok Adat", new Vector3(-30f, 1f, 26f), 1f, "Doa keselamatan alam");
            }

            // 11. Rian (Pemuda Desa)
            if (!existingIds.Contains("Rian"))
            {
                var go = SpawnCapsule(npcParent, "NPC Rian", new Vector3(-6f, 1f, -6f), matPemuda);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Rian (Pemuda Desa)";
                npc.npcId = "Rian";
                npc.wanderRadius = 3f;
                npc.social = new SocialProfile { startingValue = 0f, gainMultiplier = 1.2f, neglectMultiplier = 1.2f };
                npc.likedItemIds = new[] { ItemDatabase.Nasi, ItemDatabase.Kopi };
                npc.friendGiftItemId = ItemDatabase.Air;
                npc.closeGiftItemId = ItemDatabase.BubukKopi;
                npc.helpItemId = ItemDatabase.Nasi;
                npc.helpItemCount = 1;
                npc.normalLines = new[]
                {
                    "Nongkrong di bale-bale sambil menunggu lowongan buruh harian.",
                    "Kadang ada orang asing nawari uang cepat buat babat semak belukar di batas hutan."
                };
                npc.nightLines = new[] { "Gitaran di pos ronda bareng anak-anak muda desa." };
                npc.dangerLines = new[] { "Asapnya bikin mata perih banget! Untung aku tidak tergiur uang bakar lahan kemarin." };

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Bale-bale", new Vector3(-6f, 1f, -6f), 3f, "Mencari peluang kerja harian");
                sched.SetSlot(TimeOfDay.Siang, "Warung Kopi", new Vector3(7f, 1f, 2f), 2f, "Bantu angkat barang di warung");
                sched.SetSlot(TimeOfDay.Malam, "Pos Ronda", new Vector3(-5f, 1f, 21f), 2.5f, "Kumpul pemuda desa");
            }

            // 12. Bu Linda (Toko Sembako)
            if (!existingIds.Contains("Bu Linda"))
            {
                var go = SpawnCapsule(npcParent, "NPC Bu Linda", new Vector3(-10f, 1f, -2f), matWarga);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Bu Linda (Toko Sembako)";
                npc.npcId = "Bu Linda";
                npc.wanderRadius = 2.5f;
                npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 0.9f, neglectMultiplier = 1.1f };
                npc.likedItemIds = new[] { ItemDatabase.Beras, ItemDatabase.BubukKopi };
                npc.friendGiftItemId = ItemDatabase.Oralit;
                npc.closeGiftItemId = ItemDatabase.Beras;
                npc.helpItemId = ItemDatabase.Beras;
                npc.helpItemCount = 2;
                npc.normalLines = new[]
                {
                    "Toko Kelontong Linda menyediakan sembako grosir dan eceran.",
                    "Kalau butuh karungan beras atau gula buat stok warung, langsung order ke sini."
                };
                npc.nightLines = new[] { "Menutup rolling door toko dan menghitung pembukuan." };
                npc.dangerLines = new[] { "Jalan lintas darat tertutup kabut asap! Pasokan beras dari kota mulai langka." };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.Sayur });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 5, maxStock = 8 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.BubukKopi, stock = 4, maxStock = 6 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Oralit, stock = 3, maxStock = 5 });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Toko Kelontong", new Vector3(-10f, 1f, -2f), 2f, "Mengecek stok sembako");
                sched.SetSlot(TimeOfDay.Siang, "Toko Kelontong", new Vector3(-10f, 1f, -2f), 2f, "Melayani pembeli");
                sched.SetSlot(TimeOfDay.Malam, "Rumah Toko", new Vector3(-10f, 1f, 0f), 1f, "Rekap pembukuan kas");
            }

            // 13. Pak Tejo (Mandor Perkebunan Sawit)
            if (!existingIds.Contains("Pak Tejo"))
            {
                var go = SpawnCapsule(npcParent, "NPC Pak Tejo", new Vector3(-32f, 1f, -8f), matMandor);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Pak Tejo (Mandor Perkebunan)";
                npc.npcId = "Pak Tejo";
                npc.wanderRadius = 3.5f;
                npc.social = new SocialProfile { startingValue = -15f, gainMultiplier = 0.7f, neglectMultiplier = 1.0f };
                npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
                npc.friendGiftItemId = ItemDatabase.Air;
                npc.closeGiftItemId = ItemDatabase.Beras;
                npc.helpItemId = ItemDatabase.Beras;
                npc.helpItemCount = 1;
                npc.normalLines = new[]
                {
                    "Saya mandor lapangan perkebunan sawit. Tugas saya memastikan target panen dan jalur angkut lancar.",
                    "Orang-orang sering menyalahkan perusahaan setiap ada asap, padahal tidak semua api dari kebun kami."
                };
                npc.nightLines = new[] { "Malam hari saya mengawasi pos keamanan traktor perkebunan." };
                npc.dangerLines = new[] { "Api liar dari semak merembet ke blok tanaman sawit produktif!" };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Kopi, ItemDatabase.Nasi });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Beras, stock = 3, maxStock = 5 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 4, maxStock = 6 });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Batas Sawit", new Vector3(-32f, 1f, -8f), 3.5f, "Mengawasi kerja buruh kebun");
                sched.SetSlot(TimeOfDay.Siang, "Balai Desa", new Vector3(-1f, 1f, 11f), 2f, "Koordinasi batas lahan");
                sched.SetSlot(TimeOfDay.Malam, "Rumah Perbatasan", new Vector3(-34f, 1f, -10f), 1f, "Istirahat malam");
            }

            // 14. Siti (Pelajar)
            if (!existingIds.Contains("Siti"))
            {
                var go = SpawnCapsule(npcParent, "NPC Siti", new Vector3(-6f, 1f, -4f), matPemuda);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Siti (Pelajar)";
                npc.npcId = "Siti";
                npc.wanderRadius = 2.5f;
                npc.social = new SocialProfile { startingValue = 10f, gainMultiplier = 1.3f, neglectMultiplier = 1.0f };
                npc.likedItemIds = new[] { ItemDatabase.Air, ItemDatabase.Nasi };
                npc.friendGiftItemId = ItemDatabase.Masker;
                npc.closeGiftItemId = ItemDatabase.Oralit;
                npc.helpItemId = ItemDatabase.Masker;
                npc.helpItemCount = 1;
                npc.normalLines = new[]
                {
                    "Halo Kak! Aku Siti, lagi merapikan buku di pojok baca desa.",
                    "Aku suka membaca tentang sains, alam, dan ekosistem lahan basah gambut."
                };
                npc.nightLines = new[] { "Malam hari di rumah biasanya aku belajar dengan lampu minyak." };
                npc.dangerLines = new[] { "Sekolah kami diliburkan karena ISPU masuk zona hitam berbahaya!" };

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Pojok Baca", new Vector3(-6f, 1f, -4f), 2f, "Belajar & merapikan buku");
                sched.SetSlot(TimeOfDay.Siang, "Warung Makan", new Vector3(9f, 1f, 3f), 2f, "Makan siang & baca buku");
                sched.SetSlot(TimeOfDay.Malam, "Rumah Warga", new Vector3(-14f, 1f, -9f), 1f, "Belajar di rumah");
            }

            // 15. Pak Yanto (Nelayan Rawa)
            if (!existingIds.Contains("Pak Yanto"))
            {
                var go = SpawnCapsule(npcParent, "NPC Pak Yanto", new Vector3(28f, 1f, 16f), matNelayan);
                var npc = go.AddComponent<NpcController>();
                npc.npcName = "Pak Yanto (Nelayan Rawa)";
                npc.npcId = "Pak Yanto";
                npc.wanderRadius = 3f;
                npc.social = new SocialProfile { startingValue = -5f, gainMultiplier = 0.9f, neglectMultiplier = 0.8f };
                npc.likedItemIds = new[] { ItemDatabase.Kopi, ItemDatabase.Nasi };
                npc.friendGiftItemId = ItemDatabase.Air;
                npc.closeGiftItemId = ItemDatabase.Masker;
                npc.helpItemId = ItemDatabase.Air;
                npc.helpItemCount = 2;
                npc.normalLines = new[]
                {
                    "Saya mencari ikan gabus dan lele lembat di aliran kanal rawa gambut.",
                    "Kanal-kanal ini jalur transportasi perahu klotok terbaik di saat jalan darat tertutup semak."
                };
                npc.nightLines = new[] { "Malam hari ikan rawa lebih aktif mencari makan di bawah akar bakung." };
                npc.dangerLines = new[] { "Jika jalan darat tertutup api, jalur perahu klotok saya siap mengevakuasi warga." };

                var barter = go.AddComponent<BarterTrader>();
                barter.acceptedItemIds.AddRange(new[] { ItemDatabase.Kopi, ItemDatabase.Nasi });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Air, stock = 4, maxStock = 6 });
                barter.offers.Add(new BarterOffer { itemId = ItemDatabase.Masker, stock = 2, maxStock = 4 });

                var sched = go.AddComponent<NpcSchedule>();
                sched.SetSlot(TimeOfDay.Pagi, "Dermaga Rawa", new Vector3(28f, 1f, 16f), 3f, "Mencari ikan di kanal rawa");
                sched.SetSlot(TimeOfDay.Siang, "Pasar Desa", new Vector3(-17f, 1f, 1f), 2f, "Menjual ikan segar ke pasar");
                sched.SetSlot(TimeOfDay.Malam, "Dermaga Rawa", new Vector3(28f, 1f, 16f), 1f, "Menambatkan perahu klotok");
            }

            // Pastikan landmark investigasi dan sekat bakar juga terpasang jika belum ada
            EnsureStoryInteractablesPopulated();

            Debug.Log("[Karhutla] Seluruh 15 NPC dan fasilitas desa berhasil dilengkapi saat runtime!");
        }

        private static void EnsureStoryInteractablesPopulated()
        {
            if (Object.FindFirstObjectByType<InvestigationClueSpot>() == null)
            {
                var storyRoot = new GameObject("Story_Objects_Runtime").transform;

                // Meja Arsip Balai Desa
                var clueBalai = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clueBalai.name = "Meja Arsip Izin Konsesi";
                clueBalai.transform.SetParent(storyRoot, false);
                clueBalai.transform.position = new Vector3(-2f, 0.5f, 10.5f);
                clueBalai.transform.localScale = new Vector3(1.6f, 0.9f, 0.9f);
                var spotBalai = clueBalai.AddComponent<InvestigationClueSpot>();
                spotBalai.clueId = InvestigationSystem.ClueKonsesi;
                spotBalai.interactRange = 2.4f;

                var lblBalai = new GameObject("Label Arsip").AddComponent<WorldLabel>();
                lblBalai.transform.SetParent(storyRoot, false);
                lblBalai.transform.position = new Vector3(-2f, 1.4f, 10.5f);
                lblBalai.text = "Arsip Perizinan";
                lblBalai.color = Color.yellow;
                lblBalai.scale = 0.6f;

                // Meja Peta Pondok Gambut
                var cluePondok = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cluePondok.name = "Meja Peta Ladang Gambut";
                cluePondok.transform.SetParent(storyRoot, false);
                cluePondok.transform.position = new Vector3(-32f, 0.5f, -8f);
                cluePondok.transform.localScale = new Vector3(1.4f, 0.8f, 0.8f);
                var spotPondok = cluePondok.AddComponent<InvestigationClueSpot>();
                spotPondok.clueId = InvestigationSystem.CluePetaBakar;
                spotPondok.interactRange = 2.4f;

                var lblPondok = new GameObject("Label Peta").AddComponent<WorldLabel>();
                lblPondok.transform.SetParent(storyRoot, false);
                lblPondok.transform.position = new Vector3(-32f, 1.3f, -8f);
                lblPondok.text = "Peta Titik Api";
                lblPondok.color = Color.yellow;
                lblPondok.scale = 0.6f;
            }

            if (Object.FindFirstObjectByType<FirebreakSpot>() == null)
            {
                var fbRoot = new GameObject("Sekat_Bakar_Runtime").transform;
                CreateRuntimeFirebreak(fbRoot, "Sektor Barat", new Vector3(-24f, 0.05f, 24f));
                CreateRuntimeFirebreak(fbRoot, "Sektor Tengah", new Vector3(0f, 0.05f, 24f));
                CreateRuntimeFirebreak(fbRoot, "Sektor Timur", new Vector3(24f, 0.05f, 24f));
            }

            // Sesi 6: Bahaya Lingkungan Tambahan (Tanah Gambut Labil & Pencemaran Sumber Air - GDD Bab 8.4)
            if (Object.FindFirstObjectByType<UnstableGroundHazard>() == null)
            {
                var hzRoot = new GameObject("Bahaya_Lingkungan_Runtime").transform;
                CreateRuntimeUnstableGround(hzRoot, "Tanah Gambut Labil Utara", new Vector3(8f, 0.05f, 29f), 4.5f);
                CreateRuntimeUnstableGround(hzRoot, "Tanah Gambut Labil Barat", new Vector3(-28f, 0.05f, -3f), 4.2f);
                CreateRuntimeUnstableGround(hzRoot, "Tanah Gambut Labil Timur", new Vector3(25f, 0.05f, 10f), 4.0f);
            }

            if (Object.FindFirstObjectByType<WaterSourceHazard>() == null)
            {
                var wsRoot = new GameObject("Sumber_Air_Runtime").transform;
                CreateRuntimeWaterSource(wsRoot, "Sumur Gali Balai Desa", new Vector3(-5f, 0f, 6f));
                CreateRuntimeWaterSource(wsRoot, "Sumber Air Kanal Gambut", new Vector3(26f, 0f, 21f));
            }
        }

        private static void CreateRuntimeFirebreak(Transform parent, string sector, Vector3 pos)
        {
            var zone = new GameObject("SekatBakar_" + sector);
            zone.transform.SetParent(parent, false);
            zone.transform.position = pos;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Patok Penanda";
            post.transform.SetParent(zone.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            post.transform.localScale = new Vector3(0.3f, 1.2f, 0.3f);

            var fb = zone.AddComponent<FirebreakSpot>();
            fb.sectorName = sector;
            fb.requiredStages = 3;
            fb.interactRange = 3.5f;

            var lbl = new GameObject("Label Sekat").AddComponent<WorldLabel>();
            lbl.transform.SetParent(zone.transform, false);
            lbl.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            lbl.text = "Sekat Bakar: " + sector;
            lbl.color = Color.cyan;
            lbl.scale = 0.7f;
        }

        private static void CreateRuntimeUnstableGround(Transform parent, string name, Vector3 pos, float radius)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.position = pos;
            var hazard = zone.AddComponent<UnstableGroundHazard>();
            hazard.zoneName = name;
            hazard.hazardRadius = radius;
        }

        private static void CreateRuntimeWaterSource(Transform parent, string name, Vector3 pos)
        {
            var well = new GameObject(name);
            well.transform.SetParent(parent, false);
            well.transform.position = pos;
            var water = well.AddComponent<WaterSourceHazard>();
            water.sourceName = name;
        }

        private static GameObject SpawnCapsule(Transform parent, string name, Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return go;
        }

        private static Material _cachedTemplateMat;

        private static Material GetBaseTemplateMaterial()
        {
            if (_cachedTemplateMat != null) return _cachedTemplateMat;

            // 1. Coba cari dari NPC atau Player yang sudah ada di scene
            var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
            foreach (var n in npcs)
            {
                var rend = n.GetComponentInChildren<Renderer>();
                if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.shader != null)
                {
                    _cachedTemplateMat = rend.sharedMaterial;
                    return _cachedTemplateMat;
                }
            }

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var rend = player.GetComponentInChildren<Renderer>();
                if (rend != null && rend.sharedMaterial != null && rend.sharedMaterial.shader != null)
                {
                    _cachedTemplateMat = rend.sharedMaterial;
                    return _cachedTemplateMat;
                }
            }

            // 2. Buat primitive cube sementara untuk mengambil default material aktif project
            GameObject temp = null;
            try
            {
                temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var r = temp.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null)
                {
                    _cachedTemplateMat = r.sharedMaterial;
                }
            }
            finally
            {
                if (temp != null) Object.DestroyImmediate(temp);
            }

            return _cachedTemplateMat;
        }

        private static Material CreateSimpleMat(string name, Color color)
        {
            Material mat = null;
            var template = GetBaseTemplateMaterial();
            if (template != null)
            {
                mat = new Material(template);
            }
            else
            {
                Shader s = null;
                if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
                {
                    s = Shader.Find("Universal Render Pipeline/Lit");
                }
                if (s == null) s = Shader.Find("Standard");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Diffuse");
                if (s != null) mat = new Material(s);
            }

            if (mat != null)
            {
                mat.name = name;
                mat.color = color;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            }
            return mat;
        }

        /// <summary>
        /// Memperluas area peta dan memastikan pusat kobaran api dan pohon-pohon yang terbakar
        /// berada sepenuhnya di dalam daratan (tidak di luar peta atau menembus batas).
        /// </summary>
        public static void EnsureExpandedMapAndFireArea()
        {
            // 1. Perbesar bidang tanah utama (Tanah) menjadi 240m x 250m
            var groundGo = GameObject.Find("Tanah");
            if (groundGo != null)
            {
                if (groundGo.transform.localScale.x < 20f || groundGo.transform.position.z < 10f)
                {
                    groundGo.transform.position = new Vector3(0f, 0f, 25f);
                    groundGo.transform.localScale = new Vector3(24f, 1f, 25f);
                }
            }

            // 2. Buat hamparan tanah gambut alami di hutan utara jika belum ada
            if (GameObject.Find("Tanah_Gambut_Utara") == null)
            {
                var envGo = GameObject.Find("Environment");
                Transform envParent = envGo != null ? envGo.transform : null;

                var peat = GameObject.CreatePrimitive(PrimitiveType.Plane);
                peat.name = "Tanah_Gambut_Utara";
                if (envParent != null) peat.transform.SetParent(envParent, false);
                peat.transform.position = new Vector3(0f, 0.02f, 65f);
                peat.transform.localScale = new Vector3(24f, 1f, 15f);

                var r = peat.GetComponent<Renderer>();
                if (r != null)
                {
                    var mat = CreateSimpleMat("Mat_Tanah_Gambut", new Color(0.18f, 0.12f, 0.08f));
                    if (mat != null) r.sharedMaterial = mat;
                }
            }

            // 3. Perluas dinding pembatas tak terlihat di tepi peta
            ExpandBarrier("Batas_Utara", new Vector3(0f, 2f, 135f), new Vector3(245f, 4f, 1f));
            ExpandBarrier("Batas_Selatan", new Vector3(0f, 2f, -85f), new Vector3(245f, 4f, 1f));
            ExpandBarrier("Batas_Timur", new Vector3(115f, 2f, 25f), new Vector3(1f, 4f, 225f));
            ExpandBarrier("Batas_Barat", new Vector3(-115f, 2f, 25f), new Vector3(1f, 4f, 225f));

            // 4. Pastikan titik api DisasterSystem berada di dalam hutan utara
            var disaster = DisasterSystem.Instance ?? Object.FindFirstObjectByType<DisasterSystem>();
            if (disaster != null)
            {
                disaster.fireOrigin = new Vector3(0f, 0f, 48f);
                disaster.maxRadius = 36f;
            }

            // 5. Tambahkan pohon-pohon di hutan utara hingga z = 95f
            EnsureExpandedForestTrees();
        }

        private static void ExpandBarrier(string name, Vector3 pos, Vector3 size)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.transform.position = pos;
                var box = go.GetComponent<BoxCollider>();
                if (box != null) box.size = size;
                else go.transform.localScale = size;
            }
        }

        private static void EnsureExpandedForestTrees()
        {
            var forestGo = GameObject.Find("Hutan");
            if (forestGo == null) return;

            var existingTrees = forestGo.GetComponentsInChildren<BurnableTree>();
            if (existingTrees != null && existingTrees.Length >= 150) return;

            var templateTree = existingTrees != null && existingTrees.Length > 0 ? existingTrees[0].gameObject : null;
            if (templateTree == null) return;

            var rng = new System.Random(5678);
            for (int i = 0; i < 45; i++)
            {
                float x = (float)(rng.NextDouble() * 120.0 - 60.0);
                float z = (float)(rng.NextDouble() * 38.0 + 56.0); // 56 s.d. 94f
                Vector3 pos = new Vector3(x, 0f, z);

                var clone = Object.Instantiate(templateTree, pos, Quaternion.identity, forestGo.transform);
                clone.name = "Pohon_Utara_" + i;
            }
        }
    }
}
