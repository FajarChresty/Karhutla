using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Gerakan pemain memakai CharacterController (WASD, Shift untuk lari).
    /// Arah gerak mengikuti arah kamera, jadi W = "maju menjauhi kamera".
    /// Kecepatan dipengaruhi kondisi survival (lelah / asap) lewat SurvivalSystem.SpeedMultiplier.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Gerakan")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7f;
        [Tooltip("Makin besar = makin responsif (cepat berakselerasi/berhenti).")]
        public float acceleration = 35f;
        public float rotationSpeed = 14f;
        public float gravity = -20f;

        [Header("Referensi (opsional, otomatis memakai Camera.main)")]
        public Transform cameraTransform;

        [Header("Pengaruh Lingkungan")]
        [Tooltip("Pengali kecepatan akibat bahaya lingkungan seperti tanah gambut ambles (GDD Bab 8.4). Di-reset tiap frame.")]
        public float EnvironmentalSpeedMultiplier { get; set; } = 1f;

        public bool IsSprinting { get; private set; }
        public bool IsMoving => horizontalVelocity.sqrMagnitude > 0.25f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Vector2 input = GameState.InputLocked ? Vector2.zero : GameInput.Move;

            Transform cam = cameraTransform;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;

            Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 moveDir = forward * input.y + right * input.x;

            var survival = SurvivalSystem.Instance;
            bool canSprint = survival == null || survival.CanSprint;
            IsSprinting = GameInput.SprintHeld && moveDir.sqrMagnitude > 0.01f && canSprint && !GameState.InputLocked;

            float speed = IsSprinting ? sprintSpeed : walkSpeed;
            if (survival != null) speed *= survival.SpeedMultiplier;
            speed *= Mathf.Clamp(EnvironmentalSpeedMultiplier, 0.1f, 2f);
            EnvironmentalSpeedMultiplier = 1f; // reset tiap frame, bahaya aktif akan mengesetnya lagi selagi pemain di dalam zona

            Vector3 targetVelocity = moveDir * speed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

            if (horizontalVelocity.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Pindahkan pemain (CharacterController harus dimatikan sebentar agar posisi tidak di-override).</summary>
        public void TeleportTo(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }
}
