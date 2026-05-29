using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// Settings ScriptableObject를 어떤 프로파일에서 읽을지 구분하는 값입니다.
    /// </summary>
    [Serializable]
    public enum SettingsProfileKind
    {
        /// <summary>
        /// 서비스/빌드 기준 Settings 에셋을 사용합니다.
        /// </summary>
        Service = 0,

        /// <summary>
        /// 에디터 Play Mode에서 작업자별 개발용 Settings 에셋을 우선 사용합니다.
        /// </summary>
        Development = 1,
    }
}
