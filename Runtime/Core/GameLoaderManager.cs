using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// 모듈식 로더 매니저.
    /// - 각 패키지는 IGameLoadStep 구현체를 GameLoaderManager.Register로 주입
    /// - StartLoading 호출 시 Order 순서로 실행
    /// </summary>
    public class GameLoaderManager : MonoBehaviour
    {
        public static GameLoaderManager Instance { get; private set; }
        
        public sealed class EventArgsBeforeLoadStart : EventArgs
        {
            public bool Handled { get; set; } // 외부에서 처리했으면 true
        }

        public static Action<GameLoaderManager, EventArgsBeforeLoadStart> BeforeLoadStart;
        public static Action<GameLoaderManager, EventArgsBeforeLoadStart> BeforeLoadStartInLoadingScene;
        
        private TextMeshProUGUI _textLoadingPercent;
        public void SetTextLoadingPercent(TextMeshProUGUI value) => _textLoadingPercent = value;

        // 등록된 스텝
        private readonly List<IGameLoadStep> _steps = new();

        // 진행률 집계
        private readonly Dictionary<string, float> _stepProgress = new(); // step.Id -> 0~100
        private float _progressBasePerStep;
        private float _progressTotal;
        private bool _isLoadComplete;
        private bool _isStarted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            _progressBasePerStep = 0f;
            _progressTotal = 0f;
            _isLoadComplete = false;
            _isStarted = false;
        }

        /// <summary>
        /// 외부 패키지(예: Control)가 자신 스텝을 등록
        /// </summary>
        public void Register(IGameLoadStep step)
        {
            if (step == null) return;
            if (_isStarted)
            {
                GcLogger.LogWarning($"[GameLoaderManager] 이미 로딩이 시작된 후 스텝 등록 시도: {step.Id}");
                return;
            }
            if (_steps.Any(s => s.Id == step.Id))
            {
                GcLogger.LogWarning($"[GameLoaderManager] 중복 스텝 Id: {step.Id}");
                return;
            }
            _steps.Add(step);
        }

        /// <summary>
        /// 선택적으로 특정 스텝만 실행하고 싶다면 allowedIds 전달(Null이면 전체)
        /// </summary>
        private void StartLoading(IEnumerable<string> allowedIds = null)
        {
            if (_isStarted) return;
            
            var e = new EventArgsBeforeLoadStart { Handled = false };

            // 모든 구독자에게 알림
            BeforeLoadStart?.Invoke(this, e);

            // 아무도 처리하지 않았을때
            if (!e.Handled)
            {
                
            }
            
            _isStarted = true;
            _isLoadComplete = false;
            
            // 필터 + 정렬
            var targetSteps = (allowedIds == null)
                ? _steps.OrderBy(s => s.Order).ToList()
                : _steps.Where(s => allowedIds.Contains(s.Id)).OrderBy(s => s.Order).ToList();

            if (targetSteps.Count == 0)
            {
                GcLogger.LogWarning("[GameLoaderManager] 실행할 스텝이 없습니다.");
                OnLoadComplete();
                return;
            }

            InitializeProgress(targetSteps);
            StartCoroutine(LoadSequenceCoroutine(targetSteps));
        }

        private void InitializeProgress(List<IGameLoadStep> steps)
        {
            _stepProgress.Clear();
            foreach (var s in steps)
                _stepProgress[s.Id] = 0f;

            _progressBasePerStep = 100f / steps.Count;
            _progressTotal = 0f;

            if (_textLoadingPercent) _textLoadingPercent.text = "0%";
        }

        private IEnumerator LoadSequenceCoroutine(List<IGameLoadStep> steps)
        {
            foreach (var step in steps)
            {
                // 스텝 코루틴 실행
                var run = step.Run();
                while (run.MoveNext())
                {
                    UpdateLoadingProgress(step);
                    yield return run.Current;
                }
                // 마지막 갱신 보정
                UpdateLoadingProgress(step, forceComplete: true);
            }

            OnLoadComplete();
        }

        private void UpdateLoadingProgress(IGameLoadStep step, bool forceComplete = false)
        {
            var ratio = Mathf.Clamp01(step.GetProgress());
            if (forceComplete) ratio = 1f;

            _stepProgress[step.Id] = ratio * _progressBasePerStep;

            float sum = 0f;
            foreach (var kv in _stepProgress)
                sum += kv.Value;
            _progressTotal = sum;

            // UI
            if (_textLoadingPercent != null && LocalizationManager.Instance != null)
            {
                string subTitle = LocalizationManager.Instance.GetSceneByKey(step.LocalizedKey);
                string template = LocalizationManager.Instance.GetSceneByKey(LocalizationConstants.Keys.Loading.TextLoadingPercent());
                _textLoadingPercent.text = string.Format(template, subTitle, Mathf.FloorToInt(_progressTotal));
            }
        }

        private void OnLoadComplete()
        {
            _isLoadComplete = true;
            _isStarted = false;
            _steps.Clear();
            _stepProgress.Clear();
            _progressBasePerStep = 0f;
            _progressTotal = 0f;
            // GcLogger.Log("[GameLoaderManager] All registered steps completed.");
        }

        public bool IsCompleted() => _isLoadComplete;

        public bool RegistryTable()
        {
            
            return true;
        }

        public void StartLoadingInSceneLoading()
        { 
            // 필요한 로더/매니저 찾기(또는 생성)
            var tableLoader = Object.FindFirstObjectByType<TableLoaderManager>() ?? new GameObject("TableLoaderManager").AddComponent<TableLoaderManager>();
            var addrPrefabCommon = Object.FindFirstObjectByType<AddressableLoaderPrefabCommon>() ?? new GameObject("AddressableLoaderPrefabCommon").AddComponent<AddressableLoaderPrefabCommon>();
            var addrPrefabEffect = Object.FindFirstObjectByType<AddressableLoaderPrefabEffect>() ?? new GameObject("AddressableLoaderPrefabEffect").AddComponent<AddressableLoaderPrefabEffect>();
            var addrItem = Object.FindFirstObjectByType<AddressableLoaderItem>() ?? new GameObject("AddressableLoaderItem").AddComponent<AddressableLoaderItem>();
            var addrSkill = Object.FindFirstObjectByType<AddressableLoaderSkill>() ?? new GameObject("AddressableLoaderSkill").AddComponent<AddressableLoaderSkill>();
            var addrAffect = Object.FindFirstObjectByType<AddressableLoaderAffect>() ?? new GameObject("AddressableLoaderAffect").AddComponent<AddressableLoaderAffect>();
            var addrSound = Object.FindFirstObjectByType<AddressableLoaderSound>() ?? new GameObject("AddressableLoaderSound").AddComponent<AddressableLoaderSound>();
            var saveData = Object.FindFirstObjectByType<SaveDataLoader>() ?? new GameObject("SaveDataLoader").AddComponent<SaveDataLoader>();
            var loc = Object.FindFirstObjectByType<LocalizationManager>() ?? new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
            // 테이블 대상 목록은 프로젝트/씬에 따라 별도 주입(예: ScriptableObject나 Config에서)
            var targetTables = ConfigAddressableTable.All; // 사용 중인 곳에서 구현(예시)

            Register(new LocalizationLoadStep(
                "core.localization",
                order: 220,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeLocalization(),
                localizationManager: loc,
                localeCode: PlayerPrefsManager.LoadLocalizationLocaleCode()
            ));

            Register(new TableLoadStep(
                id: "core.table",
                order: 240,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeTables(),
                tableLoader: tableLoader,
                tables: targetTables
            ));

            Register(new AddressableTaskStep(
                id: "core.prefab.common",
                order: 300,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypePrefab(),
                startTask: () => addrPrefabCommon.LoadAllPreLoadGamePrefabsAsync(),
                getProgress: () => addrPrefabCommon.GetPrefabLoadProgress()
            ));

            Register(new AddressableTaskStep(
                id: "core.prefab.effect",
                order: 310,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeEffect(),
                startTask: () => addrPrefabEffect.LoadPrefabsAsync(),
                getProgress: () => addrPrefabEffect.GetPrefabLoadProgress()
            ));

            Register(new AddressableTaskStep(
                id: "core.item",
                order: 320,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeItem(),
                startTask: () => addrItem.LoadPrefabsAsync(),
                getProgress: () => addrItem.GetPrefabLoadProgress()
            ));

            Register(new AddressableTaskStep(
                id: "core.skill",
                order: 330,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeSkill(),
                startTask: () => addrSkill.LoadPrefabsAsync(),
                getProgress: () => addrSkill.GetPrefabLoadProgress()
            ));

            Register(new AddressableTaskStep(
                id: "core.affect",
                order: 340,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeAffect(),
                startTask: () => addrAffect.LoadPrefabsAsync(),
                getProgress: () => addrAffect.GetPrefabLoadProgress()
            ));

            Register(new AddressableTaskStep(
                id: "core.sound",
                order: 350,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeSound(),
                startTask: () => addrSound.LoadSoundAsync(ConfigAddressableLabel.Sound),
                getProgress: () => addrSound.GetLoadProgress()
            ));

            Register(new SaveDataLoadStep(
                "core.savedata",
                order: 380,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeSaveData(),
                saveDataLoader: saveData
            ));
            
            var e = new EventArgsBeforeLoadStart { Handled = false };
            BeforeLoadStartInLoadingScene?.Invoke(this, e);
            
            StartLoading();
        }

        public void StartLoadingInScenePreIntro()
        {
            var tableLoader = Object.FindFirstObjectByType<TableLoaderManager>() ?? new GameObject("TableLoaderManager").AddComponent<TableLoaderManager>();
            var addrSound = Object.FindFirstObjectByType<AddressableLoaderSound>() ?? new GameObject("AddressableLoaderSound").AddComponent<AddressableLoaderSound>();
            var addrSettings = Object.FindFirstObjectByType<AddressableLoaderSettings>() ?? new GameObject("AddressableLoaderSettings").AddComponent<AddressableLoaderSettings>();
            var loc = Object.FindFirstObjectByType<LocalizationManager>() ?? new GameObject("LocalizationManager").AddComponent<LocalizationManager>();

            var soundTable = ConfigAddressableTable.GetByKey(ConfigAddressableTable.KeySoundTable());
            var targetTables = new List<AddressableAssetInfo> { soundTable };
            
            Register(new AddressableTaskStep(
                id: "core.settings",
                order: 200,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeSettings(),
                startTask: () => addrSettings.LoadAllSettingsAsync(),
                getProgress: () => addrSettings.GetLoadProgress()
            ));
            Register(new LocalizationLoadStep(
                "core.localization",
                order: 220,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeLocalization(),
                localizationManager: loc,
                localeCode: PlayerPrefsManager.LoadLocalizationLocaleCode()
            ));
            
            Register(new TableLoadStep(
                id: "core.table",
                order: 240,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeTables(),
                tableLoader: tableLoader,
                tables: targetTables
            ));
            Register(new AddressableTaskStep(
                id: "core.sound.intro",
                order: 355,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeSound(),
                startTask: () => addrSound.LoadSoundAsync(ConfigAddressableLabel.SoundIntro),
                getProgress: () => addrSound.GetLoadProgress()
            ));
            StartLoading();
        }
    }
}
