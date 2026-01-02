using UnityEngine;
using Homebound.Features.AethianAI;
using Homebound.Features.Navigation;

namespace Homebound.Features.Visuals
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(UnitMovementController))]
    [RequireComponent(typeof(AethianBot))]
    public class UnitAnimationController : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private AnimationStateDefinition _animDefinition;

        [Header("Ajustes de Movimiento")]
        [SerializeField] private float _dampTime = 0.1f; // Suavizado de transiciones
        //[SerializeField] private float _maxSpeedReference = 4.0f; // Velocidad máxima para normalizar la animación (0 a 1)

        // Referencias Componentes
        private Animator _animator;
        private AethianBot _bot;
        private UnitMovementController _mover;

        // Variables de Estado Interno
        private Vector3 _lastPosition;
        private string _currentBoolParam; 

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _bot = GetComponent<AethianBot>();
            _mover = GetComponent<UnitMovementController>();

            if (_animDefinition == null)
            {
                Debug.LogError($"[UnitAnimationController] Falta asignar AnimationStateDefinition en {name}");
                enabled = false;
                return;
            }

            _animDefinition.Initialize();
        }

        private void OnEnable()
        {
            if (_bot != null) _bot.OnStateChanged += HandleStateChange;
        }

        private void OnDisable()
        {
            if (_bot != null) _bot.OnStateChanged -= HandleStateChange;
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void Update()
        {
            HandleMovementAnimation();
        }

        private void HandleMovementAnimation()
        {
            float currentSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
            _lastPosition = transform.position;

            _animator.SetFloat(_animDefinition.SpeedParameter, currentSpeed, _dampTime, Time.deltaTime);

            _animator.SetBool(_animDefinition.IsMovingParameter, _mover.IsMoving);
        }

        private void HandleStateChange(string newStateName)
        {
            if (!string.IsNullOrEmpty(_currentBoolParam))
            {
                _animator.SetBool(_currentBoolParam, false);
                _currentBoolParam = null;
            }

            if (_animDefinition.TryGetMapping(newStateName, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.EnterTrigger))
                {
                    _animator.SetTrigger(mapping.EnterTrigger);
                }

                if (!string.IsNullOrEmpty(mapping.StateBool))
                {
                    _animator.SetBool(mapping.StateBool, true);
                    _currentBoolParam = mapping.StateBool; 
                }

                if (mapping.HasVariants && mapping.VariantCount > 0)
                {
                    int randomVariant = Random.Range(0, mapping.VariantCount);

                    _animator.SetFloat(mapping.VariantParameter, (float)randomVariant);
                    
                }
            }
        }
    }
}