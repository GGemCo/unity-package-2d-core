using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNameIntro);
                return;
            }
            
            _gameLoaderManager = new GameObject("GameLoaderManager").AddComponent<GameLoaderManager>();
            _gameLoaderManager.SetTextLoadingPercent(textLoadingPercent);

            var soundTable = ConfigAddressableTable.GetByKey(ConfigAddressableTable.KeySoundTable());
            _gameLoaderManager.SetLoadTargetTables(ConfigAddressableTable.All);

            // 순서 중요
            // Localization 을 먼저 해야 로드 진행률 텍스트에 적용된다.
            var loadSequence = new List<GameLoaderManager.LoadType>
            {
                GameLoaderManager.LoadType.Table,
                GameLoaderManager.LoadType.GamePrefab,
                GameLoaderManager.LoadType.SaveData,
                GameLoaderManager.LoadType.GamePrefabEffect,
                GameLoaderManager.LoadType.Item,
                GameLoaderManager.LoadType.Skill,
                GameLoaderManager.LoadType.Affect,
                GameLoaderManager.LoadType.Sound,
            };

            _gameLoaderManager.StartLoading(loadSequence);
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
            SceneManager.ChangeScene(ConfigDefine.SceneNameGame);
        }
    }
}