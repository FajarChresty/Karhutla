using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>Pilihan pemain untuk hari Minggu (GDD 4.2: hari Minggu spesial, warung boleh tidak dibuka).</summary>
    public enum SundayChoice { Belum, Buka, Libur }

    /// <summary>
    /// Operasional warung gaya Diner Dash (Tahap 2). Menggantikan transaksi instan di kasir.
    ///
    /// Alur:
    ///  1. Sebelum buka, pemain menyiapkan hidangan di Dapur (KitchenStation) dan mengatur Gudang/Kulkas.
    ///  2. Buka warung lewat papan (hanya Pagi, tidak pada hari Minggu yang dipilih libur, satu sesi per hari).
    ///  3. Pelanggan datang, duduk di meja (CustomerTable), pemain berjalan ke meja untuk MENCATAT pesanan.
    ///  4. Pemain menyiapkan/mengambil makanan (masak di dapur, atau ambil dari Gudang/Kulkas dengan Q) dan
    ///     MENGANTAR ke meja yang memesan (makanan dibawa di inventaris, jadi berat dihitung).
    ///  5. Pelanggan makan lalu membayar -> pendapatan masuk, kepuasan dihitung dari lama menunggu.
    ///  6. Sesi berakhir (waktu sesi habis dan pelanggan selesai, atau pemain menutup) -> memakai waktu sesi
    ///     (TimeManager.costs.warungSessionSections) dan periode berpindah.
    ///
    /// SEMUA ANGKA (durasi sesi, kesabaran, harga, kepuasan) ADALAH NILAI SEMENTARA, bisa diubah di Inspector.
    /// GDD belum menjelaskan detail operasional warung.
    /// </summary>
    public class WarungSystem : MonoBehaviour
    {
        public static WarungSystem Instance { get; private set; }

        [Header("Referensi (diisi oleh Scene Builder)")]
        [Tooltip("Titik pelanggan muncul dan pergi.")]
        public Transform spawnPoint;
        public Material customerMaterial;

        [Header("Sesi warung (NILAI SEMENTARA, detik nyata)")]
        public float sessionDurationSeconds = 150f;
        public float baseSpawnInterval = 12f;
        [Tooltip("Batas pelanggan per sesi. 0 = tanpa batas (dibatasi jumlah meja).")]
        public int maxCustomersPerSession = 0;
        [Tooltip("Kesabaran menunggu pesanan dicatat.")]
        public float orderPatience = 25f;
        [Tooltip("Kesabaran menunggu makanan diantar setelah dicatat.")]
        public float foodPatience = 50f;
        public float eatSeconds = 4f;
        [Tooltip("GDD 4.2: warung normalnya beroperasi pada Pagi. Matikan hanya untuk eksperimen/debug.")]
        public bool onlyOpenInMorning = true;
        [Tooltip("Menutup warung lebih awal tetap memakai sesi waktu? (GDD belum menjelaskan)")]
        public bool earlyCloseSpendsTime = true;

        [Header("Kepuasan pelanggan (NILAI SEMENTARA)")]
        [Tooltip("Skor kepuasan terendah untuk pelanggan yang tetap dilayani (dilayani di detik-detik terakhir kesabaran).")]
        [Range(0f, 100f)] public float worstServedSatisfaction = 40f;
        [Tooltip("Seberapa besar kepuasan keseluruhan memengaruhi jumlah pelanggan. 0 = tidak ada pengaruh (default, GDD belum menentukan).")]
        [Range(0f, 1f)] public float demandInfluence = 0f;

        public bool IsOpen { get; private set; }
        public float SessionTimeLeft { get; private set; }
        public SundayChoice SundayDecision { get; private set; }

        /// <summary>Kepuasan keseluruhan 0..100 (rata-rata bergerak seluruh pelanggan, termasuk antarhari).</summary>
        public float OverallSatisfaction { get; private set; }
        public int RatedCustomers { get; private set; }

        public IReadOnlyList<Customer> ActiveCustomers { get { return activeCustomers; } }
        public int ActiveCount { get { return activeCustomers.Count; } }

        public int TodayIncome { get { return DayReportSystem.Instance != null ? DayReportSystem.Instance.Current.Income : 0; } }
        public int TodayServed { get { return DayReportSystem.Instance != null ? DayReportSystem.Instance.Current.Served : 0; } }
        public int TodayFailed { get { return DayReportSystem.Instance != null ? DayReportSystem.Instance.Current.Failed : 0; } }

        private readonly List<Customer> activeCustomers = new List<Customer>();
        private float spawnTimer;
        private int customersThisSession;
        private int sessionDoneDay = -1;

        private void Awake() { Instance = this; }

        private void Start()
        {
            var time = TimeManager.Instance;
            if (time == null) return;

            time.OnDayChanged += HandleNewDay;
            time.OnPeriodChanged += HandlePeriodChanged;
            time.OnTimeSkipped += HandleTimeSkipped;

            if (time.IsSundayToday) PromptSunday();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayChanged -= HandleNewDay;
                TimeManager.Instance.OnPeriodChanged -= HandlePeriodChanged;
                TimeManager.Instance.OnTimeSkipped -= HandleTimeSkipped;
            }
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsOpen || GameState.InputLocked) return;

            activeCustomers.RemoveAll(c => c == null);

            if (SessionTimeLeft > 0f)
            {
                SessionTimeLeft -= Time.deltaTime;
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f && SessionTimeLeft > 0f)
                {
                    spawnTimer = baseSpawnInterval / Mathf.Max(0.05f, CustomerRateMultiplier()) * Random.Range(0.7f, 1.3f);
                    TrySpawnCustomer();
                }
            }

            if (SessionTimeLeft <= 0f)
            {
                SessionTimeLeft = 0f;
                if (activeCustomers.Count == 0) CloseSession("Sesi warung selesai.", true);
            }
        }

        // ================= Buka / tutup =================

        /// <summary>Alasan warung belum bisa dibuka sekarang, atau null jika boleh dibuka.</summary>
        public string OpenBlockReason()
        {
            var time = TimeManager.Instance;
            if (time == null) return null;

            if (time.FinalDayEnded) return "Permainan sudah berakhir.";
            if (onlyOpenInMorning && time.Period != TimeOfDay.Pagi) return "Warung hanya buka pada pagi hari.";
            if (sessionDoneDay == time.Day) return "Sesi warung hari ini sudah selesai.";
            if (time.IsSundayToday)
            {
                if (SundayDecision == SundayChoice.Belum) return "Hari Minggu: pilih dulu buka atau libur.";
                if (SundayDecision == SundayChoice.Libur) return "Warung libur hari Minggu (pilihan pemain).";
            }
            return null;
        }

        public bool CanOpenNow { get { return OpenBlockReason() == null; } }

        public string GetSignPrompt()
        {
            if (IsOpen) return "Tutup Warung (mengakhiri sesi)";
            var time = TimeManager.Instance;
            if (time != null && time.IsSundayToday && !time.FinalDayEnded && sessionDoneDay != time.Day && SundayDecision != SundayChoice.Buka
                && (!onlyOpenInMorning || time.Period == TimeOfDay.Pagi))
            {
                return SundayDecision == SundayChoice.Libur ? "Hari Minggu: warung libur (ubah pilihan?)" : "Hari Minggu: pilih buka atau libur";
            }
            string reason = OpenBlockReason();
            return reason == null ? "Buka Warung" : "Warung: " + reason;
        }

        public void InteractSign()
        {
            if (IsOpen)
            {
                CloseSession("Warung ditutup.", earlyCloseSpendsTime);
                return;
            }

            var time = TimeManager.Instance;
            if (time != null && time.IsSundayToday && !time.FinalDayEnded && sessionDoneDay != time.Day && SundayDecision != SundayChoice.Buka
                && (!onlyOpenInMorning || time.Period == TimeOfDay.Pagi))
            {
                PromptSunday();
                return;
            }

            TryOpen();
        }

        public void TryOpen()
        {
            string reason = OpenBlockReason();
            if (reason != null)
            {
                Notifier.Show(reason);
                return;
            }
            SetOpen(true);
        }

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            if (open)
            {
                if (!CanOpenNow) return;
                IsOpen = true;
                SessionTimeLeft = sessionDurationSeconds;
                spawnTimer = 2f;
                customersThisSession = 0;
                DayReportSystem.LogSession();
                DayReportSystem.LogActivity("Membuka warung");

                int nasi = WarungStorage.StockFresh(ItemDatabase.Nasi) + (Inventory.Instance != null ? Inventory.Instance.CountFresh(ItemDatabase.Nasi) : 0);
                int kopi = WarungStorage.StockFresh(ItemDatabase.Kopi) + (Inventory.Instance != null ? Inventory.Instance.CountFresh(ItemDatabase.Kopi) : 0);
                Notifier.Show("Warung dibuka! Hidangan siap: Nasi " + nasi + ", Kopi " + kopi + ". Catat pesanan di meja, lalu antar makanannya.");
            }
            else
            {
                CloseSession("Warung ditutup.", false);
            }
        }

        public void ToggleOpen() { SetOpen(!IsOpen); }

        private void CloseSession(string message, bool spendTime)
        {
            if (!IsOpen) return;
            IsOpen = false;

            var snapshot = new List<Customer>(activeCustomers);
            foreach (var c in snapshot)
            {
                if (c == null) continue;
                if (c.State == CustomerState.Eating) CompleteOrder(c); // sudah menerima makanan: dianggap membayar
                else if (c.State != CustomerState.Leaving) FailCustomer(c, "Warung ditutup sebelum dilayani");
            }
            activeCustomers.RemoveAll(c => c == null);

            var time = TimeManager.Instance;
            sessionDoneDay = time != null ? time.Day : -1;

            Notifier.Show(message + " Pendapatan hari ini: " + TextFormat.Rupiah(TodayIncome) + ", " + TodayServed + " pesanan selesai.");

            if (spendTime && time != null)
            {
                DayReportSystem.LogActivity("Sesi warung selesai");
                time.Spend(time.CostFromSections(time.costs.warungSessionSections));
            }
        }

        /// <summary>Periode berganti saat warung masih buka (mis. tidur atau tombol debug): tutup tanpa memakai waktu lagi.</summary>
        private void HandlePeriodChanged(TimeOfDay period)
        {
            if (IsOpen) CloseSession("Waktu berpindah, warung ditutup.", false);
        }

        public int SessionDoneDay { get { return sessionDoneDay; } }

        /// <summary>Pulihkan keputusan hari Minggu dan hari sesi terakhir dari data simpan.</summary>
        public void RestoreState(SundayChoice choice, int sessionDay)
        {
            SundayDecision = choice;
            sessionDoneDay = sessionDay;
        }

        private void HandleNewDay(int day)
        {
            SundayDecision = SundayChoice.Belum;
            var time = TimeManager.Instance;
            // Selama time skip tidak ada pop-up; pilihan Minggu ditanyakan sekali di hari tujuan (HandleTimeSkipped).
            if (time != null && !time.IsSkipping && time.IsSunday(day)) PromptSunday();
        }

        private void HandleTimeSkipped(int from, int to)
        {
            var time = TimeManager.Instance;
            if (time != null && time.IsSundayToday) PromptSunday();
        }

        // ================= Hari Minggu =================

        private void PromptSunday()
        {
            var time = TimeManager.Instance;
            int day = time != null ? time.Day : 0;
            ModalManager.Show(
                "Hari Minggu",
                "Hari ke-" + day + " adalah hari Minggu.\n\nApakah warung dibuka hari ini, atau libur?\n\n" +
                "<size=22>Jika libur, tidak ada operasional warung hari ini.\nPilihan masih bisa diubah lewat papan warung sebelum warung dibuka.</size>",
                "Buka warung", "Libur hari ini",
                choice => ChooseSunday(choice == 0),
                1);
        }

        public void ChooseSunday(bool open)
        {
            if (IsOpen)
            {
                Notifier.Show("Warung sedang buka.");
                return;
            }
            SundayDecision = open ? SundayChoice.Buka : SundayChoice.Libur;
            DayReportSystem.LogSundayHoliday(!open);
            Notifier.Show(open ? "Warung akan dibuka hari Minggu ini. Siapkan hidangan lalu buka lewat papan warung." : "Warung libur hari Minggu ini.");
        }

        // ================= Pelanggan =================

        private void TrySpawnCustomer()
        {
            if (spawnPoint == null) return;
            if (maxCustomersPerSession > 0 && customersThisSession >= maxCustomersPerSession) return;

            var free = new List<CustomerTable>();
            foreach (var t in CustomerTable.All)
            {
                if (t != null && t.Occupant == null) free.Add(t);
            }
            if (free.Count == 0) return;

            CustomerTable table = free[Random.Range(0, free.Count)];
            var customer = Customer.Create(spawnPoint.position, customerMaterial, PickOrder(), table, this);
            table.Occupant = customer;
            activeCustomers.Add(customer);
            customersThisSession++;
            DayReportSystem.LogCustomer();
        }

        public void CustomerSeated(Customer c)
        {
            if (c.Table != null) Notifier.Show("Pelanggan duduk di Meja " + c.Table.tableNumber + ". Catat pesanannya!");
        }

        public void CustomerLostPatience(Customer c)
        {
            if (!activeCustomers.Contains(c)) return;

            string reason;
            if (c.State == CustomerState.WaitingToOrder) reason = "Pesanan tidak sempat dicatat";
            else if (WarungStorage.TotalFresh(c.OrderId) <= 0) reason = "Stok " + c.OrderName + " habis";
            else reason = "Makanan terlambat diantar";

            FailCustomer(c, reason);
        }

        public void CustomerFinishedEating(Customer c)
        {
            if (!activeCustomers.Contains(c)) return;
            CompleteOrder(c);
        }

        private void FailCustomer(Customer c, string reason)
        {
            int tn = c.Table != null ? c.Table.tableNumber : 0;
            activeCustomers.Remove(c);
            Rate(0f);
            DayReportSystem.LogFailed(reason);
            Notifier.Show("Pelanggan Meja " + tn + " pergi kecewa: " + reason + ".");
            c.Leave(spawnPoint != null ? spawnPoint.position : c.transform.position, false, "Kecewa...");
        }

        private void CompleteOrder(Customer c)
        {
            var def = ItemDatabase.Get(c.OrderId);
            int price = def != null ? def.Price : 0;
            int tn = c.Table != null ? c.Table.tableNumber : 0;
            float satisfaction = c.ComputeSatisfaction(worstServedSatisfaction);

            if (Inventory.Instance != null) Inventory.Instance.AddMoney(price);
            DayReportSystem.LogIncome(price);
            DayReportSystem.LogServed(c.OrderId, satisfaction);
            Rate(satisfaction);

            activeCustomers.Remove(c);
            Notifier.Show("Meja " + tn + " membayar " + TextFormat.Rupiah(price) + "  (kepuasan " + Mathf.RoundToInt(satisfaction) + ")");
            c.Leave(spawnPoint != null ? spawnPoint.position : c.transform.position, true, "Terima kasih!");
        }

        private void Rate(float satisfaction)
        {
            RatedCustomers++;
            OverallSatisfaction = RatedCustomers == 1 ? satisfaction : Mathf.Lerp(OverallSatisfaction, satisfaction, 0.2f);
        }

        // ================= Interaksi meja =================

        /// <summary>
        /// Status pesanan yang jelas untuk UI: Menunggu dicatat -> Sedang disiapkan / Menunggu makanan -> Siap diantar -> Diantar (makan) -> Selesai.
        /// "Selesai" = pelanggan sudah membayar dan pergi (dihitung di Pesanan selesai).
        /// </summary>
        public string OrderStatusLabel(Customer c)
        {
            switch (c.State)
            {
                case CustomerState.Arriving: return "Datang";
                case CustomerState.WaitingToOrder: return "Menunggu dicatat";
                case CustomerState.WaitingForFood:
                    if (Inventory.Instance != null && Inventory.Instance.CountFresh(c.OrderId) > 0) return "Siap diantar";
                    if (KitchenStation.IsPreparing(c.OrderId)) return "Sedang disiapkan";
                    return "Menunggu makanan";
                case CustomerState.Eating: return "Diantar, makan";
                default: return "Selesai";
            }
        }

        public string GetTablePrompt(CustomerTable table, string name)
        {
            var c = table.Occupant;
            if (c == null) return name + " (kosong)";

            switch (c.State)
            {
                case CustomerState.Arriving: return name + ": pelanggan sedang datang";
                case CustomerState.WaitingToOrder: return "Catat pesanan " + name;
                case CustomerState.WaitingForFood:
                    bool carrying = Inventory.Instance != null && Inventory.Instance.CountFresh(c.OrderId) > 0;
                    return carrying ? "Antar " + c.OrderName + " ke " + name : name + " menunggu " + c.OrderName + " (ambil dulu)";
                case CustomerState.Eating: return name + ": sedang makan";
            }
            return name;
        }

        public void InteractTable(CustomerTable table)
        {
            var c = table.Occupant;
            if (c == null)
            {
                Notifier.Show("Meja " + table.tableNumber + " kosong.");
                return;
            }

            switch (c.State)
            {
                case CustomerState.Arriving:
                    Notifier.Show("Pelanggan masih berjalan ke meja.");
                    break;

                case CustomerState.WaitingToOrder:
                    c.TakeOrder();
                    Notifier.Show("Meja " + table.tableNumber + " memesan: " + c.OrderName + ". Siapkan dan antar!");
                    break;

                case CustomerState.WaitingForFood:
                    Deliver(c, table);
                    break;

                case CustomerState.Eating:
                    Notifier.Show("Meja " + table.tableNumber + " sedang makan.");
                    break;
            }
        }

        private void Deliver(Customer c, CustomerTable table)
        {
            var inv = Inventory.Instance;
            if (inv == null) return;

            if (inv.Container.RemoveFresh(c.OrderId, 1))
            {
                c.Serve();
                Notifier.Show("Mengantar " + c.OrderName + " ke Meja " + table.tableNumber + ".");
            }
            else if (inv.Count(c.OrderId) > 0)
            {
                Notifier.Show(c.OrderName + " yang kamu bawa sudah basi dan tidak bisa disajikan!");
            }
            else
            {
                Notifier.Show("Kamu belum membawa " + c.OrderName + ". Masak di dapur atau ambil dari Gudang/Kulkas (Q).");
            }
        }

        /// <summary>
        /// Ambil makanan yang sedang ditunggu pelanggan dari sebuah penyimpanan ke inventaris pemain
        /// (hanya yang masih layak dan muat dalam batas berat). Umur makanan ikut terbawa.
        /// </summary>
        public void FetchOrdersFrom(ItemContainer source, string sourceName)
        {
            var inv = Inventory.Instance;
            if (inv == null || source == null) return;

            if (!IsOpen)
            {
                Notifier.Show("Warung belum dibuka.");
                return;
            }

            var need = new Dictionary<string, int>();
            foreach (var c in activeCustomers)
            {
                if (c == null || c.State != CustomerState.WaitingForFood) continue;
                int n;
                need.TryGetValue(c.OrderId, out n);
                need[c.OrderId] = n + 1;
            }

            if (need.Count == 0)
            {
                Notifier.Show("Belum ada pesanan yang menunggu makanan. Catat pesanan di meja dulu.");
                return;
            }

            int taken = 0;
            bool tooHeavy = false;
            bool missing = false;

            foreach (var kv in need)
            {
                int want = kv.Value - inv.Container.CountFresh(kv.Key);
                for (int i = 0; i < want; i++)
                {
                    if (source.CountFresh(kv.Key) <= 0) { missing = true; break; }
                    if (!inv.CanCarry(kv.Key, 1)) { tooHeavy = true; break; }

                    var unit = source.TakeOneFresh(kv.Key);
                    if (unit == null) { missing = true; break; }
                    if (inv.Add(unit.Id, 1, unit.Age)) taken++;
                    else source.Add(unit.Id, 1, unit.Age);
                }
            }

            if (taken > 0) Notifier.Show("Mengambil " + taken + " hidangan dari " + sourceName + ".");
            if (tooHeavy) Notifier.Show("Beban penuh! Antar dulu pesanan yang sudah dibawa.");
            if (missing) Notifier.Show("Sebagian pesanan tidak tersedia di " + sourceName + " (habis atau basi).");
            if (taken == 0 && !tooHeavy && !missing) Notifier.Show("Kamu sudah membawa semua yang dibutuhkan. Antar ke meja!");
        }

        public void ShowSummary()
        {
            string avg = RatedCustomers > 0 ? Mathf.RoundToInt(OverallSatisfaction) + "/100" : "-";
            Notifier.Show("Kasir: pendapatan hari ini " + TextFormat.Rupiah(TodayIncome) + ", selesai " + TodayServed + ", gagal " + TodayFailed
                          + ", kepuasan keseluruhan " + avg + ". (Pembayaran diterima langsung di meja pelanggan.)");
        }

        // ================= Permintaan pelanggan =================

        private float CustomerRateMultiplier()
        {
            float timeMult = 1f;
            var time = TimeManager.Instance;
            if (time != null)
            {
                switch (time.Period)
                {
                    case TimeOfDay.Pagi: timeMult = 1.2f; break;
                    case TimeOfDay.Siang: timeMult = 1f; break;
                    case TimeOfDay.Malam: timeMult = 0.3f; break;
                }
            }

            float dangerMult = 1f;
            var disaster = DisasterSystem.Instance;
            if (disaster != null)
            {
                switch (disaster.CurrentDanger)
                {
                    case DangerLevel.Waspada: dangerMult = 0.8f; break;
                    case DangerLevel.Bahaya: dangerMult = 0.5f; break;
                    case DangerLevel.Kritis: dangerMult = 0.2f; break;
                }
            }

            float satisfactionMult = 1f;
            if (demandInfluence > 0f && RatedCustomers > 0)
            {
                satisfactionMult = Mathf.Clamp(1f + demandInfluence * (OverallSatisfaction - 50f) / 50f, 0.5f, 1.5f);
            }

            return timeMult * dangerMult * satisfactionMult;
        }

        private string PickOrder()
        {
            float maskWeight = 0.3f;
            var disaster = DisasterSystem.Instance;
            if (disaster != null)
            {
                switch (disaster.CurrentDanger)
                {
                    case DangerLevel.Waspada: maskWeight = 1.5f; break;
                    case DangerLevel.Bahaya: maskWeight = 3f; break;
                    case DangerLevel.Kritis: maskWeight = 4f; break;
                }
            }

            float wNasi = 4f, wAir = 3f, wKopi = 2f;
            float roll = Random.value * (wNasi + wAir + wKopi + maskWeight);
            if (roll < wNasi) return ItemDatabase.Nasi;
            roll -= wNasi;
            if (roll < wAir) return ItemDatabase.Air;
            roll -= wAir;
            if (roll < wKopi) return ItemDatabase.Kopi;
            return ItemDatabase.Masker;
        }
    }
}
