using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Layar pesan/pilihan berantrean. Dipakai untuk:
    ///  - Laporan akhir hari (satu tombol: lanjut)
    ///  - Pilihan buka/libur warung pada hari Minggu (dua tombol)
    /// Beberapa permintaan sekaligus diantrekan; prioritas lebih kecil tampil lebih dulu
    /// (laporan = 0, pilihan = 1), agar laporan kemarin muncul sebelum pilihan hari baru.
    /// E = pilihan A (atau lanjut), Esc/Q = pilihan B (jika ada). Input terkunci selama tampil.
    /// </summary>
    public class ModalManager : MonoBehaviour, IMenuScreen
    {
        private class Request
        {
            public int priority;
            public string title;
            public string body;
            public string optionA;
            public string optionB;
            public Action<int> callback;
        }

        public static ModalManager Instance { get; private set; }

        public bool IsOpen { get { return current != null; } }
        public string Title { get { return current != null ? current.title : ""; } }
        public string Body { get { return current != null ? current.body : ""; } }
        public string Footer
        {
            get
            {
                if (current == null) return "";
                if (string.IsNullOrEmpty(current.optionB)) return "[E] " + current.optionA;
                return "[E] " + current.optionA + "        [Q] " + current.optionB;
            }
        }

        private readonly List<Request> queue = new List<Request>();
        private Request current;
        private int shownFrame = -1;

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (current != null) GameState.SetInputLocked(false);
            MenuScreenRegistry.Release(this);
            if (Instance == this) Instance = null;
        }

        /// <summary>Tampilkan layar. optionB kosong = hanya satu tombol. callback menerima 0 (A) atau 1 (B).</summary>
        public static void Show(string title, string body, string optionA, string optionB = null, Action<int> callback = null, int priority = 1)
        {
            if (Instance == null)
            {
                // Tanpa ModalManager di scene: pilihan A dipilih otomatis agar logika tidak macet.
                if (callback != null) callback(0);
                return;
            }
            Instance.Enqueue(new Request { priority = priority, title = title, body = body, optionA = optionA, optionB = optionB, callback = callback });
        }

        private void Enqueue(Request r)
        {
            queue.Add(r);
            queue.Sort((a, b) => a.priority.CompareTo(b.priority)); // List.Sort tidak stabil; prioritas sama tetap aman karena jarang bersamaan
            if (current == null) TryShowNext();
        }

        private void TryShowNext()
        {
            if (queue.Count == 0)
            {
                bool wasOpen = current != null;
                current = null;
                MenuScreenRegistry.Release(this);
                if (wasOpen) GameState.SetInputLocked(false);
                return;
            }

            if (!MenuScreenRegistry.IsFree(this)) return; // layar lain sedang terbuka; dicoba lagi di Update

            bool wasLocked = current != null;
            current = queue[0];
            queue.RemoveAt(0);
            shownFrame = Time.frameCount;
            MenuScreenRegistry.Active = this;
            if (!wasLocked) GameState.SetInputLocked(true);
        }

        private void Update()
        {
            if (current == null)
            {
                if (queue.Count > 0) TryShowNext();
                return;
            }
            if (Time.frameCount == shownFrame) return;

            int choice = -1;
            bool twoOptions = !string.IsNullOrEmpty(current.optionB);
            if (GameInput.ConfirmPressed) choice = 0;
            else if (GameInput.CancelPressed && twoOptions) choice = 1;
            else if (!twoOptions && GameInput.AdvanceDialoguePressed) choice = 0;

            if (choice < 0) return;

            var finished = current;
            current = null;
            // Tampilkan berikutnya dulu (jika ada) supaya input tidak sempat terbuka di antara dua layar.
            if (queue.Count > 0)
            {
                current = null;
                TryShowKeepLock();
            }
            else
            {
                MenuScreenRegistry.Release(this);
                GameState.SetInputLocked(false);
            }

            if (finished.callback != null) finished.callback(choice);
        }

        private void TryShowKeepLock()
        {
            current = queue[0];
            queue.RemoveAt(0);
            shownFrame = Time.frameCount;
            MenuScreenRegistry.Active = this;
        }
    }
}
