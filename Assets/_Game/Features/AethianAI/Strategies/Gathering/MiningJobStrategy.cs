using UnityEngine;
using Homebound.Features.TaskSystem;
using Homebound.Features.VoxelWorld;

namespace Homebound.Features.AethianAI.Strategies.Gathering
{
    public class MiningJobStrategy : IGatherStrategy
    {
        private float _workTimer;
        private const float WORK_DURATION = 1.0f; // Tiempo por golpe
        private const float INTERACTION_RANGE = 2.0f;

        public bool IsJobValid(JobRequest job)
        {
            // Validar si el chunk y bloque existen
            // Simplificación: Asumimos que si el trabajo existe, es válido hasta que falle
            return true;
        }

        public Vector3 GetWorkPosition(JobRequest job, Vector3 botPosition)
        {
            // Para minería de voxel, queremos ir a la celda adyacente más cercana, no dentro del bloque.
            // Si el pathfinding es inteligente, MoveTo(job.Position) fallaría o se acercaría lo máximo posible.
            // Retornamos la posición del bloque, confiando en que UnitMovementController se detendrá al llegar al rango de interacción.
            return job.Position;
        }

        public bool ExecuteWork(AethianBot bot, float deltaTime)
        {
            JobRequest job = bot.CurrentJob;
            if (job == null) return true;

            // Feedback visual: Mirar al objetivo
            Vector3 dir = (job.Position - bot.Position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                bot.transform.rotation = Quaternion.LookRotation(dir);

            _workTimer += deltaTime;
            if (_workTimer >= WORK_DURATION)
            {
                _workTimer = 0f;
                return PerformMiningHit(job.Position, bot);
            }

            return false;
        }

        public void OnCancel(AethianBot bot)
        {
            _workTimer = 0f;
        }

        private bool PerformMiningHit(Vector3 targetPos, AethianBot bot)
        {
            // Buscamos el bloque en esa posición mundial
            // Usamos un pequeño offset al centro del bloque para asegurar hit
            Vector3 checkPos = targetPos + new Vector3(0.5f, 0.5f, 0.5f);

            // Lógica de VoxelWorld: Encontrar Chunk y destruir
            Collider[] hits = Physics.OverlapSphere(checkPos, 0.1f, LayerMask.GetMask("Terrain", "Default"));

            Chunk targetChunk = null;
            foreach (var hit in hits)
            {
                targetChunk = hit.GetComponent<Chunk>();
                if (targetChunk != null) break;
            }

            if (targetChunk != null)
            {
                // Daño / Destrucción
                // Aquí asumimos destrucción inmediata por simplicidad o usamos lógica de daño si existe
                targetChunk.DestroyBlockAtWorldPos(targetPos);

                // Si había inventario (drop), simular recolección (Opcional, si el bloque suelta items)
                // TODO: Implementar recolección de drops de minería si el sistema de voxeles los genera

                return true; // Trabajo completado (bloque roto)
            }
            else
            {
                // Si no encontramos chunk, el bloque ya no está o es error. Terminamos.
                return true;
            }
        }
    }
}
