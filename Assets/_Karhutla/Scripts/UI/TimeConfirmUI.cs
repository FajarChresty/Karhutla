using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>Dialog konfirmasi aktivitas yang memakai jatah waktu. Membaca state dari TimeActivityManager.</summary>
    public class TimeConfirmUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text titleText;
        private Text bodyText;
        private Text hintText;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Konfirmasi Waktu", new Color(0.05f, 0.05f, 0.1f, 0.92f));
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 300f));
            panelObject = panel.gameObject;

            titleText = UIFactory.CreateText(panel, "Judul", 36, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.4f));
            titleText.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(titleText.rectTransform, 30f, 0f, 30f, 20f);

            bodyText = UIFactory.CreateText(panel, "Isi", 30, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Stretch(bodyText.rectTransform, 40f, 70f, 40f, 80f);

            hintText = UIFactory.CreateText(panel, "Petunjuk", 28, TextAnchor.LowerCenter, new Color(0.7f, 1f, 0.7f));
            UIFactory.Stretch(hintText.rectTransform, 30f, 20f, 30f, 0f);
            hintText.text = "[E] Ya, lanjutkan        [Esc / Q] Batal";

            panelObject.SetActive(false);
        }

        private void Update()
        {
            if (panelObject == null) return;

            var manager = TimeActivityManager.Instance;
            bool show = manager != null && manager.IsPending;
            if (panelObject.activeSelf != show) panelObject.SetActive(show);
            if (!show) return;

            titleText.text = manager.Title;
            bodyText.text = manager.Description;
        }
    }
}
