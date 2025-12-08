using UnityEngine;
using System.Collections.Generic; 
using System.Linq;

namespace Homebound.Features.Economy
{
    public class UnitInventory : MonoBehaviour, IInventory
    {
        [Header("Configuración")]
        [SerializeField] private int _maxCapacity = 20;
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        
        public bool IsEmpty => _slots.Count == 0;

       
        //Metodos
        public void ConfigureCapacity(int newCapacity)
        {
            _maxCapacity = newCapacity;
        }

        public void Clear()
        {
            _slots.Clear();
        }


        public int Add(ItemData item, int amount)
        {
            // Lógica simple de añadir (puedes refinarla luego con MaxStack)
            var slot = _slots.FirstOrDefault(s => s.Item == item);
            if (slot != null)
            {
                slot.Add(amount);
            }
            else
            {
                if (_slots.Count >= _maxCapacity) return amount; 
                _slots.Add(new InventorySlot(item, amount));
            }
            return 0; // Todo guardado (0 sobrante)
        }

        public bool Remove(ItemData item, int amount)
        {
            var slot = _slots.FirstOrDefault(s => s.Item == item);
            if (slot != null && slot.Amount >= amount)
            {
                slot.Remove(amount);
                if (slot.IsEmpty) _slots.Remove(slot);
                return true;
            }
            return false;
        }

        public bool Has(ItemData item, int amount)
        {
            var slot = _slots.FirstOrDefault(s => s.Item == item);
            return slot != null && slot.Amount >= amount;
        }
        
        public bool HasItem(ItemData item, int amount) => Has(item, amount); // Alias

        public bool TryConsume(ItemData item, int amount)
        {
            if (Has(item, amount))
            {
                Remove(item, amount);
                return true;
            }
            return false;
        }

        public int Count(ItemData item)
        {
            var slot = _slots.FirstOrDefault(s => s.Item == item);
            return slot != null ? slot.Amount : 0;
        }
        
        public bool TransferAllTo(StorageContainer container)
        {
            if (container == null || IsEmpty) return false;

            bool anyTransfer = false;

            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = _slots[i];
                
                // Intentamos depositar en el contenedor
                if (container.DepositItem(slot.Item, slot.Amount))
                {
                    _slots.RemoveAt(i); 
                    anyTransfer = true;
                }
            }

            return anyTransfer;
        }
    }
}