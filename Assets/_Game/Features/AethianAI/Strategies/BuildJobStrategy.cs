using UnityEngine;
using Homebound.Core;
using Homebound.Features.Construction;
using Homebound.Features.Economy;
using Homebound.Features.TaskSystem;
using Homebound.Features.Navigation; // Necesario para UnitMovementController

namespace Homebound.Features.AethianAI.Strategies
{
    public class BuildJobStrategy : IJobStrategy
    {
        private ConstructionSite _targetSite;
        private Transform _targetTransform;

        // Estado interno
        private bool _isFetchingMaterials;
        private StorageContainer _targetStorage;

        // FIX 1: Firma corregida para cumplir con IJobStrategy (añadido float deltaTime)
        public void Execute(AethianBot bot, float deltaTime)
        {
            // 1. Validación Inicial
            if (bot.CurrentJob == null || bot.CurrentJob.Target == null)
            {
                // FIX 2: Si no hay trabajo válido, simplemente salimos. 
                // El bot volverá a Idle en el siguiente ciclo del Brain.
                return;
            }

            // Cacheamos referencias
            if (_targetSite == null)
            {
                _targetSite = bot.CurrentJob.Target.GetComponent<ConstructionSite>();
                _targetTransform = bot.CurrentJob.Target;
            }

            // Si el sitio ya terminó, completamos el trabajo
            if (_targetSite == null || _targetSite.IsFinishedOrStalled())
            {
                // FIX 3: Asumo que CompleteJob existe (si no, usa bot.CurrentJob.Complete())
                bot.CompleteCurrentJob();
                return;
            }

            // FIX 4: Obtener componentes usando GetComponent (El bot es solo el contenedor)
            UnitInventory botInventory = bot.GetComponent<UnitInventory>();

            if (botInventory == null)
            {
                Debug.LogError($"[BuildStrategy] El bot {bot.name} no tiene UnitInventory!");
                return;
            }

            // 2. Determinar Necesidades
            ItemData neededItem = _targetSite.GetNextRequiredItem();
            bool hasMaterial = neededItem == null || botInventory.HasItem(neededItem, 1);

            // --- LÓGICA DE DECISIÓN ---

            if (!hasMaterial)
            {
                HandleFetching(bot, botInventory, neededItem);
            }
            else
            {
                HandleBuilding(bot, botInventory);
            }
        }

        // FIX 5: Implementación obligatoria de OnCancel
        public void OnCancel(AethianBot bot)
        {
            // Limpieza al cancelar: Detener movimiento y resetear variables
            if (bot != null)
            {
                var mover = bot.GetComponent<UnitMovementController>();
                if (mover != null) mover.StopMoving();

                // Resetear animación si es necesario
                var anim = bot.GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger("Idle");
            }

            _targetStorage = null;
            _isFetchingMaterials = false;
        }

        private void HandleFetching(AethianBot bot, UnitInventory botInventory, ItemData itemNeeded)
        {
            _isFetchingMaterials = true;

            if (_targetStorage == null || !_targetStorage.TryRetrieveItem(itemNeeded, 0, out _))
            {
                _targetStorage = FindNearestStorageWithItem(bot.transform.position, itemNeeded);

                if (_targetStorage == null)
                {
                    // FIX 6: Reemplazo de bot.StopWorking() por lógica de cancelación del Job
                    Debug.LogWarning($"[BuildStrategy] {bot.name} no encuentra {itemNeeded.name}. Cancelando trabajo.");
                    if (bot.CurrentJob != null) bot.CurrentJob.Cancel();
                    return;
                }
            }

            float dist = Vector3.Distance(bot.transform.position, _targetStorage.transform.position);
            if (dist > 2.0f)
            {
                bot.MoveTo(_targetStorage.transform.position);
            }
            else
            {
                bot.StopMoving();

                // Intentar sacar del almacén
                if (_targetStorage.TryRetrieveItem(itemNeeded, 1, out int retrievedAmount))
                {
                    botInventory.Add(itemNeeded, retrievedAmount);
                    _isFetchingMaterials = false;
                    _targetStorage = null;
                }
            }
        }

        private void HandleBuilding(AethianBot bot, UnitInventory botInventory)
        {
            _isFetchingMaterials = false;

            float dist = Vector3.Distance(bot.transform.position, _targetTransform.position);

            if (dist > 3.5f)
            {
                bot.MoveTo(_targetTransform.position);
            }
            else
            {
                bot.StopMoving();

                // FIX 7: Uso de transform.LookAt en lugar de bot.LookAt
                bot.transform.LookAt(_targetTransform.position);

                // FIX 8: Obtener Animator vía GetComponent
                Animator botAnim = bot.GetComponentInChildren<Animator>();

                // Construir
                bool success = _targetSite.ConstructBlock(botInventory);

                if (success)
                {
                    if (botAnim != null) botAnim.SetTrigger("Work");
                }
            }
        }

        private StorageContainer FindNearestStorageWithItem(Vector3 origin, ItemData item)
        {
            var allStorages = Object.FindObjectsByType<StorageContainer>(FindObjectsSortMode.None);
            StorageContainer best = null;
            float minDst = float.MaxValue;

            foreach (var store in allStorages)
            {
                if (store.TryRetrieveItem(item, 0, out _))
                {
                    float d = Vector3.Distance(origin, store.transform.position);
                    if (d < minDst)
                    {
                        minDst = d;
                        best = store;
                    }
                }
            }
            return best;
        }
    }
}