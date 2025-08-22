using System.Collections;
using TMPro;

namespace GGemCo2DCore
{
    public class SceneLoading : DefaultScene
    {
        private GameLoaderManager _gameLoaderManager;
        public TextMeshProUGUI textLoadingPercent;
        public void SetTextLoadingPercent(TextMeshProUGUI value) => textLoadingPercent = value;
        
        private void Awake()
        {
            if (!AddressableLoaderSettings.Instance)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNamePreIntro);
                return;
            }
            _gameLoaderManager = GameLoaderManager.Instance;
            _gameLoaderManager.SetTextLoadingPercent(textLoadingPercent);
        }

        private void Start()
        {
            _gameLoaderManager.StartLoadingInSceneLoading();
            StartCoroutine(WaitForLoadingComplete());
        }

        /// <summary>
        /// GameLoaderManager의 진행률 100% 도달을 기다림
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForLoadingComplete()
        {
            while (_gameLoaderManager != null && !_gameLoaderManager.IsCompleted())
            {
                yield return null;
            }

            OnIntroLoadComplete();
        }

        private void OnIntroLoadComplete()
        {
            SceneManager.ChangeScene(ConfigDefine.SceneNameGame);
        }
    }
}