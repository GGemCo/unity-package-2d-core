
namespace GGemCo2DCoreEditor
{
    /// <summary>에디터 자동 설정 도중, 현재 열린 씬에서 호출되는 후처리</summary>
    public interface ISceneConfigurator
    {
        /// <summary>씬 편집 상태에서 에디터 실행 시 호출</summary>
        void ConfigureInEditor();
        string GetConfiguratorName() => GetType().Name;
    }
}
