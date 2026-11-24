using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Animasi CADANGAN untuk karakter yang belum punya model 3D (masih kapsul dummy).
    /// Membuat karakter terasa hidup tanpa aset: bernapas saat diam, memantul & condong saat jalan/lari,
    /// mengangguk saat bicara, dan "denyut" singkat saat berinteraksi.
    ///
    /// Cara kerja: renderer kapsul di root disembunyikan (forceRenderingOff, bukan dimatikan, supaya kode lain
    /// yang mengubah warna/enabled tetap berlaku), lalu salinannya dibuat sebagai anak "ProceduralVisual"
    /// yang digerakkan. Anak yang murni visual (mis. "Penunjuk Arah") ikut dipindah ke dalamnya.
    /// Root (CharacterController/collider) tidak disentuh sama sekali.
    ///
    /// Otomatis nonaktif ketika model 3D dengan Animator dipasang (lihat CharacterVisualInstaller).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterAnimationDriver))]
    public class ProceduralCharacterMotion : MonoBehaviour
    {
        [Header("Diam (napas)")]
        public float breatheRate = 1.6f;
        public float breatheAmount = 0.025f;

        [Header("Jalan / Lari")]
        [Tooltip("Langkah per meter. Makin besar = langkah makin rapat.")]
        public float stepsPerMeter = 1.35f;
        public float bobHeight = 0.07f;
        public float squashAmount = 0.06f;
        [Tooltip("Sudut condong ke depan saat lari penuh (derajat).")]
        public float maxLean = 12f;
        public float sway = 4f;

        [Header("Bicara")]
        public float nodRate = 5f;
        public float nodAngle = 5f;

        [Header("Aksi (Interact)")]
        public float pulseDuration = 0.35f;
        public float pulseScale = 0.12f;

        private CharacterAnimationDriver driver;
        private Renderer sourceRenderer;
        private MeshRenderer proxyRenderer;
        private Transform visual;
        private Vector3 visualBasePos;
        private Vector3 visualBaseScale;
        private float stepPhase;
        private float lean;
        private float pulseTimer;
        private float timeOffset;

        public Transform Visual => visual;

        private void Awake()
        {
            driver = GetComponent<CharacterAnimationDriver>();
            timeOffset = Random.value * 10f; // supaya NPC tidak bernapas serempak
        }

        private void OnEnable()
        {
            if (visual == null) BuildProxy();
            SetSourceHidden(true);
            if (visual != null) visual.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            SetSourceHidden(false);
            if (visual != null) visual.gameObject.SetActive(false);
        }

        /// <summary>Denyut singkat (dipanggil saat interaksi).</summary>
        public void Pulse()
        {
            pulseTimer = pulseDuration;
        }

        private void BuildProxy()
        {
            sourceRenderer = GetComponent<Renderer>();
            var sourceFilter = GetComponent<MeshFilter>();

            var go = new GameObject("ProceduralVisual");
            visual = go.transform;
            visual.SetParent(transform, false);

            if (sourceRenderer is MeshRenderer && sourceFilter != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                proxyRenderer = go.AddComponent<MeshRenderer>();
                proxyRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            }

            // Pindahkan anak yang murni visual (hanya Transform + MeshFilter + MeshRenderer) agar ikut bergerak.
            var toMove = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child == visual) continue;
                var comps = child.GetComponents<Component>();
                bool pureVisual = comps.Length <= 3 && child.childCount == 0;
                foreach (var c in comps)
                {
                    if (!(c is Transform || c is MeshFilter || c is MeshRenderer)) { pureVisual = false; break; }
                }
                if (pureVisual) toMove.Add(child);
            }
            foreach (var child in toMove) child.SetParent(visual, true);

            visualBasePos = visual.localPosition;
            visualBaseScale = visual.localScale;
        }

        private void SetSourceHidden(bool hidden)
        {
            if (sourceRenderer != null) sourceRenderer.forceRenderingOff = hidden;
        }

        private void LateUpdate()
        {
            if (visual == null) return;

            // Ikuti perubahan warna/visibilitas yang dibuat kode lain pada renderer asli.
            if (proxyRenderer != null && sourceRenderer != null)
            {
                proxyRenderer.enabled = sourceRenderer.enabled;
                // sharedMaterial (tunggal) tidak mengalokasi memori tiap frame, beda dengan sharedMaterials.
                var mat = sourceRenderer.sharedMaterial;
                if (proxyRenderer.sharedMaterial != mat) proxyRenderer.sharedMaterial = mat;
            }

            float dt = Time.deltaTime;
            float t = Time.time + timeOffset;
            float speed = driver.Speed;
            float run01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, driver.SprintSpeed));
            float move01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, driver.WalkSpeed));

            // Fase langkah maju sesuai jarak tempuh, bukan waktu -> kaki tidak "meluncur".
            stepPhase += speed * stepsPerMeter * dt * Mathf.PI;
            float step = Mathf.Abs(Mathf.Sin(stepPhase));          // 0..1, dua pantulan per siklus
            float side = Mathf.Sin(stepPhase);                      // -1..1, kiri/kanan

            // Napas (hanya terasa saat diam).
            float breathe = Mathf.Sin(t * breatheRate * Mathf.PI * 2f * 0.5f) * breatheAmount * (1f - move01);

            float bob = step * bobHeight * move01 * (1f + run01 * 0.6f);
            float squash = (1f - step) * squashAmount * move01;      // tertekan saat kaki menapak

            lean = Mathf.Lerp(lean, maxLean * run01, 1f - Mathf.Exp(-dt * 8f));
            float roll = side * sway * move01;

            float nod = driver.IsTalking ? Mathf.Sin(t * nodRate) * nodAngle : 0f;

            float pulse = 0f;
            if (pulseTimer > 0f)
            {
                pulseTimer -= dt;
                float p = 1f - Mathf.Clamp01(pulseTimer / Mathf.Max(0.01f, pulseDuration));
                pulse = Mathf.Sin(p * Mathf.PI) * pulseScale;
            }

            visual.localPosition = visualBasePos + new Vector3(0f, bob, 0f);
            visual.localRotation = Quaternion.Euler(lean + nod, 0f, roll);
            float sy = 1f + breathe - squash + pulse;
            float sxz = 1f - breathe * 0.5f + squash * 0.5f + pulse * 0.5f;
            visual.localScale = new Vector3(visualBaseScale.x * sxz, visualBaseScale.y * sy, visualBaseScale.z * sxz);
        }
    }
}
