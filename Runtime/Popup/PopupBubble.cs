using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 말풍선 팝업에서 사용할 썸네일 타입입니다.
    /// </summary>
    public enum PopupBubbleThumbnailType
    {
        Player,
        Merchant,
        Witch,
        RogGoblin,
        FighterGoblin,
        MageGoblin,
        Guardian,
        BossGoblin,
    }

    /// <summary>
    /// 말풍선 팝업 초기화 메타데이터입니다.
    /// </summary>
    public class PopupMetadataBubble : PopupMetadata
    {
        /// <summary>
        /// 표시할 썸네일 타입입니다.
        /// </summary>
        public PopupBubbleThumbnailType ThumbnailType;

        /// <summary>
        /// 팝업 유지 시간(초)입니다.
        /// 0 이하면 자동 닫힘을 사용하지 않습니다.
        /// </summary>
        public float Duration;

        /// <summary>
        /// 팝업 월드 좌표입니다.
        /// </summary>
        public Vector3 Position;
    }

    /// <summary>
    /// 대화형 말풍선 팝업입니다.
    /// UIDialogueBalloon의 썸네일 정렬/플립 정책을 간소화해 적용합니다.
    /// </summary>
    public class PopupBubble : DefaultPopup
    {
        /// <summary>
        /// 썸네일 타입별 표시 옵션입니다.
        /// </summary>
        [Serializable]
        private class EntityThumbnailInfo
        {
            [Tooltip("엔티티 썸네일 타입")]
            public PopupBubbleThumbnailType thumbnailType;

            [Tooltip("썸네일 스프라이트")]
            public Sprite thumbnailSprite;

            [Tooltip("썸네일 배치 기준 위치")]
            public ConfigCommon.ThumbnailPositionType thumbnailPositionType;

            [Tooltip("썸네일 좌우 반전 정책")]
            public DialogueBalloonThumbnailFlipPolicy thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition;

            [Tooltip("원본 썸네일 기본 바라보기 방향")]
            public DialogueBalloonThumbnailSourceFacing thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("캐릭터 썸네일 이미지")]
        [SerializeField] private Image imageThumbnailCharacter;

        [Tooltip("오른쪽 배치 기준 썸네일 위치 보정값")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacter;

        [Tooltip("왼쪽 배치 기준 썸네일 위치 보정값")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacterLeft;

        [Tooltip("패널과 썸네일 사이 간격(px)")]
        [SerializeField] private float thumbnailGapPx = 0f;

        [Tooltip("썸네일 반대편 텍스트 패딩(px)")]
        [SerializeField] private int textPaddingOnNonThumbnailSidePx = 6;

        [Tooltip("썸네일 쪽 텍스트 패딩(px)")]
        [SerializeField] private int textPaddingOnThumbnailSidePx = 3;

        [SerializeField] private List<EntityThumbnailInfo> entityThumbnailInfos;

        private RectTransform _bubbleRectTransform;
        private RectTransform _panelRectTransform;
        private RectTransform _thumbnailRectTransform;
        private VerticalLayoutGroup _panelLayoutGroup;
        private EntityThumbnailInfo _currentEntityPlayerInfo;
        private float _duration;
        private Vector3 _position;
        private Coroutine _coroutineFadeOut;
        private Vector3 _thumbnailBaseScale = Vector3.one;
        private bool _hasThumbnailBaseScale;
        private bool _hasDefaultPanelPadding;
        private int _defaultPanelPaddingLeft;
        private int _defaultPanelPaddingRight;

        /// <summary>
        /// 컴포넌트 초기화 시 레이아웃 참조를 캐시합니다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _bubbleRectTransform = GetComponent<RectTransform>();
            CacheLayoutReferences();
        }

        /// <summary>
        /// 오브젝트 파괴 시 진행 중인 자동 닫힘 코루틴을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_coroutineFadeOut != null)
            {
                StopCoroutine(_coroutineFadeOut);
            }
        }

        /// <summary>
        /// 메타데이터를 해석해 말풍선 상태를 초기화합니다.
        /// </summary>
        /// <param name="popupMetadata">팝업 공통 메타데이터입니다.</param>
        protected override void OnInitialize(PopupMetadata popupMetadata)
        {
            PopupMetadataBubble popupMetadataBubble = popupMetadata as PopupMetadataBubble;
            if (popupMetadataBubble == null)
            {
                return;
            }

            _duration = popupMetadataBubble.Duration;
            _position = popupMetadataBubble.Position;
            _currentEntityPlayerInfo = GetEntityInfo(popupMetadataBubble.ThumbnailType);

            SetThumbnail();
            SetPosition();
            SetDuration();
        }

        /// <summary>
        /// 팝업 위치를 메타데이터 좌표로 설정합니다.
        /// </summary>
        private void SetPosition()
        {
            transform.position = _position;
        }

        /// <summary>
        /// 유지 시간이 유효하면 자동 닫힘 코루틴을 시작합니다.
        /// </summary>
        private void SetDuration()
        {
            if (_duration <= 0)
            {
                return;
            }

            if (_coroutineFadeOut != null)
            {
                StopCoroutine(_coroutineFadeOut);
            }

            _coroutineFadeOut = StartCoroutine(CoroutineFadeOut());
        }

        /// <summary>
        /// 지정된 시간 이후 팝업을 닫습니다.
        /// </summary>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator CoroutineFadeOut()
        {
            yield return new WaitForSeconds(_duration);
            ClosePopup();
        }

        /// <summary>
        /// 현재 썸네일 설정을 적용하고 좌표/플립을 계산합니다.
        /// </summary>
        private void SetThumbnail()
        {
            if (!TryEnsureLayoutReferences())
            {
                return;
            }

            if (_currentEntityPlayerInfo == null ||
                _currentEntityPlayerInfo.thumbnailSprite == null ||
                _currentEntityPlayerInfo.thumbnailPositionType == ConfigCommon.ThumbnailPositionType.None)
            {
                ClearThumbnail();
                return;
            }

            imageThumbnailCharacter.sprite = _currentEntityPlayerInfo.thumbnailSprite;
            imageThumbnailCharacter.SetNativeSize();
            imageThumbnailCharacter.gameObject.SetActive(true);

            ApplyPanelPaddingByThumbnailSide(_currentEntityPlayerInfo.thumbnailPositionType);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);

            float panelHalfWidth = _panelRectTransform.rect.width * 0.5f;
            float thumbnailHalfWidth = _thumbnailRectTransform.rect.width * 0.5f;
            float side = ResolveThumbnailSideSign(_currentEntityPlayerInfo.thumbnailPositionType);
            Vector3 thumbnailOffset = ResolveThumbnailOffset(_currentEntityPlayerInfo.thumbnailPositionType);
            float thumbnailGap = Mathf.Max(0f, thumbnailGapPx);

            // 패널 반너비 + 간격 + 썸네일 반너비를 기준으로 좌/우 배치합니다.
            Vector2 thumbnailAnchoredPosition = _thumbnailRectTransform.anchoredPosition;
            thumbnailAnchoredPosition.x = side * (panelHalfWidth + thumbnailGap + thumbnailHalfWidth) + thumbnailOffset.x;
            thumbnailAnchoredPosition.y = thumbnailOffset.y;
            _thumbnailRectTransform.anchoredPosition = thumbnailAnchoredPosition;

            ApplyThumbnailFlip();
        }

        /// <summary>
        /// 패널/썸네일 관련 레이아웃 참조를 캐시합니다.
        /// 프리팹 직렬화 참조가 비어 있어도 이름 기반 탐색으로 보정합니다.
        /// </summary>
        private void CacheLayoutReferences()
        {
            if (_bubbleRectTransform == null)
            {
                _bubbleRectTransform = transform as RectTransform;
            }

            if (panelContent == null)
            {
                panelContent = transform.Find("Panel") as RectTransform;
            }

            _panelRectTransform = panelContent != null
                ? panelContent
                : transform.Find("Panel") as RectTransform;

            if (panelContent == null && _panelRectTransform != null)
            {
                panelContent = _panelRectTransform;
            }

            if (imageThumbnailCharacter == null)
            {
                Transform thumbnailTransform = transform.Find("ImageThumbnail");
                if (thumbnailTransform != null)
                {
                    imageThumbnailCharacter = thumbnailTransform.GetComponent<Image>();
                }
            }

            _thumbnailRectTransform = imageThumbnailCharacter != null
                ? imageThumbnailCharacter.GetComponent<RectTransform>()
                : null;

            if (_thumbnailRectTransform != null && !_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            if (_panelRectTransform != null)
            {
                _panelLayoutGroup = _panelRectTransform.GetComponent<VerticalLayoutGroup>();
                if (_panelLayoutGroup != null && !_hasDefaultPanelPadding)
                {
                    _defaultPanelPaddingLeft = _panelLayoutGroup.padding.left;
                    _defaultPanelPaddingRight = _panelLayoutGroup.padding.right;
                    _hasDefaultPanelPadding = true;
                }
            }
        }

        /// <summary>
        /// 썸네일 배치 계산에 필요한 참조가 준비되었는지 확인합니다.
        /// </summary>
        /// <returns>참조가 유효하면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureLayoutReferences()
        {
            if (_panelRectTransform == null || _thumbnailRectTransform == null || imageThumbnailCharacter == null)
            {
                CacheLayoutReferences();
            }

            return _panelRectTransform != null &&
                   _thumbnailRectTransform != null &&
                   imageThumbnailCharacter != null;
        }

        /// <summary>
        /// 썸네일을 비활성화하고 레이아웃/스케일을 기본값으로 복원합니다.
        /// </summary>
        private void ClearThumbnail()
        {
            if (!TryEnsureLayoutReferences())
            {
                return;
            }

            imageThumbnailCharacter.sprite = null;
            imageThumbnailCharacter.gameObject.SetActive(false);
            RestoreThumbnailScaleToBase();
            RestorePanelPadding();
        }

        /// <summary>
        /// 패널 좌우 패딩을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestorePanelPadding()
        {
            if (_panelLayoutGroup == null || !_hasDefaultPanelPadding)
            {
                return;
            }

            _panelLayoutGroup.padding.left = _defaultPanelPaddingLeft;
            _panelLayoutGroup.padding.right = _defaultPanelPaddingRight;
        }

        /// <summary>
        /// 썸네일 위치(좌/우)에 맞춰 텍스트 패딩을 적용합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 기준 위치입니다.</param>
        private void ApplyPanelPaddingByThumbnailSide(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            if (_panelLayoutGroup == null)
            {
                return;
            }

            bool isThumbnailLeft = thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
            _panelLayoutGroup.padding.left = isThumbnailLeft
                ? Mathf.Max(0, textPaddingOnThumbnailSidePx)
                : Mathf.Max(0, textPaddingOnNonThumbnailSidePx);
            _panelLayoutGroup.padding.right = isThumbnailLeft
                ? Mathf.Max(0, textPaddingOnNonThumbnailSidePx)
                : Mathf.Max(0, textPaddingOnThumbnailSidePx);
        }

        /// <summary>
        /// 썸네일 배치 방향을 부호(-1, +1)로 변환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 기준 위치입니다.</param>
        /// <returns>왼쪽이면 -1, 오른쪽이면 +1입니다.</returns>
        private static float ResolveThumbnailSideSign(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left ? -1f : 1f;
        }

        /// <summary>
        /// 썸네일 배치 방향에 맞는 오프셋 값을 반환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 기준 위치입니다.</param>
        /// <returns>좌/우 기준 오프셋입니다.</returns>
        private Vector3 ResolveThumbnailOffset(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left
                ? offsetImageThumbnailCharacterLeft
                : offsetImageThumbnailCharacter;
        }

        /// <summary>
        /// 현재 플립 정책과 원본 방향을 기준으로 썸네일 좌우 반전을 적용합니다.
        /// </summary>
        private void ApplyThumbnailFlip()
        {
            if (_thumbnailRectTransform == null || _currentEntityPlayerInfo == null)
            {
                return;
            }

            if (!_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            bool shouldFlip = ResolveShouldFlipThumbnail();
            float baseAbsX = Mathf.Abs(_thumbnailBaseScale.x);
            if (baseAbsX <= Mathf.Epsilon)
            {
                baseAbsX = 1f;
            }

            float x = shouldFlip ? -baseAbsX : baseAbsX;
            _thumbnailRectTransform.localScale = new Vector3(
                x,
                _thumbnailBaseScale.y,
                _thumbnailBaseScale.z);
        }

        /// <summary>
        /// 썸네일 반전 여부를 정책 기준으로 계산합니다.
        /// </summary>
        /// <returns>반전이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ResolveShouldFlipThumbnail()
        {
            switch (_currentEntityPlayerInfo.thumbnailFlipPolicy)
            {
                case DialogueBalloonThumbnailFlipPolicy.KeepOriginal:
                    return false;

                case DialogueBalloonThumbnailFlipPolicy.ForceFlip:
                    return true;

                case DialogueBalloonThumbnailFlipPolicy.AutoBySpeakerFacing:
                case DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition:
                default:
                    // PopupBubble은 화자 방향 데이터가 없으므로 위치 기반 판단으로 안전하게 폴백합니다.
                    bool desiredFacingRight = _currentEntityPlayerInfo.thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
                    return ShouldFlipToDesiredFacing(desiredFacingRight);
            }
        }

        /// <summary>
        /// 목표 바라보기 방향과 원본 방향을 비교해 반전 필요 여부를 계산합니다.
        /// </summary>
        /// <param name="desiredFacingRight">목표 방향이 오른쪽이면 <see langword="true"/>입니다.</param>
        /// <returns>반전이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldFlipToDesiredFacing(bool desiredFacingRight)
        {
            bool sourceFacingRight = _currentEntityPlayerInfo.thumbnailSourceFacing == DialogueBalloonThumbnailSourceFacing.Right;
            return sourceFacingRight != desiredFacingRight;
        }

        /// <summary>
        /// 썸네일 스케일을 초기 기준값으로 복원합니다.
        /// </summary>
        private void RestoreThumbnailScaleToBase()
        {
            if (_thumbnailRectTransform == null || !_hasThumbnailBaseScale)
            {
                return;
            }

            _thumbnailRectTransform.localScale = _thumbnailBaseScale;
        }

        /// <summary>
        /// 요청한 타입과 일치하는 썸네일 정보를 조회합니다.
        /// </summary>
        /// <param name="thumbnailType">조회할 썸네일 타입입니다.</param>
        /// <returns>일치하는 정보가 있으면 반환하고, 없으면 <see langword="null"/>을 반환합니다.</returns>
        private EntityThumbnailInfo GetEntityInfo(PopupBubbleThumbnailType thumbnailType)
        {
            if (entityThumbnailInfos == null || entityThumbnailInfos.Count == 0)
            {
                return null;
            }

            foreach (EntityThumbnailInfo info in entityThumbnailInfos)
            {
                if (info.thumbnailType == thumbnailType)
                {
                    return info;
                }
            }

            return null;
        }
    }
}

