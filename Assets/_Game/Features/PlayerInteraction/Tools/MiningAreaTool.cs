using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.VoxelWorld;

namespace Homebound.Features.PlayerInteraction.Tools
{
    public class MiningAreaTool : AreaToolBase
    {
        private JobManager _jobManager;
        private UnitClassDefinition _requiredWorker; // Referencia opcional si necesitamos

        public MiningAreaTool(InteractionController controller, LayerMask groundLayer, UnitClassDefinition workerClass)
            : base(controller, groundLayer)
        {
            _requiredWorker = workerClass;
            // Ghost color rojo para minería
            if (_ghostBox != null)
                _ghostBox.GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 0.3f);
        }

        public override void EnterTool()
        {
            base.EnterTool();
            _jobManager = ServiceLocator.Get<JobManager>();
            Debug.Log("[MiningAreaTool] Herramienta de Minería Activada. Arrastra para seleccionar zona.");
        }

        protected override void ExecuteSelection(Vector3 start, Vector3 end)
        {
            // Determinar min/max bounds
            Vector3 min = Vector3.Min(start, end);
            Vector3 max = Vector3.Max(start, end);

            int startX = Mathf.FloorToInt(min.x);
            int startY = Mathf.FloorToInt(min.y);
            int startZ = Mathf.FloorToInt(min.z);

            int endX = Mathf.FloorToInt(max.x);
            int endY = Mathf.FloorToInt(max.y);
            int endZ = Mathf.FloorToInt(max.z);

            int jobsCreated = 0;

            // Recorrer el volumen seleccionado
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = startZ; z <= endZ; z++)
                    {
                        Vector3 blockCenter = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);

                        // Validar si hay bloque sólido que minar
                        if (HasSolidBlock(blockCenter))
                        {
                            CreateMiningJob(blockCenter);
                            jobsCreated++;
                        }
                    }
                }
            }

            Debug.Log($"[MiningAreaTool] Selección completada. {jobsCreated} trabajos de minería creados.");
        }

        private bool HasSolidBlock(Vector3 pos)
        {
            // Pequeño check físico.
            // En un sistema Voxel puro consultaríamos VoxelMapService.GetBlock(x,y,z).
            // Por ahora usamos física como proxy robusto.
            return Physics.CheckSphere(pos, 0.4f, _groundLayer);
        }

        private void CreateMiningJob(Vector3 pos)
        {
            // Ajustamos pos al entero del grid si el JobManager lo espera así,
            // pero el JobRequest suele tomar Vector3 world pos.
            // Le pasamos el centro del bloque.

            // Verificamos duplicados en JobManager si fuera necesario,
            // pero JobManager debería manejarlo o permitimos cola.

            JobRequest miningJob = new JobRequest(
                "Mine Area",
                JobType.Mine,
                new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z)),
                null,
                50,
                _requiredWorker
            );

            _jobManager.PostJob(miningJob);
        }
    }
}
