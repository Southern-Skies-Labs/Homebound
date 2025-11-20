using UnityEngine;
using System;
using System.Collections.Generic;

namespace Homebound.Core
{
    /// <summary>
    /// Este es el registro central de dependencias, la razón de esto es para eliminar la necesidad
    /// de tener muchos Singletons dispersos y acoplados
    /// </summary>
    public static class ServiceLocator
    {
        //Variables
        
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        //Metodos
        
        public static void Register<T>(T service)
        {

            var type = typeof(T);
            if (!_services.ContainsKey(type))
            {
                _services.Add(type, service);
                // Debug.Log($"[ServiceLocator] Se registro el servicio {type.Name}");
            }
            else
            {
                Debug.LogWarning($"El servicio {type} ya esta registrado");
            }


        }
        
        public static void Unregister<T>()
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }
        
        public static T Get<T>()
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            else
            {
                Debug.LogError($"[ServiceLocator] Servicio no encontrado: {type.Name}");
                return default;
            }
        } 
        
        
    }

}

