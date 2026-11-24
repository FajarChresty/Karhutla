using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Daftar model 3D karakter. Animator cukup mengisi aset ini, tanpa mengubah scene atau kode.
    ///
    /// Buat lewat: Create > Karhutla > Animasi > Character Model Library,
    /// lalu SIMPAN di folder bernama "Resources" dengan nama file "KarhutlaCharacterModels"
    /// (mis. Assets/_Karhutla/Art/Characters/Resources/KarhutlaCharacterModels.asset) agar dimuat otomatis.
    ///
    /// Prefab model: FBX/prefab yang punya Animator (rig Humanoid disarankan) dengan Animator Controller
    /// buatan menu Karhutla > Animasi > Buat Animator Controller Karakter.
    /// </summary>
    [CreateAssetMenu(menuName = "Karhutla/Animasi/Character Model Library", fileName = "KarhutlaCharacterModels")]
    public class CharacterModelLibrary : ScriptableObject
    {
        public const string ResourcePath = "KarhutlaCharacterModels";

        [Serializable]
        public class Entry
        {
            [Tooltip("Nama objek atau npcName/npcId warga, mis. \"Player\", \"Mas Hendra\", \"Pak Tejo\".")]
            public string id;
            public GameObject prefab;
            [Tooltip("Pengali ukuran model.")]
            public float scale = 1f;
            [Tooltip("Rotasi tambahan (derajat, sumbu Y) bila model tidak menghadap +Z.")]
            public float yawOffset = 0f;
        }

        [Tooltip("Model khusus per karakter (dicek lebih dulu).")]
        public List<Entry> characters = new List<Entry>();

        [Tooltip("Model untuk pemain bila tidak ada entri \"Player\".")]
        public Entry defaultPlayer = new Entry { id = "Player" };

        [Tooltip("Model untuk warga yang tidak punya entri khusus. Kosong = tetap kapsul + animasi prosedural.")]
        public Entry defaultNpc = new Entry { id = "NPC" };

        public Entry Find(string id, bool isPlayer)
        {
            if (!string.IsNullOrEmpty(id))
            {
                foreach (var e in characters)
                {
                    if (e != null && e.prefab != null && string.Equals(e.id, id, StringComparison.OrdinalIgnoreCase)) return e;
                }
            }
            var fallback = isPlayer ? defaultPlayer : defaultNpc;
            return fallback != null && fallback.prefab != null ? fallback : null;
        }

        private static CharacterModelLibrary cached;
        private static bool searched;

        /// <summary>Library dari Resources (boleh null jika belum dibuat).</summary>
        public static CharacterModelLibrary Load()
        {
            if (!searched)
            {
                searched = true;
                cached = Resources.Load<CharacterModelLibrary>(ResourcePath);
            }
            return cached;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cached = null;
            searched = false;
        }
    }
}
