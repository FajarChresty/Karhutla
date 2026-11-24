using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Memasang sistem animasi ke pemain dan semua warga SECARA OTOMATIS saat game berjalan.
    /// Tidak perlu mengubah scene: objek ini dibuat sendiri ketika scene dimuat.
    ///
    /// Untuk tiap PlayerController / NpcController (termasuk warga yang dibuat RuntimeVillagePopulator):
    ///   1. Tambah CharacterAnimationDriver.
    ///   2. Jika CharacterModelLibrary punya model untuknya -> pasang model 3D (Animator) dan sembunyikan kapsul.
    ///      Jika belum -> pakai ProceduralCharacterMotion (kapsul yang "hidup").
    ///
    /// Matikan seluruh sistem dengan mengisi <see cref="AutoInstall"/> = false sebelum scene dimuat.
    /// </summary>
    public class CharacterVisualInstaller : MonoBehaviour
    {
        public static bool AutoInstall = true;

        [Tooltip("Jeda pemindaian karakter baru (detik).")]
        public float scanInterval = 0.5f;

        private readonly HashSet<int> handled = new HashSet<int>();
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!AutoInstall) return;
#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<CharacterVisualInstaller>() != null) return;
#else
            if (FindObjectOfType<CharacterVisualInstaller>() != null) return;
#endif
            var go = new GameObject("[CharacterVisualInstaller]");
            DontDestroyOnLoad(go);
            go.AddComponent<CharacterVisualInstaller>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + scanInterval;
            Scan();
        }

        private void Scan()
        {
#if UNITY_2023_1_OR_NEWER
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            var npcs = FindObjectsByType<NpcController>(FindObjectsSortMode.None);
#else
            var players = FindObjectsOfType<PlayerController>();
            var npcs = FindObjectsOfType<NpcController>();
#endif
            foreach (var p in players) Install(p.gameObject, p.gameObject.name, true);
            foreach (var n in npcs) Install(n.gameObject, n.NpcId, false);
        }

        private void Install(GameObject character, string id, bool isPlayer)
        {
            if (!handled.Add(character.GetInstanceID())) return;

            var driver = character.GetComponent<CharacterAnimationDriver>();
            if (driver == null) driver = character.AddComponent<CharacterAnimationDriver>();

            // Model 3D sudah ada di prefab/scene (dipasang manual) -> cukup driver.
            var existing = character.GetComponentInChildren<Animator>();
            if (existing != null)
            {
                driver.SetAnimator(existing);
                return;
            }

            var library = CharacterModelLibrary.Load();
            var entry = library != null ? library.Find(id, isPlayer) : null;
            if (entry != null)
            {
                var animator = SpawnModel(character, entry);
                if (animator != null)
                {
                    driver.SetAnimator(animator);
                    return;
                }
            }

            if (character.GetComponent<ProceduralCharacterMotion>() == null)
                character.AddComponent<ProceduralCharacterMotion>();
        }

        /// <summary>Memasang model sebagai anak, kaki di dasar kapsul, ukuran tidak ikut skala kapsul.</summary>
        private static Animator SpawnModel(GameObject character, CharacterModelLibrary.Entry entry)
        {
            var root = character.transform;
            var capsuleRenderer = character.GetComponent<Renderer>();

            // Titik kaki = dasar bounds renderer/collider karakter.
            float feetY = root.position.y;
            if (capsuleRenderer != null) feetY = capsuleRenderer.bounds.min.y;
            else if (character.TryGetComponent<Collider>(out var col)) feetY = col.bounds.min.y;
            else if (character.TryGetComponent<CharacterController>(out var cc)) feetY = cc.bounds.min.y;

            var model = Instantiate(entry.prefab, root);
            model.name = "Model_" + entry.prefab.name;
            var mt = model.transform;
            mt.position = new Vector3(root.position.x, feetY, root.position.z);
            mt.localRotation = Quaternion.Euler(0f, entry.yawOffset, 0f);

            Vector3 lossy = root.lossyScale;
            float s = Mathf.Max(0.0001f, entry.scale);
            mt.localScale = new Vector3(s / Mathf.Max(0.0001f, lossy.x), s / Mathf.Max(0.0001f, lossy.y), s / Mathf.Max(0.0001f, lossy.z));

            // Collider bawaan model tidak boleh mengganggu CharacterController/raycast interaksi.
            foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;

            // Sembunyikan kapsul & anak visual lama (tanpa mematikan komponennya).
            if (capsuleRenderer != null) capsuleRenderer.forceRenderingOff = true;
            foreach (Transform child in root)
            {
                if (child == mt) continue;
                var r = child.GetComponent<MeshRenderer>();
                if (r != null && child.GetComponents<Component>().Length <= 3) r.forceRenderingOff = true;
            }

            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[Animasi] Prefab '" + entry.prefab.name + "' tidak punya Animator. Model dipasang tanpa animasi.");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[Animasi] Animator di '" + entry.prefab.name + "' belum punya Controller. Isi dengan controller dari menu Karhutla > Animasi.");
            }
            return animator;
        }
    }
}
