using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 위에 대사 말풍선을 표시하고 필요 시 타자 효과를 진행하는 UI 컴포넌트입니다.
    /// </summary>
    public class UIDialogueBalloon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMessage;
        [SerializeField] private Image imageThumbnail;
        [SerializeField] private Image imageTail;
        [SerializeField] private Transform transformBalloon;
        [Header("말풍선 월드 위치")]
        [Tooltip("프로젝트 기본 말풍선 월드 오프셋입니다. ScriptableObject를 찾지 못한 경우 이 값을 사용합니다.")]
        [SerializeField] private Vector3 projectWorldOffset = Vector3.zero;
        [Tooltip("프로젝트 기본 말풍선 월드 오프셋 X값의 화자 방향 연동 정책입니다. ScriptableObject를 찾지 못한 경우 이 값을 사용합니다.")]
        [SerializeField] private DialogueBalloonWorldOffsetXPolicy projectWorldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.KeepOriginal;

        private readonly DialogueTextRevealPlayer _revealPlayer = new();
        private CharacterBase _target;
        private Vector3 _diffTextPosition;
        private bool _useProjectWorldOffset = true;
        private Vector3 _eventWorldOffset;
        private DialogueBalloonWorldOffsetXPolicy _eventWorldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy;
        private RectTransform _balloonRectTransform;
        private RectTransform _transformBalloonRectTransform;
        private RectTransform _panelRectTransform;
        private RectTransform _thumbnailRectTransform;
        private RectTransform _tailRectTransform;
        private VerticalLayoutGroup _panelLayoutGroup;
        private LayoutElement _panelLayoutElement;
        private ConfigCommon.ThumbnailPositionType _thumbnailPositionType;
        private Vector3 _offsetImageThumbnailCharacter;
        private Vector3 _offsetImageThumbnailCharacterLeft;
        private DialogueBalloonThumbnailFlipPolicy _thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.KeepOriginal;
        private DialogueBalloonThumbnailSourceFacing _thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;
        private bool _useSymmetricLayoutByTail = true;
        private float _tailForwardOffsetPx = 3f;
        private float _minHalfExtentByTailPx;
        private int _textPaddingOnNonThumbnailSidePx = 7;
        private int _textPaddingOnThumbnailSidePx = 3;
        private float _thumbnailGapPx;
        private Vector3 _thumbnailBaseScale = Vector3.one;
        private bool _hasThumbnailBaseScale;
        private Vector3 _tailBaseScale = Vector3.one;
        private bool _hasTailBaseScale;
        private bool _hasDefaultPanelPadding;
        private int _defaultPanelPaddingLeft;
        private int _defaultPanelPaddingRight;
        private float _defaultPanelLayoutMinWidth = -1f;
        private bool _hasDefaultPanelLayoutMinWidth;
        private int _thumbnailRequestVersion;
        private bool _needsRefreshThumbnailPosition;
        private readonly Vector3[] _worldCornersBuffer = new Vector3[4];
        private CanvasGroup _balloonCanvasGroup;
        private bool _shouldShowBalloonWhenReady;
        private bool _isMessagePreparationComplete;
        private bool _isThumbnailPreparationComplete;
        private bool _isLayoutPreparationComplete;

        /// <summary>
        /// 현재 말풍선 메시지가 모두 표시되었는지 여부를 반환합니다.
        /// </summary>
        public bool IsFullyRevealed => _revealPlayer.IsFullyRevealed;

        /// <summary>
        /// 말풍선 UI에 필요한 RectTransform과 썸네일 참조를 캐시합니다.
        /// </summary>
        private void Awake()
        {
            CacheLayoutReferences();
        }

        /// <summary>
        /// 말풍선 대상 캐릭터와 표시할 대사 데이터를 초기화합니다.
        /// </summary>
        /// <param name="characterBase">말풍선을 따라갈 대상 캐릭터입니다.</param>
        /// <param name="data">말풍선 메시지와 표시 옵션입니다.</param>
        public void Initialize(CharacterBase characterBase, DialogueBalloonData data)
        {
            _target = characterBase;
            DialogueBalloonData safeData = data ?? new DialogueBalloonData();
            SetWorldPositionOptions(safeData);
            BeginBalloonPresentation();
            SetFontSize(safeData.fontSize);
            SetMessage(safeData);
            SetThumbnailOptions(safeData);
            ApplyTailFlip();
        }

        /// <summary>
        /// 타자 효과 진행 중인 메시지를 즉시 전부 표시합니다.
        /// </summary>
        public void RevealAll()
        {
            _revealPlayer.RevealAll(textMessage);
            RequestThumbnailPositionRefresh();
        }

        /// <summary>
        /// 말풍선 텍스트의 폰트 크기를 적용합니다.
        /// </summary>
        /// <param name="size">적용할 폰트 크기입니다. 0 이하이면 현재 값을 유지합니다.</param>
        private void SetFontSize(float size)
        {
            if (textMessage == null) return;
            if (size <= 0) return;
            textMessage.fontSize = size;
        }

        /// <summary>
        /// 말풍선 월드 좌표 계산에 사용할 이벤트별 오프셋 정책을 저장합니다.
        /// </summary>
        /// <param name="data">현재 말풍선 이벤트 데이터입니다.</param>
        private void SetWorldPositionOptions(DialogueBalloonData data)
        {
            _useProjectWorldOffset = data != null && data.useProjectWorldOffset;
            _eventWorldOffset = data != null ? data.worldOffset : Vector3.zero;
            _eventWorldOffsetXPolicy = data != null
                ? data.GetSafeWorldOffsetXPolicy()
                : DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy;
        }

        /// <summary>
        /// 말풍선 메시지를 적용하고 타자 효과 상태를 초기화합니다.
        /// </summary>
        /// <param name="data">말풍선 메시지와 타자 효과 설정입니다.</param>
        private void SetMessage(DialogueBalloonData data)
        {
            if (textMessage == null)
            {
                _isMessagePreparationComplete = true;
                TryShowBalloonWhenReady();
                return;
            }

            _revealPlayer.Configure(
                textMessage,
                data.message,
                data.useTypewriter,
                data.GetSafeTypewriterCharactersPerSecond());
            _isMessagePreparationComplete = true;
            RequestThumbnailPositionRefresh();
            TryShowBalloonWhenReady();
        }

        /// <summary>
        /// 말풍선 데이터의 썸네일 표시 옵션을 적용하고 비동기 썸네일 로드를 시작합니다.
        /// </summary>
        /// <param name="data">썸네일 표시 위치, 직접 지정 이미지, 오프셋 설정을 포함한 말풍선 데이터입니다.</param>
        private void SetThumbnailOptions(DialogueBalloonData data)
        {
            _thumbnailPositionType = data.thumbnailPositionType;
            _offsetImageThumbnailCharacter = data.offsetImageThumbnailCharacter;
            _offsetImageThumbnailCharacterLeft = data.offsetImageThumbnailCharacterLeft;
            _thumbnailFlipPolicy = data.thumbnailFlipPolicy;
            _thumbnailSourceFacing = data.thumbnailSourceFacing;
            _useSymmetricLayoutByTail = data.useSymmetricLayoutByTail;
            _tailForwardOffsetPx = data.GetSafeTailForwardOffsetPx();
            _minHalfExtentByTailPx = data.GetSafeMinHalfExtentByTailPx();
            _textPaddingOnNonThumbnailSidePx = data.GetSafeTextPaddingOnNonThumbnailSidePx();
            _textPaddingOnThumbnailSidePx = data.GetSafeTextPaddingOnThumbnailSidePx();
            _thumbnailGapPx = data.GetSafeThumbnailGapPx();
            _isThumbnailPreparationComplete = false;
            RequestThumbnailPositionRefresh();

            int requestVersion = ++_thumbnailRequestVersion;
            if (_thumbnailPositionType == ConfigCommon.ThumbnailPositionType.None || !TryEnsureLayoutReferences())
            {
                ClearThumbnail();
                _isThumbnailPreparationComplete = true;
                RequestThumbnailPositionRefresh();
                TryShowBalloonWhenReady();
                return;
            }

            imageThumbnail.sprite = null;
            imageThumbnail.gameObject.SetActive(false);
            _ = BindThumbnailAsync(data, requestVersion);
        }

        /// <summary>
        /// 캐릭터 정보에 맞는 썸네일을 비동기로 가져와 현재 말풍선 요청에 반영합니다.
        /// </summary>
        /// <param name="data">썸네일을 찾을 캐릭터 정보와 직접 지정 이미지 이름입니다.</param>
        /// <param name="requestVersion">이 비동기 요청이 여전히 최신 요청인지 판별하는 버전입니다.</param>
        private async Task BindThumbnailAsync(DialogueBalloonData data, int requestVersion)
        {
            Sprite sprite = null;

            try
            {
                sprite = await DialogueCharacterHelper.GetThumbnail(
                    data.characterType,
                    data.characterUid,
                    data.thumbnailImage);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"말풍선 썸네일 로드 실패: {e.Message}");
            }

            if (requestVersion != _thumbnailRequestVersion || this == null || imageThumbnail == null)
            {
                return;
            }

            if (sprite == null)
            {
                ClearThumbnail();
                _isThumbnailPreparationComplete = true;
                RequestThumbnailPositionRefresh();
                TryShowBalloonWhenReady();
                return;
            }

            imageThumbnail.sprite = sprite;
            ApplyThumbnailNativeSize();
            imageThumbnail.gameObject.SetActive(true);
            _isThumbnailPreparationComplete = true;
            ApplyThumbnailFlip();
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// 썸네일 참조와 RectTransform 참조를 찾고 캐시합니다.
        /// 프리팹 필드가 비어 있어도 ImageThumbnail 자식을 찾아 사용할 수 있게 보정합니다.
        /// </summary>
        private void CacheLayoutReferences()
        {
            _balloonRectTransform = transform as RectTransform;
            if (_balloonCanvasGroup == null && _balloonRectTransform != null)
            {
                _balloonCanvasGroup = _balloonRectTransform.GetComponent<CanvasGroup>();
                if (_balloonCanvasGroup == null)
                {
                    _balloonCanvasGroup = _balloonRectTransform.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (transformBalloon == null)
            {
                transformBalloon = transform.Find("TransformBalloon");
            }

            _transformBalloonRectTransform = transformBalloon as RectTransform;

            Transform panelTransform = _transformBalloonRectTransform?.Find("Panel");
            _panelRectTransform = panelTransform as RectTransform ??
                                  _transformBalloonRectTransform ??
                                  _balloonRectTransform;
            if (_panelRectTransform != null)
            {
                _panelLayoutGroup = _panelRectTransform.GetComponent<VerticalLayoutGroup>();
                _panelLayoutElement = _panelRectTransform.GetComponent<LayoutElement>();
                if (_panelLayoutElement == null)
                {
                    _panelLayoutElement = _panelRectTransform.gameObject.AddComponent<LayoutElement>();
                }

                if (_panelLayoutGroup != null && !_hasDefaultPanelPadding)
                {
                    _defaultPanelPaddingLeft = _panelLayoutGroup.padding.left;
                    _defaultPanelPaddingRight = _panelLayoutGroup.padding.right;
                    _hasDefaultPanelPadding = true;
                }

                if (_panelLayoutElement != null && !_hasDefaultPanelLayoutMinWidth)
                {
                    _defaultPanelLayoutMinWidth = _panelLayoutElement.minWidth;
                    _hasDefaultPanelLayoutMinWidth = true;
                }
            }

            if (imageThumbnail == null)
            {
                Transform thumbnailTransform = _transformBalloonRectTransform?.Find("ImageThumbnail") ??
                                               transform.Find("ImageThumbnail");
                if (thumbnailTransform != null)
                {
                    imageThumbnail = thumbnailTransform.GetComponent<Image>();
                }
            }

            if (imageThumbnail != null)
            {
                _thumbnailRectTransform = imageThumbnail.GetComponent<RectTransform>();
                if (_thumbnailRectTransform != null && !_hasThumbnailBaseScale)
                {
                    _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                    _hasThumbnailBaseScale = true;
                }
            }

            if (imageTail == null)
            {
                Transform tailTransform = transform.Find("IconTail") ??
                                          _transformBalloonRectTransform?.Find("IconTail");
                if (tailTransform != null)
                {
                    imageTail = tailTransform.GetComponent<Image>();
                }
            }

            if (imageTail != null)
            {
                _tailRectTransform = imageTail.GetComponent<RectTransform>();
                if (_tailRectTransform != null && !_hasTailBaseScale)
                {
                    _tailBaseScale = _tailRectTransform.localScale;
                    _hasTailBaseScale = true;
                }
            }
            else if (_tailRectTransform == null)
            {
                Transform tailTransform = transform.Find("IconTail") ??
                                          _transformBalloonRectTransform?.Find("IconTail");
                _tailRectTransform = tailTransform as RectTransform;
            }
        }

        /// <summary>
        /// 말풍선 레이아웃 계산에 필요한 참조가 준비되어 있는지 확인하고, 없으면 캐시를 다시 시도합니다.
        /// </summary>
        /// <returns>패널/꼬리/썸네일 참조가 준비되었으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureLayoutReferences()
        {
            if (imageThumbnail == null ||
                _transformBalloonRectTransform == null ||
                _thumbnailRectTransform == null ||
                _panelRectTransform == null ||
                _tailRectTransform == null ||
                _panelLayoutElement == null)
            {
                CacheLayoutReferences();
            }

            return imageThumbnail != null &&
                   _transformBalloonRectTransform != null &&
                   _thumbnailRectTransform != null &&
                   _panelRectTransform != null &&
                   _tailRectTransform != null &&
                   _panelLayoutElement != null;
        }

        /// <summary>
        /// 현재 썸네일과 표시 상태를 초기화합니다.
        /// </summary>
        private void ClearThumbnail()
        {
            if (!TryEnsureLayoutReferences())
            {
                return;
            }

            imageThumbnail.sprite = null;
            imageThumbnail.gameObject.SetActive(false);
            RestoreThumbnailScaleToBase();
        }

        /// <summary>
        /// 현재 프레임에서 실제로 썸네일을 배치해야 하는지 반환합니다.
        /// </summary>
        /// <returns>썸네일이 유효하고 활성화되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool HasVisibleThumbnail()
        {
            return _thumbnailPositionType != ConfigCommon.ThumbnailPositionType.None &&
                   imageThumbnail != null &&
                   imageThumbnail.gameObject.activeSelf &&
                   imageThumbnail.sprite != null;
        }

        /// <summary>
        /// 썸네일 배치 방향을 좌(-1) / 우(+1) 부호로 반환합니다.
        /// </summary>
        /// <returns>왼쪽 배치면 -1, 오른쪽 배치면 +1을 반환합니다.</returns>
        private float ResolveThumbnailSideSign()
        {
            return _thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left ? -1f : 1f;
        }

        /// <summary>
        /// 썸네일 배치 방향에 맞는 위치 보정값을 반환합니다.
        /// </summary>
        /// <returns>좌/우 방향별 썸네일 보정 오프셋입니다.</returns>
        private Vector3 ResolveThumbnailOffset()
        {
            return _thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left
                ? _offsetImageThumbnailCharacterLeft
                : _offsetImageThumbnailCharacter;
        }

        /// <summary>
        /// 썸네일 유무/방향에 따라 텍스트 좌우 패딩을 적용합니다.
        /// </summary>
        private void ApplyPanelPaddingByThumbnailSide()
        {
            if (_panelLayoutGroup == null)
            {
                return;
            }

            if (!HasVisibleThumbnail())
            {
                if (_hasDefaultPanelPadding)
                {
                    _panelLayoutGroup.padding.left = _defaultPanelPaddingLeft;
                    _panelLayoutGroup.padding.right = _defaultPanelPaddingRight;
                }

                return;
            }

            bool isThumbnailLeft = _thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
            _panelLayoutGroup.padding.left = isThumbnailLeft
                ? _textPaddingOnThumbnailSidePx
                : _textPaddingOnNonThumbnailSidePx;
            _panelLayoutGroup.padding.right = isThumbnailLeft
                ? _textPaddingOnNonThumbnailSidePx
                : _textPaddingOnThumbnailSidePx;
        }

        /// <summary>
        /// 풀 반환 시 말풍선 패널 레이아웃 상태를 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestorePanelLayoutDefaults()
        {
            if (_panelLayoutGroup != null && _hasDefaultPanelPadding)
            {
                _panelLayoutGroup.padding.left = _defaultPanelPaddingLeft;
                _panelLayoutGroup.padding.right = _defaultPanelPaddingRight;
            }

            if (_panelLayoutElement != null && _hasDefaultPanelLayoutMinWidth)
            {
                _panelLayoutElement.minWidth = _defaultPanelLayoutMinWidth;
            }
        }

        /// <summary>
        /// 말꼬리 중심 대칭 최소 반너비 조건에 맞도록 패널 최소 가로 크기를 갱신합니다.
        /// </summary>
        /// <param name="hasThumbnail">현재 썸네일이 배치되는 상태인지 여부입니다.</param>
        /// <param name="side">썸네일 배치 방향 부호입니다. 왼쪽 -1, 오른쪽 +1 입니다.</param>
        /// <param name="thumbnailOffsetX">썸네일 X 보정값입니다.</param>
        private void UpdatePanelMinimumWidthByTailSymmetry(bool hasThumbnail, float side, float thumbnailOffsetX)
        {
            if (_panelLayoutElement == null)
            {
                return;
            }

            if (!_useSymmetricLayoutByTail || _minHalfExtentByTailPx <= 0f)
            {
                if (_hasDefaultPanelLayoutMinWidth)
                {
                    _panelLayoutElement.minWidth = _defaultPanelLayoutMinWidth;
                }

                return;
            }

            float thumbnailWidth = hasThumbnail ? _thumbnailRectTransform.rect.width : 0f;
            float thumbnailSpan = hasThumbnail ? _thumbnailGapPx + thumbnailWidth : 0f;
            float requiredPanelWidth = (2f * _minHalfExtentByTailPx) - thumbnailSpan - (side * thumbnailOffsetX);
            requiredPanelWidth = Mathf.Max(0f, requiredPanelWidth);
            _panelLayoutElement.minWidth = requiredPanelWidth;
        }

        /// <summary>
        /// 화자 방향(좌/우)을 기준으로 말꼬리 X 오프셋을 계산합니다.
        /// </summary>
        /// <returns>말꼬리의 목표 anchoredPosition.x 값입니다.</returns>
        private float ResolveTailAnchorX()
        {
            if (_tailForwardOffsetPx <= 0f)
            {
                return 0f;
            }

            if (TryResolveSpeakerFacingRight(out bool isFacingRight))
            {
                return isFacingRight ? _tailForwardOffsetPx : -_tailForwardOffsetPx;
            }

            return 0f;
        }

        /// <summary>
        /// 말꼬리의 로컬 X 위치를 갱신하고 변경 여부를 반환합니다.
        /// </summary>
        /// <returns>말꼬리 X 값이 바뀌었으면 <see langword="true"/>를 반환합니다.</returns>
        private bool RefreshTailAnchorPosition()
        {
            if (_tailRectTransform == null)
            {
                return false;
            }

            float targetTailX = ResolveTailAnchorX();
            Vector2 anchoredPosition = _tailRectTransform.anchoredPosition;
            if (Mathf.Abs(anchoredPosition.x - targetTailX) <= 0.01f)
            {
                return false;
            }

            anchoredPosition.x = targetTailX;
            _tailRectTransform.anchoredPosition = anchoredPosition;
            return true;
        }

        /// <summary>
        /// 패널의 중심 X를 루트 로컬 좌표계에서 설정합니다.
        /// </summary>
        /// <param name="panelCenterX">설정할 패널 중심 X입니다.</param>
        private void SetPanelCenterX(float panelCenterX)
        {
            if (_panelRectTransform == null)
            {
                return;
            }

            Vector2 anchoredPosition = _panelRectTransform.anchoredPosition;
            anchoredPosition.x = panelCenterX;
            _panelRectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 썸네일이 패널(말풍선 본체) 하위에 배치되어 있는지 확인합니다.
        /// </summary>
        /// <returns>썸네일 부모가 패널이면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsThumbnailChildOfPanel()
        {
            return _thumbnailRectTransform != null &&
                   _panelRectTransform != null &&
                   _thumbnailRectTransform.parent == _panelRectTransform;
        }

        /// <summary>
        /// 루트 말풍선 좌표계 X 값을 임의의 부모 RectTransform 로컬 X 값으로 변환합니다.
        /// </summary>
        /// <param name="parentRectTransform">변환 기준 부모 RectTransform 입니다.</param>
        /// <param name="rootSpaceX">루트 말풍선 좌표계 기준 X 값입니다.</param>
        /// <returns>부모 로컬 좌표계의 X 값입니다.</returns>
        private float ConvertRootSpaceXToParentLocalX(RectTransform parentRectTransform, float rootSpaceX)
        {
            if (_balloonRectTransform == null || parentRectTransform == null)
            {
                return rootSpaceX;
            }

            Vector3 worldPoint = _balloonRectTransform.TransformPoint(new Vector3(rootSpaceX, 0f, 0f));
            Vector3 parentLocalPoint = parentRectTransform.InverseTransformPoint(worldPoint);
            return parentLocalPoint.x;
        }

        /// <summary>
        /// RectTransform 의 실제 표시 영역 X 경계를 루트 말풍선 좌표계 기준으로 계산합니다.
        /// </summary>
        /// <param name="targetRectTransform">경계를 계산할 대상 RectTransform 입니다.</param>
        /// <param name="left">계산된 왼쪽 경계 X 입니다.</param>
        /// <param name="right">계산된 오른쪽 경계 X 입니다.</param>
        /// <returns>계산에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryGetRectXBoundsInRootSpace(RectTransform targetRectTransform, out float left, out float right)
        {
            left = 0f;
            right = 0f;
            if (_balloonRectTransform == null || targetRectTransform == null)
            {
                return false;
            }

            targetRectTransform.GetWorldCorners(_worldCornersBuffer);

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            for (int i = 0; i < _worldCornersBuffer.Length; i++)
            {
                Vector3 localPoint = _balloonRectTransform.InverseTransformPoint(_worldCornersBuffer[i]);
                minX = Mathf.Min(minX, localPoint.x);
                maxX = Mathf.Max(maxX, localPoint.x);
            }

            left = minX;
            right = maxX;
            return true;
        }

        /// <summary>
        /// 썸네일 Border/Flip 을 반영한 실제 가시 영역 X 경계를 루트 말풍선 좌표계 기준으로 계산합니다.
        /// </summary>
        /// <param name="left">계산된 썸네일 가시영역 왼쪽 경계 X 입니다.</param>
        /// <param name="right">계산된 썸네일 가시영역 오른쪽 경계 X 입니다.</param>
        /// <returns>계산에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryGetThumbnailVisibleBoundsInRootSpace(out float left, out float right)
        {
            left = 0f;
            right = 0f;
            if (_thumbnailRectTransform == null || imageThumbnail == null)
            {
                return false;
            }

            if (!TryGetRectXBoundsInRootSpace(_thumbnailRectTransform, out float fullLeft, out float fullRight))
            {
                return false;
            }

            float fullWidth = fullRight - fullLeft;
            if (fullWidth <= 0f)
            {
                left = fullLeft;
                right = fullRight;
                return true;
            }

            Sprite thumbnailSprite = imageThumbnail.sprite;
            if (thumbnailSprite == null || thumbnailSprite.rect.width <= 0f)
            {
                left = fullLeft;
                right = fullRight;
                return true;
            }

            float spriteWidth = thumbnailSprite.rect.width;
            float leftTrimRatio = Mathf.Clamp01(thumbnailSprite.border.x / spriteWidth);
            float rightTrimRatio = Mathf.Clamp01(thumbnailSprite.border.z / spriteWidth);

            bool isFlippedHorizontally = _thumbnailRectTransform.lossyScale.x < 0f;
            if (isFlippedHorizontally)
            {
                float temporaryTrimRatio = leftTrimRatio;
                leftTrimRatio = rightTrimRatio;
                rightTrimRatio = temporaryTrimRatio;
            }

            left = fullLeft + (fullWidth * leftTrimRatio);
            right = fullRight - (fullWidth * rightTrimRatio);
            if (right < left)
            {
                float center = (left + right) * 0.5f;
                left = center;
                right = center;
            }

            return true;
        }

        /// <summary>
        /// 패널과 썸네일의 실제 가시 영역을 합산해 TransformBalloon 중심을 루트 중앙(X=0)으로 재정렬합니다.
        /// </summary>
        /// <param name="hasThumbnail">현재 썸네일이 표시 중인지 여부입니다.</param>
        private void RecenterTransformBalloonByVisibleBounds(bool hasThumbnail)
        {
            if (_transformBalloonRectTransform == null || _panelRectTransform == null)
            {
                return;
            }

            if (!TryGetRectXBoundsInRootSpace(_panelRectTransform, out float left, out float right))
            {
                return;
            }

            if (hasThumbnail && TryGetThumbnailVisibleBoundsInRootSpace(out float thumbnailLeft, out float thumbnailRight))
            {
                left = Mathf.Min(left, thumbnailLeft);
                right = Mathf.Max(right, thumbnailRight);
            }

            float centerOffset = (left + right) * 0.5f;
            if (Mathf.Abs(centerOffset) <= 0.01f)
            {
                return;
            }

            Vector2 anchoredPosition = _transformBalloonRectTransform.anchoredPosition;
            anchoredPosition.x -= centerOffset;
            _transformBalloonRectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// TransformBalloon 의 X 중심 좌표를 지정한 값으로 설정합니다.
        /// </summary>
        /// <param name="centerX">적용할 로컬 중심 X 값입니다.</param>
        private void SetTransformBalloonCenterX(float centerX)
        {
            if (_transformBalloonRectTransform == null)
            {
                return;
            }

            Vector2 anchoredPosition = _transformBalloonRectTransform.anchoredPosition;
            anchoredPosition.x = centerX;
            _transformBalloonRectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 새 말풍선 표시 사이클을 시작하면서 준비 완료 전까지 UI를 숨깁니다.
        /// </summary>
        private void BeginBalloonPresentation()
        {
            _shouldShowBalloonWhenReady = true;
            _isMessagePreparationComplete = false;
            _isThumbnailPreparationComplete = false;
            _isLayoutPreparationComplete = false;
            SetBalloonVisible(false);
        }

        /// <summary>
        /// 말풍선 전체 표시 여부를 CanvasGroup 알파 값으로 제어합니다.
        /// </summary>
        /// <param name="isVisible">표시하려면 <see langword="true"/>, 숨기려면 <see langword="false"/>입니다.</param>
        private void SetBalloonVisible(bool isVisible)
        {
            if (_balloonCanvasGroup == null)
            {
                CacheLayoutReferences();
            }

            if (_balloonCanvasGroup == null)
            {
                return;
            }

            _balloonCanvasGroup.alpha = isVisible ? 1f : 0f;
            _balloonCanvasGroup.interactable = isVisible;
            _balloonCanvasGroup.blocksRaycasts = isVisible;
        }

        /// <summary>
        /// 메시지/썸네일/레이아웃 준비가 모두 완료되었는지 검사하고, 준비 완료 시 말풍선을 표시합니다.
        /// </summary>
        private void TryShowBalloonWhenReady()
        {
            if (!_shouldShowBalloonWhenReady)
            {
                return;
            }

            if (!_isMessagePreparationComplete ||
                !_isThumbnailPreparationComplete ||
                !_isLayoutPreparationComplete)
            {
                return;
            }

            SetBalloonVisible(true);
            _shouldShowBalloonWhenReady = false;
        }

        /// <summary>
        /// 썸네일 RectTransform 크기를 스프라이트 원본 크기로 적용합니다.
        /// </summary>
        private void ApplyThumbnailNativeSize()
        {
            if (imageThumbnail == null || imageThumbnail.sprite == null)
            {
                return;
            }

            imageThumbnail.SetNativeSize();
        }

        /// <summary>
        /// 말꼬리 중심 좌우 대칭 규칙과 썸네일 배치 옵션을 반영해 패널/썸네일 위치를 갱신합니다.
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            _needsRefreshThumbnailPosition = false;

            if (!TryEnsureLayoutReferences())
            {
                _isLayoutPreparationComplete = true;
                TryShowBalloonWhenReady();
                return;
            }

            bool hasThumbnail = HasVisibleThumbnail();
            ApplyPanelPaddingByThumbnailSide();

            float side = ResolveThumbnailSideSign();
            Vector3 thumbnailOffset = ResolveThumbnailOffset();
            UpdatePanelMinimumWidthByTailSymmetry(hasThumbnail, side, thumbnailOffset.x);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);

            float panelHalfWidth = _panelRectTransform.rect.width * 0.5f;
            float tailX = _tailRectTransform.anchoredPosition.x;
            float panelCenterX = tailX;

            if (_useSymmetricLayoutByTail && hasThumbnail)
            {
                float thumbnailWidth = _thumbnailRectTransform.rect.width;
                float thumbnailSpan = _thumbnailGapPx + thumbnailWidth;
                panelCenterX = tailX - (side * (thumbnailSpan * 0.5f)) - (thumbnailOffset.x * 0.5f);
            }
            else if (!_useSymmetricLayoutByTail)
            {
                panelCenterX = 0f;
            }

            SetPanelCenterX(panelCenterX);

            if (hasThumbnail)
            {
                float thumbnailHalfWidth = _thumbnailRectTransform.rect.width * 0.5f;
                float thumbnailCenterXInRootSpace = panelCenterX + (side * (panelHalfWidth + _thumbnailGapPx + thumbnailHalfWidth)) + thumbnailOffset.x;
                RectTransform thumbnailParentRectTransform = _thumbnailRectTransform.parent as RectTransform;
                float thumbnailCenterX = IsThumbnailChildOfPanel()
                    ? (side * (panelHalfWidth + _thumbnailGapPx + thumbnailHalfWidth)) + thumbnailOffset.x
                    : ConvertRootSpaceXToParentLocalX(thumbnailParentRectTransform, thumbnailCenterXInRootSpace);

                Vector2 thumbnailAnchoredPosition = _thumbnailRectTransform.anchoredPosition;
                thumbnailAnchoredPosition.x = thumbnailCenterX;
                thumbnailAnchoredPosition.y = thumbnailOffset.y;
                _thumbnailRectTransform.anchoredPosition = thumbnailAnchoredPosition;
            }

            ApplyThumbnailFlip();
            RecenterTransformBalloonByVisibleBounds(hasThumbnail);
            _isLayoutPreparationComplete = true;
            TryShowBalloonWhenReady();
        }

        /// <summary>
        /// 다음 LateUpdate에서 썸네일 위치를 다시 계산하도록 예약합니다.
        /// </summary>
        private void RequestThumbnailPositionRefresh()
        {
            _needsRefreshThumbnailPosition = true;
        }

        /// <summary>
        /// 풀 반환이나 비활성화 시 남아 있는 메시지 노출 상태를 초기화합니다.
        /// </summary>
        private void OnDisable()
        {
            _thumbnailRequestVersion++;
            _target = null;
            _useProjectWorldOffset = true;
            _eventWorldOffset = Vector3.zero;
            _eventWorldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy;
            _needsRefreshThumbnailPosition = false;
            _shouldShowBalloonWhenReady = false;
            _isMessagePreparationComplete = false;
            _isThumbnailPreparationComplete = false;
            _isLayoutPreparationComplete = false;
            _revealPlayer.Clear(textMessage);
            ClearThumbnail();
            RestoreThumbnailScaleToBase();
            RestoreTailScaleToBase();
            RestorePanelLayoutDefaults();
            SetPanelCenterX(0f);
            SetTransformBalloonCenterX(0f);
            if (_tailRectTransform != null)
            {
                Vector2 tailAnchoredPosition = _tailRectTransform.anchoredPosition;
                tailAnchoredPosition.x = 0f;
                _tailRectTransform.anchoredPosition = tailAnchoredPosition;
            }

            SetBalloonVisible(true);
        }

        /// <summary>
        /// 현재 설정된 정책으로 썸네일 좌우 반전을 적용합니다.
        /// </summary>
        private void ApplyThumbnailFlip()
        {
            if (!TryEnsureLayoutReferences() || !HasVisibleThumbnail())
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
        /// 정책/배치/화자 방향을 종합해 썸네일 Flip 필요 여부를 계산합니다.
        /// </summary>
        /// <returns>좌우 반전이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ResolveShouldFlipThumbnail()
        {
            switch (_thumbnailFlipPolicy)
            {
                case DialogueBalloonThumbnailFlipPolicy.KeepOriginal:
                    return false;

                case DialogueBalloonThumbnailFlipPolicy.ForceFlip:
                    return true;

                case DialogueBalloonThumbnailFlipPolicy.AutoBySpeakerFacing:
                    if (TryResolveSpeakerFacingRight(out bool speakerFacingRight))
                    {
                        return ShouldFlipToDesiredFacing(speakerFacingRight);
                    }

                    // 화자 방향을 판단할 수 없으면 배치 기준 정책으로 안전하게 폴백합니다.
                    bool desiredFacingByPositionFallback = ResolveDesiredFacingRightByThumbnailPosition();
                    return ShouldFlipToDesiredFacing(desiredFacingByPositionFallback);

                case DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition:
                default:
                    bool desiredFacingByPosition = ResolveDesiredFacingRightByThumbnailPosition();
                    return ShouldFlipToDesiredFacing(desiredFacingByPosition);
            }
        }

        /// <summary>
        /// 썸네일 배치 위치를 기준으로 말풍선을 향하는 수평 바라보기 방향을 계산합니다.
        /// </summary>
        /// <returns>오른쪽을 바라봐야 하면 <see langword="true"/>, 왼쪽이면 <see langword="false"/>를 반환합니다.</returns>
        private bool ResolveDesiredFacingRightByThumbnailPosition()
        {
            return _thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
        }

        /// <summary>
        /// 화자의 현재 방향에서 수평(좌/우) 방향을 추출합니다.
        /// </summary>
        /// <param name="isFacingRight">오른쪽을 바라보면 <see langword="true"/>를 반환합니다.</param>
        /// <returns>좌우 방향을 판별할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryResolveSpeakerFacingRight(out bool isFacingRight)
        {
            isFacingRight = false;
            if (_target == null)
            {
                return false;
            }

            CharacterConstants.FacingDirection8 facing = _target.CurrentFacing;
            switch (facing)
            {
                case CharacterConstants.FacingDirection8.Right:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.DownRight:
                    isFacingRight = true;
                    return true;

                case CharacterConstants.FacingDirection8.Left:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
                    isFacingRight = false;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 프로젝트 말풍선 기본값 ScriptableObject를 로더에서 조회합니다.
        /// </summary>
        /// <param name="settings">조회된 프로젝트 말풍선 설정입니다.</param>
        /// <returns>설정을 찾으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryGetProjectDialogueBalloonSettings(out GGemCoDialogueBalloonSettings settings)
        {
            settings = null;
            if (AddressableLoaderSettings.Instance != null &&
                AddressableLoaderSettings.Instance.dialogueBalloonSettings != null)
            {
                settings = AddressableLoaderSettings.Instance.dialogueBalloonSettings;
                return true;
            }

            if (AddressableLoaderSettingsRegist.Instance != null &&
                AddressableLoaderSettingsRegist.Instance.dialogueBalloonSettings != null)
            {
                settings = AddressableLoaderSettingsRegist.Instance.dialogueBalloonSettings;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 프로젝트 기본 오프셋 X 정책을 유효 범위로 보정해 반환합니다.
        /// </summary>
        /// <param name="policy">보정할 프로젝트 기본 오프셋 X 정책입니다.</param>
        /// <returns>유효한 프로젝트 기본 오프셋 X 정책입니다.</returns>
        private static DialogueBalloonWorldOffsetXPolicy GetSafeProjectWorldOffsetXPolicy(DialogueBalloonWorldOffsetXPolicy policy)
        {
            return policy switch
            {
                DialogueBalloonWorldOffsetXPolicy.KeepOriginal => DialogueBalloonWorldOffsetXPolicy.KeepOriginal,
                DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing => DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing,
                DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy => DialogueBalloonWorldOffsetXPolicy.KeepOriginal,
                _ => DialogueBalloonWorldOffsetXPolicy.KeepOriginal
            };
        }

        /// <summary>
        /// 프로젝트 기본 월드 오프셋/정책을 결정합니다.
        /// ScriptableObject가 로드되어 있으면 해당 값을 우선 적용하고, 없으면 프리팹 직렬화 값을 사용합니다.
        /// </summary>
        /// <param name="offset">적용할 프로젝트 기본 월드 오프셋입니다.</param>
        /// <param name="xPolicy">적용할 프로젝트 기본 X 정책입니다.</param>
        private void ResolveProjectWorldOffsetDefaults(
            out Vector3 offset,
            out DialogueBalloonWorldOffsetXPolicy xPolicy)
        {
            offset = projectWorldOffset;
            xPolicy = GetSafeProjectWorldOffsetXPolicy(projectWorldOffsetXPolicy);

            if (!TryGetProjectDialogueBalloonSettings(out GGemCoDialogueBalloonSettings settings) || settings == null)
            {
                return;
            }

            offset = settings.worldOffset;
            xPolicy = settings.GetSafeWorldOffsetXPolicy();
        }

        /// <summary>
        /// 이벤트 정책과 프로젝트 기본 정책을 결합해 실제 오프셋 X 반영 정책을 결정합니다.
        /// </summary>
        /// <returns>실제 적용할 말풍선 월드 오프셋 X 정책입니다.</returns>
        private DialogueBalloonWorldOffsetXPolicy ResolveWorldOffsetXPolicy(DialogueBalloonWorldOffsetXPolicy projectPolicy)
        {
            return _eventWorldOffsetXPolicy == DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy
                ? projectPolicy
                : _eventWorldOffsetXPolicy;
        }

        /// <summary>
        /// 화자 좌우 방향과 정책을 반영해 월드 오프셋 X 값을 보정합니다.
        /// </summary>
        /// <param name="offsetX">기본 오프셋 X 값입니다.</param>
        /// <param name="policy">적용할 오프셋 X 정책입니다.</param>
        /// <returns>정책이 반영된 오프셋 X 값입니다.</returns>
        private float ResolveWorldOffsetXByPolicy(float offsetX, DialogueBalloonWorldOffsetXPolicy policy)
        {
            if (policy != DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing)
            {
                return offsetX;
            }

            if (!TryResolveSpeakerFacingRight(out bool isFacingRight))
            {
                return offsetX;
            }

            return isFacingRight ? offsetX : -offsetX;
        }

        /// <summary>
        /// 현재 화자 상태와 이벤트 옵션을 바탕으로 말풍선 최종 월드 좌표를 계산합니다.
        /// </summary>
        /// <returns>계산된 말풍선 월드 좌표입니다.</returns>
        private Vector3 ResolveBalloonWorldPosition()
        {
            if (_target == null)
            {
                return transform.position;
            }

            Vector3 baseWorldPosition = _target.transform.position + new Vector3(0f, _target.GetHeightByScale(), 0f);
            Vector3 configurableOffset = _eventWorldOffset;
            ResolveProjectWorldOffsetDefaults(
                out Vector3 resolvedProjectWorldOffset,
                out DialogueBalloonWorldOffsetXPolicy resolvedProjectWorldOffsetXPolicy);
            if (_useProjectWorldOffset)
            {
                configurableOffset += resolvedProjectWorldOffset;
            }

            DialogueBalloonWorldOffsetXPolicy appliedPolicy = ResolveWorldOffsetXPolicy(resolvedProjectWorldOffsetXPolicy);
            configurableOffset.x = ResolveWorldOffsetXByPolicy(configurableOffset.x, appliedPolicy);
            return baseWorldPosition + _diffTextPosition + configurableOffset;
        }

        /// <summary>
        /// 목표 수평 바라보기 방향과 원본 썸네일 기준 방향을 비교해 Flip 필요 여부를 계산합니다.
        /// </summary>
        /// <param name="desiredFacingRight">목표가 오른쪽 바라보기면 <see langword="true"/>입니다.</param>
        /// <returns>원본과 목표 방향이 다르면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldFlipToDesiredFacing(bool desiredFacingRight)
        {
            bool sourceFacingRight = _thumbnailSourceFacing == DialogueBalloonThumbnailSourceFacing.Right;
            return sourceFacingRight != desiredFacingRight;
        }

        /// <summary>
        /// 썸네일 스케일을 프리팹 기본값으로 복원합니다.
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
        /// 꼬리 레이아웃/Flip 계산에 필요한 참조가 준비되어 있는지 확인합니다.
        /// </summary>
        /// <returns>꼬리 이미지와 RectTransform 참조가 준비되었으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureTailReferences()
        {
            if (imageTail == null || _tailRectTransform == null)
            {
                CacheLayoutReferences();
            }

            return imageTail != null && _tailRectTransform != null;
        }

        /// <summary>
        /// 캐릭터 바라보기 방향에 맞춰 꼬리 이미지를 좌우 반전합니다.
        /// 기본 꼬리 리소스 방향은 오른쪽을 바라보는 상태를 기준으로 사용합니다.
        /// </summary>
        private void ApplyTailFlip()
        {
            if (!TryEnsureTailReferences())
            {
                return;
            }

            if (!_hasTailBaseScale)
            {
                _tailBaseScale = _tailRectTransform.localScale;
                _hasTailBaseScale = true;
            }

            bool isFacingRight = true;
            if (TryResolveSpeakerFacingRight(out bool speakerFacingRight))
            {
                isFacingRight = speakerFacingRight;
            }

            float baseAbsX = Mathf.Abs(_tailBaseScale.x);
            if (baseAbsX <= Mathf.Epsilon)
            {
                baseAbsX = 1f;
            }

            float x = isFacingRight ? baseAbsX : -baseAbsX;
            _tailRectTransform.localScale = new Vector3(
                x,
                _tailBaseScale.y,
                _tailBaseScale.z);
        }

        /// <summary>
        /// 꼬리 스케일을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreTailScaleToBase()
        {
            if (_tailRectTransform == null || !_hasTailBaseScale)
            {
                return;
            }

            _tailRectTransform.localScale = _tailBaseScale;
        }

        /// <summary>
        /// 매 프레임 타자 효과와 대상 캐릭터 추적 위치를 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            int previousVisibleCharacters = textMessage != null ? textMessage.maxVisibleCharacters : 0;
            bool wasFullyRevealed = _revealPlayer.IsFullyRevealed;

            _revealPlayer.Tick(textMessage, Time.deltaTime);
            if (textMessage != null)
            {
                bool didVisibleCharactersChange = previousVisibleCharacters != textMessage.maxVisibleCharacters;
                bool didCompleteRevealThisFrame = !wasFullyRevealed && _revealPlayer.IsFullyRevealed;
                if (didVisibleCharactersChange || didCompleteRevealThisFrame)
                {
                    RequestThumbnailPositionRefresh();
                }
            }

            if (RefreshTailAnchorPosition())
            {
                RequestThumbnailPositionRefresh();
            }

            ApplyTailFlip();

            if (_needsRefreshThumbnailPosition)
            {
                RefreshThumbnailPosition();
            }

            if (_target == null) return;
            transform.position = ResolveBalloonWorldPosition();
        }
    }
}
