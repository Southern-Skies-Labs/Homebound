using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    
    public class UnitMovementController : MonoBehaviour
    {
        //Variables
        [Header("Configuración de Movimiento")]
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private float _rotationSpeed = 10f;

        private PathfindingService _pathfindingService;
        private Coroutine _currentMoveCoroutine;
        
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            _pathfindingService = ServiceLocator.Get<PathfindingService>();
        }

        public void MoveTo(Vector3 targetPosition)
        {
            List<Vector3> path = _pathfindingService.FindPath(transform.position, targetPosition);

            if (path != null && path.Count > 0)
            {
                StopMoving();
                _currentMoveCoroutine = StartCoroutine(FollowPath(path));
            }
            else
            {
                IsMoving = false;
                Debug.LogWarning($"[UnitMovement] No se encontró ruta para el movimiento {targetPosition}.");
            }
        }
        
        public void StopMoving()
        {
            if (_currentMoveCoroutine != null)
            {
                StopCoroutine(_currentMoveCoroutine);
                _currentMoveCoroutine = null;
            }
            IsMoving = false;
        }

        private IEnumerator FollowPath(List<Vector3> path)
        {
            IsMoving = true; // Empezamos a movernos

            foreach (Vector3 waypoint in path)
            {
                Vector3 currentWaypoint = new Vector3(waypoint.x, transform.position.y, waypoint.z);

                while (Vector3.Distance(transform.position, currentWaypoint) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, 
                        currentWaypoint, 
                        _moveSpeed * Time.deltaTime
                    );

                    Vector3 direction = (currentWaypoint - transform.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, 
                            lookRotation, 
                            _rotationSpeed * Time.deltaTime
                        );
                    }
                    yield return null; 
                }
            }
            
            IsMoving = false; // Terminamos la ruta
        }
    }
}