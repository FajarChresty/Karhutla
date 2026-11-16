using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Layar "Beri hadiah" ke warga (tombol H di dekat warga). Pemain memilih barang dari inventaris.
    ///
    ///  - Angka 1-9: beri 1 unit barang itu. Esc/Q: tutup.
    ///  - Reaksi warga: Suka (barang kesukaan), Biasa, TidakSuka, Basi (barang kedaluwarsa).
    ///    Suka/Biasa menaikkan hubungan (maksimal satu hadiah positif per warga per hari, lewat batas kenaikan harian).
    ///    TidakSuka dan Basi MENURUNKAN hubungan (selalu diterima, dibatasi dailyLossCap).
    ///  - Hadiah positif kedua di hari yang sama ditolak dan barangnya TIDAK berpindah.
    ///  - Barang benar-benar keluar dari inventaris. Bila warga punya BarterTrader dan menawarkan barang yang sama, stoknya bertambah.
    ///  - Memakai jatah waktu menurut TimeManager.costs.giftSections (default 0, dianggap bagian percakapan) lewat TimeActivityManager.
    ///  - Selera warga hanya ditampilkan bila hubungan sudah Teman ke atas.
    /// Tidak ada uang yang berpindah.
    /// </summary>
    public class GiftManager : MonoBehaviour, IMenuScreen
    {
        public static GiftManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public string Title { get { return IsOpen && npc != null ? "BERI HADIAH ke " + npc.npcName : ""; } }
        public string Body { get { return BuildBody(); } }
        public string Footer { get { return "[1-9] beri 1 unit   [Esc/Q] tutup"; } }

        private struct Entry
        {
            public ItemDefinition Def;
            public bool Expired;
            public int Count;
        }

        private NpcController npc;
        private readonly List<Entry> entries = new List<Entry>();
        private int openedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            // Pengaman untuk scene lama yang belum punya GiftManager.
            if (FindFirstObjectByType<GiftManager>() != null) return;
            if (FindFirstObjectByType<NpcController>() == null) return;
            new GameObject("GiftManager").AddComponent<GiftManager>();
        }

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (IsOpen) GameState.SetInputLocked(false);
            MenuScreenRegistry.Release(this);
            if (Instance == this) Instance = null;
        }

        /// <summary>Mulai memberi hadiah: bayar waktu (bila ada biayanya), lalu buka layar.</summary>
        public void Open(NpcController target)
        {
            if (IsOpen || target == null || Inventory.Instance == null || !MenuScreenRegistry.IsFree(this)) return;

            var time = TimeManager.Instance;
            var activities = TimeActivityManager.Instance;
            TimeCost cost = time != null ? time.CostFromSections(time.costs.giftSections) : TimeCost.Free;

            if (activities == null || cost.IsFree)
            {
                ShowScreen(target);
                return;
            }
            activities.Request("Beri hadiah ke " + target.npcName, cost, afterSpend: () => ShowScreen(target));
        }

        private void ShowScreen(NpcController target)
        {
            if (IsOpen || target == null || !MenuScreenRegistry.IsFree(this)) return;
            npc = target;
            Rebuild();
            IsOpen = true;
            openedFrame = Time.frameCount;
            MenuScreenRegistry.Active = this;
            GameState.SetInputLocked(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            npc = null;
            entries.Clear();
            MenuScreenRegistry.Release(this);
            GameState.SetInputLocked(false);
        }

        private void Rebuild()
        {
            entries.Clear();
            var inv = Inventory.Instance;
            if (inv == null) return;
            foreach (var def in ItemDatabase.All)
            {
                int fresh = inv.Container.CountFresh(def.Id);
                int bad = inv.Container.CountExpired(def.Id);
                if (fresh > 0) entries.Add(new Entry { Def = def, Expired = false, Count = fresh });
                if (bad > 0) entries.Add(new Entry { Def = def, Expired = true, Count = bad });
            }
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            if (npc == null) { Close(); return; }
            if (GameInput.CancelPressed) { Close(); return; }

            for (int n = 1; n <= 9; n++)
            {
                if (GameInput.NumberPressed(n) && n <= entries.Count) Give(entries[n - 1]);
            }
        }

        private void Give(Entry entry)
        {
            var inv = Inventory.Instance;
            var rel = RelationshipSystem.Instance;
            if (inv == null || rel == null || npc == null) return;

            GiftReaction reaction = npc.ReactionTo(entry.Def.Id, entry.Expired);
            bool positive = reaction == GiftReaction.Suka || reaction == GiftReaction.Biasa;

            // Hadiah positif hanya sekali per hari. Ditolak SEBELUM barang berpindah.
            if (positive && !rel.CanAcceptPositiveGift(npc.NpcId))
            {
                Notifier.Show(npc.npcName + " sudah menerima hadiah darimu hari ini. Besok lagi, ya.");
                return;
            }

            // Keluarkan satu unit dari inventaris (basi: ambil dari batch basi).
            bool removed = false;
            if (entry.Expired)
            {
                foreach (var batch in inv.Container.Batches)
                {
                    if (batch.Id == entry.Def.Id && batch.IsExpired)
                    {
                        removed = inv.Container.Take(batch, 1) != null;
                        break;
                    }
                }
            }
            else
            {
                removed = inv.Container.RemoveFresh(entry.Def.Id, 1);
            }
            if (!removed) { Notifier.Show("Barang tidak tersedia lagi."); Rebuild(); return; }

            float change = rel.OnGifted(npc.NpcId, reaction);
            npc.ShowGiftReply(reaction, entry.Def.DisplayName);
            if (Mathf.Abs(change) >= 0.05f)
                Notifier.Show("Hubungan dengan " + npc.npcName + " " + (change > 0f ? "+" : "") + change.ToString("0.#") + "  (" + rel.Describe(npc.NpcId) + ")");

            // Warga yang menawarkan barang yang sama ikut bertambah stoknya (hanya barang layak).
            var trader = npc.GetComponent<BarterTrader>();
            if (trader != null && !entry.Expired) trader.ReceiveGoods(entry.Def.Id, 1);

            DayReportSystem.LogActivity("Memberi " + entry.Def.DisplayName + (entry.Expired ? " (basi)" : "") + " ke " + npc.npcName);
            Rebuild();
        }

        private string BuildBody()
        {
            if (!IsOpen || npc == null) return "";
            var rel = RelationshipSystem.Instance;
            var sb = new StringBuilder();
            sb.Append("<size=26>");

            string relText = rel != null ? rel.Describe(npc.NpcId) : "";
            bool knowsTaste = rel != null && rel.GetTier(npc.NpcId) >= RelationshipTier.Teman;
            sb.Append("Hubungan: <b>" + relText + "</b>\n");
            if (rel != null && !rel.CanAcceptPositiveGift(npc.NpcId))
                sb.Append("<color=#FFD166>Hari ini " + npc.npcName + " sudah menerima hadiah. Hanya barang tidak disukai/basi yang masih bisa diberikan (menurunkan hubungan).</color>\n");
            sb.Append("<size=20>" + (knowsTaste ? "Kamu sudah cukup kenal selera " + npc.npcName + "." : "Selera " + npc.npcName + " belum kamu ketahui (terbuka di hubungan Teman).") + "</size>\n\n");

            if (entries.Count == 0)
            {
                sb.Append("(kamu tidak membawa barang untuk diberikan)\n");
            }
            for (int i = 0; i < entries.Count && i < 9; i++)
            {
                var e = entries[i];
                string tag = "";
                if (e.Expired) tag = "  <color=#FF6B5A>BASI</color>";
                else if (knowsTaste)
                {
                    var r = npc.ReactionTo(e.Def.Id, false);
                    if (r == GiftReaction.Suka) tag = "  <color=#7CFC8A>disukai</color>";
                    else if (r == GiftReaction.TidakSuka) tag = "  <color=#FF6B5A>tidak disukai</color>";
                }
                sb.Append("[" + (i + 1) + "] " + e.Def.DisplayName + "   x" + e.Count + tag + "\n");
            }

            sb.Append("</size>");
            return sb.ToString();
        }
    }
}
