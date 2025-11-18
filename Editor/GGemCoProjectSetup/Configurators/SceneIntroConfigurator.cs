#if UNITY_EDITOR
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>Intro 씬 필수 구성: 기본 메뉴 버튼(+클릭 사운드), 옵션/팝업/사운드 매니저 연결</summary>
    public sealed class SceneIntroConfigurator : DefaultSceneEditor, ISceneConfigurator
    {
        public void ConfigureInEditor()
        {
            var sceneEditorIntro = ScriptableObject.CreateInstance<SceneEditorIntro>();
            sceneEditorIntro.SetupRequiredObjects();
        }
    }
}
#endif