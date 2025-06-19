using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 인트로 씬에서 부터 필요한 리소스 불러오기
    /// </summary>
    public class AddressablePreLoader : MonoBehaviour
    {
        public delegate void DelegateLoadSettings();
        public event DelegateLoadSettings OnLoadSettings;
        public GGemCoSaveSettings saveSettings;
        
        private void Awake()
        {
        }

        /// <summary>
        /// Addressable Settings를 비동기적으로 로드하는 함수
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadAllSettingsAsync();
        }

        /// <summary>
        /// 모든 설정 파일을 Addressables에서 로드
        /// </summary>
        private async Task LoadAllSettingsAsync()
        {
            try
            {
                var settingsTask = LoadSettingsAsync<GGemCoSaveSettings>(ConfigAddressables.KeySaveSettings);

                // 모든 작업이 완료될 때까지 대기
                await Task.WhenAll(settingsTask);

                // 결과 저장
                saveSettings = settingsTask.Result;

                // 로그 출력
                if (saveSettings != null)
                    GcLogger.Log("saveDataMaxSlotCount : " + saveSettings.saveDataMaxSlotCount);

                // 이벤트 호출
                OnLoadSettings?.Invoke();
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 제네릭을 사용하여 Addressables에서 설정을 로드하는 함수
        /// </summary>
        private async Task<T> LoadSettingsAsync<T>(string key) where T : ScriptableObject
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            return await handle.Task;
        }
    }
}
