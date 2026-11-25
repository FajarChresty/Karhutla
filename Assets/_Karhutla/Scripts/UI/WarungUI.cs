using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Panel info warung (kanan atas): status buka/tutup/libur, sisa waktu sesi, pendapatan, kepuasan,
    /// stok hidangan (gudang + kulkas) dan daftar pesanan pelanggan yang sedang berjalan.
    /// Tinggi kotak panel beradaptasi secara dinamis (responsif) sesuai panjang konten teks.
    /// </summary>
    public class WarungUI : MonoBehaviour
    {
        public static WarungUI Instance { get; private set; }
        public float CurrentHeight { get; private set; } = 180f;

        private RectTransform panelRect;
        private Text infoText;
        private float timer;
        private readonly StringBuilder sb = new StringBuilder();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Warung", new Color(0.2f, 0.1f, 0.05f, 0.75f));
            UIFactory.Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-25f, -25f), new Vector2(380f, 180f));
            panelRect = panel;

            infoText = UIFactory.CreateText(panel, "Teks Warung", 15, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(infoText.rectTransform, 12f, 8f, 10f, 8f);
        }

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f || infoText == null) return;
            timer = 0.2f;

            var w = WarungSystem.Instance;
            if (w == null) return;

            sb.Length = 0;
            sb.Append("<b>WARUNG</b>   ");
            if (w.IsOpen) sb.Append("<color=#7CFC8A>BUKA</color>   sisa sesi " + Mathf.CeilToInt(w.SessionTimeLeft) + " dtk");
            else
            {
                var time = TimeManager.Instance;
                if (time != null && time.IsSundayToday && w.SundayDecision == SundayChoice.Libur) sb.Append("<color=#FFD166>LIBUR (Minggu)</color>");
                else sb.Append("<color=#FF8A7A>TUTUP</color>");
            }

            var time2 = TimeManager.Instance;
            if (time2 != null && time2.IsSundayToday) sb.Append("\n<color=#FFD166>Hari Minggu</color>" + (w.SundayDecision == SundayChoice.Belum ? " (belum memilih buka/libur)" : ""));

            sb.Append("\nPendapatan: " + TextFormat.Rupiah(w.TodayIncome) + "   Selesai: " + w.TodayServed + "   Gagal: " + w.TodayFailed);
            string sat = w.RatedCustomers > 0 ? Mathf.RoundToInt(w.OverallSatisfaction) + "/100" : "-";
            sb.Append("\nKepuasan keseluruhan: " + sat);

            int unlockedCount = RecipeManager.Instance != null ? RecipeManager.Instance.GetUnlockedRecipes().Count : 5;
            sb.Append("\nResep Masakan Terbuka: <color=#7CFC8A>" + unlockedCount + " / 12 Menu</color>");

            // Tampilkan stok hidangan yang siap dihidangkan
            sb.Append("\n\n<b>Hidangan Siap Saji:</b>\n  ");
            int dishCount = 0;
            string[] keyDishes = { ItemDatabase.Nasi, ItemDatabase.Kopi, ItemDatabase.TehHangat, ItemDatabase.PisangGoreng, ItemDatabase.SayurAsem,
                                  ItemDatabase.NasiGoreng, ItemDatabase.MieRebus, ItemDatabase.EsJeruk, ItemDatabase.IkanBakar, ItemDatabase.AyamGoreng,
                                  ItemDatabase.BuburKacang, ItemDatabase.WedangJahe, ItemDatabase.Air, ItemDatabase.Masker };
            foreach (var dId in keyDishes)
            {
                int stk = WarungStorage.StockFresh(dId);
                if (stk > 0)
                {
                    var def = ItemDatabase.Get(dId);
                    sb.Append((def != null ? def.DisplayName : dId) + " (" + stk + ")  ");
                    dishCount++;
                }
            }
            if (dishCount == 0) sb.Append("<color=#A0A0A0>Belum ada masakan matang (masak di dapur!)</color>");

            sb.Append("\n\n<b>Pesanan berjalan</b>");
            if (w.ActiveCount == 0)
            {
                sb.Append("\n  -");
            }
            else
            {
                foreach (var c in w.ActiveCustomers)
                {
                    if (c == null || c.Table == null) continue;
                    string state;
                    switch (c.State)
                    {
                        case CustomerState.Arriving: state = "datang"; break;
                        case CustomerState.WaitingToOrder: state = "<color=#FFE066>" + w.OrderStatusLabel(c) + "</color> (" + Mathf.CeilToInt(c.PatienceLeft) + " dtk)"; break;
                        case CustomerState.WaitingForFood: state = c.OrderName + " - <color=#FFE066>" + w.OrderStatusLabel(c) + "</color> (" + Mathf.CeilToInt(c.PatienceLeft) + " dtk)"; break;
                        case CustomerState.Eating: state = w.OrderStatusLabel(c); break;
                        default: state = ""; break;
                    }
                    sb.Append("\n  Meja " + c.Table.tableNumber + ": " + state);
                }
            }

            var d = DisasterSystem.Instance;
            if (w.IsOpen && d != null && d.CurrentDanger >= DangerLevel.Bahaya)
            {
                sb.Append("\n<color=#FFB080>Asap tebal: pelanggan jadi sepi.</color>");
            }

            infoText.text = sb.ToString();

            // Responsif: Sesuaikan tinggi kotak panel secara otomatis mengikuti jumlah baris pesanan & info
            if (panelRect != null)
            {
                float targetH = Mathf.Max(130f, Mathf.Ceil(infoText.preferredHeight) + 20f);
                if (Mathf.Abs(panelRect.sizeDelta.y - targetH) > 1f)
                {
                    panelRect.sizeDelta = new Vector2(380f, targetH);
                }
                CurrentHeight = targetH;
            }
        }
    }
}
