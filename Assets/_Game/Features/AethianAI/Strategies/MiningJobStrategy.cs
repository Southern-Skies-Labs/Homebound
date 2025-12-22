using UnityEngine;
using Homebound.Features.TaskSystem;
using Homebound.Features.VoxelWorld;

namespace Homebound.Features.AethianAI.Strategies
{
    public class MiningJobStrategy : IJobStrategy
    {
        private float _workTimer;
        private const float INTERACTION_DISTANCE = 2.0f; // Distancia para empezar a picar
        private const float WORK_DURATION = 2.0f;

        public void Execute(AethianBot bot, float deltaTime)
        {
            JobRequest job = bot.CurrentJob;
            if (job == null) return;

            // Calculamos distancia ignorando altura (para evitar problemas con pivotes)
            float distance = Vector3.Distance(new Vector3(bot.Position.x, 0, bot.Position.z),
                                              new Vector3(job.Position.x, 0, job.Position.z));

            // 1. ¿Estamos lo suficientemente cerca para trabajar?
            if (distance < INTERACTION_DISTANCE)
            {
                // FRENAR: Si nos estábamos moviendo, paramos para trabajar
                if (!bot.HasReachedDestination())
                    bot.StopMoving();

                _workTimer += deltaTime;

                // Feedback visual: Mirar al objetivo
                Vector3 dir = (job.Position - bot.Position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                    bot.transform.rotation = Quaternion.LookRotation(dir);

                // Terminar trabajo
                if (_workTimer >= WORK_DURATION)
                {
                    PerformMining(job.Position);
                    bot.CompleteCurrentJob();
                    _workTimer = 0f;
                }
            }
            else
            {
                _workTimer = 0f;

                // 2. MOVERSE: Solo si NO nos estamos moviendo ya.
                // HasReachedDestination devuelve true si el bot está parado.
                if (bot.HasReachedDestination())
                {
                    // Intentamos ir al objetivo. 
                    // NOTA: Si el pathfinding falla porque el destino es sólido, el bot se quedará quieto.
                    // Idealmente tu UnitMovement debería buscar el "vecino más cercano" si el destino es sólido.
                    bot.MoveTo(job.Position);
                }
            }
        }

        public void OnCancel(AethianBot bot)
        {
            _workTimer = 0f;
            bot.StopMoving();
        }

        private void PerformMining(Vector3 targetPos)
        {
            Vector3 checkPos = targetPos + new Vector3(0.5f, 0.5f, 0.5f);
            Collider[] hits = Physics.OverlapSphere(checkPos, 0.8f, LayerMask.GetMask("Terrain"));

            if (hits != null && hits.Length > 0)
            {
                Chunk targetChunk = null;
                foreach (var hit in hits)
                {
                    targetChunk = hit.GetComponent<Chunk>();
                    if (targetChunk != null) break;
                }

                if (targetChunk != null)
                {
                    Debug.Log($"[MiningStrategy] Rompiendo bloque en {targetPos}");
                    targetChunk.DestroyBlockAtWorldPos(targetPos);
                }
            }
        }
    }
}