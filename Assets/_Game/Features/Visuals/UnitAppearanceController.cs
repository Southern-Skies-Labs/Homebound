using UnityEngine;

namespace Homebound.Features.Visuals
{
    [RequireComponent(typeof(BoneSocketManager))]
    public class UnitAppearanceController : MonoBehaviour
    {
        [Header("Base Data")]
        [SerializeField] private StyleDatabase _database;

        [Header("Target Renderers")]
        [Tooltip("Arrastra aquí los GameObjects de las partes del cuerpo (Cabeza, Torso, etc)")]
        [SerializeField] private GameObject[] _bodyPartObjects;

        private BoneSocketManager _sockets;

        private void Awake()
        {
            _sockets = GetComponent<BoneSocketManager>();
        }

        private void Start()
        {
            RandomizeAppearance();
        }

        [ContextMenu("Randomize Appearance")]
        public void RandomizeAppearance()
        {
            if (_database == null) return;

            // 1. Piel (Igual que antes)
            Material randomSkin = _database.GetRandomSkin();
            ApplyMaterialToParts(_bodyPartObjects, randomSkin);

            // 2. Ojos (CON AJUSTE MANUAL)
            var eyeSettings = _database.GetRandomEyeSettings(); // Obtenemos la config, no solo el prefab
            if (eyeSettings != null && eyeSettings.prefab != null)
            {
                // Montamos el objeto
                GameObject instance = _sockets.Mount(eyeSettings.prefab, SocketType.FaceEyes);

                // --- APLICAMOS LA CORRECCIÓN FORZADA ---
                if (instance != null)
                {
                    instance.transform.localPosition = eyeSettings.positionOffset;
                    instance.transform.localEulerAngles = eyeSettings.rotationOffset;
                    instance.transform.localScale = eyeSettings.scaleOverride;

                    ApplyMaterialTo(instance, _database.GetRandomEyeColor());
                }
            }

            // 3. Pelo (CON AJUSTE MANUAL)
            var hairSettings = _database.GetRandomHairSettings();
            if (hairSettings != null && hairSettings.prefab != null)
            {
                GameObject instance = _sockets.Mount(hairSettings.prefab, SocketType.HeadTop);

                // --- APLICAMOS LA CORRECCIÓN FORZADA ---
                if (instance != null)
                {
                    instance.transform.localPosition = hairSettings.positionOffset;
                    instance.transform.localEulerAngles = hairSettings.rotationOffset;
                    instance.transform.localScale = hairSettings.scaleOverride;

                    ApplyMaterialTo(instance, _database.GetRandomHairColor());
                }
            }
        }

        private void ApplyMaterialToParts(GameObject[] objects, Material mat)
        {
            if (objects == null || mat == null) return;

            foreach (var obj in objects)
            {
                if (obj == null) continue;
                Renderer r = obj.GetComponent<Renderer>();
                if (r == null) r = obj.GetComponentInChildren<Renderer>(); 

                if (r != null) r.sharedMaterial = mat;
            }
        }

        private void ApplyMaterialTo(GameObject rootInstance, Material mat)
        {
            if (rootInstance == null || mat == null) return;

            // Aplicar a todos los renderers dentro del objeto (útil para prefabs compuestos)
            Renderer[] renderers = rootInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }
    }
}