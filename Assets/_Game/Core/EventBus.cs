using System.Collections.Generic;
using System;
using UnityEngine;


namespace Homebound.Core
{
    /// <summary>
    /// Este es un servicio de mensajería desacoplado, lo que permite que los sistemas
    /// se comuniquen sin referenciarse directamente
    /// </summary>
    public static class EventBus
    {
        //Variables
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        
        //Metodos
        
        public static void Subscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }
            _subscribers[type].Add(callback);
        }

        public static void Unsubscribe<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
            {
                _subscribers[type].Remove(callback);
            } 
        }

        public static void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var callbacks))
            {
                //iteramos hacia atras por si alguien se desuscribe durante el evento

                for (int i = callbacks.Count - 1; i >= 0; i--)
                {
                    (callbacks[i] as Action<T>)?.Invoke(eventData);
                }
            }
        }

    }
}

