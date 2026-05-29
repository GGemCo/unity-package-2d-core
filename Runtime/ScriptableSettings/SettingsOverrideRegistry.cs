using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 Settings 로더가 사용할 개발용 Settings 공급자를 보관합니다.
    /// </summary>
    public static class SettingsOverrideRegistry
    {
        private static readonly object LockObject = new object();
        private static ISettingsOverrideProvider _provider;

        /// <summary>
        /// 현재 등록된 공급자가 있는지 반환합니다.
        /// </summary>
        public static bool HasProvider
        {
            get
            {
                lock (LockObject)
                {
                    return _provider != null;
                }
            }
        }

        /// <summary>
        /// 개발용 Settings 공급자를 등록합니다.
        /// </summary>
        /// <param name="provider">등록할 공급자입니다. null이면 기존 공급자를 제거합니다.</param>
        public static void SetProvider(ISettingsOverrideProvider provider)
        {
            lock (LockObject)
            {
                _provider = provider;
            }
        }

        /// <summary>
        /// 현재 등록된 개발용 Settings 공급자를 제거합니다.
        /// </summary>
        public static void ClearProvider()
        {
            lock (LockObject)
            {
                _provider = null;
            }
        }

        /// <summary>
        /// 등록된 공급자에서 개발용 Settings를 조회합니다.
        /// </summary>
        /// <typeparam name="T">요청하는 Settings ScriptableObject 타입입니다.</typeparam>
        /// <param name="key">서비스용 Settings Addressables Key입니다.</param>
        /// <param name="settings">조회된 개발용 Settings 에셋입니다.</param>
        /// <returns>개발용 Settings를 찾았으면 true, 없으면 false입니다.</returns>
        public static bool TryGet<T>(string key, out T settings) where T : ScriptableObject
        {
            ISettingsOverrideProvider provider;
            lock (LockObject)
            {
                provider = _provider;
            }

            settings = null;
            if (provider == null)
                return false;

            try
            {
                return provider.TryGet(key, out settings) && settings != null;
            }
            catch (Exception ex)
            {
                settings = null;
                GcLogger.LogError($"개발용 Settings 조회 중 오류가 발생했습니다. key={key}, type={typeof(T).Name}, error={ex.Message}");
                return false;
            }
        }
    }
}
