using System;
using UnityEngine;


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
        
        
        //Metodos

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

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

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"[GameManager] Cambiando estado a: {newState}");
            
            //Notificamos al resto del juego que el estado cambió
            //EventBus.Publish(new GameStateChangedEvent(newState));
           
        }
        
    }
}

