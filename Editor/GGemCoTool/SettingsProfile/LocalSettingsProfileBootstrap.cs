using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터 도메인 로드 시 작업자별 Settings Override Provider를 Runtime Registry에 연결합니다.
    /// </summary>
    [InitializeOnLoad]
    public static class LocalSettingsProfileBootstrap
    {
        static LocalSettingsProfileBootstrap()
        {
            RegisterProvider();
        }

        /// <summary>
        /// Core Runtime의 Settings Override Registry에 에디터 전용 Provider를 등록합니다.
        /// </summary>
        private static void RegisterProvider()
        {
            SettingsOverrideRegistry.SetProvider(new LocalSettingsOverrideProvider());
        }
    }
}
