using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Panel daftar hubungan dengan warga dan dinamika sosial komunitas desa (GDD Bab 6 & 8).
    /// Tombol K untuk menampilkan/menyembunyikan panel.
    /// Tombol Tab atau T untuk berpindah antar tab:
    ///   Tab 1: Warga Desa (Karakter 1 - 8)
    ///   Tab 2: Warga Desa (Karakter 9 - 15)
    ///   Tab 3: Dinamika Antar-Warga (NPC-to-NPC Events & Relasi Sosial)
    /// </summary>
    public class RelationshipUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text listText;
        private float timer;
        private int activeTab = 0; // 0 = Warga 1-8, 1 = Warga 9-15, 2 = Dinamika Antar-Warga

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Hubungan", new Color(0.08f, 0.05f, 0.12f, 0.92f));
            UIFactory.Place(panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(860f, 640f));
            panelObject = panel.gameObject;

            listText = UIFactory.CreateText(panel, "Teks Hubungan", 21, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(listText.rectTransform, 20f, 16f, 16f, 16f);
            panelObject.SetActive(false);
        }

        private void Update()
        {
            if (panelObject == null) return;

            if (!GameState.InputLocked && GameInput.RelationshipTogglePressed)
            {
                panelObject.SetActive(!panelObject.activeSelf);
                if (panelObject.activeSelf) RenderContent();
            }

            if (!panelObject.activeSelf) return;

            // Tombol ganti tab: Tab atau T
            if (GameInput.RelationshipTabPressed)
            {
                activeTab = (activeTab + 1) % 3;
                RenderContent();
            }

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = 0.35f;

            RenderContent();
        }

        private void RenderContent()
        {
            var rel = RelationshipSystem.Instance;
            var comm = CommunityEventManager.Instance;

            string tab1Style = activeTab == 0 ? "<b><color=#FFD166>[1. Warga (1-8)]</color></b>" : "<color=#AAAAAA>1. Warga (1-8)</color>";
            string tab2Style = activeTab == 1 ? "<b><color=#FFD166>[2. Warga (9-15)]</color></b>" : "<color=#AAAAAA>2. Warga (9-15)</color>";
            string tab3Style = activeTab == 2 ? "<b><color=#FFD166>[3. Dinamika Antar-Warga]</color></b>" : "<color=#AAAAAA>3. Dinamika Antar-Warga</color>";

            string header = "<b>HUBUNGAN & DINAMIKA WARGA  (K)</b>    <size=16>[Tab/T] Ganti Tab   [ ] +/-10 Uji   \\ Reset</size>\n"
                          + tab1Style + "    " + tab2Style + "    " + tab3Style + "\n\n";

            string body = "";
            if (activeTab == 0)
            {
                body = rel != null ? rel.BuildListText(page: 0, pageSize: 8) : "(sistem hubungan belum aktif)";
            }
            else if (activeTab == 1)
            {
                body = rel != null ? rel.BuildListText(page: 1, pageSize: 8) : "(sistem hubungan belum aktif)";
            }
            else
            {
                body = comm != null ? comm.BuildCommunityStatusText() : "(sistem peristiwa warga belum aktif)";
            }

            listText.text = header + body;
        }
    }
}
