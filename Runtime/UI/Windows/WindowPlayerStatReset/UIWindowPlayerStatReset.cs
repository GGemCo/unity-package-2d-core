using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스탯 초기화 UIWindow가 닫힌 이유를 정의합니다.
    /// </summary>
    internal enum PlayerStatResetCloseReason
    {
        /// <summary>
        /// 사용자가 취소하기 버튼을 눌렀습니다.
        /// </summary>
        Cancelled = 0,

        /// <summary>
        /// 변경한 스탯 분배를 성공적으로 적용했습니다.
        /// </summary>
        Applied = 1,

        /// <summary>
        /// 다른 UIWindow 전환이나 외부 종료 요청으로 창이 닫혔습니다.
        /// </summary>
        Dismissed = 2,
    }

    /// <summary>
    /// 플레이어가 이미 분배한 스탯 포인트를 임시로 초기화하고, 골드 비용을 지불해 재분배를 확정하는 윈도우입니다.
    /// </summary>
    public class UIWindowPlayerStatReset : UIWindow, IStatPointDraftChangeHandler
    {
        private const string RemainPointMessageKey = "Text_Remain_Point";
        private const string RemainPointPopupRequestKey = "PlayerStatReset.RemainPoint";

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("프리팹")]
        [Tooltip("스탯 라인 프리팹(UIElementStatReset) - 비활성 템플릿으로 두고 런타임에 복제합니다.")]
        [SerializeField] private GameObject prefabElementStat;

        [Tooltip("UIElementStatReset 오브젝트를 넣을 오브젝트")] 
        [SerializeField] private GameObject containerElement;

        [Header("텍스트 오브젝트")] 
        [Tooltip("미사용 스탯 포인트")] 
        [SerializeField] private TextMeshProUGUI textUnspent;

        [Tooltip("미사용 스탯 포인트 옆에 표시할 문구 Localization Key")] 
        [SerializeField] private string localizationKeyUnspentPrefix;

        [Tooltip("현재 레벨 및 투자 시 추가 레벨 정보를 보여주는 텍스트")] 
        [SerializeField] private TextMeshProUGUI textLevel;

        [Header("레벨 스타일")] 
        [Tooltip("레벨 텍스트 표시 규칙. 비어 있으면 prefixTextLevel 기반 기본 포맷을 사용합니다.")] 
        [SerializeField] private UIElementStatFormatterAsset levelFormatterAsset;

        [Tooltip("레벨 숫자 앞에 보여줄 문구")] 
        [SerializeField] private string prefixTextLevel = "LV.";

        [Tooltip("재화가 부족하지 않을 때 적용할 스타일 키")] 
        [SerializeField] private string styleKeyPriceNormal;

        [Header("스탯 정보")] 
        [SerializeField] private List<EntityPlayerInfo> playerInfos = new();

        [Header("Apply / Reset")] 
        [Tooltip("드래프트(미적용) 스탯 포인트를 실제 데이터에 반영")] 
        [SerializeField] private Button buttonApply;

        [Tooltip("스탯 초기화 드래프트를 취소하고 창을 닫습니다.")] 
        [SerializeField] private Button buttonReset;

        [Header("보여줄 스탯")] 
        [Tooltip("보여줄 스탯 선택(멀티 선택 가능)")] 
        [SerializeField] private CharacterConstants.PlayerInfoMask useIndexPlayerInfos = CharacterConstants.PlayerInfoMask.All;

        [Header("팝업")]
        [Tooltip("남은 포인트 안내 말풍선을 Apply 버튼 기준으로 배치할 위치 보정값")]
        [SerializeField] private Vector3 remainPointPopupOffset = new Vector3(0f, 80f, 0f);

        [Tooltip("남은 포인트 안내 말풍선을 자동으로 닫을 실제 시간(초)")]
        [SerializeField] private float remainPointPopupDuration = 2f;

        private readonly Dictionary<CharacterConstants.IndexPlayerInfo, UIElementPlayerStatReset> _playerInfos = new();
        private readonly Dictionary<CharacterConstants.IndexPlayerInfo, string> _labelCache = new();

        private Player _boundPlayer;
        private StatPointResetEditSession _editSession;
        private bool _labelsApplied;
        private string _unspentPrefix;
        private Action<PlayerStatResetCloseReason> _closeCallback;
        private PlayerStatResetCloseReason _pendingCloseReason;
        private bool _hasPendingCloseReason;
        private bool _hasLoggedMissingPopupManager;

        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.PlayerStatReset;
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
            CancelRemainPointPopup();

            // 씬 종료 중에는 다른 UIWindow를 다시 열지 않도록 보류 중인 콜백을 실행하지 않고 해제합니다.
            _closeCallback = null;
            _hasPendingCloseReason = false;
        }

        /// <summary>
        /// 닫기 연출과 GameObject 비활성화가 끝난 뒤 일회성 종료 결과를 전달합니다.
        /// 대화 재개 콜백이 아직 닫히는 중인 이 Window를 다시 닫는 재진입을 방지합니다.
        /// </summary>
        private void OnDisable()
        {
            CancelRemainPointPopup();

            if (_closeCallback == null)
            {
                _hasPendingCloseReason = false;
                return;
            }

            PlayerStatResetCloseReason closeReason =
                _hasPendingCloseReason
                    ? _pendingCloseReason
                    : PlayerStatResetCloseReason.Dismissed;
            CompleteCloseRequest(closeReason);
        }

        /// <summary>
        /// 스탯 초기화 창을 열고 닫힘 사유를 한 번 전달받을 콜백을 등록합니다.
        /// 이미 열려 있는 창에는 새 요청을 덮어쓰지 않습니다.
        /// </summary>
        /// <param name="closeCallback">취소, 적용 완료, 외부 종료 사유를 받을 일회성 콜백입니다.</param>
        /// <returns>창을 새로 열고 콜백을 등록했으면 <see langword="true"/>입니다.</returns>
        internal bool ShowWithCloseCallback(
            Action<PlayerStatResetCloseReason> closeCallback)
        {
            if (closeCallback == null || IsOpen())
            {
                return false;
            }

            Show(true);
            if (!IsOpen())
            {
                return false;
            }

            _closeCallback = closeCallback;
            _hasPendingCloseReason = false;
            return true;
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

            if (GcLogger.IsNull(prefabElementStat, $"[{nameof(UIWindowPlayerStatReset)}] prefabElementStat가 없습니다."))
            {
                return;
            }

            if (GcLogger.IsNull(containerElement, $"[{nameof(UIWindowPlayerStatReset)}] containerElement가 없습니다."))
            {
                return;
            }

            foreach (CharacterConstants.IndexPlayerInfo indexPlayerInfo in Enum.GetValues(
                         typeof(CharacterConstants.IndexPlayerInfo)))
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

            var element = go.GetComponent<UIElementPlayerStatReset>();
            if (element == null)
            {
                Debug.LogWarning($"[WindowPlayerInfo] UIElementStatReset 컴포넌트가 없습니다: {go.name}");
                return;
            }

            element.Initialize(this, idx, GetEntityPlayerInfo(idx));
            _playerInfos[idx] = element;
        }

        /// <summary>
        /// PlayerStatReset 윈도우의 단일 바인딩 진입점입니다.
        /// 라벨은 1회만 적용하고, 창이 열릴 때마다 현재 플레이어 상태 기준으로 초기화 드래프트를 새로 생성합니다.
        /// </summary>
        /// <param name="player">스탯 초기화 대상으로 바인딩할 플레이어입니다.</param>
        public void BindPlayer(Player player)
        {
            _boundPlayer = player;
            _editSession = player != null ? new StatPointResetEditSession(player) : null;

            ApplyLabelsOnce();
            RefreshValues();
        }

        /// <summary>
        /// 스탯 라인의 +/- 입력을 현재 초기화 드래프트에 반영합니다.
        /// 실제 플레이어 데이터는 Apply 버튼을 누르기 전까지 변경하지 않습니다.
        /// </summary>
        /// <param name="statType">변경할 스탯 타입입니다.</param>
        /// <param name="delta">증가 또는 감소할 포인트 수입니다.</param>
        /// <returns>드래프트 변경에 성공하면 true를 반환합니다.</returns>
        public bool TryChangeDraft(CharacterConstants.IndexPlayerInfo statType, int delta)
        {
            if (_boundPlayer == null) return false;
            if (_editSession == null) _editSession = new StatPointResetEditSession(_boundPlayer);

            bool ok = _editSession.TryChange(statType, delta);
            if (ok)
                RefreshValues();

            // 포인트를 조정하면 이전 안내는 더 이상 현재 입력 상태를 나타내지 않으므로 닫습니다.
            CancelRemainPointPopup();
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
            _unspentPrefix = loc != null && !string.IsNullOrEmpty(localizationKeyUnspentPrefix)
                ? loc.GetUIWindowPlayerStatResetByKey(localizationKeyUnspentPrefix)
                : string.Empty;

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
        /// 현재 초기화 드래프트를 기준으로 값, 미리보기, 버튼 상태를 갱신합니다.
        /// </summary>
        public void RefreshValues()
        {
            if (_boundPlayer == null) return;

            // PlayerData(레벨업 등)로 인해 스탯포인트가 변경될 수 있습니다.
            // 편집 중이 아닌 경우에는 초기화 드래프트를 최신 스냅샷으로 다시 생성합니다.
            if (_editSession == null || !_editSession.IsSamePlayer(_boundPlayer) ||
                (!_editSession.IsDirty && _editSession.IsStaleSnapshot()))
            {
                _editSession = new StatPointResetEditSession(_boundPlayer);
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

            foreach (var kv in _playerInfos)
            {
                var index = kv.Key;
                var element = kv.Value;
                if (element == null) continue;

                var renderData = BuildRenderData(index, projectedTotals);
                element.Render(renderData);
            }
        }

        /// <summary>
        /// 미사용 스탯 포인트와 Apply/Reset 버튼 상태를 현재 드래프트에 맞게 갱신합니다.
        /// </summary>
        private void UpdateUnspentUi()
        {
            if (_editSession == null)
                return;

            if (textUnspent != null)
            {
                string prefix = string.IsNullOrEmpty(_unspentPrefix) ? "" : _unspentPrefix;
                // textUnspent.text = _editSession.IsDirty
                //     ? $"{prefix}{_boundPlayer.UnspentStatPoints} → <style=UI_Emphasis>{_editSession.DraftUnspent}</style>"
                //     : $"{prefix}{_boundPlayer.UnspentStatPoints}";
                textUnspent.text = $"{prefix}<style=UI_Emphasis>{_editSession.DraftUnspent}</style>";
            }

            if (buttonApply != null)
                buttonApply.interactable = _editSession.IsDirty;

            if (buttonReset != null)
                buttonReset.interactable = _editSession != null;
        }

        private UIElementStatRenderData BuildRenderData(
            CharacterConstants.IndexPlayerInfo index,
            CharacterStat.CharacterTotals projectedTotals)
        {
            string label = GetCachedLabelOrFallback(index);
            var (currentValue, invested) = GetStatPointLineData(index, _boundPlayer);

            bool hasPreview = _editSession != null && _editSession.IsDirty;
            long previewValue = hasPreview
                ? GetTotalValueByIndex(index, projectedTotals)
                : currentValue;

            bool isTarget = CharacterConstants.IsStatPointTarget(index);
            int draftInvested = isTarget && _editSession != null ? _editSession.GetDraftInvested(index) : 0;
            int investedDelta = isTarget ? draftInvested - invested : 0;
            bool canIncrease = isTarget && _editSession != null && _editSession.CanIncrease(index);
            bool canDecrease = isTarget && _editSession != null && _editSession.CanDecrease(index);

            return new UIElementStatRenderData(
                label,
                currentValue,
                hasPreview,
                previewValue,
                isTarget,
                draftInvested,
                investedDelta,
                canIncrease,
                canDecrease);
        }

        /// <summary>
        /// 스탯 초기화 드래프트를 적용합니다.
        /// 남은 미사용 포인트가 있거나 골드가 부족하면 실제 데이터와 골드를 변경하지 않습니다.
        /// </summary>
        private void OnClickApply()
        {
            if (_boundPlayer == null) return;
            if (_editSession == null || !_editSession.IsDirty) return;

            // 스탯 초기화는 모든 포인트를 다시 분배한 상태에서만 적용합니다.
            // 남은 포인트가 있으면 요구사항대로 아무 커밋도 하지 않고 종료합니다.
            if (_editSession.DraftUnspent > 0)
            {
                ShowRemainPointPopup();
                return;
            }

            if (!_boundPlayer.CanAffordStatPointResetCost())
            {
                // SceneGame.systemMessageManager?.ShowWarningCurrency(CurrencyConstants.Type.Gold);
                return;
            }

            bool applied = _boundPlayer.TryApplyStatPointResetAllocation(
                _editSession.DraftUnspent,
                _editSession.DraftAtk,
                _editSession.DraftDef,
                _editSession.DraftHp,
                _editSession.DraftMp,
                _editSession.DraftStamina);

            if (!applied)
            {
                if (!_boundPlayer.CanAffordStatPointResetCost())
                {
                    SceneGame.systemMessageManager?.ShowWarningCurrency(CurrencyConstants.Type.Gold);
                }

                _editSession = new StatPointResetEditSession(_boundPlayer);
                RefreshValues();
                return;
            }

            _editSession = new StatPointResetEditSession(_boundPlayer);
            RefreshValues();
            CloseWithReason(PlayerStatResetCloseReason.Applied);
        }

        /// <summary>
        /// 공통 팝업 매니저를 통해 남은 스탯 포인트 안내 말풍선을 표시합니다.
        /// 동일 요청 키를 사용하므로 Apply 버튼을 반복해서 눌러도 말풍선이 중복 생성되지 않습니다.
        /// </summary>
        private void ShowRemainPointPopup()
        {
            PopupManager popupManager = GetPopupManager();
            if (popupManager == null)
            {
                if (!_hasLoggedMissingPopupManager)
                {
                    GcLogger.LogWarning($"[{nameof(UIWindowPlayerStatReset)}] PopupManager를 찾을 수 없어 남은 포인트 안내를 표시하지 못했습니다.");
                    _hasLoggedMissingPopupManager = true;
                }

                return;
            }

            Vector3 anchorPosition = buttonApply != null
                ? buttonApply.transform.position
                : transform.position;

            popupManager.ShowPopup(new PopupMetadataBubble
            {
                RequestKey = RemainPointPopupRequestKey,
                PopupType = PopupManager.Type.Bubble,
                Message = RemainPointMessageKey,
                MessageColor = new Color(62f / 255f, 31f / 255f, 0f, 1f),
                ShowConfirmButton = false,
                ShowCancelButton = false,
                IsClosableByClick = true,
                ThumbnailType = PopupBubbleThumbnailType.Witch,
                Duration = Mathf.Max(0f, remainPointPopupDuration),
                Position = anchorPosition + remainPointPopupOffset,
            });
        }

        /// <summary>
        /// 이 윈도우가 요청한 남은 포인트 안내 말풍선만 취소합니다.
        /// </summary>
        private void CancelRemainPointPopup()
        {
            GetPopupManager()?.Cancel(RemainPointPopupRequestKey);
        }

        /// <summary>
        /// 현재 게임 씬에 등록된 공통 팝업 매니저를 반환합니다.
        /// </summary>
        /// <returns>사용 가능한 매니저가 없으면 <see langword="null"/>을 반환합니다.</returns>
        private static PopupManager GetPopupManager()
        {
            return SceneGame.Instance != null ? SceneGame.Instance.popupManager : null;
        }

        /// <summary>
        /// 스탯 초기화 드래프트를 취소하고 창을 닫습니다.
        /// 실제 플레이어 데이터와 골드는 변경하지 않습니다.
        /// </summary>
        private void OnClickReset()
        {
            _editSession?.ResetToOriginal();
            if (_boundPlayer != null)
            {
                RefreshValues();
            }

            CloseWithReason(PlayerStatResetCloseReason.Cancelled);
        }

        /// <summary>
        /// 명시적인 종료 사유를 보관하고 창 닫기 전환을 시작합니다.
        /// 실제 콜백은 닫기 연출이 끝나 GameObject가 비활성화되는 시점에 실행합니다.
        /// </summary>
        /// <param name="closeReason">호출자에게 전달할 종료 사유입니다.</param>
        private void CloseWithReason(
            PlayerStatResetCloseReason closeReason)
        {
            _pendingCloseReason = closeReason;
            _hasPendingCloseReason = true;
            Show(false);
        }

        /// <summary>
        /// 등록된 일회성 닫힘 콜백을 실행하고 요청 상태를 정리합니다.
        /// </summary>
        /// <param name="closeReason">창이 닫힌 최종 사유입니다.</param>
        private void CompleteCloseRequest(
            PlayerStatResetCloseReason closeReason)
        {
            Action<PlayerStatResetCloseReason> callback = _closeCallback;
            _closeCallback = null;
            _hasPendingCloseReason = false;
            callback?.Invoke(closeReason);
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

            // 초기화 했기 때문에 1로 처리
            // int currentLevel = _boundPlayer.CurrentLevel;
            int currentLevel = 1;
            int additionalLevels = 0;
            if (_editSession != null && _editSession.IsDirty && _boundPlayer.DoesStatPointInvestIncreaseLevel())
            {
                int currentInvested = _boundPlayer.InvestedStatPointAtk + _boundPlayer.InvestedStatPointDef +
                                      _boundPlayer.InvestedStatPointHp +
                                      _boundPlayer.InvestedStatPointMp + _boundPlayer.InvestedStatPointStamina;
                int draftInvested = _editSession.DraftAtk + _editSession.DraftDef + _editSession.DraftHp +
                                    _editSession.DraftMp + _editSession.DraftStamina;
                // additionalLevels = Mathf.Max(0, draftInvested - currentInvested);
                additionalLevels = draftInvested;
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

        private static long GetTotalValueByIndex(CharacterConstants.IndexPlayerInfo idx,
            CharacterStat.CharacterTotals totals)
        {
            return idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => totals.TotalStatAtk,
                CharacterConstants.IndexPlayerInfo.Def => totals.TotalStatDef,
                CharacterConstants.IndexPlayerInfo.Stamina => totals.TotalStatStamina,
                _ => 0
            };
        }

        private static (long BaseValue, int invested) GetStatPointLineData(CharacterConstants.IndexPlayerInfo idx,
            Player player)
        {
            // totalValue는 PlayerInfo에 표시되는 모든 라인에서 의미가 있으므로,
            // IndexPlayerInfo 전체를 커버하도록 구성합니다.
            long totalValue = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.TotalStatAtk.Value,
                CharacterConstants.IndexPlayerInfo.Def => player.TotalStatDef.Value,
                CharacterConstants.IndexPlayerInfo.Stamina => player.TotalStatStamina.Value,
                _ => 0
            };

            // invested는 스탯 포인트 투자 대상에만 적용됩니다.
            int invested = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.InvestedStatPointAtk,
                CharacterConstants.IndexPlayerInfo.Def => player.InvestedStatPointDef,
                CharacterConstants.IndexPlayerInfo.Stamina => player.InvestedStatPointStamina,
                _ => 0
            };

            return (totalValue, invested);
        }

        private static (long TotalValue, int invested) GetStatPointLineData_bak(CharacterConstants.IndexPlayerInfo idx,
            Player player)
        {
            // totalValue는 PlayerInfo에 표시되는 모든 라인에서 의미가 있으므로,
            // IndexPlayerInfo 전체를 커버하도록 구성합니다.
            long totalValue = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.ResolvedAtk.Value,
                CharacterConstants.IndexPlayerInfo.Def => player.ResolvedDef.Value,
                CharacterConstants.IndexPlayerInfo.Stamina => player.MaxStamina.Value,
                _ => 0
            };

            // invested는 스탯 포인트 투자 대상에만 적용됩니다.
            int invested = idx switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => player.InvestedStatPointAtk,
                CharacterConstants.IndexPlayerInfo.Def => player.InvestedStatPointDef,
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
                CharacterConstants.IndexPlayerInfo.Stamina => ConfigCommon.StatusStatStamina,
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

            if (!show)
            {
                CancelResetDraft();
                ResetStatElementViews();
                if (!_hasPendingCloseReason)
                {
                    _pendingCloseReason =
                        PlayerStatResetCloseReason.Dismissed;
                    _hasPendingCloseReason = true;
                }

                return;
            }

            _hasPendingCloseReason = false;
            if (_boundPlayer != null)
            {
                PrepareForOpen();
                return;
            }

            ResetStatElementViews();
            if (textLevel)
            {
                textLevel.text = string.Empty;
            }
        }

        /// <summary>
        /// 창을 열 때 이전 화면 상태를 제거하고 현재 플레이어 기준의 새 초기화 드래프트를 렌더링합니다.
        /// </summary>
        private void PrepareForOpen()
        {
            BeginResetDraft();
            ResetStatElementViews();
            RefreshValues();
        }

        /// <summary>
        /// 모든 스탯 라인의 임시 표시와 이 창 내부의 UI 선택 상태를 초기화합니다.
        /// 동적으로 생성한 엘리먼트와 버튼 이벤트는 재사용하므로 다시 초기화하지 않습니다.
        /// </summary>
        private void ResetStatElementViews()
        {
            foreach (KeyValuePair<CharacterConstants.IndexPlayerInfo, UIElementPlayerStatReset> pair in _playerInfos)
            {
                pair.Value?.ResetTransientView();
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject selectedObject = eventSystem != null
                ? eventSystem.currentSelectedGameObject
                : null;
            if (selectedObject != null && selectedObject.transform.IsChildOf(transform))
            {
                // 마지막으로 누른 +/- 버튼의 Selected 상태가 재오픈 후 남지 않도록 해제합니다.
                eventSystem.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// 창이 열릴 때 현재 플레이어 상태를 기준으로 스탯 초기화 드래프트를 새로 생성합니다.
        /// </summary>
        private void BeginResetDraft()
        {
            _editSession = _boundPlayer != null ? new StatPointResetEditSession(_boundPlayer) : null;
        }

        /// <summary>
        /// 창이 닫힐 때 임시 드래프트를 원본 상태로 되돌려 다음 표시 시점에 stale 상태가 남지 않도록 합니다.
        /// </summary>
        private void CancelResetDraft()
        {
            _editSession?.ResetToOriginal();
        }

        private EntityPlayerInfo GetEntityPlayerInfo(CharacterConstants.IndexPlayerInfo indexPlayerInfo)
        {
            foreach (var info in playerInfos)
            {
                if (info.index == indexPlayerInfo) return info;
            }

            return null;
        }
    }
}
