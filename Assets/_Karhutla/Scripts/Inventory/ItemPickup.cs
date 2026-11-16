using UnityEngine;

namespace Karhutla
{
    /// <summary>Item di dunia. Tekan E untuk mengambil dan memasukkannya ke Inventory (jika berat masih muat).</summary>
    public class ItemPickup : InteractableBase
    {
        public string itemId = ItemDatabase.Nasi;
        public int amount = 1;

        private float baseY;
        private float phase;

        private void Start()
        {
            baseY = transform.position.y;
            phase = Random.value * 6.28f;
        }

        private void Update()
        {
            // Animasi melayang dan berputar agar item mudah terlihat.
            Vector3 p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 2f + phase) * 0.1f;
            transform.position = p;
            transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
        }

        public override string GetPrompt()
        {
            var def = ItemDatabase.Get(itemId);
            string name = def != null ? def.DisplayName : itemId;
            string weight = def != null ? " (" + (def.Weight * amount).ToString("0.##") + " kg)" : "";
            return "Ambil " + name + (amount > 1 ? " x" + amount : "") + weight;
        }

        public override void Interact()
        {
            var inventory = Inventory.Instance;
            if (inventory == null) return;

            var def = ItemDatabase.Get(itemId);
            if (def == null) return;

            if (!inventory.CanCarry(itemId, amount))
            {
                Notifier.Show("Terlalu berat! Beban " + inventory.CurrentWeight.ToString("0.#") + "/" + inventory.MaxWeight.ToString("0.#")
                              + " kg. Simpan barang ke gudang warung dulu.");
                return;
            }

            if (inventory.Add(itemId, amount))
            {
                Notifier.Show("Mengambil " + def.DisplayName + (amount > 1 ? " x" + amount : ""));
                Destroy(gameObject);
            }
        }
    }
}
