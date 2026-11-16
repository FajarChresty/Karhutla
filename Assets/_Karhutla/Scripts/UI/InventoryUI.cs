using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Panel daftar inventory (kanan). Tombol I menampilkan/menyembunyikan panel.
    /// Menampilkan berat saat ini / kapasitas maksimal, jumlah per jenis item, dan status basi.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private GameObject panelObject;
        private Text listText;
        private Inventory subscribedTo;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Inventaris", new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -450f), new Vector2(520f, 520f));
            panelObject = panel.gameObject;

            listText = UIFactory.CreateText(panel, "Teks Inventaris", 22, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(listText.rectTransform, 18f, 10f, 12f, 12f);

            if (Inventory.Instance != null)
            {
                subscribedTo = Inventory.Instance;
                subscribedTo.Changed += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (subscribedTo != null) subscribedTo.Changed -= Refresh;
        }

        private void Update()
        {
            if (panelObject != null && GameInput.InventoryTogglePressed && !GameState.InputLocked)
            {
                panelObject.SetActive(!panelObject.activeSelf);
            }
        }

        private void Refresh()
        {
            if (listText == null || Inventory.Instance == null) return;
            var inv = Inventory.Instance;

            var sb = new StringBuilder();
            sb.Append("INVENTARIS  (I)\n");
            string weightColor = inv.CurrentWeight >= inv.MaxWeight - 0.001f ? "#FF6B5A" : (inv.CurrentWeight >= inv.MaxWeight * 0.8f ? "#FFD166" : "#9EE7A0");
            sb.Append("Berat: <color=" + weightColor + "><b>" + inv.CurrentWeight.ToString("0.#") + " / " + inv.MaxWeight.ToString("0.#") + " kg</b></color>\n\n");

            var owned = inv.GetOwnedItems();
            if (owned.Count == 0)
            {
                sb.Append("(kosong)\nAmbil item di sekitar dengan E.");
            }
            else
            {
                for (int i = 0; i < owned.Count && i < 9; i++)
                {
                    var def = owned[i];
                    int total = inv.Count(def.Id);
                    int bad = inv.Container.CountExpired(def.Id);
                    sb.Append("[" + (i + 1) + "] " + def.DisplayName + "  x" + total + "  (" + (def.Weight * total).ToString("0.##") + " kg)");
                    if (bad > 0) sb.Append("  <color=#FF6B5A>" + bad + " basi</color>");
                    sb.Append("\n     <size=17>" + def.EffectText + "</size>\n");
                }
                sb.Append("\nAngka = pakai item.   G = buang item basi.");
            }
            listText.text = sb.ToString();
        }
    }
}
