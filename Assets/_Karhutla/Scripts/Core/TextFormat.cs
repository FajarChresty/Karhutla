using System.Globalization;

namespace Karhutla
{
    public static class TextFormat
    {
        /// <summary>50000 -> "Rp 50.000"</summary>
        public static string Rupiah(int amount)
        {
            return "Rp " + amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
        }
    }
}
