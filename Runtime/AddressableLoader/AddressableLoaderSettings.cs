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
        [HideInInspector] public GGemCoItemSettings itemSettings;
        [HideInInspector] public GGemCoSaveSettings saveSettings;
        [HideInInspector] public GGemCoOptionSettings optionSettings;
        [HideInInspector] public GGemCoSoundSettings soundSettings;
        [HideInInspector] public GGemCoGameTimeSettings gameTimeSettings;
        [HideInInspector] public GGemCoMonsterSettings monsterSettings;
        [HideInInspector] public GGemCoCutsceneSettings cutsceneSettings;
        [HideInInspector] public GGemCoWorldMapSettings worldMapSettings;
        [HideInInspector] public GGemCoDialogueBalloonSettings dialogueBalloonSettings;
        [HideInInspector] public GGemCoNpcInteractionSettings npcInteractionSettings;
        [HideInInspector] public GGemCoCharacterCollisionSettings characterCollisionSettings;

        public delegate void DelegateLoadSettings(
            GGemCoSettings settings,
            GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings,
            GGemCoSaveSettings saveSettings,
            GGemCoOptionSettings optionSettings,
            GGemCoSoundSettings soundSettings,
            GGemCoMonsterSettings monsterSettings,
            GGemCoItemSettings itemSettings);

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
        /// 모든 설정 파일을 Addressables에서 로드합니다.
        /// </summary>
        public async Task LoadAllSettingsAsync()
        {
            try
            {
                // 여러 개의 설정을 병렬적으로 로드
                Task<GGemCoSettings> settingsTask = LoadSettingsAsync<GGemCoSettings>(ConfigAddressableSetting.Settings.Key);
                Task<GGemCoPlayerSettings> playerSettingsTask = LoadSettingsAsync<GGemCoPlayerSettings>(ConfigAddressableSetting.PlayerSettings.Key);
                Task<GGemCoMapSettings> mapSettingsTask = LoadSettingsAsync<GGemCoMapSettings>(ConfigAddressableSetting.MapSettings.Key);
                Task<GGemCoItemSettings> itemSettingsTask = LoadSettingsAsync<GGemCoItemSettings>(ConfigAddressableSetting.ItemSettings.Key);
                Task<GGemCoSaveSettings> saveSettingsTask = LoadSettingsAsync<GGemCoSaveSettings>(ConfigAddressableSetting.SaveSettings.Key);
                Task<GGemCoOptionSettings> optionSettingsTask = LoadSettingsAsync<GGemCoOptionSettings>(ConfigAddressableSetting.OptionSettings.Key);
                Task<GGemCoSoundSettings> soundSettingsTask = LoadSettingsAsync<GGemCoSoundSettings>(ConfigAddressableSetting.SoundSettings.Key);
                Task<GGemCoMonsterSettings> monsterSettingsTask = LoadSettingsAsync<GGemCoMonsterSettings>(ConfigAddressableSetting.MonsterSettings.Key);
                Task<GGemCoCutsceneSettings> cutsceneSettingsTask = LoadSettingsAsync<GGemCoCutsceneSettings>(ConfigAddressableSetting.CutsceneSettings.Key);
                Task<GGemCoWorldMapSettings> worldMapSettingsTask = LoadSettingsAsync<GGemCoWorldMapSettings>(ConfigAddressableSetting.WorldMapSettings.Key);
                Task<GGemCoDialogueBalloonSettings> dialogueBalloonSettingsTask = LoadSettingsAsync<GGemCoDialogueBalloonSettings>(ConfigAddressableSetting.DialogueBalloonSettings.Key);
                Task<GGemCoNpcInteractionSettings> npcInteractionSettingsTask = LoadSettingsAsync<GGemCoNpcInteractionSettings>(ConfigAddressableSetting.NpcInteractionSettings.Key);
                Task<GGemCoCharacterCollisionSettings> characterCollisionSettingsTask = LoadSettingsAsync<GGemCoCharacterCollisionSettings>(ConfigAddressableSetting.CharacterCollisionSettings.Key);
#if GGEMCO_USE_INGAME_TIME
                Task<GGemCoGameTimeSettings> gameTimeSettingsTask = LoadSettingsAsync<GGemCoGameTimeSettings>(ConfigAddressableSetting.GameTimeSettings.Key);
#endif

#if GGEMCO_USE_INGAME_TIME
                // 모든 작업이 완료될 때까지 대기
                await Task.WhenAll(
                    settingsTask,
                    playerSettingsTask,
                    mapSettingsTask,
                    saveSettingsTask,
                    optionSettingsTask,
                    soundSettingsTask,
                    monsterSettingsTask,
                    cutsceneSettingsTask,
                    itemSettingsTask,
                    worldMapSettingsTask,
                    dialogueBalloonSettingsTask,
                    npcInteractionSettingsTask,
                    characterCollisionSettingsTask,
                    gameTimeSettingsTask);
#else
                // 모든 작업이 완료될 때까지 대기
                await Task.WhenAll(
                    settingsTask,
                    playerSettingsTask,
                    mapSettingsTask,
                    saveSettingsTask,
                    optionSettingsTask,
                    soundSettingsTask,
                    monsterSettingsTask,
                    cutsceneSettingsTask,
                    itemSettingsTask,
                    worldMapSettingsTask,
                    dialogueBalloonSettingsTask,
                    npcInteractionSettingsTask,
                    characterCollisionSettingsTask);
#endif

                // 결과 저장
                settings = settingsTask.Result;
                playerSettings = playerSettingsTask.Result;
                mapSettings = mapSettingsTask.Result;
                saveSettings = saveSettingsTask.Result;
                itemSettings = itemSettingsTask.Result;
                optionSettings = optionSettingsTask.Result;
                soundSettings = soundSettingsTask.Result;
                monsterSettings = monsterSettingsTask.Result;
                cutsceneSettings = cutsceneSettingsTask.Result;
                worldMapSettings = worldMapSettingsTask.Result;
                dialogueBalloonSettings = dialogueBalloonSettingsTask.Result;
                npcInteractionSettings = npcInteractionSettingsTask.Result;
                characterCollisionSettings = characterCollisionSettingsTask.Result;
#if GGEMCO_USE_INGAME_TIME
                gameTimeSettings = gameTimeSettingsTask.Result;
#endif

                // 이벤트 호출
                OnLoadSettings?.Invoke(
                    settings,
                    playerSettings,
                    mapSettings,
                    saveSettings,
                    optionSettings,
                    soundSettings,
                    monsterSettings,
                    itemSettings);
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 제네릭을 사용하여 Addressables에서 설정을 로드하는 함수입니다.
        /// </summary>
        /// <typeparam name="T">로드할 ScriptableObject 타입입니다.</typeparam>
        /// <param name="key">Addressables Key입니다.</param>
        /// <returns>로드된 설정 에셋입니다.</returns>
        private async Task<T> LoadSettingsAsync<T>(string key) where T : ScriptableObject
        {
            // 에디터 Play Mode에서 작업자별 개발용 Settings가 등록되어 있으면 서비스용 Addressables보다 먼저 사용합니다.
            if (SettingsRuntimeResolver.TryGetOverride(key, out T overrideSettings))
            {
                return overrideSettings;
            }

            // 키가 Addressables에 등록되어 있는지 확인
            AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> locationsHandle =
                Addressables.LoadResourceLocationsAsync(key);
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

        /// <summary>
        /// 현재 설정 로딩 진행률을 반환합니다.
        /// </summary>
        /// <returns>0~1 범위의 진행률입니다.</returns>
        public float GetLoadProgress() => _loadProgress;
    }
}
