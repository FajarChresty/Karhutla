using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>Kotak dialog di bawah layar. Membaca state dari DialogueManager.</summary>
    public class DialogueUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text speakerText;
        private Text lineText;
        private Text hintText;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Dialog", new Color(0f, 0f, 0f, 0.85f));
            UIFactory.Place(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1300f, 220f));
            panelObject = panel.gameObject;

            speakerText = UIFactory.CreateText(panel, "Nama Pembicara", 34, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.4f));
            speakerText.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(speakerText.rectTransform, 30f, 0f, 30f, 14f);

            lineText = UIFactory.CreateText(panel, "Isi Dialog", 32, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(lineText.rectTransform, 30f, 40f, 30f, 66f);

            hintText = UIFactory.CreateText(panel, "Petunjuk", 22, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.7f));
            UIFactory.Stretch(hintText.rectTransform, 30f, 10f, 30f, 0f);

            panelObject.SetActive(false);
        }

        private void Update()
        {
            if (panelObject == null) return;

            var dm = DialogueManager.Instance;
            bool show = dm != null && dm.IsActive;
            if (panelObject.activeSelf != show) panelObject.SetActive(show);
            if (!show) return;

            speakerText.text = dm.Speaker;
            lineText.text = dm.CurrentLine;
            hintText.text = dm.IsLastLine ? "[E] Selesai" : "[E] Lanjut";
        }
    }
}
