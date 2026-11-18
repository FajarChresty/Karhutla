using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Bahaya Lingkungan Tanah Gambut Ambles / Labil (GDD Bab 8.4).
    ///
    /// Aturan GDD:
    ///  - Di fase bencana (karhutla aktif), bara gambut bawah tanah membakar lapisan organik di bawah permukaan,
    ///    menciptakan rongga tersembunyi yang membuat tanah di atasnya labil dan mudah ambles/longsor.
    ///  - Pemain yang melangkah ke area ini akan terperosok, mengalami perlambatan gerak drastis (movement penalty),
    ///    kelelahan fisik meningkat cepat, dan jika berada terlalu lama akan menerima damage panas bara gambut.
    ///  - Aksi Mitigasi: Pemain dapat bergotong royong memasang papan titian kayu (stabilisasi jalur)
    ///    sehingga area tersebut aman dilewati kembali.
    /// </summary>
    public class UnstableGroundHazard : InteractableBase
    {
        public static readonly List<UnstableGroundHazard> All = new List<UnstableGroundHazard>();

        [Header("Pengaturan Zona")]
        [Tooltip("Nama zona tanah gambut labil")]
        public string zoneName = "Tanah Gambut Labil";

        [Tooltip("Radius area tanah yang labil/ambles (meter)")]
        public float hazardRadius = 4.5f;

        [Tooltip("Pengali kecepatan pemain saat menginjak tanah ambles (0.4 = gerak hanya 40%)")]
        [Range(0.1f, 0.9f)]
        public float speedPenalty = 0.42f;

        [Tooltip("Tambahan Fatigue per detik saat bergerak di tanah labil")]
        public float fatiguePerSecond = 3.5f;

        [Tooltip("Damage ke kesehatan per detik jika terperosok terlalu lama ke bara gambut")]
        public float heatDamagePerSecond = 9f;

        [Tooltip("Toleransi waktu sebelum mulai terkena luka panas (detik)")]
        public float toleranceSeconds = 1.2f;

        [Header("Status")]
        public bool isStabilized;

        public bool IsHazardActive => !isStabilized && DisasterSystem.Instance != null && DisasterSystem.Instance.FireStarted;

        private GameObject sinkholeVisual;
        private GameObject cracksVisual;
        private GameObject planksVisual;
        private WorldLabel worldLabel;

        private float timeInside;
        private float nextWarnTime;

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
            interactRange = hazardRadius + 1.2f;
            var col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = hazardRadius;

            CreateVisuals();
            UpdateVisuals();
        }

        private void CreateVisuals()
        {
            if (sinkholeVisual != null) return;

            // 1. Permukaan tanah amblas/retak (silinder pipih gelap di permukaan tanah)
            sinkholeVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sinkholeVisual.name = "TanahAmbles_Visual";
            Destroy(sinkholeVisual.GetComponent<Collider>());
            sinkholeVisual.transform.SetParent(transform, false);
            sinkholeVisual.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            sinkholeVisual.transform.localScale = new Vector3(hazardRadius * 2f, 0.04f, hazardRadius * 2f);

            var rend = sinkholeVisual.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.14f, 0.10f, 0.06f); // gambut arang gelap
            }

            // 2. Aksen retakan bara bawah tanah (kubus pipih menyilang oranye bara)
            cracksVisual = new GameObject("RetakanBara_Visual");
            cracksVisual.transform.SetParent(transform, false);
            cracksVisual.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            for (int i = 0; i < 3; i++)
            {
                var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(crack.GetComponent<Collider>());
                crack.transform.SetParent(cracksVisual.transform, false);
                crack.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                crack.transform.localScale = new Vector3(hazardRadius * 1.6f, 0.03f, 0.35f);

                var crRend = crack.GetComponent<Renderer>();
                if (crRend != null)
                {
                    crRend.material.color = new Color(0.85f, 0.32f, 0.05f); // bara oranye gambut
                }
            }

            // 3. Papan titian kayu (muncul saat sudah distabilkan)
            planksVisual = new GameObject("TitianKayu_Visual");
            planksVisual.transform.SetParent(transform, false);
            planksVisual.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            for (int i = -3; i <= 3; i++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(plank.GetComponent<Collider>());
                plank.transform.SetParent(planksVisual.transform, false);
                plank.transform.localPosition = new Vector3(i * 1.1f, 0f, 0f);
                plank.transform.localScale = new Vector3(0.9f, 0.12f, hazardRadius * 1.8f);

                var pRend = plank.GetComponent<Renderer>();
                if (pRend != null)
                {
                    pRend.material.color = new Color(0.55f, 0.38f, 0.22f); // kayu papan
                }
            }

            // Label melayang di atas zona
            var labelGo = new GameObject("Label_Hazard");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            worldLabel = labelGo.AddComponent<WorldLabel>();
            worldLabel.scale = 0.8f;
        }

        private void Update()
        {
            UpdateVisuals();

            var player = PlayerController.Instance;
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);

            if (dist <= hazardRadius)
            {
                if (IsHazardActive)
                {
                    // Terapkan penalti kecepatan
                    player.EnvironmentalSpeedMultiplier = Mathf.Min(player.EnvironmentalSpeedMultiplier, speedPenalty);

                    // Tambah kelelahan karena tanah gambut amblas & lembek
                    var surv = SurvivalSystem.Instance;
                    if (surv != null)
                    {
                        surv.AddFatigue(fatiguePerSecond * Time.deltaTime);

                        timeInside += Time.deltaTime;
                        if (timeInside > toleranceSeconds)
                        {
                            surv.TakeDamage(heatDamagePerSecond * Time.deltaTime, "Bara Gambut Bawah Tanah");

                            if (Time.time >= nextWarnTime)
                            {
                                nextWarnTime = Time.time + 3.5f;
                                Notifier.Show("AWAS! Kaki terperosok ke rongga gambut membara di " + zoneName + "!\nTanah labil dan panas, segera menepi!");
                            }
                        }
                    }
                }
                else
                {
                    timeInside = 0f;
                }
            }
            else
            {
                timeInside = 0f;
            }
        }

        private void UpdateVisuals()
        {
            if (sinkholeVisual == null) return;

            bool active = IsHazardActive;

            if (cracksVisual != null) cracksVisual.SetActive(active);
            if (planksVisual != null) planksVisual.SetActive(isStabilized);

            if (worldLabel != null)
            {
                if (isStabilized)
                {
                    worldLabel.text = zoneName + " (Aman - Bertitian Kayu)";
                    worldLabel.color = new Color(0.4f, 0.9f, 0.4f);
                }
                else if (active)
                {
                    worldLabel.text = "[AWAS] " + zoneName + " (Ambles & Membara)";
                    worldLabel.color = new Color(1f, 0.35f, 0.15f);
                }
                else
                {
                    worldLabel.text = zoneName;
                    worldLabel.color = new Color(0.8f, 0.75f, 0.65f);
                }
            }
        }

        public override string GetPrompt()
        {
            if (isStabilized)
            {
                return zoneName + " (AMAN - Sudah Terpasang Titian Kayu)";
            }

            if (IsHazardActive)
            {
                return "Gotong Royong: Pasang Titian Kayu di " + zoneName + " [E]";
            }

            return "Periksa " + zoneName + " (Kondisi tanah gambut lembek)";
        }

        public override void Interact()
        {
            if (isStabilized)
            {
                Notifier.Show("Titian kayu di " + zoneName + " sudah kokoh terpasang. Jalur aman dilalui warga.");
                return;
            }

            var surv = SurvivalSystem.Instance;
            if (surv != null && surv.Fatigue >= 90f)
            {
                Notifier.Show("Kamu terlalu lelah untuk memasang titian kayu! Istirahat dulu di bale-bale.");
                return;
            }

            // Stabilkan zona
            isStabilized = true;
            if (surv != null)
            {
                surv.AddFatigue(20f);
                surv.TakeDamage(2f); // lecet sedikit saat bekerja di zona bara
            }

            UpdateVisuals();
            Notifier.Show("GOTONG ROYONG BERHASIL!\nKamu memasang papan titian kayu di atas " + zoneName + ".\nTanah stabil dan kini aman dilewati!");
            DayReportSystem.LogActivity("Memasang titian kayu menstabilkan " + zoneName);
        }

        public void RestoreState(bool stabilized)
        {
            isStabilized = stabilized;
            UpdateVisuals();
        }
    }
}
