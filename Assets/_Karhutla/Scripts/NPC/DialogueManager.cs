using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Mengelola dialog sederhana: daftar kalimat yang ditampilkan satu per satu.
    /// Tekan E / Space untuk lanjut. Saat dialog terbuka, gerakan pemain dan waktu berhenti.
    /// DialogueUI yang menampilkannya.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        public bool IsActive { get; private set; }
        public string Speaker { get; private set; }
        public string CurrentLine => (lines != null && index < lines.Length) ? lines[index] : "";
        public bool IsLastLine => lines == null || index >= lines.Length - 1;

        private string[] lines;
        private int index;
        private Action onEnd;

        private void Awake() { Instance = this; }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsActive) GameState.SetInputLocked(false);
        }

        public void Begin(string speaker, string[] dialogueLines, Action onFinished = null)
        {
            if (IsActive || dialogueLines == null || dialogueLines.Length == 0) return;

            Speaker = speaker;
            lines = dialogueLines;
            index = 0;
            onEnd = onFinished;
            IsActive = true;
            GameState.SetInputLocked(true);
        }

        private void Update()
        {
            if (!IsActive) return;
            if (Time.frameCount == GameState.LockChangedFrame) return; // abaikan E yang baru membuka dialog

            if (GameInput.AdvanceDialoguePressed)
            {
                index++;
                if (index >= lines.Length) End();
            }
        }

        private void End()
        {
            IsActive = false;
            GameState.SetInputLocked(false);
            var callback = onEnd;
            onEnd = null;
            if (callback != null) callback();
        }
    }
}
