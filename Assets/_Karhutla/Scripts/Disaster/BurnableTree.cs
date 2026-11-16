using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    public enum TreeState { Healthy, Burning, Burnt }

    /// <summary>
    /// Pohon dummy yang bisa terbakar. DisasterSystem yang memutuskan kapan pohon menyala.
    /// Visual: sehat (hijau) -> terbakar (oranye + bola api berkedip) -> hangus (hitam).
    /// </summary>
    public class BurnableTree : MonoBehaviour
    {
        public static readonly List<BurnableTree> All = new List<BurnableTree>();

        public TreeState State { get; private set; }

        private Renderer[] renderers;
        private Material[] originalMaterials;
        private Transform flame;
        private Vector3 flameBaseScale;
        private float burnMinutesLeft;
        private float phase;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterial : null;
            phase = Random.value * 6.28f;
        }

        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public void Ignite(Material burningMaterial, Material flameMaterial, float durationMinutes)
        {
            if (State != TreeState.Healthy) return;
            State = TreeState.Burning;
            burnMinutesLeft = durationMinutes * Random.Range(0.8f, 1.2f);
            SetMaterial(burningMaterial);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Flame";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 4.4f, 0f);
            flameBaseScale = Vector3.one * 1.3f;
            go.transform.localScale = flameBaseScale;
            if (flameMaterial != null) go.GetComponent<Renderer>().sharedMaterial = flameMaterial;
            flame = go.transform;
        }

        /// <summary>Kembalikan pohon ke keadaan sehat (dipakai saat memuat data simpan).</summary>
        public void ResetToHealthy()
        {
            if (State == TreeState.Healthy) return;
            State = TreeState.Healthy;
            if (flame != null) Destroy(flame.gameObject);
            flame = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null) renderers[i].sharedMaterial = originalMaterials[i];
            }
        }

        /// <summary>Dipanggil DisasterSystem setiap menit game.</summary>
        public void TickMinute(Material burntMaterial)
        {
            if (State != TreeState.Burning) return;
            burnMinutesLeft -= 1f;
            if (burnMinutesLeft <= 0f)
            {
                State = TreeState.Burnt;
                SetMaterial(burntMaterial);
                if (flame != null) Destroy(flame.gameObject);
            }
        }

        private void Update()
        {
            if (State == TreeState.Burning && flame != null)
            {
                float s = 1f + 0.25f * Mathf.Sin(Time.time * 9f + phase);
                flame.localScale = flameBaseScale * s;
            }
        }

        private void SetMaterial(Material m)
        {
            if (m == null) return;
            foreach (var r in renderers)
            {
                if (r != null && r.gameObject != (flame != null ? flame.gameObject : null)) r.sharedMaterial = m;
            }
        }
    }
}
