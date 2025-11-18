#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 현재 열린 씬에서 ISceneConfigurator를 실행한다.
    /// - 1) 씬 내 MonoBehaviour + ISceneConfigurator (비활성 포함)
    /// - 2) 프로젝트 내 ScriptableObject 에셋(직접 서브클래스) + ISceneConfigurator
    /// - 3) 매개변수 없는 생성자가 있는 일반 클래스 + ISceneConfigurator (TypeCache 기반)
    /// 실행/검증/로깅은 Runner 컨텍스트 정책(Profile.stopOnFirstError)에 따름.
    /// </summary>
    public sealed class StepSetSceneRequireObject : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            // 별도 선행 조건은 없음. (열린 씬이 없어도 MonoBehaviour 탐색은 빈 결과)
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            ConfigureAndSave(ctx.GetShared<SceneAsset>(ConfigDefine.SceneNamePreIntro),
                () => ScriptableObject.CreateInstance<ScenePreIntroConfigurator>().ConfigureInEditor(), ctx);

            ConfigureAndSave(ctx.GetShared<SceneAsset>(ConfigDefine.SceneNameIntro),
                () => ScriptableObject.CreateInstance<SceneIntroConfigurator>().ConfigureInEditor(), ctx);

            ConfigureAndSave(ctx.GetShared<SceneAsset>(ConfigDefine.SceneNameLoading),
                () => ScriptableObject.CreateInstance<SceneLoadingConfigurator>().ConfigureInEditor(), ctx);

            ConfigureAndSave(ctx.GetShared<SceneAsset>(ConfigDefine.SceneNameGame),
                () => ScriptableObject.CreateInstance<SceneGameConfigurator>().ConfigureInEditor(), ctx);
        }
        private static void ConfigureAndSave(SceneAsset sceneAsset, System.Action configure, EditorSetupContext ctx)
        {
            var path = AssetDatabase.GetAssetPath(sceneAsset);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            configure?.Invoke();

            // 변경 표시 및 저장(안전하게 두 번: 씬/에셋)
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ctx.Logger.Info($"씬 설정 완료. Name: {sceneAsset.name}");
        }
    }
}
#endif
