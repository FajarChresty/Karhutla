using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Teks 3D kecil di atas objek (nama NPC, nama item, dll) yang selalu menghadap kamera.
    /// Dummy sederhana memakai TextMesh bawaan Unity.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        public string text = "Label";
        public Color color = Color.white;
        public float scale = 1f;

        private TextMesh textMesh;
        private Camera cam;

        private void Start() { Refresh(); }

        public void SetText(string newText)
        {
            text = newText;
            Refresh();
        }

        public void Refresh()
        {
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMesh>();
                if (textMesh == null) textMesh = gameObject.AddComponent<TextMesh>();
                Font font = UIFactory.DefaultFont;
                textMesh.font = font;
                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.sharedMaterial = font.material;
            }
            textMesh.text = text;
            textMesh.color = color;
            textMesh.fontSize = 100;
            textMesh.characterSize = 0.06f * scale;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }

        public static WorldLabel Create(Transform parent, Vector3 localPosition, string text, Color color, float scale = 1f)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var label = go.AddComponent<WorldLabel>();
            label.text = text;
            label.color = color;
            label.scale = scale;
            label.Refresh();
            return label;
        }
    }
}
