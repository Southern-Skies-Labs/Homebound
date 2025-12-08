using System;
using UnityEngine;
using System.Collections.Generic;
using Homebound.Core;
using Homebound.Features.Economy;

namespace Homebound.Features.UI
{
    public class GlobalResourcesUI : MonoBehaviour
    {
        //Variables
        [Header("Referencias UI")] 
        [SerializeField] private GameObject _detailsWindow;
        [SerializeField] private Transform _listContainer;
        [SerializeField] private ResourceRowUI _rowPrefab;

        private CityInventory _cityInventory;
        private bool _isWindowOpen = false;
        
        //Metodos de inicio

        private void Start()
        {
            _cityInventory = ServiceLocator.Get<CityInventory>();

            if (_cityInventory != null)
            {
                _cityInventory.OnInventoryUpdated += RefreshUI;
            }
            _detailsWindow.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_cityInventory != null)
            {
                _cityInventory.OnInventoryUpdated -= RefreshUI;
            }
        }
        
        //Metodos de Interacción0

        public void ToggleWindow()
        {
            _isWindowOpen = !_isWindowOpen;
            _detailsWindow.SetActive(_isWindowOpen);

            if (_isWindowOpen)
            {
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (!_isWindowOpen) return;

            foreach (Transform child in _listContainer)
            {
                Destroy(child.gameObject);    
            }

            var allItems = _cityInventory.GetAllItems();

            foreach (var kvp in allItems)
            {
                ItemData item = kvp.Key;
                int amount = kvp.Value;

                var newRow = Instantiate(_rowPrefab, _listContainer);
                newRow.Setup(item, amount);
            }
        }
        
    }
}
