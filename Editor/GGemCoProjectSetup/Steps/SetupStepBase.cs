#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터 초기 설정 과정에서 사용되는 모든 설정 스텝의 공통 베이스 클래스입니다.
    /// 각 스텝은 실행 여부, 순서, 검증 및 실행 로직을 정의합니다.
    /// </summary>
    public abstract class SetupStepBase
    {
        /// <summary>
        /// 이 설정 스텝을 실제로 실행할지 여부를 나타냅니다.
        /// </summary>
        [Tooltip("이 스텝을 실행할지 여부")]
        public readonly bool enabledStep = true;

        /// <summary>
        /// 설정 스텝의 실행 순서를 나타냅니다.
        /// 값이 낮을수록 먼저 실행됩니다.
        /// </summary>
        [Tooltip("작업 순서(낮을수록 먼저 실행)")]
        public readonly int order = 0;

        /// <summary>
        /// 설정 스텝에 대한 설명 또는 메모를 저장합니다.
        /// 에디터 UI에서 참고용으로 사용됩니다.
        /// </summary>
        [TextArea, Tooltip("스텝 설명/메모")]
        public string description;

        /// <summary>
        /// 스텝 실행 전에 사전 조건을 검증합니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        /// <param name="message">검증 실패 시 사용자에게 표시할 메시지</param>
        /// <returns>검증이 통과되면 true, 실패하면 false</returns>
        public virtual bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// 설정 스텝의 실제 실행 로직을 수행합니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        public abstract void Execute(EditorSetupContext ctx);

        /// <summary>
        /// 지정된 씬 에셋을 열어 설정을 적용한 뒤, 변경 사항을 저장합니다.
        /// </summary>
        /// <param name="sceneAsset">설정을 적용할 대상 씬 에셋</param>
        /// <param name="configure">씬이 열린 이후 실행할 설정 로직</param>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        protected static void ConfigureAndSave(
            SceneAsset sceneAsset,
            System.Action configure,
            EditorSetupContext ctx)
        {
            var path = AssetDatabase.GetAssetPath(sceneAsset);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            configure?.Invoke();

            // 씬 변경 사항을 명시적으로 표시하고 저장
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 연관된 에셋 변경 사항까지 함께 저장
            AssetDatabase.SaveAssets();

            ctx.Logger.Info($"씬 설정 완료. Name: {sceneAsset.name}");
        }

        /// <summary>
        /// 프로젝트 내에서 지정된 타입의 ScriptableObject 설정 에셋을 하나 찾아 반환합니다.
        /// 여러 개가 존재할 경우, 검색 결과 중 첫 번째 에셋을 사용합니다.
        /// </summary>
        /// <typeparam name="T">검색할 ScriptableObject 타입</typeparam>
        /// <returns>
        /// 발견된 설정 에셋 인스턴스,
        /// 존재하지 않을 경우 null
        /// </returns>
        protected static T FindSettingsAsset<T>() where T : ScriptableObject
        {
            // 타입 이름 기반 검색 (예: "t:GGemCoSettings")
            string typeName = typeof(T).Name;
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}");

            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
#endif
