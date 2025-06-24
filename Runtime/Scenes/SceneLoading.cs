
namespace GGemCo2DCore
{
    public class SceneLoading : DefaultScene
    {
        private void Awake()
        {
            if (!AddressableLoaderSettings.Instance)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNameIntro);
                return;
            }
        }
    }
}