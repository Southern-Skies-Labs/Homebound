using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Homebound.Features.Economy
{
    public class UnitInventory : MonoBehaviour, IInventory
    {
        //Variable
        [Header("Mochila")]
        [SerializeField] private List<InventorySlot> _backpack = new List<InventorySlot>();
        [SerializeField] private int _maxSlots = 5;
        
        // Metodos
        public int Add(ItemData item, int amount)
        {
            var existingSlot = _backpack.FirstOrDefault(s => s.Item == item);
            if (existingSlot != null)
            {
                existingSlot.Add(amount);
                return 0;
            }

            if (_backpack.Count < _maxSlots)
            {
                _backpack.Add(new InventorySlot(item, amount));
                return 0;
            }
            //Mochila llena
            Debug.LogWarning($"{{name}}: ¡Mochila llena! No puedo recoger {{item.DisplayName}}");
            return amount;
        }

        public bool Remove(ItemData item, int amount)
        {
            var slot = _backpack.FirstOrDefault(s => s.Item == item);
            if (slot != null && slot.Amount >= amount)
            {
                slot.Remove(amount);
                if (slot.IsEmpty) _backpack.Remove(slot);
                return true;
            }
            return false;
        }
        
        public bool Has(ItemData item, int amount)
        {
            var slot = _backpack.FirstOrDefault(s => s.Item == item);
            return slot != null && slot.Amount >= amount;
        }
        
        public int Count(ItemData item)
        {
            var slot = _backpack.FirstOrDefault(s => s.Item == item);
            return slot != null ? slot.Amount : 0;
        }

    }

}
