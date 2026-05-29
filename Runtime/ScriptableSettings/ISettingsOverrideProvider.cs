using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables로 서비스용 Settings를 읽기 전에 개발용 Settings를 제공할 수 있는 공급자 계약입니다.
    /// </summary>
    public interface ISettingsOverrideProvider
    {
        /// <summary>
        /// 지정한 Addressables Key에 대응하는 개발용 Settings 에셋을 조회합니다.
        /// </summary>
        /// <typeparam name="T">요청하는 Settings ScriptableObject 타입입니다.</typeparam>
        /// <param name="key">서비스용 Settings Addressables Key입니다.</param>
        /// <param name="settings">조회된 개발용 Settings 에셋입니다.</param>
        /// <returns>개발용 Settings를 찾았으면 true, 없으면 false입니다.</returns>
        bool TryGet<T>(string key, out T settings) where T : ScriptableObject;
    }
}
