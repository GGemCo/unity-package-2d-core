using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    [Serializable]
    public class EntityPlayerInfo
    {
        public CharacterConstants.IndexPlayerInfo index;
        [Tooltip("스탯 설명 문구 Localization 키")]
        public string localizationKeyDescription;
    }
    
    /// <summary>
    /// 플레이어 stat 정보 보여주는 윈도우
    /// </summary>
    public class UIWindowPlayerInfo : UIWindow, IStatPointDraftChangeHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("프리팹")]
        [Tooltip("스탯 라인 프리팹(UIElementStat) - 비활성 템플릿으로 두고 런타임에 복제합니다.")]
        [SerializeField] private GameObject prefabElementStat;

        [Tooltip("UIElementStat 오브젝트를 넣을 오브젝트")]
        [SerializeField] private GameObject containerElement;

        [Header("텍스트 오브젝트")]
        [Tooltip("미사용 스탯 포인트")]
        [SerializeField] private TextMeshProUGUI textUnspent;
        [Tooltip("현재 레벨 및 투자 시 추가 레벨 정보를 보여주는 텍스트")]
        [SerializeField] private TextMeshProUGUI textLevel;
        [Tooltip("스탯 포인트 구매 비용과 현재 재화를 보여주는 텍스트")]
        [SerializeField] private TextMeshProUGUI textGold;
        
        [Header("스타일 키")]
        [Tooltip("재화가 부족하지 않을 때 적용할 스타일 키")]
        [SerializeField] private string styleKeyPriceNormal;
        [Tooltip("재화가 부족할 때 적용할 스타일 키")]
        [SerializeField] private string styleKeyPriceLack;

        [Header("레벨 스타일")]
        [Tooltip("레벨 텍스트 표시 규칙. 비어 있으면 prefixTextLevel 기반 기본 포맷을 사용합니다.")]
        [SerializeField] private UIElementStatFormatterAsset levelFormatterAsset;
        [Tooltip("레벨 숫자 앞에 보여줄 문구")]
        [SerializeField] private string prefixTextLevel = "LV.";
        
        [Header("스탯 정보")]
        [SerializeField] private List<EntityPlayerInfo> playerInfos = new();

        [Tooltip("HP 하트가 표시될 위치")]
        [SerializeField] private Vector3 positionHeartMp;

        // 스탯 증가, 감소 버튼 클릭당 적용할 포인트 수. 1로 고정
        private const int BuyStatPointAmountPerClick = 1;

        [Header("Apply / Reset")]
        [Tooltip("드래프트(미적용) 스탯 포인트를 실제 데이터에 반영")]
        [SerializeField] private Button buttonApply;

        [Tooltip("드래프트를 원본으로 되돌림(미적용 변경사항 폐기)")]
        [SerializeField] private Button buttonReset;

        [Header("보여줄 스탯")]
        [Tooltip("보여줄 스탯 선택(멀티 선택 가능)")]
        [SerializeField] private CharacterConstants.PlayerInfoMask useIndexPlayerInfos = CharacterConstants.PlayerInfoMask.All;
        
        private readonly Dictionary<CharacterConstants.IndexPlayerInfo, UIElementStat> _playerInfos = new();
        private readonly Dictionary<CharacterConstants.IndexPlayerInfo, string> _labelCache = new();

        private Player _boundPlayer;
        private StatPointEditSession _editSession;
        private bool _labelsApplied;
        private string _unspentPrefix;

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
            if (buttonApply != null) buttonApply.onClick.RemoveListener(OnClickApply);
            if (buttonReset != null) buttonReset.onClick.RemoveListener(OnClickReset);
        }

        /// <summary>
        /// 현재 Inspector 설정 기준으로 해당 PlayerInfo 라인을 생성할지 여부
        /// </summary>
        private bool ShouldCreateStatPointLine(CharacterConstants.IndexPlayerInfo indexPlayerInfo)
        {
            if (indexPlayerInfo == CharacterConstants.IndexPlayerInfo.None)
                return false;

            return CharacterConstants.HasPlayerInfoFlag(useIndexPlayerInfos, indexPlayerInfo);
        }

        /// <summary>
        /// PlayerInfo 창 내부에서 스탯포인트 투자 라인을 동적으로 생성합니다.
        /// prefabElementStat는 템플릿(비활성)로 두고, 런타임에 복제하여 사용합니다.
        /// </summary>
        private void InitializeStatPointLines()
        {
            _playerInfos.Clear();

            if (prefabElementStat == null)
            {
                Debug.LogWarning("[UIWindowPlayerInfo] prefabElementStat가 없습니다.");
                return;
            }

            if (containerElement == null)
            {
                Debug.LogWarning("[UIWindowPlayerInfo] containerElement가 없습니다.");
                return;
            }

            foreach (CharacterConstants.IndexPlayerInfo indexPlayerInfo in Enum.GetValues(typeof(CharacterConstants.IndexPlayerInfo)))
            {
                if (!ShouldCreateStatPointLine(indexPlayerInfo))
                    continue;

                CreateStatPointLine(indexPlayerInfo, containerElement.transform);
            }
        }

        private void CreateStatPointLine(CharacterConstants.IndexPlayerInfo idx, Transform parent)
        {
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

            element.Initialize(this, idx, GetEntityPlayerInfo(idx));
            _playerInfos[idx] = element;
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

            ApplyLabelsOnce();
            RefreshValues();
        }

        public bool TryChangeDraft(CharacterConstants.IndexPlayerInfo statType, int delta)
        {
            if (_boundPlayer == null) return false;
            if (_editSession == null) _editSession = new StatPointEditSession(_boundPlayer);

            bool ok = _editSession.TryChange(statType, delta);
            if (ok)
                RefreshValues();

            return ok;
        }

        /// <summary>
        /// 라벨은 변경될 일이 거의 없으므로 1회만 적용합니다.
        /// (향후 런타임 언어 변경을 지원하면 _labelsApplied를 false로 되돌리고 재호출하면 됩니다.)
        /// </summary>
        private void ApplyLabelsOnce()
        {
            if (_labelsApplied) return;

            var loc = LocalizationManager.Instance;
            _unspentPrefix = loc != null ? loc.GetUIWindowPlayerInfoByKey("Text_UnspentStatPoints") : string.Empty;

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

            // PlayerData(레벨업 등)로 인해 스탯포인트가 변경될 수 있습니다.
            // 드래프트가 없는 상태(IsDirty == false)에서는 최신 값으로 스냅샷을 재구성하여
            // + / - 버튼 활성, 남은 포인트 표시가 즉시 반영되도록 합니다.
            if (_editSession == null || !_editSession.IsSamePlayer(_boundPlayer) || (!_editSession.IsDirty && _editSession.IsStaleSnapshot()))
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

            UpdateUnspentUi();
            UpdateLevelText();
            UpdatePurchaseUi();

            foreach (var kv in _playerInfos)
            {
                var index = kv.Key;
                var element = kv.Value;
                if (element == null) continue;

                var renderData = BuildRenderData(index, projectedTotals);
                element.Render(renderData);
            }
        }

        private void UpdateUnspentUi()
        {
            if (_editSession == null)
                return;

            if (textUnspent != null)
            {
                string prefix = string.IsNullOrEmpty(_unspentPrefix) ? "남은포인트" : _unspentPrefix;
                if (_editSession.UsesReservedGoldBudget())
                {
                    textUnspent.text = _editSession.IsDirty
                        ? $"{prefix}: {_boundPlayer.UnspentStatPoints} → {_editSession.DraftUnspent}"
                        : $"{prefix}: {_boundPlayer.UnspentStatPoints}";
                }
                else
                {
                    textUnspent.text = _editSession.IsDirty
                        ? $"{prefix}: {_boundPlayer.UnspentStatPoints} → {_editSession.DraftUnspent}"
                        : $"{prefix}: {_boundPlayer.UnspentStatPoints}";
                }
            }

            if (buttonApply != null)
                buttonApply.interactable = _editSession.IsDirty;

            if (buttonReset != null)
                buttonReset.interactable = _editSession.IsDirty;
        }

        /// <summary>
        /// PlayerInfo의 단일 스탯 라인을 그리기 위한 표시 전용 데이터를 생성합니다.
        /// </summary>
        /// <param name="index">표시할 PlayerInfo 스탯 인덱스입니다.</param>
        /// <param name="projectedTotals">드래프트 투자 포인트를 반영한 미리보기 총합입니다.</param>
        /// <returns>UIElementStat가 소비할 렌더 데이터입니다.</returns>
        private UIElementStatRenderData BuildRenderData(
            CharacterConstants.IndexPlayerInfo index,
            CharacterStat.CharacterTotals projectedTotals)
        {
            string label = GetCachedLabelOrFallback(index);
            var lineData = GetStatPointLineData(index, _boundPlayer);

            bool hasPreview = _editSession != null && _editSession.IsDirty;
            bool isTarget = CharacterConstants.IsStatPointTarget(index);
            int draftInvested = isTarget && _editSession != null ? _editSession.GetDraftInvested(index) : 0;
            int investedDelta = isTarget ? draftInvested - lineData.invested : 0;

            // 스탯 포인트 투자 대상 라인은 메인 값(textValue)에 Stat* 총합이 아니라
            // 현재 투자 포인트와 드래프트 투자 포인트를 표시합니다.
            // 실제 스탯 수치 변화는 보조 값 영역에서 TotalStat* 현재/미리보기 값으로 분리해 보여줍니다.
            long currentValue = isTarget ? lineData.invested : lineData.displayValue;
            long previewValue = GetPreviewDisplayValue(index, projectedTotals, hasPreview, isTarget, draftInvested, lineData.displayValue);

            bool canIncrease = isTarget && _editSession != null && _editSession.CanIncrease(index);
            bool canDecrease = isTarget && _editSession != null && _editSession.CanDecrease(index);
            bool hasBaseValue = isTarget;
            long previewBaseValue = hasPreview
                ? GetBaseValueByIndex(index, projectedTotals)
                : lineData.baseValue;

            return new UIElementStatRenderData(
                label,
                currentValue,
                hasPreview,
                previewValue,
                isTarget,
                draftInvested,
                investedDelta,
                canIncrease,
                canDecrease,
                hasBaseValue,
                lineData.baseValue,
                hasPreview,
                previewBaseValue);
        }

        private void OnClickApply()
        {
            if (_boundPlayer == null) return;
            if (_editSession == null || !_editSession.IsDirty) return;

            long reservedDraftGoldCost = _editSession.UsesReservedGoldBudget()
                ? _editSession.DraftReservedGoldCost
                : 0;

            bool applied = _boundPlayer.TryApplyStatPointAllocation(
                _editSession.DraftUnspent,
                _editSession.DraftAtk,
                _editSession.DraftDef,
                _editSession.DraftHp,
                _editSession.DraftMp,
                _editSession.DraftStamina,
                reservedDraftGoldCost);

            if (!applied && _editSession.UsesReservedGoldBudget())
            {
                SceneGame.systemMessageManager?.ShowWarningCurrency(CurrencyConstants.Type.Gold);
            }

            // 실패 시(총 포인트 불일치 등) 안전하게 재스냅샷
            _editSession = new StatPointEditSession(_boundPlayer);
            RefreshValues();
        }

        private void OnClickReset()
        {
            if (_editSession == null) return;
            _editSession.ResetToOriginal();
            RefreshValues();
        }

        private void UpdateLevelText()
        {
            if (textLevel == null)
                return;

            if (_boundPlayer == null)
            {
                textLevel.text = string.Empty;
                return;
            }

            int currentLevel = _boundPlayer.CurrentLevel;
            int additionalLevels = 0;
            if (_editSession != null && _editSession.IsDirty && _boundPlayer.DoesStatPointInvestIncreaseLevel())
            {
                int currentInvested = _boundPlayer.InvestedStatPointAtk + _boundPlayer.InvestedStatPointDef + _boundPlayer.InvestedStatPointHp +
                                     _boundPlayer.InvestedStatPointMp + _boundPlayer.InvestedStatPointStamina;
                int draftInvested = _editSession.DraftAtk + _editSession.DraftDef + _editSession.DraftHp + _editSession.DraftMp + _editSession.DraftStamina;
                additionalLevels = Mathf.Max(0, draftInvested - currentInvested);
            }

            int previewLevel = currentLevel + additionalLevels;
            if (levelFormatterAsset != null)
            {
                var renderData = new UIElementStatRenderData(
                    prefixTextLevel,
                    currentLevel,
                    additionalLevels > 0,
                    previewLevel,
                    false,
                    0,
                    0,
                    false,
                    false);
                textLevel.text = levelFormatterAsset.FormatValue(renderData);
                return;
            }

            textLevel.text = additionalLevels > 0
                ? $"{prefixTextLevel} {currentLevel} ▶ <style={styleKeyPriceNormal}>{previewLevel}</style>"
                : $"{prefixTextLevel} {currentLevel}";
        }

        private void UpdatePurchaseUi()
        {
            if (textGold == null)
                return;

            if (_boundPlayer == null)
            {
                if (textGold != null)
                {
                    textGold.text = string.Empty;
                }

                return;
            }

            if (_editSession != null && _editSession.UsesReservedGoldBudget())
            {
                if (textGold == null)
                    return;

                long currentGold = _boundPlayer.CurrentGold;
                long reservedGold = _editSession.DraftReservedGoldCost;
                long previewGold = _editSession.GetPreviewGoldAfterReservation();
                long nextNeedGold = _editSession.GetNextRequiredGoldForIncrease();
                bool canAffordNext = nextNeedGold <= 0 || previewGold >= nextNeedGold;
                string previewStyleKey = reservedGold > 0 ? styleKeyPriceNormal : string.Empty;
                string nextStyleKey = canAffordNext ? styleKeyPriceNormal : styleKeyPriceLack;

                string previewGoldText = $"<style={nextStyleKey}>{previewGold}</style>";
                string nextNeedText = $"{nextNeedGold}";

                // textGold.text = $"Gold: {previewGoldText} / 예약 {reservedGold} / 다음 필요 {nextNeedText}";
                textGold.text = $"( {previewGoldText} / {nextNeedText} )";
                return;
            }

            bool canPurchase = _boundPlayer.CanPurchaseStatPoints();
            int amount = Mathf.Max(1, BuyStatPointAmountPerClick);
            bool canAfford = canPurchase && _boundPlayer.CanAffordStatPointPurchase(amount);

            if (textGold == null)
                return;

            if (!canPurchase)
            {
                textGold.text = string.Empty;
                return;
            }

            var currencyType = _boundPlayer.GetStatPointPurchaseCurrencyType();
            long currentCurrency = currencyType switch
            {
                CurrencyConstants.Type.Gold => _boundPlayer.CurrentGold,
                CurrencyConstants.Type.Silver => _boundPlayer.CurrentSilver,
                _ => 0
            };
            long needCurrency = _boundPlayer.GetStatPointPurchasePrice(amount);
            string currencyName = CurrencyConstants.GetNameByCurrencyType(currencyType);
            string styleKey = canAfford ? styleKeyPriceNormal : styleKeyPriceLack;

            textGold.text = string.Format("{0}: <style={1}>{2}</style> / {3} (+{4}pt)", currencyName, styleKey, currentCurrency, needCurrency, amount);
        }

        /// <summary>
        /// PlayerInfo 메인 값 영역의 미리보기 값을 조회합니다.
        /// </summary>
        /// <param name="idx">표시할 PlayerInfo 스탯 인덱스입니다.</param>
        /// <param name="projectedTotals">드래프트 투자 포인트를 반영한 미리보기 총합입니다.</param>
        /// <param name="hasPreview">드래프트 미리보기 사용 여부입니다.</param>
        /// <param name="isTarget">스탯 포인트 투자 대상 여부입니다.</param>
        /// <param name="draftInvested">드래프트 기준 투자 포인트입니다.</param>
        /// <param name="currentDisplayValue">미리보기가 없을 때 사용할 현재 표시값입니다.</param>
        /// <returns>메인 값 영역에 표시할 미리보기 값입니다.</returns>
        private static long GetPreviewDisplayValue(
            CharacterConstants.IndexPlayerInfo idx,
            CharacterStat.CharacterTotals projectedTotals,
            bool hasPreview,
            bool isTarget,
            int draftInvested,
            long currentDisplayValue)
        {
            if (!hasPreview)
                return currentDisplayValue;

            // 스탯 포인트 투자 대상은 메인 값에 투자 포인트 자체를 표시합니다.
            // TotalStat* 보정 결과는 보조 값 영역이 담당하므로 여기서는 드래프트 투자량만 반환합니다.
            if (isTarget)
                return draftInvested;

            return GetDisplayValueByIndex(idx, projectedTotals);
        }

        /// <summary>
        /// PlayerInfo 메인 값 영역에 표시할 값을 조회합니다.
        /// </summary>
        /// <param name="idx">표시할 PlayerInfo 스탯 인덱스입니다.</param>
        /// <param name="totals">조회할 총합 스냅샷입니다.</param>
        /// <returns>비투자 스탯 미리보기에 사용할 일반 총합 값입니다.</returns>
        private static long GetDisplayValueByIndex(CharacterConstants.IndexPlayerInfo idx, CharacterStat.CharacterTotals totals)
        {
            return idx switch
            {
                // 투자 대상 라인은 BuildRenderData에서 투자 포인트로 대체되지만,
                // 직접 조회되는 상황을 대비해 Stat* 값을 유지합니다.
                CharacterConstants.IndexPlayerInfo.Atk => totals.StatAtk,
                CharacterConstants.IndexPlayerInfo.Def => totals.StatDef,
                CharacterConstants.IndexPlayerInfo.Hp => totals.StatHp,
                CharacterConstants.IndexPlayerInfo.Mp => totals.StatMp,
                CharacterConstants.IndexPlayerInfo.Stamina => totals.StatStamina,
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

        /// <summary>
        /// 스탯 포인트 투자 대상 라인에서 보조 값 영역에 표시할 TotalStat* 값을 조회합니다.
        /// </summary>
        /// <param name="idx">표시할 PlayerInfo 스탯 인덱스입니다.</param>
        /// <param name="totals">조회할 총합 스냅샷입니다.</param>
        /// <returns>보조 값 영역에 표시할 TotalStat* 값입니다.</returns>
        private static long GetBaseValueByIndex(CharacterConstants.IndexPlayerInfo idx, CharacterStat.CharacterTotals totals)
        {
            return idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => totals.StatAtk,
                CharacterConstants.IndexPlayerInfo.Def => totals.StatDef,
                CharacterConstants.IndexPlayerInfo.Hp => totals.StatHp,
                CharacterConstants.IndexPlayerInfo.Mp => totals.StatMp,
                CharacterConstants.IndexPlayerInfo.Stamina => totals.StatStamina,
                _ => 0
            };
        }

        /// <summary>
        /// 현재 플레이어 기준으로 PlayerInfo 라인에 필요한 현재값과 투자 포인트를 조회합니다.
        /// </summary>
        /// <param name="idx">표시할 PlayerInfo 스탯 인덱스입니다.</param>
        /// <param name="player">값을 조회할 플레이어입니다.</param>
        /// <returns>비투자 스탯 표시값, 보조 값 표시값, 현재 투자 포인트입니다.</returns>
        private static (long displayValue, long baseValue, int invested) GetStatPointLineData(CharacterConstants.IndexPlayerInfo idx, Player player)
        {
            // displayValue는 비투자 스탯의 메인 표시값으로 사용합니다.
            // 투자 대상 라인은 BuildRenderData에서 현재 투자 포인트 값으로 대체합니다.
            long displayValue = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.TotalStatAtk.Value,
                CharacterConstants.IndexPlayerInfo.Def => player.TotalStatDef.Value,
                CharacterConstants.IndexPlayerInfo.Hp => player.TotalStatHp.Value,
                CharacterConstants.IndexPlayerInfo.Mp => player.TotalStatMp.Value,
                CharacterConstants.IndexPlayerInfo.Stamina => player.TotalStatStamina.Value,
                CharacterConstants.IndexPlayerInfo.MoveSpeed => player.TotalMoveSpeed.Value,
                CharacterConstants.IndexPlayerInfo.AttackSpeed => player.TotalAttackSpeed.Value,
                CharacterConstants.IndexPlayerInfo.CriticalDamage => player.TotalCriticalDamage.Value,
                CharacterConstants.IndexPlayerInfo.CriticalProbability => player.TotalCriticalProbability.Value,
                CharacterConstants.IndexPlayerInfo.RegistFire => player.TotalRegistFire.Value,
                CharacterConstants.IndexPlayerInfo.RegistCold => player.TotalRegistCold.Value,
                CharacterConstants.IndexPlayerInfo.RegistLightning => player.TotalRegistLightning.Value,
                _ => 0
            };

            // baseValue는 스탯 포인트가 실제로 보정하는 TotalStat* 표시용 값입니다.
            long baseValue = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.TotalStatAtk.Value,
                CharacterConstants.IndexPlayerInfo.Def => player.TotalStatDef.Value,
                CharacterConstants.IndexPlayerInfo.Hp => player.TotalStatHp.Value,
                CharacterConstants.IndexPlayerInfo.Mp => player.TotalStatMp.Value,
                CharacterConstants.IndexPlayerInfo.Stamina => player.TotalStatStamina.Value,
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

            return (displayValue, baseValue, invested);
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
                CharacterConstants.IndexPlayerInfo.MoveSpeed => ConfigCommon.BaseStatMoveSpeed,
                CharacterConstants.IndexPlayerInfo.AttackSpeed => ConfigCommon.BaseStatAttackSpeed,
                CharacterConstants.IndexPlayerInfo.CriticalDamage => ConfigCommon.BaseStatCriticalDamage,
                CharacterConstants.IndexPlayerInfo.CriticalProbability => ConfigCommon.BaseStatCriticalProbability,
                CharacterConstants.IndexPlayerInfo.RegistFire => ConfigCommon.BaseStatRegistFire,
                CharacterConstants.IndexPlayerInfo.RegistCold => ConfigCommon.BaseStatRegistCold,
                CharacterConstants.IndexPlayerInfo.RegistLightning => ConfigCommon.BaseStatRegistLightning,
                CharacterConstants.IndexPlayerInfo.RegistPoison => ConfigCommon.BaseStatRegistPoison,
                _ => string.Empty
            };

            if (loc != null && !string.IsNullOrEmpty(statusKey))
            {
                string localized = loc.GetStatusNameByKey(statusKey);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }

            return idx.ToString();
        }

        /// <summary>
        /// 외부에서 특정 스탯의 값 변경 알림을 주더라도, 실제 표시에는 preview/투자 상태가 함께 반영되어야 하므로 전체 라인을 다시 렌더링합니다.
        /// </summary>
        public void UpdateValue(CharacterConstants.IndexPlayerInfo index, long value)
        {
            if (index == CharacterConstants.IndexPlayerInfo.None) return;
            RefreshValues();
        }

        public override void OnShow(bool show)
        {
            base.OnShow(show);
            
            // 예외 처리
            UpdateUIWindowHudGameObjectHp(show);

            if (_boundPlayer != null)
            {
                RefreshValues();
                return;
            }

            if (textLevel)
            {
                textLevel.text = string.Empty;
            }

            if (textGold)
            {
                textGold.text = string.Empty;
            }
        }

        private EntityPlayerInfo GetEntityPlayerInfo(CharacterConstants.IndexPlayerInfo indexPlayerInfo)
        {
            foreach (var info in playerInfos)
            {
                if (info.index == indexPlayerInfo) return info;
            }

            return null;
        }
        
        /// <summary>
        /// 플레이어 HP 정보를 보여주기위해 HUD에 있는 UIElementHeart를 여기로 가져온다.
        /// </summary>
        /// <param name="show"></param>
        private void UpdateUIWindowHudGameObjectHp(bool show)
        {
            var windowHud = SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowHud>(UIWindowConstants.WindowUid.Hud);
            if (windowHud != null)
            {
                if (windowHud.gameObjectHp != null)
                {
                    if (show)
                    {
                        windowHud.gameObjectHp.gameObject.transform.SetParent(transform, false);
                        windowHud.gameObjectHp.gameObject.transform.localPosition = positionHeartMp;
                        var horizontalLayoutGroup = windowHud.gameObjectHp.GetComponent<HorizontalLayoutGroup>();
                        if (horizontalLayoutGroup != null)
                        {
                            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                        }
                    }
                    else
                    {
                        windowHud.gameObjectHp.gameObject.transform.SetParent(windowHud.transform, false);
                        windowHud.ResetPositionHeartObject();
                    }
                }
            }
        }
    }
}
