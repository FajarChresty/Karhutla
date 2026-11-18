using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Menampilkan prompt interaksi yang adaptif terhadap isi teks saat pemain mendekati objek / NPC.
    /// Ukuran kotak latar belakang (lebar dan tinggi) otomatis menyesuaikan panjang dan jumlah opsi,
    /// serta memiliki spasi antar baris yang lega sehingga tidak mepet.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        private RectTransform panelRect;
        private GameObject panelObject;
        private Text promptText;

        private const float PaddingX = 32f;
        private const float PaddingY = 18f;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Prompt Interaksi", new Color(0.04f, 0.05f, 0.08f, 0.82f));
            // Posisi tetap di atas area dialog/bawah layar, anchor di tengah bawah
            UIFactory.Place(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 195f), new Vector2(240f, 40f));
            panelRect = panel;
            panelObject = panel.gameObject;

            promptText = UIFactory.CreateText(panel, "Teks Prompt", 16, TextAnchor.MiddleCenter, Color.white);
            // Izinkan text overflow agar perhitungan preferredWidth/preferredHeight akurat
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;
            // Berikan spasi antar baris yang lega agar opsi tidak mepet
            promptText.lineSpacing = 1.42f;

            UIFactory.Stretch(promptText.rectTransform, PaddingX * 0.5f, PaddingY * 0.5f, PaddingX * 0.5f, PaddingY * 0.5f);
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

            // Jika hanya 1 baris (seperti ambil item), pusatkan teks. Jika multi-baris, ratakan kiri agar tombol sejajar rapi
            promptText.alignment = (lines > 1) ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

            // Kotak background beradaptasi terus menerus dengan teks
            float targetWidth = Mathf.Max(180f, promptText.preferredWidth + PaddingX);
            float targetHeight = promptText.preferredHeight + PaddingY;

            panelRect.sizeDelta = new Vector2(targetWidth, targetHeight);
        }
    }
}
