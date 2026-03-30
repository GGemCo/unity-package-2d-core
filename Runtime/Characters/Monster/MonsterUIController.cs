using System;
using System.Collections.Generic;
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

        public void RebuildRuntimeUi()
        {
            _sceneGame ??= SceneGame.Instance;
            _monsterSettings ??= AddressableLoaderSettings.Instance.monsterSettings;
            _uiWindowBattleHudMonster =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowBattleHudMonster>(UIWindowConstants.WindowUid.BattleHudMonster);

            if (_sliderHpBar == null)
            {
                CreateHpBar();
            }

            if (_monster.CurrentSuperArmor.Value > 0 && _monsterUISuperArmor == null)
            {
                CreateSuperArmor();
            }

            SetSliderHp(_monster.CurrentHp.Value);
            if (_monster.CurrentSuperArmor.Value > 0)
            {
                SetSuperArmor(_monster.CurrentSuperArmor.Value);
            }
            SetBattleStatus(_monster.CurrentBattleStatus.Value);
        }

        public void InitSubscribe()
        {
            // 순서 중요. 윈도우 먼저 초기화 해야 한다.
            _uiWindowBattleHudMonster =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowBattleHudMonster>(UIWindowConstants.WindowUid
                    .BattleHudMonster);

            CreateHpBar();
            _monster.CurrentHp
                .Subscribe(SetSliderHp)
                .AddTo(_monster);

            if (_monster.CurrentSuperArmor.Value > 0)
            {
                CreateSuperArmor();
                _monster.CurrentSuperArmor
                    .Subscribe(SetSuperArmor)
                    .AddTo(_monster);
            }
            
            _monster.CurrentBattleStatus
                .Subscribe(_ => SetBattleStatus(_monster.CurrentBattleStatus.Value))
                .AddTo(_monster);

        }

        private void CreateHpBar()
        {
            if (SceneGame.Instance.containerMonsterHpBar == null)
            {
                GcLogger.LogError("SceneGame 에 containerMonsterHpBar 가 설정되지 않았습니다.");
                return;
            }
            _prefabSliderHpBar = _monsterSettings.GetMonsterHpBar(_monster.Grade);
            if (!_prefabSliderHpBar) return;
            _containerMonsterHpBar = SceneGame.Instance.containerMonsterHpBar.transform;
            _sliderHpBar = UnityEngine.Object.Instantiate(_prefabSliderHpBar, _containerMonsterHpBar);
            MonsterHpBar monsterHpBar = _sliderHpBar.GetComponent<MonsterHpBar>();
            monsterHpBar.Initialize(_monster);
        }
        /// <summary>
        /// 전투 시작, 종료 호출 시 처리
        /// </summary>
        /// <param name="value"></param>
        private void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            if (!_monsterSettings.UseBattleHud) return;
            bool useBattleHud = _monsterSettings.IsBattleHudEnabledFor(_monster.Grade);
            if (!useBattleHud) return;
            
            if (_uiWindowBattleHudMonster != null)
            {
                _uiWindowBattleHudMonster.Show(value == CharacterConstants.BattleStatus.InBattle);
                if (value == CharacterConstants.BattleStatus.None) return;
                _uiWindowBattleHudMonster.UpdateInfo(_monster);
            }
        }

        private void SetSliderHp(long value)
        {
            if (_sliderHpBar != null)
            {
                _sliderHpBar.GetComponent<MonsterHpBar>().SetValue(value);    
            }
            if (_uiWindowBattleHudMonster != null) 
            {
                _uiWindowBattleHudMonster.SetSliderHp(value, _monster.TotalHp.Value);
            }
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
        /// 슈퍼 아머 UI 만들기
        /// </summary>
        private void CreateSuperArmor()
        {
            if (SceneGame.Instance.containerMonsterHpBar == null)
            {
                GcLogger.LogError("SceneGame 에 containerMonsterHpBar 가 설정되지 않았습니다.");
                return;
            }
            _prefabPanelMonsterSuperArmor = ConfigResources.PanelMonsterSuperArmor.Load();
            if (_prefabPanelMonsterSuperArmor == null) return;
            _containerMonsterHpBar ??= SceneGame.Instance.containerMonsterHpBar.transform;
            _monsterUISuperArmor = UnityEngine.Object.Instantiate(_prefabPanelMonsterSuperArmor, _containerMonsterHpBar);
            MonsterUISuperArmor monsterSuperArmor = _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>();
            monsterSuperArmor.Initialize(_monster);
        }
        
        public void SetSuperArmor(int value)
        {
            // GcLogger.Log("SetSuperArmor: " + value);
            if (_monsterUISuperArmor != null) 
            {
                _monsterUISuperArmor.GetComponent<MonsterUISuperArmor>().SetValue(value);   
            }
            if (_uiWindowBattleHudMonster != null) 
            {
                _uiWindowBattleHudMonster.SetSuperArmor(value);
            }
        }
    }
}