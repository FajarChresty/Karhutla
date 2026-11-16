using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Fase cerita tingkat tinggi sesuai tabel GDD 3.3.</summary>
    public enum StoryPhase
    {
        Pengenalan,          // Hari 1-14
        TandaAwal,           // sekitar Hari 15
        InsidenKorupsi,      // sekitar Hari 30
        Ketegangan,          // Hari 31-59
        KebakaranMulai,      // sekitar Hari 60
        FokusSurvival        // Hari 61-90
    }

    /// <summary>
    /// Timeline bencana tingkat tinggi (GDD 3.3). Hanya menentukan FASE per hari, bukan isi cerita.
    ///
    /// GDD hanya menulis "sekitar Hari 15 / 30 / 60". Angka hari tepat di bawah adalah NILAI SEMENTARA
    /// yang bisa diubah di Inspector. Detail event, misi, dan insiden korupsi BELUM ada di GDD
    /// (menunggu Narrative GDD dan Investigation &amp; Corruption Sub-GDD), jadi di sini hanya ada
    /// penanda fase dan pesan generik, tanpa event cerita karangan.
    ///
    /// Sebelum kebakaran (Hari &lt; fireStartDay) TIDAK ADA api aktif. Pemain hanya menerima penanda
    /// fase dan suasana lingkungan yang berangsur muram (Tension01), bukan api atau asap.
    /// </summary>
    public class DisasterTimeline : MonoBehaviour
    {
        public static DisasterTimeline Instance { get; private set; }

        [Header("Hari awal tiap fase (NILAI SEMENTARA dari 'sekitar' di GDD)")]
        [Tooltip("GDD: sekitar Hari 15, tanda awal masalah lingkungan mulai terlihat.")]
        public int earlySignsDay = 15;
        [Tooltip("GDD: sekitar Hari 30, insiden terkait korupsi mulai berkembang.")]
        public int corruptionIncidentDay = 30;
        [Tooltip("GDD: Hari 31-59, ketegangan dan kondisi lingkungan memburuk. Fase ini mulai sehari setelah insiden.")]
        public int tensionStartDay = 31;
        [Tooltip("GDD: sekitar Hari 60, kebakaran hutan mulai. Tidak ada api aktif sebelum hari ini.")]
        public int fireStartDay = 60;
        [Tooltip("GDD: Hari 61-90, permainan makin berfokus pada survival.")]
        public int survivalFocusDay = 61;

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
            int[] fixedDays = { earlySignsDay, corruptionIncidentDay, tensionStartDay, fireStartDay, survivalFocusDay };
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
                   || day == survivalFocusDay || (extraKeyDays != null && extraKeyDays.Contains(day));
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
                case StoryPhase.Pengenalan: return "Pengenalan desa";
                case StoryPhase.TandaAwal: return "Tanda awal masalah lingkungan";
                case StoryPhase.InsidenKorupsi: return "Insiden korupsi berkembang";
                case StoryPhase.Ketegangan: return "Ketegangan meningkat";
                case StoryPhase.KebakaranMulai: return "Kebakaran hutan dimulai";
                case StoryPhase.FokusSurvival: return "Fokus survival";
            }
            return "";
        }

        private void HandleDayChanged(int day)
        {
            StoryPhase newPhase = PhaseForDay(day);
            if (newPhase == CurrentPhase) return;
            CurrentPhase = newPhase;

            // Pesan generik saja. Isi event sebenarnya menunggu Narrative GDD.
            switch (newPhase)
            {
                case StoryPhase.TandaAwal:
                    Notifier.Show("Hari ke-" + day + ": tanda awal masalah lingkungan mulai terlihat di sekitar desa.");
                    break;
                case StoryPhase.InsidenKorupsi:
                    Notifier.Show("Hari ke-" + day + ": sebuah insiden terkait korupsi mulai berkembang.");
                    break;
                // KebakaranMulai: pesan ditampilkan DisasterSystem saat api benar-benar mulai.
            }

            if (OnPhaseChanged != null) OnPhaseChanged(newPhase);
        }
    }
}
