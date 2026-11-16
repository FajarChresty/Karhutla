using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Class dasar untuk semua objek yang bisa diinteraksikan dengan tombol E.
    /// Cara membuat objek interaktif baru: turunkan class ini, isi GetPrompt() dan Interact().
    /// Objek harus punya Collider agar terdeteksi oleh Interactor.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour
    {
        [Tooltip("Teks yang muncul di prompt, mis. 'Buka pintu'.")]
        public string promptText = "Berinteraksi";

        [Tooltip("Jarak maksimum (dari tepi collider) agar bisa diinteraksikan.")]
        public float interactRange = 2.5f;

        public virtual string GetPrompt() { return promptText; }
        public virtual bool CanInteract() { return true; }
        public abstract void Interact();

        /// <summary>Aksi kedua (tombol Q). Kembalikan null/kosong jika objek tidak punya aksi kedua.</summary>
        public virtual string GetSecondaryPrompt() { return null; }
        public virtual void InteractSecondary() { }

        /// <summary>Aksi ketiga (tombol R), mis. barter. Kembalikan null/kosong jika tidak ada.</summary>
        public virtual string GetTertiaryPrompt() { return null; }
        public virtual void InteractTertiary() { }

        /// <summary>Aksi keempat (tombol H), mis. memberi hadiah. Kembalikan null/kosong jika tidak ada.</summary>
        public virtual string GetQuaternaryPrompt() { return null; }
        public virtual void InteractQuaternary() { }
    }
}
