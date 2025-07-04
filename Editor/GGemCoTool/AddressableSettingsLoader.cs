﻿using System;
using System.Threading.Tasks;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// GGemCo Settings 불러오기
    /// </summary>
    public class AddressableSettingsLoader
    {
        private GGemCoSettings _settings;
        private GGemCoPlayerSettings _playerSettings;
        private GGemCoMapSettings _mapSettings;
        private GGemCoSaveSettings _saveSettings;

        public delegate void DelegateLoadSettings(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings);
        public event DelegateLoadSettings OnLoadSettings;

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
                // 여러 개의 설정을 병렬적으로 로드
                var settingsTask = LoadSettingsAsync<GGemCoSettings>(ConfigAddressableSetting.Settings.Key);
                var playerSettingsTask = LoadSettingsAsync<GGemCoPlayerSettings>(ConfigAddressableSetting.PlayerSettings.Key);
                var mapSettingsTask = LoadSettingsAsync<GGemCoMapSettings>(ConfigAddressableSetting.MapSettings.Key);
                var saveSettingsTask = LoadSettingsAsync<GGemCoSaveSettings>(ConfigAddressableSetting.SaveSettings.Key);

                // 모든 작업이 완료될 때까지 대기
                await Task.WhenAll(settingsTask, playerSettingsTask, mapSettingsTask, saveSettingsTask);

                // 결과 저장
                _settings = settingsTask.Result;
                _playerSettings = playerSettingsTask.Result;
                _mapSettings = mapSettingsTask.Result;
                _saveSettings = saveSettingsTask.Result;

                // 로그 출력
                // if (settings != null)
                //     Debug.Log("Spine2d 사용여부 : " + settings.useSpine2d);
                // if (playerSettings != null)
                //     Debug.Log("Player statAtk : " + playerSettings.statAtk);
                // if (mapSettings != null)
                //     Debug.Log("Tilemap 크기 : " + mapSettings.tilemapGridCellSize);
                // if (saveSettings != null)
                //     Debug.Log("최대 저장 슬롯 개수 : " + saveSettings.saveDataMaxSlotCount);

                // 이벤트 호출
                OnLoadSettings?.Invoke(_settings, _playerSettings, _mapSettings, _saveSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
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
                Debug.LogError($"[AddressableSettingsLoader] '{key}' 가 Addressables에 등록되지 않았습니다. '{key}' 를 생성한 후 {ConfigDefine.NameSDK}Tool > 기본 셋팅하기 메뉴를 열고 Addressable 추가하기 버튼을 클릭해주세요.");
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
    }
}
