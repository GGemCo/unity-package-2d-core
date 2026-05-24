using R3;
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

        private UIWindowBattleHudMonster _uiWindowBattleHudMonster;
        
        private GameObject _prefabSliderHpBar;
        private Transform _containerMonsterHpBar;
        private GameObject _prefabPanelMonsterSuperArmor;
        private GameObject _sliderHpBar;
        private GameObject _monsterUISuperArmor;
        private GGemCoMonsterSettings _monsterSettings;
        
        public void Initialize(Monster monster)
        {
            _monster = monster;
            _sceneGame = SceneGame.Instance;
            _monsterSettings = AddressableLoaderSettings.Instance.monsterSettings;
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
            if (_uiWindowBattleHudMonster != null)
                _uiWindowBattleHudMonster.Show(false);
        }

        /// <summary>
        /// 풀링으로 다시 활성화되거나 런타임 참조가 재구성될 때 몬스터 UI를 현재 설정 기준으로 다시 생성합니다.
        /// </summary>
        public void RebuildRuntimeUi()
        {
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

            SetSliderHp(_monster.CurrentHp.Value);
            SetSuperArmor(_monster.CurrentSuperArmor.Value);
            SetBattleStatus(_monster.CurrentBattleStatus.Value);
        }

        /// <summary>
        /// 몬스터 리소스와 전투 상태 변경 이벤트를 UI 갱신 함수에 연결합니다.
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

            _monster.CurrentHp
                .Subscribe(SetSliderHp)
                .AddTo(_monster);

            _monster.CurrentSuperArmor
                .Subscribe(SetSuperArmor)
                .AddTo(_monster);
            
            _monster.CurrentBattleStatus
                .Subscribe(_ => SetBattleStatus(_monster.CurrentBattleStatus.Value))
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
            monsterHpBar.Initialize(_monster);
        }
        /// <summary>
        /// 전투 상태 변경에 따라 Battle HUD 표시 여부와 표시 데이터를 갱신합니다.
        /// </summary>
        /// <param name="value">현재 몬스터 전투 상태입니다.</param>
        /// <remarks>
        /// 윈도우 초기화 순서로 HUD 참조가 늦게 준비될 수 있어,
        /// 표시 갱신 시점에 참조를 한 번 더 복구 시도합니다.
        /// </remarks>
        private void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            if (_uiWindowBattleHudMonster == null)
            {
                RefreshRuntimeReferences();
                if (_uiWindowBattleHudMonster == null) return;
            }

            bool canShowBattleHud = CanShowBattleHud();
            bool isInBattle = value == CharacterConstants.BattleStatus.InBattle;
            bool shouldShow = canShowBattleHud && isInBattle;
            _uiWindowBattleHudMonster.Show(shouldShow);

            if (!shouldShow)
            {
                _uiWindowBattleHudMonster.SetSuperArmor(0);
                return;
            }

            _uiWindowBattleHudMonster.UpdateInfo(_monster, CanShowBattleHudSuperArmor());
            SyncBattleHudHpOnShow();
        }

        /// <summary>
        /// 몬스터 현재 HP를 월드 HP 바와 Battle HUD에 동기화합니다.
        /// </summary>
        /// <param name="value">현재 HP 값입니다.</param>
        /// <remarks>
        /// Battle HUD가 지연 생성되는 프레임에서도 HP 변경을 누락하지 않도록
        /// HUD 참조를 재확인한 뒤 슬라이더를 갱신합니다.
        /// </remarks>
        private void SetSliderHp(long value)
        {
            if (_sliderHpBar != null)
            {
                _sliderHpBar.GetComponent<MonsterHpBar>().SetValue(value);    
            }

            if (_uiWindowBattleHudMonster == null)
            {
                RefreshRuntimeReferences();
            }

            if (_uiWindowBattleHudMonster != null)
            {
                _uiWindowBattleHudMonster.SetSliderHp(value, _monster.TotalHp.Value);
            }
        }

        /// <summary>
        /// Battle HUD가 다시 표시되는 순간 최신 HP를 즉시 반영합니다.
        /// </summary>
        /// <remarks>
        /// HUD가 숨김 상태일 때 발생한 HP 변경 이벤트가 시각적으로 누락될 수 있어,
        /// 표시 전환 직후 한 번 더 현재 HP/최대 HP를 강제 동기화합니다.
        /// </remarks>
        private void SyncBattleHudHpOnShow()
        {
            if (_monster == null || _uiWindowBattleHudMonster == null)
            {
                return;
            }

            _uiWindowBattleHudMonster.SetSliderHp(_monster.CurrentHp.Value, _monster.TotalHp.Value);
        }

        /// <summary>
        /// 런타임에서 필요한 씬, 설정, HUD 참조를 최신 상태로 갱신합니다.
        /// </summary>
        private void RefreshRuntimeReferences()
        {
            _sceneGame ??= SceneGame.Instance;
            _monsterSettings ??= AddressableLoaderSettings.Instance.monsterSettings;
            _uiWindowBattleHudMonster =
                _sceneGame?.uIWindowManager?.GetUIWindowByUid<UIWindowBattleHudMonster>(UIWindowConstants.WindowUid.BattleHudMonster);
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
        /// Battle HUD를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>Battle HUD를 표시할 수 있으면 true입니다.</returns>
        private bool CanShowBattleHud()
        {
            return _monsterSettings != null
                   && _monster != null
                   && _monsterSettings.IsBattleHudEnabledFor(_monster.Grade);
        }

        /// <summary>
        /// Battle HUD 안의 Super Armor UI를 표시할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>Battle HUD Super Armor UI를 표시할 수 있으면 true입니다.</returns>
        private bool CanShowBattleHudSuperArmor()
        {
            return _monsterSettings != null
                   && _monster != null
                   && _monsterSettings.CanShowBattleHudSuperArmor(_monster.Grade, GetMaxSuperArmor());
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
            monsterSuperArmor.Initialize(_monster);
            monsterSuperArmor.SetValue(_monster.CurrentSuperArmor.Value);
        }
        
        /// <summary>
        /// Super Armor 값 변경을 머리 위 UI와 Battle HUD UI에 반영합니다.
        /// </summary>
        /// <param name="value">현재 Super Armor 값입니다.</param>
        public void SetSuperArmor(int value)
        {
            if (CanShowWorldSuperArmor())
            {
                CreateSuperArmor();
                if (_monsterUISuperArmor != null) 
                {
                    _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>().SetValue(value);   
                }
            }
            else
            {
                DestroySuperArmor();
            }

            if (_uiWindowBattleHudMonster != null) 
            {
                _uiWindowBattleHudMonster.SetSuperArmor(CanShowBattleHudSuperArmor() ? value : 0);
            }
        }
    }
}
