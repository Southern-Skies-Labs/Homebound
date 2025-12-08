using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation;

namespace Homebound.Features.VoxelWorld
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Settings - Map")]
        [SerializeField] private int _mapSize = 50;
        [SerializeField] private int _mapHeight = 20;
        [SerializeField] private Chunk _chunkPrefab;

        [Header("Settings - Vegetation")]
        [SerializeField] private GameObject _treePrefab;
        [SerializeField] private int _treeCount = 10;
        [SerializeField] private int _seed = 12345; // Semilla para resultados consistentes
        [SerializeField] private Transform _resourceContainer; // Carpeta para organizarlos

        private GridManager _gridManager;

        private void Start()
        {
            // 1. Configurar Semilla (Determinismo)
            Random.InitState(_seed);

            // 2. Inicializar Sistemas
            _gridManager = ServiceLocator.Get<GridManager>();
            if (_gridManager == null)
            {
                Debug.LogError("[WorldGenerator] CRÍTICO: GridManager no encontrado.");
                return;
            }

            InitializeNavigationGrid();
            CreateChunk();
            
            // 3. Poblar el Mundo (NUEVO)
            GenerateVegetation();
        }
        
        private void InitializeNavigationGrid()
        {
            // Inicializamos la matriz lógica
            _gridManager.InitializeGrid(_mapSize, _mapHeight, _mapSize);
        }

        private void CreateChunk()
        {
            Chunk chunk = GetComponentInChildren<Chunk>();
            if (chunk == null)
            {
                chunk = Instantiate(_chunkPrefab, transform);
            }
            
            // El Chunk genera la mesh y notifica al Grid qué es suelo y qué es aire
            chunk.Initialize(_mapSize, _mapHeight, _mapSize);
        }

        private void GenerateVegetation()
        {
            if (_treePrefab == null) return;

            int placedTrees = 0;
            int attempts = 0;
            int maxAttempts = _treeCount * 5; // Evitar bucles infinitos

            // Carpeta organizadora
            if (_resourceContainer == null)
            {
                var containerObj = new GameObject("Resources_Container");
                containerObj.transform.SetParent(this.transform);
                _resourceContainer = containerObj.transform;
            }

            Debug.Log($"[WorldGenerator] Iniciando plantación de {_treeCount} árboles...");

            while (placedTrees < _treeCount && attempts < maxAttempts)
            {
                attempts++;

                // 1. Coordenada Aleatoria (Considerando el offset del mundo, ej: -25 a 25)
                int halfSize = _mapSize / 2;
                int x = Random.Range(-halfSize, halfSize);
                int z = Random.Range(-halfSize, halfSize);

                // 2. Buscar Suelo (Raycast Lógico Vertical)
                // Empezamos desde arriba y bajamos hasta encontrar suelo
                for (int y = _mapHeight - 2; y > 0; y--)
                {
                    PathNode floorNode = _gridManager.GetNode(x, y, z);
                    PathNode airNode = _gridManager.GetNode(x, y + 1, z);

                    // Validamos: Abajo no es aire (es suelo), Arriba es aire (espacio libre)
                    // Nota: floorNode.Type != NodeType.Air asume que el Chunk ya marcó el suelo como Solid
                    if (floorNode != null && floorNode.Type != NodeType.Air && 
                        airNode != null && airNode.Type == NodeType.Air)
                    {
                        // ¡Sitio válido encontrado!
                        SpawnTree(x, y + 1, z); // Spawnear ENCIMA del suelo
                        placedTrees++;
                        break; // Salir del bucle Y, ir al siguiente árbol
                    }
                }
            }
            
            Debug.Log($"[WorldGenerator] Vegetación generada: {placedTrees}/{_treeCount} árboles.");
        }

        private void SpawnTree(int x, int y, int z)
        {
            Vector3 position = new Vector3(x, y, z);
            
            // Instanciar
            GameObject newTree = Instantiate(_treePrefab, position, Quaternion.identity, _resourceContainer);
            
            // Importante: Aleatorizar rotación (solo eje Y) para variedad visual
            float randomYRot = Random.Range(0, 4) * 90f; // Rotaciones de 90 grados estilo voxel
            newTree.transform.rotation = Quaternion.Euler(0, randomYRot, 0);

            // ACTUALIZACIÓN DE NAVGRID:
            // El tronco del árbol ocupa un espacio, los bots no deben caminar a través de él.
            // Marcamos ese nodo como "Solid" (Obstáculo) en la lógica de navegación.
            _gridManager.SetNode(x, y, z, NodeType.Solid);
        }
    }
}