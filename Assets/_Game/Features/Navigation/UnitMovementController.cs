using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation.FailSafe;

namespace Homebound.Features.Navigation
{
    // Aseguramos que tenga Collider y Rigidbody para interactuar con el mundo
    [RequireComponent(typeof(CapsuleCollider))] // O BoxCollider
    [RequireComponent(typeof(Rigidbody))] 
    public class UnitMovementController : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private float _rotationSpeed = 12.0f;
        [SerializeField] private float _collisionCheckDist = 0.6f; // Distancia de frenado ante muros

        private PathfindingService _pathfindingService;
        private Coroutine _moveCoroutine;
        public bool IsMoving { get; private set; }
        private Rigidbody _rb;
        private StuckMonitor _stuckMonitor;

        private void Awake()
        {
            _pathfindingService = ServiceLocator.Get<PathfindingService>();
            _rb = GetComponent<Rigidbody>();
            
            _stuckMonitor = GetComponent<StuckMonitor>();
            
            // Configuración física crítica para evitar comportamientos raros
            _rb.isKinematic = true; // Nos movemos por script, no por gravedad/física pura
            _rb.useGravity = false;
        }
        
        // private void Update()
        // {
        //     // Si no nos estamos moviendo activamente por una ruta...
        //     if (!IsMoving)
        //     {
        //         ApplyGravity();
        //     }
        // }
        //
        // private void ApplyGravity()
        // {
        //     // Raycast hacia abajo para ver si hay suelo
        //     // Origen: Un poco arriba de los pies
        //     Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        //
        //     // Buscamos suelo a corta distancia (0.2f = 0.1 origen + 0.1 tolerancia)
        //     if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.2f))
        //     {
        //         // ¡No hay suelo! Caemos.
        //         // Usamos una velocidad de caída fija o acumulativa
        //         float fallSpeed = 5.0f; 
        //         transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        //     
        //         // Opcional: Alinear al centro de la celda mientras cae para evitar quedar en bordes
        //         // (Matemática de voxel centering)
        //     }
        // }

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
                Debug.LogWarning($"[UnitMovement] No se encontró camino a {targetPosition}");
                IsMoving = false;

                // --- AÑADIR ESTO ---
                // Si no hay camino, avisamos al monitor que estamos "Lógicamente Atascados"
                if (_stuckMonitor != null)
                {
                    _stuckMonitor.ReportPathfindingFailure();
                }
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
            int currentWaypointIndex = 0;

            while (currentWaypointIndex < path.Count)
            {
                Vector3 waypoint = path[currentWaypointIndex];
                Vector3 targetPos = waypoint; 

                // --- PARACHOQUES INTELIGENTE (Rodilla vs Cabeza) ---
                Vector3 kneeOrigin = transform.position + Vector3.up * 0.5f; 
                Vector3 headOrigin = transform.position + Vector3.up * 1.6f; // Altura visual de un humanoide
                Vector3 direction = (targetPos - transform.position).normalized;

                bool movingHorizontally = Mathf.Abs(direction.y) < 0.5f;

                if (direction != Vector3.zero && movingHorizontally)
                {
                    bool hitKnee = Physics.Raycast(kneeOrigin, direction, out RaycastHit kneeHit, _collisionCheckDist);
                    bool hitHead = Physics.Raycast(headOrigin, direction, out RaycastHit headHit, _collisionCheckDist);

                    if (hitKnee)
                    {
                        // 1. Filtro Básico: Ignorar Triggers y a mí mismo
                        if (kneeHit.collider.isTrigger || kneeHit.collider.transform == transform) 
                        {
                            // Ignorar
                        }
                        // 2. NUEVO FILTRO: Ignorar Estructuras Trepables (Escaleras)
                        else if (kneeHit.collider.GetComponentInParent<Homebound.Features.Navigation.FailSafe.ClimbableStructure>() != null)
                        {
                            // ¡Es una escalera! La ignoramos y seguimos caminando (trepando)
                        }
                        else
                        {
                            // Si chocamos rodillas y NO es escalera...
                            // Verificamos cabeza
                            if (hitHead && !headHit.collider.isTrigger)
                            {
                                // Chequeo doble: ¿Lo de arriba también es escalera?
                                if (headHit.collider.GetComponentInParent<Homebound.Features.Navigation.FailSafe.ClimbableStructure>() == null)
                                {
                                    // Chocamos arriba y abajo, y NO es escalera -> ES UN MURO.
                                    yield return null; 
                                    continue; 
                                }
                            }
                        }
                    }
                }

                // Movimiento
                if (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    Vector3 lookDir = new Vector3(direction.x, 0, direction.z);
                    if (lookDir != Vector3.zero)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, _rotationSpeed * Time.deltaTime);
                    }
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                }
                else
                {
                    currentWaypointIndex++;
                }

                yield return null;
            }
            IsMoving = false;
        }
    }
}