using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터 Play Mode에서 작업자별 개발용 Settings 에셋을 Runtime Settings 로더에 제공합니다.
    /// </summary>
    public sealed class LocalSettingsOverrideProvider : ISettingsOverrideProvider
    {
        /// <summary>
        /// 현재 선택된 프로파일이 Development이면 작업자 로컬 Settings 에셋을 조회합니다.
        /// </summary>
        /// <typeparam name="T">요청하는 Settings ScriptableObject 타입입니다.</typeparam>
        /// <param name="key">서비스용 Settings Addressables Key입니다.</param>
        /// <param name="settings">조회된 개발용 Settings 에셋입니다.</param>
        /// <returns>개발용 Settings가 있으면 true, 없으면 false입니다.</returns>
        public bool TryGet<T>(string key, out T settings) where T : ScriptableObject
        {
            settings = null;
            if (SettingsProfileEditorPrefs.CurrentProfile != SettingsProfileKind.Development)
                return false;

            string assetPath = SettingsProfileEditorPrefs.GetDevelopmentAssetPath(key);
            settings = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            return settings != null;
        }
    }
}
