using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Karhutla
{
    public enum NpcPairStatus { Musuhan, Bersitegang, Netral, Akur, RekanErat }

    [Serializable]
    public class NpcPairRelation
    {
        public string npcAId;
        public string npcBId;
        public string pairTopic;
        public float affinity; // -100 .. 100
        public string currentNote;

        public NpcPairStatus Status
        {
            get
            {
                if (affinity <= -40f) return NpcPairStatus.Musuhan;
                if (affinity <= -10f) return NpcPairStatus.Bersitegang;
                if (affinity < 30f) return NpcPairStatus.Netral;
                if (affinity < 70f) return NpcPairStatus.Akur;
                return NpcPairStatus.RekanErat;
            }
        }
    }

    [Serializable]
    public class CommunityEventOption
    {
        public string optionTitle;
        [TextArea(2, 4)]
        public string choiceDescription;
        public string requiredTierNpcId;
        public RelationshipTier requiredTier = RelationshipTier.Kenalan;
        public float playerDeltaA;
        public float playerDeltaB;
        public float pairAffinityDelta;
        [TextArea(2, 4)]
        public string outcomeNarration;
        public string villageImpact;
    }

    [Serializable]
    public class CommunityEvent
    {
        public string id;
        public string title;
        public string npcAId;
        public string npcBId;
        public string locationName;
        public Vector3 meetingPosition;
        public TimeOfDay preferredPeriod;
        public int minDay;
        public DangerLevel minDanger = DangerLevel.Aman;
        [TextArea(2, 4)]
        public string[] dialogueLines;
        public List<CommunityEventOption> options = new List<CommunityEventOption>();

        // Status kejadian
        public bool isResolved;
        public int chosenOptionIndex = -1;
        public int resolvedDay = -1;
    }

    /// <summary>
    /// Peristiwa Dinamika Antar-Warga (NPC-to-NPC Events - GDD Bab 6.4).
    /// Mengelola relasi independen antar sesama warga desa dan kejadian sosial terencana (authored events)
    /// yang perkembangannya dapat dipengaruhi oleh keputusan pemain dan kedekatan pemain dengan masing-masing warga.
    /// </summary>
    public class CommunityEventManager : MonoBehaviour
    {
        public static CommunityEventManager Instance { get; private set; }

        private const string SaveKey = "Karhutla.CommunityEvents.v1";

        [Header("Daftar Relasi Antar-Warga (NPC-to-NPC)")]
        public List<NpcPairRelation> pairRelations = new List<NpcPairRelation>();

        [Header("Daftar Skenario Peristiwa Warga (Authored Events)")]
        public List<CommunityEvent> authoredEvents = new List<CommunityEvent>();

        public CommunityEvent ActiveEvent { get; private set; }
        private GameObject activeTriggerObject;

        [Serializable]
        private class SavePair
        {
            public string a;
            public string b;
            public float val;
            public string note;
        }

        [Serializable]
        private class SaveEventState
        {
            public string id;
            public bool resolved;
            public int chosenIdx;
            public int day;
        }

        [Serializable]
        private class SaveContainer
        {
            public List<SavePair> pairs = new List<SavePair>();
            public List<SaveEventState> events = new List<SaveEventState>();
        }

        private void Awake()
        {
            Instance = this;
            InitializeDefaultRelations();
            InitializeAuthoredEvents();
            LoadFromSave();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPeriodChanged += HandlePeriodChanged;
                TimeManager.Instance.OnDayChanged += HandleDayChanged;
            }
            CheckAndTriggerEvent();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPeriodChanged -= HandlePeriodChanged;
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            }
            if (Instance == this) Instance = null;
        }

        private void InitializeDefaultRelations()
        {
            if (pairRelations.Count > 0) return;

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Pak Joko",
                npcBId = "Pak Tejo",
                pairTopic = "Sengketa Kanal Gambut & Batas Lahan",
                affinity = -20f,
                currentNote = "Bersitegang soal pembongkaran sekat kanal rawa demi akses alat berat sawit."
            });

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Bu Warti",
                npcBId = "Pak Udin",
                pairTopic = "Harga Sembako & Pasokan Sayur",
                affinity = -10f,
                currentNote = "Sering berdebat di pasar terkait fluktuasi harga bahan mentah kemarau."
            });

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Bidan Ratna",
                npcBId = "Pak Kades",
                pairTopic = "Kesiapsiagaan Posko & Logistik Medis",
                affinity = 10f,
                currentNote = "Bidan mendesak Kades mengesahkan anggaran darurat masker filter ISPA."
            });

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Kang Dedi",
                npcBId = "Pak Joko",
                pairTopic = "Perawatan Mesin Pompa & Selang Pemadam",
                affinity = 40f,
                currentNote = "Kerap bekerjasama memperbaiki mesin pompa air jinjing untuk tim MPA."
            });

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Rian",
                npcBId = "Mas Hendra",
                pairTopic = "Pencegahan Jerat Upah Membakar Lahan",
                affinity = 0f,
                currentNote = "Hendra berusaha membimbing Rian agar tidak tergiur upah membakar ilalang."
            });

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Bu Sari",
                npcBId = "Bu Linda",
                pairTopic = "Solidaritas Logistik Dapur Umum",
                affinity = 35f,
                currentNote = "Berkoordinasi menyiapkan lumbung sembako warga sebelum jalur darat tertutup asap."
            });

            pairRelations.Add(new NpcPairRelation
            {
                npcAId = "Pak Salim",
                npcBId = "Pak Yanto",
                pairTopic = "Penjagaan Rawa Gambut & Pohon Sialang",
                affinity = 50f,
                currentNote = "Sepakat menjaga kelembapan kubah rawa dan rute perahu tradisional."
            });
        }

        private void InitializeAuthoredEvents()
        {
            if (authoredEvents.Count > 0) return;

            // Event 1: Pak Joko vs Pak Tejo
            authoredEvents.Add(new CommunityEvent
            {
                id = "evt_joko_tejo_kanal",
                title = "Sengketa Pembongkaran Sekat Kanal Gambut",
                npcAId = "Pak Joko",
                npcBId = "Pak Tejo",
                locationName = "Balai Desa",
                meetingPosition = new Vector3(-2f, 1f, 10f),
                preferredPeriod = TimeOfDay.Siang,
                minDay = 3,
                minDanger = DangerLevel.Aman,
                dialogueLines = new[]
                {
                    "Pak Joko: \"Tejo, jangan sentuh sekat kanal di blok barat! Sekat itu menjaga rawa gambut tetap basah!\"",
                    "Pak Tejo: \"Mandor pusat mendesak jalan angkut dibuka sebelum panen, Joko. Kalau kanal tidak ditimbun, truk sawit amblas!\"",
                    "Pak Joko: \"Kalau gambutnya kering kerontang, satu puntung rokok saja bisa membakar separuh desa kita!\"",
                    "Pak Tejo: \"Aku juga warga desa ini! Tapi aku dan buruh lain butuh makan! Jangan cuma bicara idealisme!\""
                },
                options = new List<CommunityEventOption>
                {
                    new CommunityEventOption
                    {
                        optionTitle = "Dukung Pak Joko (Tolak Bongkar Sekat)",
                        choiceDescription = "Tegaskan bahwa keselamatan desa dari karhutla jauh lebih penting daripada jalan pintas perusahaan.",
                        playerDeltaA = 10f,
                        playerDeltaB = -6f,
                        pairAffinityDelta = -15f,
                        outcomeNarration = "Kamu dengan tegas membela Pak Joko. Sekat kanal tetap dipertahankan. Pak Tejo mengalah walau dengan wajah masam.",
                        villageImpact = "Kelembapan kubah gambut desa terjaga. Risiko kebakaran dini berkurang."
                    },
                    new CommunityEventOption
                    {
                        optionTitle = "Tawarkan Jalur Kompromi (Rute Memutar)",
                        choiceDescription = "Sarankan truk melintasi jalur lama berbatu dan warga membantu memperkuat jembatan kayu sementara.",
                        requiredTierNpcId = "Pak Joko",
                        requiredTier = RelationshipTier.Kenalan,
                        playerDeltaA = 8f,
                        playerDeltaB = 8f,
                        pairAffinityDelta = 25f,
                        outcomeNarration = "Berkat hubungan baikmu, kedua belah pihak mendengarkan usulanmu. Pak Tejo setuju memutar rute tanpa merusak sekat kanal rawa.",
                        villageImpact = "Hubungan warga harmonis. Sekat kanal aman dan akses ekonomi tetap berjalan."
                    }
                }
            });

            // Event 2: Bu Warti vs Pak Udin
            authoredEvents.Add(new CommunityEvent
            {
                id = "evt_warti_udin_pasar",
                title = "Perdebatan Fluktuasi Harga Pasar",
                npcAId = "Bu Warti",
                npcBId = "Pak Udin",
                locationName = "Pasar Tradisional",
                meetingPosition = new Vector3(-17f, 1f, 2f),
                preferredPeriod = TimeOfDay.Pagi,
                minDay = 5,
                minDanger = DangerLevel.Aman,
                dialogueLines = new[]
                {
                    "Bu Warti: \"Udin! Kenapa harga belimu pada sayur panenanku anjlok, tapi harga jualmu ke warga malah melambung?!\"",
                    "Pak Udin: \"Aduh Bu Warti, biaya kirim dari luar daerah naik karena cuaca terik. Pembeli juga sepi, saya harus tutup ongkos lapak!\"",
                    "Bu Warti: \"Jangan memanfaatkan kemarau untuk spekulasi sembako! Petani yang banting tulang di ladang gambut!\""
                },
                options = new List<CommunityEventOption>
                {
                    new CommunityEventOption
                    {
                        optionTitle = "Beli Panen Bu Warti untuk Warung",
                        choiceDescription = "Tawarkan warungmu membeli langsung pasokan sayur Bu Warti dengan harga wajar untuk menstabilkan pasar.",
                        playerDeltaA = 12f,
                        playerDeltaB = 4f,
                        pairAffinityDelta = 15f,
                        outcomeNarration = "Kamu menjadi penengah dengan membeli sebagian sayur untuk warung. Ketegangan mereda dan Pak Udin menyesuaikan harganya.",
                        villageImpact = "Pasokan bahan pangan segar di desa stabil dan hubungan petani-pedagang membaik."
                    },
                    new CommunityEventOption
                    {
                        optionTitle = "Dukung Stabilitas Harga Pak Udin",
                        choiceDescription = "Ajak Bu Warti memahami risiko logistik pedagang agar ada kesepakatan pasokan berkala.",
                        playerDeltaA = -4f,
                        playerDeltaB = 8f,
                        pairAffinityDelta = 5f,
                        outcomeNarration = "Pak Udin berterima kasih atas pengertianmu. Bu Warti mengerti walau tetap menghitung teliti setiap ikat sayurnya.",
                        villageImpact = "Lapak pasar tetap buka normal tanpa aksi boikot pedagang."
                    }
                }
            });

            // Event 3: Bidan Ratna vs Pak Kades Surya
            authoredEvents.Add(new CommunityEvent
            {
                id = "evt_ratna_kades_masker",
                title = "Tuntutan Anggaran Darurat Masker & Obat ISPA",
                npcAId = "Bidan Ratna",
                npcBId = "Pak Kades",
                locationName = "Balai Desa",
                meetingPosition = new Vector3(-2f, 1f, 12f),
                preferredPeriod = TimeOfDay.Siang,
                minDay = 10,
                minDanger = DangerLevel.Aman,
                dialogueLines = new[]
                {
                    "Bidan Ratna: \"Pak Kades, klinik Pustu kehabisan stok masker filter dan obat pereda sesak! Anak-anak balita mulai batuk!\"",
                    "Pak Kades: \"Saya paham, Bu Bidan. Tapi dana darurat desa butuh laporan resmi dari kecamatan yang belum turun.\"",
                    "Bidan Ratna: \"Asap kebakaran tidak akan menunggu tanda tangan camat, Pak! Kita harus amankan stok sekarang sebelum harga melonjak!\""
                },
                options = new List<CommunityEventOption>
                {
                    new CommunityEventOption
                    {
                        optionTitle = "Desak Kades Ambil Tindakan Cepat (Diskresi)",
                        choiceDescription = "Yakinkan Pak Kades bahwa keselamatan warga adalah hukum tertinggi dan warung siap jadi posko pembagian masker.",
                        playerDeltaA = 10f,
                        playerDeltaB = 6f,
                        pairAffinityDelta = 20f,
                        outcomeNarration = "Dukunganmu membulatkan tekad Pak Kades untuk menandatangani diskresi darurat. Pengadaan masker dan tabung oksigen langsung disetujui.",
                        villageImpact = "Stok masker dan obat ISPA di Puskesmas Pembantu langsung diamankan untuk seluruh warga."
                    },
                    new CommunityEventOption
                    {
                        optionTitle = "Saran Penggalangan Swadaya Mandiri Warga",
                        choiceDescription = "Usulkan penggalangan dana sukarela antar warga toko dan warung untuk menutupi kebutuhan darurat sementara.",
                        playerDeltaA = 6f,
                        playerDeltaB = 8f,
                        pairAffinityDelta = 10f,
                        outcomeNarration = "Solusi swadaya diterima dengan baik. Pak Kades merasa terbantu dan Bidan Ratna segera menyusun daftar logistik darurat.",
                        villageImpact = "Solidaritas warga terbangun menghadapi potensi kabut asap."
                    }
                }
            });

            // Event 4: Kang Dedi vs Pak Joko
            authoredEvents.Add(new CommunityEvent
            {
                id = "evt_dedi_joko_pompa",
                title = "Perakitan Mesin Pompa Air Jinjing Rawa",
                npcAId = "Kang Dedi",
                npcBId = "Pak Joko",
                locationName = "Bengkel Dedi",
                meetingPosition = new Vector3(21f, 1f, -8f),
                preferredPeriod = TimeOfDay.Siang,
                minDay = 7,
                minDanger = DangerLevel.Aman,
                dialogueLines = new[]
                {
                    "Pak Joko: \"Kang Dedi, mesin pompa tua ini sering tersumbat lumpur gambut. Kita butuh filter saringan kawat tebal.\"",
                    "Kang Dedi: \"Bisa kubuatkan, Joko. Tapi aku butuh pipa sambungan fleksibel dan seal karet tahan panas yang masih langka.\"",
                    "Pak Joko: \"Kalau peralatan ini siap sebelum api mendekat, kita bisa menyiram titik panas gambut dalam hitungan menit.\""
                },
                options = new List<CommunityEventOption>
                {
                    new CommunityEventOption
                    {
                        optionTitle = "Bantu Suku Cadang & Semangati Kerjasama",
                        choiceDescription = "Bantu mencarikan sambungan pipa bekas di gudang desa dan semangati kedua tokoh relawan ini.",
                        playerDeltaA = 8f,
                        playerDeltaB = 8f,
                        pairAffinityDelta = 30f,
                        outcomeNarration = "Dengan bantuanmu, Kang Dedi dan Pak Joko berhasil memodifikasi mesin pompa air lumpur bertekanan tinggi!",
                        villageImpact = "Kecepatan pemadaman relawan Masyarakat Peduli Api meningkat secara signifikan."
                    }
                }
            });

            // Event 5: Rian vs Mas Hendra
            authoredEvents.Add(new CommunityEvent
            {
                id = "evt_rian_hendra_bujukan",
                title = "Godaan Upah Membakar Lahan & Investigasi Cukong",
                npcAId = "Rian",
                npcBId = "Mas Hendra",
                locationName = "Bale-bale Desa",
                meetingPosition = new Vector3(-6f, 1f, -6f),
                preferredPeriod = TimeOfDay.Malam,
                minDay = 12,
                minDanger = DangerLevel.Aman,
                dialogueLines = new[]
                {
                    "Mas Hendra: \"Rian, siapa orang luar berjaket hitam yang memberimu amplop tadi sore di dekat batas hutan?\"",
                    "Rian: \"Bukan urusanmu, Hendra! Aku cuma disuruh bersih-bersih lahan ilalang kering, upahnya lumayan buat cicilan motor!\"",
                    "Mas Hendra: \"Bersih-bersih pakai korek api dan jeriken bensin?! Kalau tertangkap satgas, kamu dipenjara 10 tahun sementara cukongnya kabur!\""
                },
                options = new List<CommunityEventOption>
                {
                    new CommunityEventOption
                    {
                        optionTitle = "Nasihati Rian & Beri Pekerjaan di Warung",
                        choiceDescription = "Peringatkan Rian tentang bahaya memusnahkan kampung sendiri dan tawarkan upah bantu-bantu di warungmu.",
                        playerDeltaA = 12f,
                        playerDeltaB = 10f,
                        pairAffinityDelta = 25f,
                        outcomeNarration = "Rian termenung dan menyerahkan korek serta nomor telepon oknum cukong ke Hendra. Rian memilih bekerja jujur di warungmu.",
                        villageImpact = "Pencegahan pembakaran lahan berhasil dan Hendra mengantongi bukti awal jaringan mafia karhutla!"
                    },
                    new CommunityEventOption
                    {
                        optionTitle = "Dukung Investigasi Keras Mas Hendra",
                        choiceDescription = "Desak Hendra segera melaporkan oknum mencurigakan itu ke aparat Babinsa dan Pak RT.",
                        playerDeltaA = -6f,
                        playerDeltaB = 8f,
                        pairAffinityDelta = -10f,
                        outcomeNarration = "Hendra mencatat ciri-ciri pelaku untuk dilaporkan. Rian menjauh dengan perasaan cemas dan malu.",
                        villageImpact = "Patroli aparat di batas desa diperketat."
                    }
                }
            });
        }

        private void HandlePeriodChanged(TimeOfDay period)
        {
            CheckAndTriggerEvent();
        }

        private void HandleDayChanged(int day)
        {
            CheckAndTriggerEvent();
        }

        public void CheckAndTriggerEvent()
        {
            if (ActiveEvent != null && !ActiveEvent.isResolved) return;

            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.Day : 1;
            TimeOfDay currentPeriod = TimeManager.Instance != null ? TimeManager.Instance.Period : TimeOfDay.Pagi;
            DangerLevel currentDanger = DisasterSystem.Instance != null ? DisasterSystem.Instance.CurrentDanger : DangerLevel.Aman;

            for (int i = 0; i < authoredEvents.Count; i++)
            {
                var ev = authoredEvents[i];
                if (ev.isResolved) continue;
                if (currentDay >= ev.minDay && currentPeriod == ev.preferredPeriod && currentDanger >= ev.minDanger)
                {
                    ActivateEvent(ev);
                    break;
                }
            }
        }

        private void ActivateEvent(CommunityEvent ev)
        {
            ActiveEvent = ev;

            // Pindahkan sementara kedua NPC ke titik pertemuan
            RepositionNpcForEvent(ev.npcAId, ev.meetingPosition + new Vector3(-1.2f, 0f, 0f));
            RepositionNpcForEvent(ev.npcBId, ev.meetingPosition + new Vector3(1.2f, 0f, 0f));

            // Buat trigger interaksi di titik temu
            if (activeTriggerObject != null) Destroy(activeTriggerObject);
            activeTriggerObject = new GameObject("CommunityEventTrigger_" + ev.id);
            activeTriggerObject.transform.position = ev.meetingPosition;
            var trigger = activeTriggerObject.AddComponent<CommunityEventTrigger>();
            trigger.Initialize(ev);

            Notifier.Show("Peristiwa Warga: \"" + ev.title + "\" terjadi di " + ev.locationName + "! (Dekati untuk ikut musyawarah)");
            DayReportSystem.LogRelationship("Peristiwa Desa: " + ev.title + " di " + ev.locationName);
        }

        private void RepositionNpcForEvent(string npcId, Vector3 pos)
        {
            var npcs = FindObjectsByType<NpcController>(FindObjectsSortMode.None);
            for (int i = 0; i < npcs.Length; i++)
            {
                if (npcs[i].NpcId == npcId || npcs[i].npcName.Contains(npcId))
                {
                    npcs[i].SetScheduleTarget(pos, 0.8f, ActiveEvent.locationName, "Musyawarah: " + ActiveEvent.title, immediate: false);
                    break;
                }
            }
        }

        /// <summary>
        /// Dipanggil saat pemain memilih opsi penyelesaian peristiwa warga.
        /// </summary>
        public void ResolveEvent(CommunityEvent ev, int optionIndex)
        {
            if (ev == null || optionIndex < 0 || optionIndex >= ev.options.Count) return;

            var opt = ev.options[optionIndex];
            ev.isResolved = true;
            ev.chosenOptionIndex = optionIndex;
            ev.resolvedDay = TimeManager.Instance != null ? TimeManager.Instance.Day : 1;

            // 1. Terapkan perubahan relasi pemain dengan NPC A & B
            if (RelationshipSystem.Instance != null)
            {
                if (!Mathf.Approximately(opt.playerDeltaA, 0f))
                    RelationshipSystem.Instance.Modify(ev.npcAId, opt.playerDeltaA, "Keputusan Peristiwa: " + ev.title);

                if (!Mathf.Approximately(opt.playerDeltaB, 0f))
                    RelationshipSystem.Instance.Modify(ev.npcBId, opt.playerDeltaB, "Keputusan Peristiwa: " + ev.title);
            }

            // 2. Terapkan perubahan relasi antar NPC (NPC-to-NPC)
            ModifyPairAffinity(ev.npcAId, ev.npcBId, opt.pairAffinityDelta, opt.outcomeNarration);

            // 3. Catat ke laporan harian
            DayReportSystem.LogRelationship("Hasil Musyawarah (" + ev.title + "): " + opt.optionTitle);
            if (!string.IsNullOrEmpty(opt.villageImpact))
                DayReportSystem.LogRelationship("Dampak Desa: " + opt.villageImpact);

            // 4. Notifikasi
            Notifier.Show("Keputusan diambil: " + opt.optionTitle + "\n" + opt.villageImpact);

            // 5. Bersihkan trigger objek & kembalikan jadwal NPC
            if (activeTriggerObject != null)
            {
                Destroy(activeTriggerObject);
                activeTriggerObject = null;
            }
            ActiveEvent = null;

            RestoreNpcSchedules(ev.npcAId, ev.npcBId);
            Save();
        }

        private void RestoreNpcSchedules(string idA, string idB)
        {
            var schedules = FindObjectsByType<NpcSchedule>(FindObjectsSortMode.None);
            for (int i = 0; i < schedules.Length; i++)
            {
                var ctrl = schedules[i].GetComponent<NpcController>();
                if (ctrl != null && (ctrl.NpcId == idA || ctrl.NpcId == idB || ctrl.npcName.Contains(idA) || ctrl.npcName.Contains(idB)))
                {
                    schedules[i].ApplySchedule(immediate: false);
                }
            }
        }

        public void ModifyPairAffinity(string idA, string idB, float delta, string note)
        {
            var pair = GetPair(idA, idB);
            if (pair == null)
            {
                pair = new NpcPairRelation { npcAId = idA, npcBId = idB, affinity = 0f, pairTopic = "Relasi Desa" };
                pairRelations.Add(pair);
            }

            pair.affinity = Mathf.Clamp(pair.affinity + delta, -100f, 100f);
            if (!string.IsNullOrEmpty(note)) pair.currentNote = note;
            Save();
        }

        public NpcPairRelation GetPair(string idA, string idB)
        {
            for (int i = 0; i < pairRelations.Count; i++)
            {
                var p = pairRelations[i];
                if ((p.npcAId == idA && p.npcBId == idB) || (p.npcAId == idB && p.npcBId == idA))
                    return p;
            }
            return null;
        }

        public string BuildCommunityStatusText()
        {
            var sb = new StringBuilder();
            sb.Append("<b>DINAMIKA SOSIAL ANTAR-WARGA (NPC-to-NPC)</b>\n");
            sb.Append("<size=18>Relasi independen antar tokoh desa yang dipengaruhi keputusan pemain (GDD 6.4)</size>\n\n");

            for (int i = 0; i < pairRelations.Count; i++)
            {
                var p = pairRelations[i];
                string colorHex = "#FFFFFF";
                switch (p.Status)
                {
                    case NpcPairStatus.Musuhan: colorHex = "#FF5555"; break;
                    case NpcPairStatus.Bersitegang: colorHex = "#FFAA55"; break;
                    case NpcPairStatus.Netral: colorHex = "#DDDDDD"; break;
                    case NpcPairStatus.Akur: colorHex = "#88FF88"; break;
                    case NpcPairStatus.RekanErat: colorHex = "#55FFFF"; break;
                }

                sb.Append("<b>").Append(p.npcAId).Append(" & ").Append(p.npcBId).Append("</b>  [<color=").Append(colorHex).Append(">")
                  .Append(p.Status).Append(" ").Append(Mathf.RoundToInt(p.affinity)).Append("</color>]\n");
                sb.Append("   <size=17>Topik: ").Append(p.pairTopic).Append("\n   Catatan: ").Append(p.currentNote).Append("</size>\n\n");
            }

            int resolvedCount = 0;
            for (int i = 0; i < authoredEvents.Count; i++) if (authoredEvents[i].isResolved) resolvedCount++;

            sb.Append("<b>Riwayat Peristiwa Diselesaikan:</b> ").Append(resolvedCount).Append(" dari ").Append(authoredEvents.Count).Append(" peristiwa\n");
            if (ActiveEvent != null && !ActiveEvent.isResolved)
            {
                sb.Append("<color=#FFD166><b>[SEDANG BERLANGSUNG]</b> ").Append(ActiveEvent.title).Append(" di ").Append(ActiveEvent.locationName).Append("</color>\n");
            }

            return sb.ToString();
        }

        public void Save()
        {
            try
            {
                var c = new SaveContainer();
                for (int i = 0; i < pairRelations.Count; i++)
                {
                    var p = pairRelations[i];
                    c.pairs.Add(new SavePair { a = p.npcAId, b = p.npcBId, val = p.affinity, note = p.currentNote });
                }
                for (int i = 0; i < authoredEvents.Count; i++)
                {
                    var ev = authoredEvents[i];
                    c.events.Add(new SaveEventState { id = ev.id, resolved = ev.isResolved, chosenIdx = ev.chosenOptionIndex, day = ev.resolvedDay });
                }
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(c));
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Gagal menyimpan data peristiwa warga: " + ex.Message);
            }
        }

        public void LoadFromSave()
        {
            try
            {
                if (!PlayerPrefs.HasKey(SaveKey)) return;
                var c = JsonUtility.FromJson<SaveContainer>(PlayerPrefs.GetString(SaveKey));
                if (c == null) return;

                if (c.pairs != null)
                {
                    for (int i = 0; i < c.pairs.Count; i++)
                    {
                        var sp = c.pairs[i];
                        var p = GetPair(sp.a, sp.b);
                        if (p != null)
                        {
                            p.affinity = sp.val;
                            p.currentNote = sp.note;
                        }
                    }
                }

                if (c.events != null)
                {
                    for (int i = 0; i < c.events.Count; i++)
                    {
                        var se = c.events[i];
                        for (int j = 0; j < authoredEvents.Count; j++)
                        {
                            if (authoredEvents[j].id == se.id)
                            {
                                authoredEvents[j].isResolved = se.resolved;
                                authoredEvents[j].chosenOptionIndex = se.chosenIdx;
                                authoredEvents[j].resolvedDay = se.day;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Gagal memuat data peristiwa warga: " + ex.Message);
            }
        }

        public static void ClearSavedData()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Trigger interaksi di dunia untuk menyaksikan dan memutuskan peristiwa dinamika antar-warga.
    /// </summary>
    public class CommunityEventTrigger : InteractableBase
    {
        private CommunityEvent ev;
        private WorldLabel label;

        public void Initialize(CommunityEvent communityEvent)
        {
            ev = communityEvent;
            label = WorldLabel.Create(transform, new Vector3(0f, 2.2f, 0f), "[!] Peristiwa Warga", new Color(1f, 0.85f, 0.2f), 1f);
        }

        public override string GetPrompt()
        {
            return "Ikut Musyawarah Warga: " + (ev != null ? ev.title : "Peristiwa");
        }

        public override void Interact()
        {
            if (ev == null || ev.isResolved || DialogueManager.Instance == null) return;

            // Putar dialog percakapan antar kedua NPC
            DialogueManager.Instance.Begin(ev.title, ev.dialogueLines, ShowDecisionModal);
        }

        private void ShowDecisionModal()
        {
            if (ev == null || ev.options == null || ev.options.Count == 0) return;

            var optA = ev.options[0];
            var optB = ev.options.Count > 1 ? ev.options[1] : null;

            string body = "<b>" + ev.title + "</b>\n" + ev.locationName + "\n\n"
                        + "Bagaimana sikapmu dalam musyawarah ini?\n\n"
                        + "<b>[E] " + optA.optionTitle + "</b>\n" + optA.choiceDescription + "\n\n";

            if (optB != null)
            {
                body += "<b>[Q] " + optB.optionTitle + "</b>\n" + optB.choiceDescription;
            }

            ModalManager.Show(
                "Keputusan Musyawarah Warga",
                body,
                optA.optionTitle,
                optB != null ? optB.optionTitle : null,
                choiceIndex =>
                {
                    CommunityEventManager.Instance.ResolveEvent(ev, choiceIndex);
                },
                priority: 1
            );
        }
    }
}
