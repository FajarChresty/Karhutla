using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Status global yang sangat sederhana.
    /// InputLocked = true saat dialog terbuka (player tidak bisa bergerak, waktu berhenti).
    /// </summary>
    public static class GameState
    {
        public static bool InputLocked { get; private set; }

        /// <summary>Frame saat InputLocked terakhir berubah. Dipakai agar tombol E tidak "bocor" ke sistem lain di frame yang sama.</summary>
        public static int LockChangedFrame { get; private set; } = -1;

        public static void SetInputLocked(bool locked)
        {
            if (InputLocked == locked) return;
            InputLocked = locked;
            LockChangedFrame = Time.frameCount;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            InputLocked = false;
            LockChangedFrame = -1;
        }
    }
}
