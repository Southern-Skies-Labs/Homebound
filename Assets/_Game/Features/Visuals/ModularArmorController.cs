using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.Visuals
{
    public class ModularArmorController : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("El hueso raíz del esqueleto PRINCIPAL del personaje (generalmente Hips o la raíz de la armadura)")]
        [SerializeField] private Transform _mainRootBone;

        // Cache para buscar huesos rápidamente por nombre (ej: "Forearm_L" -> Transform real)
        private Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();

        // Rastreo de la armadura actual para poder quitársela antes de poner otra
        private GameObject _currentArmorInstance;

        //Variable Temporal
        public GameObject DebugArmorPrefab;

        private void Awake()
        {
            if (_mainRootBone == null)
            {
                // Intento automático de encontrar el root si es un setup estándar
                Animator anim = GetComponent<Animator>();
                if (anim != null) _mainRootBone = anim.transform; // Fallback, idealmente asignar manual
            }

            // Mapeamos TODOS los huesos del personaje original al iniciar
            MapBones(_mainRootBone);
        }

        /// <summary>
        /// Recorre recursivamente el esqueleto y guarda referencias por nombre.
        /// </summary>
        private void MapBones(Transform current)
        {
            if (!_boneMap.ContainsKey(current.name))
            {
                _boneMap.Add(current.name, current);
            }

            foreach (Transform child in current)
            {
                MapBones(child);
            }
        }

        /// <summary>
        /// Equipa una pieza de armadura usando la técnica de Reskinning.
        /// </summary>
        /// <param name="armorPrefab">Prefab que contiene SOLO el SkinnedMeshRenderer de la ropa</param>
        public void EquipArmor(GameObject armorPrefab)
        {
            // 1. Limpieza
            if (_currentArmorInstance != null) Destroy(_currentArmorInstance);
            if (armorPrefab == null) return;

            // 2. Instanciar la ropa (desactivada para que no parpadee al ajustar)
            _currentArmorInstance = Instantiate(armorPrefab, transform);
            _currentArmorInstance.SetActive(false);

            // 3. Obtener el Renderer de la ropa
            var armorRenderer = _currentArmorInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (armorRenderer == null)
            {
                Debug.LogError($"[ModularArmorController] El prefab {armorPrefab.name} no tiene SkinnedMeshRenderer.");
                return;
            }

            // 4. EL TRUCO DE MAGIA (Reskinning)
            // Creamos un nuevo array de huesos que coincida con lo que la ropa espera,
            // pero apuntando a los huesos REALES de este personaje.
            Transform[] newBones = new Transform[armorRenderer.bones.Length];

            for (int i = 0; i < armorRenderer.bones.Length; i++)
            {
                string boneName = armorRenderer.bones[i].name;

                if (_boneMap.TryGetValue(boneName, out Transform realBone))
                {
                    newBones[i] = realBone;
                }
                else
                {
                    Debug.LogWarning($"[ModularArmorController] La ropa busca el hueso '{boneName}' pero el personaje no lo tiene.");
                    // Fallback: Asignar al root para evitar errores críticos, aunque se verá raro
                    newBones[i] = _mainRootBone;
                }
            }

            // 5. Asignar los nuevos huesos y encender
            armorRenderer.bones = newBones;
            armorRenderer.rootBone = _boneMap.ContainsKey(armorRenderer.rootBone.name) ? _boneMap[armorRenderer.rootBone.name] : _mainRootBone;

            _currentArmorInstance.SetActive(true);
            Debug.Log($"[ModularArmorController] Armadura {armorPrefab.name} equipada y sincronizada.");
        }

        // Método de prueba para usar desde el Inspector (Menú contextual)
        [ContextMenu("Test Equip Armor (Assign Prefab First)")]
        public void TestEquip()
        {
            EquipArmor(DebugArmorPrefab);
        }
    }
}