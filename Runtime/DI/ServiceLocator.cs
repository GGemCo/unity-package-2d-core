using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 경량 서비스 로케이터 + 지연 바인딩 이벤트
    /// - Register/Resolve/TryResolve
    /// - OnServiceRegistered: 특정 타입 등록 시점 알림
    /// - UnregisterAll: 씬 전환/테스트 종료 시 정리
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        public static event Action<Type, object> OnServiceRegistered;

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var t = typeof(T);

            if (Services.TryGetValue(t, out var exist) && !ReferenceEquals(exist, instance))
                Debug.LogWarning($"[ServiceLocator] Service already registered: {t.Name}. Overwriting.");

            Services[t] = instance;
            OnServiceRegistered?.Invoke(t, instance);
        }

        public static T Resolve<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj)) return (T)obj;
            throw new InvalidOperationException($"[ServiceLocator] Service not found: {typeof(T).Name}");
        }

        public static bool TryResolve<T>(out T instance) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj))
            {
                instance = (T)obj;
                return true;
            }
            instance = null;
            return false;
        }

        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        public static void UnregisterAll()
        {
            Services.Clear();
        }
    }
}