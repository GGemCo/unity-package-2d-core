using GGemCo2DCore;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GGemCo2DCoreEditor
{
    public class DefaultSceneEditor : DefaultEditorWindow
    {
        // 현재 불러온 씬 이름을 체크하기 위해 추가
        protected GGemCoSettings GGemCoSettings;
        protected override void OnEnable()
        {
            base.OnEnable();
            GGemCoSettings = GetScriptableSetting();
        }
        private Scene GetActiveSceneInEditor()
        {
            // 현재 에디터에서 활성화된 씬을 가져옴
            return SceneManager.GetActiveScene();
        }
        protected bool CheckCurrentLoadedScene(string sceneName)
        {
            Scene scene = GetActiveSceneInEditor();
            return scene.name == sceneName;
        }
        private GGemCoSettings GetScriptableSetting()
        {
            GGemCoSettings scriptable =
                AssetDatabaseLoaderManager.LoadScriptableObject(ConfigAddressableSetting.Settings.Path) as
                    GGemCoSettings;
            return scriptable == null ? null : scriptable;
        }
    }
}