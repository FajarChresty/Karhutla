using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Titik Interaksi Penyelidikan Bukti Korupsi (GDD Bab 3.4 & 3.5).
    /// Diletakkan di landmark desa seperti Meja Arsip Balai Desa, Pondok Ladang Gambut, dll.
    /// </summary>
    public class InvestigationClueSpot : InteractableBase
    {
        [Tooltip("ID Petunjuk dari InvestigationSystem (mis. clue_konsesi, clue_kwitansi, dll)")]
        public string clueId;

        public override string GetPrompt()
        {
            var invSys = InvestigationSystem.Instance;
            bool found = invSys != null && invSys.HasClue(clueId);
            var data = InvestigationSystem.GetClueData(clueId);
            string title = data != null ? data.title : "Berkas Rahasia";

            if (found)
            {
                return "Periksa kembali: " + title + " (Sudah Disimpan)";
            }
            return "Selidiki Berkas: " + title;
        }

        public override void Interact()
        {
            var invSys = InvestigationSystem.Instance;
            if (invSys == null) return;

            var data = InvestigationSystem.GetClueData(clueId);
            if (data == null) return;

            if (!invSys.HasClue(clueId))
            {
                invSys.CollectClue(clueId);
                ModalManager.Show(
                    "BUKTI PENYELIDIKAN DITEMUKAN",
                    "<b>" + data.title + "</b>\n"
                    + "Lokasi: <i>" + data.locationName + "</i>\n\n"
                    + data.description + "\n\n"
                    + "<size=22>Bukti ini disimpan di catatan penyelidikan. Kumpulkan seluruh bukti untuk mengungkap korupsi ke publik!</size>",
                    "Simpan Bukti", null,
                    choice => { });
            }
            else
            {
                ModalManager.Show(
                    "CATATAN BUKTI TERSIMPAN",
                    "<b>" + data.title + "</b>\n"
                    + "Lokasi: <i>" + data.locationName + "</i>\n\n"
                    + data.description,
                    "Tutup", null,
                    choice => { });
            }
        }
    }
}
