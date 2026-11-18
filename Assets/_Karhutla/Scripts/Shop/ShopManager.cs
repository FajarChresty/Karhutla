using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Layar belanja di Pasar (pembelian dengan UANG, berbeda dari barter).
    ///  Mode penuh  : dibuka setelah pemain memakai satu sesi untuk "Belanja ke pasar" (GDD 4.1). Boleh membeli banyak.
    ///  Mode cepat  : membeli SATU item saja tanpa memakai sesi (GDD 4.1: membeli satu item tidak memakai sesi).
    /// 1-9 beli 1 unit, Shift+1-9 beli 5 unit (hanya mode penuh), Esc/Q tutup.
    /// Pembelian dibatasi uang dan batas berat bawaan. Pengeluaran dicatat ke laporan akhir hari.
    /// Daftar barang dan harga (ItemDefinition.Price) adalah NILAI SEMENTARA.
    /// </summary>
    public class ShopManager : MonoBehaviour, IMenuScreen
    {
        public static ShopManager Instance { get; private set; }

        private static readonly string[] Catalog =
        {
            ItemDatabase.Beras, ItemDatabase.Sayur, ItemDatabase.BubukKopi, ItemDatabase.Teh,
            ItemDatabase.Telur, ItemDatabase.Pisang, ItemDatabase.Mie, ItemDatabase.Air, ItemDatabase.Masker
        };

        public bool IsOpen { get; private set; }
        public string Title { get { return quickMode ? "PASAR  (beli cepat 1 item, tanpa memakai sesi)" : "PASAR  (belanja sembako & bahan)"; } }
        public string Body { get { return BuildBody(); } }
        public string Footer
        {
            get
            {
                return quickMode ? "[1-9] beli 1 item lalu selesai   [Esc/Q] batal"
                                 : "[1-9] beli 1   [Shift+1-9] beli 5   [Esc/Q] selesai belanja";
            }
        }

        private bool quickMode;
        private bool pendingOpen;
        private bool pendingQuick;
        private int openedFrame = -1;

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (IsOpen) GameState.SetInputLocked(false);
            MenuScreenRegistry.Release(this);
            if (Instance == this) Instance = null;
        }

        /// <summary>Buka belanja. Jika layar lain (mis. laporan akhir hari) sedang tampil, pembukaan ditunda sampai selesai.</summary>
        public void Open(bool quick)
        {
            if (IsOpen) return;
            pendingOpen = true;
            pendingQuick = quick;
            TryOpenNow();
        }

        private void TryOpenNow()
        {
            if (!pendingOpen || IsOpen || !MenuScreenRegistry.IsFree(this)) return;
            if (Inventory.Instance == null) { pendingOpen = false; return; }

            pendingOpen = false;
            quickMode = pendingQuick;
            IsOpen = true;
            openedFrame = Time.frameCount;
            MenuScreenRegistry.Active = this;
            GameState.SetInputLocked(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            MenuScreenRegistry.Release(this);
            GameState.SetInputLocked(false);
        }

        private void Update()
        {
            if (pendingOpen && !IsOpen)
            {
                if (!GameState.InputLocked || MenuScreenRegistry.IsFree(this)) TryOpenNow();
                return;
            }
            if (!IsOpen || Time.frameCount == openedFrame) return;

            if (GameInput.CancelPressed) { Close(); return; }

            for (int n = 1; n <= Catalog.Length && n <= 9; n++)
            {
                if (!GameInput.NumberPressed(n)) continue;
                int qty = (!quickMode && GameInput.SprintHeld) ? 5 : 1;
                bool bought = Buy(Catalog[n - 1], qty);
                if (bought && quickMode) Close();
                break;
            }
        }

        private bool Buy(string id, int qty)
        {
            var inv = Inventory.Instance;
            var def = ItemDatabase.Get(id);
            if (inv == null || def == null) return false;

            // Kurangi jumlah bila tidak cukup uang / berat, lalu beli sebanyak yang bisa.
            int affordable = qty;
            while (affordable > 0 && (inv.Money < def.Price * affordable || !inv.CanCarry(id, affordable))) affordable--;

            if (affordable <= 0)
            {
                if (inv.Money < def.Price) Notifier.Show("Uang tidak cukup untuk " + def.DisplayName + ".");
                else Notifier.Show("Terlalu berat untuk dibawa! Simpan barang ke gudang warung dulu.");
                return false;
            }

            int total = def.Price * affordable;
            if (!inv.TrySpend(total)) return false;
            if (!inv.Add(id, affordable))
            {
                inv.AddMoney(total); // batalkan (seharusnya tidak terjadi)
                return false;
            }

            DayReportSystem.LogExpense(total);
            Notifier.Show("Membeli " + def.DisplayName + " x" + affordable + "  -" + TextFormat.Rupiah(total));
            if (affordable < qty) Notifier.Show("Hanya " + affordable + " dari " + qty + " yang bisa dibeli (uang/berat).");
            return true;
        }

        private string BuildBody()
        {
            var inv = Inventory.Instance;
            if (inv == null) return "";

            var sb = new StringBuilder();
            sb.Append("Uang: <b>" + TextFormat.Rupiah(inv.Money) + "</b>     Berat bawaan: " + inv.CurrentWeight.ToString("0.#") + " / " + inv.MaxWeight.ToString("0.#") + " kg\n\n");
            for (int i = 0; i < Catalog.Length; i++)
            {
                var def = ItemDatabase.Get(Catalog[i]);
                sb.Append("[" + (i + 1) + "] " + def.DisplayName + "   " + TextFormat.Rupiah(def.Price) + "   " + def.Weight.ToString("0.##") + " kg");
                sb.Append("   (punya " + inv.Count(def.Id) + ")\n");
            }
            sb.Append("\n<size=22>Bahan mentah cepat basi bila tidak disimpan di kulkas. Barter dengan warga hanya saat masa bencana.</size>");
            return sb.ToString();
        }
    }
}
