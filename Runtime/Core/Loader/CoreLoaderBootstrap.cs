using UnityEngine;

namespace GGemCo2DCore
{
    public static class CoreLoaderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateGameLoader()
        {
            if (GameLoaderManager.Instance == null)
            {
                new GameObject("GameLoaderManager").AddComponent<GameLoaderManager>();
            }
        }
    }
}
