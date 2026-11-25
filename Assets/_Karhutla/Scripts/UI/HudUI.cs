using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// HUD utama: jam & uang, bar survival, indikator bahaya kebakaran, pesan singkat (toast), dan bantuan kontrol.
    /// Hanya MEMBACA data dari sistem lain; tidak mengubah gameplay.
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        private struct Toast
        {
            public string text;
            public float expireAt;
        }

        private Text timeText;
        private Text dangerTitle;
        private Text dangerDetail;
        private Image dangerBackground;
        private Text maskText;
        private UIFactory.StatBar weightBar;
        private Text toastText;

        private UIFactory.StatBar hungerBar, thirstBar, fatigueBar, smokeBar, healthBar;

        private readonly List<Toast> toasts = new List<Toast>();
        private readonly StringBuilder sb = new StringBuilder();
        private string lastToastString = "";

        private void Start()
        {
            Build();
            Notifier.Message += OnMessage;
        }

        private void OnDestroy()
        {
            Notifier.Message -= OnMessage;
        }

        private void Build()
        {
            Transform root = UIFactory.Canvas.transform;

            // Panel waktu (kiri atas) - ukuran diperkecil dan dirapikan agar tidak bertabrakan dengan banner tengah
            var timePanel = UIFactory.CreatePanel(root, "Panel Waktu", new Color(0f, 0f, 0f, 0.65f));
            UIFactory.Place(timePanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -25f), new Vector2(500f, 165f));
            timeText = UIFactory.CreateText(timePanel, "Teks Waktu", 20, TextAnchor.MiddleLeft, Color.white);
            UIFactory.Stretch(timeText.rectTransform, 16f, 8f, 12f, 8f);

            // Banner bahaya (tengah atas) - dirampingkan agar proporsional
            var dangerPanel = UIFactory.CreatePanel(root, "Panel Bahaya", new Color(0.2f, 0.7f, 0.3f, 0.85f));
            UIFactory.Place(dangerPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(520f, 80f));
            dangerBackground = dangerPanel.GetComponent<Image>();
            dangerTitle = UIFactory.CreateText(dangerPanel, "Judul Bahaya", 26, TextAnchor.UpperCenter, Color.white);
            dangerTitle.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(dangerTitle.rectTransform, 8f, 0f, 8f, 6f);
            dangerDetail = UIFactory.CreateText(dangerPanel, "Detail Bahaya", 17, TextAnchor.LowerCenter, Color.white);
            UIFactory.Stretch(dangerDetail.rectTransform, 8f, 6f, 8f, 0f);

            // Bar survival (kiri bawah)
            Vector2 anchor = new Vector2(0f, 0f);
            Vector2 pivot = new Vector2(0f, 0f);
            Vector2 size = new Vector2(340f, 32f);
            healthBar = UIFactory.StatBar.Create(root, "Bar Kesehatan", anchor, pivot, new Vector2(30f, 30f), size);
            smokeBar = UIFactory.StatBar.Create(root, "Bar Asap", anchor, pivot, new Vector2(30f, 70f), size);
            fatigueBar = UIFactory.StatBar.Create(root, "Bar Lelah", anchor, pivot, new Vector2(30f, 110f), size);
            thirstBar = UIFactory.StatBar.Create(root, "Bar Haus", anchor, pivot, new Vector2(30f, 150f), size);
            hungerBar = UIFactory.StatBar.Create(root, "Bar Lapar", anchor, pivot, new Vector2(30f, 190f), size);

            weightBar = UIFactory.StatBar.Create(root, "Bar Berat", anchor, pivot, new Vector2(30f, 230f), size);

            maskText = UIFactory.CreateText(root, "Teks Masker", 22, TextAnchor.LowerLeft, new Color(0.7f, 0.95f, 1f));
            UIFactory.Place(maskText.rectTransform, anchor, pivot, new Vector2(30f, 272f), new Vector2(340f, 30f));

            // Toast notifikasi (tengah bawah) - diperkecil agar tidak menutupi tengah layar
            toastText = UIFactory.CreateText(root, "Teks Toast", 18, TextAnchor.LowerCenter, Color.white);
            UIFactory.Place(toastText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 260f), new Vector2(700f, 90f));
            var shadow = toastText.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Bantuan kontrol (kanan bawah) - font diperkecil dan padat
            var help = UIFactory.CreateText(root, "Teks Kontrol", 13, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.7f));
            UIFactory.Place(help.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(480f, 180f));
            help.text = "WASD  Gerak     Shift  Lari\nE  Interaksi     Q  Aksi kedua     R  Aksi ketiga (barter)\nZ / C  Putar kamera (4 Arah)     Scroll  Zoom\nI  Inventaris     K  Hubungan     L  Ringkasan hari\n1-9  Pakai item     G  Buang item basi\n[DEBUG]  T jatah   Y periode   U hari (Shift+U +10)   F api\n[DEBUG]  F7 Sinematik H40 (Korupsi)   F8 Sinematik H75 (Karhutla)\n[DEBUG]  Shift+P Plot Point Skip (simulasi lompatan cerita)";
            help.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);
        }

        private void OnMessage(string message)
        {
            toasts.Add(new Toast { text = message, expireAt = Time.unscaledTime + 5f });
            if (toasts.Count > 4) toasts.RemoveAt(0);
        }

        private void Update()
        {
            if (timeText == null) return;
            UpdateTime();
            UpdateBars();
            UpdateDanger();
            UpdateToasts();
        }

        private void UpdateTime()
        {
            var time = TimeManager.Instance;
            var inv = Inventory.Instance;
            if (time == null) return;

            string money = inv != null ? TextFormat.Rupiah(inv.Money) : "-";

            // Jatah waktu periode berjalan, mis. [####] 4/4
            var barText = new StringBuilder("[");
            for (int i = 0; i < time.unitsPerSection; i++) barText.Append(i < time.UnitsRemaining ? '#' : '-');
            barText.Append("] ").Append(time.UnitsRemaining).Append('/').Append(time.unitsPerSection);

            var timeline = DisasterTimeline.Instance;
            string phase = timeline != null ? DisasterTimeline.Label(timeline.CurrentPhase) : "";

            string weekday = time.WeekdayName(time.Day);
            string view = CameraFollow.Instance != null ? "   " + CameraFollow.Instance.ViewName : "";

            // Format kalender 4 Bulan ("Bulan 1 - Hari 10") dan pencatatan "Hari Dimainkan: X dari 60"
            string dateStr = "<b>Bulan " + time.Month + " - Hari " + time.DayInMonth + "</b> (Hari " + time.Day + "/" + time.totalDays + ", " + weekday + ")";
            string header = time.FinalDayEnded
                ? dateStr + "   <color=#FF7777>(BERAKHIR)</color>"
                : dateStr + "   <color=#FFFF88>" + time.Period.ToString().ToUpper() + "</color>";

            string playCountStr = "Dimainkan: " + time.DaysPlayed + "/" + time.targetDaysPlayed + " hari   (Dilewati: " + time.DaysSkipped + " hari)";

            timeText.text = header + "\n" +
                            "Jatah waktu " + barText + "  |  " + money + "\n" +
                            "<size=16><color=#CCDDEE>" + phase + view + "</color></size>\n" +
                            "<size=15><color=#FFE088>" + playCountStr + "</color></size>";
        }

        private void UpdateBars()
        {
            var s = SurvivalSystem.Instance;
            if (s == null) return;

            hungerBar.Set(s.Hunger / 100f, UIFactory.Gradient(s.Hunger / 100f), "Lapar  " + Mathf.RoundToInt(s.Hunger));
            thirstBar.Set(s.Thirst / 100f, UIFactory.Gradient(s.Thirst / 100f), "Haus  " + Mathf.RoundToInt(s.Thirst));
            fatigueBar.Set(s.Fatigue / 100f, UIFactory.Gradient(s.Fatigue / 100f), "Lelah  " + Mathf.RoundToInt(s.Fatigue));
            smokeBar.Set(s.SmokeExposure / 100f, UIFactory.Gradient(s.SmokeExposure / 100f), "Paparan Asap  " + Mathf.RoundToInt(s.SmokeExposure));
            healthBar.Set(s.Health / 100f, UIFactory.Gradient(1f - s.Health / 100f), "Kesehatan  " + Mathf.RoundToInt(s.Health));

            var inv = Inventory.Instance;
            if (inv != null && weightBar != null)
            {
                float frac = inv.MaxWeight > 0f ? inv.CurrentWeight / inv.MaxWeight : 0f;
                weightBar.Set(frac, UIFactory.Gradient(frac), "Beban  " + inv.CurrentWeight.ToString("0.#") + " / " + inv.MaxWeight.ToString("0.#") + " kg");
            }

            if (s.IsMasked)
            {
                int m = s.MaskMinutesLeft;
                maskText.text = "Masker aktif (" + (m / 60) + "j " + (m % 60) + "m)";
            }
            else
            {
                maskText.text = "";
            }
        }

        private void UpdateDanger()
        {
            var d = DisasterSystem.Instance;
            if (d == null) return;

            Color color;
            switch (d.CurrentDanger)
            {
                case DangerLevel.Waspada: color = new Color(0.85f, 0.7f, 0.1f, 0.9f); break;
                case DangerLevel.Bahaya: color = new Color(0.95f, 0.45f, 0.1f, 0.9f); break;
                case DangerLevel.Kritis: color = new Color(0.8f, 0.1f, 0.1f, 0.9f); break;
                default: color = new Color(0.2f, 0.65f, 0.3f, 0.85f); break;
            }
            dangerBackground.color = color;
            dangerTitle.text = "STATUS: " + d.CurrentDanger.ToString().ToUpper();

            if (!d.FireStarted)
            {
                dangerDetail.text = "Belum ada kebakaran terdeteksi";
            }
            else
            {
                float dist = d.DistanceToFront;
                string distText = dist <= 0f ? "Kamu di area api!" : "Api " + Mathf.RoundToInt(dist) + " m ke arah " + d.DirectionToFire;
                string debugTag = d.FireStartedByDebug ? "[DEBUG] " : "";
                dangerDetail.text = debugTag + distText + "   |   Asap sekitar " + Mathf.RoundToInt(d.SmokeAtPlayer * 100f) + "%   |   Api menyebar " + Mathf.RoundToInt(d.FireProgress01 * 100f) + "%";
            }
        }

        private void UpdateToasts()
        {
            toasts.RemoveAll(t => t.expireAt < Time.unscaledTime);

            sb.Length = 0;
            for (int i = 0; i < toasts.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(toasts[i].text);
            }

            string result = sb.ToString();
            if (result != lastToastString)
            {
                lastToastString = result;
                toastText.text = result;
            }
        }
    }
}
