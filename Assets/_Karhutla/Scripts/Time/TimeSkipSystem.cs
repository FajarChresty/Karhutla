using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// TIME SKIP: melewati beberapa hari yang TIDAK punya event penting, agar pemain tidak dipaksa mengulang
    /// rutinitas yang sama puluhan hari hanya untuk menunggu kebakaran. 90 hari = rentang waktu cerita;
    /// pemain tidak harus memainkan semuanya.
    ///
    /// Aturan:
    ///  - Hari penting (tanda awal, insiden korupsi, awal ketegangan, kebakaran mulai, fokus survival, dan hari
    ///    tambahan di DisasterTimeline.extraKeyDays) TIDAK pernah dilewati: skip berhenti paling jauh DI hari itu
    ///    sehingga event-nya tetap terjadi pada urutan dan waktu yang benar. Daftar hari penting berasal dari
    ///    satu sumber: DisasterTimeline.
    ///  - Setelah kebakaran dimulai, hari dimainkan penuh (allowSkipAfterFire = false) karena fase survival.
    ///  - Tidak bisa saat warung sedang buka.
    ///  - Semua konsekuensi tetap dihitung: setiap hari disimulasikan (kedaluwarsa per periode, lapar/haus/lelah,
    ///    penyebaran api bila aktif). Warung tidak beroperasi saat dilewati. Hubungan sosial tidak berubah
    ///    (belum ada peluruhan; keputusan Game Designer).
    ///  - Pemain diberi tahu sebelum (konfirmasi) dan sesudah (ringkasan) time skip.
    ///
    /// Dipicu dari bale-bale (tombol Q). Semua angka = NILAI SEMENTARA di Inspector.
    /// </summary>
    public class TimeSkipSystem : MonoBehaviour
    {
        public static TimeSkipSystem Instance { get; private set; }

        [Tooltip("Maksimal hari yang bisa dilewati sekali skip.")]
        public int maxSkipDays = 7;
        [Tooltip("Minimal hari agar time skip ditawarkan (di bawah ini cukup tidur biasa).")]
        public int minSkipDays = 2;
        [Tooltip("Izinkan time skip setelah kebakaran mulai. Default mati: fase survival dimainkan penuh.")]
        public bool allowSkipAfterFire = false;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Apakah time skip boleh dilakukan sekarang? Mengisi hari tujuan, atau alasan bila tidak.</summary>
        public bool CanSkip(out int targetDay, out string reason)
        {
            targetDay = 0;
            reason = null;

            var time = TimeManager.Instance;
            if (time == null) { reason = "Sistem waktu tidak aktif."; return false; }
            if (time.FinalDayEnded) { reason = "Permainan sudah berakhir."; return false; }
            if (time.IsSkipping) { reason = "Time skip sedang berjalan."; return false; }

            var warung = WarungSystem.Instance;
            if (warung != null && warung.IsOpen) { reason = "Tutup warung dulu sebelum melewati hari."; return false; }

            var timeline = DisasterTimeline.Instance;
            var disaster = DisasterSystem.Instance;
            bool fire = (timeline != null && timeline.CurrentPhase >= StoryPhase.KebakaranMulai) || (disaster != null && disaster.FireStarted);
            if (fire && !allowSkipAfterFire) { reason = "Masa bencana sudah dimulai. Hari dimainkan penuh."; return false; }

            int nextKey = timeline != null ? timeline.NextKeyDay(time.Day) : int.MaxValue;
            int target = Mathf.Min(Mathf.Min(time.Day + Mathf.Max(1, maxSkipDays), nextKey), time.totalDays);

            if (target - time.Day < Mathf.Max(2, minSkipDays))
            {
                reason = nextKey != int.MaxValue && nextKey - time.Day < minSkipDays
                    ? "Ada hari penting (Hari " + nextKey + ") terlalu dekat. Tidak ada hari kosong untuk dilewati."
                    : "Tidak ada cukup hari kosong untuk dilewati.";
                return false;
            }

            targetDay = target;
            return true;
        }

        /// <summary>Tampilkan konfirmasi time skip. Dipanggil bale-bale.</summary>
        public void RequestSkip()
        {
            int target;
            string reason;
            if (!CanSkip(out target, out reason))
            {
                Notifier.Show("Time skip tidak tersedia: " + reason);
                return;
            }

            var time = TimeManager.Instance;
            var timeline = DisasterTimeline.Instance;
            int from = time.Day;
            int days = target - from;
            int nextKey = timeline != null ? timeline.NextKeyDay(from) : int.MaxValue;

            var sb = new StringBuilder();
            sb.Append("Lewati <b>" + days + " hari</b>: dari Hari " + from + " (" + time.WeekdayName(from) + ") sampai Pagi Hari " + target + " (" + time.WeekdayName(target) + ").\n\n");
            sb.Append("Hari yang dilewati tidak memiliki event penting.\n");
            if (target == nextKey) sb.Append("Hari " + target + " adalah hari penting dan akan dimainkan penuh.\n");
            sb.Append("\n<b>Konsekuensi:</b>\n");
            sb.Append("  - Bahan dan masakan menua (bisa basi; kulkas memperlambat)\n");
            sb.Append("  - Lapar, haus, dan lelah ikut berjalan\n");
            sb.Append("  - Warung tidak beroperasi (tanpa pendapatan)\n");
            sb.Append("  - Hubungan sosial tidak berubah\n");
            sb.Append("  - Kondisi lingkungan/bencana tetap berjalan sesuai tanggal\n");

            ModalManager.Show("TIME SKIP", sb.ToString(), "Lewati hari", "Batal",
                choice => { if (choice == 0) Execute(); }, 1);
        }

        private void Execute()
        {
            int target;
            string reason;
            if (!CanSkip(out target, out reason))
            {
                Notifier.Show("Time skip dibatalkan: " + reason);
                return;
            }

            var time = TimeManager.Instance;
            var survival = SurvivalSystem.Instance;
            var report = DayReportSystem.Instance;
            int historyStart = report != null ? report.History.Count : 0;
            int from = time.Day;

            DayReportSystem.LogActivity("Time skip Hari " + from + " -> Hari " + target);
            if (survival != null) survival.IsSleeping = true;
            int elapsed = time.SkipDays(target - from);
            if (survival != null) survival.IsSleeping = false;

            ShowSummary(from, elapsed, historyStart);
        }

        private void ShowSummary(int from, int elapsed, int historyStart)
        {
            var time = TimeManager.Instance;
            var report = DayReportSystem.Instance;
            var sb = new StringBuilder();

            sb.Append("Waktu dilewati dari Hari " + from + " ke <b>Hari " + time.Day + " (" + time.WeekdayName(time.Day) + ")</b>, " + elapsed + " hari.\n");
            sb.Append("Hari cerita: " + time.Day + " / " + time.totalDays + "   |   dimainkan: " + time.DaysPlayed + "   dilewati: " + time.DaysSkipped + "\n\n");

            if (report != null)
            {
                int expired = 0, discarded = 0, income = 0;
                for (int i = historyStart; i < report.History.Count; i++)
                {
                    expired += report.History[i].ExpiredFound;
                    discarded += report.History[i].Discarded;
                    income += report.History[i].Income;
                }
                sb.Append("Selama dilewati:\n");
                sb.Append("  - Item yang menjadi basi: " + expired + " unit (periksa inventaris, gudang, kulkas)\n");
                sb.Append("  - Pendapatan tercatat: " + TextFormat.Rupiah(income) + "\n");
            }

            var s = SurvivalSystem.Instance;
            if (s != null)
            {
                sb.Append("  - Lapar " + Mathf.RoundToInt(s.Hunger) + "   Haus " + Mathf.RoundToInt(s.Thirst) + "   Lelah " + Mathf.RoundToInt(s.Fatigue) + "   Kesehatan " + Mathf.RoundToInt(s.Health) + "\n");
            }

            var timeline = DisasterTimeline.Instance;
            if (timeline != null) sb.Append("  - Fase cerita sekarang: " + DisasterTimeline.Label(timeline.CurrentPhase) + "\n");
            var d = DisasterSystem.Instance;
            if (d != null && d.FireStarted) sb.Append("  - Kebakaran aktif, api menyebar " + Mathf.RoundToInt(d.FireProgress01 * 100f) + "%\n");

            ModalManager.Show("TIME SKIP SELESAI", sb.ToString(), "Lanjut", null, null, 0);
        }
    }
}
