using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Meja pelanggan (dummy: Cylinder). Pelanggan duduk di meja, pemain mencatat pesanan dan mengantar makanan ke sini.
    ///  E : catat pesanan (pelanggan baru datang) / antar makanan yang dipesan (jika sedang dibawa).
    /// </summary>
    public class CustomerTable : InteractableBase
    {
        public static readonly List<CustomerTable> All = new List<CustomerTable>();

        public int tableNumber = 1;
        [Tooltip("Titik tempat pelanggan berdiri/duduk. Jika kosong, dipakai posisi meja digeser sedikit.")]
        public Transform seatPoint;

        public Customer Occupant { get; set; }

        public Vector3 SeatPosition
        {
            get
            {
                if (seatPoint != null) return seatPoint.position;
                return transform.position + new Vector3(0f, 0f, -1.6f);
            }
        }

        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public override string GetPrompt()
        {
            var w = WarungSystem.Instance;
            string name = "Meja " + tableNumber;
            if (w == null) return name;
            return w.GetTablePrompt(this, name);
        }

        public override void Interact()
        {
            var w = WarungSystem.Instance;
            if (w != null) w.InteractTable(this);
        }
    }
}
