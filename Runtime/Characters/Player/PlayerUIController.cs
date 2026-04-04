using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어와 연관된 HUD, 플레이어 정보창, 버프 정보창 UI를 초기화하고
    /// 플레이어 상태 변화에 따라 각 UI를 갱신합니다.
    /// </summary>
    public class PlayerUIController
    {
        private Player _player;
        private SceneGame _sceneGame;

        private UIWindowHud _uiWindowHud;
        private UIWindowPlayerInfo _uiWindowPlayerInfo;
        private UIWindowPlayerBuffInfo _uiWindowPlayerBuffInfo;
        private GGemCoPlayerSettings _playerSettings;

        private long _lastObservedHp;
        private bool _isHpInitialized;
        
        private Color _textColorHeal;

        /// <summary>
        /// 플레이어 스탯과 PlayerInfo UI 항목 간의 바인딩 정보를 정의합니다.
        /// </summary>
        [Serializable]
        private struct StatUIBinding
        {
            /// <summary>
            /// 값을 출력할 PlayerInfo UI 항목 식별자입니다.
            /// </summary>
            public CharacterConstants.IndexPlayerInfo textUI;

            /// <summary>
            /// 플레이어로부터 대상 스탯 Observable을 가져오는 함수입니다.
            /// </summary>
            public Func<Player, BehaviorSubject<long>> GetStat;
        }

        private readonly List<StatUIBinding> _statBindings = new();

        /// <summary>
        /// 플레이어 UI 제어에 필요한 기본 참조를 초기화합니다.
        /// </summary>
        /// <param name="player">UI와 상태 변화를 연결할 대상 플레이어입니다.</param>
        public void Initialize(Player player)
        {
            _player = player;
            _sceneGame = SceneGame.Instance;
            _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            
            if (AddressableLoaderSettings.Instance.settings)
            {
                _textColorHeal = AddressableLoaderSettings.Instance.settings.textColorHeal;
            }
        }

        /// <summary>
        /// 플레이어 상태와 각 UI 요소를 구독 방식으로 연결하고 초기 바인딩을 수행합니다.
        /// </summary>
        public void InitSubscribe()
        {
            // 순서 중요. 윈도우를 먼저 획득해야 이후 갱신 로직이 정상 동작한다.
            _uiWindowHud = _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowHud>(UIWindowConstants.WindowUid.Hud);
            _uiWindowPlayerInfo =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerInfo>(UIWindowConstants.WindowUid.PlayerInfo);
            _uiWindowPlayerBuffInfo =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerBuffInfo>(
                    UIWindowConstants.WindowUid.PlayerBuffInfo);

            // Affect UI 바인딩(단일 진실 소스: AffectComponent)
            if (_player != null && _uiWindowPlayerBuffInfo != null)
            {
                // Affect 패키지가 설치된 경우에만 Player 버프 UI를 자동 바인딩한다.
                // Core는 Affect를 직접 참조하지 않으므로 브리지 경유로 연결한다.
                AffectRuntimeBridge.TryBindPlayerBuffInfo(_player, _uiWindowPlayerBuffInfo);
            }

            if (_player != null && _uiWindowHud != null && _uiWindowHud.gameObjectHp is IAffectHudVisualStateReceiver affectHudReceiver)
            {
                AffectRuntimeBridge.TryBindPlayerHudAffectState(_player, affectHudReceiver);
            }

            // 최대 HP/현재 HP 중 어느 값이 바뀌어도 HUD HP 표시를 다시 계산한다.
            _player.TotalHp
                .Subscribe(_ => SetWindowHudHp())
                .AddTo(_player);

            _player.CurrentHp
                .Subscribe(_ => SetWindowHudHp())
                .AddTo(_player);

            _lastObservedHp = _player.CurrentHp.Value;
            _isHpInitialized = true;

            _player.CurrentHp
                .Subscribe(OnPlayerHpChangedForFloatingText)
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

            _player.TotalHpTemp
                .Subscribe(_ => SetWindowHudHpTemp())
                .AddTo(_player);

            _player.CurrentHpTemp
                .Subscribe(_ => SetWindowHudHpTemp())
                .AddTo(_player);

            _player.CurrentBattleStatus
                .Subscribe(_ => SetWindowHudBattle(_player.CurrentBattleStatus.Value))
                .AddTo(_player);

            InitializeStatBindings();

            // StatPoint UI(PlayerInfo) 바인딩
            if (_uiWindowPlayerInfo != null && _player != null)
            {
                // PlayerInfo 윈도우가 라벨(Localization)을 1회 적용하고 이후 값만 갱신하도록 초기 바인딩한다.
                _uiWindowPlayerInfo.BindPlayer(_player);

                // 저장 데이터의 스탯 포인트 변경 시 PlayerInfo 표시값을 다시 갱신한다.
                var playerData = _sceneGame.saveDataManager.Player;
                if (playerData != null)
                {
                    playerData.OnStatPointsChanged()
                        .Subscribe(_ => _uiWindowPlayerInfo.RefreshValues())
                        .AddTo(_player);
                }
            }
        }

        /// <summary>
        /// 현재 HP와 최대 HP를 기준으로 HUD의 HP 표시를 갱신합니다.
        /// </summary>
        private void SetWindowHudHp()
        {
            if (_uiWindowHud == null)
            {
                return;
            }

            _uiWindowHud.SetHp(_player.CurrentHp.Value, _player.TotalHp.Value);
        }

        /// <summary>
        /// 임시 HP와 임시 최대 HP를 기준으로 HUD의 임시 HP 표시를 갱신합니다.
        /// </summary>
        private void SetWindowHudHpTemp()
        {
            if (_uiWindowHud == null) return;

            _uiWindowHud.SetHpTemp(_player.CurrentHpTemp.Value, _player.TotalHpTemp.Value);
        }

        /// <summary>
        /// 현재 MP와 최대 MP를 기준으로 HUD의 MP 슬라이더를 갱신합니다.
        /// </summary>
        /// <param name="value">현재 MP 값입니다.</param>
        private void SetWindowHudSliderMp(long value)
        {
            if (_uiWindowHud == null)
            {
                return;
            }

            _uiWindowHud.SetMp(value, _player.TotalMp.Value);
        }

        /// <summary>
        /// 현재 스태미나와 최대 스태미나를 기준으로 HUD의 스태미나 슬라이더를 갱신합니다.
        /// </summary>
        /// <param name="value">현재 스태미나 값입니다.</param>
        private void SetWindowHudSliderStamina(long value)
        {
            if (_uiWindowHud == null)
            {
                return;
            }

            _uiWindowHud.SetStamina(value, _player.TotalStamina.Value);
        }

        /// <summary>
        /// 현재 전투 상태를 HUD에 반영합니다.
        /// </summary>
        /// <param name="value">표시할 플레이어의 전투 상태입니다.</param>
        private void SetWindowHudBattle(CharacterConstants.BattleStatus value)
        {
            if (_uiWindowHud == null)
            {
                return;
            }

            _uiWindowHud.SetBattleStatus(value);
        }

        /// <summary>
        /// 플레이어의 주요 스탯과 PlayerInfo UI 항목 간의 바인딩을 구성하고,
        /// 값 변경 시 UI가 자동 갱신되도록 구독을 등록합니다.
        /// </summary>
        private void InitializeStatBindings()
        {
            _statBindings.AddRange(new[]
            {
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Atk, GetStat = p => p.TotalAtk },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Def, GetStat = p => p.TotalDef },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Hp, GetStat = p => p.TotalHp },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Mp, GetStat = p => p.TotalMp },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.Stamina, GetStat = p => p.TotalStamina },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.MoveSpeed, GetStat = p => p.TotalMoveSpeed },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.AttackSpeed, GetStat = p => p.TotalAttackSpeed },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.CriticalDamage, GetStat = p => p.TotalCriticalDamage },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.CriticalProbability, GetStat = p => p.TotalCriticalProbability },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistFire, GetStat = p => p.TotalRegistFire },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistCold, GetStat = p => p.TotalRegistCold },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistLightning, GetStat = p => p.TotalRegistLightning },
                new StatUIBinding { textUI = CharacterConstants.IndexPlayerInfo.RegistPoison, GetStat = p => p.TotalRegistPoison }
            });

            foreach (var binding in _statBindings)
            {
                binding.GetStat(_player)
                    .DistinctUntilChanged()
                    .Subscribe(value => UpdatePlayerInfoValue(binding.textUI, value))
                    .AddTo(_player);
            }
        }

        /// <summary>
        /// PlayerInfo 윈도우의 특정 스탯 항목 표시값을 갱신합니다.
        /// </summary>
        /// <param name="textUI">갱신할 UI 항목 식별자입니다.</param>
        /// <param name="value">UI에 표시할 스탯 값입니다.</param>
        private void UpdatePlayerInfoValue(CharacterConstants.IndexPlayerInfo textUI, long value)
        {
            if (_uiWindowPlayerInfo == null) return;

            _uiWindowPlayerInfo.UpdateValue(textUI, value);
        }

        /// <summary>
        /// 플레이어 HP 변화를 감지하여 회복량이 발생한 경우 플로팅 텍스트를 표시합니다.
        /// </summary>
        /// <param name="currentHp">변경 후 현재 HP 값입니다.</param>
        private void OnPlayerHpChangedForFloatingText(long currentHp)
        {
            if (!_isHpInitialized)
            {
                _lastObservedHp = currentHp;
                _isHpInitialized = true;
                return;
            }

            long delta = currentHp - _lastObservedHp;
            _lastObservedHp = currentHp;

            if (delta <= 0) return;

            ShowHealText(delta);
        }

        /// <summary>
        /// 스테미나 HUD 피격 피드백을 프리셋 기본 방향으로 재생합니다.
        /// </summary>
        public void PlayStaminaDamageFeedback()
        {
            _uiWindowHud?.PlayStaminaDamageFeedback();
        }

        /// <summary>
        /// 공격자의 월드 위치를 기준으로 스테미나 HUD 피격 피드백 방향을 결정하여 재생합니다.
        /// </summary>
        /// <param name="attackerWorldPosition">피격을 발생시킨 공격자 월드 위치입니다.</param>
        public void PlayStaminaDamageFeedbackFromAttackerPosition(Vector3 attackerWorldPosition)
        {
            if (_player == null)
            {
                return;
            }

            var directionMode = ResolveDamageShakeDirection(_player.transform.position.x, attackerWorldPosition.x);
            _uiWindowHud?.PlayStaminaDamageFeedback(directionMode);
        }

        /// <summary>
        /// 공격자 Transform을 기준으로 스테미나 HUD 피격 피드백 방향을 결정하여 재생합니다.
        /// </summary>
        /// <param name="attackerTransform">피격을 발생시킨 공격자의 Transform입니다.</param>
        public void PlayStaminaDamageFeedbackFromAttacker(Transform attackerTransform)
        {
            if (attackerTransform == null)
            {
                _uiWindowHud?.PlayStaminaDamageFeedback();
                return;
            }

            PlayStaminaDamageFeedbackFromAttackerPosition(attackerTransform.position);
        }

        /// <summary>
        /// 피해자와 공격자의 X축 위치를 비교해 HUD 흔들림 방향을 계산합니다.
        /// 공격자가 오른쪽에 있으면 왼쪽으로, 공격자가 왼쪽에 있으면 오른쪽으로 흔들립니다.
        /// </summary>
        /// <param name="victimWorldX">피해자 X 위치입니다.</param>
        /// <param name="attackerWorldX">공격자 X 위치입니다.</param>
        /// <returns>HUD 흔들림 방향입니다.</returns>
        private static UIEffectShakeDirectionMode ResolveDamageShakeDirection(float victimWorldX, float attackerWorldX)
        {
            return attackerWorldX >= victimWorldX
                ? UIEffectShakeDirectionMode.Left
                : UIEffectShakeDirectionMode.Right;
        }

        /// <summary>
        /// 플레이어 위치 기준으로 회복량 플로팅 텍스트를 생성합니다.
        /// </summary>
        /// <param name="value">표시할 회복량입니다.</param>
        private void ShowHealText(long value)
        {
            if (value <= 0) return;
            if (_player == null) return;
            if (_sceneGame == null) return;
            if (_sceneGame.damageTextManager == null) return;

            var metadataDamageText = new MetadataDamageText
            {
                Damage = value,
                Color = _textColorHeal,
                WorldPosition = _player.transform.position + 
                                new Vector3(0f, _player.GetHeight() * Mathf.Abs(_player.originalScaleX), 0f)
            };

            _sceneGame.damageTextManager.ShowDamageText(metadataDamageText);
        }
    }
}