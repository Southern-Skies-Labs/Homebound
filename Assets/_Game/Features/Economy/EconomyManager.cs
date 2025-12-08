using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Homebound.Core;

namespace Homebound.Features.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        //Registro de los Containers del mapa
        private List<StorageContainer> _storageContainers = new List<StorageContainer>(); 
        
        
        //Metodiños
        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<EconomyManager>();
            _storageContainers.Clear();
        }
        
        
        //Api de Registro

        public void RegisterStorage(StorageContainer container)
        {
            if (!_storageContainers.Contains(container))
            {
                _storageContainers.Add(container);
                // Debug.Log($"[EconomyManager] Almacén registrado. Total: {_storageContainers.Count}");
            }
        }

        public void UnregisterStorage(StorageContainer container)
        {
            if (_storageContainers.Contains(container))
            {
                _storageContainers.Remove(container);
            }
        }
        
        //Api de Busqueda

        public StorageContainer GetNearestStorage(Vector3 position, ItemData itemToCheck = null)
        {
            StorageContainer bestContainer = null;
            float closestDistanceSqr = float.MaxValue;

            foreach (var container in _storageContainers)
            {
                if (container == null) continue;

                if (itemToCheck != null && !container.CanAccept(itemToCheck)) continue;

                float distSqr = (container.transform.position - position).sqrMagnitude;

                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    bestContainer = container;
                }

            }

            return bestContainer;
        }
        
        
        
        
    }
}