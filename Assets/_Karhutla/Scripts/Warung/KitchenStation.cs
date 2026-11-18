using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Area dapur (dummy: Cube "kompor"). Memasak hidangan dari bahan makanan.
    ///  E : mulai memasak menu terpilih (bahan diambil dari inventaris, gudang, lalu kulkas — hanya yang masih layak)
    ///      / ambil hasil masakan yang sudah matang ke inventaris (dibatasi berat bawaan).
    ///  Q : ganti menu.
    /// Memasak memakan waktu nyata (cookSeconds) agar pemain harus mengatur giliran seperti di Diner Dash.
    /// Dapat dipakai sebelum warung dibuka (persiapan) maupun saat warung buka.
    /// Resep dan jumlah porsi adalah NILAI SEMENTARA (lihat RecipeDatabase).
    /// </summary>
    public class KitchenStation : InteractableBase
    {
        private enum KitchenState { Idle, Cooking, Ready }

        [Tooltip("Lama memasak (detik nyata). NILAI SEMENTARA.")]
        public float cookSeconds = 4f;

        private static readonly System.Collections.Generic.List<KitchenStation> all = new System.Collections.Generic.List<KitchenStation>();

        /// <summary>Apakah ada dapur yang sedang memasak atau menyimpan hasil masakan (belum diambil) untuk item ini?</summary>
        public static bool IsPreparing(string itemId)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var k = all[i];
                if (k != null && k.state != KitchenState.Idle && k.cooking != null && k.cooking.OutputId == itemId) return true;
            }
            return false;
        }

        private void OnEnable() { all.Add(this); }
        private void OnDisable() { all.Remove(this); }

        private KitchenState state = KitchenState.Idle;
        private int recipeIndex;
        private Recipe cooking;
        private float cookLeft;
        private int readyCount;

        private System.Collections.Generic.List<Recipe> AvailableRecipes
        {
            get
            {
                if (RecipeManager.Instance != null)
                {
                    var list = RecipeManager.Instance.GetUnlockedRecipes();
                    if (list != null && list.Count > 0) return list;
                }
                return new System.Collections.Generic.List<Recipe>(RecipeDatabase.All);
            }
        }

        private Recipe Selected
        {
            get
            {
                var list = AvailableRecipes;
                if (list.Count == 0) return RecipeDatabase.All[0];
                return list[recipeIndex % list.Count];
            }
        }

        private void Update()
        {
            if (state != KitchenState.Cooking || GameState.InputLocked) return;

            cookLeft -= Time.deltaTime;
            if (cookLeft <= 0f)
            {
                state = KitchenState.Ready;
                readyCount = cooking.OutputCount;
                DayReportSystem.LogCooked(cooking.OutputId, cooking.OutputCount);
                Notifier.Show(cooking.Name + " sudah matang! Ambil di dapur.");
            }
        }

        public override string GetPrompt()
        {
            switch (state)
            {
                case KitchenState.Cooking:
                    return "Memasak " + cooking.Name + "... (" + Mathf.CeilToInt(Mathf.Max(0f, cookLeft)) + " dtk)";
                case KitchenState.Ready:
                    return "Ambil " + readyCount + " " + cooking.Name + " yang matang";
                default:
                    var r = Selected;
                    return "Masak " + r.Name + " (" + r.IngredientText + " -> " + r.OutputCount + " porsi)";
            }
        }

        public override string GetSecondaryPrompt()
        {
            if (state != KitchenState.Idle) return null;
            var list = AvailableRecipes;
            if (list.Count <= 1) return null;
            var next = list[(recipeIndex + 1) % list.Count];
            return "Ganti menu (berikutnya: " + next.Name + ") [" + (recipeIndex % list.Count + 1) + "/" + list.Count + "]";
        }

        public override void InteractSecondary()
        {
            if (state != KitchenState.Idle) return;
            var list = AvailableRecipes;
            if (list.Count <= 1) return;
            recipeIndex = (recipeIndex + 1) % list.Count;
            Notifier.Show("Menu dapur: " + Selected.Name + " (" + Selected.IngredientText + "). [" + (recipeIndex + 1) + "/" + list.Count + " resep terbuka]");
        }

        public override void Interact()
        {
            switch (state)
            {
                case KitchenState.Cooking:
                    Notifier.Show("Masih memasak " + cooking.Name + ".");
                    break;
                case KitchenState.Ready:
                    Collect();
                    break;
                default:
                    StartCooking();
                    break;
            }
        }

        private void StartCooking()
        {
            var recipe = Selected;

            // Periksa semua bahan dulu (layak = belum basi).
            for (int i = 0; i < recipe.IngredientIds.Length; i++)
            {
                string id = recipe.IngredientIds[i];
                int need = recipe.IngredientCounts[i];
                if (WarungStorage.TotalFresh(id) < need)
                {
                    var def = ItemDatabase.Get(id);
                    string name = def != null ? def.DisplayName : id;
                    Notifier.Show("Bahan kurang: " + name + " (butuh " + need + ", layak " + WarungStorage.TotalFresh(id) + "). Beli di Pasar atau buang yang basi.");
                    return;
                }
            }

            for (int i = 0; i < recipe.IngredientIds.Length; i++)
            {
                WarungStorage.ConsumeFresh(recipe.IngredientIds[i], recipe.IngredientCounts[i]);
            }

            cooking = recipe;
            cookLeft = cookSeconds;
            state = KitchenState.Cooking;
            Notifier.Show("Mulai memasak " + recipe.Name + ".");
        }

        private void Collect()
        {
            var inv = Inventory.Instance;
            if (inv == null) return;

            int moved = 0;
            while (readyCount > 0 && inv.CanCarry(cooking.OutputId, 1))
            {
                if (!inv.Add(cooking.OutputId, 1)) break;
                readyCount--;
                moved++;
            }

            if (moved > 0) Notifier.Show("Mengambil " + moved + " " + cooking.Name + ".");
            if (readyCount > 0)
            {
                Notifier.Show("Beban penuh! " + readyCount + " porsi masih di dapur. Simpan barang ke gudang/kulkas, lalu ambil lagi.");
            }
            else
            {
                state = KitchenState.Idle;
            }
        }
    }
}
