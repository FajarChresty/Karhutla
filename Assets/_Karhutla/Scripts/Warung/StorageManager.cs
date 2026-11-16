using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Layar pemindahan barang antara INVENTARIS pemain dan penyimpanan warung (Gudang / Kulkas).
    /// Input terkunci selama layar terbuka.
    ///   1-9 : pindahkan 1 unit      Shift+1-9 : pindahkan satu batch penuh
    ///   R   : ganti mode (Simpan / Ambil)      G : buang item basi di inventaris dan wadah ini
    ///   Esc / Q : tutup
    /// Umur item (kedaluwarsa) ikut berpindah, jadi memindahkan barang tidak "menyegarkan" barang basi.
    /// Pemindahan ke inventaris dibatasi berat bawaan; ke gudang/kulkas dibatasi berat kapasitasnya.
    /// </summary>
    public class StorageManager : MonoBehaviour, IMenuScreen
    {
        public static StorageManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public string Title { get { return IsOpen ? (kind == StorageKind.Kulkas ? "KULKAS WARUNG" : "GUDANG WARUNG") : ""; } }
        public string Body { get { return BuildBody(); } }
        public string Footer
        {
            get { return "[1-9] pindah 1   [Shift+1-9] pindah semua   [R] ganti mode   [G] buang yang basi   [Esc/Q] tutup"; }
        }

        private StorageKind kind;
        private bool depositMode = true; // true: inventaris -> penyimpanan, false: penyimpanan -> inventaris
        private int openedFrame = -1;

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (IsOpen) GameState.SetInputLocked(false);
            MenuScreenRegistry.Release(this);
            if (Instance == this) Instance = null;
        }

        public void Open(StorageKind which)
        {
            if (IsOpen || !MenuScreenRegistry.IsFree(this)) return;
            if (WarungStorage.Instance == null || Inventory.Instance == null) return;

            kind = which;
            depositMode = true;
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
            if (!IsOpen || Time.frameCount == openedFrame) return;

            if (GameInput.CancelPressed) { Close(); return; }
            if (GameInput.SwitchModePressed) depositMode = !depositMode;
            if (GameInput.DiscardPressed) DiscardBoth();

            for (int n = 1; n <= 9; n++)
            {
                if (GameInput.NumberPressed(n)) MoveEntry(n - 1, GameInput.SprintHeld);
            }
        }

        private ItemContainer Storage { get { return WarungStorage.Instance.Get(kind); } }

        private void MoveEntry(int index, bool wholeBatch)
        {
            var inv = Inventory.Instance;
            var storage = Storage;
            ItemContainer from = depositMode ? inv.Container : storage;
            ItemContainer to = depositMode ? storage : inv.Container;

            var list = from.Sorted();
            if (index < 0 || index >= list.Count) return;

            var batch = list[index];
            int want = wholeBatch ? batch.Count : 1;
            var def = batch.Def;
            if (def == null) return;

            // Hitung berapa unit yang masih muat di tujuan (berat).
            int fit = 0;
            for (int i = 1; i <= want; i++)
            {
                bool ok = depositMode ? to.CanAddWeight(def.Weight * i) : inv.CanCarry(batch.Id, i);
                if (!ok) break;
                fit = i;
            }

            if (fit <= 0)
            {
                Notifier.Show(depositMode ? to.Name + " penuh (batas berat)." : "Terlalu berat untuk dibawa! (batas " + inv.MaxWeight.ToString("0.#") + " kg)");
                return;
            }

            var moved = from.Take(batch, fit);
            if (moved == null) return;
            bool added = depositMode ? to.Add(moved.Id, moved.Count, moved.Age) : inv.Add(moved.Id, moved.Count, moved.Age);
            if (!added)
            {
                from.Add(moved.Id, moved.Count, moved.Age); // kembalikan (seharusnya tidak terjadi)
                return;
            }

            if (fit < want) Notifier.Show("Hanya " + fit + " dari " + want + " yang muat.");
        }

        private void DiscardBoth()
        {
            int a = Inventory.Instance.Container.DiscardExpired();
            int b = Storage.DiscardExpired();
            int total = a + b;
            if (total > 0)
            {
                DayReportSystem.LogDiscarded(total);
                Notifier.Show("Membuang " + total + " item basi.");
            }
            else
            {
                Notifier.Show("Tidak ada item basi.");
            }
        }

        private string BuildBody()
        {
            if (!IsOpen || Inventory.Instance == null || WarungStorage.Instance == null) return "";

            var inv = Inventory.Instance;
            var storage = Storage;
            ItemContainer from = depositMode ? inv.Container : storage;
            ItemContainer to = depositMode ? storage : inv.Container;

            var sb = new StringBuilder();
            sb.Append("Berat bawaan: " + inv.CurrentWeight.ToString("0.#") + " / " + inv.MaxWeight.ToString("0.#") + " kg");
            sb.Append("      " + storage.Name + ": " + storage.TotalWeight.ToString("0.#") + " / " + storage.MaxWeight.ToString("0.#") + " kg\n");
            if (kind == StorageKind.Kulkas) sb.Append("<color=#9EE7FF>Kulkas memperlambat kedaluwarsa makanan.</color>\n");
            sb.Append("\nMODE: <b>" + (depositMode ? "SIMPAN  (Inventaris -> " + storage.Name + ")" : "AMBIL  (" + storage.Name + " -> Inventaris)") + "</b>\n\n");

            var list = from.Sorted();
            if (list.Count == 0)
            {
                sb.Append("(tidak ada barang untuk dipindahkan)\n");
            }
            else
            {
                for (int i = 0; i < list.Count && i < 9; i++)
                {
                    var b = list[i];
                    var def = b.Def;
                    string fresh = FoodExpirySystem.FreshnessText(b.Id, b.Age);
                    sb.Append("[" + (i + 1) + "] " + def.DisplayName + " x" + b.Count + "   " + def.Weight.ToString("0.##") + " kg/unit");
                    if (!string.IsNullOrEmpty(fresh)) sb.Append("   " + fresh);
                    sb.Append("\n");
                }
                if (list.Count > 9) sb.Append("... dan " + (list.Count - 9) + " batch lain (pindahkan yang di atas dulu)\n");
            }

            sb.Append("\nIsi " + to.Name + ": " + WarungStorage.Summary(to));
            return sb.ToString();
        }
    }
}
