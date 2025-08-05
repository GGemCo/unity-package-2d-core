using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    public class GameLoaderManager : MonoBehaviour
    {
        public enum LoadType
        {
            None,
            Table,
            GamePrefab,
            SaveData,
            GamePrefabEffect,
            Item,
            Skill,
            Affect,
            Sound,
            Settings,
            Localization,
            SoundIntro
        }

        [Header("UI")]
        public TextMeshProUGUI textLoadingPercent;
        public void SetTextLoadingPercent(TextMeshProUGUI value) => textLoadingPercent = value;

        private TableLoaderManager _tableLoader;
        private SaveDataLoader _saveDataLoader;
        private AddressableLoaderPrefabCommon _addressableLoaderPrefabCommon;
        private AddressableLoaderPrefabEffect _addressableLoaderPrefabEffect;
        private AddressableLoaderItem _addressableLoaderItem;
        private AddressableLoaderSkill _addressableLoaderSkill;
        private AddressableLoaderAffect _addressableLoaderAffect;
        private AddressableLoaderSound _addressableLoaderSound;
        private AddressableLoaderSettings _addressableLoaderSettings;
        private LocalizationManager _localizationManager;

        private readonly Dictionary<LoadType, float> _progressDict = new();
        private List<LoadType> _loadSequence;
        private List<AddressableAssetInfo> _loadTargetTables;

        private float _progressBase;
        private float _progressTotal;
        private bool _isLoadComplete = false;

        private void Awake()
        {
            InitializeLoaderComponents();
        }

        private void InitializeLoaderComponents()
        {
            InitializeSingleton<TableLoaderManager>(ref _tableLoader, "TableLoaderManager");
            InitializeSingleton<AddressableLoaderPrefabCommon>(ref _addressableLoaderPrefabCommon, "AddressableLoaderPrefabCommon");
            InitializeSingleton<AddressableLoaderPrefabEffect>(ref _addressableLoaderPrefabEffect, "AddressableLoaderPrefabEffect");
            InitializeSingleton<AddressableLoaderItem>(ref _addressableLoaderItem, "AddressableLoaderItem");
            InitializeSingleton<AddressableLoaderSkill>(ref _addressableLoaderSkill, "AddressableLoaderSkill");
            InitializeSingleton<AddressableLoaderAffect>(ref _addressableLoaderAffect, "AddressableLoaderAffect");
            InitializeSingleton<AddressableLoaderSound>(ref _addressableLoaderSound, "AddressableLoaderSound");
            InitializeSingleton<SaveDataLoader>(ref _saveDataLoader, "SaveDataLoader");
            InitializeSingleton<AddressableLoaderSettings>(ref _addressableLoaderSettings, "AddressableLoaderSettings");
            InitializeSingleton<LocalizationManager>(ref _localizationManager, "LocalizationManager");
        }
        private void InitializeSingleton<T>(ref T instance, string objectName) where T : Component
        {
            if (instance != null) return;
            
#if UNITY_6000_0_OR_NEWER
            instance = GameObject.FindFirstObjectByType<T>();
#else
            instance = GameObject.FindObjectOfType<T>();
#endif
            if (instance == null)
            {
                GameObject obj = new GameObject(objectName);
                instance = obj.AddComponent<T>();
            }
        }
        public void SetLoadTargetTables(List<AddressableAssetInfo> tables)
        {
            _loadTargetTables = tables;
        }

        public void StartLoading(List<LoadType> loadSequence)
        {
            _loadSequence = loadSequence;
            InitializeProgress();
            StartCoroutine(LoadSequenceCoroutine());
        }

        private void InitializeProgress()
        {
            _progressDict.Clear();
            foreach (var type in _loadSequence)
            {
                _progressDict[type] = 0f;
            }

            _progressBase = 100f / _loadSequence.Count;
            _progressTotal = 0f;

            if (textLoadingPercent)
                textLoadingPercent.text = "0%";
        }

        private IEnumerator LoadSequenceCoroutine()
        {
            foreach (var type in _loadSequence)
            {
                switch (type)
                {
                    case LoadType.Table:
                        yield return LoadTableData();
                        break;
                    case LoadType.GamePrefab:
                        yield return LoadAddressablePrefabCommon();
                        break;
                    case LoadType.GamePrefabEffect:
                        yield return LoadAddressablePrefabEffect();
                        break;
                    case LoadType.Item:
                        yield return LoadAddressableItem();
                        break;
                    case LoadType.Skill:
                        yield return LoadAddressableSkill();
                        break;
                    case LoadType.Affect:
                        yield return LoadAddressableAffect();
                        break;
                    case LoadType.Sound:
                        yield return LoadAddressableSound();
                        break;
                    case LoadType.SoundIntro:
                        yield return LoadAddressableSoundIntro();
                        break;
                    case LoadType.SaveData:
                        yield return LoadSaveData();
                        break;
                    case LoadType.Settings:
                        yield return LoadAddressableSettings();
                        break;
                    case LoadType.Localization:
                        yield return LoadLocalization();
                        break;
                }
            }

            OnLoadComplete();
        }

        private IEnumerator LoadLocalization()
        {
            int localeIndex = PlayerPrefsManager.LoadIndexLocalizationLocale();

            yield return StartCoroutine(_localizationManager.ChangeLocaleRoutine(localeIndex));

            // LocalizationManager 내부에서 진행률 100% 설정 필요 (필요시)
            _progressDict[LoadType.Localization] = _progressBase;
            UpdateLoadingProgress(LoadType.Localization);
        }

        private IEnumerator LoadAddressableSettings()
        {
            Task loadTask = _addressableLoaderSettings.LoadAllSettingsAsync();

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.Settings] = _addressableLoaderSettings.GetLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.Settings);
                yield return null;
            }
        }

        private IEnumerator LoadTableData()
        {
            if (_loadTargetTables == null || _loadTargetTables.Count == 0)
            {
                Debug.LogWarning("No Tables to Load.");
                yield break;
            }

            int fileCount = _loadTargetTables.Count;
            for (int i = 0; i < fileCount; i++)
            {
                var addressableAssetInfo = _loadTargetTables[i];
                yield return _tableLoader.LoadDataFile(addressableAssetInfo);

                _progressDict[LoadType.Table] = (float)(i + 1) / fileCount * _progressBase;
                UpdateLoadingProgress(LoadType.Table);
            }
        }

        private IEnumerator LoadAddressablePrefabCommon()
        {
            Task loadTask = _addressableLoaderPrefabCommon.LoadAllPreLoadGamePrefabsAsync();

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.GamePrefab] = _addressableLoaderPrefabCommon.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.GamePrefab);
                yield return null;
            }
        }

        private IEnumerator LoadAddressablePrefabEffect()
        {
            Task loadTask = _addressableLoaderPrefabEffect.LoadPrefabsAsync();

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.GamePrefabEffect] = _addressableLoaderPrefabEffect.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.GamePrefabEffect);
                yield return null;
            }
        }

        private IEnumerator LoadAddressableItem()
        {
            Task loadTask = _addressableLoaderItem.LoadPrefabsAsync();

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.Item] = _addressableLoaderItem.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.Item);
                yield return null;
            }
        }

        private IEnumerator LoadAddressableSkill()
        {
            Task loadTask = _addressableLoaderSkill.LoadPrefabsAsync();

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.Skill] = _addressableLoaderSkill.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.Skill);
                yield return null;
            }
        }

        private IEnumerator LoadAddressableAffect()
        {
            Task loadTask = _addressableLoaderAffect.LoadPrefabsAsync();

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.Affect] = _addressableLoaderAffect.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.Affect);
                yield return null;
            }
        }

        private IEnumerator LoadAddressableSound()
        {
            Task loadTask = _addressableLoaderSound.LoadSoundAsync(ConfigAddressableLabel.Sound);

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.Sound] = _addressableLoaderSound.GetLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.Sound);
                yield return null;
            }
        }
        private IEnumerator LoadAddressableSoundIntro()
        {
            Task loadTask = _addressableLoaderSound.LoadSoundAsync(ConfigAddressableLabel.SoundIntro);

            while (!loadTask.IsCompleted)
            {
                _progressDict[LoadType.SoundIntro] = _addressableLoaderSound.GetLoadProgress() * _progressBase;
                UpdateLoadingProgress(LoadType.SoundIntro);
                yield return null;
            }
        }

        private IEnumerator LoadSaveData()
        {
            yield return _saveDataLoader.LoadData(progress =>
            {
                _progressDict[LoadType.SaveData] = progress * _progressBase;
                UpdateLoadingProgress(LoadType.SaveData);
            });
        }

        private void UpdateLoadingProgress(LoadType type)
        {
            float sumProgress = 0f;
            foreach (var kvp in _progressDict)
            {
                sumProgress += kvp.Value;
            }
            _progressTotal = sumProgress;

            string subTitle = GetLocalizedSubTitle(type);
            string template = LocalizationManager.Instance.GetSceneByKey(LocalizationConstants.Keys.Loading.TextLoadingPercent());
            if (textLoadingPercent != null)
            {
                textLoadingPercent.text = string.Format(template, subTitle, Mathf.FloorToInt(_progressTotal));
            }
        }

        private string GetLocalizedSubTitle(LoadType type)
        {
            string subKey = type switch
            {
                LoadType.Table => LocalizationConstants.Keys.Loading.TextTypeTables(),
                LoadType.GamePrefab => LocalizationConstants.Keys.Loading.TextTypePrefab(),
                LoadType.GamePrefabEffect => LocalizationConstants.Keys.Loading.TextTypeEffect(),
                LoadType.Item => LocalizationConstants.Keys.Loading.TextTypeItem(),
                LoadType.Skill => LocalizationConstants.Keys.Loading.TextTypeSkill(),
                LoadType.Affect => LocalizationConstants.Keys.Loading.TextTypeAffect(),
                LoadType.Sound => LocalizationConstants.Keys.Loading.TextTypeSound(),
                LoadType.SoundIntro => LocalizationConstants.Keys.Loading.TextTypeSound(),
                LoadType.SaveData => LocalizationConstants.Keys.Loading.TextTypeSaveData(),
                LoadType.Settings => LocalizationConstants.Keys.Loading.TextTypeSettings(),
                LoadType.Localization => LocalizationConstants.Keys.Loading.TextTypeLocalization(),
                _ => ""
            };

            return LocalizationManager.Instance.GetSceneByKey(subKey);
        }

        private void OnLoadComplete()
        {
            _isLoadComplete = true;
            // Debug.Log("All selected resources loaded.");
        }

        public bool IsCompleted() => _isLoadComplete;
    }
}
