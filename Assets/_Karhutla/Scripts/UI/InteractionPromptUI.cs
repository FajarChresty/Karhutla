using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>Menampilkan "[E] ..." (dan "[Q] ..." bila ada aksi kedua) saat pemain dekat objek interaktif.</summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        private RectTransform panelRect;
        private GameObject panelObject;
        private Text promptText;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Prompt Interaksi", new Color(0f, 0f, 0f, 0.7f));
            UIFactory.Place(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 290f), new Vector2(1100f, 70f));
            panelRect = panel;
            panelObject = panel.gameObject;

            promptText = UIFactory.CreateText(panel, "Teks Prompt", 32, TextAnchor.MiddleCenter, Color.white);
            UIFactory.Stretch(promptText.rectTransform, 10f, 0f, 10f, 0f);
            panelObject.SetActive(false);
        }

        private void Update()
        {
            if (panelObject == null) return;

            InteractableBase target = Interactor.Instance != null ? Interactor.Instance.Current : null;
            bool show = target != null;

            if (panelObject.activeSelf != show) panelObject.SetActive(show);
            if (!show) return;

            string text = "[E]  " + target.GetPrompt();
            int lines = 1;

            string second = target.GetSecondaryPrompt();
            if (!string.IsNullOrEmpty(second))
            {
                text += "\n[Q]  " + second;
                lines++;
            }

            string third = target.GetTertiaryPrompt();
            if (!string.IsNullOrEmpty(third))
            {
                text += "\n[R]  " + third;
                lines++;
            }

            string fourth = target.GetQuaternaryPrompt();
            if (!string.IsNullOrEmpty(fourth))
            {
                text += "\n[H]  " + fourth;
                lines++;
            }

            promptText.text = text;
            panelRect.sizeDelta = new Vector2(1100f, 20f + 50f * lines);
        }
    }
}
