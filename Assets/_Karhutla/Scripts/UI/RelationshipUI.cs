using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>Panel kecil daftar hubungan dengan warga. Tombol K menampilkan/menyembunyikan.</summary>
    public class RelationshipUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text listText;
        private float timer;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Hubungan", new Color(0.1f, 0.05f, 0.12f, 0.8f));
            UIFactory.Place(panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(760f, 520f));
            panelObject = panel.gameObject;

            listText = UIFactory.CreateText(panel, "Teks Hubungan", 23, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(listText.rectTransform, 18f, 10f, 12f, 12f);
            panelObject.SetActive(false);
        }

        private void Update()
        {
            if (panelObject == null) return;

            if (!GameState.InputLocked && GameInput.RelationshipTogglePressed) panelObject.SetActive(!panelObject.activeSelf);
            if (!panelObject.activeSelf) return;

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = 0.3f;

            var rel = RelationshipSystem.Instance;
            listText.text = "<b>HUBUNGAN  (K)</b>   <size=17>[ ] +/-10 ke warga terdekat (uji)   \\ reset</size>\n\n" + (rel != null ? rel.BuildListText() : "(sistem hubungan tidak aktif)");
        }
    }
}
