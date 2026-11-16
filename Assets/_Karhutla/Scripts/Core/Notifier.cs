using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Pengirim pesan singkat ke layar (toast). Sistem mana pun cukup memanggil Notifier.Show("...").
    /// HudUI yang menampilkannya. Dengan begini sistem gameplay tidak perlu tahu soal UI.
    /// </summary>
    public static class Notifier
    {
        public static event Action<string> Message;

        public static void Show(string text)
        {
            Debug.Log("[Karhutla] " + text);
            Message?.Invoke(text);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Message = null;
        }
    }
}
