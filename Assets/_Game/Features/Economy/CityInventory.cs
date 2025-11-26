using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Homebound.Core;
using NUnit.Framework;

namespace Homebound.Features.Economy
{
    public class CityInventory : MonoBehaviour, IInventory
    {
        //Variable
        [Header("Almacen Global")]
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        public event Action OnInventoryUpdated;
        
        //Metodos
        private void Awake()
        {
            ServiceLocator.Register<CityInventory>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CityInventory>();
        }
        
        //Implementaciónm del IInventory

        public int Add(ItemData item, int amount)
        {
            var existingSlot = _slots.FirstOrDefault(s => s.Item == item);

            if (existingSlot != null)
            {
                existingSlot.Add(amount);
            }
            else
            {
                _slots.Add(new InventorySlot(item, amount));
            }

            OnInventoryUpdated?.Invoke();
            Debug.Log($"[CityInventory] Añadido {amount} de {item.DisplayName}. Total: {Count(item)}");
            return 0;
        }

        public bool Remove(ItemData item, int amount)
        {
            if (!Has(item, amount)) return false;
            var slot = _slots.FirstOrDefault(s => s.Item == item);
            if (slot != null)
            {
                slot.Remove(amount);
                if (slot.IsEmpty) _slots.Remove(slot);
                OnInventoryUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public bool Has(ItemData item, int amount)
        {
            return Count(item) >= amount;
        }

        public int Count(ItemData item)
        {
            var slot = _slots.FirstOrDefault(s => s.Item == item);
            return slot != null ? slot.Amount : 0;
        }
    }
}

