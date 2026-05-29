using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 서비스용 Addressables Settings 로딩 전에 개발용 Settings Override를 우선 확인하는 공용 해석기입니다.
    /// </summary>
    public static class SettingsRuntimeResolver
    {
        /// <summary>
        /// 등록된 개발용 Settings가 있으면 반환합니다.
        /// </summary>
        /// <typeparam name="T">요청하는 Settings ScriptableObject 타입입니다.</typeparam>
        /// <param name="key">서비스용 Settings Addressables Key입니다.</param>
        /// <param name="settings">조회된 개발용 Settings 에셋입니다.</param>
        /// <returns>개발용 Settings가 있으면 true, 없으면 false입니다.</returns>
        public static bool TryGetOverride<T>(string key, out T settings) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(key))
            {
                settings = null;
                return false;
            }

            return SettingsOverrideRegistry.TryGet(key, out settings);
        }
    }
}
