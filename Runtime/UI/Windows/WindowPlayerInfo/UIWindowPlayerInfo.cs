using System;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 stat 정보 보여주는 윈도우
    /// </summary>
    public class UIWindowPlayerInfo : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("StatPoint")]
        [Tooltip("스탯 라인 프리팹(UIElementStat) - 비활성 템플릿으로 두고 런타임에 복제합니다.")]
        public GameObject prefabElementStat;
        [Tooltip("UIElementStat 오브젝트를 넣을 오브젝트")]
        public GameObject containerElement;
        [Tooltip("미사용 스탯 포인트")]
        public TextMeshProUGUI textUnspent;

        [Header("Apply / Reset")]
        [Tooltip("드래프트(미적용) 스탯 포인트를 실제 데이터에 반영")]
        public Button buttonApply;
        [Tooltip("드래프트를 원본으로 되돌림(미적용 변경사항 폐기)")]
        public Button buttonReset;

        private readonly Dictionary<CharacterConstants.IndexPlayerInfo, UIElementStat> _playerInfos = new();
        private Player _boundPlayer;

        private StatPointEditSession _editSession;

        private bool _labelsApplied;
        private string _unspentPrefix;
        private readonly Dictionary<CharacterConstants.IndexPlayerInfo, string> _labelCache = new();
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.PlayerInfo;
            if (TableLoaderManager.Instance == null) return;
            // 순서 중요
            InitializeStatPointLines();
            base.Awake();

            if (buttonApply != null) buttonApply.onClick.AddListener(OnClickApply);
            if (buttonReset != null) buttonReset.onClick.AddListener(OnClickReset);
        }

        private void OnDestroy()
        {
            if (buttonApply != null) buttonApply.onClick.RemoveAllListeners();
            if (buttonReset != null) buttonReset.onClick.RemoveAllListeners();
        }
        /// <summary>
        /// PlayerInfo 창 내부에서 스탯포인트 투자 라인을 동적으로 생성합니다.
        /// prefabElementStat는 템플릿(비활성)로 두고, 런타임에 복제하여 사용합니다.
        /// </summary>
        private void InitializeStatPointLines()
        {
            _playerInfos.Clear();
            foreach (CharacterConstants.IndexPlayerInfo indexPlayerInfo in Enum.GetValues(typeof(CharacterConstants.IndexPlayerInfo)))
            {
                if (indexPlayerInfo == CharacterConstants.IndexPlayerInfo.None) continue;
                CreateStatPointLine(indexPlayerInfo, containerElement.transform);    
            }
        }

        private void CreateStatPointLine(CharacterConstants.IndexPlayerInfo idx, Transform parent) {
            if (prefabElementStat == null) return;

            var go = Instantiate(prefabElementStat, parent);
            go.name = $"{prefabElementStat.name}_{idx}";
            go.SetActive(true);

            var element = go.GetComponent<UIElementStat>();
            if (element == null)
            {
                Debug.LogWarning($"[UIWindowPlayerInfo] UIElementStat 컴포넌트가 없습니다: {go.name}");
                return;
            }

            element.Initialize(this, _boundPlayer, idx);

            _playerInfos[idx] = element;
        }

        /// <summary>
        /// Player를 바인딩하여 스탯포인트 UI를 갱신합니다.
        /// PlayerUIController에서 호출됩니다.
        /// </summary>
        public void BindPlayerForStatPoint(Player player)
        {
            BindPlayer(player);
        }

        /// <summary>
        /// PlayerInfo 윈도우의 단일 바인딩 진입점
        /// - 라벨(Localization)은 1회만 적용
        /// - 값(스탯/포인트)은 필요 시마다 갱신
        /// </summary>
        public void BindPlayer(Player player)
        {
            _boundPlayer = player;

            _editSession = player != null ? new StatPointEditSession(player) : null;

            // UIElementStat들은 Awake에서 먼저 생성될 수 있으므로, 여기서 Player를 주입합니다.
            foreach (var kv in _playerInfos)
            {
                kv.Value?.BindPlayer(player);
            }

            ApplyLabelsOnce();
            RefreshValues();
        }

        public bool TryChangeDraft(CharacterConstants.IndexPlayerInfo statType, int delta)
        {
            if (_boundPlayer == null) return false;
            if (_editSession == null) _editSession = new StatPointEditSession(_boundPlayer);

            bool ok = _editSession.TryChange(statType, delta);
            if (ok) RefreshValues();
            return ok;
        }

        /// <summary>
        /// 라벨은 변경될 일이 거의 없으므로 1회만 적용합니다.
        /// (향후 런타임 언어 변경을 지원하면 _labelsApplied를 false로 되돌리고 재호출하면 됩니다.)
        /// </summary>
        public void ApplyLabelsOnce()
        {
            if (_labelsApplied) return;

            var loc = LocalizationManager.Instance;

            // 미사용 포인트 접두어
            // todo. localization
            _unspentPrefix = GetCommonUILabelOrFallback(loc, "PlayerInfo_Text_UnspentStatPoints", "남은포인트");

            _labelCache.Clear();

            foreach (var kv in _playerInfos)
            {
                var idx = kv.Key;
                var element = kv.Value;
                if (element == null) continue;

                string label = ResolveLabel(loc, idx);
                _labelCache[idx] = label;
                element.SetLabel(label);
            }

            _labelsApplied = true;
        }

        /// <summary>
        /// 값(스탯/포인트)만 갱신합니다.
        /// </summary>
        public void RefreshValues()
        {
            if (_boundPlayer == null) return;

            if (_editSession == null || !_editSession.IsSamePlayer(_boundPlayer))
            {
                _editSession = new StatPointEditSession(_boundPlayer);
            }

            // 드래프트가 있을 때만 미리보기 totals를 계산(부작용 없음)
            CharacterStat.CharacterTotals projectedTotals = default;
            if (_editSession.IsDirty)
            {
                projectedTotals = _boundPlayer.CalculateProjectedTotalsForStatPoints(
                    _editSession.DraftAtk,
                    _editSession.DraftDef,
                    _editSession.DraftHp,
                    _editSession.DraftMp,
                    _editSession.DraftStamina);
            }

            if (textUnspent != null)
            {
                // 라벨은 1회만 세팅되지만, 남은 포인트 숫자는 계속 변할 수 있으므로 여기서만 갱신합니다.
                // todo. localization
                var prefix = string.IsNullOrEmpty(_unspentPrefix) ? "남은포인트" : _unspentPrefix;
                textUnspent.text = _editSession.IsDirty
                    ? $"{prefix}: {_boundPlayer.UnspentStatPoints} → {_editSession.DraftUnspent}"
                    : $"{prefix}: {_boundPlayer.UnspentStatPoints}";
            }

            if (buttonApply != null) buttonApply.interactable = _editSession.IsDirty;
            if (buttonReset != null) buttonReset.interactable = _editSession.IsDirty;

            foreach (var kv in _playerInfos)
            {
                var indexPlayerInfo = kv.Key;
                var uiElementStat = kv.Value;
                if (uiElementStat == null) continue;

                var (totalValue, invested) = GetStatPointLineData(indexPlayerInfo, _boundPlayer);

                long previewValue = totalValue;
                if (_editSession.IsDirty)
                {
                    previewValue = GetTotalValueByIndex(indexPlayerInfo, projectedTotals);
                }

                if (uiElementStat.textValue != null)
                {
                    var label = GetCachedLabelOrFallback(indexPlayerInfo);
                    uiElementStat.textValue.text = _editSession.IsDirty
                        ? $"{label}: {totalValue} → {previewValue}"
                        : $"{label}: {totalValue}";
                }
                
                // 투자 대상 여부에 따라 투자 UI를 일관되게 갱신/정리합니다.
                bool isTarget = CharacterConstants.IsStatPointTarget(indexPlayerInfo);

                if (isTarget)
                {
                    int draftInvested = _editSession.GetDraftInvested(indexPlayerInfo);
                    int diff = draftInvested - invested;
                    if (uiElementStat.textInvested != null)
                    {
                        uiElementStat.textInvested.text = _editSession.IsDirty && diff != 0
                            ? $"(+{draftInvested}, Δ{diff:+#;-#;0})"
                            : $"(+{draftInvested})";
                    }

                    // 버튼 활성/비활성
                    bool canPlus = _editSession.DraftUnspent > 0;
                    if (uiElementStat.buttonPlus != null) uiElementStat.buttonPlus.interactable = canPlus;
                    if (uiElementStat.buttonMinus != null) uiElementStat.buttonMinus.interactable = draftInvested > 0;
                }
                else
                {
                    // 투자 대상이 아닌 라인은 투자 텍스트/버튼 상태가 남지 않도록 정리
                    if (uiElementStat.textInvested != null) uiElementStat.textInvested.text = string.Empty;
                    if (uiElementStat.buttonPlus != null) uiElementStat.buttonPlus.interactable = false;
                    if (uiElementStat.buttonMinus != null) uiElementStat.buttonMinus.interactable = false;
                }
            }
        }

        private void OnClickApply()
        {
            if (_boundPlayer == null) return;
            if (_editSession == null || !_editSession.IsDirty) return;

            bool ok = _boundPlayer.TryApplyStatPointAllocation(
                _editSession.DraftUnspent,
                _editSession.DraftAtk,
                _editSession.DraftDef,
                _editSession.DraftHp,
                _editSession.DraftMp,
                _editSession.DraftStamina);

            // 실패 시(총 포인트 불일치 등) 안전하게 재스냅샷
            _editSession = new StatPointEditSession(_boundPlayer);
            if (!ok)
            {
                RefreshValues();
                return;
            }

            RefreshValues();
        }

        private void OnClickReset()
        {
            if (_editSession == null) return;
            _editSession.ResetToOriginal();
            RefreshValues();
        }

        private static long GetTotalValueByIndex(CharacterConstants.IndexPlayerInfo idx, CharacterStat.CharacterTotals totals)
        {
            return idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => totals.Atk,
                CharacterConstants.IndexPlayerInfo.Def => totals.Def,
                CharacterConstants.IndexPlayerInfo.Hp => totals.Hp,
                CharacterConstants.IndexPlayerInfo.Mp => totals.Mp,
                CharacterConstants.IndexPlayerInfo.Stamina => totals.Stamina,
                CharacterConstants.IndexPlayerInfo.MoveSpeed => totals.MoveSpeed,
                CharacterConstants.IndexPlayerInfo.AttackSpeed => totals.AttackSpeed,
                CharacterConstants.IndexPlayerInfo.CriticalDamage => totals.CriticalDamage,
                CharacterConstants.IndexPlayerInfo.CriticalProbability => totals.CriticalProbability,
                CharacterConstants.IndexPlayerInfo.RegistFire => totals.RegistFire,
                CharacterConstants.IndexPlayerInfo.RegistCold => totals.RegistCold,
                CharacterConstants.IndexPlayerInfo.RegistLightning => totals.RegistLightning,
                _ => 0
            };
        }

        private sealed class StatPointEditSession
        {
            private readonly Player _player;

            private readonly int _originalUnspent;
            private readonly int _originalAtk;
            private readonly int _originalDef;
            private readonly int _originalHp;
            private readonly int _originalMp;
            private readonly int _originalStamina;

            public int DraftUnspent { get; private set; }
            public int DraftAtk { get; private set; }
            public int DraftDef { get; private set; }
            public int DraftHp { get; private set; }
            public int DraftMp { get; private set; }
            public int DraftStamina { get; private set; }

            public bool IsDirty =>
                DraftUnspent != _originalUnspent ||
                DraftAtk != _originalAtk ||
                DraftDef != _originalDef ||
                DraftHp != _originalHp ||
                DraftMp != _originalMp ||
                DraftStamina != _originalStamina;

            public StatPointEditSession(Player player)
            {
                _player = player;
                _originalUnspent = player != null ? player.UnspentStatPoints : 0;
                _originalAtk = player != null ? player.InvestedStatPointAtk : 0;
                _originalDef = player != null ? player.InvestedStatPointDef : 0;
                _originalHp = player != null ? player.InvestedStatPointHp : 0;
                _originalMp = player != null ? player.InvestedStatPointMp : 0;
                _originalStamina = player != null ? player.InvestedStatPointStamina : 0;

                ResetToOriginal();
            }

            public bool IsSamePlayer(Player player) => ReferenceEquals(_player, player);

            public void ResetToOriginal()
            {
                DraftUnspent = _originalUnspent;
                DraftAtk = _originalAtk;
                DraftDef = _originalDef;
                DraftHp = _originalHp;
                DraftMp = _originalMp;
                DraftStamina = _originalStamina;
            }

            public int GetDraftInvested(CharacterConstants.IndexPlayerInfo type)
            {
                return type switch
                {
                    CharacterConstants.IndexPlayerInfo.Atk => DraftAtk,
                    CharacterConstants.IndexPlayerInfo.Def => DraftDef,
                    CharacterConstants.IndexPlayerInfo.Hp => DraftHp,
                    CharacterConstants.IndexPlayerInfo.Mp => DraftMp,
                    CharacterConstants.IndexPlayerInfo.Stamina => DraftStamina,
                    _ => 0
                };
            }

            public bool TryChange(CharacterConstants.IndexPlayerInfo type, int delta)
            {
                if (delta == 0) return false;
                if (!CharacterConstants.IsStatPointTarget(type)) return false;

                // +
                if (delta > 0)
                {
                    if (DraftUnspent < delta) return false;

                    switch (type)
                    {
                        case CharacterConstants.IndexPlayerInfo.Atk: DraftAtk += delta; break;
                        case CharacterConstants.IndexPlayerInfo.Def: DraftDef += delta; break;
                        case CharacterConstants.IndexPlayerInfo.Hp: DraftHp += delta; break;
                        case CharacterConstants.IndexPlayerInfo.Mp: DraftMp += delta; break;
                        case CharacterConstants.IndexPlayerInfo.Stamina: DraftStamina += delta; break;
                        default: return false;
                    }

                    DraftUnspent -= delta;
                    return true;
                }

                // -
                int amount = -delta;
                switch (type)
                {
                    case CharacterConstants.IndexPlayerInfo.Atk:
                        if (DraftAtk < amount) return false;
                        DraftAtk -= amount;
                        break;
                    case CharacterConstants.IndexPlayerInfo.Def:
                        if (DraftDef < amount) return false;
                        DraftDef -= amount;
                        break;
                    case CharacterConstants.IndexPlayerInfo.Hp:
                        if (DraftHp < amount) return false;
                        DraftHp -= amount;
                        break;
                    case CharacterConstants.IndexPlayerInfo.Mp:
                        if (DraftMp < amount) return false;
                        DraftMp -= amount;
                        break;
                    case CharacterConstants.IndexPlayerInfo.Stamina:
                        if (DraftStamina < amount) return false;
                        DraftStamina -= amount;
                        break;
                    default:
                        return false;
                }

                DraftUnspent += amount;
                return true;
            }
        }

        public void RefreshStatPointUI() => RefreshValues(); // legacy 호출 호환

        private static (long totalValue, int invested) GetStatPointLineData(CharacterConstants.IndexPlayerInfo idx, Player player)
        {
            // totalValue는 PlayerInfo에 표시되는 모든 라인에서 의미가 있으므로,
            // IndexPlayerInfo 전체를 커버하도록 구성합니다.
            long totalValue = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.TotalAtk.Value,
                CharacterConstants.IndexPlayerInfo.Def => player.TotalDef.Value,
                CharacterConstants.IndexPlayerInfo.Hp => player.TotalHp.Value,
                CharacterConstants.IndexPlayerInfo.Mp => player.TotalMp.Value,
                CharacterConstants.IndexPlayerInfo.Stamina => player.TotalStamina.Value,

                CharacterConstants.IndexPlayerInfo.MoveSpeed => player.TotalMoveSpeed.Value,
                CharacterConstants.IndexPlayerInfo.AttackSpeed => player.TotalAttackSpeed.Value,
                CharacterConstants.IndexPlayerInfo.CriticalDamage => player.TotalCriticalDamage.Value,
                CharacterConstants.IndexPlayerInfo.CriticalProbability => player.TotalCriticalProbability.Value,
                CharacterConstants.IndexPlayerInfo.RegistFire => player.TotalRegistFire.Value,
                CharacterConstants.IndexPlayerInfo.RegistCold => player.TotalRegistCold.Value,
                CharacterConstants.IndexPlayerInfo.RegistLightning => player.TotalRegistLightning.Value,
                _ => 0
            };

            // invested는 스탯 포인트 투자 대상에만 적용됩니다.
            int invested = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.InvestedStatPointAtk,
                CharacterConstants.IndexPlayerInfo.Def => player.InvestedStatPointDef,
                CharacterConstants.IndexPlayerInfo.Hp => player.InvestedStatPointHp,
                CharacterConstants.IndexPlayerInfo.Mp => player.InvestedStatPointMp,
                CharacterConstants.IndexPlayerInfo.Stamina => player.InvestedStatPointStamina,
                _ => 0
            };

            return (totalValue, invested);
        }

        private string GetCachedLabelOrFallback(CharacterConstants.IndexPlayerInfo idx)
        {
            if (_labelCache.TryGetValue(idx, out var label) && !string.IsNullOrEmpty(label))
                return label;
            return idx.ToString();
        }

        private static string ResolveLabel(LocalizationManager loc, CharacterConstants.IndexPlayerInfo idx)
        {
            // StatusName 테이블 기반(이미 프로젝트에서 사용 중)
            string statusKey = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => ConfigCommon.StatusStatAtk,
                CharacterConstants.IndexPlayerInfo.Def => ConfigCommon.StatusStatDef,
                CharacterConstants.IndexPlayerInfo.Hp => ConfigCommon.StatusStatHp,
                CharacterConstants.IndexPlayerInfo.Mp => ConfigCommon.StatusStatMp,
                CharacterConstants.IndexPlayerInfo.Stamina => ConfigCommon.StatusStatStamina,
                CharacterConstants.IndexPlayerInfo.MoveSpeed => ConfigCommon.StatusStatMoveSpeed,
                CharacterConstants.IndexPlayerInfo.AttackSpeed => ConfigCommon.StatusStatAttackSpeed,
                CharacterConstants.IndexPlayerInfo.CriticalDamage => ConfigCommon.StatusStatCriticalDamage,
                CharacterConstants.IndexPlayerInfo.CriticalProbability => ConfigCommon.StatusStatCriticalProbability,
                CharacterConstants.IndexPlayerInfo.RegistFire => ConfigCommon.StatusStatResistanceFire,
                CharacterConstants.IndexPlayerInfo.RegistCold => ConfigCommon.StatusStatResistanceCold,
                CharacterConstants.IndexPlayerInfo.RegistLightning => ConfigCommon.StatusStatResistanceLightning,
                _ => string.Empty
            };

            if (loc != null && !string.IsNullOrEmpty(statusKey))
            {
                var localized = loc.GetStatusNameByKey(statusKey);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }

            // Fallback
            return idx.ToString();
        }

        private static string GetCommonUILabelOrFallback(LocalizationManager loc, string key, string fallback)
        {
            if (loc == null) return fallback;
            var localized = loc.GetCommonUIByKey(key);
            return string.IsNullOrEmpty(localized) ? fallback : localized;
        }

        /// <summary>
        /// 라벨은 윈도우가 1회만 적용하고, 이후에는 값만 갱신합니다.
        /// </summary>
        public void UpdateValue(CharacterConstants.IndexPlayerInfo index, long value)
        {
            if (index == CharacterConstants.IndexPlayerInfo.None) return;
            if (_playerInfos.TryGetValue(index, out var element) && element != null)
            {
                var label = GetCachedLabelOrFallback(index);
                element.textValue.text = $"{label}: {value}";
            }
        }
    }
}
