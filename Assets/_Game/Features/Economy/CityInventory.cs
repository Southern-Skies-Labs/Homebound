using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Homebound.Core;

namespace Homebound.Features.Economy
{
    //Gestiona los recursos globales de la ciudad.
    public class CityInventory : MonoBehaviour, IInventory
    {

        //Variables
        private Dictionary<ItemData, int> _resourceDatabase = new Dictionary<ItemData, int>();

        [Header("Debug View (Read Only)")]
        [SerializeField] private List<InventorySlot> _debugInventoryView = new List<InventorySlot>();

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

        

        public int Add(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            if (_resourceDatabase.ContainsKey(item))
            {
                _resourceDatabase[item] += amount;
            }
            else
            {
                _resourceDatabase.Add(item, amount);
            }

            UpdateDebugView();
            OnInventoryUpdated?.Invoke();
            
            Debug.Log($"[CityInventory] Reino ganó: {amount} {item.DisplayName}. Total: {_resourceDatabase[item]}");
            return 0; 
        }

        public bool Remove(ItemData item, int amount)
        {
            if (!Has(item, amount)) return false;

            _resourceDatabase[item] -= amount;

            
            if (_resourceDatabase[item] <= 0)
            {
                _resourceDatabase.Remove(item);
            }

            UpdateDebugView();
            OnInventoryUpdated?.Invoke();
            return true;
        }

        public bool Has(ItemData item, int amount)
        {
            if (item == null) return false;
            return _resourceDatabase.ContainsKey(item) && _resourceDatabase[item] >= amount;
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
            if (item == null) return 0;
            return _resourceDatabase.ContainsKey(item) ? _resourceDatabase[item] : 0;
        }

        public Dictionary<ItemData, int> GetAllItems()
        {
            return new Dictionary<ItemData, int>(_resourceDatabase);
        }

        // METODOS DE DEBUG

        private void UpdateDebugView()
        {
            // Solo actualizamos la lista si estamos en el editor para evitar Garbage Collection innecesario en builds
#if UNITY_EDITOR
            _debugInventoryView.Clear();
            foreach (var kvp in _resourceDatabase)
            {
                _debugInventoryView.Add(new InventorySlot(kvp.Key, kvp.Value));
            }
#endif
        }
        
        // Método Cheat para Debug
        [ContextMenu("Add Test Resources")]
        public void DebugAddResources()
        {
            Debug.LogWarning("💰 [CHEAT] Añadiendo recursos de prueba...");
            // Aquí idealmente cargarías items de una carpeta Resources, 
            // pero como no tenemos referencias directas, esto es solo un placeholder
            // para que lo llames manualmente si tienes referencias.
        }
    }
}