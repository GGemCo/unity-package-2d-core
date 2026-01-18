using System;
using System.Collections.Generic;
using GGemCo2DAffect;
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
        private LocalizationManager _localizationManager;
        
        private UIWindowHud _uiWindowHud;
        private UIWindowPlayerInfo _uiWindowPlayerInfo;
        private UIWindowPlayerBuffInfo _uiWindowPlayerBuffInfo;

        private PlayerAffectUiPresenter _affectUiPresenter;
        
        [Serializable]
        private struct StatUIBinding
        {
            public UIWindowPlayerInfo.IndexPlayerInfo textUI;
            public Func<Player, BehaviorSubject<long>> getStat;
            public string label;
        }
        private readonly List<StatUIBinding> _statBindings = new();
        public void Initialize(Player player)
        {
            _player = player;
            _localizationManager = LocalizationManager.Instance;
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
                _affectUiPresenter = _player.GetComponent<PlayerAffectUiPresenter>();
                if (_affectUiPresenter == null)
                    _affectUiPresenter = _player.gameObject.AddComponent<PlayerAffectUiPresenter>();

                var affectComp = _player.GetComponent<AffectComponent>();
                if (affectComp != null)
                    _affectUiPresenter.Bind(affectComp, _uiWindowPlayerBuffInfo);
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
            
            InitializeStatBindings();
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
        /// <summary>
        /// Player의 스탯과 UI를 매핑하여 리스트에 저장
        /// </summary>
        private void InitializeStatBindings()
        {
            _statBindings.AddRange(new[]
            {
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Atk, getStat = p => p.TotalAtk, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatAtk) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Def, getStat = p => p.TotalDef, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatDef) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Hp, getStat = p => p.TotalHp, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatHp) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Mp, getStat = p => p.TotalMp, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatMp) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.MoveSpeed, getStat = p => p.TotalMoveSpeed, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatMoveSpeed) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.AttackSpeed, getStat = p => p.TotalAttackSpeed, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatAttackSpeed) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.CriticalDamage, getStat = p => p.TotalCriticalDamage, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatCriticalDamage) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.CriticalProbability, getStat = p => p.TotalCriticalProbability, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatCriticalProbability) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.RegistFire, getStat = p => p.TotalRegistFire, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatResistanceFire) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.RegistCold, getStat = p => p.TotalRegistCold, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatResistanceCold) },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.RegistLightning, getStat = p => p.TotalRegistLightning, label = _localizationManager.GetStatusNameByKey(ConfigCommon.StatusStatResistanceLightning) }
            });
            foreach (var binding in _statBindings)
            {
                binding.getStat(_player).DistinctUntilChanged()
                    .Subscribe(value => UpdatePlayerInfoText(binding.textUI, binding.label, value))
                    .AddTo(_player);
            }
        }
        /// <summary>
        /// UIWindowPlayerInfo 에 text 업데이트 하기
        /// </summary>
        /// <param name="textUI"></param>
        /// <param name="label"></param>
        /// <param name="value"></param>
        private void UpdatePlayerInfoText(UIWindowPlayerInfo.IndexPlayerInfo textUI, string label, long value)
        {
            if (_uiWindowPlayerInfo == null) return;
            _uiWindowPlayerInfo.UpdateText(textUI, label, value);
        }
    }
}