using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Titik Interaktif Pembuatan Sekat Bakar (Firebreak) bersama Relawan Desa (GDD Bab 3.4).
    /// Diletakkan di sepanjang perbatasan utara antara desa dan hutan gambut.
    /// </summary>
    public class FirebreakSpot : InteractableBase
    {
        [Tooltip("Nama sektor sekat bakar (mis. Sektor Barat, Sektor Tengah, Sektor Timur)")]
        public string sectorName = "Sektor Utara";

        [Tooltip("Tahap pengerjaan yang dibutuhkan (default: 3 tahap)")]
        public int requiredStages = 3;

        public int currentStage = 0;
        public bool IsFinished => currentStage >= requiredStages;

        private GameObject trenchVisual;

        private void Start()
        {
            CreateTrenchVisual();
            UpdateVisual();
        }

        private void CreateTrenchVisual()
        {
            if (trenchVisual != null) return;

            // Visual parit sekat bakar (Cube panjang pipih warna tanah gelap/basah)
            trenchVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trenchVisual.name = "ParitSekatBakar";
            Destroy(trenchVisual.GetComponent<Collider>());
            trenchVisual.transform.SetParent(transform, false);
            trenchVisual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            trenchVisual.transform.localScale = new Vector3(14f, 0.2f, 3f);

            var rend = trenchVisual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.2f, 0.14f, 0.08f); // tanah gambut basah
            }
        }

        private void UpdateVisual()
        {
            if (trenchVisual != null)
            {
                trenchVisual.SetActive(currentStage > 0);
                if (currentStage > 0)
                {
                    float width = Mathf.Lerp(1.5f, 4.5f, (float)currentStage / requiredStages);
                    trenchVisual.transform.localScale = new Vector3(14f, 0.2f, width);
                }
            }
        }

        public override string GetPrompt()
        {
            if (IsFinished)
            {
                return "Sekat Bakar " + sectorName + " (TUNTAS & AMAN)";
            }
            return "Kerja Bakti: Gali Sekat Bakar " + sectorName + " (Tahap " + currentStage + "/" + requiredStages + ")";
        }

        public override void Interact()
        {
            if (IsFinished)
            {
                Notifier.Show("Sekat bakar " + sectorName + " sudah selesai dibuat dan kokoh memutus api gambut.");
                return;
            }

            var surv = SurvivalSystem.Instance;
            if (surv != null && surv.Fatigue >= 90f)
            {
                Notifier.Show("Kamu terlalu lelah untuk menggali sekat bakar! Istirahat dulu di bale-bale.");
                return;
            }

            currentStage++;
            UpdateVisual();

            // Konsumsi stamina / kelelahan
            if (surv != null)
            {
                surv.ApplyItem(new ItemDefinition("dummy_work", "", "", 0, Color.black, fatigue: -25f, thirst: -15f));
            }

            if (currentStage >= requiredStages)
            {
                if (FirefightingMission.Instance != null)
                {
                    FirefightingMission.Instance.CompleteFirebreakZone(sectorName);
                }
                else
                {
                    Notifier.Show("Sekat Bakar " + sectorName + " selesai!");
                }
            }
            else
            {
                Notifier.Show("Mengerjakan sekat bakar " + sectorName + " (Tahap " + currentStage + "/" + requiredStages + ").\nSeresah kering dibersihkan dan parit tanah mulai terbentuk.");
            }
        }
    }
}
