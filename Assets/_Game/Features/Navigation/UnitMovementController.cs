using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class UnitMovementController : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private float _rotationSpeed = 12.0f;
        
        // Prefab de emergencia (Opcional por ahora, pero necesario para que compile si lo usabas)
        [SerializeField] private GameObject _emergencyLadderPrefab; 

        private PathfindingService _pathfindingService;
        private Coroutine _moveCoroutine;
        public bool IsMoving { get; private set; }

        private void Awake() => _pathfindingService = ServiceLocator.Get<PathfindingService>();

        public void MoveTo(Vector3 targetPosition)
        {
            if (_pathfindingService == null) _pathfindingService = ServiceLocator.Get<PathfindingService>();
            if (_pathfindingService == null) return;

            List<Vector3> path = _pathfindingService.FindPath(transform.position, targetPosition);

            if (path != null && path.Count > 0)
            {
                StopMoving();
                _moveCoroutine = StartCoroutine(FollowPath(path));
            }
            else
            {
                // Si falla el camino, aquí iría la lógica de emergencia.
                // Por ahora solo logueamos.
                Debug.LogWarning($"[UnitMovement] No se encontró camino a {targetPosition}");
                IsMoving = false;
            }
        }

        public void StopMoving()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
            IsMoving = false;
        }

        private IEnumerator FollowPath(List<Vector3> path)
        {
            IsMoving = true;
            foreach (Vector3 waypoint in path)
            {
                // Movimiento fluido
                while (Vector3.Distance(transform.position, waypoint) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, waypoint, _moveSpeed * Time.deltaTime);
                    
                    Vector3 dir = (waypoint - transform.position).normalized;
                    dir.y = 0;
                    if (dir != Vector3.zero)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), _rotationSpeed * Time.deltaTime);
                    
                    yield return null;
                }
            }
            IsMoving = false;
        }
    }
}