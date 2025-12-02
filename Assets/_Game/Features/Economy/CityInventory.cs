using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Homebound.Core;

namespace Homebound.Features.Economy
{
    public class CityInventory : MonoBehaviour, IInventory
    {
        
        //Variables
        [Header("Almacen Global")]
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        public event Action OnInventoryUpdated;
        
        
        //Metodos
        private void Awake()
        {
            ServiceLocator.Register<CityInventory>(this);
        }
        
        private void Start()
        {
            // --- DEBUG / CHEAT DE INICIO ---
            // Le regalamos 50 de cada item posible para probar la construcción
            Debug.LogWarning("💰 [DEBUG] CityInventory: Añadiendo recursos iniciales de prueba.");
            
            // Busca todos los ItemData en tu carpeta de recursos (Resources_Data)
            // Asegúrate de que tus ScriptableObjects estén en una carpeta dentro de Resources si usas Resources.Load
            // O mejor, arrástralos en una lista en el inspector si prefieres.
            
            // FORMA RÁPIDA (Si tienes la lista _slots visible en inspector):
            // Simplemente añade manualmente en el Inspector del objeto _System -> CityInventory
            // Elemento 0: Item: Stone, Cantidad: 50.
            // Elemento 1: Item: Dirt, Cantidad: 50.
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CityInventory>();
        }
        
        

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
            // Debug.Log($"[CityInventory] Añadido {amount} de {item.DisplayName}.");
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

        
        public bool HasItem(ItemData item, int amount) => Has(item, amount);

        
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
    }
}