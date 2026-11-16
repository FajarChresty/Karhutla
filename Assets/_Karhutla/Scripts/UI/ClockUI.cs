using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Jam digital ala dunia nyata (JJ:MM) + nama waktu: Subuh, Pagi, Siang, Malam.
    ///
    /// HANYA MEMBACA TimeManager (Day, Period, SectionProgress01) dan jam langit milik DayNightCycle.
    /// Tidak mengubah sistem waktu, tidak menambah biaya waktu, tidak memicu apa pun.
    ///  - Sistem 3 babak (Pagi/Siang/Malam) dan aturan "waktu berkurang saat aktivitas" tetap seperti sebelumnya.
    ///  - Jam bergerak halus (animasi) menuju jam target tiap kali jatah waktu terpakai / periode berganti / tidur.
    ///  - Jam target memakai rentang jam yang sama dengan langit (DayNightCycle.pagiHours/siangHours/malamHours),
    ///    jadi angka di jam selalu cocok dengan terang-gelapnya langit.
    ///  - Subuh tampil saat jam berjalan melewati 04:00-06:00 (misalnya saat tidur menuju pagi berikutnya).
    ///
    /// Tidak perlu dipasang di scene: script ini membuat dirinya sendiri saat Play bila TimeManager ada.
    /// Jika tidak ingin jam ini, hapus file ini.
    /// </summary>
    public class ClockUI : MonoBehaviour
    {
        [Header("Batas nama waktu (jam 0-24, NILAI SEMENTARA)")]
        public float subuhStart = 4f;
        public float pagiStart = 6f;
        public float siangStart = 11f;
        public float malamStart = 18f;

        [Header("Animasi")]
        [Tooltip("Kecepatan jam bergerak ke jam target, dalam jam per detik nyata (sama dengan kecepatan langit).")]
        public float clockSpeedHoursPerSecond = 4f;
        [Tooltip("Jika selisih jam lebih dari ini (mis. time skip berhari-hari), jam langsung loncat tanpa animasi.")]
        public float snapIfFartherThanHours = 30f;

        private TimeManager time;
        private DayNightCycle sky;
        private Text clockText;
        private Text labelText;
        private Image panelImage;

        private float shownHour;      // jam absolut yang ditampilkan (Hari 1 jam 0 = 0)
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<ClockUI>() != null) return;
            if (FindFirstObjectByType<TimeManager>() == null) return;
            new GameObject("ClockUI").AddComponent<ClockUI>();
        }

        private void Start()
        {
            time = TimeManager.Instance;
            sky = FindFirstObjectByType<DayNightCycle>();
            Build();
        }

        private void Build()
        {
            Transform root = UIFactory.Canvas.transform;

            // Tengah atas, tepat di bawah banner bahaya (banner: 100 px tinggi, 30 px dari atas).
            var panel = UIFactory.CreatePanel(root, "Panel Jam", new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(280f, 96f));
            panelImage = panel.GetComponent<Image>();

            clockText = UIFactory.CreateText(panel, "Teks Jam", 46, TextAnchor.UpperCenter, Color.white);
            clockText.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(clockText.rectTransform, 8f, 0f, 8f, 4f);

            labelText = UIFactory.CreateText(panel, "Teks Waktu", 24, TextAnchor.LowerCenter, Color.white);
            UIFactory.Stretch(labelText.rectTransform, 8f, 6f, 8f, 0f);
        }

        private void Update()
        {
            if (time == null) { time = TimeManager.Instance; if (time == null) return; }
            if (clockText == null) return;

            float target = TargetAbsoluteHour();

            if (!initialized || Mathf.Abs(target - shownHour) > snapIfFartherThanHours)
            {
                shownHour = target;
                initialized = true;
            }
            else
            {
                shownHour = Mathf.MoveTowards(shownHour, target, Mathf.Max(0.01f, clockSpeedHoursPerSecond) * Time.unscaledDeltaTime);
            }

            Refresh();
        }

        /// <summary>Jam absolut target: (Hari-1)*24 + jam hari ini. Selalu naik, jadi animasi melewati tengah malam dengan benar.</summary>
        private float TargetAbsoluteHour()
        {
            Vector2 range = RangeFor(time.Period);
            float hourOfDay = Mathf.Lerp(range.x, range.y, time.SectionProgress01);
            return (time.Day - 1) * 24f + hourOfDay;
        }

        private Vector2 RangeFor(TimeOfDay period)
        {
            if (sky == null) sky = FindFirstObjectByType<DayNightCycle>();

            // Cadangan bila DayNightCycle tidak ada (nilai sama dengan default DayNightCycle).
            Vector2 pagi = new Vector2(7.5f, 10.5f);
            Vector2 siang = new Vector2(11.5f, 15.5f);
            Vector2 malam = new Vector2(20.5f, 23.5f);
            if (sky != null) { pagi = sky.pagiHours; siang = sky.siangHours; malam = sky.malamHours; }

            switch (period)
            {
                case TimeOfDay.Siang: return siang;
                case TimeOfDay.Malam: return malam;
                default: return pagi;
            }
        }

        private void Refresh()
        {
            float hourOfDay = Mathf.Repeat(shownHour, 24f);
            int totalMinutes = Mathf.FloorToInt(hourOfDay * 60f);
            int hh = (totalMinutes / 60) % 24;
            int mm = totalMinutes % 60;

            clockText.text = hh.ToString("00") + ":" + mm.ToString("00");

            string label;
            Color tint;
            if (hourOfDay >= subuhStart && hourOfDay < pagiStart) { label = "Subuh"; tint = new Color(0.95f, 0.65f, 0.55f); }
            else if (hourOfDay >= pagiStart && hourOfDay < siangStart) { label = "Pagi"; tint = new Color(1f, 0.9f, 0.5f); }
            else if (hourOfDay >= siangStart && hourOfDay < malamStart) { label = "Siang"; tint = new Color(1f, 1f, 0.8f); }
            else { label = "Malam"; tint = new Color(0.6f, 0.7f, 1f); }

            labelText.text = label;
            labelText.color = tint;
        }
    }
}
