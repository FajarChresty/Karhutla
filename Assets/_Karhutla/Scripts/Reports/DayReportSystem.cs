using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Data satu hari. Semua angka berasal dari kejadian gameplay yang tercatat, bukan angka acak.</summary>
    public class DayReport
    {
        public int Day;
        public int WarungSessions;
        public int Customers;
        public bool WasSkipped;
        public bool SundayHoliday;
        public int Income;
        public int Expenses;
        public int Served;
        public readonly Dictionary<string, int> ServedByItem = new Dictionary<string, int>();
        public readonly Dictionary<string, int> FailedByReason = new Dictionary<string, int>();
        public readonly List<float> Satisfactions = new List<float>();
        public readonly Dictionary<string, int> Cooked = new Dictionary<string, int>();
        public int ExpiredFound;
        public int Discarded;
        public readonly List<string> Barters = new List<string>();
        public readonly List<string> Activities = new List<string>();
        public readonly List<string> RelationshipChanges = new List<string>();

        public int Failed
        {
            get
            {
                int n = 0;
                foreach (var kv in FailedByReason) n += kv.Value;
                return n;
            }
        }

        public bool HasSatisfaction { get { return Satisfactions.Count > 0; } }

        public float AverageSatisfaction
        {
            get
            {
                if (Satisfactions.Count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < Satisfactions.Count; i++) sum += Satisfactions[i];
                return sum / Satisfactions.Count;
            }
        }
    }

    /// <summary>
    /// Pencatat kejadian harian + laporan akhir hari.
    /// Sistem lain mengirim data lewat method statis Log...() (aman dipanggil walau sistem ini tidak ada di scene).
    /// Saat hari berganti (atau Hari terakhir selesai) laporan hari yang baru selesai ditampilkan lewat ModalManager.
    /// Tombol L menampilkan laporan hari ini yang masih berjalan.
    /// </summary>
    public class DayReportSystem : MonoBehaviour
    {
        public static DayReportSystem Instance { get; private set; }

        [Header("Label kepuasan (NILAI SEMENTARA)")]
        public float veryHappyThreshold = 80f;
        public float happyThreshold = 60f;
        public float okThreshold = 40f;

        public DayReport Current { get; private set; }
        public DayReport LastReport { get; private set; }
        public readonly List<DayReport> History = new List<DayReport>();

        private int trackedDay = 1;
        private bool finalReported;

        private void Awake()
        {
            Instance = this;
            Current = new DayReport { Day = 1 };
        }

        private void Start()
        {
            var time = TimeManager.Instance;
            if (time != null)
            {
                trackedDay = time.Day;
                Current.Day = trackedDay;
                time.OnDayChanged += HandleDayChanged;
                time.OnFinalDayEnded += HandleFinalDayEnded;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleDayChanged;
                TimeManager.Instance.OnFinalDayEnded -= HandleFinalDayEnded;
            }
            if (Instance == this) Instance = null;
        }

        /// <summary>Mulai laporan baru untuk hari ini setelah data simpan dimuat (catatan hari berjalan tidak ikut tersimpan).</summary>
        public void RestoreDay(int day)
        {
            trackedDay = day;
            Current = new DayReport { Day = day };
            finalReported = false;
        }

        private void Update()
        {
            if (GameState.InputLocked) return;
            if (GameInput.ReportPressed)
            {
                ModalManager.Show("Ringkasan Hari ke-" + Current.Day + " (sementara, belum selesai)", BuildText(Current), "Tutup", null, null, 2);
            }
        }

        private void HandleDayChanged(int newDay)
        {
            FinishDay();
            trackedDay = newDay;
            Current = new DayReport { Day = newDay };
        }

        private void HandleFinalDayEnded()
        {
            if (finalReported) return;
            finalReported = true;
            FinishDay();
            Current = new DayReport { Day = trackedDay };
        }

        private void FinishDay()
        {
            var report = Current;
            report.Day = trackedDay;
            LastReport = report;
            History.Add(report);

            // Selama time skip, laporan per hari tidak dimunculkan (ringkasan tunggal dibuat TimeSkipSystem).
            var time = TimeManager.Instance;
            if (time != null && time.IsSkipping)
            {
                report.WasSkipped = true;
                return;
            }

            ModalManager.Show("LAPORAN AKHIR HARI ke-" + report.Day, BuildText(report), "Lanjut", null, null, 0);
        }

        // ================= Pencatatan (statis agar mudah dipanggil) =================

        public static void LogIncome(int amount) { if (Instance != null) Instance.Current.Income += amount; }
        public static void LogExpense(int amount) { if (Instance != null) Instance.Current.Expenses += amount; }
        public static void LogExpired(int units) { if (Instance != null) Instance.Current.ExpiredFound += units; }
        public static void LogDiscarded(int units) { if (Instance != null) Instance.Current.Discarded += units; }
        public static void LogCustomer() { if (Instance != null) Instance.Current.Customers++; }
        public static void LogSession() { if (Instance != null) Instance.Current.WarungSessions++; }
        public static void LogSundayHoliday(bool holiday) { if (Instance != null) Instance.Current.SundayHoliday = holiday; }
        public static void LogBarter(string text) { if (Instance != null) Instance.Current.Barters.Add(text); }
        public static void LogRelationship(string text) { if (Instance != null) Instance.Current.RelationshipChanges.Add(text); }

        public static void LogActivity(string text)
        {
            if (Instance == null) return;
            var time = TimeManager.Instance;
            string tag = time != null ? "[" + time.Period + "] " : "";
            Instance.Current.Activities.Add(tag + text);
        }

        public static void LogServed(string itemId, float satisfaction)
        {
            if (Instance == null) return;
            var r = Instance.Current;
            r.Served++;
            r.Satisfactions.Add(satisfaction);
            Add(r.ServedByItem, itemId, 1);
        }

        public static void LogFailed(string reason)
        {
            if (Instance == null) return;
            var r = Instance.Current;
            Add(r.FailedByReason, reason, 1);
            r.Satisfactions.Add(0f); // pelanggan yang gagal dilayani dihitung puas = 0
        }

        public static void LogCooked(string itemId, int portions)
        {
            if (Instance != null) Add(Instance.Current.Cooked, itemId, portions);
        }

        private static void Add(Dictionary<string, int> dict, string key, int amount)
        {
            int c;
            dict.TryGetValue(key, out c);
            dict[key] = c + amount;
        }

        // ================= Teks laporan =================

        public string SatisfactionLabel(float value)
        {
            if (value >= veryHappyThreshold) return "Sangat puas";
            if (value >= happyThreshold) return "Puas";
            if (value >= okThreshold) return "Cukup";
            return "Kurang puas";
        }

        public string BuildText(DayReport r)
        {
            var time = TimeManager.Instance;
            var sb = new StringBuilder();

            string weekday = time != null ? time.WeekdayName(r.Day) : "";
            sb.Append("<b>Hari ke-" + r.Day + (weekday.Length > 0 ? " (" + weekday + ")" : "") + "</b>\n\n");

            string status;
            if (r.WarungSessions > 0) status = "Buka (" + r.WarungSessions + " sesi)";
            else if (r.SundayHoliday) status = "Libur (dipilih pemain pada hari Minggu)";
            else status = "Tidak dibuka";
            sb.Append("Warung: " + status + "\n");

            sb.Append("Pendapatan: " + TextFormat.Rupiah(r.Income) + "     Belanja bahan: " + TextFormat.Rupiah(r.Expenses)
                      + "     Selisih: " + TextFormat.Rupiah(r.Income - r.Expenses) + "\n\n");

            sb.Append("Jumlah pelanggan: <b>" + r.Customers + "</b>\n");
            sb.Append("Pesanan selesai: <b>" + r.Served + "</b>");
            if (r.ServedByItem.Count > 0)
            {
                sb.Append("   (");
                bool first = true;
                foreach (var kv in r.ServedByItem)
                {
                    var def = ItemDatabase.Get(kv.Key);
                    if (!first) sb.Append(", ");
                    sb.Append((def != null ? def.DisplayName : kv.Key) + " x" + kv.Value);
                    first = false;
                }
                sb.Append(")");
            }
            sb.Append("\n");

            sb.Append("Pesanan gagal / belum dilayani: <b>" + r.Failed + "</b>\n");
            foreach (var kv in r.FailedByReason) sb.Append("   - " + kv.Key + ": " + kv.Value + "\n");

            if (r.HasSatisfaction)
            {
                float avg = r.AverageSatisfaction;
                sb.Append("Kepuasan pelanggan hari ini: <b>" + Mathf.RoundToInt(avg) + "/100</b> (" + SatisfactionLabel(avg) + ")");
            }
            else
            {
                sb.Append("Kepuasan pelanggan hari ini: - (tidak ada pelanggan)");
            }
            var warung = WarungSystem.Instance;
            if (warung != null && warung.RatedCustomers > 0)
                sb.Append("     Keseluruhan: " + Mathf.RoundToInt(warung.OverallSatisfaction) + "/100");
            sb.Append("\n\n");

            if (r.Cooked.Count > 0)
            {
                sb.Append("Dimasak: ");
                bool first = true;
                foreach (var kv in r.Cooked)
                {
                    var def = ItemDatabase.Get(kv.Key);
                    if (!first) sb.Append(", ");
                    sb.Append((def != null ? def.DisplayName : kv.Key) + " x" + kv.Value);
                    first = false;
                }
                sb.Append("\n");
            }

            sb.Append("Makanan/bahan basi: " + r.ExpiredFound + " unit tercatat basi, " + r.Discarded + " unit dibuang\n");

            if (r.Barters.Count > 0)
            {
                sb.Append("Barter (" + r.Barters.Count + "):\n");
                foreach (var b in r.Barters) sb.Append("   - " + b + "\n");
            }
            if (r.RelationshipChanges.Count > 0)
            {
                sb.Append("Hubungan sosial:\n");
                foreach (var c in r.RelationshipChanges) sb.Append("   - " + c + "\n");
            }

            sb.Append("\nAktivitas:\n");
            if (r.Activities.Count == 0) sb.Append("   (tidak ada aktivitas yang memakai waktu)\n");
            int start = Mathf.Max(0, r.Activities.Count - 10);
            if (start > 0) sb.Append("   ... " + start + " aktivitas sebelumnya\n");
            for (int i = start; i < r.Activities.Count; i++) sb.Append("   - " + r.Activities[i] + "\n");

            return sb.ToString();
        }
    }
}
