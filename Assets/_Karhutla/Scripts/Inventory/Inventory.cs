using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Inventory pemain: item (di dalam ItemContainer berbasis BERAT) dan uang.
    ///
    /// Tahap 2: kapasitas dihitung dari berat (maxCarryWeight, NILAI SEMENTARA). Pemain tidak bisa mengambil item
    /// bila total berat melewati kapasitas. Item punya umur (kedaluwarsa); item basi tidak bisa dipakai/disajikan
    /// dan bisa dibuang dengan tombol G.
    /// Tombol angka 1-9 memakai item sesuai nomor di daftar inventory (per jenis item).
    /// Inventory ini TERPISAH dari penyimpanan warung (WarungStorage).
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        public int startingMoney = 50000;
        [Tooltip("Batas berat bawaan pemain dalam kg (NILAI SEMENTARA, GDD belum menentukan).")]
        public float maxCarryWeight = 15f;
        public int Money { get; private set; }

        /// <summary>Dipicu setiap kali isi inventory atau uang berubah. UI berlangganan ke sini.</summary>
        public event Action Changed;

        public ItemContainer Container { get; private set; }

        public float MaxWeight { get { return maxCarryWeight; } }
        public float CurrentWeight { get { return Container.TotalWeight; } }

        private void Awake()
        {
            Instance = this;
            Money = startingMoney;
            Container = new ItemContainer("Inventaris", maxCarryWeight);
            Container.Changed += RaiseChanged;
            Container.Add(ItemDatabase.Air, 1); // bekal awal (dummy)
        }

        private void OnDestroy()
        {
            if (Container != null) Container.Changed -= RaiseChanged;
            if (Instance == this) Instance = null;
        }

        private void RaiseChanged()
        {
            if (Changed != null) Changed();
        }

        private void Update()
        {
            Container.MaxWeight = maxCarryWeight;
            if (GameState.InputLocked) return;

            if (GameInput.DiscardPressed) DiscardExpired();

            for (int n = 1; n <= 9; n++)
            {
                if (!GameInput.NumberPressed(n)) continue;
                var owned = GetOwnedItems();
                if (n <= owned.Count) Use(owned[n - 1].Id);
            }
        }

        public int Count(string id) { return Container.Count(id); }
        public int CountFresh(string id) { return Container.CountFresh(id); }

        /// <summary>Apakah item sebanyak ini masih muat dalam batas berat bawaan?</summary>
        public bool CanCarry(string id, int amount = 1)
        {
            var def = ItemDatabase.Get(id);
            if (def == null || amount <= 0) return false;
            return Container.TotalWeight + def.Weight * amount <= maxCarryWeight + 0.001f;
        }

        /// <summary>Tambah item. False jika id tidak dikenal atau melewati kapasitas berat (tidak ada yang ditambahkan).</summary>
        public bool Add(string id, int amount = 1, float age = 0f)
        {
            if (!CanCarry(id, amount)) return false;
            Container.MaxWeight = maxCarryWeight;
            return Container.Add(id, amount, age);
        }

        /// <summary>Kurangi item yang masih layak. False jika jumlah layak tidak cukup.</summary>
        public bool Remove(string id, int amount = 1)
        {
            return Container.RemoveFresh(id, amount);
        }

        /// <summary>Atur uang langsung (dipakai saat memuat data simpan).</summary>
        public void SetMoney(int amount)
        {
            Money = Mathf.Max(0, amount);
            RaiseChanged();
        }

        public void AddMoney(int amount)
        {
            Money += amount;
            RaiseChanged();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Money < amount) return false;
            Money -= amount;
            RaiseChanged();
            return true;
        }

        /// <summary>Jenis item yang dimiliki, urutannya tetap mengikuti ItemDatabase.</summary>
        public List<ItemDefinition> GetOwnedItems()
        {
            var result = new List<ItemDefinition>();
            foreach (var def in ItemDatabase.All)
            {
                if (Container.Count(def.Id) > 0) result.Add(def);
            }
            return result;
        }

        public bool Use(string id)
        {
            var def = ItemDatabase.Get(id);
            var survival = SurvivalSystem.Instance;
            if (def == null || Count(id) <= 0) return false;

            if (!def.Usable)
            {
                Notifier.Show(def.DisplayName + " tidak bisa dipakai langsung.");
                return false;
            }
            if (survival == null) return false;

            if (CountFresh(id) <= 0)
            {
                Notifier.Show(def.DisplayName + " sudah basi dan tidak bisa dipakai. Tekan G untuk membuangnya.");
                return false;
            }

            survival.ApplyItem(def);
            Container.RemoveFresh(id, 1);
            Notifier.Show("Memakai " + def.DisplayName + ": " + def.EffectText);
            return true;
        }

        /// <summary>Buang semua item basi di inventaris pemain.</summary>
        public int DiscardExpired()
        {
            int removed = Container.DiscardExpired();
            if (removed > 0)
            {
                DayReportSystem.LogDiscarded(removed);
                Notifier.Show("Membuang " + removed + " item basi dari inventaris.");
            }
            else
            {
                Notifier.Show("Tidak ada item basi di inventaris.");
            }
            return removed;
        }
    }
}
