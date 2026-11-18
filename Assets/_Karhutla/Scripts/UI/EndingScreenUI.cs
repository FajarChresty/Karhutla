using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Layar penutupan cerita (Ending Screen UI).
    /// Menampilkan statistik komprehensif performa 4 Bulan / 50-60 hari gameplay aktif.
    ///
    /// [DUMMY UI]:
    /// Seluruh tampilan ini dibuat secara prosedural menggunakan Unity UI bawaan (UIFactory),
    /// tanpa membutuhkan aset grafis eksternal. Ketika UI art final sudah selesai,
    /// struktur ini bisa langsung digantikan dengan prefab antarmuka final.
    /// </summary>
    public class EndingScreenUI : MonoBehaviour
    {
        public static EndingScreenUI Instance { get; private set; }

        private GameObject rootPanel;
        private Text titleText;
        private Text subtitleText;
        private Text statsText;
        private Text summaryText;
        private Text footerText;

        private bool isOpen;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Transform canvasRoot = UIFactory.Canvas.transform;

            // Panel latar belakang overlay hitam pekat
            var panelRt = UIFactory.CreatePanel(canvasRoot, "DummyEndingScreenPanel", new Color(0.03f, 0.04f, 0.06f, 0.96f));
            UIFactory.Stretch(panelRt, 0f, 0f, 0f, 0f);
            panelRt.GetComponent<Image>().raycastTarget = true;
            rootPanel = panelRt.gameObject;

            // Box Konten Utama
            var boxRt = UIFactory.CreatePanel(panelRt, "EndingContentBox", new Color(0.08f, 0.1f, 0.14f, 0.92f));
            UIFactory.Place(boxRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 680f));

            // Judul Utama Ending
            titleText = UIFactory.CreateText(boxRt, "EndingTitle", 36, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.35f));
            titleText.fontStyle = FontStyle.Bold;
            UIFactory.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1100f, 50f));

            // Subtitle Alasan Ending
            subtitleText = UIFactory.CreateText(boxRt, "EndingSubtitle", 22, TextAnchor.UpperCenter, new Color(0.75f, 0.85f, 1f));
            UIFactory.Place(subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(1100f, 40f));

            // Panel Kiri: Statistik Akhir
            var leftPanel = UIFactory.CreatePanel(boxRt, "LeftStatsPanel", new Color(0.12f, 0.15f, 0.2f, 0.85f));
            UIFactory.Place(leftPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, -20f), new Vector2(520f, 460f));

            var leftHeader = UIFactory.CreateText(leftPanel, "StatsHeader", 24, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.95f));
            leftHeader.fontStyle = FontStyle.Bold;
            leftHeader.text = "STATISTIK PERJALANAN";
            UIFactory.Place(leftHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -20f), new Vector2(470f, 35f));

            statsText = UIFactory.CreateText(leftPanel, "StatsContent", 20, TextAnchor.UpperLeft, Color.white);
            UIFactory.Place(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -60f), new Vector2(470f, 380f));

            // Panel Kanan: Evaluasi Naratif
            var rightPanel = UIFactory.CreatePanel(boxRt, "RightSummaryPanel", new Color(0.12f, 0.15f, 0.2f, 0.85f));
            UIFactory.Place(rightPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, -20f), new Vector2(520f, 460f));

            var rightHeader = UIFactory.CreateText(rightPanel, "SummaryHeader", 24, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.95f));
            rightHeader.fontStyle = FontStyle.Bold;
            rightHeader.text = "EVALUASI AKHIR & NARASI";
            UIFactory.Place(rightHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -20f), new Vector2(470f, 35f));

            summaryText = UIFactory.CreateText(rightPanel, "SummaryContent", 20, TextAnchor.UpperLeft, Color.white);
            UIFactory.Place(summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -60f), new Vector2(470f, 380f));

            // Footer Instruksi Tombol
            footerText = UIFactory.CreateText(boxRt, "EndingFooter", 22, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.8f));
            UIFactory.Place(footerText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(1100f, 40f));
            footerText.text = "[E] Mulai Permainan Baru          [Q / Esc] Keluar Game";

            rootPanel.SetActive(false);
        }

        public void Show(string reason)
        {
            if (rootPanel == null) BuildUI();

            isOpen = true;
            rootPanel.SetActive(true);
            GameState.SetInputLocked(true);

            // Kumpulkan data statistik
            var time = TimeManager.Instance;
            var inv = Inventory.Instance;
            var surv = SurvivalSystem.Instance;
            var dis = DisasterSystem.Instance;
            var rel = RelationshipSystem.Instance;

            int day = time != null ? time.Day : 1;
            int month = time != null ? time.Month : 1;
            int dayInMonth = time != null ? time.DayInMonth : 1;
            int daysPlayed = time != null ? time.DaysPlayed : 1;
            int daysSkipped = time != null ? time.DaysSkipped : 0;
            int money = inv != null ? inv.Money : 0;

            float health = surv != null ? surv.Health : 100f;
            float smoke = surv != null ? surv.SmokeExposure : 0f;
            DangerLevel danger = dis != null ? dis.CurrentDanger : DangerLevel.Aman;

            // Periksa 3 Objektif Utama GDD
            bool corr = InvestigationSystem.Instance != null && InvestigationSystem.Instance.CorruptionExposed;
            bool fire = FirefightingMission.Instance != null && FirefightingMission.Instance.IsFireContained;
            bool bankrupt = (reason != null && reason.IndexOf("KEBANGKRUTAN", System.StringComparison.OrdinalIgnoreCase) >= 0)
                || (BankruptcySystem.Instance != null && BankruptcySystem.Instance.IsInFinancialCrisis && BankruptcySystem.Instance.ConsecutiveCriticalDays >= 2);

            // Rata-rata hubungan sosial
            float avgSocial = 0f;
            int npcCount = 0;
            var relSb = new StringBuilder();
            if (rel != null && rel.RegisteredIds != null)
            {
                foreach (string id in rel.RegisteredIds)
                {
                    var entry = rel.Get(id);
                    if (entry == null) continue;

                    float val = entry.Value;
                    RelationshipTier tier = rel.GetTier(id);
                    string name = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.Id;

                    avgSocial += val;
                    npcCount++;
                    relSb.Append("  \u2022 ").Append(name).Append(": ").Append(Mathf.RoundToInt(val))
                         .Append(" (").Append(tier).Append(")\n");
                }
                if (npcCount > 0) avgSocial /= npcCount;
            }

            // Tentukan Hasil Ending dari 3-4 Variasi Utama
            var endingData = DummyStoryData.DetermineEnding(daysPlayed, daysSkipped, money, avgSocial, danger, health, corr, fire, bankrupt);

            titleText.text = endingData.Title;
            titleText.color = endingData.ThemeColor;
            subtitleText.text = endingData.Subtitle + (string.IsNullOrEmpty(reason) ? "" : "  [" + reason + "]");

            var statsSb = new StringBuilder();
            statsSb.Append("<b>Kalender Cerita:</b> Bulan ").Append(month).Append(" - Hari ").Append(dayInMonth)
                   .Append(" (Total: Hari ke-").Append(day).Append(" / ").Append(time != null ? time.totalDays : 120).Append(")\n\n");
            statsSb.Append("<b>Hari Dimainkan Aktif:</b> ").Append(daysPlayed).Append(" dari ").Append(time != null ? time.targetDaysPlayed : 60).Append(" hari\n");
            statsSb.Append("<b>Hari Dilewati (Plot Skip):</b> ").Append(daysSkipped).Append(" hari\n\n");
            statsSb.Append("<b>Keuangan Warung:</b> ").Append(TextFormat.Rupiah(money)).Append("\n");

            int unlockedRecipes = RecipeManager.Instance != null ? RecipeManager.Instance.GetUnlockedRecipes().Count : 5;
            statsSb.Append("<b>Resep Masakan Terbuka:</b> ").Append(unlockedRecipes).Append(" / 12 menu\n\n");

            statsSb.Append("<b>Kondisi Pemain:</b> Kesehatan ").Append(Mathf.RoundToInt(health)).Append("%  |  Paparan Asap ").Append(Mathf.RoundToInt(smoke)).Append("%\n");
            statsSb.Append("<b>Status Bencana:</b> ").Append(danger.ToString().ToUpper());
            if (dis != null && dis.FireStarted) statsSb.Append(" (Penyebaran Api: ").Append(Mathf.RoundToInt(dis.FireProgress01 * 100f)).Append("%)");
            statsSb.Append("\n\n");

            statsSb.Append("<b>Hubungan dengan Warga:</b>\n");
            if (npcCount > 0) statsSb.Append(relSb);
            else statsSb.Append("  Belum ada data relasi warga.\n");

            statsText.text = statsSb.ToString();

            // Evaluasi Naratif & 3 Objektif GDD
            var summarySb = new StringBuilder();
            summarySb.Append("<b>EVALUASI 3 MISI UTAMA (GDD 3.4):</b>\n");
            summarySb.Append("<b>1. Bertahan Hidup:</b> ").Append(endingData.Objective1Status).Append("\n");
            summarySb.Append("<b>2. Mengungkap Korupsi:</b> ").Append(endingData.Objective2Status);
            if (InvestigationSystem.Instance != null)
                summarySb.Append(" [").Append(InvestigationSystem.Instance.ClueCount).Append("/").Append(InvestigationSystem.Instance.TotalClues).Append(" bukti]");
            summarySb.Append("\n");
            summarySb.Append("<b>3. Mengendalikan Karhutla:</b> ").Append(endingData.Objective3Status);
            if (FirefightingMission.Instance != null)
                summarySb.Append(" [").Append(FirefightingMission.Instance.CompletedFirebreaks).Append("/3 sekat, ").Append(FirefightingMission.Instance.ExtinguishedTreesCount).Append(" pohon dipadamkan]");
            summarySb.Append("\n\n");

            summarySb.Append("<b>EPILOG CERITA:</b>\n");
            summarySb.Append(endingData.EpilogueText);

            summaryText.text = summarySb.ToString();
        }

        private void Update()
        {
            if (!isOpen) return;

            // Pilihan [E]: Mulai Baru (Reset Save)
            if (GameInput.ConfirmPressed)
            {
                GameSaveSystem.ClearSavedData();
                GameState.SetInputLocked(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            // Pilihan [Q] atau Esc: Keluar Game
            else if (GameInput.CancelPressed)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
