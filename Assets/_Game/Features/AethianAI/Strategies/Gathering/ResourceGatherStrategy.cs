using UnityEngine;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy; // Para ResourceNode, UnitInventory

namespace Homebound.Features.AethianAI.Strategies.Gathering
{
    public class ResourceGatherStrategy : IGatherStrategy
    {
        private float _gatherTimer;
        private const float GATHER_INTERVAL = 1.0f;

        private ResourceNode _cachedNode;

        public bool IsJobValid(JobRequest job)
        {
            if (job.Target == null) return false;
            if (_cachedNode == null) _cachedNode = job.Target.GetComponent<ResourceNode>();

            return _cachedNode != null && !_cachedNode.IsDepleted;
        }

        public Vector3 GetWorkPosition(JobRequest job, Vector3 botPosition)
        {
            // Vamos a la posición del transform del recurso
            return job.Target.position;
        }

        public bool ExecuteWork(AethianBot bot, float deltaTime)
        {
            if (_cachedNode == null) return true; // Error, terminar

            // Feedback visual
            Vector3 lookTarget = _cachedNode.transform.position;
            lookTarget.y = bot.Position.y;
            bot.transform.LookAt(lookTarget);

            _gatherTimer += deltaTime;
            if (_gatherTimer >= GATHER_INTERVAL)
            {
                _gatherTimer = 0f;
                return PerformGatherHit(bot);
            }

            return false;
        }

        public void OnCancel(AethianBot bot)
        {
            _gatherTimer = 0f;
            _cachedNode = null;
        }

        private bool PerformGatherHit(AethianBot bot)
        {
            float damage = bot.Stats.GatheringPower;
            int amount = _cachedNode.Gather(damage);

            if (amount > 0)
            {
                if (bot.TryGetComponent(out UnitInventory inventory))
                {
                    ItemData itemType = _cachedNode.GetDrop().Item;
                    inventory.Add(itemType, amount);
                }
            }

            return _cachedNode.IsDepleted;
        }
    }
}
