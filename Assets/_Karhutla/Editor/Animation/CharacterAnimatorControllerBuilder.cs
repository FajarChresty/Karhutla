using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Karhutla.EditorTools
{
    /// <summary>
    /// Menu: Karhutla > Animasi > Buat Animator Controller Karakter
    ///
    /// Membuat Animator Controller standar untuk semua karakter Karhutla, dengan parameter yang dibaca
    /// CharacterAnimationDriver: Speed, IsMoving, IsSprinting, IsTalking, Interact.
    ///
    /// Klip dicari otomatis di folder Art/Characters (termasuk klip di dalam FBX) berdasarkan nama:
    ///   idle | walk | run/sprint | talk | interact/pickup/use
    /// Klip yang tidak ketemu dibiarkan kosong; isi manual di jendela Animator.
    /// Menjalankan menu lagi akan MENIMPA controller lama (klip terbaru ikut terpasang).
    /// </summary>
    public static class CharacterAnimatorControllerBuilder
    {
        private const string CharactersFolder = "Assets/_Karhutla/Art/Characters";
        private const string ControllerFolder = CharactersFolder + "/Animators";
        private const string ControllerPath = ControllerFolder + "/KarhutlaCharacter.controller";

        // Kecepatan dari PlayerController (walk 4.5, sprint 7) dan NpcController (moveSpeed 1.6).
        private const float NpcWalkSpeed = 1.6f;
        private const float PlayerWalkSpeed = 4.5f;
        private const float PlayerRunSpeed = 7f;

        [MenuItem("Karhutla/Animasi/Buat Animator Controller Karakter")]
        public static void Build()
        {
            EnsureFolder(ControllerFolder);
            var clips = FindClips();

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            // --- Locomotion: blend tree berdasarkan Speed ---
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            AddChild(tree, clips.idle, 0f, 1f);
            AddChild(tree, clips.walk, NpcWalkSpeed, 1f);                              // langkah santai warga
            AddChild(tree, clips.walk, PlayerWalkSpeed, PlayerWalkSpeed / NpcWalkSpeed * 0.6f); // jalan pemain (klip jalan dipercepat)
            AddChild(tree, clips.run != null ? clips.run : clips.walk, PlayerRunSpeed, clips.run != null ? 1f : 2f);
            sm.defaultState = locomotion;

            // --- Talk: saat dialog dan tidak bergerak ---
            var talk = sm.AddState("Talk", new Vector3(300f, 120f, 0f));
            talk.motion = clips.talk != null ? clips.talk : clips.idle;
            var toTalk = locomotion.AddTransition(talk);
            toTalk.hasExitTime = false;
            toTalk.duration = 0.2f;
            toTalk.AddCondition(AnimatorConditionMode.If, 0f, "IsTalking");
            toTalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            var fromTalk = talk.AddTransition(locomotion);
            fromTalk.hasExitTime = false;
            fromTalk.duration = 0.25f;
            fromTalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsTalking");

            // --- Interact: sekali putar lalu kembali ---
            var interact = sm.AddState("Interact", new Vector3(300f, -40f, 0f));
            interact.motion = clips.interact;
            var toInteract = sm.AddAnyStateTransition(interact);
            toInteract.hasExitTime = false;
            toInteract.duration = 0.1f;
            toInteract.canTransitionToSelf = false;
            toInteract.AddCondition(AnimatorConditionMode.If, 0f, "Interact");
            var fromInteract = interact.AddTransition(locomotion);
            fromInteract.hasExitTime = true;
            fromInteract.exitTime = 0.9f;
            fromInteract.duration = 0.15f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);

            Debug.Log("[Animasi] Controller dibuat: " + ControllerPath + "\n" + clips.Describe());
        }

        [MenuItem("Karhutla/Animasi/Buat Character Model Library")]
        public static void CreateLibrary()
        {
            const string folder = CharactersFolder + "/Resources";
            string path = folder + "/" + CharacterModelLibrary.ResourcePath + ".asset";
            EnsureFolder(folder);
            var existing = AssetDatabase.LoadAssetAtPath<CharacterModelLibrary>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<CharacterModelLibrary>();
                AssetDatabase.CreateAsset(existing, path);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
        }

        private static void AddChild(BlendTree tree, Motion motion, float threshold, float timeScale)
        {
            tree.AddChild(motion, threshold);
            var children = tree.children;
            children[children.Length - 1].timeScale = timeScale;
            tree.children = children;
        }

        private class ClipSet
        {
            public AnimationClip idle, walk, run, talk, interact;

            public string Describe()
            {
                return "Klip ditemukan -> idle: " + N(idle) + ", walk: " + N(walk) + ", run: " + N(run)
                       + ", talk: " + N(talk) + ", interact: " + N(interact);
            }

            private static string N(Object o) => o != null ? o.name : "(kosong)";
        }

        private static ClipSet FindClips()
        {
            var set = new ClipSet();
            if (!AssetDatabase.IsValidFolder(CharactersFolder)) return set;

            var seen = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { CharactersFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!seen.Add(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    var clip = asset as AnimationClip;
                    if (clip == null || clip.name.StartsWith("__preview__")) continue;
                    string n = clip.name.ToLowerInvariant();
                    if (set.idle == null && n.Contains("idle")) set.idle = clip;
                    else if (set.run == null && (n.Contains("run") || n.Contains("sprint"))) set.run = clip;
                    else if (set.walk == null && n.Contains("walk")) set.walk = clip;
                    else if (set.talk == null && n.Contains("talk")) set.talk = clip;
                    else if (set.interact == null && (n.Contains("interact") || n.Contains("pickup") || n.Contains("pick") || n.Contains("use"))) set.interact = clip;
                }
            }
            return set;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
