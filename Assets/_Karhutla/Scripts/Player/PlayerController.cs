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
        /// <summary>Kecepatan gerak horizontal saat ini (dibaca PlayerAnimator).</summary>
        public Vector3 HorizontalVelocity => horizontalVelocity;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            EnsureCharacterVisual();
        }

        /// <summary>
        /// Memastikan karakter 2D (character.prefab) dan animasinya terpasang otomatis
        /// pada Player dengan hierarki yang tepat (CharacterVisual -> character) agar clip animasi berjalan.
        /// </summary>
        public void EnsureCharacterVisual()
        {
            var existingVisual = transform.Find("CharacterVisual");
            if (existingVisual != null)
            {
                var charChild = existingVisual.Find("character");
                // Cek apakah struktur sudah valid (memiliki child "character" terpusat di (0,0,0) dan Animator)
                if (charChild != null && charChild.localPosition.sqrMagnitude < 0.01f && existingVisual.GetComponent<Animator>() != null)
                {
                    return;
                }
                // Jika struktur tidak valid (misal dari build lama yang tergeser), bersihkan agar dibuat ulang dengan benar
                DestroyImmediate(existingVisual.gameObject);
            }

            GameObject prefab = Resources.Load<GameObject>("character");
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Karhutla/Prefabs/character.prefab");
            }
#endif
            if (prefab != null)
            {
                var visualGo = new GameObject("CharacterVisual");
                visualGo.transform.SetParent(transform, false);
                visualGo.transform.localPosition = new Vector3(0f, -0.95f, 0f);
                visualGo.transform.localRotation = Quaternion.identity;

                var charInstance = Instantiate(prefab, visualGo.transform);
                charInstance.name = "character"; // Wajib "character" karena track animasi memiliki jalur "character/Front", dsb.
                charInstance.transform.localPosition = Vector3.zero;
                charInstance.transform.localRotation = Quaternion.identity;

                // Sembunyikan mesh kapsul dummy dan penunjuk arah
                var mr = GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                var nose = transform.Find("Penunjuk Arah");
                if (nose != null) nose.gameObject.SetActive(false);

                // Tambahkan bayangan kontak (blob shadow) sederhana di bawah kaki agar karakter tidak tampak melayang
                var shadow = transform.Find("BlobShadow");
                if (shadow == null)
                {
                    var sGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    sGo.name = "BlobShadow";
                    sGo.transform.SetParent(transform, false);
                    sGo.transform.localPosition = new Vector3(0f, -0.98f, 0f);
                    sGo.transform.localScale = new Vector3(0.7f, 0.01f, 0.7f);
                    var col = sGo.GetComponent<Collider>();
                    if (col != null) DestroyImmediate(col);
                    var ren = sGo.GetComponent<Renderer>();
                    if (ren != null)
                    {
                        ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        ren.receiveShadows = false;
                        ren.material.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                    }
                }

                // Pasang controller animasi pada CharacterVisual
                var anim = visualGo.GetComponent<Animator>();
                if (anim == null) anim = visualGo.AddComponent<Animator>();
                if (anim.runtimeAnimatorController == null)
                {
                    var animCtrl = Resources.Load<RuntimeAnimatorController>("Player");
#if UNITY_EDITOR
                    if (animCtrl == null)
                    {
                        animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player/Player.controller");
                    }
#endif
                    if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
                }

                // Pasang komponen PlayerAnimator untuk menangani 4 arah hadap
                var pAnim = GetComponent<PlayerAnimator>();
                if (pAnim == null) pAnim = gameObject.AddComponent<PlayerAnimator>();
                pAnim.visual = visualGo.transform;
                pAnim.controller = this;
            }
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
