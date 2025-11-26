using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Homebound.Core.Inputs;

namespace Homebound.Features.CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        //Variables

        [Header("Referencias del Rig")]
        [SerializeField] private Transform _rigRoot;
        [SerializeField] private Transform _pivot;
        [SerializeField] private Transform _target;
        
        [Header("Settings de movimiento")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotationSpeed = 100f;
        [SerializeField] private float _smoothTime = 0.1f;
        
        [Header("Settings Zoom Hibrido")]
        [SerializeField] private float _zoomStep = 5f;
        [SerializeField] private float _minZoomDist = 2f;
        [SerializeField] private float _maxZoomDist = 30f;
        [SerializeField] private float _zoomDamping = 5f;

        [Header("Transición inmersiva")] [SerializeField]
        private AnimationCurve _pitchCurve;

        private RTSInputs _input;
        private Vector3 _targetPos;
        private Quaternion _targetRot;
        private float _currentZoomDist;
        private float _targetZoomDist;
        private Vector3 _refVelocityPos;
        
        
        //Metodos
        private void Awake()
        {
            _input = new RTSInputs();
            
            //Inicializamos objetivos con la posicón actual del rig de la escena
            _targetPos = _rigRoot.position;
            _targetRot = _rigRoot.rotation;
            
            //Inicializamos el Zoom a una distancia media
            _targetZoomDist = (_maxZoomDist + _minZoomDist) / 2;
            _currentZoomDist = _targetZoomDist;
            
            //Convifugración de seguridad
            if (_pitchCurve.length == 0)
            {
                _pitchCurve = new AnimationCurve(new Keyframe(0, 10), new Keyframe(1, 60));
            }
        }

        private void OnEnable()
        {
            _input.Enable();
            UnityEngine.InputSystem.InputSystem.settings.updateMode = UnityEngine.InputSystem.InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        } 
        private void OnDisable() => _input.Disable();

        private void Update()
        {
            HandleInput();
            MoveRig();
            HandleHybridZoom();
        }
        
        private void HandleInput()
        {
            //Movimiento WASD
            Vector2 moveInput = _input.Gameplay.Move.ReadValue<Vector2>();
            
            //Calcula dirección relativa a donde mira el RIG
            Vector3 forward = _rigRoot.forward;
            Vector3 right = _rigRoot.right;
            
            //Aplanamos vectores para no volar cuando nos movemos
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            
            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

            if (moveInput.sqrMagnitude > 0.001f)
            {
                Vector3 oldPos = _targetPos;
                _targetPos += moveDir * _moveSpeed * Time.unscaledDeltaTime;    
            }
            
            
            //Rotación Q/E
            float rotInput = _input.Gameplay.Rotate.ReadValue<float>();
            if (MathF.Abs(rotInput)> 0.001f)
            {
                _targetRot *= Quaternion.Euler(0, rotInput * _rotationSpeed * Time.unscaledDeltaTime, 0);
            }
            
            
            // Zoom del Scroll
            float scroll = _input.Gameplay.Zoom.ReadValue<float>();
            if (MathF.Abs(scroll) > 0.01f)
            {
                float zoomDir = scroll > 0 ? -1 : 1;
                _targetZoomDist += zoomDir * _zoomStep;
                _targetZoomDist = Mathf.Clamp(_targetZoomDist, _minZoomDist, _maxZoomDist);
            }

            // if (Time.timeScale == 0 && moveInput != Vector2.zero)
            // {
            //     Debug.Log($"Input Recibido en pausa: {moveInput}");
            // }
            
        }

        private void MoveRig()
        {
            // Aplicamos movimiento suave al Root
            _rigRoot.position = Vector3.SmoothDamp(
                _rigRoot.position, 
                _targetPos, 
                ref _refVelocityPos, 
                _smoothTime, 
                Mathf.Infinity,        
                Time.unscaledDeltaTime 
            );
            
            // Aplicamos rotacion suave al root
            _rigRoot.rotation = Quaternion.Slerp(_rigRoot.rotation, _targetRot, Time.unscaledDeltaTime * 10f);
        }

        private void HandleHybridZoom()
        {
            //Componente Lerp del valor numerico del zoom
            _currentZoomDist = Mathf.Lerp(_currentZoomDist, _targetZoomDist, Time.unscaledDeltaTime * _zoomDamping);
            
            //calculamos el porcentaje de zoom
            float t = Mathf.InverseLerp(_minZoomDist, _maxZoomDist, _currentZoomDist);
            
            // Evaluamos la curva para tener el angulo
            float targetPitch = _pitchCurve.Evaluate(t);
            
            //Aplicamos la rotación al pivote para inclinar la cámara
            _pivot.localRotation = Quaternion.Euler(targetPitch, 0 ,0);
            
            //Aplicamos la distancia al target
            _target.localPosition = new Vector3(0, 0, -_currentZoomDist);
        }
    }
}

