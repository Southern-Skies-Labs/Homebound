using UnityEngine;
using System.Collections.Generic;

namespace Homebound.Features.Visuals
{
    public enum SocketType
    {
        MainHand,
        OffHand,
        Back,
        HeadTop,
        FaceEyes
    }

    public class BoneSocketManager : MonoBehaviour
    {
        [Header("Configuración de Huesos")]
        [SerializeField] private Transform _mainHandBone;
        [SerializeField] private Transform _offHandBone;
        [SerializeField] private Transform _backBone;
        [SerializeField] private Transform _headTopBone; 
        [SerializeField] private Transform _faceEyesBone; 


        // Cache de lo que tenemos equipado actualmente para poder borrarlo
        private Dictionary<SocketType, GameObject> _currentAttachments = new Dictionary<SocketType, GameObject>();

        public GameObject Mount(GameObject prefab, SocketType socketType)
        {
            ClearSocket(socketType); // Limpia lo anterior

            if (prefab == null) return null;

            Transform targetBone = GetBone(socketType);
            if (targetBone == null) return null;

            GameObject instance = Instantiate(prefab, targetBone);

            // Reset transforms
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            _currentAttachments[socketType] = instance;

            return instance;
        }

        public void ClearSocket(SocketType socketType)
        {
            if (_currentAttachments.TryGetValue(socketType, out GameObject current) && current != null)
            {
                Destroy(current);
                _currentAttachments.Remove(socketType);
            }
        }

        public void ClearAll()
        {
            ClearSocket(SocketType.MainHand);
            ClearSocket(SocketType.OffHand);
            ClearSocket(SocketType.Back);
        }

        private Transform GetBone(SocketType type)
        {
            switch (type)
            {
                case SocketType.MainHand: return _mainHandBone;
                case SocketType.OffHand: return _offHandBone;
                case SocketType.Back: return _backBone;
                case SocketType.HeadTop: return _headTopBone;
                case SocketType.FaceEyes: return _faceEyesBone;
                default: return null;
            }
        }

        // Utilidad para encontrar huesos automáticamente si es un Humanoid estándar
        [ContextMenu("Auto-Find Humanoid Bones")]
        private void AutoFindBones()
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null && anim.isHuman)
            {
                _mainHandBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
                _offHandBone = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                _backBone = anim.GetBoneTransform(HumanBodyBones.Chest);
                _headTopBone = anim.GetBoneTransform(HumanBodyBones.Head);
                _faceEyesBone = anim.GetBoneTransform(HumanBodyBones.Head);
                Debug.Log("Huesos encontrados automáticamente.");
            }
        }
    }
}