using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Dua titik interaksi warung (pilih lewat Mode):
    ///  - Sign    : papan untuk buka/tutup warung (dan pilihan buka/libur hari Minggu)
    ///  - Counter : meja kasir, hanya menampilkan ringkasan (pembayaran terjadi di meja pelanggan, bukan di kasir)
    ///
    /// Tahap 2: transaksi instan di kasir DIHAPUS. Mode Shelf lama digantikan StorageInteractable (Gudang/Kulkas).
    /// </summary>
    public class WarungInteractable : InteractableBase
    {
        public enum Mode { Sign, Counter }
        public Mode mode = Mode.Sign;

        public override string GetPrompt()
        {
            var warung = WarungSystem.Instance;
            if (warung == null) return promptText;

            switch (mode)
            {
                case Mode.Sign:
                    return warung.GetSignPrompt();

                case Mode.Counter:
                    return warung.IsOpen ? "Kasir: lihat ringkasan warung" : "Kasir (warung tutup)";
            }
            return promptText;
        }

        public override void Interact()
        {
            var warung = WarungSystem.Instance;
            if (warung == null) return;

            switch (mode)
            {
                case Mode.Sign:
                    warung.InteractSign();
                    break;

                case Mode.Counter:
                    warung.ShowSummary();
                    break;
            }
        }
    }
}
