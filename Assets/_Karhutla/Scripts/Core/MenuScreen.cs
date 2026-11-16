using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Layar menu teks (penyimpanan, belanja, barter, laporan, pilihan). Satu layar aktif pada satu waktu.
    /// Manager yang membuka layar mengisi MenuScreenRegistry.Active; MenuScreenUI hanya membaca dan menampilkannya.
    /// Dengan pola ini UI tidak mengubah gameplay, dan menambah layar baru cukup membuat satu manager.
    /// </summary>
    public interface IMenuScreen
    {
        bool IsOpen { get; }
        string Title { get; }
        string Body { get; }
        string Footer { get; }
    }

    public static class MenuScreenRegistry
    {
        public static IMenuScreen Active;

        public static bool IsFree(IMenuScreen requester)
        {
            return Active == null || !Active.IsOpen || ReferenceEquals(Active, requester);
        }

        public static void Release(IMenuScreen owner)
        {
            if (ReferenceEquals(Active, owner)) Active = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
        }
    }
}
