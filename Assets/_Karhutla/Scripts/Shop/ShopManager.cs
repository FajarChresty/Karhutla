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

        private static readonly string[][] Categories = new string[][]
        {
            // Kategori 1: Sembako & Bahan Pokok (GDD 5.4)
            new string[]
            {
                ItemDatabase.Beras, ItemDatabase.Sayur, ItemDatabase.Tepung, ItemDatabase.Telur,
                ItemDatabase.Mie, ItemDatabase.Pisang, ItemDatabase.KacangHijau, ItemDatabase.Gula
            },
            // Kategori 2: Lauk, Minuman & Rempah (GDD 5.4)
            new string[]
            {
                ItemDatabase.BubukKopi, ItemDatabase.Teh, ItemDatabase.Jeruk, ItemDatabase.Ikan,
                ItemDatabase.Ayam, ItemDatabase.Jahe, ItemDatabase.Air
            },
            // Kategori 3: Kebutuhan Bencana & Obat Darurat (GDD 7 & 8)
            new string[]
            {
                ItemDatabase.Masker, ItemDatabase.Obat, ItemDatabase.Oralit
            }
        };

        private static readonly string[] CategoryNames =
        {
            "Sembako & Bahan Pokok",
            "Lauk, Minuman & Rempah",
            "Kebutuhan Bencana & Medis"
        };

        private int currentCategory = 0;

        public bool IsOpen { get; private set; }
        public string Title { get { return quickMode ? "PASAR  (beli cepat 1 item, tanpa memakai sesi)" : "PASAR DESA  (belanja sembako, lauk & persediaan)"; } }
        public string Body { get { return BuildBody(); } }
        public string Footer
        {
            get
            {
                return quickMode ? "[1-8] beli 1 item lalu selesai   [Tab / A / D] ganti kategori   [Esc/Q] batal"
                                 : "[1-8] beli 1   [Shift+1-8] beli 5   [Tab / A / D] ganti kategori   [Esc/Q] selesai";
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

            // Navigasi Kategori dengan Tab atau tombol A / D / Panah
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentCategory = (currentCategory + 1) % Categories.Length;
                return;
            }
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentCategory = (currentCategory - 1 + Categories.Length) % Categories.Length;
                return;
            }

            var currentList = Categories[currentCategory];
            for (int n = 1; n <= currentList.Length && n <= 8; n++)
            {
                if (!GameInput.NumberPressed(n)) continue;
                int qty = (!quickMode && GameInput.SprintHeld) ? 5 : 1;
                bool bought = Buy(currentList[n - 1], qty);
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
            sb.Append("Uang: <b>" + TextFormat.Rupiah(inv.Money) + "</b>     Beban bawaan: " + inv.CurrentWeight.ToString("0.#") + " / " + inv.MaxWeight.ToString("0.#") + " kg\n");

            // Baris Navigasi Tab Kategori
            sb.Append("Kategori: ");
            for (int c = 0; c < Categories.Length; c++)
            {
                if (c == currentCategory)
                    sb.Append("<color=#FFD166><b>[ " + CategoryNames[c] + " ]</b></color>  ");
                else
                    sb.Append("<color=#A0A0A0>" + CategoryNames[c] + "</color>  ");
            }
            sb.Append("\n\n");

            var currentList = Categories[currentCategory];
            for (int i = 0; i < currentList.Length; i++)
            {
                var def = ItemDatabase.Get(currentList[i]);
                if (def == null) continue;
                sb.Append("[" + (i + 1) + "] " + def.DisplayName + "   <b>" + TextFormat.Rupiah(def.Price) + "</b>   " + def.Weight.ToString("0.##") + " kg");
                sb.Append("   (di tas: " + inv.Count(def.Id) + " | di warung: " + WarungStorage.StockFresh(def.Id) + ")");
                if (!string.IsNullOrEmpty(def.Description))
                {
                    sb.Append("\n     <size=18><color=#C0C0C0>" + def.Description + "</color></size>");
                }
                sb.Append("\n");
            }
            sb.Append("\n<size=20><i>* Bahan masakan mentah cepat basi jika tidak disimpan di kulkas warung.</i></size>");
            return sb.ToString();
        }
    }
}
