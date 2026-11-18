using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    [Serializable]
    public class SaveBatch
    {
        public string id;
        public int count;
        public float age;
    }

    /// <summary>Seluruh data simpan permainan (satu slot, PlayerPrefs, JSON).</summary>
    [Serializable]
    public class GameSaveData
    {
        public int version = 1;

        // Waktu
        public int day = 1;
        public int period;
        public int unitsRemaining;
        public int minutesIntoSection;
        public int totalMinutes;
        public int daysPlayed = 1;
        public int daysSkipped;
        public bool finalDayEnded;

        // Bencana
        public bool fireStarted;
        public bool fireByDebug;
        public int fireStartMinute;
        public float fireRadiusBonus;

        // Tubuh pemain
        public float hunger, thirst, fatigue, smoke, health;
        public int maskMinutes;

        // Barang
        public int money;
        public List<SaveBatch> inventory = new List<SaveBatch>();
        public List<SaveBatch> gudang = new List<SaveBatch>();
        public List<SaveBatch> kulkas = new List<SaveBatch>();

        // Warung
        public int sundayChoice;
        public int sessionDoneDay = -1;

        // Posisi pemain
        public float px, py, pz;

        // Stok barter tiap warga
        public List<BarterTraderSave> traders = new List<BarterTraderSave>();

        // Sesi 3 & 4: Resep, Penyelidikan Korupsi, Pemadaman Api, dan Kebangkrutan
        public List<string> unlockedRecipes = new List<string>();
        public List<string> collectedClues = new List<string>();
        public bool corruptionExposed;
        public int completedFirebreaks;
        public int extinguishedTrees;
        public bool fireContained;
        public int bankruptcyCriticalDays;
        public bool inBankruptcyCrisis;

        // Sesi 6: Bahaya Lingkungan Tambahan (GDD Bab 8.4)
        public List<string> stabilizedHazards = new List<string>();
        public List<string> protectedWaterSources = new List<string>();
    }

    /// <summary>
    /// Simpan dan muat permainan secara keseluruhan (satu slot). Melengkapi RelationshipSystem yang menyimpan hubungan sendiri.
    ///
    /// Yang disimpan: hari/periode/jatah waktu, api (mulai kapan), kondisi tubuh, uang, inventaris, gudang, kulkas
    /// (beserta umur tiap barang), keputusan hari Minggu warung, posisi pemain, stok barter tiap warga.
    /// Hubungan sosial: lewat RelationshipSystem (PlayerPrefs terpisah) dan dimuat ulang bersama data ini.
    ///
    /// Kapan: otomatis saat hari/periode berganti dan saat keluar dari Play; manual F5 = simpan, F9 = muat, F10 = hapus data (mulai baru).
    /// Memuat otomatis saat Play dimulai bila ada data (autoLoadOnStart).
    /// Tidak dilakukan saat sesi warung sedang berlangsung (pelanggan dan masakan yang sedang dibuat tidak ikut tersimpan).
    ///
    /// Batasan: catatan laporan harian yang sedang berjalan, pelanggan aktif, masakan yang sedang dimasak, dan pohon yang sudah hangus
    /// tidak ikut tersimpan (pohon di dalam radius api akan menyala lagi setelah memuat).
    /// </summary>
    public class GameSaveSystem : MonoBehaviour
    {
        public static GameSaveSystem Instance { get; private set; }

        private const string SaveKey = "Karhutla.GameSave.v1";

        [Header("Penyimpanan")]
        public bool autoLoadOnStart = true;
        public bool autoSave = true;
        [Tooltip("F5 = simpan, F9 = muat, F10 = hapus data simpan (mulai baru).")]
        public bool saveKeysEnabled = true;

        public static bool HasSave { get { return PlayerPrefs.HasKey(SaveKey); } }

        private bool dirty;
        private bool blocked;   // setelah F10: jangan menulis data simpan lagi pada sesi Play ini
        private bool loading;

        private void Awake() { Instance = this; }

        private IEnumerator Start()
        {
            var time = TimeManager.Instance;
            if (time != null)
            {
                time.OnDayChanged += MarkDirty;
                time.OnPeriodChanged += MarkDirty2;
                time.OnTimeSkipped += MarkDirty3;
            }

            // Tunggu semua sistem selesai Start() (NPC terdaftar, dll.) sebelum memuat.
            yield return null;
            yield return null;
            if (autoLoadOnStart && HasSave) Load(true);
        }

        private void MarkDirty(int day) { dirty = true; }
        private void MarkDirty2(TimeOfDay p) { dirty = true; }
        private void MarkDirty3(int a, int b) { dirty = true; }

        private void OnDestroy()
        {
            var time = TimeManager.Instance;
            if (time != null)
            {
                time.OnDayChanged -= MarkDirty;
                time.OnPeriodChanged -= MarkDirty2;
                time.OnTimeSkipped -= MarkDirty3;
            }
            if (Instance == this) Instance = null;
        }

        private void OnApplicationQuit()
        {
            if (autoSave) Save(true);
        }

        private void Update()
        {
            if (!saveKeysEnabled || GameState.InputLocked) return;
            if (GameInput.SaveGamePressed) Save(false);
            else if (GameInput.LoadGamePressed) Load(false);
            else if (GameInput.NewGamePressed) NewGame();
        }

        private void LateUpdate()
        {
            // Simpan di akhir frame supaya semua handler hari/periode sudah selesai.
            if (dirty && autoSave)
            {
                dirty = false;
                Save(true);
            }
        }

        private static bool WarungBusy()
        {
            return WarungSystem.Instance != null && WarungSystem.Instance.IsOpen;
        }

        // ================= Simpan =================

        public bool Save(bool silent)
        {
            if (blocked || loading) return false;
            if (WarungBusy())
            {
                if (!silent) Notifier.Show("Tidak bisa menyimpan saat sesi warung berlangsung. Selesaikan sesi dulu.");
                return false;
            }

            var time = TimeManager.Instance;
            var inv = Inventory.Instance;
            if (time == null || inv == null) return false;

            try
            {
                var d = new GameSaveData
                {
                    day = time.Day, period = (int)time.Period, unitsRemaining = time.UnitsRemaining, minutesIntoSection = time.MinutesIntoSection,
                    totalMinutes = time.TotalMinutes, daysPlayed = time.DaysPlayed, daysSkipped = time.DaysSkipped, finalDayEnded = time.FinalDayEnded
                };

                var disaster = DisasterSystem.Instance;
                if (disaster != null)
                {
                    d.fireStarted = disaster.FireStarted;
                    d.fireByDebug = disaster.FireStartedByDebug;
                    d.fireStartMinute = disaster.FireStartMinute;
                    d.fireRadiusBonus = disaster.DebugRadiusBonus;
                }

                var survival = SurvivalSystem.Instance;
                if (survival != null)
                {
                    d.hunger = survival.Hunger; d.thirst = survival.Thirst; d.fatigue = survival.Fatigue;
                    d.smoke = survival.SmokeExposure; d.health = survival.Health; d.maskMinutes = survival.MaskMinutesLeft;
                }

                d.money = inv.Money;
                Pack(inv.Container, d.inventory);
                var storage = WarungStorage.Instance;
                if (storage != null)
                {
                    Pack(storage.Gudang, d.gudang);
                    Pack(storage.Fridge, d.kulkas);
                }

                var warung = WarungSystem.Instance;
                if (warung != null)
                {
                    d.sundayChoice = (int)warung.SundayDecision;
                    d.sessionDoneDay = warung.SessionDoneDay;
                }

                var player = PlayerController.Instance;
                if (player != null)
                {
                    Vector3 p = player.transform.position;
                    d.px = p.x; d.py = p.y; d.pz = p.z;
                }

                foreach (var trader in FindObjectsByType<BarterTrader>(FindObjectsSortMode.None)) d.traders.Add(trader.ExportState());

                // Resep masakan terbuka
                if (RecipeManager.Instance != null) d.unlockedRecipes = RecipeManager.Instance.GetSavedUnlockedIds();

                // Penyelidikan korupsi
                if (InvestigationSystem.Instance != null)
                {
                    d.collectedClues = InvestigationSystem.Instance.GetSavedClues();
                    d.corruptionExposed = InvestigationSystem.Instance.CorruptionExposed;
                }

                // Pemadaman api & sekat bakar
                if (FirefightingMission.Instance != null)
                {
                    d.completedFirebreaks = FirefightingMission.Instance.CompletedFirebreaks;
                    d.extinguishedTrees = FirefightingMission.Instance.ExtinguishedTreesCount;
                    d.fireContained = FirefightingMission.Instance.IsFireContained;
                }

                // Kebangkrutan warung
                if (BankruptcySystem.Instance != null)
                {
                    d.bankruptcyCriticalDays = BankruptcySystem.Instance.ConsecutiveCriticalDays;
                    d.inBankruptcyCrisis = BankruptcySystem.Instance.IsInFinancialCrisis;
                }

                // Bahaya Lingkungan (Tanah Gambut Labil & Sumber Air)
                foreach (var hazard in UnstableGroundHazard.All)
                {
                    if (hazard != null && hazard.isStabilized)
                        d.stabilizedHazards.Add(hazard.zoneName);
                }
                foreach (var water in WaterSourceHazard.All)
                {
                    if (water != null && water.isProtected)
                        d.protectedWaterSources.Add(water.sourceName);
                }

                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
                PlayerPrefs.Save();
                if (!silent) Notifier.Show("Permainan disimpan (Hari " + d.day + ").");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Gagal menyimpan permainan: " + ex.Message);
                if (!silent) Notifier.Show("Gagal menyimpan permainan. Lihat Console.");
                return false;
            }
        }

        private static void Pack(ItemContainer container, List<SaveBatch> target)
        {
            if (container == null) return;
            foreach (var b in container.Batches) target.Add(new SaveBatch { id = b.Id, count = b.Count, age = b.Age });
        }

        private static void Unpack(List<SaveBatch> source, ItemContainer container)
        {
            if (container == null) return;
            container.Clear();
            if (source == null) return;
            foreach (var b in source) container.RestoreBatch(b.id, b.count, b.age);
        }

        // ================= Muat =================

        public bool Load(bool silent)
        {
            if (loading) return false;
            if (!HasSave)
            {
                if (!silent) Notifier.Show("Belum ada data simpan.");
                return false;
            }
            if (WarungBusy())
            {
                if (!silent) Notifier.Show("Tidak bisa memuat saat sesi warung berlangsung. Selesaikan sesi dulu.");
                return false;
            }

            GameSaveData d;
            try { d = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(SaveKey)); }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Data simpan rusak: " + ex.Message);
                d = null;
            }
            if (d == null || d.version != 1)
            {
                if (!silent) Notifier.Show("Data simpan tidak bisa dibaca.");
                return false;
            }

            var time = TimeManager.Instance;
            var inv = Inventory.Instance;
            if (time == null || inv == null) return false;

            loading = true;
            try
            {
                // 1. Waktu (tanpa memicu event hari/periode).
                time.RestoreState(d.day, (TimeOfDay)Mathf.Clamp(d.period, 0, 2), d.unitsRemaining, d.minutesIntoSection, d.totalMinutes, d.daysPlayed, d.daysSkipped, d.finalDayEnded);

                // 2. Bencana (memakai waktu yang sudah dipulihkan).
                if (DisasterTimeline.Instance != null) DisasterTimeline.Instance.Resync();
                if (DisasterSystem.Instance != null) DisasterSystem.Instance.RestoreState(d.fireStarted, d.fireByDebug, d.fireStartMinute, d.fireRadiusBonus);

                // 3. Tubuh pemain.
                if (SurvivalSystem.Instance != null) SurvivalSystem.Instance.RestoreState(d.hunger, d.thirst, d.fatigue, d.smoke, d.health, d.maskMinutes);

                // 4. Barang.
                inv.SetMoney(d.money);
                Unpack(d.inventory, inv.Container);
                if (WarungStorage.Instance != null)
                {
                    Unpack(d.gudang, WarungStorage.Instance.Gudang);
                    Unpack(d.kulkas, WarungStorage.Instance.Fridge);
                }

                // 5. Warung, laporan, penuaan makanan.
                if (WarungSystem.Instance != null) WarungSystem.Instance.RestoreState((SundayChoice)Mathf.Clamp(d.sundayChoice, 0, 2), d.sessionDoneDay);
                if (DayReportSystem.Instance != null) DayReportSystem.Instance.RestoreDay(d.day);
                if (FoodExpirySystem.Instance != null) FoodExpirySystem.Instance.ResyncWithoutAging();

                // 6. Stok barter.
                if (d.traders != null)
                {
                    foreach (var trader in FindObjectsByType<BarterTrader>(FindObjectsSortMode.None))
                    {
                        foreach (var saved in d.traders)
                        {
                            if (saved.npcId == trader.NpcId) { trader.ImportState(saved); break; }
                        }
                    }
                }

                // 7. Posisi pemain.
                if (PlayerController.Instance != null) PlayerController.Instance.TeleportTo(new Vector3(d.px, d.py, d.pz));

                // 8. Hubungan sosial (dibandingkan dengan hari yang sudah dipulihkan).
                if (RelationshipSystem.Instance != null) RelationshipSystem.Instance.ReloadFromSave();

                // 9. Resep masakan terbuka
                if (RecipeManager.Instance != null && d.unlockedRecipes != null) RecipeManager.Instance.RestoreUnlockedIds(d.unlockedRecipes);

                // 10. Penyelidikan korupsi
                if (InvestigationSystem.Instance != null) InvestigationSystem.Instance.RestoreState(d.collectedClues, d.corruptionExposed);

                // 11. Misi pemadaman api & sekat bakar
                if (FirefightingMission.Instance != null) FirefightingMission.Instance.RestoreState(d.completedFirebreaks, d.extinguishedTrees, d.fireContained);

                // 12. Sistem kebangkrutan
                if (BankruptcySystem.Instance != null) BankruptcySystem.Instance.RestoreState(d.bankruptcyCriticalDays, d.inBankruptcyCrisis);

                // 13. Bahaya Lingkungan (Tanah Gambut Labil & Sumber Air)
                if (d.stabilizedHazards != null)
                {
                    foreach (var hazard in UnstableGroundHazard.All)
                    {
                        if (hazard != null) hazard.RestoreState(d.stabilizedHazards.Contains(hazard.zoneName));
                    }
                }
                if (d.protectedWaterSources != null)
                {
                    foreach (var water in WaterSourceHazard.All)
                    {
                        if (water != null) water.RestoreState(d.protectedWaterSources.Contains(water.sourceName));
                    }
                }

                if (!silent) Notifier.Show("Permainan dimuat (Hari " + d.day + ").");
                else Notifier.Show("Melanjutkan permainan: Hari " + d.day + ". (F10 = mulai baru)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Karhutla] Gagal memuat permainan: " + ex.Message);
                if (!silent) Notifier.Show("Gagal memuat permainan. Lihat Console.");
                return false;
            }
            finally
            {
                loading = false;
            }
        }

        // ================= Mulai baru =================

        /// <summary>Hapus semua data simpan. Untuk benar-benar mulai dari awal, hentikan lalu jalankan Play lagi.</summary>
        public void NewGame()
        {
            ClearSavedData();
            blocked = true; // jangan menulis ulang data simpan pada sesi Play ini
            if (RelationshipSystem.Instance != null)
            {
                RelationshipSystem.Instance.saveToPlayerPrefs = false;
                RelationshipSystem.Instance.ResetAll();
            }
            CommunityEventManager.ClearSavedData();
            Notifier.Show("Data simpan dihapus. Hentikan Play lalu Play lagi untuk mulai dari awal.");
        }

        public static void ClearSavedData()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            RelationshipSystem.ClearSavedData();
            CommunityEventManager.ClearSavedData();
            PlayerPrefs.Save();
        }
    }
}
