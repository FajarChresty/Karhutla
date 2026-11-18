using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Sistem Misi Aksi Nyata Membantu Pemadaman Api & Sekat Bakar (GDD Bab 3.4 & 3.5).
    ///
    /// Aturan GDD:
    ///  - Pemain berkontribusi secara nyata menanggulangi karhutla bersama regu relawan desa (MPA).
    ///  - Pembuatan sekat bakar (firebreaks) di perbatasan utara untuk memutus jalur api gambut.
    ///  - Aksi langsung menyiram dan memadamkan pohon terbakar dengan air.
    ///  - Keberhasilan mengendalikan api menjadi salah satu dari 3 pilar penentu Ending permainan.
    /// </summary>
    public class FirefightingMission : MonoBehaviour
    {
        public static FirefightingMission Instance { get; private set; }

        public const int RequiredFirebreaks = 3;

        public int CompletedFirebreaks { get; private set; }
        public int ExtinguishedTreesCount { get; private set; }
        public bool IsFireContained { get; private set; }

        public event Action<int> OnFirebreakProgressChanged;
        public event Action<int> OnTreeExtinguished;
        public event Action OnFireContained;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Catat satu zona sekat bakar yang selesai dibangun.</summary>
        public void CompleteFirebreakZone(string sectorName)
        {
            CompletedFirebreaks++;
            DayReportSystem.LogActivity("Menuntaskan sekat bakar " + sectorName);

            // Terapkan perlambatan nyata pada api
            var disaster = DisasterSystem.Instance;
            if (disaster != null)
            {
                disaster.spreadDaysToMaxRadius += 10f;
                disaster.smokeReach = Mathf.Max(15f, disaster.smokeReach - 8f);
                disaster.maxRadius = Mathf.Max(25f, disaster.maxRadius - 3.5f);
            }

            Notifier.Show("SEKAT BAKAR SELESAI (" + CompletedFirebreaks + "/" + RequiredFirebreaks + ")!\n"
                + "Parit sekat bakar " + sectorName + " berhasil memutus jalur rambatan api gambut.");

            OnFirebreakProgressChanged?.Invoke(CompletedFirebreaks);

            if (CompletedFirebreaks >= RequiredFirebreaks && !IsFireContained)
            {
                SetFireContained();
            }
        }

        public void SetFireContained()
        {
            if (IsFireContained) return;
            IsFireContained = true;

            Notifier.Show("KEBAKARAN TERKENDALI!\nSeluruh sekat bakar desa telah tersambung kokoh. Pemukiman warga aman dari amukan api!");
            DayReportSystem.LogActivity("MISI SUKSES: Kebakaran hutan berhasil dikendalikan!");

            OnFireContained?.Invoke();
        }

        /// <summary>Catat aksi pemain memadamkan pohon terbakar.</summary>
        public void RecordTreeExtinguished()
        {
            ExtinguishedTreesCount++;
            DayReportSystem.LogActivity("Memadamkan titik api pohon (" + ExtinguishedTreesCount + ")");
            OnTreeExtinguished?.Invoke(ExtinguishedTreesCount);

            if (ExtinguishedTreesCount >= 5 && CompletedFirebreaks >= 2 && !IsFireContained)
            {
                SetFireContained();
            }
        }

        // Simpan & Muat
        public void RestoreState(int firebreaks, int treesExtinguished, bool contained)
        {
            CompletedFirebreaks = firebreaks;
            ExtinguishedTreesCount = treesExtinguished;
            IsFireContained = contained;
        }
    }
}
