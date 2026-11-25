using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Mengatur animasi dan penyajian 2.5D karakter pemain (rig sprite 4 arah di Prefabs/character.prefab).
    /// Mengintegrasikan kemiringan sudut kamera (camera pitch tilt), dinamika langkah 2.5D (step bounce),
    /// kemiringan halus saat berbelok (banking lean), serta bayangan kontak (blob shadow) di bawah kaki.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        // Urutan harus sama dengan nilai parameter "Facing" di controller.
        private enum Facing { Depan = 0, Belakang = 1, Kiri = 2, Kanan = 3 }

        [Tooltip("Objek tampilan karakter (anak Player). Kosong = anak pertama.")]
        public Transform visual;
        [Tooltip("Kosong = dicari di objek yang sama.")]
        public PlayerController controller;
        [Tooltip("Batas pengali kecepatan animasi jalan (1 = kecepatan jalan normal).")]
        public Vector2 walkAnimSpeedRange = new Vector2(0.8f, 1.6f);
        [Tooltip("Makin besar = arah makin tahan berubah saat bergerak diagonal.")]
        public float diagonalBias = 1.2f;

        [Header("Pengaturan Estetika 2.5D")]
        [Tooltip("Kompensasi kemiringan pandangan kamera atas agar sprite tidak terlihat gepeng (0.6 - 0.85).")]
        public float cameraTiltFactor = 0.72f;
        [Tooltip("Amplitudo pantulan langkah vertikal (bouncy step).")]
        public float stepBobAmount = 0.038f;
        [Tooltip("Kecepatan frekuensi pantulan langkah.")]
        public float stepBobSpeed = 10.5f;

        private static readonly int FacingHash = Animator.StringToHash("Facing");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

        private Animator animator;
        private Facing facing = Facing.Depan;
        private Vector3 baseVisualLocalPos = new Vector3(0f, -1.02f, 0f);
        private float stepCycle = 0f;
        private float idleCycle = 0f;
        private float currentLean = 0f;

        private Transform shadowTransform;
        private Vector3 baseShadowScale = new Vector3(0.95f, 0.65f, 1f);
        private static Texture2D softShadowTex;

        public static Texture2D GetOrCreateShadowTexture()
        {
            if (softShadowTex != null) return softShadowTex;
            int size = 64;
            softShadowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            softShadowTex.wrapMode = TextureWrapMode.Clamp;
            softShadowTex.filterMode = FilterMode.Bilinear;
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    alpha = Mathf.Pow(alpha, 1.4f) * 0.65f; // Falloff radial alami
                    softShadowTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }
            softShadowTex.Apply();
            return softShadowTex;
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            ResolveReferences();
            SetupBlobShadow();
        }

        private void ResolveReferences()
        {
            if (visual == null)
            {
                var child = transform.Find("CharacterVisual");
                if (child != null) visual = child;
                else if (transform.childCount > 0) visual = transform.GetChild(0);
            }
            if (visual != null)
            {
                // Setel posisi dasar visual tepat di level pijakan tanah agar sepatu benar-benar menapak
                visual.localPosition = new Vector3(0f, -1.02f, 0f);
                baseVisualLocalPos = visual.localPosition;
            }
            if (animator == null)
            {
                if (visual != null) animator = visual.GetComponent<Animator>();
                if (animator == null) animator = GetComponent<Animator>();
            }
        }

        private void SetupBlobShadow()
        {
            var existing = transform.Find("BlobShadow");
            if (existing != null)
            {
                var mf = existing.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.name.ToLower().Contains("quad"))
                {
                    if (Application.isPlaying) Destroy(existing.gameObject);
                    else DestroyImmediate(existing.gameObject);
                    existing = null;
                }
            }

            GameObject shadowGo;
            if (existing != null)
            {
                shadowGo = existing.gameObject;
            }
            else
            {
                shadowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shadowGo.name = "BlobShadow";
                shadowGo.transform.SetParent(transform, false);
                var col = shadowGo.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
            }

            shadowGo.transform.localPosition = new Vector3(0f, -0.992f, 0f);
            shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowGo.transform.localScale = baseShadowScale;

            ConfigureShadowRenderer(shadowGo);
            shadowTransform = shadowGo.transform;
        }

        private void ConfigureShadowRenderer(GameObject shadowGo)
        {
            var ren = shadowGo.GetComponent<Renderer>();
            if (ren != null)
            {
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ren.receiveShadows = false;

                Shader shader = null;
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Transparent");
                }

                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.mainTexture = GetOrCreateShadowTexture();
                    mat.color = Color.white;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                    if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    ren.sharedMaterial = mat;
                }
            }
        }

        private void Update()
        {
            if (controller == null) return;
            if (animator == null)
            {
                ResolveReferences();
                if (animator == null) return;
            }

            Vector3 velocity = controller.HorizontalVelocity;
            bool moving = controller.IsMoving;
            if (moving) facing = ChooseFacing(velocity);

            animator.SetInteger(FacingHash, (int)facing);
            animator.SetBool(IsMovingHash, moving);

            float speed = 1f;
            if (moving && controller.walkSpeed > 0.01f)
                speed = Mathf.Clamp(velocity.magnitude / controller.walkSpeed, walkAnimSpeedRange.x, walkAnimSpeedRange.y);
            animator.SetFloat(MoveSpeedHash, speed);

            // Dinamika 2.5D: Step Bobbing & Pantulan Berjalan
            if (visual != null)
            {
                float verticalOffset = 0f;
                if (moving)
                {
                    float cycleSpeed = (controller.IsSprinting ? 1.4f : 1f) * stepBobSpeed;
                    stepCycle += Time.deltaTime * cycleSpeed;
                    verticalOffset = Mathf.Sin(stepCycle) * (controller.IsSprinting ? stepBobAmount * 1.3f : stepBobAmount);

                    // Kemiringan halus (lean/banking) saat berbelok ke samping
                    float targetLean = -velocity.x * 0.8f;
                    currentLean = Mathf.Lerp(currentLean, targetLean, 10f * Time.deltaTime);
                }
                else
                {
                    // Efek pernapasan lembut (breathing) saat diam
                    idleCycle += Time.deltaTime * 2.8f;
                    verticalOffset = Mathf.Sin(idleCycle) * 0.008f;
                    currentLean = Mathf.Lerp(currentLean, 0f, 8f * Time.deltaTime);
                }

                visual.localPosition = new Vector3(baseVisualLocalPos.x, baseVisualLocalPos.y + verticalOffset, baseVisualLocalPos.z);

                // Squash & stretch bayangan di bawah kaki mengikuti hentakan langkah
                if (shadowTransform != null)
                {
                    float squash = 1f - Mathf.Clamp(verticalOffset * 2.5f, -0.15f, 0.15f);
                    shadowTransform.localScale = new Vector3(baseShadowScale.x * squash, baseShadowScale.y * squash, 1f);
                }
            }
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (visual != null && cam != null)
            {
                // Sudut 2.5D: Memadukan yaw kamera dengan sebagian pitch kamera atas (tilt back).
                // Karakter tetap berdiri kokoh di atas tanah, namun wajah dan proporsi tubuh
                // tampil tegak utuh tanpa terdistorsi/tertekan oleh kamera isometrik atas.
                float camPitch = cam.transform.eulerAngles.x;
                float camYaw = cam.transform.eulerAngles.y;
                float tiltAngle = Mathf.Clamp(camPitch * cameraTiltFactor, 10f, 32f);

                visual.rotation = Quaternion.Euler(tiltAngle, camYaw, currentLean);

                // Pastikan bayangan lantai selalu berada tepat sejajar di bawah kaki visual karakter
                if (shadowTransform != null)
                {
                    Transform charChild = visual.Find("character");
                    Vector3 feetPos = charChild != null ? charChild.position : visual.position;

                    float groundY = transform.position.y - 0.992f;
                    RaycastHit[] hits = Physics.RaycastAll(new Vector3(feetPos.x, transform.position.y + 0.5f, feetPos.z), Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var h = hits[i];
                        if (h.collider != null && h.collider.transform != transform && !h.collider.transform.IsChildOf(transform))
                        {
                            groundY = h.point.y + 0.008f;
                            break;
                        }
                    }

                    shadowTransform.position = new Vector3(feetPos.x, groundY, feetPos.z);
                    shadowTransform.rotation = Quaternion.Euler(90f, camYaw, 0f);
                }
            }
        }

        private Facing ChooseFacing(Vector3 worldVelocity)
        {
            var cam = Camera.main;
            float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * worldVelocity;

            float side = Mathf.Abs(local.x);
            float depth = Mathf.Abs(local.z);
            bool horizontal = facing == Facing.Kiri || facing == Facing.Kanan;

            // Tahan arah yang sedang tampil sampai sumbu lain jelas lebih dominan, supaya tidak berkedip saat diagonal.
            if (horizontal) horizontal = side * diagonalBias >= depth;
            else horizontal = side > depth * diagonalBias;

            if (horizontal) return local.x < 0f ? Facing.Kiri : Facing.Kanan;
            // z positif = menjauhi kamera, jadi yang tampak adalah punggung.
            return local.z > 0f ? Facing.Belakang : Facing.Depan;
        }
    }
}
