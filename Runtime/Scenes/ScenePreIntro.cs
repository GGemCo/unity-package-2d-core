using System.Collections;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인트로 씬
    /// </summary>
    public class ScenePreIntro : MonoBehaviour
    {
        public string GetFieldNameSceneIntro() => nameof(ScenePreIntro);
        
        [Header(ConfigCommon.TitleHeaderRequired)]
        public TextMeshProUGUI textLoadingPercent;
        public void SetTextLoadingPercent(TextMeshProUGUI value) => textLoadingPercent = value;

        private GameLoaderManager _gameLoaderManager;
        private void Awake()
        {
            _gameLoaderManager = GameLoaderManager.Instance;
            _gameLoaderManager.SetTextLoadingPercent(textLoadingPercent);
        }
        private void Start()
        {
            _gameLoaderManager.StartLoadingInScenePreIntro();
            // 로딩 완료 후 콜백 등록 (GameLoaderManager에서 OnLoadComplete 호출 시 연결)
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
            SceneManager.ChangeScene(ConfigDefine.SceneNameIntro);
        }
    }
}