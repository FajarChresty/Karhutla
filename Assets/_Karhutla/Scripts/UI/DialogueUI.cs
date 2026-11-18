using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Kotak dialog di bawah layar (kompak & rapi agar tidak menutupi layar).
    /// Membaca state dari DialogueManager.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text speakerText;
        private Text lineText;
        private Text hintText;

        private void Start()
        {
            // Panel dirampingkan dari 1300x220 menjadi 780x135, dengan warna semi-transparan elegan
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Dialog", new Color(0.04f, 0.05f, 0.08f, 0.8f));
            UIFactory.Place(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(780f, 135f));
            panelObject = panel.gameObject;

            // Nama pembicara dirapikan: font size 20 (sebelumnya 34)
            speakerText = UIFactory.CreateText(panel, "Nama Pembicara", 20, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.35f));
            speakerText.fontStyle = FontStyle.Bold;
            UIFactory.Place(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(620f, 26f));

            // Isi dialog: font size 17 (sebelumnya 32)
            lineText = UIFactory.CreateText(panel, "Isi Dialog", 17, TextAnchor.UpperLeft, Color.white);
            lineText.lineSpacing = 1.35f;
            UIFactory.Stretch(lineText.rectTransform, 20f, 44f, 20f, 32f);

            // Petunjuk tombol: font size 14 (sebelumnya 22)
            hintText = UIFactory.CreateText(panel, "Petunjuk", 14, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.65f));
            UIFactory.Place(hintText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 10f), new Vector2(180f, 22f));

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
