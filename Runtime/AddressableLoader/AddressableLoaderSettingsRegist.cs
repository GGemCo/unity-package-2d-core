using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// GGemCo Settings + 외부(Settings) Addressables 로더
    /// </summary>
    public class AddressableLoaderSettingsRegist : MonoBehaviour
    {
        public static AddressableLoaderSettingsRegist Instance { get; private set; }

        [HideInInspector] public GGemCoSettings settings;
        [HideInInspector] public GGemCoPlayerSettings playerSettings;
        [HideInInspector] public GGemCoMapSettings mapSettings;
        [HideInInspector] public GGemCoSaveSettings saveSettings;
        [HideInInspector] public GGemCoOptionSettings optionSettings;
        [HideInInspector] public GGemCoSoundSettings soundSettings;

        public delegate void DelegateLoadSettings(
            GGemCoSettings settings,
            GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings,
            GGemCoSaveSettings saveSettings,
            GGemCoOptionSettings optionSettings,
            GGemCoSoundSettings soundSettings
        );
        public event DelegateLoadSettings OnLoadSettings;

        // [NEW] 외부 Settings 단일 항목 로드 완료 이벤트 (선택)
        public event Action<string, UnityEngine.Object> OnLoadExternalSetting;

        private readonly HashSet<AsyncOperationHandle> _activeHandles = new();
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

        /// <summary>모든 로드된 리소스를 해제합니다.</summary>
        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
            _activeHandles.Clear();
        }

        /// <summary>
        /// 모든 설정 파일(+외부 등록 Settings)을 Addressables에서 로드
        /// </summary>
        public async Task LoadAllSettingsAsync()
        {
            try
            {
                _loadProgress = 0f;

                // 1) Core 6종 병렬 Task 구성
                var tasks = new List<Task<UnityEngine.Object>>(8)
                {
                    LoadSettingsAsObjectAsync(ConfigAddressableSetting.Settings.Key),
                    LoadSettingsAsObjectAsync(ConfigAddressableSetting.PlayerSettings.Key),
                    LoadSettingsAsObjectAsync(ConfigAddressableSetting.MapSettings.Key),
                    LoadSettingsAsObjectAsync(ConfigAddressableSetting.SaveSettings.Key),
                    LoadSettingsAsObjectAsync(ConfigAddressableSetting.OptionSettings.Key),
                    LoadSettingsAsObjectAsync(ConfigAddressableSetting.SoundSettings.Key)
                };

                // 2) [NEW] 외부 등록 Settings 병렬 Task 추가
                var externals = SettingsRegistry.GetAll();
                foreach (var ext in externals)
                    tasks.Add(LoadExternalAsObjectAsync(ext));

                // 3) 전체 병렬 로드
                await Task.WhenAll(tasks);

                // 4) Core 6종 할당 (null 안전 캐스팅)
                settings       = tasks[0].Result as GGemCoSettings;
                playerSettings = tasks[1].Result as GGemCoPlayerSettings;
                mapSettings    = tasks[2].Result as GGemCoMapSettings;
                saveSettings   = tasks[3].Result as GGemCoSaveSettings;
                optionSettings = tasks[4].Result as GGemCoOptionSettings;
                soundSettings  = tasks[5].Result as GGemCoSoundSettings;

                // 5) 이벤트 (기존)
                OnLoadSettings?.Invoke(settings, playerSettings, mapSettings, saveSettings, optionSettings, soundSettings);
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 최종 진행률 보정
                _loadProgress = 1f;
            }
        }

        /// <summary>
        /// [기존 일반화] 키로 ScriptableObject를 로드하되, Object로 받음
        /// </summary>
        private async Task<UnityEngine.Object> LoadSettingsAsObjectAsync(string key)
        {
            // 키가 Addressables에 등록되어 있는지 확인
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            _activeHandles.Add(locationsHandle);
            await locationsHandle.Task;

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result.Count == 0)
            {
                GcLogger.LogError($"[AddressableSettingsLoader] '{key}' 가 Addressables에 등록되지 않았습니다. '{key}' 를 생성한 후 {ConfigDefine.NameSDK}Tool > 기본 셋팅하기 메뉴를 열고 Addressable 추가하기 버튼을 클릭해주세요.");
                Addressables.Release(locationsHandle);
                _activeHandles.Remove(locationsHandle);
                return null;
            }

            // 설정 로드
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
            _activeHandles.Add(handle);
            var asset = await handle.Task;

            // 진행률 갱신
            UpdateProgress();

            // 핸들 정리(로더 라이프사이클에 맞춰 적절히 해제)
            Addressables.Release(locationsHandle);
            _activeHandles.Remove(locationsHandle);
            // 여기서는 handle 유지(나중에 ReleaseAll에서 정리)
            return asset;
        }

        /// <summary>
        /// [NEW] 외부 등록 Settings 로드
        /// </summary>
        private async Task<UnityEngine.Object> LoadExternalAsObjectAsync(SettingsRegistry.Registration ext)
        {
            UnityEngine.Object obj = await LoadSettingsAsObjectAsync(ext.Key);
            try
            {
                ext.OnLoaded?.Invoke(obj);  // 외부 콜백(해당 패키지에서 캐스팅/보관)
                OnLoadExternalSetting?.Invoke(ext.Id, obj);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"외부 Settings 콜백 처리 중 오류(id={ext.Id}, key={ext.Key}): {e.Message}");
            }
            return obj;
        }

        /// <summary>
        /// [NEW] 현재까지의 모든 Addressables 핸들의 평균 PercentComplete로 진행률 계산
        /// </summary>
        private void UpdateProgress()
        {
            if (_activeHandles.Count == 0)
            {
                _loadProgress = 0f;
                return;
            }
            float sum = 0f;
            int count = 0;
            foreach (var h in _activeHandles)
            {
                if (h.IsValid())
                {
                    sum += h.PercentComplete;
                    count++;
                }
            }
            _loadProgress = (count > 0) ? Mathf.Clamp01(sum / count) : 0f;
        }

        public float GetLoadProgress() => _loadProgress;

        // ============================
        // [NEW] 외부 Settings 레지스트리
        // ============================
        public static class SettingsRegistry
        {
            public struct Registration
            {
                public string Id;                 // 예: "control.settings"
                public string Key;                // Addressables key
                public Action<UnityEngine.Object> OnLoaded; // 로드 완료 콜백(패키지 내부에서 캐스팅)
            }

            private static readonly List<Registration> List = new();
            private static readonly object Lock = new();

            /// <summary>외부 패키지에서 등록</summary>
            public static void Register(string id, string key, Action<UnityEngine.Object> onLoaded)
            {
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key))
                    return;

                lock (Lock)
                {
                    // 중복 id는 최신 등록으로 교체(또는 무시 처리 가능)
                    int idx = List.FindIndex(r => r.Id == id);
                    var reg = new Registration { Id = id, Key = key, OnLoaded = onLoaded };
                    if (idx >= 0) List[idx] = reg;
                    else List.Add(reg);
                }
            }

            /// <summary>외부 패키지에서 등록 취소</summary>
            public static void Unregister(string id)
            {
                lock (Lock)
                {
                    List.RemoveAll(r => r.Id == id);
                }
            }

            /// <summary>전체 조회(내부 전용)</summary>
            public static List<Registration> GetAll()
            {
                lock (Lock)
                {
                    return new List<Registration>(List);
                }
            }

            /// <summary>모두 초기화</summary>
            public static void Clear()
            {
                lock (Lock)
                {
                    List.Clear();
                }
            }
        }
    }
}
