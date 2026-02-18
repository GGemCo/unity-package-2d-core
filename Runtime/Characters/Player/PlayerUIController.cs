using System;
using System.Collections.Generic;
using R3;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어와 연관된 UI 관리 클래스
    /// </summary>
    public class PlayerUIController
    {
        private Player _player;
        private SceneGame _sceneGame;
        
        private UIWindowHud _uiWindowHud;
        private UIWindowPlayerInfo _uiWindowPlayerInfo;
        private UIWindowPlayerBuffInfo _uiWindowPlayerBuffInfo;
        
        [Serializable]
        private struct StatUIBinding
        {
            public CharacterConstants.IndexPlayerInfo textUI;
            public Func<Player, BehaviorSubject<long>> getStat;
        }
        private readonly List<StatUIBinding> _statBindings = new();
        public void Initialize(Player player)
        {
            _player = player;
            _sceneGame = SceneGame.Instance;
        }

        public void InitSubscribe()
        {
            // 순서 중요. 윈도우 먼저 초기화 해야 한다.
            _uiWindowHud = _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowHud>(UIWindowConstants.WindowUid.Hud);
            _uiWindowPlayerInfo =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerInfo>(UIWindowConstants.WindowUid.PlayerInfo);
            _uiWindowPlayerBuffInfo =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerBuffInfo>(UIWindowConstants.WindowUid
                    .PlayerBuffInfo);

            // Affect UI 바인딩(단일 진실 소스: AffectComponent)
            if (_player != null && _uiWindowPlayerBuffInfo != null)
            {
                // Affect 패키지가 설치되어 있으면 Player 버프 UI를 자동 바인딩한다.
                // (Core는 Affect를 직접 참조하지 않는다.)
                AffectRuntimeBridge.TryBindPlayerBuffInfo(_player, _uiWindowPlayerBuffInfo);
            }

            // TotalHp, Mp 가 바뀌어도 현재 값이 바뀌면 안된다.
            _player.TotalHp
                .Subscribe(_ => SetWindowHudSliderHp(_player.CurrentHp.Value))
                .AddTo(_player);
            _player.CurrentHp
                .Subscribe(_ => SetWindowHudSliderHp(_player.CurrentHp.Value))
                .AddTo(_player);
            _player.TotalMp
                .Subscribe(_ => SetWindowHudSliderMp(_player.CurrentMp.Value))
                .AddTo(_player);
            _player.CurrentMp
                .Subscribe(_ => SetWindowHudSliderMp(_player.CurrentMp.Value))
                .AddTo(_player);
            _player.CurrentStamina
                .Subscribe(_ => SetWindowHudSliderStamina(_player.CurrentStamina.Value))
                .AddTo(_player);

            _player.CurrentBattleStatus
                .Subscribe(_ => SetWindowHudBattle(_player.CurrentBattleStatus.Value))
                .AddTo(_player);
            
            InitializeStatBindings();

            // StatPoint UI(PlayerInfo) 바인딩
            if (_uiWindowPlayerInfo != null && _player != null)
            {
                // PlayerInfo 윈도우가 라벨(Localization)을 1회만 적용하고, 이후 값만 갱신하도록 바인딩
                _uiWindowPlayerInfo.BindPlayer(_player);

                // 포인트 변경 시 갱신
                var playerData = _sceneGame.saveDataManager.Player;
                if (playerData != null)
                {
                    playerData.OnStatPointsChanged()
                        .Subscribe(_ => _uiWindowPlayerInfo.RefreshValues())
                        .AddTo(_player);
                }
            }
        }
        private void SetWindowHudSliderHp(long value)
        {
            if (_uiWindowHud == null)
            {
                return;
            }
            _uiWindowHud.SetSliderHp(value, _player.TotalHp.Value);
        }
        private void SetWindowHudSliderMp(long value)
        {
            if (_uiWindowHud == null) 
            {
                return;
            }
            _uiWindowHud.SetSliderMp(value, _player.TotalMp.Value);
        }
        private void SetWindowHudSliderStamina(long value)
        {
            if (_uiWindowHud == null) 
            {
                return;
            }
            _uiWindowHud.SetSliderStamina(value, _player.TotalStamina.Value);
        }

        private void SetWindowHudBattle(CharacterConstants.BattleStatus value)
        {
            if (_uiWindowHud == null) 
            {
                return;
            }
            _uiWindowHud.SetBattleStatus(value);
        }

        /// <summary>
        /// Player의 스탯과 UI를 매핑하여 리스트에 저장
        /// </summary>
        private void InitializeStatBindings()
        {
            _statBindings.AddRange(new[]
            {
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Atk, getStat = p => p.TotalAtk },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Def, getStat = p => p.TotalDef },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Hp, getStat = p => p.TotalHp },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Mp, getStat = p => p.TotalMp },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Stamina, getStat = p => p.TotalStamina },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.MoveSpeed, getStat = p => p.TotalMoveSpeed },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.AttackSpeed, getStat = p => p.TotalAttackSpeed },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.CriticalDamage, getStat = p => p.TotalCriticalDamage },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.CriticalProbability, getStat = p => p.TotalCriticalProbability },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistFire, getStat = p => p.TotalRegistFire },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistCold, getStat = p => p.TotalRegistCold },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistLightning, getStat = p => p.TotalRegistLightning },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistPoison, getStat = p => p.TotalRegistPoison }
            });
            foreach (var binding in _statBindings)
            {
                binding.getStat(_player).DistinctUntilChanged()
                    .Subscribe(value => UpdatePlayerInfoValue(binding.textUI, value))
                    .AddTo(_player);
            }
        }
        /// <summary>
        /// UIWindowPlayerInfo 에 text 업데이트 하기
        /// </summary>
        /// <param name="textUI"></param>
        /// <param name="label"></param>
        /// <param name="value"></param>
        private void UpdatePlayerInfoValue(CharacterConstants.IndexPlayerInfo textUI, long value)
        {
            if (_uiWindowPlayerInfo == null) return;
            _uiWindowPlayerInfo.UpdateValue(textUI, value);
        }
    }
}