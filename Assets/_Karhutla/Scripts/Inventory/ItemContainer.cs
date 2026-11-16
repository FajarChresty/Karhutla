using System;
using System.Collections.Generic;
using UnityEngine;

namespace Karhutla
{
    /// <summary>
    /// Sekumpulan unit item dengan UMUR yang sama (dalam "sesi" waktu). Umur dipakai untuk kedaluwarsa.
    /// Item yang sama tapi umurnya berbeda disimpan sebagai batch terpisah.
    /// </summary>
    public class ItemBatch
    {
        public string Id;
        public int Count;
        public float Age;

        public ItemDefinition Def { get { return ItemDatabase.Get(Id); } }
        public bool IsExpired { get { return FoodExpirySystem.IsExpired(Id, Age); } }
        public float TotalWeight { get { var d = Def; return d != null ? d.Weight * Count : 0f; } }
    }

    /// <summary>
    /// Wadah item berbasis BERAT. Dipakai oleh inventaris pemain, gudang warung, dan kulkas
    /// (tiga wadah terpisah dengan kapasitas masing-masing). MaxWeight &lt;= 0 berarti tanpa batas.
    /// </summary>
    public class ItemContainer
    {
        public string Name;
        public float MaxWeight;

        private readonly List<ItemBatch> batches = new List<ItemBatch>();

        public event Action Changed;

        public ItemContainer(string name, float maxWeight)
        {
            Name = name;
            MaxWeight = maxWeight;
        }

        public IReadOnlyList<ItemBatch> Batches { get { return batches; } }

        public float TotalWeight
        {
            get
            {
                float w = 0f;
                for (int i = 0; i < batches.Count; i++) w += batches[i].TotalWeight;
                return w;
            }
        }

        public float FreeWeight { get { return MaxWeight <= 0f ? float.MaxValue : Mathf.Max(0f, MaxWeight - TotalWeight); } }

        public bool CanAddWeight(float weight)
        {
            return MaxWeight <= 0f || TotalWeight + weight <= MaxWeight + 0.001f;
        }

        public bool CanAdd(string id, int count)
        {
            var def = ItemDatabase.Get(id);
            if (def == null || count <= 0) return false;
            return CanAddWeight(def.Weight * count);
        }

        /// <summary>Tambah item. Gagal (false) jika id tidak dikenal atau melewati kapasitas berat.</summary>
        public bool Add(string id, int count, float age = 0f)
        {
            if (!CanAdd(id, count)) return false;

            ItemBatch target = null;
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].Id == id && Mathf.Abs(batches[i].Age - age) < 0.01f)
                {
                    target = batches[i];
                    break;
                }
            }

            if (target != null) target.Count += count;
            else batches.Add(new ItemBatch { Id = id, Count = count, Age = age });

            RaiseChanged();
            return true;
        }

        /// <summary>Pulihkan satu batch dari data simpan (membawa umurnya). Tidak memeriksa kapasitas; id yang tidak dikenal dilewati.</summary>
        public void RestoreBatch(string id, int count, float age)
        {
            if (count <= 0 || ItemDatabase.Get(id) == null) return;
            batches.Add(new ItemBatch { Id = id, Count = count, Age = Mathf.Max(0f, age) });
            RaiseChanged();
        }

        public int Count(string id)
        {
            int c = 0;
            for (int i = 0; i < batches.Count; i++) if (batches[i].Id == id) c += batches[i].Count;
            return c;
        }

        public int CountFresh(string id)
        {
            int c = 0;
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].Id == id && !batches[i].IsExpired) c += batches[i].Count;
            }
            return c;
        }

        public int CountExpired(string id)
        {
            return Count(id) - CountFresh(id);
        }

        public int TotalExpired()
        {
            int c = 0;
            for (int i = 0; i < batches.Count; i++) if (batches[i].IsExpired) c += batches[i].Count;
            return c;
        }

        /// <summary>Ambil sejumlah unit yang masih layak (yang paling tua lebih dulu). Semua atau tidak sama sekali.</summary>
        public bool RemoveFresh(string id, int count)
        {
            if (count <= 0 || CountFresh(id) < count) return false;

            var order = new List<ItemBatch>();
            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].Id == id && !batches[i].IsExpired) order.Add(batches[i]);
            }
            order.Sort((a, b) => b.Age.CompareTo(a.Age));

            int left = count;
            for (int i = 0; i < order.Count && left > 0; i++)
            {
                int take = Mathf.Min(left, order[i].Count);
                order[i].Count -= take;
                left -= take;
            }
            batches.RemoveAll(b => b.Count <= 0);
            RaiseChanged();
            return true;
        }

        /// <summary>Ambil SATU unit layak yang paling tua beserta umurnya (agar umur tidak "direset" saat dipindah). Null jika tidak ada.</summary>
        public ItemBatch TakeOneFresh(string id)
        {
            ItemBatch best = null;
            for (int i = 0; i < batches.Count; i++)
            {
                var b = batches[i];
                if (b.Id != id || b.IsExpired || b.Count <= 0) continue;
                if (best == null || b.Age > best.Age) best = b;
            }
            if (best == null) return null;
            return Take(best, 1);
        }

        /// <summary>Keluarkan sejumlah unit dari sebuah batch. Mengembalikan batch baru (terlepas) yang membawa umurnya.</summary>
        public ItemBatch Take(ItemBatch batch, int count)
        {
            if (batch == null || !batches.Contains(batch) || count <= 0) return null;
            int take = Mathf.Min(count, batch.Count);
            var result = new ItemBatch { Id = batch.Id, Count = take, Age = batch.Age };
            batch.Count -= take;
            if (batch.Count <= 0) batches.Remove(batch);
            RaiseChanged();
            return result;
        }

        /// <summary>Buang semua unit yang sudah basi. Mengembalikan jumlah unit yang dibuang.</summary>
        public int DiscardExpired()
        {
            int removed = 0;
            for (int i = batches.Count - 1; i >= 0; i--)
            {
                if (batches[i].IsExpired)
                {
                    removed += batches[i].Count;
                    batches.RemoveAt(i);
                }
            }
            if (removed > 0) RaiseChanged();
            return removed;
        }

        /// <summary>Tambah umur semua batch. Mengembalikan jumlah unit yang BARU menjadi basi.</summary>
        public int AgeAll(float amount)
        {
            if (amount <= 0f) return 0;
            int newlyExpired = 0;
            for (int i = 0; i < batches.Count; i++)
            {
                bool was = batches[i].IsExpired;
                batches[i].Age += amount;
                if (!was && batches[i].IsExpired) newlyExpired += batches[i].Count;
            }
            RaiseChanged();
            return newlyExpired;
        }

        /// <summary>Daftar batch terurut (urutan ItemDatabase, lalu paling tua dulu). Dipakai UI.</summary>
        public List<ItemBatch> Sorted()
        {
            var list = new List<ItemBatch>(batches);
            list.Sort((a, b) =>
            {
                int c = ItemDatabase.IndexOf(a.Id).CompareTo(ItemDatabase.IndexOf(b.Id));
                return c != 0 ? c : b.Age.CompareTo(a.Age);
            });
            return list;
        }

        public void Clear()
        {
            batches.Clear();
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (Changed != null) Changed();
        }
    }
}
