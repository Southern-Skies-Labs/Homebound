using UnityEngine;
using Homebound.Features.AethianAI;

namespace Homebound.Features.Visuals
{
    [RequireComponent(typeof(BoneSocketManager))]
    [RequireComponent(typeof(AethianBot))]
    public class ToolVisualizer : MonoBehaviour
    {
        [SerializeField] private ItemVisualDatabase _database;

        private BoneSocketManager _sockets;
        private AethianBot _bot;

        private void Awake()
        {
            _sockets = GetComponent<BoneSocketManager>();
            _bot = GetComponent<AethianBot>();

            if (_database != null) _database.Initialize();
        }

        private void OnEnable()
        {
            _bot.OnStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            _bot.OnStateChanged -= HandleStateChange;
        }

        private void HandleStateChange(string stateName)
        {
            // Lógica simple: Si trabajamos, buscamos si hay herramienta para ese estado.
            // Si salimos de trabajar (Idle, Sleep), escondemos todo.

            // Ejemplo de convención: Si el estado es "Working", necesitamos saber QUÉ trabajo.
            // Como AethianBot actual no expone el "CurrentJobType" en el evento string,
            // usaremos una convención temporal o lógica directa.

            if (stateName == "Idle" || stateName == "Sleep" || stateName == "Survival")
            {
                _sockets.ClearSocket(SocketType.MainHand);
                return;
            }

            if (stateName == "Working")
            {
                // TODO: Aquí deberíamos leer del AethianBot qué trabajo específico tiene.
                // Por ahora, para probar, forzaremos "Job_Mine" si sabemos que es minero, 
                // o lo dejaremos genérico.

                // Opción A: Intentar obtener herramienta por nombre del estado (si creas estados como "Working_Mine")
                TryEquipTool(stateName);

                // Opción B (Mejor para futuro): Leer propiedad pública del Bot (CurrentJobId)
                // string jobId = _bot.GetCurrentJobId(); // Requeriría editar AethianBot
            }

            // Intenta equipar algo si el nombre del estado coincide con una herramienta en la DB
            TryEquipTool(stateName);
        }

        private void TryEquipTool(string id)
        {
            if (_database.TryGetVisual(id, out GameObject prefab, out SocketType socket))
            {
                _sockets.Mount(prefab, socket);
            }
        }
    }
}