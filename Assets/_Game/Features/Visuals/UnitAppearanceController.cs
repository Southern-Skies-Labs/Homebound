using UnityEngine;

namespace Homebound.Features.Visuals
{
    [RequireComponent(typeof(BoneSocketManager))]
    public class UnitAppearanceController : MonoBehaviour
    {
        [Header("Base Data")]
        [SerializeField] private StyleDatabase _database;

        [Header("Target Renderers")]
        [Tooltip("Arrastra aquí los OBJETOS (GameObjects) de las partes del cuerpo (Cabeza, Torso, etc.). El script buscará sus renderers automáticamente.")]
        [SerializeField] private GameObject[] _bodyPartObjects; // <--- CAMBIO: Ahora es GameObject (Acepta todo)

        private BoneSocketManager _sockets;

        private void Awake()
        {
            _sockets = GetComponent<BoneSocketManager>();
        }

        private void Start()
        {
            // Opcional: Validar al inicio si falta algún renderer
            ValidateRenderers();

            RandomizeAppearance();
        }

        [ContextMenu("Randomize Appearance")]
        public void RandomizeAppearance()
        {
            if (_database == null) return;

            // 1. Aplicar Piel
            Material randomSkin = _database.GetRandomSkin();

            if (randomSkin != null && _bodyPartObjects != null)
            {
                foreach (var obj in _bodyPartObjects)
                {
                    if (obj == null) continue;

                    // Buscamos el Renderer (Skinned o Mesh) en el objeto
                    Renderer r = obj.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = randomSkin;
                    }
                    else
                    {
                        // Intento de seguridad: buscar en hijos si arrastraste un padre vacío
                        r = obj.GetComponentInChildren<Renderer>();
                        if (r != null) r.sharedMaterial = randomSkin;
                    }
                }
            }

            // 2. Colocar y Colorear Ojos
            GameObject eyePrefab = _database.GetRandomEyes();
            if (eyePrefab != null)
            {
                GameObject eyeInstance = _sockets.Mount(eyePrefab, SocketType.FaceEyes);
                ApplyMaterialTo(eyeInstance, _database.GetRandomEyeColor());
            }

            // 3. Colocar y Colorear Cabello
            GameObject hairPrefab = _database.GetRandomHair();
            if (hairPrefab != null)
            {
                GameObject hairInstance = _sockets.Mount(hairPrefab, SocketType.HeadTop);
                ApplyMaterialTo(hairInstance, _database.GetRandomHairColor());
            }
        }

        private void ApplyMaterialTo(GameObject obj, Material mat)
        {
            if (obj == null || mat == null) return;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }

        private void ValidateRenderers()
        {
            if (_bodyPartObjects == null) return;
            foreach (var obj in _bodyPartObjects)
            {
                if (obj != null && obj.GetComponent<Renderer>() == null)
                {
                    Debug.LogWarning($"[UnitAppearanceController] El objeto '{obj.name}' asignado en BodyParts NO tiene componente Renderer.");
                }
            }
        }
    }
}