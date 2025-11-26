using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Homebound.Features.AethianAI;
using Homebound.Features.PlayerInteraction;

namespace Homebound.Features.UI
{
    public class UnitDetailsPanel : MonoBehaviour
    {
        //Variables
        [Header("UI References")] 
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _stateText;
        
        [Header("Bars")]
        [SerializeField] private Image _healthBar;
        [SerializeField] private Image _hungerBar;
        [SerializeField] private Image _energyBar;
        
        [Header("Controller")]
        [SerializeField] private InteractionController _interactionController;

        private AethianBot _selectedBot;
        
        //Metodos
        private void Start()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_interactionController == null) 
                _interactionController = FindFirstObjectByType<InteractionController>();

            if (_interactionController != null)
            {
                _interactionController.OnUnitSelected += HandleSelection;
            }
        }

        private void OnDestroy()
        {
            if(_interactionController != null)
                _interactionController.OnUnitSelected -= HandleSelection;
        }

        private void Update()
        {
            if (_selectedBot != null && _panelRoot.activeSelf)
            {
                UpdateBars();
                UpdateStateText();
            }
        }

        private void HandleSelection(AethianBot bot)
        {
            
            _selectedBot = bot;
            if (_selectedBot != null)
            {
                _panelRoot.SetActive(true);
                RefreshStaticInfo();
            }
            else
            {
                _panelRoot.SetActive(false);
            }
            Debug.Log($"UI Recibió selección: {(bot != null ? bot.name : "NULL")}");
        }

        private void RefreshStaticInfo()
        {
            if (_nameText != null)
            {
                _nameText.text = _selectedBot.Stats.GetFullName();
            }
        }
        
        private void UpdateBars()
        {
            if (_selectedBot == null) 
            {
                Debug.LogError("[UI] UpdateBars llamado pero _selectedBot es NULL");
                return;
            }
            
            if(_healthBar != null)
                _healthBar.fillAmount = _selectedBot.Stats.Health / _selectedBot.Stats.MaxHealth;

            if (_hungerBar != null)
                _hungerBar.fillAmount = _selectedBot.Stats.Hunger.Value / _selectedBot.Stats.Hunger.MaxValue;
            
            if(_energyBar != null)
                _energyBar.fillAmount = _selectedBot.Stats.Energy.Value / _selectedBot.Stats.Energy.MaxValue;
        }
        
        private void UpdateStateText()
        {
            if (_stateText != null)
            {
                _stateText.text = _selectedBot.Stats.ToString();
            }
            
        }
        
    }
}
