using TMPro;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>Loading 씬 필수 구성: SceneLoading + Canvas + 퍼센트 텍스트</summary>
    public sealed class SceneLoadingConfigurator : DefaultSceneEditor, ISceneConfigurator
    {
        public void ConfigureInEditor()
        {
            var sceneEditorLoading = ScriptableObject.CreateInstance<SceneEditorLoading>();
            sceneEditorLoading.SetupRequiredObjects();
        }
    }
}
