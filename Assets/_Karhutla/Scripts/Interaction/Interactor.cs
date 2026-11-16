using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Dipasang di Player. Setiap frame mencari InteractableBase terdekat,
    /// lalu memanggil Interact() saat pemain menekan E.
    /// InteractionPromptUI membaca properti Current untuk menampilkan prompt.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        public static Interactor Instance { get; private set; }

        public float detectRadius = 4.5f;
        public InteractableBase Current { get; private set; }

        private readonly Collider[] buffer = new Collider[48];

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (GameState.InputLocked)
            {
                Current = null;
                return;
            }

            Current = FindBest();

            if (Current != null && Time.frameCount != GameState.LockChangedFrame)
            {
                if (GameInput.InteractPressed)
                {
                    Current.Interact();
                }
                else if (GameInput.SecondaryInteractPressed && !string.IsNullOrEmpty(Current.GetSecondaryPrompt()))
                {
                    Current.InteractSecondary();
                }
                else if (GameInput.TertiaryInteractPressed && !string.IsNullOrEmpty(Current.GetTertiaryPrompt()))
                {
                    Current.InteractTertiary();
                }
                else if (GameInput.QuaternaryInteractPressed && !string.IsNullOrEmpty(Current.GetQuaternaryPrompt()))
                {
                    Current.InteractQuaternary();
                }
            }
        }

        private InteractableBase FindBest()
        {
            Vector3 origin = transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, detectRadius, buffer, ~0, QueryTriggerInteraction.Collide);

            InteractableBase best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = buffer[i];
                var candidate = col.GetComponentInParent<InteractableBase>();
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.CanInteract()) continue;

                Vector3 closest = col.ClosestPoint(origin);
                closest.y = origin.y; // abaikan beda tinggi
                float d = Vector3.Distance(origin, closest);

                if (d <= candidate.interactRange && d < bestDist)
                {
                    best = candidate;
                    bestDist = d;
                }
            }
            return best;
        }
    }
}
