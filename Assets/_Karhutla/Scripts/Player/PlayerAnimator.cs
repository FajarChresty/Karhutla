using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Mengatur animasi karakter pemain (rig sprite 4 arah di Prefabs/character.prefab).
    /// Karakter selalu dihadapkan ke kamera, sedangkan arah yang tampil (depan / belakang / kiri / kanan)
    /// dipilih dari arah gerak relatif terhadap kamera. Jadi tetap benar walau kamera diputar dengan Z / C.
    /// Parameter Animator: Facing (int), IsMoving (bool), MoveSpeed (float), lihat Animations/Player/Player.controller.
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

        private static readonly int FacingHash = Animator.StringToHash("Facing");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

        private Animator animator;
        private Facing facing = Facing.Depan;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (visual == null)
            {
                var child = transform.Find("CharacterVisual");
                if (child != null) visual = child;
                else if (transform.childCount > 0) visual = transform.GetChild(0);
            }
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null && visual != null) animator = visual.GetComponent<Animator>();
            }
        }

        private void Update()
        {
            if (controller == null) return;
            if (animator == null && visual != null) animator = visual.GetComponent<Animator>();
            if (animator == null) return;

            Vector3 velocity = controller.HorizontalVelocity;
            bool moving = controller.IsMoving;
            if (moving) facing = ChooseFacing(velocity);

            animator.SetInteger(FacingHash, (int)facing);
            animator.SetBool(IsMovingHash, moving);

            float speed = 1f;
            if (moving && controller.walkSpeed > 0.01f)
                speed = Mathf.Clamp(velocity.magnitude / controller.walkSpeed, walkAnimSpeedRange.x, walkAnimSpeedRange.y);
            animator.SetFloat(MoveSpeedHash, speed);
        }

        private void LateUpdate()
        {
            // Pemain berputar mengikuti arah gerak, tapi gambar karakter harus tetap tegak menghadap kamera.
            var cam = Camera.main;
            if (visual != null && cam != null)
                visual.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
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
