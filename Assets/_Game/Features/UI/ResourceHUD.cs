using System;
using Codice.Client.GameUI.Update;
using UnityEngine;
using Homebound.Features.Economy;
using TMPro;
using Homebound.Core;

namespace Homebound.Features.UI
{
    public class ResourceHUD : MonoBehaviour
    {
        //Variables
        [Header("Referencias UI")] 
        [SerializeField] private TextMeshProUGUI _woodText;
        [SerializeField] private TextMeshProUGUI _stoneText;
        [SerializeField] private TextMeshProUGUI _foodText;
        
        
        [Header("Data References")]
        [SerializeField] private ItemData _woodData;
        [SerializeField] private ItemData _stoneData;
        [SerializeField] private ItemData _foodData;
        
        private CityInventory _inventory;
        
        
        //Metodos

        private void Start()
        {
            _inventory = ServiceLocator.Get<CityInventory>();
            if (_inventory != null)
            {
                _inventory.OnInventoryUpdated += UpdateUI;
                UpdateUI();
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryUpdated -= UpdateUI;
            }
        }
        
        private void UpdateUI()
        {
            if (_inventory == null) return;

            if (_woodText != null && _woodData != null) _woodText.text = $"{_inventory.Count(_woodData)}";
            
            if (_stoneText != null && _stoneData != null) _stoneText.text = $"{_inventory.Count(_stoneData)}";
            
            if (_foodText != null && _foodData != null) _foodText.text = $"{_inventory.Count(_foodData)}";


        }
        
        
    }
}
