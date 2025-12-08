using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Economy
{
    public class StorageContainer : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private int _capacity = 1000;
        [SerializeField] private bool _acceptsAllItems = true;
        
        // Inventario interno (Físico)
        private UnitInventory _internalInventory; 

        private void Awake()
        {
            _internalInventory = gameObject.AddComponent<UnitInventory>();
            _internalInventory.ConfigureCapacity(_capacity);
        }

        private void Start()
        {
            var economyManager = ServiceLocator.Get<EconomyManager>();
            if (economyManager != null) economyManager.RegisterStorage(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<EconomyManager>(out var economyManager))
            {
                economyManager.UnregisterStorage(this);
            }
        }

        public bool DepositItem(ItemData item, int amount)
        {
            
            if (!_acceptsAllItems) return false; // Aquí podrías añadir lógica de filtro por tipo

            
            int leftOver = _internalInventory.Add(item, amount);
            int depositedAmount = amount - leftOver;

            
            if (depositedAmount > 0)
            {
                var cityInv = ServiceLocator.Get<CityInventory>();
                if (cityInv != null)
                {
                    cityInv.Add(item, depositedAmount);
                }
                return true;
            }

            return false;
        }

        public bool CanAccept(ItemData item)
        {
            
            return !_internalInventory.IsEmpty || _internalInventory.Count(item) < _capacity; // Simplificado
        }
        
        public Vector3 GetDropOffPoint() => transform.position;
    }
}