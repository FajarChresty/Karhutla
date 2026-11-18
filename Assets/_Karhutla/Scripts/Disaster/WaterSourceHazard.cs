using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Bahaya Pencemaran Sumber Air Warga oleh Abu & Jelaga Karhutla (GDD Bab 8.4).
    ///
    /// Aturan GDD:
    ///  - Di fase bencana (saat karhutla aktif), partikel abu pekat dan jelaga pembakaran gambut
    ///    mencemari sumber air warga (sumur gali desa dan kanal air).
    ///  - Air sumur berubah keruh kehitaman, berbau sangit, dan berbahaya diminum secara langsung
    ///    (memberikan item Air Tercemar, menyebabkan kerusakan kesehatan & rasa mual).
    ///  - Pemain dapat membawa air tercemar ke dapur warung untuk disaring dan direbus menjadi air bersih.
    ///  - Aksi Mitigasi: Pemain dapat bergotong royong bersama warga memasang penutup terpal
    ///    dan filter arang pelindung pada sumber air agar kembali bersih dan aman dari hujan abu karhutla.
    /// </summary>
    public class WaterSourceHazard : InteractableBase
    {
        public static readonly List<WaterSourceHazard> All = new List<WaterSourceHazard>();

        [Header("Pengaturan Sumber Air")]
        [Tooltip("Nama fasilitas sumber air")]
        public string sourceName = "Sumur Gali Desa";

        [Tooltip("Status apakah sumur sudah dipasangi penutup & filter pelindung abu")]
        public bool isProtected;

        public bool IsContaminated => !isProtected && DisasterSystem.Instance != null && DisasterSystem.Instance.FireStarted;

        private GameObject wellRoot;
        private GameObject waterSurface;
        private GameObject coverVisual;
        private WorldLabel worldLabel;

        private Renderer waterRenderer;
        private readonly Color cleanWaterColor = new Color(0.2f, 0.55f, 0.95f, 0.8f);
        private readonly Color murkyWaterColor = new Color(0.22f, 0.18f, 0.13f, 0.95f);

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Start()
        {
            interactRange = 2.8f;
            var col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.8f;
            col.center = new Vector3(0f, 0.5f, 0f);

            CreateVisuals();
            UpdateVisuals();
        }

        private void CreateVisuals()
        {
            if (wellRoot != null) return;

            wellRoot = new GameObject("Well_Structure");
            wellRoot.transform.SetParent(transform, false);

            // 1. Cincin sumur batu / beton
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DindingSumur";
            ring.transform.SetParent(wellRoot.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            ring.transform.localScale = new Vector3(2.2f, 0.45f, 2.2f);
            var ringRend = ring.GetComponent<Renderer>();
            if (ringRend != null) ringRend.material.color = new Color(0.48f, 0.48f, 0.48f); // batu abu-abu

            // 2. Permukaan air di dalam sumur
            waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterSurface.name = "PermukaanAir";
            Destroy(waterSurface.GetComponent<Collider>());
            waterSurface.transform.SetParent(wellRoot.transform, false);
            waterSurface.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            waterSurface.transform.localScale = new Vector3(1.85f, 0.05f, 1.85f);
            waterRenderer = waterSurface.GetComponent<Renderer>();

            // 3. Tiang kayu penyangga atap
            for (int i = -1; i <= 1; i += 2)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(pole.GetComponent<Collider>());
                pole.transform.SetParent(wellRoot.transform, false);
                pole.transform.localPosition = new Vector3(i * 1.0f, 1.4f, 0f);
                pole.transform.localScale = new Vector3(0.18f, 1.8f, 0.18f);
                var pRend = pole.GetComponent<Renderer>();
                if (pRend != null) pRend.material.color = new Color(0.45f, 0.3f, 0.18f);
            }

            // 4. Atap pelindung sumur
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(roof.GetComponent<Collider>());
            roof.transform.SetParent(wellRoot.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            roof.transform.localScale = new Vector3(2.5f, 0.15f, 2.2f);
            var rfRend = roof.GetComponent<Renderer>();
            if (rfRend != null) rfRend.material.color = new Color(0.6f, 0.35f, 0.2f); // genteng kayu

            // 5. Penutup terpal & filter kayu (muncul saat sumur sudah diproteksi)
            coverVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coverVisual.name = "PenutupTerpal_Filter";
            Destroy(coverVisual.GetComponent<Collider>());
            coverVisual.transform.SetParent(wellRoot.transform, false);
            coverVisual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            coverVisual.transform.localScale = new Vector3(2.3f, 0.08f, 2.3f);
            var covRend = coverVisual.GetComponent<Renderer>();
            if (covRend != null) covRend.material.color = new Color(0.2f, 0.35f, 0.25f); // terpal hijau gelap

            // 6. Label dunia di atas sumur
            var labelGo = new GameObject("Label_Sumur");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            worldLabel = labelGo.AddComponent<WorldLabel>();
            worldLabel.scale = 0.85f;
        }

        private void Update()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (wellRoot == null) return;

            bool contaminated = IsContaminated;

            if (waterRenderer != null)
            {
                waterRenderer.material.color = contaminated ? murkyWaterColor : cleanWaterColor;
            }

            if (coverVisual != null)
            {
                coverVisual.SetActive(isProtected);
            }

            if (worldLabel != null)
            {
                if (isProtected)
                {
                    worldLabel.text = sourceName + " (Terlindung Filter Arang)";
                    worldLabel.color = new Color(0.4f, 0.9f, 0.4f);
                }
                else if (contaminated)
                {
                    worldLabel.text = "[TERCEMAR] " + sourceName + " (Abu Karhutla)";
                    worldLabel.color = new Color(1f, 0.3f, 0.15f);
                }
                else
                {
                    worldLabel.text = sourceName + " (Air Bersih)";
                    worldLabel.color = new Color(0.35f, 0.75f, 1f);
                }
            }
        }

        public override string GetPrompt()
        {
            if (IsContaminated)
            {
                return "Periksa / Timba " + sourceName + " (TERCEMAR ABU) [E]";
            }

            if (isProtected)
            {
                return "Ambil Air Bersih dari " + sourceName + " (Terlindung) [E]";
            }

            return "Ambil Air Bersih / Minum dari " + sourceName + " [E]";
        }

        public override string GetSecondaryPrompt()
        {
            if (IsContaminated)
            {
                return "Gotong Royong: Pasang Penutup & Filter Arang Pelindung [Q]";
            }
            return null;
        }

        public override void Interact()
        {
            var surv = SurvivalSystem.Instance;
            var inv = Inventory.Instance;

            if (IsContaminated)
            {
                // Air tercemar!
                if (inv != null && inv.CanCarry(ItemDatabase.AirTercemar, 1))
                {
                    inv.Add(ItemDatabase.AirTercemar, 1);
                    Notifier.Show("Kamu menimba 1 botol [Air Tercemar Abu].\nBawa ke dapur warung untuk disaring & direbus agar layak minum.");
                }
                else
                {
                    // Minum darurat langsung
                    if (surv != null)
                    {
                        surv.TakeDamage(15f, "Keracunan Air Tercemar Abu");
                        surv.AddFatigue(15f);
                        surv.RestoreThirst(10f);
                        Notifier.Show("UHUK! Air di " + sourceName + " pekat bercampur jelaga dan abu gambut!\nPerut terasa mual melilit dan stamina anjlok (-15 HP, +15 Lelah).");
                    }
                }
            }
            else
            {
                // Air bersih & aman
                if (surv != null)
                {
                    surv.RestoreThirst(35f);
                }

                if (inv != null && inv.CanCarry(ItemDatabase.Air, 1))
                {
                    inv.Add(ItemDatabase.Air, 1);
                    Notifier.Show("Kamu menimba 1 botol [Air Mineral] bersih dan meminumnya segar (Haus -35).");
                }
                else
                {
                    Notifier.Show("Kamu meminum air sumur yang segar dan jernih (Haus -35).\n(Inventaris penuh untuk menimba botol air).");
                }
            }
        }

        public override void InteractSecondary()
        {
            if (!IsContaminated) return;

            var surv = SurvivalSystem.Instance;
            if (surv != null && surv.Fatigue >= 90f)
            {
                Notifier.Show("Kamu terlalu lelah untuk memasang penutup sumur! Istirahat dulu.");
                return;
            }

            isProtected = true;
            if (surv != null)
            {
                surv.AddFatigue(20f);
            }

            UpdateVisuals();
            Notifier.Show("GOTONG ROYONG BERHASIL!\nKamu dan warga memasang penutup terpal dan filter arang pada " + sourceName + ".\nSumber air warga kini aman dan terlindungi dari hujan abu karhutla!");
            DayReportSystem.LogActivity("Memasang penutup & filter pelindung pada " + sourceName);
        }

        public void RestoreState(bool protectedState)
        {
            isProtected = protectedState;
            UpdateVisuals();
        }
    }
}
