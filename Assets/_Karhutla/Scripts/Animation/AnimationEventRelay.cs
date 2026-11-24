using System;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Penerima Animation Event. Pasang di objek yang sama dengan Animator (root model).
    ///
    /// Di jendela Animation, tambahkan event pada klip dengan nama fungsi:
    ///   Footstep()          -> saat kaki menapak (memutar suara langkah bila ada).
    ///   AnimEvent(string)   -> event bebas, mis. "PickupItem", "SplashWater". Programmer cukup
    ///                          berlangganan ke <see cref="OnAnimEvent"/> tanpa mengubah klip.
    /// </summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        [Tooltip("Suara langkah (dipilih acak). Kosong = tanpa suara.")]
        public AudioClip[] footstepClips;
        [Range(0f, 1f)] public float footstepVolume = 0.5f;
        [Tooltip("Variasi pitch acak agar langkah tidak monoton.")]
        public float pitchJitter = 0.08f;

        public event Action OnFootstep;
        public event Action<string> OnAnimEvent;

        private AudioSource source;

        public void Footstep()
        {
            OnFootstep?.Invoke();
            if (footstepClips == null || footstepClips.Length == 0) return;

            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.maxDistance = 20f;
            }
            source.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            source.PlayOneShot(footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)], footstepVolume);
        }

        public void AnimEvent(string eventName)
        {
            OnAnimEvent?.Invoke(eventName);
        }
    }
}
