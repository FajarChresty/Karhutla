using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Jembatan antara logika game (PlayerController / NpcController) dan animasi karakter.
    ///
    /// - Membaca kondisi karakter: kecepatan, lari, sedang bicara.
    /// - Mengirimnya ke Animator (jika model 3D sudah dipasang) lewat parameter:
    ///     Speed (float), IsMoving (bool), IsSprinting (bool), IsTalking (bool), Interact (trigger).
    ///   Parameter yang tidak ada di Animator Controller dilewati, jadi controller boleh berisi sebagian saja.
    /// - Nilai yang sama dipakai ProceduralCharacterMotion (animasi cadangan untuk kapsul dummy).
    ///
    /// Tidak mengubah gerakan sama sekali: hanya membaca. Root Motion selalu dimatikan.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterAnimationDriver : MonoBehaviour
    {
        public static readonly int SpeedHash = Animator.StringToHash("Speed");
        public static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        public static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        public static readonly int IsTalkingHash = Animator.StringToHash("IsTalking");
        public static readonly int InteractHash = Animator.StringToHash("Interact");

        [Tooltip("Animator model 3D. Kosong = dicari otomatis di anak objek.")]
        public Animator animator;

        [Tooltip("Waktu penghalusan nilai Speed (detik). Mencegah animasi patah-patah saat berhenti/mulai jalan.")]
        public float speedDamp = 0.1f;

        [Tooltip("Kecepatan minimum agar dianggap bergerak (m/detik).")]
        public float movingThreshold = 0.2f;

        [Tooltip("Lompatan posisi lebih dari ini dalam satu frame dianggap teleport (tidak dihitung sebagai lari).")]
        public float teleportDistance = 3f;

        public float Speed { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsTalking { get; private set; }

        /// <summary>Kecepatan jalan & lari karakter ini (dipakai animasi prosedural untuk menormalkan ayunan).</summary>
        public float WalkSpeed { get; private set; } = 2f;
        public float SprintSpeed { get; private set; } = 5f;

        private PlayerController player;
        private NpcController npc;
        private Vector3 lastPosition;
        private readonly HashSet<int> animatorParams = new HashSet<int>();
        private Animator cachedFor;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            npc = GetComponent<NpcController>();
            if (player != null) { WalkSpeed = player.walkSpeed; SprintSpeed = player.sprintSpeed; }
            else if (npc != null) { WalkSpeed = npc.moveSpeed; SprintSpeed = npc.moveSpeed * 2f; }
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
        }

        /// <summary>Dipanggil installer setelah model 3D dipasang/diganti.</summary>
        public void SetAnimator(Animator newAnimator)
        {
            animator = newAnimator;
            if (animator != null) animator.applyRootMotion = false;
            cachedFor = null;
        }

        /// <summary>Memutar animasi aksi sekali (mis. mengambil barang, memadamkan api).</summary>
        public void PlayInteract()
        {
            if (HasParam(InteractHash)) animator.SetTrigger(InteractHash);
            var motion = GetComponent<ProceduralCharacterMotion>();
            if (motion != null) motion.Pulse();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;

            float rawSpeed = delta.magnitude > teleportDistance ? 0f : delta.magnitude / dt;
            Speed = Mathf.Lerp(Speed, rawSpeed, speedDamp <= 0f ? 1f : 1f - Mathf.Exp(-dt / speedDamp));
            IsMoving = Speed > movingThreshold;

            IsSprinting = player != null && player.IsSprinting && IsMoving;
            if (npc != null) IsTalking = npc.IsTalking;
            else if (player != null) IsTalking = DialogueManager.Instance != null && DialogueManager.Instance.IsActive;

            // Aksi pemain: tombol interaksi ditekan saat ada objek yang bisa diinteraksi.
            if (player != null && GameInput.InteractPressed && !GameState.InputLocked
                && Interactor.Instance != null && Interactor.Instance.Current != null)
            {
                PlayInteract();
            }

            PushToAnimator();
        }

        private void PushToAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null) return;
                animator.applyRootMotion = false;
            }
            if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

            if (HasParam(SpeedHash)) animator.SetFloat(SpeedHash, Speed);
            if (HasParam(IsMovingHash)) animator.SetBool(IsMovingHash, IsMoving);
            if (HasParam(IsSprintingHash)) animator.SetBool(IsSprintingHash, IsSprinting);
            if (HasParam(IsTalkingHash)) animator.SetBool(IsTalkingHash, IsTalking);
        }

        private bool HasParam(int hash)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            if (cachedFor != animator)
            {
                animatorParams.Clear();
                foreach (var p in animator.parameters) animatorParams.Add(p.nameHash);
                cachedFor = animator;
            }
            return animatorParams.Contains(hash);
        }
    }
}
