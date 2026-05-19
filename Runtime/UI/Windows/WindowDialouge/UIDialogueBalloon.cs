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

        private readonly DialogueTextRevealPlayer _revealPlayer = new();
        private CharacterBase _target;
        private Vector3 _diffTextPosition;
        private RectTransform _balloonRectTransform;
        private RectTransform _panelRectTransform;
        private RectTransform _thumbnailRectTransform;
        private ConfigCommon.ThumbnailPositionType _thumbnailPositionType;
        private Vector3 _offsetImageThumbnailCharacter;
        private Vector3 _offsetImageThumbnailCharacterLeft;
        private DialogueBalloonThumbnailFlipPolicy _thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.KeepOriginal;
        private DialogueBalloonThumbnailSourceFacing _thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;
        private Vector3 _thumbnailBaseScale = Vector3.one;
        private bool _hasThumbnailBaseScale;
        private int _thumbnailRequestVersion;
        private bool _needsRefreshThumbnailPosition;

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
            SetFontSize(safeData.fontSize);
            SetMessage(safeData);
            SetThumbnailOptions(safeData);
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
        /// 말풍선 메시지를 적용하고 타자 효과 상태를 초기화합니다.
        /// </summary>
        /// <param name="data">말풍선 메시지와 타자 효과 설정입니다.</param>
        private void SetMessage(DialogueBalloonData data)
        {
            if (textMessage == null) return;
            _revealPlayer.Configure(
                textMessage,
                data.message,
                data.useTypewriter,
                data.GetSafeTypewriterCharactersPerSecond());
            RequestThumbnailPositionRefresh();
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
            RequestThumbnailPositionRefresh();

            int requestVersion = ++_thumbnailRequestVersion;
            if (_thumbnailPositionType == ConfigCommon.ThumbnailPositionType.None || !TryEnsureThumbnailImage())
            {
                ClearThumbnail();
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
                return;
            }

            imageThumbnail.sprite = sprite;
            imageThumbnail.gameObject.SetActive(true);
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

            Transform panelTransform = transform.Find("Panel");
            _panelRectTransform = panelTransform as RectTransform ?? _balloonRectTransform;

            if (imageThumbnail == null)
            {
                Transform thumbnailTransform = transform.Find("ImageThumbnail");
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
        }

        /// <summary>
        /// 썸네일 이미지 참조가 준비되어 있는지 확인하고, 없으면 캐시를 다시 시도합니다.
        /// </summary>
        /// <returns>썸네일 Image를 사용할 수 있으면 <see langword="true"/>, 없으면 <see langword="false"/>를 반환합니다.</returns>
        private bool TryEnsureThumbnailImage()
        {
            if (imageThumbnail == null || _thumbnailRectTransform == null || _panelRectTransform == null)
            {
                CacheLayoutReferences();
            }

            return imageThumbnail != null && _thumbnailRectTransform != null && _panelRectTransform != null;
        }

        /// <summary>
        /// 현재 썸네일과 표시 상태를 초기화합니다.
        /// </summary>
        private void ClearThumbnail()
        {
            if (!TryEnsureThumbnailImage())
            {
                return;
            }

            imageThumbnail.sprite = null;
            imageThumbnail.gameObject.SetActive(false);
            RestoreThumbnailScaleToBase();
        }

        /// <summary>
        /// PopupBubble과 동일한 방식으로 말풍선 패널 크기와 썸네일 크기를 기준으로 썸네일 위치를 갱신합니다.
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            _needsRefreshThumbnailPosition = false;

            if (_thumbnailPositionType == ConfigCommon.ThumbnailPositionType.None || !TryEnsureThumbnailImage())
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRectTransform);

            float panelHalfWidth = _panelRectTransform.rect.width * 0.5f;
            float thumbnailHalfWidth = _thumbnailRectTransform.rect.width * 0.5f;
            Vector3 offset = _offsetImageThumbnailCharacter;
            float side = 1f;

            if (_thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left)
            {
                offset = _offsetImageThumbnailCharacterLeft;
                side = -1f;
            }

            float x = side * (panelHalfWidth + thumbnailHalfWidth) + offset.x;
            float y = offset.y;
            _thumbnailRectTransform.localPosition = new Vector3(x, y, 0f);
            ApplyThumbnailFlip();
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
            _needsRefreshThumbnailPosition = false;
            _revealPlayer.Clear(textMessage);
            ClearThumbnail();
            RestoreThumbnailScaleToBase();
        }

        /// <summary>
        /// 현재 설정된 정책으로 썸네일 좌우 반전을 적용합니다.
        /// </summary>
        private void ApplyThumbnailFlip()
        {
            if (!TryEnsureThumbnailImage())
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
        /// 매 프레임 타자 효과와 대상 캐릭터 추적 위치를 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            _revealPlayer.Tick(textMessage, Time.deltaTime);
            if (_needsRefreshThumbnailPosition)
            {
                RefreshThumbnailPosition();
            }

            if (_target == null) return;
            // 아이템 위 월드 좌표 설정
            Vector3 npcNameWorldPosition = _target.gameObject.transform.position + new Vector3(0, _target.GetHeightByScale(), 0) + _diffTextPosition;
            gameObject.transform.position = npcNameWorldPosition;
        }
    }
}
