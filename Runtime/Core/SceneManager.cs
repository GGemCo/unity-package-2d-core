using UnityEngine.SceneManagement;

namespace GGemCo2DCore
{
    public abstract class SceneManager
    {
        public static void ChangeScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        public static void AddScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        public static void UnLoadScene(string name)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(name);
        }
    }
}