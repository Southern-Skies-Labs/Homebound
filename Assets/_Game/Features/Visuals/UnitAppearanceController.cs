using UnityEngine;

namespace Homebound.Features.Visuals
{
    [RequireComponent(typeof(BoneSocketManager))]
    [RequireComponent(typeof(ModularArmorController))]
    public class UnitAppearanceController : MonoBehaviour
    {
        [Header("Base Data")]
        [SerializeField] private StyleDatabase _database;

        [Header("Target Renderers")]
        [Tooltip("Arrastra aquí los GameObjects de las partes del cuerpo (Cabeza, Torso, etc)")]
        [SerializeField] private GameObject[] _bodyPartObjects;

        private BoneSocketManager _sockets;
        private ModularArmorController _armorController;

        private void Awake()
        {
            _sockets = GetComponent<BoneSocketManager>();
            _armorController = GetComponent<ModularArmorController>();
        }

        private void Start()
        {
            RandomizeAppearance();
        }

        [ContextMenu("Randomize Appearance")]
        public void RandomizeAppearance()
        {
            if (_database == null) return;

            // 1. Piel 
            Material randomSkin = _database.GetRandomSkin();
            ApplyMaterialToParts(_bodyPartObjects, randomSkin);

            // 2. Ojos 
            var eyeSettings = _database.GetRandomEyeSettings(); 
            if (eyeSettings != null && eyeSettings.prefab != null)
            {
                GameObject instance = _sockets.Mount(eyeSettings.prefab, SocketType.FaceEyes);

                if (instance != null)
                {
                    instance.transform.localPosition = eyeSettings.positionOffset;
                    instance.transform.localEulerAngles = eyeSettings.rotationOffset;
                    instance.transform.localScale = eyeSettings.scaleOverride;

                    ApplyMaterialTo(instance, _database.GetRandomEyeColor());
                }
            }

            // 3. Pelo 
            var hairSettings = _database.GetRandomHairSettings();
            if (hairSettings != null && hairSettings.prefab != null)
            {
                GameObject instance = _sockets.Mount(hairSettings.prefab, SocketType.HeadTop);

                if (instance != null)
                {
                    instance.transform.localPosition = hairSettings.positionOffset;
                    instance.transform.localEulerAngles = hairSettings.rotationOffset;
                    instance.transform.localScale = hairSettings.scaleOverride;

                    ApplyMaterialTo(instance, _database.GetRandomHairColor());
                }
            }

            // 4. Ropa
            GameObject randomOutfit = _database.GetRandomOutfit();
            if (randomOutfit != null && _armorController != null)
            {
                // AQUÍ ASUMO QUE TU ModularArmorController TIENE UN MÉTODO 'Equip'
                // Si se llama diferente, cambia el nombre aquí abajo.
                _armorController.EquipArmor(randomOutfit);
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
