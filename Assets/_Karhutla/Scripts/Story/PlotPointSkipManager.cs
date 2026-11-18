using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Sistem "Plot Point Skip" otomatis: melompati beberapa hari yang tidak memerlukan gameplay aktif
    /// setelah suatu event narasi penting selesai.
    ///
    /// Sistem ini memastikan pemain hanya memainkan target 50-60 hari gameplay aktif
    /// di dalam total kalender cerita 4 Bulan (120 Hari), sesuai arahan game design.
    ///
    /// [DUMMY UI & ASSETS]:
    /// Tampilan transisi cutscene teks di sini menggunakan UI dummy procedural (uGUI).
    /// Ketika aset cutscene visual atau ilustrasi transisi final sudah ada,
    /// cukup ganti metode BuildTransitionUI() atau sambungkan prefab final.
    /// </summary>
    public class PlotPointSkipManager : MonoBehaviour
    {
        public static PlotPointSkipManager Instance { get; private set; }

        [Header("Pengaturan")]
        [Tooltip("Izinkan skip otomatis saat event/fase penting tercapai.")]
        public bool allowAutoPlotSkips = true;
        [Tooltip("Indeks skenario dummy yang sedang aktif.")]
        public int currentScenarioIndex = 0;

        private GameObject transitionPanel;
        private Text titleText;
        private Text narrationText;
        private Text daysChangeText;
        private Text promptText;

        private bool isTransitionActive = false;
        private Action pendingOnComplete = null;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (isTransitionActive) GameState.SetInputLocked(false);
        }

        private void Start()
        {
            BuildTransitionUI();
        }

        private void Update()
        {
            // Debug Shortcut: Shift + P untuk menguji Plot Point Skip langsung
            if (GameInput.SprintHeld && GameInput.DebugPlotSkipPressed)
            {
                TriggerNextScenarioSkip();
            }

            if (!isTransitionActive) return;

            // Tekan E atau Space untuk menyelesaikan transisi dan melanjutkan gameplay
            if (GameInput.AdvanceDialoguePressed || GameInput.ConfirmPressed)
            {
                CloseTransition();
            }
        }

        /// <summary>
        /// Membuat UI transisi narasi dummy secara prosedural (tanpa perlu prefab eksternal).
        /// </summary>
        private void BuildTransitionUI()
        {
            Transform canvasRoot = UIFactory.Canvas.transform;

            // Panel latar belakang hitam elegan layar penuh
            var panelRt = UIFactory.CreatePanel(canvasRoot, "DummyPlotSkipTransitionPanel", new Color(0.04f, 0.05f, 0.07f, 0.94f));
            UIFactory.Stretch(panelRt, 0f, 0f, 0f, 0f);
            panelRt.GetComponent<Image>().raycastTarget = true;
            transitionPanel = panelRt.gameObject;

            // Kontainer konten tengah
            var contentBox = UIFactory.CreatePanel(panelRt, "ContentBox", new Color(0.1f, 0.12f, 0.16f, 0.85f));
            UIFactory.Place(contentBox, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 520f));

            // Judul Plot Point
            titleText = UIFactory.CreateText(contentBox, "TitleText", 34, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.3f));
            titleText.fontStyle = FontStyle.Bold;
            UIFactory.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(900f, 60f));

            // Info Perubahan Hari
            daysChangeText = UIFactory.CreateText(contentBox, "DaysChangeText", 24, TextAnchor.MiddleCenter, new Color(0.7f, 0.9f, 1f));
            UIFactory.Place(daysChangeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(900f, 40f));

            // Narasi Transisi Cerita (Dummy Story Text)
            narrationText = UIFactory.CreateText(contentBox, "NarrationText", 24, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Place(narrationText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(860f, 240f));

            // Tombol petunjuk lanjut
            promptText = UIFactory.CreateText(contentBox, "PromptText", 22, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.75f));
            UIFactory.Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(800f, 40f));
            promptText.text = "[E / Space] Lanjutkan Perjalanan";

            transitionPanel.SetActive(false);
        }

        /// <summary>
        /// Memicu Plot Point Skip dengan jumlah hari tertentu dan narasi transisi.
        /// </summary>
        public void TriggerPlotSkip(int daysToSkip, string title, string narration, Action onComplete = null)
        {
            var time = TimeManager.Instance;
            if (time == null || time.FinalDayEnded || daysToSkip <= 0)
            {
                if (onComplete != null) onComplete();
                return;
            }

            int startDay = time.Day;
            string startWeekday = time.WeekdayName(startDay);
            int startMonth = time.Month;
            int startDayInMonth = time.DayInMonth;

            // Eksekusi lompatan hari di TimeManager
            var survival = SurvivalSystem.Instance;
            if (survival != null) survival.IsSleeping = true;
            int elapsed = time.SkipDays(daysToSkip);
            if (survival != null) survival.IsSleeping = false;

            int newDay = time.Day;
            string newWeekday = time.WeekdayName(newDay);
            int newMonth = time.Month;
            int newDayInMonth = time.DayInMonth;

            pendingOnComplete = onComplete;
            ShowTransition(title, narration, startDay, startMonth, startDayInMonth, startWeekday, newDay, newMonth, newDayInMonth, newWeekday, elapsed);
        }

        /// <summary>
        /// Memicu Plot Point Skip langsung ke hari tertentu.
        /// </summary>
        public void TriggerPlotSkipToDay(int targetDay, string title, string narration, Action onComplete = null)
        {
            var time = TimeManager.Instance;
            if (time == null || targetDay <= time.Day)
            {
                if (onComplete != null) onComplete();
                return;
            }

            int days = targetDay - time.Day;
            TriggerPlotSkip(days, title, narration, onComplete);
        }

        /// <summary>
        /// Memicu skenario plot point skip dummy berikutnya dari database.
        /// Sangat berguna untuk pengujian dan otomasi alur cerita 4 bulan.
        /// </summary>
        public void TriggerNextScenarioSkip(Action onComplete = null)
        {
            if (DummyStoryData.DefaultPlotSkips == null || DummyStoryData.DefaultPlotSkips.Count == 0) return;

            int index = Mathf.Clamp(currentScenarioIndex, 0, DummyStoryData.DefaultPlotSkips.Count - 1);
            var scenario = DummyStoryData.DefaultPlotSkips[index];
            currentScenarioIndex = (currentScenarioIndex + 1) % DummyStoryData.DefaultPlotSkips.Count;

            TriggerPlotSkip(scenario.daysToSkip, scenario.title, scenario.transitionNarration, onComplete);
        }

        private void ShowTransition(string title, string narration, int fromDay, int fromMonth, int fromDayInMonth, string fromWeekday, int toDay, int toMonth, int toDayInMonth, string toWeekday, int elapsed)
        {
            if (transitionPanel == null) BuildTransitionUI();

            titleText.text = "PLOT POINT: " + (string.IsNullOrEmpty(title) ? "WAKTU BERLALU" : title.ToUpper());
            narrationText.text = narration;

            var time = TimeManager.Instance;
            daysChangeText.text = string.Format(
                "Bulan {0} - Hari {1} ({2})   \u27A4   <b>Bulan {3} - Hari {4} ({5})</b>   (+{6} Hari Cerita)\n" +
                "<size=20><color=#FFDD88>Hari Dimainkan Aktif: {7} / {8} | Hari Dilewati: {9}</color></size>",
                fromMonth, fromDayInMonth, fromWeekday,
                toMonth, toDayInMonth, toWeekday,
                elapsed,
                time != null ? time.DaysPlayed : 1,
                time != null ? time.targetDaysPlayed : 60,
                time != null ? time.DaysSkipped : 0
            );

            transitionPanel.SetActive(true);
            isTransitionActive = true;
            GameState.SetInputLocked(true);
        }

        private void CloseTransition()
        {
            isTransitionActive = false;
            if (transitionPanel != null) transitionPanel.SetActive(false);
            GameState.SetInputLocked(false);

            var callback = pendingOnComplete;
            pendingOnComplete = null;
            if (callback != null) callback();
        }
    }
}
