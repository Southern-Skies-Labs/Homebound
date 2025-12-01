using System.Collections.Generic;
using UnityEngine;

namespace Homebound.Features.Navigation
{
    [RequireComponent(typeof(CharacterController))]
    public class GridMovementAgent : MonoBehaviour
    {
        [Header("Settings")]
        public float MoveSpeed = 5.0f;
        public float RotationSpeed = 10.0f;
        public float StopDistance = 0.5f;

        private CharacterController _controller;
        private List<Vector3> _path;
        private int _currentWaypointIndex = 0;
        private bool _isMoving = false;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void SetPath(List<Vector3> newPath)
        {
            if (newPath == null || newPath.Count == 0) return;

            _path = newPath;
            _currentWaypointIndex = 0;
            _isMoving = true;
        }

        public void Stop()
        {
            _isMoving = false;
            _path = null;
        }

        public bool HasReachedDestination()
        {
            if (_path == null || _path.Count == 0) return true;
            if (!_isMoving) return true;

            // Si estamos en el último waypoint y cerca
            if (_currentWaypointIndex >= _path.Count - 1)
            {
                 float dist = Vector3.Distance(transform.position, _path[_path.Count - 1]);
                 return dist <= StopDistance;
            }
            return false;
        }

        private void Update()
        {
            if (!_isMoving || _path == null || _currentWaypointIndex >= _path.Count) return;

            Vector3 target = _path[_currentWaypointIndex];
            // Ignoramos Y para la distancia plana si queremos movernos primero en XZ
            Vector3 direction = (target - transform.position);
            direction.y = 0; // Solo rotar en plano XZ

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * RotationSpeed);
            }

            // Movimiento
            // Recalculamos dirección completa hacia el objetivo (incluyendo Y para rampas/escalones suaves)
            // Pero como es Voxel, el CharacterController tiene StepOffset.
            // Simplemente nos movemos hacia el target.

            Vector3 moveDir = (target - transform.position).normalized;

            // Gravedad simple
            Vector3 velocity = moveDir * MoveSpeed;
            velocity.y += Physics.gravity.y * Time.deltaTime; // Añadir gravedad

            // Como CharacterController.SimpleMove aplica gravedad, Move requiere manual.
            // Para simplicidad en steps:
            // Vamos a movernos hacia el punto. Si el punto está arriba, el StepOffset debería encargarse si es pequeño.
            // Si es un "Salto" (1 bloque), necesitamos que el target esté arriba.

            // Ajuste fino: Si estamos muy cerca en XZ del target, pasamos al siguiente
            float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(target.x, target.z));

            if (distXZ < 0.5f)
            {
                // Si la altura es diferente, aseguremos que hemos llegado en Y también o que el CC lo maneja
                if (Mathf.Abs(transform.position.y - target.y) < 1.1f) // Margen de 1 bloque
                {
                    _currentWaypointIndex++;
                }
            }

            // Aplicar movimiento
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}
