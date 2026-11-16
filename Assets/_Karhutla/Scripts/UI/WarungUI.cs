using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Panel info warung (kanan atas): status buka/tutup/libur, sisa waktu sesi, pendapatan, kepuasan,
    /// stok hidangan (gudang + kulkas) dan daftar pesanan pelanggan yang sedang berjalan.
    /// Hanya membaca data dari WarungSystem / WarungStorage / DayReportSystem.
    /// </summary>
    public class WarungUI : MonoBehaviour
    {
        private Text infoText;
        private float timer;
        private readonly StringBuilder sb = new StringBuilder();

        private void Start()
        {
            var panel = UIFactory.CreatePanel(UIFactory.Canvas.transform, "Panel Warung", new Color(0.25f, 0.12f, 0.05f, 0.7f));
            UIFactory.Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(520f, 400f));

            infoText = UIFactory.CreateText(panel, "Teks Warung", 21, TextAnchor.UpperLeft, Color.white);
            UIFactory.Stretch(infoText.rectTransform, 18f, 10f, 12f, 12f);
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

            sb.Append("\n\nHidangan siap (gudang+kulkas):  Nasi " + WarungStorage.StockFresh(ItemDatabase.Nasi)
                      + " | Air " + WarungStorage.StockFresh(ItemDatabase.Air)
                      + " | Kopi " + WarungStorage.StockFresh(ItemDatabase.Kopi)
                      + " | Masker " + WarungStorage.StockFresh(ItemDatabase.Masker));
            sb.Append("\nBahan: Beras " + WarungStorage.StockFresh(ItemDatabase.Beras)
                      + " | Sayur " + WarungStorage.StockFresh(ItemDatabase.Sayur)
                      + " | B.Kopi " + WarungStorage.StockFresh(ItemDatabase.BubukKopi));

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
        }
    }
}
