using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Option.FileName,
        menuName = ConfigScriptableObject.Option.MenuName, order = ConfigScriptableObject.Option.Ordering)]
    public class GGemCoOptionSettings : ScriptableObject
    {
        [Header("디폴트 언어")] public LocalizationConstants.LanguageIndex defaultLanguage;
        [Header("BGM 볼륨")] public float volumeBGM;
        [Header("효과음 볼륨")] public float volumeSfx;

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            defaultLanguage = LocalizationConstants.LanguageIndex.En;
            volumeBGM = 0.5f;
            volumeSfx = 0.5f;
        }
    }
}