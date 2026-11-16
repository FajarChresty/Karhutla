using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Objek interaktif paling sederhana: menampilkan pesan saat ditekan E.
    /// Dipakai untuk rumah warga, papan info, dll.
    /// </summary>
    public class SimpleInteractable : InteractableBase
    {
        [TextArea(2, 4)]
        public string message = "Tidak ada apa-apa di sini.";

        public override void Interact()
        {
            Notifier.Show(message);
        }
    }
}
