using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Satu panel di tengah layar yang menampilkan layar menu aktif (gudang/kulkas, pasar, barter, laporan, pilihan).
    /// Hanya MEMBACA MenuScreenRegistry.Active; logika ada di masing-masing manager.
    /// </summary>
    public class MenuScreenUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text titleText;
        private Text bodyText;
        private Text footerText;
        private float refreshTimer;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Layar Menu", new Color(0.04f, 0.05f, 0.09f, 0.94f));
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1240f, 860f));
            panelObject = panel.gameObject;

            titleText = UIFactory.CreateText(panel, "Judul", 38, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.4f));
            titleText.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(titleText.rectTransform, 30f, 0f, 30f, 22f);

            bodyText = UIFactory.CreateText(panel, "Isi", 27, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(bodyText.rectTransform, 50f, 90f, 50f, 90f);

            footerText = UIFactory.CreateText(panel, "Petunjuk", 24, TextAnchor.LowerCenter, new Color(0.7f, 1f, 0.7f));
            UIFactory.Stretch(footerText.rectTransform, 30f, 22f, 30f, 0f);

            panelObject.SetActive(false);
        }

        private void Update()
        {
            if (panelObject == null) return;

            var screen = MenuScreenRegistry.Active;
            bool show = screen != null && screen.IsOpen;
            if (panelObject.activeSelf != show) panelObject.SetActive(show);
            if (!show) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.05f;

            titleText.text = screen.Title;
            bodyText.text = screen.Body;
            footerText.text = screen.Footer;
        }
    }
}
