using System;
using UnityEngine;
using System.Collections.Generic;


namespace Homebound.Core
{
    //Estados del gameplay
    public enum GameState
    {
        Boot,
        Gameplay,
        Paused
    }
    
    
    //Clase principal
    public class GameManager : MonoBehaviour
    {
        //Variables
        
        public static GameManager Instance { get; private set; } //Este sería el único Singleton permitido
        
        public GameState CurrentState { get; private set; }

        private List<ITickable> _tickables = new List<ITickable>();
        
        //Metodos

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return;}

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }
        
        private void InitializeServices()
        {
            Debug.Log("[GameManager] Inicializando servicios Core....");
            
            //Desde aca se registran los futuros sistemas a implementar
            
            
            ChangeState(GameState.Boot);
        }

        private void Start()
        {
            ChangeState(GameState.Gameplay);
        }

        private void Update()
        {
            if (CurrentState == GameState.Gameplay)
            {
                float dt = Time.deltaTime;

                for (int i = 0; i < _tickables.Count; i++)
                {
                    _tickables[i].Tick(dt);
                }
            }
        }

        public void RegisterTickable(ITickable tickable)
        {
            if (!_tickables.Contains(tickable))
            {
                _tickables.Add(tickable);
            }
        }

        public void UnregisterTickable(ITickable tickable)
        {
            if (_tickables.Contains(tickable))
            {
                _tickables.Remove(tickable);
            }
        }


        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"[GameManager] Cambiando estado a: {newState}");
            
            //Notificamos al resto del juego que el estado cambió
            //EventBus.Publish(new GameStateChangedEvent(newState));
           
        }
        
    }
}

