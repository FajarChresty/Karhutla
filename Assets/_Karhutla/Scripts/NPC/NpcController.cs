using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// NPC dummy: berjalan acak di sekitar titik awalnya, berhenti dan menghadap pemain saat diajak bicara.
    /// Dialog berbeda tergantung kondisi: ada kebakaran (Waspada ke atas), malam hari, atau normal.
    ///
    /// Tahap 2: terhubung ke RelationshipSystem (hubungan sosial) dan BarterTrader (barter saat bencana).
    ///
    /// ASUMSI PROTOTYPE: isi dialog dan hadiah masker hanyalah contoh, bukan naskah final.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NpcController : InteractableBase
    {
        public string npcName = "Warga";
        [Tooltip("ID unik untuk sistem hubungan sosial. Kosong = memakai npcName.")]
        public string npcId = "";

        [Header("Dialog")]
        [TextArea(2, 4)] public string[] normalLines;
        [TextArea(2, 4)] public string[] nightLines;
        [TextArea(2, 4)] public string[] dangerLines;
        [Tooltip("Dipakai bila hubungan sudah setingkat Teman atau lebih (dan tidak sedang bahaya/malam). Kosong = tidak dipakai.")]
        [TextArea(2, 4)] public string[] friendlyLines;

        [Header("Dialog menurut hubungan sosial (kosong = memakai dialog biasa)")]
        [Tooltip("Dipakai bila hubungan Dingin (dan tidak sedang bahaya/malam).")]
        [TextArea(2, 4)] public string[] coldLines;
        [Tooltip("Dipakai bila hubungan Dekat (dan tidak sedang bahaya/malam). Kosong = memakai friendlyLines.")]
        [TextArea(2, 4)] public string[] closeLines;
        [Tooltip("Dipakai saat pemain mengajak bicara terlalu sering dalam sehari. Kosong = kalimat bawaan.")]
        [TextArea(2, 4)] public string[] annoyedLines;
        [Tooltip("Cerita khusus setelah 'Habiskan waktu bersama' pada hubungan Teman.")]
        [TextArea(2, 4)] public string[] bondingLines;
        [Tooltip("Cerita khusus setelah 'Habiskan waktu bersama' pada hubungan Dekat. Kosong = memakai bondingLines.")]
        [TextArea(2, 4)] public string[] closeBondingLines;

        [Header("Info tambahan (hubungan Teman ke atas, data nyata dari game)")]
        [Tooltip("Menambahkan info kondisi api (jarak, arah, saran) ke dialog.")]
        public bool sharesFireInfo = false;
        [Tooltip("Menambahkan info persediaan barter warga ini ke dialog.")]
        public bool sharesStockInfo = false;

        [Header("Hubungan sosial")]
        [Tooltip("Watak warga ini: nilai awal dan laju hubungan. Tiap warga berbeda.")]
        public SocialProfile social = new SocialProfile();

        [Header("Selera hadiah dari pemain (id item dari ItemDatabase)")]
        [Tooltip("Barang kesukaan: hubungan naik lebih banyak.")]
        public string[] likedItemIds;
        [Tooltip("Barang yang tidak disukai: hubungan turun.")]
        public string[] dislikedItemIds;

        [Header("Hadiah (opsional)")]
        [Tooltip("Item yang diberikan sekali setelah dialog saat ada bahaya kebakaran (butuh hubungan minimal Kenalan). Kosongkan jika tidak ada.")]
        public string giftItemId = "";
        [Tooltip("Hadiah berkala setelah ngobrol bila hubungan Teman. Kosong = tidak ada.")]
        public string friendGiftItemId = "";
        [Tooltip("Hadiah berkala setelah ngobrol bila hubungan Dekat. Kosong = memakai friendGiftItemId.")]
        public string closeGiftItemId = "";

        [Header("Bantuan (hubungan Dekat)")]
        [Tooltip("Barang yang ditawarkan warga sebagai bantuan setelah ngobrol bila hubungan Dekat. Kosong = tidak ada.")]
        public string helpItemId = "";
        public int helpItemCount = 1;

        [Header("Gerakan")]
        public float wanderRadius = 5f;
        public float moveSpeed = 1.6f;
        public float minIdle = 1.5f;
        public float maxIdle = 4f;

        private Vector3 home;
        private Vector3 target;
        private float idleTimer;
        private bool talking;
        private bool giftGiven;
        private bool giftPending;
        private bool lastTalkAnnoyed;
        private BarterTrader barter;
        private WorldLabel worldLabel;

        public string NpcId { get { return string.IsNullOrEmpty(npcId) ? npcName : npcId; } }
        public Vector3 HomePosition { get { return home; } set { home = value; } }
        public string CurrentLocationName { get; private set; } = "";
        public string CurrentActivity { get; private set; } = "";

        /// <summary>
        /// Mengatur tujuan jadwal NPC (dipanggil NpcSchedule).
        /// immediate = true jika waktu melompat (teleport instan).
        /// </summary>
        public void SetScheduleTarget(Vector3 dest, float radius, string locName, string activity, bool immediate = false)
        {
            home = dest;
            wanderRadius = Mathf.Max(0.5f, radius);
            CurrentLocationName = locName;
            CurrentActivity = activity;

            if (immediate)
            {
                transform.position = new Vector3(dest.x, transform.position.y, dest.z);
                target = transform.position;
            }
            else
            {
                target = new Vector3(dest.x, transform.position.y, dest.z);
            }
        }

        private void Awake()
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
        }

        private void Start()
        {
            home = transform.position;
            target = transform.position;
            idleTimer = Random.Range(minIdle, maxIdle);
            WorldLabel.Create(transform, new Vector3(0f, 1.7f, 0f), npcName, new Color(1f, 0.95f, 0.6f), 0.8f);

            barter = GetComponent<BarterTrader>();
            if (RelationshipSystem.Instance != null) RelationshipSystem.Instance.Register(NpcId, npcName, social);
        }

        private void Update()
        {
            if (talking)
            {
                FacePlayer();
                return;
            }

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude > 0.1f)
            {
                Vector3 dir = toTarget.normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                transform.forward = Vector3.Slerp(transform.forward, dir, 8f * Time.deltaTime);
                return;
            }

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                PickNewTarget();
                idleTimer = Random.Range(minIdle, maxIdle);
            }
        }

        private void PickNewTarget()
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                Vector3 candidate = new Vector3(home.x + offset.x, transform.position.y, home.z + offset.y);
                Vector3 dir = candidate - transform.position;

                // Jangan berjalan menembus objek (cek sederhana dengan raycast).
                if (!Physics.Raycast(transform.position, dir.normalized, dir.magnitude + 0.6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    target = candidate;
                    return;
                }
            }
        }

        private void FacePlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 dir = player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
            }
        }

        public override string GetPrompt()
        {
            string rel = RelationshipSystem.Instance != null ? RelationshipSystem.Instance.Describe(NpcId) : "";
            string act = !string.IsNullOrEmpty(CurrentActivity) ? "  [" + CurrentActivity + "]" : "";
            return "Bicara dengan " + npcName + (rel.Length > 0 ? "  (" + rel + ")" : "") + act;
        }

        public override bool CanInteract() { return !talking; }

        // [E] Percakapan biasa. GDD 4.1: tidak memakai sesi waktu (biaya diatur di TimeManager.costs).
        public override void Interact()
        {
            if (talking || DialogueManager.Instance == null) return;

            var time = TimeManager.Instance;
            var activities = TimeActivityManager.Instance;
            TimeCost cost = time != null ? time.CostFromSections(time.costs.conversationSections) : TimeCost.Free;

            if (activities == null || cost.IsFree)
            {
                StartConversation();
                return;
            }

            activities.Request("Bicara dengan " + npcName, cost, afterSpend: StartConversation);
        }

        // [Q] Menghabiskan waktu khusus bersama karakter. GDD 4.1: memakai satu sesi (lewat TimeActivityManager).
        // Hanya warga yang hubungannya cukup (bukan Dingin) mau diajak. Pada Teman/Dekat ada cerita khusus setelahnya.
        public override string GetSecondaryPrompt()
        {
            if (talking) return null;
            var rel = RelationshipSystem.Instance;
            if (rel != null && !rel.WillingToSpendTime(NpcId)) return npcName + " belum mau menghabiskan waktu denganmu (hubungan Dingin)";
            string note = rel != null && rel.AlreadySpentTimeToday(NpcId) ? "  - hari ini tidak menambah hubungan lagi" : "";
            return "Habiskan waktu bersama " + npcName + " (1 sesi)" + note;
        }

        // [R] Barter (hanya jika warga ini punya BarterTrader). Ketersediaan (masa bencana) dicek BarterManager.
        public override string GetTertiaryPrompt()
        {
            if (talking || barter == null) return null;
            string reason;
            bool ok = BarterManager.Instance != null && BarterManager.Instance.CanBarterWith(barter, out reason);
            if (ok) return "Barter dengan " + npcName + " (tanpa uang" + SessionNote(tm => tm.costs.barterSections) + ")";
            var rel = RelationshipSystem.Instance;
            bool refused = rel != null && !rel.CanBarter(NpcId, out reason);
            return refused ? npcName + " menolak barter denganmu (hubungan buruk)" : "Barter dengan " + npcName + " (hanya saat bencana)";
        }

        // Barter memakai jatah waktu (TimeManager.costs.barterSections, default 1 sesi). Syarat dicek DULU supaya waktu tidak
        // terpakai untuk barter yang ternyata ditolak. Satu kunjungan = satu layar barter (boleh beberapa transaksi).
        public override void InteractTertiary()
        {
            if (talking || barter == null || BarterManager.Instance == null) return;

            string reason;
            if (!BarterManager.Instance.CanBarterWith(barter, out reason)) { Notifier.Show(reason); return; }
            if (barter.offers == null || barter.offers.Count == 0) { Notifier.Show(npcName + " tidak punya barang untuk ditukar."); return; }

            var time = TimeManager.Instance;
            var activities = TimeActivityManager.Instance;
            TimeCost cost = time != null ? time.CostFromSections(time.costs.barterSections) : TimeCost.Free;

            if (activities == null || cost.IsFree)
            {
                BarterManager.Instance.Open(barter);
                return;
            }
            activities.Request("Barter dengan " + npcName, cost, afterSpend: () => { if (BarterManager.Instance != null) BarterManager.Instance.Open(barter); });
        }

        private static string SessionNote(System.Func<TimeManager, float> sections)
        {
            var time = TimeManager.Instance;
            if (time == null) return "";
            float s = sections(time);
            return s > 0f ? ", " + s.ToString("0.##") + " sesi" : "";
        }

        // [H] Memberi hadiah. Layar pilihan barang dibuka GiftManager; reaksi warga memengaruhi hubungan.
        public override string GetQuaternaryPrompt()
        {
            if (talking || GiftManager.Instance == null) return null;
            var rel = RelationshipSystem.Instance;
            string note = rel != null && !rel.CanAcceptPositiveGift(NpcId) ? "  - hari ini sudah menerima hadiah" : "";
            return "Beri hadiah ke " + npcName + SessionNote(tm => tm.costs.giftSections) + note;
        }

        public override void InteractQuaternary()
        {
            if (talking || GiftManager.Instance == null) return;
            GiftManager.Instance.Open(this);
        }

        /// <summary>Bagaimana warga ini bereaksi terhadap sebuah barang (basi lebih dulu, lalu selera warga).</summary>
        public GiftReaction ReactionTo(string itemId, bool expired)
        {
            if (expired) return GiftReaction.Basi;
            if (dislikedItemIds != null && System.Array.IndexOf(dislikedItemIds, itemId) >= 0) return GiftReaction.TidakSuka;
            if (likedItemIds != null && System.Array.IndexOf(likedItemIds, itemId) >= 0) return GiftReaction.Suka;
            return GiftReaction.Biasa;
        }

        /// <summary>Jawaban singkat warga setelah menerima hadiah (tampil sebagai notifikasi).</summary>
        public void ShowGiftReply(GiftReaction reaction, string itemName)
        {
            string line;
            switch (reaction)
            {
                case GiftReaction.Suka: line = "Wah, " + itemName + "! Ini yang aku suka. Terima kasih banyak!"; break;
                case GiftReaction.Biasa: line = "Terima kasih sudah memberiku " + itemName + "."; break;
                case GiftReaction.TidakSuka: line = itemName + "? Hmm... aku tidak terlalu suka ini."; break;
                default: line = "Ini sudah basi! Kenapa kamu memberiku " + itemName + " seperti ini?"; break;
            }
            Notifier.Show(npcName + ": \"" + line + "\"");
        }

        public override void InteractSecondary()
        {
            var time = TimeManager.Instance;
            var activities = TimeActivityManager.Instance;
            if (talking || time == null || activities == null) return;

            var rel = RelationshipSystem.Instance;
            if (rel != null && !rel.WillingToSpendTime(NpcId))
            {
                Notifier.Show(npcName + " belum mau menghabiskan waktu denganmu. Ajak ngobrol dulu agar hubungan membaik.");
                return;
            }

            TimeCost cost = time.CostFromSections(time.costs.dedicatedTimeSections);
            activities.Request(
                "Habiskan waktu bersama " + npcName,
                cost,
                afterSpend: () =>
                {
                    Notifier.Show("Kamu menghabiskan waktu bersama " + npcName + ".");
                    ApplyRelationship(rel != null ? rel.OnDedicatedTime(NpcId) : 0f);
                    StartBondingDialogue();
                });
        }

        // Cerita khusus setelah waktu bersama: hanya untuk hubungan Teman ke atas.
        private void StartBondingDialogue()
        {
            var rel = RelationshipSystem.Instance;
            if (talking || rel == null || DialogueManager.Instance == null) return;

            var tier = rel.GetTier(NpcId);
            if (tier < RelationshipTier.Teman) return;

            string[] lines = (tier >= RelationshipTier.Dekat && closeBondingLines != null && closeBondingLines.Length > 0) ? closeBondingLines : bondingLines;
            if (lines == null || lines.Length == 0) return;

            lastTalkAnnoyed = false;
            giftPending = false;
            talking = true;
            DialogueManager.Instance.Begin(SpeakerLabel(tier), lines, OnDialogueEnd);
        }

        private string SpeakerLabel(RelationshipTier tier)
        {
            return npcName + "  (" + tier + ")";
        }

        private void StartConversation()
        {
            if (talking || DialogueManager.Instance == null) return;

            var rel = RelationshipSystem.Instance;
            RelationshipTier tier = rel != null ? rel.GetTier(NpcId) : RelationshipTier.Kenalan;
            bool annoyed = rel != null && rel.IsAnnoyed(NpcId);

            string[] baseLines = ChooseLines(tier, annoyed);
            if (baseLines == null || baseLines.Length == 0) return;

            var lines = new List<string>(baseLines);
            if (!annoyed) AppendExtraInfo(lines, tier);

            if (rel != null) ApplyRelationship(rel.OnTalked(NpcId));

            talking = true;
            giftPending = false;
            lastTalkAnnoyed = annoyed;

            // Hadiah saat bahaya: hanya untuk warga yang sudah mengenalmu (bukan Dingin).
            var disaster = DisasterSystem.Instance;
            bool danger = disaster != null && disaster.CurrentDanger >= DangerLevel.Waspada;
            bool acquainted = rel == null || tier >= RelationshipTier.Kenalan;
            if (danger && acquainted && !annoyed && !giftGiven && !string.IsNullOrEmpty(giftItemId)) giftPending = true;

            DialogueManager.Instance.Begin(SpeakerLabel(tier), lines.ToArray(), OnDialogueEnd);
        }

        // Prioritas dialog: bahaya > kesal (terlalu sering diajak bicara) > malam > tingkat hubungan (Dingin/Dekat/Teman) > biasa.
        private string[] ChooseLines(RelationshipTier tier, bool annoyed)
        {
            var disaster = DisasterSystem.Instance;
            if (disaster != null && disaster.CurrentDanger >= DangerLevel.Waspada && dangerLines != null && dangerLines.Length > 0)
                return dangerLines;

            if (annoyed)
                return (annoyedLines != null && annoyedLines.Length > 0)
                    ? annoyedLines
                    : new[] { "Kita kan baru saja ngobrol tadi...", "Beri aku waktu sebentar, ya. Nanti kita bicara lagi." };

            var time = TimeManager.Instance;
            if (time != null && time.Period == TimeOfDay.Malam && nightLines != null && nightLines.Length > 0)
                return nightLines;

            if (tier == RelationshipTier.Dingin && coldLines != null && coldLines.Length > 0) return coldLines;
            if (tier == RelationshipTier.Dekat && closeLines != null && closeLines.Length > 0) return closeLines;
            if (tier >= RelationshipTier.Teman && friendlyLines != null && friendlyLines.Length > 0) return friendlyLines;

            return normalLines;
        }

        // Info tambahan untuk Teman ke atas. Isinya diambil dari kondisi game yang sebenarnya, bukan teks tetap.
        private void AppendExtraInfo(List<string> lines, RelationshipTier tier)
        {
            if (tier < RelationshipTier.Teman) return;

            if (sharesFireInfo)
            {
                var d = DisasterSystem.Instance;
                if (d == null || !d.FireStarted)
                {
                    if (tier >= RelationshipTier.Dekat) lines.Add("Belum ada api di dekat desa. Tapi saya terus memantau, kamu orang pertama yang saya kabari.");
                }
                else if (tier == RelationshipTier.Teman)
                {
                    lines.Add("Info dariku: api sekitar " + Mathf.RoundToInt(d.DistanceToFront) + " m dari tempatmu. Tingkat bahaya: " + d.CurrentDanger + ".");
                }
                else
                {
                    lines.Add("Info lengkap: api ada di arah " + d.DirectionToFire + ", sekitar " + Mathf.RoundToInt(d.DistanceToFront)
                              + " m dari tempatmu. Radius api kini kira-kira " + Mathf.RoundToInt(d.FireRadius) + " m. Tingkat bahaya: " + d.CurrentDanger + ".");
                    lines.Add(FireAdvice(d.CurrentDanger));
                }
            }

            if (sharesStockInfo && barter != null && barter.offers != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var o in barter.offers)
                {
                    if (o == null || !barter.IsUnlocked(o)) continue;
                    var def = ItemDatabase.Get(o.itemId);
                    if (def == null) continue;
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(def.DisplayName + " " + o.stock);
                }
                if (sb.Length > 0) lines.Add("Kalau mau tukar barang, persediaanku sekarang: " + sb + ".");
            }
        }

        private static string FireAdvice(DangerLevel level)
        {
            switch (level)
            {
                case DangerLevel.Aman: return "Untuk sekarang aman, tapi siapkan masker dan air.";
                case DangerLevel.Waspada: return "Siapkan masker dan jangan terlalu lelah.";
                case DangerLevel.Bahaya: return "Pakai masker terus dan menjauhlah dari arah api.";
                default: return "Ini kritis! Segera menjauh ke tempat asapnya paling tipis.";
            }
        }

        private void ApplyRelationship(float change)
        {
            var rel = RelationshipSystem.Instance;
            if (rel == null || Mathf.Abs(change) < 0.05f) return;
            Notifier.Show("Hubungan dengan " + npcName + " " + (change > 0f ? "+" : "") + change.ToString("0.#") + "  (" + rel.Describe(NpcId) + ")");
        }

        private void OnDialogueEnd()
        {
            talking = false;
            CheckInvestigationInteractions();
            GiveDangerGift();

            if (lastTalkAnnoyed) return;
            GiveRelationshipGift();
            OfferHelp();
        }

        private void CheckInvestigationInteractions()
        {
            var invSys = InvestigationSystem.Instance;
            if (invSys == null) return;

            // 1. Mas Hendra: Jurnalis pembagi kwitansi suap & partner ekspos korupsi
            if (NpcId == "Mas Hendra")
            {
                if (!invSys.HasClue(InvestigationSystem.ClueKwitansi))
                {
                    invSys.CollectClue(InvestigationSystem.ClueKwitansi);
                    Notifier.Show("Mas Hendra: \"Ini salinan buku kas aliran suap perizinan yang berhasil kutelusuri. Simpan ini untuk bukti!\"");
                }
                else if (invSys.CanExpose)
                {
                    ModalManager.Show(
                        "BONGKAR KORUPSI BERSAMA MAS HENDRA",
                        "Mas Hendra: \"Bukti dokumen ilegal kita sudah lebih dari cukup (" + invSys.ClueCount + " bukti)! "
                        + "Apakah kamu siap merilis berkas ini ke media nasional dan kejaksaan sekarang?\"",
                        "Rilis Berkas Sekarang", "Tunda Dulu",
                        choice => {
                            if (choice == 0) invSys.ExposeCorruption();
                        });
                }
            }
            // 2. Pak Tejo (Mandor Perkebunan): Catatan lapangan perintah sulutan api
            else if (NpcId == "Pak Tejo")
            {
                if (!invSys.HasClue(InvestigationSystem.ClueMandor))
                {
                    invSys.CollectClue(InvestigationSystem.ClueMandor);
                    Notifier.Show("Kamu melihat buku catatan kerja mandor yang terjatuh dari saku Pak Tejo, berisi instruksi pembakaran!");
                }
            }
        }

        private void GiveDangerGift()
        {
            if (!giftPending || giftGiven || Inventory.Instance == null) return;
            giftPending = false;
            var def = ItemDatabase.Get(giftItemId);
            if (def == null) return;

            if (Inventory.Instance.Add(giftItemId, 1))
            {
                giftGiven = true;
                Notifier.Show(npcName + " memberimu " + def.DisplayName + ".");
            }
            else
            {
                // Bawaan terlalu berat: hadiah tidak hilang, bisa diterima pada percakapan berikutnya.
                Notifier.Show("Bawaanmu terlalu berat untuk menerima " + def.DisplayName + " dari " + npcName + ".");
            }
        }

        // Hadiah berkala (Teman ke atas). Jeda antar hadiah diatur RelationshipSystem.giftCooldownDays.
        private void GiveRelationshipGift()
        {
            var rel = RelationshipSystem.Instance;
            if (rel == null || Inventory.Instance == null || !rel.CanReceiveGift(NpcId)) return;

            string itemId = (rel.GetTier(NpcId) >= RelationshipTier.Dekat && !string.IsNullOrEmpty(closeGiftItemId)) ? closeGiftItemId : friendGiftItemId;
            var def = ItemDatabase.Get(itemId);
            if (def == null) return;

            if (Inventory.Instance.Add(itemId, 1))
            {
                rel.MarkGift(NpcId);
                Notifier.Show(npcName + " memberimu " + def.DisplayName + " sebagai tanda pertemanan.");
            }
            else
            {
                Notifier.Show("Bawaanmu terlalu berat untuk menerima hadiah " + def.DisplayName + " dari " + npcName + ".");
            }
        }

        // Tawaran bantuan (hanya hubungan Dekat). Pemain boleh menolak; tawaran muncul lagi pada percakapan berikutnya.
        private void OfferHelp()
        {
            var rel = RelationshipSystem.Instance;
            if (rel == null || Inventory.Instance == null || string.IsNullOrEmpty(helpItemId) || !rel.CanOfferHelp(NpcId)) return;

            var def = ItemDatabase.Get(helpItemId);
            if (def == null) return;
            int count = Mathf.Max(1, helpItemCount);

            ModalManager.Show(
                "Bantuan dari " + npcName,
                npcName + " menawarkan bantuan karena kalian sudah dekat:\n\n<b>" + def.DisplayName + " x" + count + "</b>  (" + (def.Weight * count).ToString("0.##") + " kg)\n\n<size=22>Gratis, tanpa barter.</size>",
                "Terima bantuan", "Tidak perlu",
                choice =>
                {
                    if (choice != 0) return;
                    if (Inventory.Instance != null && Inventory.Instance.Add(helpItemId, count))
                    {
                        rel.MarkHelp(NpcId);
                        Notifier.Show(npcName + " membantumu dengan " + def.DisplayName + " x" + count + ".");
                    }
                    else
                    {
                        Notifier.Show("Bawaanmu terlalu berat untuk menerima bantuan itu. Kurangi bawaan lalu bicara lagi.");
                    }
                },
                1);
        }
    }
}
