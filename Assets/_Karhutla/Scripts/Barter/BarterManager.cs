using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Layar barter dengan warga desa (bukan transaksi uang: tidak ada uang yang berpindah).
    ///
    /// Cara pakai:
    ///  - Dekati warga yang bisa barter, tekan R.
    ///  - R                : ganti barang yang diinginkan.
    ///  - Panah Atas/Bawah : jumlah barang yang diminta.
    ///  - 1-9              : tawarkan 1 unit barangmu (hanya yang diterima warga). Shift+1-9: tarik 1 unit kembali.
    ///  - Backspace        : kosongkan tawaran.
    ///  - E                : setujui barter. Nilai tawaran harus >= nilai barang yang diminta (kelebihan tidak dikembalikan).
    ///  - Esc / Q          : tutup.
    ///
    /// Aturan:
    ///  - Hanya pada MASA BENCANA (fase kebakaran sudah dimulai atau api aktif), kecuali ignoreDisasterCondition untuk uji.
    ///  - Nilai dihitung BarterTrader (jenis x kelangkaan x kebutuhan bencana x stok x hubungan sosial).
    ///  - Barter ditolak jika nilai kurang, stok warga kurang, barang tawaran sudah tidak layak/hilang, atau barang yang diterima
    ///    membuat bawaan melebihi kapasitas tas. Dalam kasus mana pun tidak ada barang yang berpindah.
    ///  - Bila berhasil: inventaris pemain dan stok warga diperbarui bersamaan (barang tawaran keluar, barang diminta masuk,
    ///    stok warga berkurang, barang yang sama dengan tawaran warga menambah stoknya sampai batas).
    ///  - Warga menolak barter bila hubungan terlalu buruk; sebagian barang hanya ditawarkan pada hubungan tertentu.
    /// </summary>
    public class BarterManager : MonoBehaviour, IMenuScreen
    {
        public static BarterManager Instance { get; private set; }

        [Tooltip("Hanya untuk pengujian: izinkan barter walau belum masa bencana.")]
        public bool ignoreDisasterCondition = false;

        public bool IsOpen { get; private set; }
        public string Title { get { return IsOpen && trader != null ? "BARTER dengan " + trader.DisplayName + "  (tanpa uang)" : ""; } }
        public string Body { get { return BuildBody(); } }
        public string Footer { get { return "[R] ganti barang   [Atas/Bawah] jumlah   [1-9] tawarkan 1   [Shift+1-9] tarik 1   [Backspace] kosongkan   [E] setujui   [Esc/Q] tutup"; } }

        private BarterTrader trader;
        private int wantIndex;
        private int wantCount = 1;
        private readonly Dictionary<string, int> offered = new Dictionary<string, int>();
        private int openedFrame = -1;

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (IsOpen) GameState.SetInputLocked(false);
            MenuScreenRegistry.Release(this);
            if (Instance == this) Instance = null;
        }

        /// <summary>Apakah barter diizinkan sekarang menurut kondisi bencana? Mengisi alasan bila tidak.</summary>
        public bool IsBarterAvailable(out string reason)
        {
            reason = null;
            if (ignoreDisasterCondition) return true;

            var timeline = DisasterTimeline.Instance;
            var disaster = DisasterSystem.Instance;
            bool disasterPhase = (timeline != null && timeline.CurrentPhase >= StoryPhase.KebakaranMulai) || (disaster != null && disaster.FireStarted);
            if (disasterPhase) return true;

            reason = "Warga belum mau barter. Barter hanya dilakukan pada masa bencana.";
            return false;
        }

        /// <summary>Kondisi bencana DAN kemauan warga (hubungan sosial). Dipakai prompt dan Open().</summary>
        public bool CanBarterWith(BarterTrader target, out string reason)
        {
            if (!IsBarterAvailable(out reason)) return false;
            var rel = RelationshipSystem.Instance;
            if (rel != null && target != null && !rel.CanBarter(target.NpcId, out reason)) return false;
            return true;
        }

        public void Open(BarterTrader target)
        {
            if (IsOpen || target == null || !MenuScreenRegistry.IsFree(this) || Inventory.Instance == null) return;

            string reason;
            if (!CanBarterWith(target, out reason))
            {
                Notifier.Show(reason);
                return;
            }
            if (target.offers == null || target.offers.Count == 0)
            {
                Notifier.Show(target.DisplayName + " tidak punya barang untuk ditukar.");
                return;
            }

            trader = target;
            wantIndex = NextSelectable(0);
            wantCount = 1;
            ClampWantCount();
            offered.Clear();
            IsOpen = true;
            openedFrame = Time.frameCount;
            MenuScreenRegistry.Active = this;
            GameState.SetInputLocked(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            trader = null;
            offered.Clear();
            MenuScreenRegistry.Release(this);
            GameState.SetInputLocked(false);
        }

        // ================= Pemilihan barang =================

        private bool Selectable(BarterOffer o)
        {
            return o != null && o.stock > 0 && trader.IsUnlocked(o);
        }

        /// <summary>Indeks barang berikutnya yang bisa dipilih (terbuka dan ada stok). Bila tidak ada, tetap di posisi awal.</summary>
        private int NextSelectable(int from)
        {
            int n = trader.offers.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = ((from + i) % n + n) % n;
                if (Selectable(trader.offers[idx])) return idx;
            }
            return Mathf.Clamp(from, 0, n - 1);
        }

        private BarterOffer Want { get { return trader.offers[wantIndex]; } }

        private void ClampWantCount()
        {
            var want = Want;
            int max = Mathf.Max(1, want.stock);
            wantCount = Mathf.Clamp(wantCount, 1, max);
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;

            string reason;
            if (!CanBarterWith(trader, out reason))
            {
                Notifier.Show(reason);
                Close();
                return;
            }

            if (GameInput.CancelPressed) { Close(); return; }

            if (GameInput.SwitchModePressed)
            {
                wantIndex = NextSelectable(wantIndex + 1);
                wantCount = 1;
                ClampWantCount();
                offered.Remove(Want.itemId); // barang yang sama tidak boleh ditukar dengan dirinya sendiri
            }
            if (GameInput.MenuUpPressed) { wantCount++; ClampWantCount(); }
            if (GameInput.MenuDownPressed) { wantCount--; ClampWantCount(); }
            if (GameInput.ClearPressed) offered.Clear();

            var eligible = EligibleItems();
            for (int n = 1; n <= 9; n++)
            {
                if (!GameInput.NumberPressed(n) || n > eligible.Count) continue;
                string id = eligible[n - 1].Id;
                int cur;
                offered.TryGetValue(id, out cur);

                if (GameInput.SprintHeld)
                {
                    if (cur > 0) offered[id] = cur - 1;
                }
                else if (cur < Inventory.Instance.Container.CountFresh(id))
                {
                    offered[id] = cur + 1;
                }
                else
                {
                    Notifier.Show("Tidak ada lagi " + eligible[n - 1].DisplayName + " yang layak untuk ditawarkan.");
                }
            }

            if (GameInput.ConfirmPressed) TryConfirm();
        }

        /// <summary>Barang di inventaris yang diterima warga ini, masih layak, dan bukan barang yang sedang diminta.</summary>
        private List<ItemDefinition> EligibleItems()
        {
            var list = new List<ItemDefinition>();
            if (trader == null) return list;
            string wantId = Want.itemId;
            foreach (var def in ItemDatabase.All)
            {
                if (def.Id == wantId) continue;
                if (trader.Accepts(def.Id) && Inventory.Instance.Container.CountFresh(def.Id) > 0) list.Add(def);
            }
            return list;
        }

        private float OfferedValue()
        {
            float v = 0f;
            foreach (var kv in offered) v += trader.ValueOfOffered(kv.Key, kv.Value);
            return v;
        }

        private float OfferedWeight()
        {
            float w = 0f;
            foreach (var kv in offered)
            {
                var def = ItemDatabase.Get(kv.Key);
                if (def != null) w += def.Weight * kv.Value;
            }
            return w;
        }

        /// <summary>Berat bawaan setelah barter ini selesai: barang tawaran keluar, barang diminta masuk.</summary>
        private float ProjectedWeight(ItemDefinition wantDef)
        {
            return Inventory.Instance.CurrentWeight - OfferedWeight() + (wantDef != null ? wantDef.Weight * wantCount : 0f);
        }

        // ================= Transaksi =================

        private void TryConfirm()
        {
            var inv = Inventory.Instance;
            var want = Want;
            var wantDef = ItemDatabase.Get(want.itemId);
            if (wantDef == null) return;

            // 1. Barang harus tersedia pada tingkat hubungan ini dan stok cukup.
            if (!trader.IsUnlocked(want)) { Notifier.Show("Barter ditolak: " + trader.DisplayName + " belum mau menukar " + wantDef.DisplayName + " denganmu (butuh hubungan " + TierName(want.minTier) + ")."); return; }
            if (want.stock <= 0) { Notifier.Show("Barter ditolak: " + trader.DisplayName + " sudah kehabisan " + wantDef.DisplayName + "."); return; }
            if (wantCount > want.stock) { Notifier.Show("Barter ditolak: persediaan " + wantDef.DisplayName + " hanya " + want.stock + "."); wantCount = want.stock; return; }

            // 2. Keseimbangan nilai.
            float required = trader.RequiredValue(want, wantCount);
            float have = OfferedValue();
            if (have <= 0f) { Notifier.Show("Tawarkan dulu barangmu (tombol angka)."); return; }
            if (have + 0.001f < required)
            {
                Notifier.Show("Barter ditolak: nilai tawaranmu " + have.ToString("0.0") + " kurang dari " + required.ToString("0.0") + ".");
                return;
            }

            // 3. Barang tawaran harus masih ada dan layak.
            foreach (var kv in offered)
            {
                if (kv.Value > inv.Container.CountFresh(kv.Key))
                {
                    Notifier.Show("Barang tawaran berubah. Atur ulang tawaran.");
                    offered.Clear();
                    return;
                }
            }

            // 4. Kapasitas tas: dicek SEBELUM ada barang yang berpindah. Jika tidak muat, transaksi tidak diselesaikan.
            float projected = ProjectedWeight(wantDef);
            if (projected > inv.MaxWeight + 0.001f)
            {
                Notifier.Show("Tas tidak muat: bawaan akan menjadi " + projected.ToString("0.0") + " kg (batas " + inv.MaxWeight.ToString("0.0")
                              + " kg). Kurangi bawaan " + (projected - inv.MaxWeight).ToString("0.0") + " kg atau kurangi jumlah yang diminta. Barter belum diselesaikan.");
                return;
            }

            // 5. Jalankan transaksi: barang tawaran keluar dulu, lalu barang diminta masuk.
            var removed = new List<KeyValuePair<string, int>>();
            var given = new StringBuilder();
            foreach (var kv in offered)
            {
                if (kv.Value <= 0) continue;
                inv.Container.RemoveFresh(kv.Key, kv.Value);
                removed.Add(kv);
                var d = ItemDatabase.Get(kv.Key);
                if (given.Length > 0) given.Append(", ");
                given.Append(d.DisplayName + " x" + kv.Value);
            }

            if (!inv.Add(want.itemId, wantCount))
            {
                // Seharusnya tidak terjadi (berat sudah dicek). Kembalikan barang tawaran agar inventaris tidak berkurang.
                foreach (var r in removed) inv.Add(r.Key, r.Value);
                Notifier.Show("Barter dibatalkan: barang tidak bisa dimasukkan ke tas. Barangmu dikembalikan.");
                return;
            }

            // 6. Sinkronkan stok warga.
            want.stock = Mathf.Max(0, want.stock - wantCount);
            foreach (var r in removed) trader.ReceiveGoods(r.Key, r.Value);

            string result = given + " -> " + wantDef.DisplayName + " x" + wantCount;
            DayReportSystem.LogBarter(trader.DisplayName + ": " + result);

            var rel = RelationshipSystem.Instance;
            if (rel != null)
            {
                float change = rel.OnBartered(trader.NpcId);
                if (change > 0f) Notifier.Show("Hubungan dengan " + trader.DisplayName + " +" + change.ToString("0.#") + "  (" + rel.Describe(trader.NpcId) + ")");
            }

            Notifier.Show("Barter berhasil dengan " + trader.DisplayName + ": " + result + ".");
            offered.Clear();
            wantIndex = want.stock > 0 ? wantIndex : NextSelectable(wantIndex + 1);
            ClampWantCount();
        }

        // ================= Tampilan =================

        private static string TierName(RelationshipTier tier) { return tier.ToString(); }

        private string BuildBody()
        {
            if (!IsOpen || trader == null || Inventory.Instance == null) return "";
            var inv = Inventory.Instance;
            var rel = RelationshipSystem.Instance;
            var sb = new StringBuilder();
            sb.Append("<size=24>");

            // Baris status: hubungan, harga, kondisi bencana.
            var tier = trader.Tier;
            string relText = rel != null ? rel.Describe(trader.NpcId) : TierName(tier);
            float priceMult = trader.RelationshipPriceMultiplier;
            var disaster = DisasterSystem.Instance;
            string disasterText = ignoreDisasterCondition ? "mode uji"
                : (disaster != null && disaster.FireStarted ? "aktif, bahaya " + disaster.CurrentDanger : "fase kebakaran");
            sb.Append("Hubungan: <b>" + relText + "</b>   Harga hubungan: <b>x" + priceMult.ToString("0.##") + "</b>   Bencana: " + disasterText + "\n\n");

            // Barang yang bisa didapat.
            sb.Append("<b>BARANG YANG BISA KAMU DAPAT</b>   <size=20>(nilai sudah termasuk kelangkaan, stok, hubungan)</size>\n");
            for (int i = 0; i < trader.offers.Count; i++)
            {
                var o = trader.offers[i];
                var def = ItemDatabase.Get(o.itemId);
                if (def == null) continue;
                string line = def.DisplayName + "   nilai " + trader.RequiredValue(o).ToString("0.0") + "/unit   " + def.Weight.ToString("0.##") + " kg   stok " + o.stock + "/" + o.maxStock;

                if (!trader.IsUnlocked(o))
                    sb.Append("<color=#8A8A8A>    " + def.DisplayName + "   [terkunci: butuh hubungan " + TierName(o.minTier) + "]</color>\n");
                else if (o.stock <= 0)
                    sb.Append("<color=#FF6B5A>    " + def.DisplayName + "   habis</color>\n");
                else
                    sb.Append((i == wantIndex ? "<color=#FFE680>>> " : "    ") + line + (i == wantIndex ? "</color>" : "") + "\n");
            }

            var want = Want;
            var wantDef = ItemDatabase.Get(want.itemId);
            float required = trader.RequiredValue(want, wantCount);
            sb.Append("\nDiminta: <b>" + (wantDef != null ? wantDef.DisplayName : want.itemId) + " x" + wantCount + "</b>   nilai dibutuhkan <b>" + required.ToString("0.0") + "</b>\n\n");

            // Barang pemain.
            sb.Append("<b>TAWARANMU</b>   <size=20>(barang yang diterima " + trader.DisplayName + ")</size>\n");
            var eligible = EligibleItems();
            if (eligible.Count == 0)
            {
                string wants = "";
                foreach (var id in trader.acceptedItemIds)
                {
                    var d = ItemDatabase.Get(id);
                    wants += (wants.Length > 0 ? ", " : "") + (d != null ? d.DisplayName : id);
                }
                sb.Append("   (kamu tidak punya barang yang diterima; dia mau: " + wants + ")\n");
            }
            for (int i = 0; i < eligible.Count && i < 9; i++)
            {
                var def = eligible[i];
                int cur;
                offered.TryGetValue(def.Id, out cur);
                sb.Append("[" + (i + 1) + "] " + def.DisplayName + "   punya " + inv.Container.CountFresh(def.Id) + "   ditawarkan <b>" + cur + "</b>   nilai "
                          + trader.ValueOfOffered(def.Id, 1).ToString("0.0") + "/unit\n");
            }

            // Neraca nilai.
            float have = OfferedValue();
            string verdict;
            if (have <= 0f) verdict = "<color=#FFD166>pilih barang tawaranmu</color>";
            else if (have + 0.001f < required) verdict = "<color=#FF6B5A>DITOLAK: kurang " + (required - have).ToString("0.0") + "</color>";
            else verdict = "<color=#7CFC8A>CUKUP</color>" + (have - required > 0.05f ? "  <size=20>(kelebihan " + (have - required).ToString("0.0") + " tidak dikembalikan)</size>" : "");
            sb.Append("\n<b>NERACA</b>  tawaran <b>" + have.ToString("0.0") + "</b>  vs  dibutuhkan <b>" + required.ToString("0.0") + "</b>   " + verdict + "\n");

            // Berat.
            float projected = ProjectedWeight(wantDef);
            string wColor = projected > inv.MaxWeight + 0.001f ? "#FF6B5A" : "#7CFC8A";
            string wNote = projected > inv.MaxWeight + 0.001f ? "  TIDAK MUAT, kurangi " + (projected - inv.MaxWeight).ToString("0.0") + " kg" : "";
            sb.Append("<b>BERAT TAS</b>  " + inv.CurrentWeight.ToString("0.0") + " kg  ->  <color=" + wColor + "><b>" + projected.ToString("0.0") + " / " + inv.MaxWeight.ToString("0.0") + " kg</b>" + wNote + "</color>\n");

            sb.Append("</size>");
            return sb.ToString();
        }
    }
}
