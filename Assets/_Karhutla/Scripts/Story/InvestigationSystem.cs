using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    [Serializable]
    public class ClueData
    {
        public string id;
        public string title;
        public string locationName;
        [TextArea(2, 5)]
        public string description;
    }

    /// <summary>
    /// Sistem Penyelidikan & Misi Mengungkap Pejabat Korup (GDD Bab 3.4 & 3.5).
    ///
    /// Aturan GDD:
    ///  - Pemain menyelidiki izin pembukaan lahan ilegal yang menjadi akar bencana karhutla.
    ///  - Mengumpulkan bukti (clues) dari berbagai penjuru desa (Balai Desa, Pondok Gambut, dll).
    ///  - Melakukan konfrontasi bersama jurnalis & warga untuk membongkar perizinan korup ke penegak hukum.
    ///  - Keberhasilan misi ini menjadi salah satu dari 3 pilar penentu Ending permainan.
    /// </summary>
    public class InvestigationSystem : MonoBehaviour
    {
        public static InvestigationSystem Instance { get; private set; }

        public const string ClueKonsesi = "clue_konsesi";
        public const string ClueKwitansi = "clue_kwitansi";
        public const string CluePetaBakar = "clue_peta_bakar";
        public const string ClueMandor = "clue_mandor";

        public static readonly ClueData[] AllClues =
        {
            new ClueData
            {
                id = ClueKonsesi,
                title = "Salinan Izin Konsesi Bodong PT Sawit Alam",
                locationName = "Arsip Balai Desa",
                description = "Dokumen izin pelepasan kawasan hutan lindung gambut dengan tanda tangan palsu dan cap stempel ilegal milik pejabat daerah."
            },
            new ClueData
            {
                id = ClueKwitansi,
                title = "Buku Kas & Kwitansi Aliran Dana Siluman",
                locationName = "Berkas Investigasi Mas Hendra",
                description = "Catatan transfer uang pelicin ratusan juta rupiah yang mengalir ke rekening oknum aparat desa untuk membiarkan pembakaran lahan."
            },
            new ClueData
            {
                id = CluePetaBakar,
                title = "Peta Titik Koordinat Sulutan Api Gambut",
                locationName = "Pondok Ladang Gambut",
                description = "Peta topografi konsesi gambut dengan coretan koordinat titik sulutan api yang sengaja diatur searah angin menuju pemukiman warga."
            },
            new ClueData
            {
                id = ClueMandor,
                title = "Buku Harian Operasional Mandor Tejo",
                locationName = "Pondok Mandor Sawit",
                description = "Log kerja harian mandor lapangan yang mencatat perintah 'pembersihan cepat lahan gambut sebelum musim hujan' dari petinggi korporasi."
            }
        };

        private readonly HashSet<string> collectedClueIds = new HashSet<string>();

        public bool CorruptionExposed { get; private set; }
        public int ClueCount => collectedClueIds.Count;
        public int TotalClues => AllClues.Length;
        public bool CanExpose => ClueCount >= 3 && !CorruptionExposed;

        public event Action<ClueData> OnClueFound;
        public event Action OnCorruptionExposed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool HasClue(string clueId)
        {
            return collectedClueIds.Contains(clueId);
        }

        public static ClueData GetClueData(string clueId)
        {
            foreach (var c in AllClues) if (c.id == clueId) return c;
            return null;
        }

        /// <summary>Kumpulkan bukti penyelidikan baru.</summary>
        public bool CollectClue(string clueId)
        {
            var data = GetClueData(clueId);
            if (data == null) return false;

            if (collectedClueIds.Add(clueId))
            {
                Notifier.Show("BUKTI DITEMUKAN [" + ClueCount + "/" + TotalClues + "]:\n" + data.title + " (" + data.locationName + ")");
                DayReportSystem.LogActivity("Menemukan bukti korupsi: " + data.title);
                OnClueFound?.Invoke(data);

                if (CanExpose)
                {
                    Notifier.Show("Bukti sudah cukup kuat (" + ClueCount + "/" + TotalClues + ")! Bicaralah dengan Mas Hendra atau Kades untuk membongkar skandal ini.");
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Aksi Puncak: Membongkar skandal korupsi perizinan ilegal ke publik dan aparat hukum.
        /// </summary>
        public bool ExposeCorruption()
        {
            if (CorruptionExposed)
            {
                Notifier.Show("Skandal perizinan ilegal sudah resmi dilaporkan ke publik dan penegak hukum.");
                return false;
            }

            if (ClueCount < 3)
            {
                Notifier.Show("Bukti belum cukup kuat! Kumpulkan setidaknya 3 bukti dokumen ilegal (" + ClueCount + "/3).");
                return false;
            }

            CorruptionExposed = true;
            DayReportSystem.LogActivity("MEMBONGKAR KORUPSI PERIZINAN LAHAN!");

            // Dialog / Narasi pengungkapan korupsi
            ModalManager.Show(
                "SKANDAL KORUPSI TERBONGKAR!",
                "Dengan " + ClueCount + " bukti dokumen otentik yang kamu kumpulkan bersama Mas Hendra, "
                + "rekayasa perizinan pembukaan lahan ilegal PT Sawit Alam resmi disiarkan ke media nasional dan dilaporkan ke kejaksaan!\n\n"
                + "<size=22>Oknum pejabat dan mandor lapangan tidak lagi bisa mengelak. Warga desa bersatu menuntut keadilan lingkungan. "
                + "Kamu berhasil menyelesaikan Misi Utama Cerita: Mengungkap Pejabat Korup!</size>",
                "Tuntaskan Keadilan", null,
                choice => {
                    Notifier.Show("Misi Selesai: Konspirasi izin pembakaran lahan berhasil dibongkar ke publik!");
                });

            OnCorruptionExposed?.Invoke();
            return true;
        }

        // Simpan & Muat
        public List<string> GetSavedClues()
        {
            return new List<string>(collectedClueIds);
        }

        public void RestoreState(List<string> clues, bool exposed)
        {
            collectedClueIds.Clear();
            if (clues != null)
            {
                foreach (var c in clues) collectedClueIds.Add(c);
            }
            CorruptionExposed = exposed;
        }
    }
}
