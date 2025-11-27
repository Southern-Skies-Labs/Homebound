using System;
using UnityEngine;
using System.Collections.Generic;
using Homebound.Core;

namespace Homebound.Features.Navigation
{
    public class LadderManager : MonoBehaviour, ITickable
    {
        private List<LadderController> _activeLadders = new List<LadderController>();
        
        //Metodos
        private void Awake()
        {
            ServiceLocator.Register<LadderManager>(this);
        }

        private void Start()
        {
            if(GameManager.Instance != null) 
                GameManager.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<LadderManager>();
            if (GameManager.Instance != null)
                GameManager.Instance.UnregisterTickable(this);
        }
        
        // API Publica
        public void RegisterLadder(LadderController ladder)
        {
            if (!_activeLadders.Contains(ladder))
            {
                _activeLadders.Add(ladder);
                // Debug.Log($"[LadderManager] Escalera registrada. Total {_activeLadders.Count}");
            }
        }

        public void UnregisterLadder(LadderController ladder)
        {
            if (_activeLadders.Contains(ladder))
            {
                _activeLadders.Remove(ladder);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = _activeLadders.Count - 1; i >= 0; i--)
            {
                var ladder = _activeLadders[i];
                if (ladder.ExpiryTime > 0)
                {
                    ladder.ExpiryTime -= deltaTime;
                    if (ladder.ExpiryTime >= 0)
                    {
                        DestroyLadder(ladder);
                    }
                }
            }
        }

        private void DestroyLadder(LadderController ladder)
        {
            _activeLadders.Remove(ladder);
            if (ladder != null)
            {
                Destroy(ladder.gameObject);
            }
        }
    }
}
