#if UNITY_EDITOR
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>Game 씬 필수 구성: SceneGame + Camera/Canvas/World/Black + Popup/Sound/WindowManager</summary>
    public sealed class SceneGameConfigurator : DefaultSceneEditor, ISceneConfigurator
    {
        public void ConfigureInEditor()
        {
            var sceneEditorGame = ScriptableObject.CreateInstance<SceneEditorGame>();
            sceneEditorGame.SetupRequiredObjects();
        }
    }
}
#endif