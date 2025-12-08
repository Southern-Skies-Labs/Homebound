using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Homebound.Features.Economy;

namespace Homebound.Features.UI
{
    public class ResourceRowUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Setup(ItemData item, int amount)
        {
            if (item == null) return;

            _iconImage.sprite = item.Icon;
            _nameText.text = item.DisplayName;
            _amountText.text = amount.ToString("N0");

        }

    }
}
