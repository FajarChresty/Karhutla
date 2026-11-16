using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Tingkat hubungan. Batas nilainya NILAI SEMENTARA (GDD belum menjelaskan relationship meter).</summary>
    public enum RelationshipTier { Dingin, Kenalan, Teman, Dekat }

    /// <summary>Reaksi warga terhadap hadiah dari pemain.</summary>
    public enum GiftReaction { Basi, TidakSuka, Biasa, Suka }

    /// <summary>
    /// Watak sosial satu warga. Diisi per NPC di NpcController, jadi tiap warga punya nilai awal dan laju yang berbeda.
    /// Semua angka NILAI SEMENTARA (belum ada di GDD).
    /// </summary>
    [Serializable]
    public class SocialProfile
    {
        [Tooltip("Nilai hubungan awal dengan pemain (dipakai bila belum ada data tersimpan). Di bawah 0 = mulai Dingin.")]
        public float startingValue = 0f;
        [Tooltip("Pengali kenaikan hubungan. Warga ramah > 1, warga tertutup < 1.")]
        public float gainMultiplier = 1f;
        [Tooltip("Pengali penurunan karena diabaikan. Warga yang mudah merasa dilupakan > 1.")]
        public float neglectMultiplier = 1f;
    }

    public class RelationshipEntry
    {
        public string Id;
        public string DisplayName;
        public float Value;
        public int LastInteractionDay;
        public SocialProfile Profile = new SocialProfile();

        // Penanda harian (di-reset saat hari berganti)
        public bool TalkedToday;
        public bool DedicatedToday;
        public bool BarteredToday;
        public bool GivenGiftToday;
        public int TalkCountToday;
        public float GainedToday;
        public float LostToday;
        public int FlagsDay;

        // Penanda lintas hari
        public int LastGiftDay;
        public int LastHelpDay;
        public int LastDecayDay;
    }

    /// <summary>
    /// Sistem hubungan sosial antara pemain dan setiap NPC/warga.
    ///
    ///  - Data: satu RelationshipEntry per NPC (id unik) dengan watak sendiri (SocialProfile). Nilai dibatasi minValue..maxValue.
    ///  - Naik: bicara, waktu khusus bersama, barter. Tiap jenis hanya menambah SEKALI PER HARI per NPC. Kenaikan dikali watak NPC,
    ///    dikali faktor tingkat (makin dekat makin lambat), dan dibatasi dailyGainCap per hari. Jadi tidak ada lonjakan drastis.
    ///  - Turun: (1) mengganggu: mengajak bicara terlalu sering dalam sehari; (2) diabaikan: tidak ada interaksi lebih dari
    ///    neglectGraceDays hari. Pengabaian hanya menurunkan sampai batas Kenalan (tidak membuat warga Dingin).
    ///  - Dampak per tingkat (lihat DescribeBenefits): harga barter, mau/tidak diajak menghabiskan waktu, barang barter khusus,
    ///    dialog, info tambahan, hadiah berkala (Teman+), tawaran bantuan (Dekat).
    ///  - Waktu: interaksi tetap lewat TimeActivityManager (dipanggil NpcController). Hari berganti dibaca dari TimeManager.OnDayChanged,
    ///    termasuk time skip: pengabaian dihitung dari selisih hari nyata.
    ///  - Simpan/muat: PlayerPrefs (JSON). Nilai, hari terakhir, penanda harian, dan hitungan hadiah/bantuan ikut tersimpan.
    /// </summary>
    public class RelationshipSystem : MonoBehaviour
    {
        public static RelationshipSystem Instance { get; private set; }

        private const string SaveKey = "Karhutla.Relationships.v1";

        [Header("Rentang & tingkat (NILAI SEMENTARA)")]
        public float minValue = -100f;
        public float maxValue = 100f;
        [Tooltip("Dipakai hanya bila NPC tidak memberi SocialProfile.")]
        public float startingValue = 0f;
        public float kenalanThreshold = 0f;
        public float temanThreshold = 25f;
        public float dekatThreshold = 60f;

        [Header("Kenaikan dari interaksi (NILAI SEMENTARA, maksimal sekali per hari per jenis)")]
        public float talkGain = 2f;
        public float dedicatedTimeGain = 8f;
        public float barterGain = 3f;
        [Tooltip("Hadiah dari pemain (maksimal 1 hadiah positif per warga per hari). Suka = barang kesukaan warga, Biasa = barang lain yang masih layak.")]
        public float giftLikedGain = 6f;
        public float giftNeutralGain = 2f;
        [Tooltip("Batas total kenaikan per NPC per hari (mencegah lonjakan).")]
        public float dailyGainCap = 10f;
        [Tooltip("Pengali kenaikan menurut tingkat saat ini: Dingin, Kenalan, Teman, Dekat. Makin dekat makin lambat.")]
        public float[] tierGainMultiplier = { 1.2f, 1f, 0.75f, 0.5f };

        [Header("Penurunan (NILAI SEMENTARA)")]
        [Tooltip("Mengajak bicara yang sama lebih dari (1 + nilai ini) kali dalam sehari dianggap mengganggu.")]
        public int talkFreeRepeats = 2;
        public float talkSpamPenalty = 1f;
        [Tooltip("Penurunan saat memberi barang yang tidak disukai atau barang basi (tidak dibatasi 1x per hari, tapi dibatasi dailyLossCap).")]
        public float giftDislikedPenalty = 3f;
        public float giftSpoiledPenalty = 5f;
        [Tooltip("Batas total penurunan per NPC per hari.")]
        public float dailyLossCap = 5f;
        [Tooltip("Hari tanpa interaksi sebelum hubungan mulai menurun. 0 = tidak ada penurunan karena diabaikan.")]
        public int neglectGraceDays = 4;
        public float neglectLossPerDay = 1f;
        [Tooltip("Penurunan karena diabaikan berhenti di nilai ini (default = batas Kenalan).")]
        public float neglectFloor = 0f;

        [Header("Dampak per tingkat (NILAI SEMENTARA)")]
        [Tooltip("Pengali harga barang yang diminta pemain saat barter, menurut tingkat: Dingin, Kenalan, Teman, Dekat. < 1 = diskon.")]
        public float[] barterPriceMultiplier = { 1.5f, 1f, 0.85f, 0.7f };
        [Tooltip("Warga menolak barter bila nilai hubungan di bawah ini.")]
        public float refuseBarterBelow = -30f;
        public RelationshipTier spendTimeMinTier = RelationshipTier.Kenalan;
        public RelationshipTier giftMinTier = RelationshipTier.Teman;
        public RelationshipTier helpMinTier = RelationshipTier.Dekat;
        public int giftCooldownDays = 3;
        public int helpCooldownDays = 5;

        [Header("Penyimpanan")]
        [Tooltip("Simpan ke PlayerPrefs setiap hubungan berubah dan setiap hari berganti.")]
        public bool saveToPlayerPrefs = true;
        [Tooltip("Muat data tersimpan saat Play dimulai, sehingga hubungan bertahan antar sesi bermain. Menu Karhutla > Build Prototype Scene menghapus data tersimpan.")]
        public bool loadSavedOnStart = true;

        [Header("Debug (hanya untuk pengujian)")]
        [Tooltip("] = +10 ke warga terdekat, [ = -10, \\ = reset semua hubungan.")]
        public bool debugKeysEnabled = true;

        public event Action<string, float, float> OnChanged; // id, nilai lama, nilai baru

        private readonly Dictionary<string, RelationshipEntry> entries = new Dictionary<string, RelationshipEntry>();
        private readonly List<string> order = new List<string>();

        [Serializable]
        private class SaveEntry
        {
            public string id;
            public float value;
            public int lastDay;
            public int flagsDay;
            public bool talked;
            public bool dedicated;
            public bool bartered;
            public bool givenGift;
            public int talkCount;
            public float gained;
            public float lost;
            public int lastGift;
            public int lastHelp;
            public int lastDecay;
        }
        [Serializable] private class SaveData { public List<SaveEntry> entries = new List<SaveEntry>(); }

        private SaveData pendingLoad;

        private void Awake()
        {
            Instance = this;
            SanitizeSettings();
            if (loadSavedOnStart) pendingLoad = LoadFromPrefs();
        }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += HandleDayChanged;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            if (Instance == this) Instance = null;
        }

        private void OnApplicationQuit() { Save(); }

        private void SanitizeSettings()
        {
            if (tierGainMultiplier == null || tierGainMultiplier.Length != 4) tierGainMultiplier = new[] { 1.2f, 1f, 0.75f, 0.5f };
            if (barterPriceMultiplier == null || barterPriceMultiplier.Length != 4) barterPriceMultiplier = new[] { 1.5f, 1f, 0.85f, 0.7f };
            talkFreeRepeats = Mathf.Max(0, talkFreeRepeats);
            giftCooldownDays = Mathf.Max(0, giftCooldownDays);
            helpCooldownDays = Mathf.Max(0, helpCooldownDays);
        }

        private void Update()
        {
            if (!debugKeysEnabled || GameState.InputLocked) return;

            if (GameInput.DebugRelationResetPressed)
            {
                ResetAll();
                Notifier.Show("[DEBUG] Semua hubungan sosial dikembalikan ke nilai awal.");
                return;
            }

            float delta = 0f;
            if (GameInput.DebugRelationUpPressed) delta = 10f;
            else if (GameInput.DebugRelationDownPressed) delta = -10f;
            if (delta == 0f) return;

            var npc = Interactor.Instance != null ? Interactor.Instance.Current as NpcController : null;
            if (npc == null) { Notifier.Show("[DEBUG] Dekati warga dulu."); return; }
            Modify(npc.NpcId, delta, "debug");
            Notifier.Show("[DEBUG] " + npc.npcName + ": " + Describe(npc.NpcId));
        }

        private void HandleDayChanged(int day)
        {
            foreach (var e in entries.Values)
            {
                ResetDailyFlags(e, day);
                ApplyNeglect(e, day);
            }
            Save();
        }

        private static void ResetDailyFlags(RelationshipEntry e, int day)
        {
            e.TalkedToday = false;
            e.DedicatedToday = false;
            e.BarteredToday = false;
            e.GivenGiftToday = false;
            e.TalkCountToday = 0;
            e.GainedToday = 0f;
            e.LostToday = 0f;
            e.FlagsDay = day;
        }

        /// <summary>Penurunan karena diabaikan. Dihitung dari selisih hari nyata, jadi benar juga setelah time skip.</summary>
        private void ApplyNeglect(RelationshipEntry e, int day)
        {
            if (neglectGraceDays <= 0 || neglectLossPerDay <= 0f) return;
            if (e.LastInteractionDay <= 0) return; // belum pernah berinteraksi: tidak ada yang "diabaikan"

            int firstPenaltyDay = e.LastInteractionDay + neglectGraceDays;
            int from = Mathf.Max(e.LastDecayDay, firstPenaltyDay);
            int lostDays = day - from;
            e.LastDecayDay = day;
            if (lostDays <= 0 || e.Value <= neglectFloor) return;

            float loss = lostDays * neglectLossPerDay * Mathf.Max(0f, e.Profile.neglectMultiplier);
            float target = Mathf.Max(neglectFloor, e.Value - loss);
            ApplyDelta(e, target - e.Value, "diabaikan " + (day - e.LastInteractionDay) + " hari", false);
        }

        // ================= API =================

        public static string DefaultId(string npcName) { return npcName; }

        /// <summary>Daftarkan NPC tanpa watak khusus (kompatibel dengan versi lama).</summary>
        public RelationshipEntry Register(string id, string displayName) { return Register(id, displayName, null); }

        /// <summary>Daftarkan NPC (dipanggil NpcController). Aman dipanggil berulang.</summary>
        public RelationshipEntry Register(string id, string displayName, SocialProfile profile)
        {
            RelationshipEntry e;
            if (entries.TryGetValue(id, out e))
            {
                e.DisplayName = displayName;
                if (profile != null) e.Profile = profile;
                return e;
            }

            var p = profile ?? new SocialProfile { startingValue = startingValue };
            e = new RelationshipEntry { Id = id, DisplayName = displayName, Profile = p, Value = Mathf.Clamp(p.startingValue, minValue, maxValue) };

            var time = TimeManager.Instance;
            int today = time != null ? time.Day : 1;
            e.FlagsDay = today;

            if (pendingLoad != null)
            {
                foreach (var s in pendingLoad.entries)
                {
                    if (s.id != id) continue;
                    ApplySaved(e, s, today);
                    break;
                }
            }
            entries[id] = e;
            order.Add(id);
            return e;
        }

        private void ApplySaved(RelationshipEntry e, SaveEntry s, int today)
        {
            e.Value = Mathf.Clamp(s.value, minValue, maxValue);
            // Hanya hubungan yang disimpan, bukan kalender permainan. Jika data berasal dari hari yang "belum terjadi"
            // di permainan ini (mis. permainan dimulai ulang dari Hari 1), hari-hari itu dianggap hari ini agar tidak ada
            // penurunan atau hitung mundur yang aneh.
            e.LastInteractionDay = s.lastDay > today ? today : s.lastDay;
            e.LastDecayDay = s.lastDecay > today ? today : s.lastDecay;
            e.LastGiftDay = s.lastGift > today ? 0 : s.lastGift;
            e.LastHelpDay = s.lastHelp > today ? 0 : s.lastHelp;

            if (s.flagsDay == today)
            {
                e.TalkedToday = s.talked;
                e.DedicatedToday = s.dedicated;
                e.BarteredToday = s.bartered;
                e.GivenGiftToday = s.givenGift;
                e.TalkCountToday = s.talkCount;
                e.GainedToday = s.gained;
                e.LostToday = s.lost;
            }
        }

        public RelationshipEntry Get(string id)
        {
            RelationshipEntry e;
            return (id != null && entries.TryGetValue(id, out e)) ? e : null;
        }

        public float GetValue(string id)
        {
            var e = Get(id);
            return e != null ? e.Value : startingValue;
        }

        public RelationshipTier TierOf(float value)
        {
            if (value >= dekatThreshold) return RelationshipTier.Dekat;
            if (value >= temanThreshold) return RelationshipTier.Teman;
            if (value >= kenalanThreshold) return RelationshipTier.Kenalan;
            return RelationshipTier.Dingin;
        }

        public RelationshipTier GetTier(string id) { return TierOf(GetValue(id)); }

        public bool TierAtLeast(string id, RelationshipTier tier) { return GetTier(id) >= tier; }

        public IReadOnlyList<string> RegisteredIds { get { return order; } }

        private static int Today()
        {
            var time = TimeManager.Instance;
            return time != null ? time.Day : 1;
        }

        private void Touch(RelationshipEntry e) { e.LastInteractionDay = Today(); }

        /// <summary>Ubah nilai tanpa batas harian (dipakai debug dan sistem lain). Mengembalikan perubahan yang terjadi.</summary>
        public float Modify(string id, float delta, string reason)
        {
            var e = Get(id);
            if (e == null || Mathf.Approximately(delta, 0f)) return 0f;
            return ApplyDelta(e, delta, reason, true);
        }

        private float ApplyDelta(RelationshipEntry e, float delta, string reason, bool touch)
        {
            if (Mathf.Approximately(delta, 0f)) return 0f;

            float old = e.Value;
            RelationshipTier oldTier = TierOf(old);
            e.Value = Mathf.Clamp(e.Value + delta, minValue, maxValue);
            if (touch) Touch(e);

            float actual = e.Value - old;
            if (Mathf.Approximately(actual, 0f)) return 0f;

            DayReportSystem.LogRelationship(e.DisplayName + " " + (actual > 0 ? "+" : "") + actual.ToString("0.#") + " (" + reason + ")");

            RelationshipTier newTier = TierOf(e.Value);
            if (newTier != oldTier)
            {
                DayReportSystem.LogRelationship(e.DisplayName + " kini " + DescribeTier(newTier));
                Notifier.Show("Hubunganmu dengan " + e.DisplayName + (newTier > oldTier ? " naik menjadi " : " turun menjadi ") + DescribeTier(newTier) + ".");
            }

            if (OnChanged != null) OnChanged(e.Id, old, e.Value);
            Save();
            return actual;
        }

        /// <summary>Kenaikan dari interaksi: dikali watak NPC dan faktor tingkat, lalu dibatasi dailyGainCap.</summary>
        private float Gain(RelationshipEntry e, float baseGain, string reason)
        {
            Touch(e);
            float mult = Mathf.Max(0f, e.Profile.gainMultiplier) * tierGainMultiplier[(int)TierOf(e.Value)];
            float amount = baseGain * mult;
            float room = Mathf.Max(0f, dailyGainCap - e.GainedToday);
            amount = Mathf.Min(amount, room);
            if (amount < 0.05f) return 0f;

            float actual = ApplyDelta(e, amount, reason, true);
            e.GainedToday += Mathf.Max(0f, actual);
            return actual;
        }

        private float Loss(RelationshipEntry e, float baseLoss, string reason)
        {
            Touch(e);
            float room = Mathf.Max(0f, dailyLossCap - e.LostToday);
            float amount = Mathf.Min(baseLoss, room);
            if (amount < 0.05f) return 0f;

            float actual = ApplyDelta(e, -amount, reason, true);
            e.LostToday += Mathf.Max(0f, -actual);
            return actual;
        }

        // ---------- Interaksi ----------

        /// <summary>Percakapan. Pertama kali hari ini menambah nilai; terlalu sering dalam sehari mengurangi nilai. Mengembalikan perubahan.</summary>
        public float OnTalked(string id)
        {
            var e = Get(id);
            if (e == null) return 0f;

            e.TalkCountToday++;
            if (!e.TalkedToday)
            {
                e.TalkedToday = true;
                return Gain(e, talkGain, "mengobrol");
            }

            Touch(e);
            if (e.TalkCountToday > 1 + talkFreeRepeats) return Loss(e, talkSpamPenalty, "terlalu sering mengajak bicara");
            return 0f;
        }

        /// <summary>True bila percakapan berikutnya hari ini akan dianggap mengganggu (NPC mulai kesal).</summary>
        public bool IsAnnoyed(string id)
        {
            var e = Get(id);
            return e != null && e.TalkCountToday >= 1 + talkFreeRepeats;
        }

        /// <summary>Menghabiskan waktu khusus bersama NPC (memakai satu sesi).</summary>
        public float OnDedicatedTime(string id)
        {
            var e = Get(id);
            if (e == null) return 0f;
            Touch(e);
            if (e.DedicatedToday) return 0f;
            e.DedicatedToday = true;
            return Gain(e, dedicatedTimeGain, "waktu bersama");
        }

        public bool AlreadySpentTimeToday(string id)
        {
            var e = Get(id);
            return e != null && e.DedicatedToday;
        }

        /// <summary>Barter selesai. Selalu menyegarkan "terakhir berinteraksi", kenaikan nilai sekali per hari.</summary>
        public float OnBartered(string id)
        {
            var e = Get(id);
            if (e == null) return 0f;
            Touch(e);
            if (e.BarteredToday) return 0f;
            e.BarteredToday = true;
            return Gain(e, barterGain, "barter");
        }

        /// <summary>Apakah warga masih mau menerima hadiah positif hari ini? (maksimal satu per hari)</summary>
        public bool CanAcceptPositiveGift(string id)
        {
            var e = Get(id);
            return e != null && !e.GivenGiftToday;
        }

        /// <summary>
        /// Pemain memberi hadiah. Basi dan tidak disukai MENURUNKAN hubungan; Suka/Biasa menaikkan (sekali per hari, lewat batas harian).
        /// Mengembalikan perubahan nilai. Pemanggil bertanggung jawab memindahkan barangnya.
        /// </summary>
        public float OnGifted(string id, GiftReaction reaction)
        {
            var e = Get(id);
            if (e == null) return 0f;

            switch (reaction)
            {
                case GiftReaction.Basi: return Loss(e, giftSpoiledPenalty, "memberi barang basi");
                case GiftReaction.TidakSuka: return Loss(e, giftDislikedPenalty, "hadiah tidak disukai");
            }

            Touch(e);
            if (e.GivenGiftToday) return 0f;
            e.GivenGiftToday = true;
            return Gain(e, reaction == GiftReaction.Suka ? giftLikedGain : giftNeutralGain, reaction == GiftReaction.Suka ? "hadiah kesukaan" : "hadiah");
        }

        /// <summary>
        /// Muat ulang data hubungan dari penyimpanan ke NPC yang sudah terdaftar (dipakai GameSaveSystem setelah hari permainan
        /// dipulihkan, supaya hari terakhir interaksi dibandingkan dengan hari yang benar).
        /// </summary>
        public void ReloadFromSave()
        {
            pendingLoad = LoadFromPrefs();
            if (pendingLoad == null) return;
            int today = Today();
            foreach (var e in entries.Values)
            {
                foreach (var s in pendingLoad.entries)
                {
                    if (s.id != e.Id) continue;
                    ResetDailyFlags(e, today);
                    ApplySaved(e, s, today);
                    break;
                }
            }
        }

        // ---------- Dampak gameplay per tingkat ----------

        /// <summary>Pengali harga barter untuk NPC ini (tingkat hubungan).</summary>
        public float BarterPriceMultiplier(string id)
        {
            return barterPriceMultiplier[(int)GetTier(id)];
        }

        /// <summary>Apakah NPC mau barter sama sekali? Mengisi alasan bila menolak.</summary>
        public bool CanBarter(string id, out string reason)
        {
            reason = null;
            var e = Get(id);
            if (e != null && e.Value < refuseBarterBelow)
            {
                reason = e.DisplayName + " menolak barter denganmu. Perbaiki hubungan dulu (ngobrol, jangan terlalu sering mengganggu).";
                return false;
            }
            return true;
        }

        public bool WillingToSpendTime(string id) { return GetTier(id) >= spendTimeMinTier; }

        private static int DaysSince(int lastDay, int today)
        {
            if (lastDay <= 0 || lastDay > today) return int.MaxValue;
            return today - lastDay;
        }

        public bool CanReceiveGift(string id)
        {
            var e = Get(id);
            return e != null && GetTier(id) >= giftMinTier && DaysSince(e.LastGiftDay, Today()) >= giftCooldownDays;
        }

        public void MarkGift(string id)
        {
            var e = Get(id);
            if (e == null) return;
            e.LastGiftDay = Today();
            Save();
        }

        public bool CanOfferHelp(string id)
        {
            var e = Get(id);
            return e != null && GetTier(id) >= helpMinTier && DaysSince(e.LastHelpDay, Today()) >= helpCooldownDays;
        }

        public void MarkHelp(string id)
        {
            var e = Get(id);
            if (e == null) return;
            e.LastHelpDay = Today();
            Save();
        }

        /// <summary>Hari tersisa sampai hadiah/bantuan berikutnya bisa diberikan (0 = sudah bisa).</summary>
        public int DaysUntilGift(string id)
        {
            var e = Get(id);
            if (e == null) return 0;
            int since = DaysSince(e.LastGiftDay, Today());
            return since == int.MaxValue ? 0 : Mathf.Max(0, giftCooldownDays - since);
        }

        public int DaysUntilHelp(string id)
        {
            var e = Get(id);
            if (e == null) return 0;
            int since = DaysSince(e.LastHelpDay, Today());
            return since == int.MaxValue ? 0 : Mathf.Max(0, helpCooldownDays - since);
        }

        public string DescribeBenefits(RelationshipTier tier)
        {
            string price = "Harga barter x" + barterPriceMultiplier[(int)tier].ToString("0.##");
            switch (tier)
            {
                case RelationshipTier.Dingin:
                    return price + ". Tidak mau menghabiskan waktu bersama. Tidak memberi hadiah.";
                case RelationshipTier.Kenalan:
                    return price + ". Bisa menghabiskan waktu bersama.";
                case RelationshipTier.Teman:
                    return price + ". Info tambahan, hadiah berkala, barang barter khusus.";
                default:
                    return price + ". Info lengkap, tawaran bantuan, barang barter langka.";
            }
        }

        // ---------- Tampilan ----------

        public string DescribeTier(RelationshipTier tier)
        {
            switch (tier)
            {
                case RelationshipTier.Dingin: return "Dingin";
                case RelationshipTier.Kenalan: return "Kenalan";
                case RelationshipTier.Teman: return "Teman";
                default: return "Dekat";
            }
        }

        public string Describe(string id)
        {
            var e = Get(id);
            if (e == null) return "";
            return DescribeTier(TierOf(e.Value)) + " " + Mathf.RoundToInt(e.Value);
        }

        private static string TierColor(RelationshipTier tier)
        {
            switch (tier)
            {
                case RelationshipTier.Dingin: return "#8FB8FF";
                case RelationshipTier.Kenalan: return "#E0E0E0";
                case RelationshipTier.Teman: return "#9EE7A0";
                default: return "#FFD166";
            }
        }

        private float NextThreshold(RelationshipTier tier)
        {
            switch (tier)
            {
                case RelationshipTier.Dingin: return kenalanThreshold;
                case RelationshipTier.Kenalan: return temanThreshold;
                case RelationshipTier.Teman: return dekatThreshold;
                default: return maxValue;
            }
        }

        private float CurrentThreshold(RelationshipTier tier)
        {
            switch (tier)
            {
                case RelationshipTier.Dingin: return minValue;
                case RelationshipTier.Kenalan: return kenalanThreshold;
                case RelationshipTier.Teman: return temanThreshold;
                default: return dekatThreshold;
            }
        }

        public string BuildListText()
        {
            var sb = new StringBuilder();
            if (order.Count == 0) return "(belum ada warga yang dikenal)";
            foreach (var id in order)
            {
                var e = entries[id];
                var tier = TierOf(e.Value);
                sb.Append("<b>" + e.DisplayName + "</b>  <color=" + TierColor(tier) + ">" + DescribeTier(tier) + "</color>  " + Mathf.RoundToInt(e.Value));

                if (tier != RelationshipTier.Dekat)
                {
                    float lo = CurrentThreshold(tier);
                    float hi = NextThreshold(tier);
                    float t = hi > lo ? Mathf.Clamp01((e.Value - lo) / (hi - lo)) : 1f;
                    int filled = Mathf.RoundToInt(t * 10f);
                    sb.Append("   [" + new string('#', filled) + new string('-', 10 - filled) + "] " + DescribeTier(tier + 1) + " " + Mathf.RoundToInt(hi));
                }
                sb.Append("\n   <size=17>" + DescribeBenefits(tier));
                if (e.LastInteractionDay > 0) sb.Append("  Terakhir: Hari " + e.LastInteractionDay + ".");
                if (tier >= giftMinTier && DaysUntilGift(id) > 0) sb.Append(" Hadiah lagi " + DaysUntilGift(id) + " hari.");
                if (tier >= helpMinTier && DaysUntilHelp(id) > 0) sb.Append(" Bantuan lagi " + DaysUntilHelp(id) + " hari.");
                sb.Append("</size>\n");
            }
            return sb.ToString();
        }

        // ================= Penyimpanan =================

        public void ResetAll()
        {
            int today = Today();
            foreach (var e in entries.Values)
            {
                e.Value = Mathf.Clamp(e.Profile.startingValue, minValue, maxValue);
                e.LastInteractionDay = 0;
                e.LastGiftDay = 0;
                e.LastHelpDay = 0;
                e.LastDecayDay = 0;
                ResetDailyFlags(e, today);
            }
            Save();
        }

        public void Save()
        {
            if (!saveToPlayerPrefs) return;
            try
            {
                var data = new SaveData();
                foreach (var id in order)
                {
                    var e = entries[id];
                    data.entries.Add(new SaveEntry
                    {
                        id = e.Id, value = e.Value, lastDay = e.LastInteractionDay,
                        flagsDay = e.FlagsDay, talked = e.TalkedToday, dedicated = e.DedicatedToday, bartered = e.BarteredToday, givenGift = e.GivenGiftToday,
                        talkCount = e.TalkCountToday, gained = e.GainedToday, lost = e.LostToday,
                        lastGift = e.LastGiftDay, lastHelp = e.LastHelpDay, lastDecay = e.LastDecayDay
                    });
                }
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Gagal menyimpan hubungan sosial: " + ex.Message);
            }
        }

        private SaveData LoadFromPrefs()
        {
            try
            {
                if (!PlayerPrefs.HasKey(SaveKey)) return null;
                return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Gagal memuat hubungan sosial: " + ex.Message);
                return null;
            }
        }

        public static void ClearSavedData()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
