
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>PreIntro 씬 필수 구성: Canvas + 로딩 퍼센트 텍스트</summary>
    public class ScenePreIntroConfigurator : DefaultSceneEditor, ISceneConfigurator
    {
        public void ConfigureInEditor()
        {
            var sceneEditorPreIntro = ScriptableObject.CreateInstance<SceneEditorPreIntro>();
            sceneEditorPreIntro.SetupRequiredObjects();
        }
    }
}
