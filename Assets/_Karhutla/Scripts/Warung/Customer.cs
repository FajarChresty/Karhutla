using UnityEngine;

namespace Karhutla
{
    public enum CustomerState
    {
        Arriving,        // berjalan ke meja
        WaitingToOrder,  // menunggu pesanannya dicatat pemain
        WaitingForFood,  // pesanan sudah dicatat, menunggu makanan diantar
        Eating,          // makanan sudah diterima
        Leaving
    }

    /// <summary>
    /// Pelanggan dummy (capsule). Alur: datang -> duduk di meja -> menunggu dicatat -> menunggu makanan -> makan -> bayar -> pergi.
    /// Kesabaran habis pada fase menunggu = pelanggan pergi kecewa (pesanan gagal).
    /// Semua timer berhenti saat input terkunci (dialog/menu), konsisten dengan waktu permainan.
    /// Tidak memakai NavMesh agar tetap sederhana (jalan lurus, tanpa collider).
    /// </summary>
    public class Customer : MonoBehaviour
    {
        public string OrderId { get; private set; }
        public CustomerState State { get; private set; }
        public CustomerTable Table { get; private set; }
        public float PatienceLeft { get; private set; }
        public float TotalWaited { get; private set; }

        private WarungSystem owner;
        private Vector3 target;
        private float speed = 2.6f;
        private float patienceBudget = 1f;
        private float eatLeft;
        private WorldLabel label;
        private float labelTimer;
        private string lastLabel = "";
        private bool lockedLabelText;

        public static Customer Create(Vector3 position, Material baseMaterial, string orderId, CustomerTable table, WarungSystem owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Pelanggan";
            Destroy(go.GetComponent<Collider>()); // tanpa collider agar tidak menghalangi pemain
            go.transform.localScale = Vector3.one * 0.9f;
            go.transform.position = new Vector3(position.x, 0.9f, position.z);

            var rend = go.GetComponent<Renderer>();
            if (baseMaterial != null)
            {
                var m = new Material(baseMaterial);
                m.color = Color.HSVToRGB(Random.value, 0.45f, 0.9f);
                rend.material = m;
            }

            var customer = go.AddComponent<Customer>();
            customer.OrderId = orderId;
            customer.Table = table;
            customer.owner = owner;
            customer.State = CustomerState.Arriving;
            customer.patienceBudget = Mathf.Max(1f, owner.orderPatience + owner.foodPatience);
            customer.target = new Vector3(table.SeatPosition.x, go.transform.position.y, table.SeatPosition.z);
            customer.label = WorldLabel.Create(go.transform, new Vector3(0f, 1.7f, 0f), "Mau makan...", Color.white, 0.6f);
            return customer;
        }

        /// <summary>Nama pesanan yang ditampilkan (setelah dicatat).</summary>
        public string OrderName
        {
            get
            {
                var def = ItemDatabase.Get(OrderId);
                return def != null ? def.DisplayName : OrderId;
            }
        }

        public string StatusText
        {
            get
            {
                int secs = Mathf.CeilToInt(Mathf.Max(0f, PatienceLeft));
                switch (State)
                {
                    case CustomerState.Arriving: return "Mau makan...";
                    case CustomerState.WaitingToOrder: return "Mau pesan! (" + secs + ")";
                    case CustomerState.WaitingForFood: return OrderName + " (" + secs + ")";
                    case CustomerState.Eating: return "Makan...";
                }
                return "";
            }
        }

        public float PatienceFraction
        {
            get
            {
                float max = State == CustomerState.WaitingToOrder ? owner.orderPatience : owner.foodPatience;
                return max <= 0f ? 0f : Mathf.Clamp01(PatienceLeft / max);
            }
        }

        // ================= Dipanggil WarungSystem =================

        public void TakeOrder()
        {
            if (State != CustomerState.WaitingToOrder) return;
            State = CustomerState.WaitingForFood;
            PatienceLeft = owner.foodPatience;
        }

        public void Serve()
        {
            if (State != CustomerState.WaitingForFood) return;
            State = CustomerState.Eating;
            eatLeft = owner.eatSeconds;
        }

        /// <summary>0..100. 100 = dilayani cepat, turun sampai worstSatisfaction bila hampir kehabisan kesabaran.</summary>
        public float ComputeSatisfaction(float worstSatisfaction)
        {
            float fraction = Mathf.Clamp01(TotalWaited / patienceBudget);
            return Mathf.Lerp(100f, worstSatisfaction, fraction);
        }

        public void Leave(Vector3 exitPosition, bool happy, string text)
        {
            if (Table != null && Table.Occupant == this) Table.Occupant = null;
            Table = null;
            State = CustomerState.Leaving;
            target = new Vector3(exitPosition.x, transform.position.y, exitPosition.z);

            lockedLabelText = true;
            if (label != null)
            {
                label.color = happy ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                label.SetText(text);
            }
        }

        // ================= Update =================

        private void Update()
        {
            if (GameState.InputLocked) return;

            Vector3 pos = transform.position;
            Vector3 toTarget = target - pos;
            toTarget.y = 0f;

            if (toTarget.magnitude > 0.05f)
            {
                transform.position = Vector3.MoveTowards(pos, target, speed * Time.deltaTime);
                if (toTarget.sqrMagnitude > 0.01f) transform.forward = toTarget.normalized;
                return;
            }

            switch (State)
            {
                case CustomerState.Leaving:
                    Destroy(gameObject);
                    return;

                case CustomerState.Arriving:
                    State = CustomerState.WaitingToOrder;
                    PatienceLeft = owner.orderPatience;
                    owner.CustomerSeated(this);
                    break;

                case CustomerState.WaitingToOrder:
                case CustomerState.WaitingForFood:
                    PatienceLeft -= Time.deltaTime;
                    TotalWaited += Time.deltaTime;
                    if (PatienceLeft <= 0f) owner.CustomerLostPatience(this);
                    break;

                case CustomerState.Eating:
                    eatLeft -= Time.deltaTime;
                    if (eatLeft <= 0f) owner.CustomerFinishedEating(this);
                    break;
            }

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label == null || lockedLabelText) return;
            labelTimer -= Time.deltaTime;
            if (labelTimer > 0f) return;
            labelTimer = 0.25f;

            string text = StatusText;
            if (text != lastLabel)
            {
                lastLabel = text;
                label.SetText(text);
            }

            bool waiting = State == CustomerState.WaitingToOrder || State == CustomerState.WaitingForFood;
            Color c = Color.white;
            if (waiting) c = PatienceFraction < 0.3f ? new Color(1f, 0.45f, 0.4f) : (State == CustomerState.WaitingToOrder ? new Color(1f, 0.95f, 0.5f) : Color.white);
            label.color = c;
            label.Refresh();
        }
    }
}
