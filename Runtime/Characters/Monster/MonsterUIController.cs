using R3;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터와 연관된 UI 관리 클래스
    /// </summary>
    public class MonsterUIController
    {
        private Monster _monster;
        private SceneGame _sceneGame;
        
        private GameObject _prefabSliderHpBar;
        private Transform _containerMonsterHpBar;
        private GameObject _prefabPanelMonsterSuperArmor;
        private GameObject _sliderHpBar;
        private GameObject _monsterUISuperArmor;
        private GameObject _monsterDebugLevelText;
        private GameObject _monsterDebugHpText;
        private GGemCoMonsterSettings _monsterSettings;

        /// <summary>
        /// 몬스터 UI가 사용할 몬스터와 런타임 설정 참조를 초기화합니다.
        /// </summary>
        /// <param name="monster">UI 표시 대상 몬스터입니다.</param>
        public void Initialize(Monster monster)
        {
            _monster = monster;
            _sceneGame = SceneGame.Instance;
            AddressableLoaderSettings settingsLoader = AddressableLoaderSettings.Instance;
            _monsterSettings = settingsLoader != null ? settingsLoader.monsterSettings : null;
        }

        public void Dispose()
        {
            if (_sliderHpBar != null)
            {
                UnityEngine.Object.Destroy(_sliderHpBar);
                _sliderHpBar = null;
            }
            if (_monsterUISuperArmor != null)
            {
                UnityEngine.Object.Destroy(_monsterUISuperArmor);
                _monsterUISuperArmor = null;
            }
            DestroyDebugLevelText();
            DestroyDebugHpText();
        }

        /// <summary>
        /// 풀링으로 다시 활성화되거나 런타임 참조가 재구성될 때 몬스터 UI를 현재 설정 기준으로 다시 생성합니다.
        /// </summary>
        public void RebuildRuntimeUi()
        {
            if (_monster == null)
            {
                GcLogger.LogWarning("[MonsterUI] 몬스터 참조가 없어 런타임 UI 재구성을 건너뜁니다.");
                return;
            }

            RefreshRuntimeReferences();

            if (_sliderHpBar == null)
            {
                CreateHpBar();
            }

            if (CanShowWorldSuperArmor())
            {
                CreateSuperArmor();
            }
            else
            {
                DestroySuperArmor();
            }

            RefreshDebugLevelText();
            RefreshDebugHpText();
            if (_monster.CurrentHp != null)
                SetSliderHp(_monster.CurrentHp.Value);
            if (_monster.CurrentSuperArmor != null)
                SetSuperArmor(_monster.CurrentSuperArmor.Value);
        }

        /// <summary>
        /// 몬스터 리소스 변경 이벤트를 머리 위 UI 갱신 함수에 연결합니다.
        /// </summary>
        public void InitSubscribe()
        {
            // 순서 중요. 윈도우 먼저 초기화 해야 한다.
            RefreshRuntimeReferences();

            CreateHpBar();
            if (CanShowWorldSuperArmor())
            {
                CreateSuperArmor();
            }

            RefreshDebugLevelText();
            RefreshDebugHpText();

            _monster.CurrentHp
                .Subscribe(SetSliderHp)
                .AddTo(_monster);

            _monster.MaxHp
                .Subscribe(_ => SetSliderHp(_monster.CurrentHp.Value))
                .AddTo(_monster);

            _monster.CurrentSuperArmor
                .Subscribe(SetSuperArmor)
                .AddTo(_monster);

        }

        /// <summary>
        /// 몬스터 등급에 맞는 머리 위 HP 바를 생성합니다.
        /// </summary>
        private void CreateHpBar()
        {
            if (_sliderHpBar != null) return;
            if (!TryGetMonsterUiContainer(out Transform container)) return;
            if (_monsterSettings == null) return;

            _prefabSliderHpBar = _monsterSettings.GetMonsterHpBar(_monster.Grade);
            if (!_prefabSliderHpBar) return;

            _containerMonsterHpBar = container;
            _sliderHpBar = UnityEngine.Object.Instantiate(_prefabSliderHpBar, _containerMonsterHpBar);
            MonsterHpBar monsterHpBar = _sliderHpBar.GetComponent<MonsterHpBar>();
            if (monsterHpBar == null)
            {
                GcLogger.LogError(
                    $"[MonsterUI] 생성된 HP 바 프리팹에 {nameof(MonsterHpBar)} 컴포넌트가 없습니다. " +
                    $"monsterUid={_monster.uid}, prefab={_prefabSliderHpBar.name}");
                UnityEngine.Object.Destroy(_sliderHpBar);
                _sliderHpBar = null;
                return;
            }

            monsterHpBar.Initialize(_monster);
        }
        /// <summary>
        /// 몬스터 현재 HP를 머리 위 HP 바와 디버그 텍스트에 동기화합니다.
        /// </summary>
        /// <param name="value">현재 HP 값입니다.</param>
        /// <remarks>
        /// 전역 Battle HUD는 플레이어 교전 목록 기반 Presenter가 별도로 갱신합니다.
        /// </remarks>
        private void SetSliderHp(long value)
        {
            if (_sliderHpBar != null)
            {
                MonsterHpBar monsterHpBar = _sliderHpBar.GetComponent<MonsterHpBar>();
                monsterHpBar?.SetValue(value);
            }

            UpdateDebugHpText(value);
        }

        /// <summary>
        /// 런타임에서 필요한 씬과 몬스터 설정 참조를 최신 상태로 갱신합니다.
        /// </summary>
        private void RefreshRuntimeReferences()
        {
            _sceneGame ??= SceneGame.Instance;
            if (_monsterSettings == null)
            {
                AddressableLoaderSettings settingsLoader = AddressableLoaderSettings.Instance;
                _monsterSettings = settingsLoader != null ? settingsLoader.monsterSettings : null;
            }
        }

        /// <summary>
        /// 몬스터 머리 위 UI를 배치할 컨테이너를 가져옵니다.
        /// </summary>
        /// <param name="container">몬스터 머리 위 UI 컨테이너입니다.</param>
        /// <returns>컨테이너를 가져왔으면 true입니다.</returns>
        private bool TryGetMonsterUiContainer(out Transform container)
        {
            container = null;
            SceneGame sceneGame = _sceneGame != null ? _sceneGame : SceneGame.Instance;
            if (sceneGame == null || sceneGame.containerMonsterHpBar == null)
            {
                GcLogger.LogError("SceneGame 에 containerMonsterHpBar 가 설정되지 않았습니다.");
                return false;
            }

            container = sceneGame.containerMonsterHpBar.transform;
            return true;
        }

        /// <summary>
        /// Super Armor 아이콘 생성 기준이 되는 최대 값을 계산합니다.
        /// </summary>
        /// <returns>현재 값과 최대 값 중 더 큰 Super Armor 값입니다.</returns>
        private int GetMaxSuperArmor()
        {
            if (_monster == null) return 0;
            return Mathf.Max(_monster.TotalSuperArmor.Value, _monster.CurrentSuperArmor.Value);
        }

        /// <summary>
        /// 머리 위 Super Armor UI를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>머리 위 Super Armor UI를 표시할 수 있으면 true입니다.</returns>
        private bool CanShowWorldSuperArmor()
        {
            return _monsterSettings != null
                   && _monster != null
                   && _monsterSettings.CanShowWorldSuperArmor(_monster.Grade, GetMaxSuperArmor());
        }

        /// <summary>
        /// 몬스터 레벨 디버그 텍스트 표시 가능 여부를 확인합니다.
        /// </summary>
        /// <returns>레벨 디버그 텍스트를 표시할 수 있으면 true입니다.</returns>
        private bool CanShowDebugLevelText()
        {
            return _monsterSettings != null
                   && _monster != null
                   && _monsterSettings.CanShowSpawnLevelDebugText();
        }

        /// <summary>
        /// 몬스터 HP 숫자 디버그 텍스트 표시 가능 여부를 확인합니다.
        /// </summary>
        /// <returns>HP 숫자 디버그 텍스트를 표시할 수 있으면 true입니다.</returns>
        private bool CanShowDebugHpText()
        {
            return _monsterSettings != null
                   && _monster != null
                   && _monsterSettings.CanShowSpawnHpDebugText();
        }

        /// <summary>
        /// 생성되어 있는 머리 위 Super Armor UI를 제거합니다.
        /// </summary>
        private void DestroySuperArmor()
        {
            if (_monsterUISuperArmor == null) return;
            UnityEngine.Object.Destroy(_monsterUISuperArmor);
            _monsterUISuperArmor = null;
        }

        /// <summary>
        /// 몬스터 디버그 설정에 따라 레벨 텍스트를 생성하거나 제거합니다.
        /// </summary>
        private void RefreshDebugLevelText()
        {
            if (CanShowDebugLevelText())
            {
                CreateDebugLevelText();
                return;
            }

            DestroyDebugLevelText();
        }

        /// <summary>
        /// 스폰된 몬스터의 현재 레벨을 표시하는 디버그 텍스트 오브젝트를 생성합니다.
        /// </summary>
        private void CreateDebugLevelText()
        {
            if (_monsterDebugLevelText != null) return;
            if (!CanShowDebugLevelText()) return;
            if (!TryGetMonsterUiContainer(out Transform container)) return;

            _containerMonsterHpBar = container;
            _monsterDebugLevelText = new GameObject(
                "MonsterDebugLevelText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(CanvasGroup),
                typeof(MonsterDebugLevelText));
            _monsterDebugLevelText.transform.SetParent(_containerMonsterHpBar, false);

            RectTransform rectTransform = _monsterDebugLevelText.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120f, 32f);

            MonsterDebugLevelText levelText = _monsterDebugLevelText.GetComponent<MonsterDebugLevelText>();
            levelText.Initialize(_monster, _monsterSettings);
        }

        /// <summary>
        /// 생성되어 있는 몬스터 레벨 디버그 텍스트를 제거합니다.
        /// </summary>
        private void DestroyDebugLevelText()
        {
            if (_monsterDebugLevelText == null) return;
            UnityEngine.Object.Destroy(_monsterDebugLevelText);
            _monsterDebugLevelText = null;
        }

        /// <summary>
        /// 몬스터 디버그 설정에 따라 HP 숫자 텍스트를 생성하거나 제거합니다.
        /// </summary>
        private void RefreshDebugHpText()
        {
            if (CanShowDebugHpText())
            {
                CreateDebugHpText();
                UpdateDebugHpText(_monster.CurrentHp.Value);
                return;
            }

            DestroyDebugHpText();
        }

        /// <summary>
        /// 스폰된 몬스터의 현재 HP와 최대 HP를 표시하는 디버그 텍스트 오브젝트를 생성합니다.
        /// </summary>
        private void CreateDebugHpText()
        {
            if (_monsterDebugHpText != null) return;
            if (!CanShowDebugHpText()) return;
            if (!TryGetMonsterUiContainer(out Transform container)) return;

            _containerMonsterHpBar = container;
            _monsterDebugHpText = new GameObject(
                "MonsterDebugHpText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(CanvasGroup),
                typeof(MonsterDebugHpText));
            _monsterDebugHpText.transform.SetParent(_containerMonsterHpBar, false);

            RectTransform rectTransform = _monsterDebugHpText.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 32f);

            MonsterDebugHpText hpText = _monsterDebugHpText.GetComponent<MonsterDebugHpText>();
            hpText.Initialize(_monster, _monsterSettings);
        }

        /// <summary>
        /// 생성되어 있는 몬스터 HP 숫자 디버그 텍스트를 제거합니다.
        /// </summary>
        private void DestroyDebugHpText()
        {
            if (_monsterDebugHpText == null) return;
            UnityEngine.Object.Destroy(_monsterDebugHpText);
            _monsterDebugHpText = null;
        }

        /// <summary>
        /// 생성되어 있는 몬스터 HP 숫자 디버그 텍스트 값을 갱신합니다.
        /// </summary>
        /// <param name="currentHp">현재 HP 값입니다.</param>
        private void UpdateDebugHpText(long currentHp)
        {
            if (_monsterDebugHpText == null) return;
            MonsterDebugHpText hpText = _monsterDebugHpText.GetComponent<MonsterDebugHpText>();
            hpText.SetValue(currentHp, _monster.MaxHp.Value);
        }

        public void StartFadeIn()
        {
            if (_sliderHpBar != null)
            {
                _sliderHpBar.GetComponent<MonsterHpBar>().StartFadeIn();
            }
            
            if (_monsterUISuperArmor != null)
            {
                _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>().StartFadeIn();
            }

            if (_monsterDebugLevelText != null)
            {
                _monsterDebugLevelText.GetComponent<MonsterDebugLevelText>().StartFadeIn();
            }

            if (_monsterDebugHpText != null)
            {
                _monsterDebugHpText.GetComponent<MonsterDebugHpText>().StartFadeIn();
            }
        }

        public void StartFadeOut()
        {
            if (_sliderHpBar != null)
            {
                _sliderHpBar.GetComponent<MonsterHpBar>().StartFadeOut();
            }
            if (_monsterUISuperArmor != null)
            {
                _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>().StartFadeOut();
            }
            if (_monsterDebugLevelText != null)
            {
                _monsterDebugLevelText.GetComponent<MonsterDebugLevelText>().StartFadeOut();
            }

            if (_monsterDebugHpText != null)
            {
                _monsterDebugHpText.GetComponent<MonsterDebugHpText>().StartFadeOut();
            }
        }
        
        /// <summary>
        /// 등급별 HP 바 프리팹 정책을 만족할 때 머리 위 Super Armor UI를 생성합니다.
        /// </summary>
        private void CreateSuperArmor()
        {
            if (_monsterUISuperArmor != null) return;
            if (!CanShowWorldSuperArmor()) return;
            if (!TryGetMonsterUiContainer(out Transform container)) return;

            _prefabPanelMonsterSuperArmor = ConfigResources.PanelMonsterSuperArmor.Load();
            if (_prefabPanelMonsterSuperArmor == null) return;

            _containerMonsterHpBar = container;
            _monsterUISuperArmor = UnityEngine.Object.Instantiate(_prefabPanelMonsterSuperArmor, _containerMonsterHpBar);
            MonsterUISuperArmor monsterSuperArmor = _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>();
            if (monsterSuperArmor == null)
            {
                GcLogger.LogError(
                    $"[MonsterUI] 생성된 슈퍼아머 프리팹에 {nameof(MonsterUISuperArmor)} 컴포넌트가 없습니다. " +
                    $"monsterUid={_monster.uid}, prefab={_prefabPanelMonsterSuperArmor.name}");
                UnityEngine.Object.Destroy(_monsterUISuperArmor);
                _monsterUISuperArmor = null;
                return;
            }

            monsterSuperArmor.Initialize(_monster);
            if (_monster.CurrentSuperArmor != null)
                monsterSuperArmor.SetValue(_monster.CurrentSuperArmor.Value);
        }
        
        /// <summary>
        /// Super Armor 값 변경을 머리 위 UI에 반영합니다.
        /// </summary>
        /// <param name="value">현재 Super Armor 값입니다.</param>
        public void SetSuperArmor(int value)
        {
            if (CanShowWorldSuperArmor())
            {
                CreateSuperArmor();
                if (_monsterUISuperArmor != null) 
                {
                    MonsterUISuperArmor monsterSuperArmor =
                        _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>();
                    monsterSuperArmor?.SetValue(value);
                }
            }
            else
            {
                DestroySuperArmor();
            }
        }
    }
}
