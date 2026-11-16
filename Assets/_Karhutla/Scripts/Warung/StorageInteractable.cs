using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Gudang atau Kulkas warung (pilih lewat kind).
    ///  E : buka layar pemindahan barang (inventaris &lt;-&gt; penyimpanan).
    ///  Q : saat warung buka, ambil makanan yang sedang ditunggu pelanggan (untuk diantar ke meja).
    /// </summary>
    public class StorageInteractable : InteractableBase
    {
        public StorageKind kind = StorageKind.Gudang;

        private string Label { get { return kind == StorageKind.Kulkas ? "Kulkas" : "Gudang Warung"; } }

        public override string GetPrompt() { return "Buka " + Label + " (simpan / ambil barang)"; }

        public override void Interact()
        {
            if (StorageManager.Instance != null) StorageManager.Instance.Open(kind);
        }

        public override string GetSecondaryPrompt()
        {
            var warung = WarungSystem.Instance;
            if (warung == null || !warung.IsOpen) return null;
            return "Ambil pesanan pelanggan dari " + Label;
        }

        public override void InteractSecondary()
        {
            var warung = WarungSystem.Instance;
            var storage = WarungStorage.Instance;
            if (warung == null || storage == null) return;
            warung.FetchOrdersFrom(storage.Get(kind), Label);
        }
    }
}
