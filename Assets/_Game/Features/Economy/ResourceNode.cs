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
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;
        [SerializeField] private int _amountPerHit = 1; 
        [SerializeField] private int _amountToDrop = 5; // Configuración para GetDrop

        [Header("Reservations")]
        [SerializeField] private int _maxWorkers = 2; 
        
        // --- ESTADO ---
        private List<GameObject> _activeWorkers = new List<GameObject>();
        private bool _isDepleted = false;

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

        public int Gather(float efficiency)
        {
            if (_isDepleted) return 0;

            _currentHealth -= efficiency;
            int amountToGive = _amountPerHit; 
            
            if (_currentHealth <= 0)
            {
                Die();
            }

            return amountToGive;
        }

        // Recuperamos este método para cumplir con la interfaz y ayudar al Bot
        public InventorySlot GetDrop()
        {
            // Devuelve una "muestra" de lo que dropea este nodo
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