namespace GGemCo2DCore
{
    public abstract class SceneManager
    {
        public static void ChangeScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}