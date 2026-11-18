using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    // =========================================================================================
    // [DUMMY ASSETS - NARASI & TEKS SEMENTARA]
    // File ini memisahkan semua data narasi, teks plot point, dan teks ending dummy dari logika inti.
    // Saat aset cerita/dialog final atau ScriptableObject sudah siap dari tim naratif/designer,
    // pengembang hanya perlu mengganti isi data di file ini tanpa harus merombak sistem backend.
    // =========================================================================================

    [Serializable]
    public class PlotPointScenario
    {
        public string id;
        public string title;
        public int daysToSkip;
        [TextArea(3, 8)]
        public string transitionNarration;
    }

    public enum EndingCategory
    {
        BestEnding,
        GoodEnding,
        BitterEnding,
        BadEnding,
        BankruptcyGameOver,
        CollapseGameOver
    }

    public class EndingResult
    {
        public EndingCategory Category;
        public string Title;
        public string Subtitle;
        public Color ThemeColor;
        public bool SurvivalSuccess;
        public bool CorruptionExposed;
        public bool FireContained;
        public string Objective1Status; // Bertahan Hidup
        public string Objective2Status; // Mengungkap Korupsi
        public string Objective3Status; // Mengendalikan Karhutla
        public string EpilogueText;
    }

    public static class DummyStoryData
    {
        /// <summary>
        /// Daftar skenario transisi hari dummy untuk Plot Point Skip otomatis.
        /// Membantu melompati hari-hari repetitif sehingga total hari yang dimainkan aktif tetap ~50-60 hari.
        /// </summary>
        public static readonly List<PlotPointScenario> DefaultPlotSkips = new List<PlotPointScenario>
        {
            new PlotPointScenario
            {
                id = "skip_intro_to_early_signs",
                title = "Beberapa Pekan di Desa Berlalu",
                daysToSkip = 5,
                transitionNarration = "Hari-hari awal di desa berjalan tenang. Kamu mulai mengenal ritme kehidupan warga desa dan mengelola warung dengan baik. Namun, hawa panas kemarau perlahan mulai terasa lebih menyengat dari biasanya."
            },
            new PlotPointScenario
            {
                id = "skip_early_signs_investigation",
                title = "Kemarau Semakin Terik & Kecurigaan Tumbuh",
                daysToSkip = 7,
                transitionNarration = "Seminggu berlalu tanpa setetes hujan. Sumber air warga mulai menyusut. Kabar burung tentang aktivitas pembukaan lahan mencurigakan di batas konsesi hutan mulai menjadi perbincangan hangat di warung kopi."
            },
            new PlotPointScenario
            {
                id = "skip_corruption_investigation",
                title = "Dokumen Terungkap & Ketegangan Memuncak",
                daysToSkip = 8,
                transitionNarration = "Hari-hari tegang berlalu di balai desa. Konflik agraria dan kejanggalan izin lahan memicu perdebatan sengit antar pemangku kepentingan. Sementara itu, angin kering berhembus kencang membawa aroma dedaunan kering yang siap tersulut."
            },
            new PlotPointScenario
            {
                id = "skip_pre_fire_drought",
                title = "Kering Kerontang Menjelang Bencana",
                daysToSkip = 6,
                transitionNarration = "Matahari memanggang tanah desa hingga retak. Daun-daun hutan berubah kuning kecokelatan. Udara terasa berat dan menyesakkan dada. Warga mulai waspada menjaga batas ladang mereka siang dan malam."
            },
            new PlotPointScenario
            {
                id = "skip_emergency_peak",
                title = "Kabut Asap Mengurung Desa",
                daysToSkip = 5,
                transitionNarration = "Setelah berhari-hari berjuang melawan kepulan asap dari batas utara, kebakaran semakin tak terkendali. Mobil pemadam dan relawan bahu-membahu, sementara warga saling berbagi masker dan pasokan makanan."
            }
        };

        /// <summary>
        /// Mengevaluasi seluruh pencapaian pemain berdasarkan 3 Objektif Utama GDD Bab 3.4 & 3.5
        /// dan menentukan salah satu dari 3-4 variasi ending utama atau kondisi kegagalan.
        /// </summary>
        public static EndingResult DetermineEnding(int daysPlayed, int daysSkipped, int money, float avgSocial,
            DangerLevel danger, float health, bool corruptionExposed, bool fireContained, bool isBankruptcy = false)
        {
            var res = new EndingResult();

            // Kasus Spesial 1: Kebangkrutan Warung di Paruh Pertama (GDD 5.5)
            if (isBankruptcy)
            {
                res.Category = EndingCategory.BankruptcyGameOver;
                res.Title = "GAME OVER: KEBANGKRUTAN WARUNG";
                res.Subtitle = "Kegagalan Mengelola Keuangan & Modal Usaha";
                res.ThemeColor = new Color(0.95f, 0.25f, 0.25f);
                res.SurvivalSuccess = false;
                res.CorruptionExposed = false;
                res.FireContained = false;
                res.Objective1Status = "<color=#ff5555>GAGAL</color> (Gulung tikar kehabisan modal)";
                res.Objective2Status = "<color=#aaaaaa>TIDAK TERCAPAI</color> (Usaha terhenti sebelum penyelidikan selesai)";
                res.Objective3Status = "<color=#aaaaaa>TIDAK TERCAPAI</color> (Tidak sempat menghadapi bencana)";
                res.EpilogueText = "Keuangan warung habis tak bersisa dan kamu gagal menyediakan bahan pangan pokok untuk operasional sehari-hari. "
                    + "Di tengah teriknya awal musim kemarau, warung keluarga yang menjadi tumpuan harapan terpaksa tutup permanen. "
                    + "Kamu harus meninggalkan desa dengan tangan hampa sebelum bencana karhutla sempat dihadapi.";
                return res;
            }

            // Kasus Spesial 2: Kematian Fisik Survival (Health <= 0)
            if (health <= 0f)
            {
                res.Category = EndingCategory.CollapseGameOver;
                res.Title = "GAME OVER: TRAGEDI KABUT ASAP";
                res.Subtitle = "Kegagalan Mempertahankan Kondisi Fisik";
                res.ThemeColor = new Color(0.8f, 0.15f, 0.15f);
                res.SurvivalSuccess = false;
                res.CorruptionExposed = corruptionExposed;
                res.FireContained = fireContained;
                res.Objective1Status = "<color=#ff5555>GAGAL</color> (Tumbang akibat paparan asap & kelelahan)";
                res.Objective2Status = corruptionExposed ? "<color=#55ff55>TERCAPAI</color>" : "<color=#ff5555>GAGAL</color>";
                res.Objective3Status = fireContained ? "<color=#55ff55>TERCAPAI</color>" : "<color=#ff5555>GAGAL</color>";
                res.EpilogueText = "Kombinasi infeksi saluran pernapasan akut (ISPA) dan kelelahan ekstrem merenggut staminamu di tengah tebalnya kabut asap. "
                    + "Meskipun warga sempat berusaha menolong, tubuhmu tidak mampu lagi bertahan. Kisah perjuanganmu berakhir tragis sebelum fajar pemulihan tiba.";
                return res;
            }

            // Keberhasilan Bertahan Hidup
            res.SurvivalSuccess = true;
            res.CorruptionExposed = corruptionExposed;
            res.FireContained = fireContained;

            res.Objective1Status = "<color=#55ff55>TERCAPAI</color> (Berhasil bertahan hidup hingga akhir)";
            res.Objective2Status = corruptionExposed
                ? "<color=#55ff55>TERCAPAI</color> (Izin bodong & suap berhasil dibongkar ke publik)"
                : "<color=#ff5555>GAGAL</color> (Bukti tidak lengkap, dalang korupsi lolos)";
            res.Objective3Status = fireContained
                ? "<color=#55ff55>TERCAPAI</color> (Sekat bakar kokoh menahan rambatan api)"
                : "<color=#ff5555>GAGAL</color> (Api tak terbendung melalap sebagian pemukiman)";

            // EVALUASI 4 CABANG ENDING UTAMA (GDD Bab 3.4, 3.5, 11)

            // Cabang 1: BEST ENDING (Fajar Harapan Desa / Village Reborn)
            if (corruptionExposed && fireContained)
            {
                res.Category = EndingCategory.BestEnding;
                res.Title = "ENDING 1: FAJAR HARAPAN DESA";
                res.Subtitle = "Pahlawan Desa & Kemenangan Keadilan Sejati";
                res.ThemeColor = new Color(1f, 0.85f, 0.25f); // Emas

                var sb = new System.Text.StringBuilder();
                sb.Append("Langit desa yang berbulan-bulan kelabu perlahan kembali membiru. ");
                sb.Append("Berkat kerja kerasmu membangun sekat bakar dan memadamkan api bersama relawan desa, permukiman warga berhasil diselamatkan seutuhnya dari amukan karhutla. ");
                sb.Append("Di sisi lain, berkas konspirasi izin lahan bodong yang kamu kumpulkan bersama Mas Hendra sukses membongkar sindikat korupsi oknum pejabat hingga tingkat kabupaten. ");
                if (avgSocial >= 25f)
                {
                    sb.Append("Warga desa mengarakmu sebagai pahlawan sejati. Warung keluargamu kini menjadi lumbung kebersamaan dan lambang ketahanan desa yang mandiri dan berdaya!");
                }
                else
                {
                    sb.Append("Meskipun hubungan sosialmu sederhana, kontribusi nyatamu menjaga kelestarian desa akan dikenang oleh generasi mendatang.");
                }
                res.EpilogueText = sb.ToString();
            }
            // Cabang 2: GOOD ENDING (Benteng di Tengah Badai / Shield of Peatland)
            else if (!corruptionExposed && fireContained)
            {
                res.Category = EndingCategory.GoodEnding;
                res.Title = "ENDING 2: BENTENG DI TENGAH BADAI";
                res.Subtitle = "Desa Selamat, Namun Keadilan Belum Tuntas";
                res.ThemeColor = new Color(0.35f, 0.85f, 0.45f); // Hijau

                res.EpilogueText = "Gotong royong parit sekat bakar dan perjuangan gigih memadamkan bara gambut membuahkan hasil: api berhasil diputus sebelum melahap desa. Seluruh keluarga dan rumah tetap utuh berdiri. "
                    + "Namun, karena bukti perizinan ilegal tidak sempat kamu tuntaskan ke publik, para oknum pejabat dan korporasi sawit nakal lolos dari jerat hukum dengan dalih 'bencana alam murni'. "
                    + "Desa aman dari ancaman bara api untuk saat ini, namun luka ketidakadilan masih menyisakan perjuangan panjang di masa depan.";
            }
            // Cabang 3: BITTER ENDING (Suara Kebenaran di Atas Abu / Ash and Truth)
            else if (corruptionExposed && !fireContained)
            {
                res.Category = EndingCategory.BitterEnding;
                res.Title = "ENDING 3: SUARA KEBENARAN DI ATAS ABU";
                res.Subtitle = "Kemenangan Hukum di Tengah Runtuhnya Ladang";
                res.ThemeColor = new Color(0.95f, 0.6f, 0.2f); // Oranye

                res.EpilogueText = "Dokumen izin konsesi bodong dan aliran dana suap yang kamu bongkar mengguncang pemberitaan nasional. Para pelaku kejahatan lingkungan akhirnya ditangkap dan diseret ke pengadilan tipikor. "
                    + "Akan tetapi, rapuhnya pertahanan sekat bakar membuat keganasan api gambut merembet tak terbendung ke ladang dan tepi pemukiman. Sebagian warga terpaksa mengungsi membawa sisa barang berharga. "
                    + "Meski desa porak-poranda oleh amukan abu, kamu telah membuktikan bahwa kebenaran tidak bisa dibungkam oleh asap pekat kebohongan.";
            }
            // Cabang 4: BAD ENDING (Sunyi di Tanah Hangus / Silent Embers)
            else
            {
                res.Category = EndingCategory.BadEnding;
                res.Title = "ENDING 4: SUNYI DI TANAH HANGUS";
                res.Subtitle = "Bertahan Sendiri di Tengah Desa Mati";
                res.ThemeColor = new Color(0.6f, 0.65f, 0.7f); // Abu-abu

                res.EpilogueText = "Kamu berhasil mempertahankan diri sendiri hingga musim bencana berlalu. Namun desa tempat warung keluargamu berada kini tinggal nama. "
                    + "Tanpa adanya sekat bakar yang dibangun dan minimnya kepedulian bersama, api gambut melahap rumah-rumah kayu dan kebun warga, memaksa seluruh penduduk terpencar mengungsi. "
                    + "Para pejabat korup yang tidak pernah tersentuh hukum pergi membawa pundi-pundi uang, meninggalkan desa sunyi senyap berselimutkan jelaga hitam yang abadi.";
            }

            return res;
        }

        /// <summary>
        /// Kompatibilitas fungsi ringkasan naratif lama.
        /// </summary>
        public static string GenerateEndingSummary(int daysPlayed, int daysSkipped, int money, float avgSocial, DangerLevel danger, float health)
        {
            bool corr = InvestigationSystem.Instance != null && InvestigationSystem.Instance.CorruptionExposed;
            bool fire = FirefightingMission.Instance != null && FirefightingMission.Instance.IsFireContained;
            bool bankrupt = BankruptcySystem.Instance != null && BankruptcySystem.Instance.IsInFinancialCrisis;

            var res = DetermineEnding(daysPlayed, daysSkipped, money, avgSocial, danger, health, corr, fire, bankrupt);
            return res.EpilogueText;
        }
    }
}
