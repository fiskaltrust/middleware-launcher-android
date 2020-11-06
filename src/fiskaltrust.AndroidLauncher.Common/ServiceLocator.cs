using System;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Common
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

        public static void Register<T>(T instance)
        {
            _instances[typeof(T)] = instance;
        }

        public static T Resolve<T>()
        {
            return (T) _instances[typeof(T)];
        }
    }
}