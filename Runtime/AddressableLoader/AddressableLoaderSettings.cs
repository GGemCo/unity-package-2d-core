using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// GGemCo Settings 불러오기
    /// </summary>
    public class AddressableLoaderSettings : MonoBehaviour
    {
        public static AddressableLoaderSettings Instance { get; private set; }

        [HideInInspector] public GGemCoSettings settings;
        [HideInInspector] public GGemCoPlayerSettings playerSettings;
        [HideInInspector] public GGemCoMapSettings mapSettings;
        [HideInInspector] public GGemCoSaveSettings saveSettings;
        [HideInInspector] public GGemCoOptionSettings optionSettings;
        [HideInInspector] public GGemCoSoundSettings soundSettings;

        public delegate void DelegateLoadSettings(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings, GGemCoOptionSettings optionSettings,
            GGemCoSoundSettings soundSettings);
        public event DelegateLoadSettings OnLoadSettings;
        
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private float _loadProgress;

        private void Awake()
        {
            _loadProgress = 0f;
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }

        /// <summary>
        /// 모든 로드된 리소스를 해제합니다.
        /// </summary>
        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
        }
        /// <summary>
        /// 모든 설정 파일을 Addressables에서 로드
        /// </summary>
        public async Task LoadAllSettingsAsync()
        {
            try
            {
                // 여러 개의 설정을 병렬적으로 로드
                var settingsTask = LoadSettingsAsync<GGemCoSettings>(ConfigAddressableSetting.Settings.Key);
                var playerSettingsTask = LoadSettingsAsync<GGemCoPlayerSettings>(ConfigAddressableSetting.PlayerSettings.Key);
                var mapSettingsTask = LoadSettingsAsync<GGemCoMapSettings>(ConfigAddressableSetting.MapSettings.Key);
                var saveSettingsTask = LoadSettingsAsync<GGemCoSaveSettings>(ConfigAddressableSetting.SaveSettings.Key);
                var optionSettingsTask = LoadSettingsAsync<GGemCoOptionSettings>(ConfigAddressableSetting.OptionSettings.Key);
                var soundSettingsTask = LoadSettingsAsync<GGemCoSoundSettings>(ConfigAddressableSetting.SoundSettings.Key);

                // 모든 작업이 완료될 때까지 대기
                await Task.WhenAll(settingsTask, playerSettingsTask, mapSettingsTask, saveSettingsTask,
                    optionSettingsTask, soundSettingsTask);

                // 결과 저장
                settings = settingsTask.Result;
                playerSettings = playerSettingsTask.Result;
                mapSettings = mapSettingsTask.Result;
                saveSettings = saveSettingsTask.Result;
                optionSettings = optionSettingsTask.Result;
                soundSettings = soundSettingsTask.Result;

                // 로그 출력
                // if (settings != null)
                //     GcLogger.Log("Spine2d 사용여부 : " + settings.useSpine2d);
                // if (playerSettings != null)
                //     GcLogger.Log("Player statAtk : " + playerSettings.statAtk);
                // if (mapSettings != null)
                //     GcLogger.Log("Tilemap 크기 : " + mapSettings.tilemapGridCellSize);
                // if (saveSettings != null)
                //     GcLogger.Log("최대 저장 슬롯 개수 : " + saveSettings.saveDataMaxSlotCount);

                // 이벤트 호출
                OnLoadSettings?.Invoke(settings, playerSettings, mapSettings, saveSettings, optionSettings, soundSettings);
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
            // 키가 Addressables에 등록되어 있는지 확인
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            await locationsHandle.Task;

            if (!locationsHandle.Status.Equals(AsyncOperationStatus.Succeeded) || locationsHandle.Result.Count == 0)
            {
                GcLogger.LogError($"[AddressableSettingsLoader] '{key}' 가 Addressables에 등록되지 않았습니다. '{key}' 를 생성한 후 {ConfigDefine.NameSDK}Tool > 기본 셋팅하기 메뉴를 열고 Addressable 추가하기 버튼을 클릭해주세요.");
                Addressables.Release(locationsHandle);
                return null;
            }

            // 설정 로드
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T asset = await handle.Task;

            // 핸들 해제
            Addressables.Release(locationsHandle);
            return asset;
        }
        public float GetLoadProgress() => _loadProgress;
    }
}
