using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Third-Person Follow Camera sederhana (tanpa Cinemachine).
    ///
    ///  - Dekat dengan karakter secara default (initialDistance), bisa ZOOM dengan scroll mouse
    ///    antara minDistance dan maxDistance.
    ///  - Rotasi 4 arah: Z = putar kiri, C = putar kanan (90 derajat per tekan, tetap seperti sebelumnya).
    ///    Urutan: Depan -> Kanan -> Belakang -> Kiri -> Depan.
    ///  - MOUSE LOOK (default aktif): kamera mengikuti gerakan mouse / touchpad langsung, tanpa menekan tombol. Geser kiri-kanan
    ///    memutar 360 derajat, geser atas-bawah mengubah sudut pandang. Kursor dikunci saat bermain dan dibebaskan saat menu/dialog
    ///    terbuka. M = nyalakan/matikan mouse look. Z/C tetap bisa dipakai sebagai putar cepat 90 derajat.
    ///  - Rotasi bebas (dipakai bila mouse look dimatikan): tahan tombol mouse kanan (atau tengah) lalu geser mouse. Geser vertikal mengubah sudut pandang
    ///    (dibatasi minPitch..maxPitch). Z/C setelah drag akan "menempel" ke arah 90 derajat terdekat lalu berputar satu langkah.
    ///  - Selalu mengikuti pemain dan menatap titik fokus setinggi focusHeight di atas pijakan karakter.
    ///  - Tidak menembus dinding/objek: bila ada penghalang antara fokus dan kamera, kamera maju mendekati karakter.
    ///
    /// Gerak pemain dihitung dari arah kamera (PlayerController memproyeksikan cam.forward ke tanah), jadi W/A/S/D selalu
    /// konsisten terhadap layar untuk semua arah kamera. Interaksi berbasis jarak ke pemain dan tidak bergantung kamera.
    /// Semua kamera ini hanya aktif bila input tidak terkunci (menu/dialog terbuka = tidak zoom/putar).
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public static readonly string[] ViewNames = { "Depan", "Kanan", "Belakang", "Kiri" };

        public static CameraFollow Instance { get; private set; }

        public Transform target;

        [Header("Jarak & zoom")]
        [Tooltip("Jarak kamera awal dari titik fokus (meter).")]
        public float initialDistance = 6.5f;
        public float minDistance = 2.5f;
        public float maxDistance = 14f;
        [Tooltip("Meter per 1 notch scroll mouse.")]
        public float zoomSpeed = 1.5f;
        [Tooltip("Kehalusan zoom (detik). Makin kecil = makin cepat.")]
        public float zoomSmoothTime = 0.12f;

        [Header("Sudut & fokus")]
        [Tooltip("Sudut pandang vertikal awal (derajat, 0 = sejajar tanah, 90 = dari atas).")]
        [Range(5f, 80f)] public float pitch = 35f;
        [Range(5f, 80f)] public float minPitch = 12f;
        [Range(5f, 85f)] public float maxPitch = 70f;
        [Tooltip("Tinggi titik fokus di atas pijakan karakter (meter).")]
        public float focusHeight = 1.4f;

        [Header("Mouse look (kamera mengikuti mouse / touchpad)")]
        [Tooltip("GDD Bab 9.4: Kamera top-down terkunci pada 4 arah diskrit. Fitur putar bebas dinonaktifkan secara default.")]
        public bool mouseLook = false;
        [Tooltip("Derajat per piksel gerak mouse/touchpad. Naikkan bila terasa lambat.")]
        public float lookSensitivity = 0.12f;
        public bool invertY = false;
        [Tooltip("Kunci dan sembunyikan kursor saat bermain (hanya bila mouse look aktif). Default false agar kursor bebas untuk interaksi & UI.")]
        public bool lockCursor = false;

        [Header("Rotasi")]
        [Tooltip("Lama putaran 90 derajat (detik, kira-kira). Makin kecil = makin cepat.")]
        public float rotateSmoothTime = 0.2f;
        public bool allowRotation = true;
        [Tooltip("GDD Bab 9.4: Rotasi bebas dengan mouse kanan/tengah dinonaktifkan default agar sudut kamera terkunci pada 4 arah.")]
        public bool allowMouseDrag = false;
        [Tooltip("Derajat per piksel gerak mouse.")]
        public float dragSensitivity = 0.25f;
        public bool dragChangesPitch = true;

        [Header("Mengikuti")]
        [Tooltip("Kehalusan mengikuti pemain (detik). Kecil = tidak tertinggal saat lari.")]
        public float followSmoothTime = 0.05f;

        [Header("Hindari menembus objek")]
        public bool avoidObstacles = true;
        public float collisionPadding = 0.35f;
        [Tooltip("Jarak terdekat kamera ke fokus saat terhalang.")]
        public float minCollisionDistance = 0.8f;

        /// <summary>0 = Depan, 1 = Kanan, 2 = Belakang, 3 = Kiri (arah 90 derajat terdekat).</summary>
        public int ViewIndex
        {
            get { return ((Mathf.RoundToInt(targetYaw / 90f) % 4) + 4) % 4; }
        }
        public string ViewName { get { return ViewNames[ViewIndex]; } }
        public float Distance { get { return distance; } }

        /// <summary>Posisi kamera awal relatif terhadap pemain (dipakai Scene Builder agar kamera tidak lompat saat Play).</summary>
        public Vector3 InitialOffset
        {
            get { return Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -initialDistance) + Vector3.up * focusHeight; }
        }

        private float yaw, targetYaw, yawVelocity;
        private float currentPitch, targetPitch, pitchVelocity;
        private float distance, targetDistance, distanceVelocity;
        private Vector3 focus, focusVelocity;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private bool wasWantingLock;
        private int skipLookFrames;
        private float fastRotateTimer;

        private Transform originalTarget;
        private bool isCutsceneOverridden;

        public void SetCutsceneFocus(Transform cutsceneTarget, float targetYawAngle, float targetPitchAngle, float targetDist, float smoothTime = 0.5f)
        {
            if (!isCutsceneOverridden) originalTarget = target;
            isCutsceneOverridden = true;
            target = cutsceneTarget;
            targetYaw = targetYawAngle;
            targetPitch = targetPitchAngle;
            targetDistance = targetDist;
            followSmoothTime = smoothTime;
            rotateSmoothTime = smoothTime;
        }

        public void ClearCutsceneFocus()
        {
            if (isCutsceneOverridden && originalTarget != null)
            {
                target = originalTarget;
            }
            isCutsceneOverridden = false;
            followSmoothTime = 0.05f;
            rotateSmoothTime = 0.2f;
            targetPitch = pitch;
            targetDistance = initialDistance;
        }

        private void Awake() { Instance = this; }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Kunci kursor hanya saat mouse look aktif dan input tidak terkunci (menu/dialog = kursor bebas).</summary>
        private void UpdateCursor()
        {
            bool want = mouseLook && lockCursor && !GameState.InputLocked;
            if (want)
            {
                if (Cursor.lockState != CursorLockMode.Locked && (!wasWantingLock || GameInput.LeftClickPressed))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    skipLookFrames = 2; // abaikan lonjakan gerak saat kursor baru dikunci
                }
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            wasWantingLock = want;
        }

        private void Start()
        {
            SanitizeSettings();
            currentPitch = targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = targetDistance = Mathf.Clamp(initialDistance, minDistance, maxDistance);
            yaw = targetYaw = 0f;
            if (target != null) focus = target.position + Vector3.up * focusHeight;
            Apply(true);
        }

        private void SanitizeSettings()
        {
            minDistance = Mathf.Max(0.5f, minDistance);
            maxDistance = Mathf.Max(minDistance + 0.5f, maxDistance);
            maxPitch = Mathf.Max(minPitch + 1f, maxPitch);
            zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime);
            rotateSmoothTime = Mathf.Max(0.01f, rotateSmoothTime);
            followSmoothTime = Mathf.Max(0f, followSmoothTime);
        }

        private void Update()
        {
            UpdateCursor();
            fastRotateTimer -= Time.unscaledDeltaTime;
            if (GameState.InputLocked) return;

            if (GameInput.CameraLockTogglePressed)
            {
                mouseLook = !mouseLook;
                Notifier.Show(mouseLook ? "Kamera mouse look diaktifkan (M untuk mengunci kembali ke 4 arah)." : "Kamera terkunci 4 arah diskrit (GDD 9.4: Z / C).");
            }

            // Zoom (scroll)
            float scroll = GameInput.ScrollDelta;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            if (!allowRotation) return;

            // Rotasi 4 arah (Z/C)
            if (GameInput.CameraRightPressed) Rotate(1);
            else if (GameInput.CameraLeftPressed) Rotate(-1);

            // Mouse look: kamera mengikuti gerak mouse/touchpad langsung (tanpa tombol)
            if (mouseLook)
            {
                if (skipLookFrames > 0) { skipLookFrames--; return; }
                if (!lockCursor || Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 look = GameInput.LookDelta;
                    if (look.sqrMagnitude > 0.0001f) fastRotateTimer = 0.3f; // saat digerakkan, kamera mengikuti dengan cepat (tanpa terasa tertinggal)
                    targetYaw += look.x * lookSensitivity;
                    float dy = look.y * lookSensitivity * (invertY ? -1f : 1f);
                    targetPitch = Mathf.Clamp(targetPitch - dy, minPitch, maxPitch);
                }
                return;
            }

            // Rotasi bebas dengan mouse (hanya bila mouse look dimatikan)
            if (allowMouseDrag && GameInput.CameraDragHeld)
            {
                Vector2 d = GameInput.MouseDelta;
                targetYaw += d.x * dragSensitivity;
                if (dragChangesPitch) targetPitch = Mathf.Clamp(targetPitch - d.y * dragSensitivity, minPitch, maxPitch);
            }
        }

        /// <summary>Putar satu langkah 90 derajat (menempel ke arah terdekat dulu). direction: +1 kanan, -1 kiri.</summary>
        public void Rotate(int direction)
        {
            direction = direction >= 0 ? 1 : -1;
            targetYaw = Mathf.Round(targetYaw / 90f) * 90f + 90f * direction;
        }

        /// <summary>Langsung ke arah tertentu (0 Depan, 1 Kanan, 2 Belakang, 3 Kiri) lewat rute terpendek.</summary>
        public void SetView(int index)
        {
            index = ((index % 4) + 4) % 4;
            float snapped = Mathf.Round(targetYaw / 90f) * 90f;
            targetYaw = snapped + Mathf.DeltaAngle(snapped, index * 90f);
        }

        /// <summary>Atur zoom langsung (dibatasi min/max).</summary>
        public void SetDistance(float meters)
        {
            targetDistance = Mathf.Clamp(meters, minDistance, maxDistance);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Apply(false);
        }

        private void Apply(bool immediate)
        {
            if (target == null) return;

            Vector3 desiredFocus = target.position + Vector3.up * focusHeight;
            if (immediate || followSmoothTime <= 0.0001f) { focus = desiredFocus; focusVelocity = Vector3.zero; }
            else focus = Vector3.SmoothDamp(focus, desiredFocus, ref focusVelocity, followSmoothTime);

            if (immediate)
            {
                yaw = targetYaw;
                currentPitch = targetPitch;
                distance = targetDistance;
            }
            else
            {
                float rotateTime = fastRotateTimer > 0f ? Mathf.Min(rotateSmoothTime, 0.03f) : rotateSmoothTime;
                yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, rotateTime);
                currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotateTime);
                distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, zoomSmoothTime);
            }

            Quaternion orbit = Quaternion.Euler(currentPitch, yaw, 0f);
            Vector3 back = orbit * Vector3.back;

            float usable = Mathf.Max(minCollisionDistance, FreeDistance(focus, back, distance));
            Vector3 position = focus + back * Mathf.Min(distance, usable);

            transform.position = position;
            Vector3 look = focus - position;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        /// <summary>Jarak bebas dari fokus ke arah kamera sebelum menabrak objek (dikurangi padding). Pemain, NPC, dan trigger diabaikan.</summary>
        private float FreeDistance(Vector3 from, Vector3 dir, float wanted)
        {
            if (!avoidObstacles) return wanted;

            int count = Physics.RaycastNonAlloc(from, dir, hits, wanted + collisionPadding, ~0, QueryTriggerInteraction.Ignore);
            float nearest = wanted + collisionPadding;
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                if (col.transform.IsChildOf(target) || col.GetComponentInParent<NpcController>() != null) continue;
                if (hits[i].distance < nearest) nearest = hits[i].distance;
            }
            return nearest - collisionPadding;
        }
    }
}
