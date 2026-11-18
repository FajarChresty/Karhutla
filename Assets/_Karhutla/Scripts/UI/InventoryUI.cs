using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Panel daftar inventory (kanan). Tombol I menampilkan/menyembunyikan panel.
    /// Menampilkan berat saat ini / kapasitas maksimal, jumlah per jenis item, dan status basi.
    /// Tinggi kotak panel beradaptasi secara dinamis (responsif) sesuai jumlah item yang dimiliki.
    /// Posisi Y otomatis menempel rapi di bawah Panel Warung.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private GameObject panelObject;
        private RectTransform panelRect;
        private Text listText;
        private Inventory subscribedTo;

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Inventaris", new Color(0f, 0f, 0f, 0.65f));
            UIFactory.Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-25f, -220f), new Vector2(380f, 140f));
            panelRect = panel;
            panelObject = panel.gameObject;

            listText = UIFactory.CreateText(panel, "Teks Inventaris", 15, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(listText.rectTransform, 12f, 8f, 10f, 8f);

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

            // Adaptif: Menempel tepat di bawah Panel Warung dan ukuran tinggi kotak responsif sesuai banyak item
            if (panelObject != null && panelObject.activeSelf && panelRect != null)
            {
                float warungH = WarungUI.Instance != null && WarungUI.Instance.gameObject.activeInHierarchy ? WarungUI.Instance.CurrentHeight : 0f;
                float topY = warungH > 0f ? (-25f - warungH - 10f) : -25f;
                if (Mathf.Abs(panelRect.anchoredPosition.y - topY) > 0.5f)
                {
                    panelRect.anchoredPosition = new Vector2(-25f, topY);
                }

                float targetH = Mathf.Max(90f, Mathf.Ceil(listText.preferredHeight) + 20f);
                if (Mathf.Abs(panelRect.sizeDelta.y - targetH) > 1f)
                {
                    panelRect.sizeDelta = new Vector2(380f, targetH);
                }
            }
        }

        private void Refresh()
        {
            if (listText == null || Inventory.Instance == null) return;
            var inv = Inventory.Instance;

            var sb = new StringBuilder();
            sb.Append("<b>INVENTARIS</b>  (I)\n");
            string weightColor = inv.CurrentWeight >= inv.MaxWeight - 0.001f ? "#FF6B5A" : (inv.CurrentWeight >= inv.MaxWeight * 0.8f ? "#FFD166" : "#9EE7A0");
            sb.Append("Beban: <color=" + weightColor + "><b>" + inv.CurrentWeight.ToString("0.#") + " / " + inv.MaxWeight.ToString("0.#") + " kg</b></color>\n\n");

            var owned = inv.GetOwnedItems();
            if (owned.Count == 0)
            {
                sb.Append("<color=#AAAAAA>(kosong - ambil item dengan E)</color>");
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
                    if (!string.IsNullOrEmpty(def.EffectText))
                    {
                        sb.Append("\n     <size=13><color=#BBDDFF>" + def.EffectText + "</color></size>");
                    }
                    sb.Append("\n");
                }
                sb.Append("\n<size=13><color=#CCCCCC>Angka = pakai item   G = buang basi</color></size>");
            }
            listText.text = sb.ToString();

            if (panelRect != null)
            {
                float targetH = Mathf.Max(90f, Mathf.Ceil(listText.preferredHeight) + 20f);
                panelRect.sizeDelta = new Vector2(380f, targetH);
            }
        }
    }
}
