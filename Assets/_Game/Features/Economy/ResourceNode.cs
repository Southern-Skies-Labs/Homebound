using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Economy
{
    public class ResourceNode : MonoBehaviour, IGatherable
    {
        //Variables
        [Header("Configuración")] 
        [SerializeField] private string _nodeName = "Árbol";
        [SerializeField] private float _health = 100f;

        [Header("Loot")]
        [SerializeField] private ItemData _resourceToDrop;
        [SerializeField] private int _amountToDrop = 5;

        public string Name => _nodeName;
        public Transform Transform => transform;
        
        
        //Metodos
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public InventorySlot GetDrop()
        {
            return new InventorySlot(_resourceToDrop, _amountToDrop);
        }

        public bool Gather(float efficiency)
        {
            _health -= efficiency;
            //transform.DOShakeScale(0.1f, 0.1f);

            if (_health <= 0)
            {
                Die();
                return true;
            }
            return false;
        }

        private void Die()
        {
            Debug.Log($"[ResourceNode] {{_nodeName}} recolectado. Drop: {{_amountToDrop}} {{_resourceToDrop.name}}");

            var cityInventory = ServiceLocator.Get<CityInventory>();
            if (cityInventory != null)
            {
                cityInventory.Add(_resourceToDrop, _amountToDrop);
            }
            Destroy(gameObject);
        }
        
    }

}
