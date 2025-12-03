using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation;

namespace Homebound.Features.VoxelWorld
{
    public class WorldGenerator : MonoBehaviour
    {
        //Variables
        [Header("Settings")]
        [SerializeField] private int _mapSize = 50;
        [SerializeField] private int _mapHeight = 20;
        [SerializeField] private Chunk _chunkPrefab;

        
        //Metodos
        private void Start()
        {
            // InitializeNavigationGrid();
            CreateChunk();
        }
        
        private void InitializeNavigationGrid()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null)
            {
                gridManager.InitializeGrid(_mapSize, 20, _mapSize);
            }
            else
            {
                Debug.LogError("[WorldGenerator] GridManager no encontrado.");
            }
        }

        private void CreateChunk()
        {
            Chunk chunk = GetComponentInChildren<Chunk>();
            if (chunk == null)
            {
                Debug.LogError("No se encontró componente Chunk en WorldGenerator o hijos.");
                return;
            }

            
            chunk.Initialize(_mapSize, _mapHeight, _mapSize);
        }
    }
}