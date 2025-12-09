using System.Collections.Generic;
using UnityEngine;
using Homebound.Core; 

namespace Homebound.Features.Economy
{
    public class ResourceNode : MonoBehaviour, IGatherable, IReservable
    {
        // --- CONFIGURACIÓN ---
        [Header("Identity")]
        [SerializeField] private string _nodeName = "Resource";
        [SerializeField] private ItemData _resourceType;

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f; // Vida (Dureza del árbol)
        [SerializeField] private float _currentHealth;
        
        [Tooltip("Cantidad TOTAL de recursos que tiene este árbol dentro.")]
        [SerializeField] private int _amountToDrop = 20; // <--- AHORA SÍ IMPORTA ESTO

        [Header("Reservations")]
        [SerializeField] private int _maxWorkers = 2; 
        
        // --- ESTADO ---
        private List<GameObject> _activeWorkers = new List<GameObject>();
        private bool _isDepleted = false;
        
        // Acumulador para manejar decimales (ej: si un golpe da 0.5 de madera)
        private float _extractionAccumulator = 0f; 

        // --- PROPIEDADES DE INTERFAZ ---
        public string Name => _nodeName;
        public Vector3 Position => transform.position;
        public Transform Transform => transform;
        public bool IsDepleted => _isDepleted;

        // IReservable
        public bool CanReserve => !_isDepleted && _activeWorkers.Count < _maxWorkers;
        public int MaxWorkers => _maxWorkers;
        public int CurrentWorkers => _activeWorkers.Count;

        // --- UNITY EVENTS ---
        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        // --- LÓGICA DE GATHERING (IGatherable) ---

        public int Gather(float damage) // 'efficiency' es el daño de la herramienta
        {
            if (_isDepleted) return 0;

            // 1. Calcular porcentaje de daño realizado respecto a la vida total
            // Ejemplo: Si vida es 100 y daño es 10 -> Hicimos un 10% de daño (0.1)
            float damagePercent = damage / _maxHealth;

            // 2. Calcular cuántos recursos corresponden a ese porcentaje
            // Ejemplo: Si el drop total es 20 y sacamos el 10% -> 2 recursos.
            float rawAmount = damagePercent * _amountToDrop;

            // 3. Sumar al acumulador
            _extractionAccumulator += rawAmount;

            // 4. Extraer la parte entera (lo que le damos al jugador)
            int amountToGive = Mathf.FloorToInt(_extractionAccumulator);
            
            // 5. Restar lo entregado del acumulador (guardamos el decimal restante)
            _extractionAccumulator -= amountToGive;

            // 6. Aplicar daño a la vida
            _currentHealth -= damage;
            
            if (_currentHealth <= 0)
            {
                // Al morir, si queda algún remanente importante o queremos asegurar 
                // que se entregue el último trozo, podría forzarse aquí, 
                // pero con el acumulador suele ser preciso.
                Die();
            }

            return amountToGive;
        }

        public InventorySlot GetDrop()
        {
            return new InventorySlot(_resourceType, _amountToDrop);
        }

        private void Die()
        {
            if (_isDepleted) return;
            _isDepleted = true;

            Debug.Log($"[ResourceNode] {_nodeName} agotado.");
            Destroy(gameObject); 
        }

        // --- LÓGICA DE RESERVAS (IReservable) ---

        public bool Reserve(GameObject worker)
        {
            if (!CanReserve) return false;
            if (_activeWorkers.Contains(worker)) return true; 

            _activeWorkers.Add(worker);
            return true;
        }

        public void Release(GameObject worker)
        {
            if (_activeWorkers.Contains(worker))
            {
                _activeWorkers.Remove(worker);
            }
        }

        private void OnDestroy()
        {
            _activeWorkers.Clear();
        }
    }
}