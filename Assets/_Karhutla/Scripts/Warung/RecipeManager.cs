using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Pengelola pembukaan resep masakan warung bertahap (GDD Bab 5.4).
    ///
    /// Aturan GDD:
    ///  - Menu pembuka memiliki ~5 hidangan (Nasi Bungkus, Kopi Hitam, Teh Hangat, Pisang Goreng, Sayur Asem).
    ///  - Bertambah bertahap hingga ~12 hidangan seiring berjalannya hari, kepuasan pelanggan, dan hubungan warga.
    ///
    /// Menyediakan daftar resep aktif untuk KitchenStation dan pesanan pelanggan di WarungSystem.
    /// </summary>
    public class RecipeManager : MonoBehaviour
    {
        public static RecipeManager Instance { get; private set; }

        private readonly HashSet<string> unlockedIds = new HashSet<string>();

        public event Action<Recipe> OnRecipeUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeDefaultRecipes();
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged += CheckAutoUnlockByDay;
                TimeManager.Instance.OnTimeSkipped += (from, to) => CheckAutoUnlockByDay(to);
            }

            // Periksa unlock hari pertama
            int currentDay = TimeManager.Instance != null ? TimeManager.Instance.Day : 1;
            CheckAutoUnlockByDay(currentDay);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= CheckAutoUnlockByDay;
            }
            if (Instance == this) Instance = null;
        }

        private void InitializeDefaultRecipes()
        {
            // Buka 5 menu pembuka awal (UnlockDay == 0)
            foreach (var recipe in RecipeDatabase.All)
            {
                if (recipe.UnlockDay <= 0)
                {
                    unlockedIds.Add(recipe.Id);
                }
            }
        }

        /// <summary>Periksa pembukaan resep otomatis berdasarkan hari kalender.</summary>
        public void CheckAutoUnlockByDay(int currentDay)
        {
            foreach (var recipe in RecipeDatabase.All)
            {
                if (recipe.UnlockDay > 0 && currentDay >= recipe.UnlockDay)
                {
                    if (!unlockedIds.Contains(recipe.Id))
                    {
                        UnlockRecipe(recipe.Id, "Mencapai Hari ke-" + currentDay);
                    }
                }
            }
        }

        /// <summary>Buka resep secara manual (misal hadiah NPC atau dibeli di pasar).</summary>
        public bool UnlockRecipe(string recipeId, string reason = "")
        {
            var recipe = RecipeDatabase.Get(recipeId);
            if (recipe == null) return false;

            if (unlockedIds.Add(recipe.Id))
            {
                string note = string.IsNullOrEmpty(reason) ? "" : " (" + reason + ")";
                Notifier.Show("Resep Baru Terbuka: " + recipe.Name + "!" + note + "\nSekarang bisa dimasak di Dapur.");
                OnRecipeUnlocked?.Invoke(recipe);
                return true;
            }
            return false;
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            return unlockedIds.Contains(recipeId);
        }

        /// <summary>Daftar seluruh resep masakan yang saat ini sudah terbuka.</summary>
        public List<Recipe> GetUnlockedRecipes()
        {
            var list = new List<Recipe>();
            foreach (var recipe in RecipeDatabase.All)
            {
                if (unlockedIds.Contains(recipe.Id))
                {
                    list.Add(recipe);
                }
            }
            if (list.Count == 0 && RecipeDatabase.All.Length > 0)
            {
                list.Add(RecipeDatabase.All[0]);
            }
            return list;
        }

        /// <summary>Daftar ID item pesanan yang bisa dipesan pelanggan warung saat ini.</summary>
        public List<string> GetUnlockedOrderableItems()
        {
            var items = new List<string>();
            foreach (var r in GetUnlockedRecipes())
            {
                var def = ItemDatabase.Get(r.OutputId);
                if (def != null && def.Orderable && !items.Contains(r.OutputId))
                {
                    items.Add(r.OutputId);
                }
            }

            // Air mineral selalu ada sebagai minuman standar warung
            if (!items.Contains(ItemDatabase.Air)) items.Add(ItemDatabase.Air);

            return items;
        }

        // Simpan & Muat
        public List<string> GetSavedUnlockedIds()
        {
            return new List<string>(unlockedIds);
        }

        public void RestoreUnlockedIds(List<string> ids)
        {
            if (ids == null) return;
            unlockedIds.Clear();
            InitializeDefaultRecipes();
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id)) unlockedIds.Add(id);
            }
        }
    }
}
