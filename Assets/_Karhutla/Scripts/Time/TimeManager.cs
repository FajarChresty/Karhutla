using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Tiga periode harian sesuai GDD 4.1 (Sore dihapus).</summary>
    public enum TimeOfDay { Pagi = 0, Siang = 1, Malam = 2 }

    /// <summary>
    /// Biaya waktu sebuah aktivitas.
    ///  - Units        : jumlah "jatah waktu" yang dipakai pada periode berjalan.
    ///  - UntilNextDay : melewati sisa hari sampai Pagi hari berikutnya (tidur).
    /// </summary>
    [Serializable]
    public struct TimeCost
    {
        public int Units;
        public bool UntilNextDay;

        public bool IsFree { get { return !UntilNextDay && Units <= 0; } }

        public static TimeCost Free { get { return new TimeCost(); } }
        public static TimeCost OfUnits(int units) { return new TimeCost { Units = Mathf.Max(0, units) }; }
        public static TimeCost ToNextDay { get { return new TimeCost { UntilNextDay = true }; } }
    }

    /// <summary>
    /// Biaya waktu per jenis aktivitas, dinyatakan dalam SESI (1 = satu sesi penuh).
    /// Nilai default mengikuti GDD 4.1:
    ///  - Percakapan biasa, berjalan, membeli satu item  -> tidak memakai sesi (0).
    ///  - Waktu khusus bersama karakter, berbelanja, acara desa, makan di restoran lain,
    ///    berenang/olahraga, aktivitas lain yang ditetapkan -> memakai SATU sesi.
    /// Semua angka ini bisa diubah di Inspector (komponen TimeManager).
    /// </summary>
    [Serializable]
    public class ActivityTimeCosts
    {
        [Tooltip("Percakapan biasa dengan NPC. GDD 4.1: tidak memakai sesi (0). Isi > 0 jika ingin percakapan memakai jatah waktu.")]
        public float conversationSections = 0f;

        [Tooltip("Menghabiskan waktu khusus bersama karakter. GDD 4.1: satu sesi.")]
        public float dedicatedTimeSections = 1f;

        [Tooltip("Kegiatan berbelanja. GDD 4.1: satu sesi (membeli satu item saja tidak memakai sesi).")]
        public float shoppingSections = 1f;

        [Tooltip("Aktivitas lain yang ditetapkan GDD (acara desa, makan di tempat lain, berenang, olahraga). GDD 4.1: satu sesi.")]
        public float otherDesignatedSections = 1f;

        [Tooltip("Barter dengan warga (satu kunjungan barter = satu layar barter, boleh beberapa transaksi). Dibayar saat layar barter dibuka. Belum ada di GDD; default 1 sesi seperti berbelanja. NILAI SEMENTARA.")]
        public float barterSections = 1f;

        [Tooltip("Memberi hadiah ke warga. Dianggap bagian dari percakapan, jadi default 0 (seperti percakapan biasa). Isi > 0 bila ingin memakai jatah waktu. NILAI SEMENTARA.")]
        public float giftSections = 0f;

        [Tooltip("Satu sesi operasional warung (Diner Dash). Dipakai saat sesi warung berakhir/ditutup. GDD 4.2 hanya menyebut warung normalnya buka Pagi; 1 = menghabiskan sesi Pagi. NILAI SEMENTARA.")]
        public float warungSessionSections = 1f;
    }

    /// <summary>
    /// Sistem waktu berbasis sesi (GDD 4.1). Waktu TIDAK berjalan otomatis.
    ///
    ///  - Hari terdiri dari 3 periode: Pagi, Siang, Malam.
    ///  - Setiap periode punya "jatah waktu" (unitsPerSection). Aktivitas pemain mengurangi jatah itu
    ///    lewat Spend(). Jika jatah habis, periode berpindah: Pagi -> Siang -> Malam -> Pagi (hari berikutnya).
    ///  - Tidur (TimeCost.ToNextDay) melewati sisa hari menuju Pagi berikutnya.
    ///  - Jalan kaki dan percakapan biasa tidak memakai jatah (GDD).
    ///
    /// Agar sistem lain (Survival, Disaster) tetap bisa memakai simulasi per menit seperti V1, setiap sesi
    /// dipetakan ke simulatedMinutesPerSection menit simulasi. OnMinutePassed dipicu hanya saat jatah dipakai.
    ///
    /// NILAI SEMENTARA (tidak ditentukan GDD): unitsPerSection, simulatedMinutesPerSection, jam visual langit.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Hari")]
        public int startDay = 1;
        [Tooltip("Format 4 Bulan: 120 hari total kalender cerita (1 bulan = 30 hari).")]
        public int totalDays = 120;

        [Header("Target Gameplay Aktif (Format 50-60 Hari)")]
        public const int DaysPerMonth = 30;
        public const int TotalMonths = 4;
        public const int TargetActiveDays = 60;
        [Tooltip("Target jumlah hari gameplay aktif yang benar-benar dimainkan oleh pemain (~50-60 hari).")]
        public int targetDaysPlayed = TargetActiveDays;

        [Header("Kalender mingguan (NILAI SEMENTARA)")]
        [Tooltip("Hari-dalam-minggu untuk Hari 1: 0=Senin, 1=Selasa, ... 6=Minggu. GDD 4.2 menyebut hari Minggu spesial tetapi tidak menyebut Hari 1 jatuh pada hari apa.")]
        [Range(0, 6)] public int day1Weekday = 0;

        [Header("Jatah waktu per periode (NILAI SEMENTARA, belum ada di GDD)")]
        [Tooltip("Berapa unit jatah waktu dalam satu periode. Aktivitas 'satu sesi' menghabiskan seluruh sisa jatah.")]
        public int unitsPerSection = 4;
        public ActivityTimeCosts costs = new ActivityTimeCosts();

        [Header("Simulasi dunia (NILAI SEMENTARA)")]
        [Tooltip("Menit simulasi per sesi. 3 sesi x 480 = 1440 menit = satu hari. Dipakai Survival dan Disaster.")]
        public int simulatedMinutesPerSection = 480;

        [Header("Tampilan langit (hanya visual, NILAI SEMENTARA)")]
        [Tooltip("Jam visual awal Pagi, Siang, Malam.")]
        public float[] sectionStartHour = { 6f, 11f, 18f };
        [Tooltip("Jam visual akhir Pagi, Siang, Malam. Malam berakhir 23.5 lalu langit bergerak ke Pagi berikutnya.")]
        public float[] sectionEndHour = { 10.5f, 16.5f, 23.5f };

        [Header("Debug (hanya untuk pengujian, bukan mekanik game)")]
        [Tooltip("T = pakai 1 jatah, Y = lewati sisa periode, U = loncat 1 hari (Shift+U = 10 hari).")]
        public bool debugKeysEnabled = true;

        public int Day { get; private set; }
        public TimeOfDay Period { get; private set; }
        public int UnitsRemaining { get; private set; }
        public bool FinalDayEnded { get; private set; }

        /// <summary>True selama time skip berlangsung (sistem lain menekan pop-up/notifikasi per hari).</summary>
        public bool IsSkipping { get; private set; }
        /// <summary>Jumlah hari cerita yang benar-benar dimainkan (dimulai secara normal).</summary>
        public int DaysPlayed { get; private set; }
        /// <summary>Jumlah hari cerita yang dilewati lewat time skip (atau loncat debug).</summary>
        public int DaysSkipped { get; private set; }

        /// <summary>Total menit simulasi sejak Hari 1 (dipakai Disaster untuk pertumbuhan api).</summary>
        public int TotalMinutes { get; private set; }

        public int SimulatedMinutesPerDay { get { return 3 * simulatedMinutesPerSection; } }

        public static readonly string[] WeekdayNames = { "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu", "Minggu" };

        /// <summary>Bulan berjalan (1 sampai 4).</summary>
        public int Month { get { return Mathf.Clamp(((Day - 1) / DaysPerMonth) + 1, 1, TotalMonths); } }
        /// <summary>Hari di dalam bulan berjalan (1 sampai 30).</summary>
        public int DayInMonth { get { return ((Day - 1) % DaysPerMonth) + 1; } }

        /// <summary>Format tampilan bulan dan hari (misal: 'Bulan 1 - Hari 10').</summary>
        public string FormattedDate { get { return "Bulan " + Month + " - Hari " + DayInMonth; } }
        /// <summary>Format pencatatan hari dimainkan aktif (misal: 'Hari Dimainkan: 15 dari 60').</summary>
        public string FormattedActiveDays { get { return "Hari Dimainkan: " + DaysPlayed + " dari " + targetDaysPlayed; } }

        /// <summary>0=Senin ... 6=Minggu untuk hari permainan tertentu.</summary>
        public int WeekdayIndex(int day)
        {
            return (((day - 1 + day1Weekday) % 7) + 7) % 7;
        }

        public string WeekdayName(int day) { return WeekdayNames[WeekdayIndex(day)]; }
        public bool IsSunday(int day) { return WeekdayIndex(day) == 6; }
        public bool IsSundayToday { get { return IsSunday(Day); } }

        /// <summary>Kemajuan di dalam periode berjalan, 0..1 (naik saat jatah waktu dipakai). Dipakai DayNightCycle.</summary>
        public float SectionProgress01
        {
            get
            {
                if (simulatedMinutesPerSection <= 0) return 0f;
                return Mathf.Clamp01(minutesIntoSection / (float)simulatedMinutesPerSection);
            }
        }

        /// <summary>
        /// (Lama, tidak dipakai lagi oleh DayNightCycle sejak perbaikan pencahayaan.) Jam visual 0..24 dari sectionStartHour/EndHour.
        /// DayNightCycle sekarang memakai jam visual per periode miliknya sendiri agar Pagi tidak jatuh tepat di jam matahari terbit (gelap).
        /// </summary>
        public float VisualHour
        {
            get
            {
                int i = (int)Period;
                float p = simulatedMinutesPerSection > 0 ? minutesIntoSection / (float)simulatedMinutesPerSection : 0f;
                return Mathf.Lerp(sectionStartHour[i], sectionEndHour[i], Mathf.Clamp01(p));
            }
        }

        /// <summary>Dipicu per menit simulasi, hanya saat jatah waktu dipakai.</summary>
        public event Action OnMinutePassed;
        public event Action<TimeOfDay> OnPeriodChanged;
        public event Action<int> OnDayChanged;
        public event Action OnUnitsChanged;
        /// <summary>Dipicu setelah loncat waktu (debug) agar sistem lain menyinkronkan diri.</summary>
        public event Action OnTimeJumped;
        public event Action OnFinalDayEnded;
        /// <summary>Dipicu saat kondisi akhir permainan terpenuhi (Hari 120 atau 60 hari aktif): (alasan).</summary>
        public event Action<string> OnEndingTriggered;
        /// <summary>Dipicu setelah time skip selesai: (hari asal, hari tujuan).</summary>
        public event Action<int, int> OnTimeSkipped;

        public bool IsEndingTriggered { get; private set; }

        private int minutesIntoSection;

        private void Awake()
        {
            Instance = this;
            SanitizeSettings();

            Day = Mathf.Clamp(startDay, 1, Mathf.Max(1, totalDays));
            Period = TimeOfDay.Pagi;
            UnitsRemaining = unitsPerSection;
            minutesIntoSection = 0;
            TotalMinutes = (Day - 1) * SimulatedMinutesPerDay;
            DaysPlayed = 1;

            // Pengaman: scene lama belum punya komponen baru ini. Dibuat otomatis jika belum ada.
            if (FindFirstObjectByType<TimeActivityManager>() == null) gameObject.AddComponent<TimeActivityManager>();
            if (FindFirstObjectByType<TimeConfirmUI>() == null) gameObject.AddComponent<TimeConfirmUI>();
            if (FindFirstObjectByType<PlotPointSkipManager>() == null) gameObject.AddComponent<PlotPointSkipManager>();
            if (FindFirstObjectByType<EndingManager>() == null) gameObject.AddComponent<EndingManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SanitizeSettings()
        {
            unitsPerSection = Mathf.Max(1, unitsPerSection);
            simulatedMinutesPerSection = Mathf.Max(1, simulatedMinutesPerSection);
            if (totalDays <= 90) totalDays = 120; // Otomatis upgrade format lama (90 hari) ke 4 Bulan (120 hari)
            totalDays = Mathf.Max(1, totalDays);
            if (sectionStartHour == null || sectionStartHour.Length != 3) sectionStartHour = new[] { 6f, 11f, 18f };
            if (sectionEndHour == null || sectionEndHour.Length != 3) sectionEndHour = new[] { 10.5f, 16.5f, 23.5f };
            if (costs == null) costs = new ActivityTimeCosts();
        }

        private void Update()
        {
            if (!debugKeysEnabled || GameState.InputLocked) return;

            if (GameInput.DebugTimeUnitPressed)
            {
                Notifier.Show("[DEBUG] Memakai 1 jatah waktu.");
                Spend(TimeCost.OfUnits(1));
            }
            if (GameInput.DebugNextSectionPressed)
            {
                Notifier.Show("[DEBUG] Melewati sisa periode.");
                Spend(TimeCost.OfUnits(unitsPerSection));
            }
            if (GameInput.DebugNextDayPressed)
            {
                DebugJumpToDay(Day + (GameInput.SprintHeld ? 10 : 1));
            }
        }

        // ================= Simpan / muat =================

        public int MinutesIntoSection { get { return minutesIntoSection; } }

        /// <summary>
        /// Dipakai GameSaveSystem untuk memulihkan waktu dari data simpan. SENGAJA tidak memicu OnDayChanged/OnPeriodChanged/OnTimeJumped
        /// (agar warung tidak tutup, makanan tidak menua, hubungan/stok tidak berganti hari). Hanya OnUnitsChanged untuk menyegarkan UI.
        /// </summary>
        public void RestoreState(int day, TimeOfDay period, int unitsRemaining, int minutesInto, int totalMinutes, int daysPlayed, int daysSkipped, bool finalEnded)
        {
            Day = Mathf.Clamp(day, 1, totalDays);
            Period = period;
            UnitsRemaining = Mathf.Clamp(unitsRemaining, 0, unitsPerSection);
            minutesIntoSection = Mathf.Clamp(minutesInto, 0, simulatedMinutesPerSection);
            TotalMinutes = Mathf.Max(0, totalMinutes);
            DaysPlayed = Mathf.Max(1, daysPlayed);
            DaysSkipped = Mathf.Max(0, daysSkipped);
            FinalDayEnded = finalEnded;
            if (OnUnitsChanged != null) OnUnitsChanged();
        }

        // ================= API untuk aktivitas =================

        /// <summary>Biaya (dalam unit) dari sekian sesi. 1 sesi = seluruh jatah satu periode.</summary>
        public TimeCost CostFromSections(float sections)
        {
            if (sections <= 0f) return TimeCost.Free;
            return TimeCost.OfUnits(Mathf.Max(1, Mathf.CeilToInt(sections * unitsPerSection - 0.0001f)));
        }

        /// <summary>
        /// Pakai jatah waktu. Jika biaya >= sisa jatah, sisa periode habis dan waktu berpindah ke periode berikutnya
        /// (aturan sementara untuk biaya yang melebihi sisa jatah; GDD belum menjelaskan kasus ini).
        /// </summary>
        public bool Spend(TimeCost cost)
        {
            if (FinalDayEnded)
            {
                Notifier.Show("Hari ke-" + totalDays + " sudah berakhir.");
                return false;
            }

            if (cost.UntilNextDay)
            {
                SleepToNextDay();
                return true;
            }

            if (cost.Units <= 0) return true;

            if (cost.Units >= UnitsRemaining)
            {
                FinishSection();
                return true;
            }

            UnitsRemaining -= cost.Units;
            int targetMinutes = Mathf.RoundToInt((unitsPerSection - UnitsRemaining) * (float)simulatedMinutesPerSection / unitsPerSection);
            SimulateMinutes(targetMinutes - minutesIntoSection);
            if (OnUnitsChanged != null) OnUnitsChanged();
            return true;
        }

        /// <summary>Teks penjelasan untuk dialog konfirmasi: apa yang akan terjadi jika biaya ini dipakai.</summary>
        public string DescribeCost(TimeCost cost)
        {
            if (cost.IsFree) return "Tidak memakai jatah waktu.";

            if (cost.UntilNextDay)
            {
                return Day >= totalDays
                    ? "Sisa Hari ke-" + Day + " akan dilewati. Ini hari terakhir, hari akan berakhir."
                    : "Sisa Hari ke-" + Day + " akan dilewati. Waktu lanjut ke Pagi Hari ke-" + (Day + 1) + ".";
            }

            if (cost.Units >= UnitsRemaining)
            {
                return "Memakai sisa jatah waktu " + Period + ". Waktu berpindah ke " + NextPeriodLabel() + ".";
            }

            return "Memakai " + cost.Units + " jatah waktu " + Period + ". Sisa jatah: " + (UnitsRemaining - cost.Units) + " dari " + unitsPerSection + ".";
        }

        /// <summary>
        /// TIME SKIP: lewati hari demi hari sampai Pagi hari tujuan. Setiap hari tetap disimulasikan penuh
        /// (menit simulasi, kedaluwarsa per periode, survival, api), hanya pop-up/notifikasi per hari yang ditekan.
        /// Event penting TIDAK boleh dilewati: pemanggil (TimeSkipSystem) yang membatasi hari tujuan.
        /// Mengembalikan jumlah hari yang berlalu.
        /// </summary>
        public int SkipDays(int days)
        {
            if (FinalDayEnded || days <= 0 || IsSkipping) return 0;

            int startDay = Day;
            int target = Mathf.Min(totalDays, Day + days);

            IsSkipping = true;
            int guard = 0;
            while (Day < target && !FinalDayEnded && guard < 100)
            {
                SleepToNextDay();
                guard++;
            }
            IsSkipping = false;

            int elapsed = Day - startDay;
            if (elapsed > 0)
            {
                DaysPlayed++;                       // hari tujuan dimainkan secara normal
                DaysSkipped += Mathf.Max(0, elapsed - 1); // hari di antaranya dilewati
                if (OnTimeSkipped != null) OnTimeSkipped(startDay, Day);

                if (DaysPlayed >= targetDaysPlayed && !FinalDayEnded)
                {
                    TriggerEnding("Target " + targetDaysPlayed + " hari gameplay aktif telah tercapai.");
                }
                else if (Day >= totalDays && !FinalDayEnded)
                {
                    TriggerEnding("Kalender 4 bulan (" + totalDays + " hari) telah selesai.");
                }
            }
            return elapsed;
        }

        public string NextPeriodLabel()
        {
            if (Period == TimeOfDay.Malam)
                return Day >= totalDays ? "akhir permainan (Hari " + totalDays + " selesai)" : "Pagi Hari ke-" + (Day + 1);
            return ((TimeOfDay)((int)Period + 1)).ToString();
        }

        /// <summary>
        /// Pemicu akhir cerita (ending). Dipicu saat kalender mencapai 120 hari (4 bulan)
        /// atau saat total hari yang dimainkan aktif mencapai target ~50-60 hari.
        /// </summary>
        public void TriggerEnding(string reason)
        {
            if (FinalDayEnded) return;
            FinalDayEnded = true;
            IsEndingTriggered = true;
            UnitsRemaining = 0;
            Notifier.Show("Cerita berakhir: " + reason);
            if (OnUnitsChanged != null) OnUnitsChanged();
            if (OnFinalDayEnded != null) OnFinalDayEnded();
            if (OnEndingTriggered != null) OnEndingTriggered(reason);
        }

        // ================= Internal =================

        private void SleepToNextDay()
        {
            int startingDay = Day;
            int guard = 0;
            while (Day == startingDay && !FinalDayEnded && guard < 10)
            {
                FinishSection();
                guard++;
            }
        }

        private void FinishSection()
        {
            SimulateMinutes(simulatedMinutesPerSection - minutesIntoSection);
            AdvancePeriod();
        }

        private void SimulateMinutes(int minutes)
        {
            for (int i = 0; i < minutes; i++)
            {
                TotalMinutes++;
                minutesIntoSection++;
                if (OnMinutePassed != null) OnMinutePassed();
            }
        }

        private void AdvancePeriod()
        {
            minutesIntoSection = 0;
            UnitsRemaining = unitsPerSection;

            if (Period == TimeOfDay.Malam)
            {
                if (Day >= totalDays)
                {
                    TriggerEnding("Kalender 4 bulan (" + totalDays + " hari) telah selesai.");
                    return;
                }

                Day++;
                Period = TimeOfDay.Pagi;
                if (!IsSkipping)
                {
                    DaysPlayed++;
                    if (DaysPlayed >= targetDaysPlayed)
                    {
                        TriggerEnding("Target " + targetDaysPlayed + " hari gameplay aktif telah tercapai.");
                        return;
                    }
                }

                if (OnDayChanged != null) OnDayChanged(Day);
                if (!IsSkipping) Notifier.Show("Hari ke-" + Day + " (" + FormattedDate + ") dimulai. Pagi.");
            }
            else
            {
                Period = (TimeOfDay)((int)Period + 1);
                if (!IsSkipping) Notifier.Show("Waktu beralih ke " + Period + ".");
            }

            if (OnPeriodChanged != null) OnPeriodChanged(Period);
            if (OnUnitsChanged != null) OnUnitsChanged();
        }

        /// <summary>
        /// DEBUG: loncat ke awal (Pagi) hari tertentu TANPA mensimulasikan menit di antaranya
        /// (survival tidak berjalan, api dihitung ulang dari hari). Hanya untuk pengujian.
        /// </summary>
        public void DebugJumpToDay(int targetDay)
        {
            targetDay = Mathf.Clamp(targetDay, 1, totalDays);
            Notifier.Show("[DEBUG] Loncat ke Hari ke-" + targetDay + " (Pagi).");

            if (targetDay > Day) { DaysPlayed++; DaysSkipped += targetDay - Day - 1; }

            Day = targetDay;
            Period = TimeOfDay.Pagi;
            FinalDayEnded = false;
            UnitsRemaining = unitsPerSection;
            minutesIntoSection = 0;
            TotalMinutes = (Day - 1) * SimulatedMinutesPerDay;

            if (OnDayChanged != null) OnDayChanged(Day);
            if (OnPeriodChanged != null) OnPeriodChanged(Period);
            if (OnUnitsChanged != null) OnUnitsChanged();
            if (OnTimeJumped != null) OnTimeJumped();
        }
    }
}
