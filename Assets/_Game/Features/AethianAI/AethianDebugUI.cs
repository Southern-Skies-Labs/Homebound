using System;
using UnityEngine;
using TMPro;

namespace Homebound.Features.AethianAI
{
    
    public class AethianDebugUI : MonoBehaviour
    {
        //Variables
        [Header("References")] 
        [SerializeField] private AethianBot _bot;
        [SerializeField] private TextMeshProUGUI _infoText;
        [SerializeField] private Canvas _canvas;
        
        
        //Metodos
        private void Start()
        {
            if (_bot == null) _bot = GetComponentInParent<AethianBot>();

            if (_canvas != null)
            {
                _canvas.worldCamera = Camera.main;
            }

            if (_bot != null)
            {
                _bot.OnStateChanged += UpdateUI;
                UpdateUI("Initializing");
            }
        }

        private void OnDestroy()
        {
            if (_bot != null) _bot.OnStateChanged -= UpdateUI;
        }

        private void LateUpdate()
        {
            if (_canvas != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void UpdateUI(string stateName)
        {
            if (_infoText != null && _bot != null)
            {
                string fullName = _bot.Stats.GetFullName();

                string colorHex = "#000000";
                if (stateName == "Survival") colorHex = "#FF0000";
                else if (stateName == "Working") colorHex = "#00FF00";

                string nameColor = "#000000";
                
                _infoText.text = $"<color={nameColor}>{fullName}</color>\n<color={colorHex}>[{stateName}]</color>";
            }
        }
    }
}
