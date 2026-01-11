#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 지정된 씬(SceneAsset)들을 순차적으로 열고, 각 씬에 대한 ISceneConfigurator 기반 구성을 에디터에서 적용한 뒤 저장합니다.
    /// ConfigureAndSave를 통해 씬을 OpenSceneMode.Single로 교체 로드하고, 설정 적용 → 변경 표시 → 씬/에셋 저장을 수행합니다.
    /// </summary>
    /// <remarks>
    /// NOTE:
    /// - 본 구현은 "현재 열린 씬"에 국한되지 않고, 컨텍스트에서 가져온 여러 씬을 차례로 열어 설정합니다.
    /// - 각 Configurator는 ScriptableObject 인스턴스로 생성되어 ConfigureInEditor()를 호출합니다.
    /// </remarks>
    public sealed class StepSetSceneRequireObject : SetupStepBase
    {
        /// <summary>
        /// 씬 구성 스텝 실행 전 사전 조건을 검증합니다.
        /// 현재 구현은 별도의 선행 조건 없이 항상 통과합니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        /// <param name="message">검증 실패 시 사용자에게 표시할 메시지</param>
        /// <returns>검증이 통과되면 true, 실패하면 false</returns>
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            // 별도 선행 조건은 없음. (씬 참조를 못 가져오면 Execute 단계에서 오류가 날 수 있음)
            message = null;
            return true;
        }

        /// <summary>
        /// PreIntro/Intro/Loading/Game 씬을 차례대로 구성하고 저장합니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        public override void Execute(EditorSetupContext ctx)
        {
            // PreIntro 씬 구성
            ConfigureAndSave(
                ctx.GetShared<SceneAsset>(ConfigDefine.SceneNamePreIntro),
                () => ScriptableObject.CreateInstance<ScenePreIntroConfigurator>().ConfigureInEditor(),
                ctx);

            // Intro 씬 구성
            ConfigureAndSave(
                ctx.GetShared<SceneAsset>(ConfigDefine.SceneNameIntro),
                () => ScriptableObject.CreateInstance<SceneIntroConfigurator>().ConfigureInEditor(),
                ctx);

            // Loading 씬 구성
            ConfigureAndSave(
                ctx.GetShared<SceneAsset>(ConfigDefine.SceneNameLoading),
                () => ScriptableObject.CreateInstance<SceneLoadingConfigurator>().ConfigureInEditor(),
                ctx);

            // Game 씬 구성
            ConfigureAndSave(
                ctx.GetShared<SceneAsset>(ConfigDefine.SceneNameGame),
                () => ScriptableObject.CreateInstance<SceneGameConfigurator>().ConfigureInEditor(),
                ctx);
        }
    }
}
#endif
