using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.Visuals
{
    public class ModularArmorController : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Si lo dejas vacío, el script intentará encontrar 'Armature/Hips' o 'Hips' automáticamente.")]
        [SerializeField] private Transform _mainRootBone;

        [Header("Debug")]
        [SerializeField] private GameObject _debugArmorToEquip;

        // Cache de huesos del cuerpo base
        private Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();
        private GameObject _currentArmorInstance;

        private void Awake()
        {
            // --- AUTO-CORRECCIÓN DE REFERENCIA ---
            if (_mainRootBone == null)
            {
                // Intentamos encontrar el hueso raíz por nombre común
                _mainRootBone = transform.Find("Armature/Hips"); // Estructura Blender típica

                if (_mainRootBone == null)
                    _mainRootBone = transform.Find("Hips"); // Estructura directa

                if (_mainRootBone == null)
                    _mainRootBone = transform.Find("Armature"); // Solo Armature

                if (_mainRootBone == null)
                {
                    Debug.LogError($"[ModularArmorController] ¡CRÍTICO! No encuentro el hueso raíz en {name}. Asígnalo manual o revisa nombres.");
                    return; // Abortar para evitar errores peores
                }
            }
            // -------------------------------------

            // Mapear el esqueleto base al iniciar
            MapBones(_mainRootBone);
        }

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

        public void EquipArmor(GameObject armorPrefab)
        {
            // 1. Limpieza
            if (_currentArmorInstance != null) Destroy(_currentArmorInstance);
            if (armorPrefab == null) return;

            // 2. Instanciación SEGURA
            _currentArmorInstance = Instantiate(armorPrefab, null);
            _currentArmorInstance.transform.SetParent(transform);

            // 3. Resetear Transformaciones
            _currentArmorInstance.transform.localPosition = Vector3.zero;
            _currentArmorInstance.transform.localRotation = Quaternion.identity;
            _currentArmorInstance.transform.localScale = Vector3.one;

            // 4. Reskinning
            var renderers = _currentArmorInstance.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (renderers.Length == 0) Debug.LogWarning($"[ModularArmorController] {armorPrefab.name} no tiene Renderer.");

            foreach (var renderer in renderers)
            {
                Transform[] newBones = new Transform[renderer.bones.Length];

                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    Transform bone = renderer.bones[i];

                    // Verificación extra: si el hueso es null en la lista original
                    if (bone != null && _boneMap.TryGetValue(bone.name, out Transform realBone))
                    {
                        newBones[i] = realBone;
                    }
                    else
                    {
                        newBones[i] = _mainRootBone;
                    }
                }

                renderer.bones = newBones;

                // --- CORRECCIÓN DEL ERROR ---
                // Verificamos si rootBone existe antes de pedir su nombre.
                if (renderer.rootBone != null && _boneMap.ContainsKey(renderer.rootBone.name))
                {
                    renderer.rootBone = _boneMap[renderer.rootBone.name];
                }
                else
                {
                    // Si venía en 'None' o no encontramos el nombre, asignamos el principal.
                    renderer.rootBone = _mainRootBone;
                }
            }
        }

        [ContextMenu("Debug Equip Armor")]
        public void DebugEquip()
        {
            if (_debugArmorToEquip != null)
            {
                EquipArmor(_debugArmorToEquip);
            }
            else
            {
                Debug.LogWarning("Asigna una ropa en 'Debug Armor To Equip' antes de probar.");
            }
        }
    }
}