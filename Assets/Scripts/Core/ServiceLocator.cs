using System.Collections.Generic;
using System;

namespace PictureGenerator
{
    public static class ServiceLocator 
    {
        public static event Action<Type> ServiceRegistered;
        private static readonly Dictionary<string, object> Services = new();
        public static bool IsRegistered<T>() => Services.ContainsKey(typeof(T).Name);

        public static T Get<T>()
        {
            var key = typeof(T).Name;
            if (!Services.ContainsKey(key))
            {
                throw new Exception($"[ServiceError] {key} not registered");
            }

            return (T)Services[key];
        }

        public static void Register<T>(T service)
        {
            var key = typeof(T).Name;
            if (Services.ContainsKey(key))
            {
                throw new Exception($"Attempted to register service of type {key} which is already registered");
            }
            Services.Add(key, service);
            ServiceRegistered?.Invoke(typeof(T));
            //Utils.Log($"[Service] + {typeof(T)} {service.GetHashCode()}");
        }

        public static void Unregister<T>()
        {
            var key = typeof(T).Name;
            if (!Services.ContainsKey(key))
            {
                throw new Exception($"Attempted to unregister service of type {key} which is not registered");
            }

            Services.Remove(key);
            //Utils.Log($"[Service] removed {typeof(T)}");
        }
    }
}
