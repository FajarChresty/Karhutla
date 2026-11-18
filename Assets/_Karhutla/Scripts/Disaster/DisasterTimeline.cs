using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Fase cerita tingkat tinggi skala 4 Bulan (120 Hari).</summary>
    public enum StoryPhase
    {
        Pengenalan,          // Bulan 1 (Hari 1-19)
        TandaAwal,           // Bulan 1 (sekitar Hari 20)
        InsidenKorupsi,      // Bulan 2 (sekitar Hari 40)
        Ketegangan,          // Bulan 2 (Hari 45-74)
        KebakaranMulai,      // Bulan 3 (sekitar Hari 75)
        FokusSurvival,       // Bulan 3 (Hari 76-94)
        PuncakDarurat        // Bulan 4 (Hari 95-120: Puncak Darurat Karhutla)
    }

    /// <summary>
    /// Timeline bencana tingkat tinggi (skala 4 Bulan / 120 Hari). Menentukan FASE cerita per hari.
    ///
    /// Jadwal disesuaikan dengan arahan 4 Bulan:
    ///  - Bulan 1: Pengenalan desa dan tanda awal (Hari 20).
    ///  - Bulan 2: Insiden korupsi (Hari 40) dan eskalasi ketegangan (Hari 45).
    ///  - Bulan 3: Kebakaran hutan mulai (sekitar Hari 75) dan fokus survival (Hari 76).
    ///  - Bulan 4: Puncak darurat karhutla (Hari 95-120).
    /// </summary>
    public class DisasterTimeline : MonoBehaviour
    {
        public static DisasterTimeline Instance { get; private set; }

        [Header("Jadwal Fase Cerita 4 Bulan (120 Hari)")]
        [Tooltip("Bulan 1: Tanda awal masalah lingkungan mulai terlihat (sekitar Hari 20).")]
        public int earlySignsDay = 20;
        [Tooltip("Bulan 2: Insiden terkait korupsi mulai berkembang (sekitar Hari 40).")]
        public int corruptionIncidentDay = 40;
        [Tooltip("Bulan 2: Ketegangan dan kondisi lingkungan memburuk (Hari 45).")]
        public int tensionStartDay = 45;
        [Tooltip("Bulan 3: Kebakaran hutan mulai (sekitar Hari 75). Tidak ada api aktif sebelum hari ini.")]
        public int fireStartDay = 75;
        [Tooltip("Bulan 3: Permainan berfokus penuh pada survival (Hari 76).")]
        public int survivalFocusDay = 76;
        [Tooltip("Bulan 4: Puncak darurat kebakaran hutan dan kabut asap (Hari 95-120).")]
        public int peakEmergencyDay = 95;

        [Header("Hari penting tambahan (untuk time skip)")]
        [Tooltip("Hari lain yang punya event cerita penting dan TIDAK boleh dilewati time skip. Hari fase di atas otomatis dianggap penting.")]
        public System.Collections.Generic.List<int> extraKeyDays = new System.Collections.Generic.List<int>();

        public StoryPhase CurrentPhase { get; private set; }

        /// <summary>
        /// Hari penting pertama setelah 'day' (event cerita/bencana). Time skip berhenti paling jauh di hari ini.
        /// Sumber tunggal: semua hari di sini berasal dari konfigurasi timeline di atas (bukan angka tertanam di script lain).
        /// Mengembalikan int.MaxValue bila tidak ada lagi.
        /// </summary>
        public int NextKeyDay(int day)
        {
            int best = int.MaxValue;
            int[] fixedDays = { earlySignsDay, corruptionIncidentDay, tensionStartDay, fireStartDay, survivalFocusDay, peakEmergencyDay };
            for (int i = 0; i < fixedDays.Length; i++)
            {
                if (fixedDays[i] > day && fixedDays[i] < best) best = fixedDays[i];
            }
            if (extraKeyDays != null)
            {
                for (int i = 0; i < extraKeyDays.Count; i++)
                {
                    if (extraKeyDays[i] > day && extraKeyDays[i] < best) best = extraKeyDays[i];
                }
            }
            return best;
        }

        public bool IsKeyDay(int day)
        {
            return day == earlySignsDay || day == corruptionIncidentDay || day == tensionStartDay || day == fireStartDay
                   || day == survivalFocusDay || day == peakEmergencyDay || (extraKeyDays != null && extraKeyDays.Contains(day));
        }

        /// <summary>
        /// 0..1: seberapa muram suasana lingkungan SEBELUM kebakaran (GDD 9.5 'Transition': warna memudar, makin mendung).
        /// 0 sebelum earlySignsDay, naik linear sampai 1 pada hari terakhir sebelum api. Hanya dipakai untuk visual.
        /// </summary>
        public float Tension01
        {
            get
            {
                var time = TimeManager.Instance;
                if (time == null || time.Day < earlySignsDay) return 0f;
                float end = Mathf.Max(earlySignsDay + 1, fireStartDay - 1);
                return Mathf.Clamp01(Mathf.InverseLerp(earlySignsDay, end, time.Day));
            }
        }

        public event Action<StoryPhase> OnPhaseChanged;

        private void Awake() { Instance = this; }

        private void Start()
        {
            var time = TimeManager.Instance;
            if (time == null) return;

            CurrentPhase = PhaseForDay(time.Day);
            time.OnDayChanged += HandleDayChanged;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= HandleDayChanged;
            if (Instance == this) Instance = null;
        }

        /// <summary>Sinkronkan fase cerita dengan hari sekarang tanpa pesan/event (dipakai setelah memuat data simpan).</summary>
        public void Resync()
        {
            var time = TimeManager.Instance;
            if (time != null) CurrentPhase = PhaseForDay(time.Day);
        }

        public StoryPhase PhaseForDay(int day)
        {
            if (day >= peakEmergencyDay) return StoryPhase.PuncakDarurat;
            if (day >= survivalFocusDay) return StoryPhase.FokusSurvival;
            if (day >= fireStartDay) return StoryPhase.KebakaranMulai;
            if (day >= tensionStartDay) return StoryPhase.Ketegangan;
            if (day >= corruptionIncidentDay) return StoryPhase.InsidenKorupsi;
            if (day >= earlySignsDay) return StoryPhase.TandaAwal;
            return StoryPhase.Pengenalan;
        }

        public static string Label(StoryPhase phase)
        {
            switch (phase)
            {
                case StoryPhase.Pengenalan: return "Bulan 1: Pengenalan desa";
                case StoryPhase.TandaAwal: return "Bulan 1: Tanda awal masalah lingkungan";
                case StoryPhase.InsidenKorupsi: return "Bulan 2: Insiden korupsi berkembang";
                case StoryPhase.Ketegangan: return "Bulan 2: Ketegangan meningkat";
                case StoryPhase.KebakaranMulai: return "Bulan 3: Kebakaran hutan dimulai";
                case StoryPhase.FokusSurvival: return "Bulan 3: Fokus survival";
                case StoryPhase.PuncakDarurat: return "Bulan 4: Puncak Darurat Karhutla";
            }
            return "";
        }

        private void HandleDayChanged(int day)
        {
            StoryPhase newPhase = PhaseForDay(day);
            if (newPhase == CurrentPhase) return;
            CurrentPhase = newPhase;

            // Pesan fase narasi generik (dummy sebelum narrative final diintegrasikan)
            switch (newPhase)
            {
                case StoryPhase.TandaAwal:
                    Notifier.Show("Hari ke-" + day + " (Bulan 1): tanda awal masalah lingkungan mulai terlihat di sekitar desa.");
                    break;
                case StoryPhase.InsidenKorupsi:
                    Notifier.Show("Hari ke-" + day + " (Bulan 2): sebuah insiden terkait korupsi perizinan lahan mulai memanas.");
                    break;
                case StoryPhase.Ketegangan:
                    Notifier.Show("Hari ke-" + day + " (Bulan 2): ketegangan warga dan kecurigaan meningkat seiring kemarau parah.");
                    break;
                case StoryPhase.PuncakDarurat:
                    Notifier.Show("Hari ke-" + day + " (Bulan 4): DARURAT! Kebakaran hutan mencapai puncak darurat di sekitar desa!");
                    break;
                // KebakaranMulai: pesan ditampilkan DisasterSystem saat api benar-benar mulai.
            }

            if (OnPhaseChanged != null) OnPhaseChanged(newPhase);
        }
    }
}
