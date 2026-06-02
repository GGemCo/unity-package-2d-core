using TMPro;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 공용
    /// </summary>
    public class UIIcon : MonoBehaviour
    {
        [Header(UIWindowConstants.TitleHeaderCommon)] 
        [Tooltip("개수를 표현할 텍스트")]
        public TextMeshProUGUI textCount;
        [Tooltip("개수 앞에 보여줄 문구. 예) x")]
        [SerializeField] private string prefixCount;
        [Tooltip("일반 상태 색상")]
        [SerializeField] private Color colorTextCountNormal = new(255, 255, 255, 255);
        [Tooltip("선택 되었을 때, 개수 색상")]
        [SerializeField] private Color colorTextCountSelected = new(255, 255, 255, 255);
        [Tooltip("장착 되었을 때, 개수 색상")]
        [SerializeField] private Color colorTextCountEquipped = new(255, 255, 0, 255);
        [Tooltip("아이콘 정보를 지웠을 경우 보여줄, 투명한 이미지")]
        [SerializeField] private Sprite spriteBlank;

        [Tooltip("쿨타임 게이지")]
        public Image imageCoolTimeGauge;
        [Tooltip("잠금 표시 이미지")]
        public Image imageLock;
        [Tooltip("Fade In/Out 처리를 위해 Canvas Group 사용 여부.")]
        public bool useCanvasGroup;
        [Tooltip("Canvas Group의 Interactable 설정")]
        public bool isCanvasGroupInteractable = true;

        [Header("UI Effect")]
        [Tooltip("아이콘에 연결할 UIEffectTarget. 비어 있으면 현재 GameObject에서 자동 탐색합니다.")]
        [SerializeField] protected UIEffectTarget effectTarget;
        [Tooltip("포인터 오버 시 재생할 프리셋")]
        [SerializeField] protected UIEffectPreset hoverPreset;
        [Tooltip("포인터 클릭 시 재생할 프리셋")]
        [SerializeField] protected UIEffectPreset clickPreset;
        [Tooltip("잘못된 사용 시 재생할 프리셋")]
        [SerializeField] protected UIEffectPreset invalidPreset;
        [Tooltip("쿨타임 시작 시 재생할 프리셋")]
        [SerializeField] protected UIEffectPreset cooldownStartPreset;
        [Tooltip("쿨타임 완료 시 재생할 프리셋")]
        [SerializeField] protected UIEffectPreset cooldownReadyPreset;
        [Tooltip("장착/등록 완료 시 재생할 프리셋")]
        [SerializeField] protected UIEffectPreset equipPreset;

        [Header("비활성")]
        [Tooltip("비활성 상태일 때 아이콘 이미지에 적용할 색상입니다. 알파 값으로 투명도를 함께 지정합니다.")]
        [SerializeField] private Color colorInactive = new Color(1f, 1f, 1f, 0.35f);
        [Tooltip("비활성 상태일 때 표시할 아이콘 이미지입니다.")]
        [SerializeField] private Sprite inactiveSprite;
        [Tooltip("Inactive Sprite를 표시할 Image입니다. 비어 있으면 기존처럼 ImageIcon에 직접 적용합니다.")]
        [SerializeField] private Image imageInactiveSprite;
        
        // 윈도우 
        [HideInInspector] public UIWindow window;
        // 윈도우 고유번호
        [HideInInspector] public UIWindowConstants.WindowUid windowUid;
        // 번호
        [HideInInspector] public int index;
        // 슬롯 번호
        [HideInInspector] public int slotIndex;
        // 고유번호 (아이템일때는 아이템 고유번호)
        [HideInInspector] public int uid;
        
        // instance uid for rolled options
        [HideInInspector] public long instanceId;
        
        protected bool PossibleClick;
        protected IconConstants.Type IconType;
        // 아이콘 이미지
        protected Image ImageIcon;
        private CanvasGroup _canvasGroup;
        public CanvasGroup CanvasGroup => _canvasGroup;
        
        private bool _showSelectedImage;
        private bool _showOverImage;
        private bool _showZeroCountText;
        private bool _isInactive;
        private bool _hasNormalIconColor;
        private Color _normalIconColor = Color.white;
        private bool _hasNormalIconSprite;
        private Sprite _normalIconSprite;
        private Sprite _inactiveSpriteOverride;
        
        // 부모 윈도우 uid
        private UIWindowConstants.WindowUid _parentWindowUid;
        // 부모 아이콘 슬롯 index
        private int _parentSlotIndex;
        private IconConstants.Status _iconStatus;
        private bool _isSelected;
        private bool _isTextCountEquipped;
        // 개수
        protected int count;
        // 레벨
        private int _level;
        // 배웠는지
        private bool _isLearn;
        // 등급
        private IconConstants.Grade _grade;
        // 등급 아이콘
        [HideInInspector] public Image imageGrade;
            
        // 드래그 핸들러
        private UIDragHandler _dragHandler;
        private RectTransform _rectTransform;
        private UIWindowManager _uiWindowManager;
        private int _iconImageRequestVersion;
        
        private Vector2 _slotSize;
        private UIIconCoolTimeManager _iconCoolTimeManager;

        /// <summary>
        /// 현재 아이콘이 비활성 슬롯 표현 상태인지 반환합니다.
        /// 비활성 아이콘은 실제 아이콘 정보를 가지지 않고 드래그와 클릭을 허용하지 않습니다.
        /// </summary>
        public bool IsInactive => _isInactive;

        protected virtual void Awake()
        {
        }

        private void InitializeComponent()
        {
            if (ImageIcon == null)
                ImageIcon = GetComponent<Image>();

            if (!_hasNormalIconColor && ImageIcon != null)
            {
                _normalIconColor = ImageIcon.color;
                _hasNormalIconColor = true;
            }

            if (!_hasNormalIconSprite && ImageIcon != null)
            {
                CacheNormalIconSprite(ImageIcon.sprite);
            }
            
            _dragHandler = GetComponent<UIDragHandler>();
            if (_dragHandler == null)
                _dragHandler = gameObject.AddComponent<UIDragHandler>();
            
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            
            if (useCanvasGroup)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.interactable = isCanvasGroupInteractable;
            }

            if (effectTarget == null)
            {
                effectTarget = GetComponent<UIEffectTarget>();
            }
        }

        protected virtual void Start()
        {
            if (!SceneGame.Instance) return;
            _uiWindowManager = SceneGame.Instance.uIWindowManager;
            _iconCoolTimeManager = SceneGame.Instance.uIIconCoolTimeManager;   
        }

        public void Initialize(UIWindow pwindow, UIWindowConstants.WindowUid pwindowUid, int pindex, int pslotIndex, 
            Vector2 iconSize, Vector2 slotSize)
        {
            PossibleClick = true;
            uid = 0;
            instanceId = 0;
            _level = 0;
            _parentWindowUid = 0;
            _parentSlotIndex = 0;
            _isLearn = false;
            _iconStatus = IconConstants.Status.Normal;
            _isTextCountEquipped = false;
            _isInactive = false;
            IconType = IconConstants.Type.None;

            if (imageCoolTimeGauge != null)
            {
                imageCoolTimeGauge.gameObject.SetActive(false);
            }
            SetSelected(false);
            SetIconLock(false);
            
            InitializeComponent();
            window = pwindow;
            windowUid = pwindowUid;
            index = pindex;
            slotIndex = pslotIndex;
            _slotSize = slotSize;
            SetCount(0);
            ChangeIconImageSize(iconSize, slotSize);
            ApplyInactiveVisual(false);
            _showSelectedImage = window.showSelectedIconImage;
            _showOverImage = window.showOverIconImage;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public bool IsItem() => IconType == IconConstants.Type.Item;
        public bool IsSkill() => IconType == IconConstants.Type.Skill;
        public bool IsBuff() => IconType == IconConstants.Type.Buff;

        public virtual bool IsEquipType() => false;
        public virtual bool IsPotionType() => false;
        public virtual bool IsHpPotionType() => false;
        public virtual bool IsMpPotionType() => false;
        public virtual bool IsSeedType() => false;
        public virtual bool IsToolType() => false;
        public virtual bool IsAffectUid() => false;
        public IconConstants.Type GetIconType() => IconType;
        public virtual ItemConstants.Type GetItemType() => ItemConstants.Type.None;
        public virtual ItemConstants.Category GetItemCategory() => ItemConstants.Category.None;
        public virtual ItemConstants.SubCategory GetItemSubCategory() => ItemConstants.SubCategory.None;
        public virtual ItemConstants.PartsType GetItemPartsType() => ItemConstants.PartsType.None;
        public IconConstants.Grade GetGrade() => _grade;
        private void SetStatus(IconConstants.Status status) => this._iconStatus = status;

        protected bool IsLock()
        {
            return _iconStatus == IconConstants.Status.Lock;
        }

        protected void UpdateInfo()
        {
            if (uid <= 0) return;
            UpdateIconImage();
        }

        /// <summary>
        /// 다른 uid 로 변경하기
        /// </summary>
        /// <param name="cardUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="iconIsLearn"></param>
        /// <param name="remainCoolTime"></param>
        /// <param name="iconInstanceId"></param>
        /// <param name="iconType"></param>
        public virtual bool ChangeInfoByUid(int cardUid, int iconCount = 0, int iconLevel = 0, 
            bool iconIsLearn = false, int remainCoolTime = 0, long iconInstanceId = 0, 
            IconConstants.Type iconType = IconConstants.Type.None)
        {
            if (_isInactive)
                return false;

            _iconCoolTimeManager?.SetRemainCoolTime(windowUid, cardUid, remainCoolTime);

            if (cardUid == 0 && iconCount == 0)
            {
                ClearIconInfos();
                return false;
            }

            uid = cardUid;
            instanceId = iconInstanceId;
            if (iconType != IconConstants.Type.None)
                IconType = iconType;
            SetCount(iconCount);
            SetLevel(iconLevel);
            SetIsLearn(iconIsLearn);
            return true;
        }

        /// <summary>
        /// 개수 추가하기
        /// </summary>
        /// <param name="value"></param>
        public void AddCount(int value) => SetCount(count + value);
        /// <summary>
        /// 총 개수 가져오기
        /// </summary>
        /// <param name="value"></param>
        public virtual void SetCount(int value)
        {
            count = value;
            if (textCount != null)
            {
                textCount.text = count switch
                {
                    <= 0 => _showZeroCountText ? $"{prefixCount}0" : "",
                    1 => "",
                    _ => $"{prefixCount}{count}",
                };
            }
        }

        /// <summary>
        /// 개수가 0인 아이템을 특수하게 보여주는 선택 문맥에서만 0 텍스트를 노출합니다.
        /// 일반 아이콘은 기존처럼 0 또는 1일 때 개수 텍스트를 숨깁니다.
        /// </summary>
        public void SetShowZeroCountText(bool show)
        {
            _showZeroCountText = show;
            SetCount(count);
        }

        /// <summary>
        /// 아이콘의 비활성 표시 상태를 설정합니다.
        /// 비활성 상태가 되면 실제 아이콘 정보를 지우고, 드래그/클릭을 차단한 뒤 비활성 이미지를 표시합니다.
        /// </summary>
        /// <param name="inactive">비활성 여부입니다.</param>
        public void SetInactiveState(bool inactive)
        {
            if (!_isInactive && !inactive)
            {
                ApplyInactiveVisual(false);
                return;
            }

            _isInactive = inactive;

            if (_isInactive)
            {
                if (uid > 0 || count > 0 || instanceId != 0)
                {
                    ClearIconInfos();
                }
                else
                {
                    uid = 0;
                    instanceId = 0;
                    IconType = IconConstants.Type.None;
                    _level = 0;
                    _isLearn = false;
                    SetIconLock(false);
                    SetTextCountEquippedState(false);
                    SetShowZeroCountText(false);
                    SetCount(0);
                }

                SetSelected(false);
                SetClick(false);
                SetDrag(false);
                ApplyInactiveVisual(true);
                return;
            }

            ApplyInactiveVisual(false);
            SetClick(true);
            SetDrag(!IsLock());

            if (uid <= 0)
            {
                ClearIconInfos();
            }
            else
            {
                UpdateIconImage();
            }
        }

        /// <summary>
        /// 아이콘 정보를 지우지 않고 비활성 표시 상태만 설정합니다.
        /// </summary>
        /// <param name="inactive">비활성 표시를 적용할지 여부입니다.</param>
        /// <param name="blockInteraction">비활성 표시 중 클릭과 드래그를 막을지 여부입니다.</param>
        public void SetInactiveVisualState(bool inactive, bool blockInteraction = true)
        {
            if (_isInactive == inactive)
            {
                ApplyInactiveVisual(inactive);
                return;
            }

            _isInactive = inactive;

            if (_isInactive)
            {
                SetSelected(false);
                SetIconLock(false);
                if (blockInteraction)
                {
                    SetClick(false);
                    SetDrag(false);
                }

                ApplyInactiveVisual(true);
                return;
            }

            ApplyInactiveVisual(false);
            SetClick(true);
            SetDrag(!IsLock());

            if (uid > 0)
            {
                UpdateIconImage();
            }
        }

        /// <summary>
        /// 비활성 상태에 사용할 Sprite를 런타임 데이터 기준으로 오버라이드합니다.
        /// </summary>
        /// <param name="sprite">비활성 상태에 표시할 Sprite입니다. null이면 프리팹 기본 Sprite를 사용합니다.</param>
        public void SetInactiveSpriteOverride(Sprite sprite)
        {
            _inactiveSpriteOverride = sprite;
            if (_isInactive)
            {
                ApplyInactiveVisual(true);
            }
        }

        /// <summary>
        /// 비활성 상태에 맞춰 아이콘 색상과 비활성 스프라이트를 적용합니다.
        /// </summary>
        /// <param name="inactive">비활성 표시를 적용할지 여부입니다.</param>
        private void ApplyInactiveVisual(bool inactive)
        {
            if (ImageIcon == null)
                return;

            SetColorImageIcon(inactive ? colorInactive : _normalIconColor);
            ApplyInactiveSprite(inactive);
        }

        private void SetColorImageIcon(Color color)
        {
            if (ImageIcon) 
                ImageIcon.color = color;
            OnSetColorImageIcon(color);
        }

        protected virtual void OnSetColorImageIcon(Color color)
        {
            
        }

        /// <summary>
        /// 비활성 스프라이트를 설정된 전용 Image 또는 기본 아이콘 Image에 적용합니다.
        /// </summary>
        /// <param name="inactive">비활성 스프라이트를 표시할지 여부입니다.</param>
        private void ApplyInactiveSprite(bool inactive)
        {
            Sprite resolvedInactiveSprite = _inactiveSpriteOverride != null ? _inactiveSpriteOverride : inactiveSprite;
            if (imageInactiveSprite != null)
            {
                imageInactiveSprite.sprite = inactive ? resolvedInactiveSprite : null;
                imageInactiveSprite.gameObject.SetActive(inactive && resolvedInactiveSprite != null);
                return;
            }

            if (inactive)
            {
                if (resolvedInactiveSprite != null)
                {
                    if (ImageIcon.sprite != resolvedInactiveSprite)
                    {
                        CacheNormalIconSprite(ImageIcon.sprite);
                    }

                    ImageIcon.sprite = resolvedInactiveSprite;
                }

                return;
            }

            if (_hasNormalIconSprite)
            {
                ImageIcon.sprite = _normalIconSprite;
            }
        }

        /// <summary>
        /// 활성 상태에서 사용할 기본 아이콘 스프라이트를 캐시합니다.
        /// </summary>
        /// <param name="sprite">기본 아이콘으로 복원할 스프라이트입니다.</param>
        protected void CacheNormalIconSprite(Sprite sprite)
        {
            _normalIconSprite = sprite;
            _hasNormalIconSprite = true;
        }

        /// <summary>
        /// 아이템 잠금
        /// </summary>
        /// <param name="set"></param>
        public void SetIconLock(bool set)
        {
            SetStatus(set ? IconConstants.Status.Lock : IconConstants.Status.Normal);

            if (imageLock != null)
            {
                imageLock.gameObject.SetActive(set && !_isInactive);
            }

            SetDrag(!set);
        }
        /// <summary>
        /// 드래그 가능 여부 on/off
        /// </summary>
        /// <param name="set"></param>
        public void SetDrag(bool set)
        {
            if (_dragHandler == null) return;
            _dragHandler.SetIsPossibleDrag(set && !_isInactive);
        }
        /// <summary>
        /// 아이템 정보 지우기
        /// </summary>
        public virtual void ClearIconInfos()
        {
            _iconCoolTimeManager?.ResetCoolTime(windowUid, uid);
            instanceId = 0;
            IconType = IconConstants.Type.None;
            _level = 0;
            _isLearn = false;
            
            uid = 0;
            if (ImageIcon != null)
            {
                ImageIcon.sprite = spriteBlank;
                CacheNormalIconSprite(spriteBlank);
            }
            if (imageGrade != null)
            {
                imageGrade.sprite = spriteBlank;
            }

            SetIconLock(false);
            SetTextCountEquippedState(false);
            SetShowZeroCountText(false);
            SetCount(0);

            if (_isInactive)
            {
                SetClick(false);
                SetDrag(false);
                ApplyInactiveVisual(true);
            }
        }

        /// <summary>
        /// 아이콘 이미지 경로 가져오기 
        /// </summary>
        /// <returns></returns>
        protected virtual string GetIconImagePath()
        {
            return "";
        }
        /// <summary>
        /// 아이콘 이미지 업데이트 하기
        /// </summary>
        protected virtual void UpdateIconImage()
        {
            if (ImageIcon == null) return;
            int requestVersion = ++_iconImageRequestVersion;
            string path = GetIconImagePath();
            if (string.IsNullOrEmpty(path))
            {
                ImageIcon.sprite = null;
                CacheNormalIconSprite(null);
                return;
            }

            AddressableIconSpriteRequest request =
                new AddressableIconSpriteRequest(AddressableIconAtlasType.ItemIcon, path);

            if (AddressableIconSpriteService.TryGetCachedSprite(request, out Sprite sprite))
            {
                ImageIcon.sprite = sprite;
                CacheNormalIconSprite(sprite);
                return;
            }

            ImageIcon.sprite = null;
            CacheNormalIconSprite(null);
            _ = UpdateIconImageAsync(requestVersion, request);
        }

        /// <summary>
        /// 아이템 아이콘 아틀라스가 아직 준비되지 않은 경우 비동기 로드 후 현재 요청에만 Sprite를 적용합니다.
        /// </summary>
        /// <param name="requestVersion">아이콘 요청 버전입니다.</param>
        /// <param name="request">아이콘 Sprite 요청 정보입니다.</param>
        private async Task UpdateIconImageAsync(
            int requestVersion,
            AddressableIconSpriteRequest request)
        {
            try
            {
                Sprite sprite = await AddressableIconSpriteService.LoadSpriteAsync(request);
                if (this == null || requestVersion != _iconImageRequestVersion || ImageIcon == null)
                {
                    return;
                }

                ImageIcon.sprite = sprite;
                CacheNormalIconSprite(sprite);
            }
            catch (System.Exception ex)
            {
                GcLogger.LogWarning($"UIIcon 아이콘 비동기 갱신 중 오류가 발생했습니다. sprite={request.SpriteName}, error={ex.Message}");
            }
        }

        public void ChangeIconImage(Sprite sprite)
        {
            if (ImageIcon == null) return;
            if (GcLogger.IsNull(sprite, nameof(sprite))) return;
            ImageIcon.sprite = sprite;
            CacheNormalIconSprite(sprite);
        }
        /// <summary>
        /// 이미지 사이즈 변경하기
        /// ray cast 사이즈를 슬롯 사이즈와 같게 변경하기 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="slotSize"></param>
        private void ChangeIconImageSize(Vector2 size, Vector2 slotSize)
        {
            _rectTransform.sizeDelta = size;
            var diff = (slotSize.x - size.x)/2;
            ImageIcon.raycastPadding = new Vector4(-diff, -diff, -diff, -diff);
        }
        /// <summary>
        /// 아이콘의 원래 위치 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector3 GetDragOriginalPosition()
        {
            return _dragHandler.GetOriginalPosition();
        }
        /// <summary>
        /// 선택 상태를 시각적으로 반영하고 선택 이미지를 즉시 갱신합니다.
        /// 실제 선택 변경은 UIWindow.SetSelectedIcon 에서 관리하고, 이 메서드는 선택 표현만 담당합니다.
        /// </summary>
        /// <param name="selected">선택 상태로 표시하면 true입니다.</param>
        public virtual void SetSelected(bool selected)
        {
            SetSelected(selected, true);
        }

        /// <summary>
        /// 선택 상태를 시각적으로 반영하고, 선택 이미지 표시 여부를 호출자가 결정할 수 있게 합니다.
        /// </summary>
        /// <param name="selected">선택 상태로 표시하면 true입니다.</param>
        /// <param name="showSelectedImage">선택 이미지를 즉시 갱신하면 true입니다.</param>
        public void SetSelected(bool selected, bool showSelectedImage)
        {
            _isSelected = selected && !_isInactive;

            if (window)
            {
                var slot = window.GetSlotByIndex(index);
                if (slot != null)
                {
                    slot.SetSelected(_isSelected);
                }
            }

            RefreshTextCountColor();

            if (showSelectedImage)
            {
                RefreshSelectedIconImage();
            }
        }

        /// <summary>
        /// 현재 선택 상태를 기준으로 공통 선택 이미지 표시를 갱신합니다.
        /// </summary>
        public void RefreshSelectedIconImage()
        {
            _uiWindowManager ??= SceneGame.Instance?.uIWindowManager;
            if (!_showSelectedImage || !_uiWindowManager) return;
            Sprite selectedImageSprite = window != null ? window.GetSelectedIconImageSprite(this) : null;
            GameObject selectedImagePrefab = window != null ? window.GetSelectedIconImagePrefab(this) : null;
            UISelectedIconAnimationSettings selectedIconAnimation =
                window != null ? window.GetSelectedIconAnimation(this) : null;
            Transform selectedImageParent = ResolveSelectedIconImageParent();

            _uiWindowManager.ShowSelectIconImage(
                _isSelected,
                gameObject.transform.position,
                _slotSize,
                selectedImageSprite,
                selectedImagePrefab,
                selectedIconAnimation,
                selectedImageParent);
        }

        /// <summary>
        /// 현재 아이콘 선택 이미지가 배치될 부모 Transform을 반환합니다.
        /// UIWindow가 선택 아이콘 하위 배치를 요청하면 현재 아이콘 Transform을 반환하고, 기본 캔버스 배치이면 null을 반환합니다.
        /// </summary>
        /// <returns>선택 이미지가 붙을 부모 Transform입니다. null이면 UIWindowManager의 기본 캔버스를 사용합니다.</returns>
        private Transform ResolveSelectedIconImageParent()
        {
            if (window == null)
            {
                return null;
            }

            return window.GetSelectedIconImageParentMode(this) == UISelectedIconImageParentMode.SelectedIcon
                ? transform
                : null;
        }

        public bool IsSelected() => _isSelected;

        /// <summary>
        /// 개수 텍스트의 장착 상태를 저장하고 색상을 갱신합니다.
        /// 장착 상태는 선택 상태보다 우선 순위가 높습니다.
        /// </summary>
        /// <param name="equipped"></param>
        protected void SetTextCountEquippedState(bool equipped)
        {
            _isTextCountEquipped = equipped;
            RefreshTextCountColor();
        }

        /// <summary>
        /// 개수 텍스트 색상을 현재 상태 우선 순위에 맞게 갱신합니다.
        /// 우선 순위: 장착 > 선택 > 일반
        /// </summary>
        private void RefreshTextCountColor()
        {
            if (!textCount) return;

            if (_isTextCountEquipped)
            {
                textCount.color = colorTextCountEquipped;
                return;
            }

            textCount.color = _isSelected ? colorTextCountSelected : colorTextCountNormal;
        }

        protected void ShowOverImage(bool show)
        {
            _uiWindowManager ??= SceneGame.Instance?.uIWindowManager;
            if (!_showOverImage || !_uiWindowManager) return;
            _uiWindowManager.ShowOverIconImage(show, gameObject.transform.position, _slotSize);
        }
        public virtual ItemConstants.PartsType GetPartsType() => GetItemPartsType();
        public virtual int GetStatusValue1() => 0;
        public virtual string GetStatusId1() => "";
        public virtual float GetDuration() => 0;

        public void SetPosition(Vector3 position)
        {
            transform.localPosition = position;
        }

        public virtual bool CheckRequireLevel() => false;

        private void SetLevel(int value)
        {
            _level = value;
        }

        private void SetIsLearn(bool value)
        {
            _isLearn = value;
        }
        public int GetLevel() => _level;
        public int GetCount() => count;

        public bool IsLearn() => _isLearn;
        public Sprite GetImageIconSprite() => ImageIcon.sprite;

        public virtual ConfigCommon.SuffixType GetStatusSuffix1() => ConfigCommon.SuffixType.None;
        public virtual void CheckStatusAffect() { }

        /// <summary>
        /// 쿨타임 시작하기
        /// </summary>
        /// <param name="coolTime"></param>
        /// <returns></returns>
        public bool PlayCoolTime(float coolTime)
        {
            float time = _iconCoolTimeManager.GetCurrentCoolTime(windowUid, uid);
            if (time > 0)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("Action_CannotUseDuringCooldown");//"쿨타임 중에는 사용할 수 없습니다."
                HandleInvalidEffect();
                return false;
            }
            
            bool started = _iconCoolTimeManager.StartHandler(windowUid, this, coolTime);
            if (!started)
            {
                HandleInvalidEffect();
            }
            return started;
        }
        /// <summary>
        /// Raycast Target 설정
        /// </summary>
        /// <param name="set"></param>
        public void SetRaycastTarget(bool set)
        {
            if (ImageIcon == null) return;
            ImageIcon.raycastTarget = set;
        }

        public virtual bool IsAntiFlag(ItemConstants.AntiFlag flag) => false;

        /// <summary>
        /// Regist 되었을때 부모 윈도우와 slot index 정보 셋팅하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        public void SetParentInfo(UIWindowConstants.WindowUid fromWindowUid, int fromIndex)
        {
            _parentWindowUid = fromWindowUid;
            _parentSlotIndex = fromIndex;
        }
        public (UIWindowConstants.WindowUid, int) GetParentInfo()
        {
            return (_parentWindowUid, _parentSlotIndex);
        }
        public virtual float GetCoolTime() => 0;

        /// <summary>
        /// lock 이미지를 사용안하도록 삭제처리 하기
        /// </summary>
        public void RemoveLockImage()
        {
            imageLock = null;
        }

        public void SetClick(bool set)
        {
            PossibleClick = set && !_isInactive;
        }

        public virtual int GetPartsSlotIndex() => -1;

        public void SetOriginalPosition(Vector3 position)
        {
            _dragHandler.SetOriginalPosition(position);
        }

        public void SetAlpha(float alpha)
        {
            if (!useCanvasGroup || _canvasGroup == null) return;
            _canvasGroup.alpha = alpha;
        }

        protected UIEffectTarget ResolveEffectTarget()
        {
            if (effectTarget == null)
            {
                effectTarget = UIEffectTarget.GetOrAdd(gameObject);
            }

            return effectTarget;
        }

        protected void HandlePointerEnterEffect(PointerEventData eventData)
        {
            PlayEffect(hoverPreset, UIEffectEventType.None);
        }

        protected void HandlePointerClickEffect(PointerEventData eventData)
        {
            PlayEffect(clickPreset, UIEffectEventType.None);
        }

        public virtual void HandleInvalidEffect()
        {
            PlayEffect(invalidPreset, UIEffectEventType.None);
        }

        public virtual void HandleCooldownStartedEffect()
        {
            PlayEffect(cooldownStartPreset, UIEffectEventType.None);
        }

        public virtual void HandleCooldownReadyEffect()
        {
            PlayEffect(cooldownReadyPreset, UIEffectEventType.CooldownCompleted);
        }

        public virtual void HandleEquipEffect()
        {
            PlayEffect(equipPreset, UIEffectEventType.None);
        }

        protected void PlayEffect(UIEffectPreset preset, UIEffectEventType eventType)
        {
            if (preset == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            UIEffectTarget target = ResolveEffectTarget();
            if (target == null)
            {
                return;
            }

            UIEffectService.Play(this, target, preset);
        }
    }
}
