using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 정보 윈도우
    /// </summary>
    public class UIWindowItemInfo : UIWindow
    {
        /// <summary>
        /// 아이템 정보창을 표시할 위치 계산 방식입니다.
        /// </summary>
        public enum PositionType
        {
            None,
            Left,
            Right,

            /// <summary>
            /// 외부에서 전달한 위치에 고정합니다.
            /// </summary>
            Fixed
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("기본정보")]
        [Tooltip("아이템 아이콘 이미지")]
        [SerializeField] private Image imageIcon;
        [Tooltip("아이템 이름")]
        [SerializeField] private TextMeshProUGUI textName;
        [Tooltip("아이템 타입")]
        [SerializeField] private TextMeshProUGUI textType;
        [Tooltip("아이템 카테고리")]
        [SerializeField] private TextMeshProUGUI textCategory;
        [Tooltip("아이템 서브카테고리")]
        [SerializeField] private TextMeshProUGUI textSubCategory;
        [Tooltip("아이템 Anti Flag")]
        [SerializeField] private TextMeshProUGUI textAntiFlag;

        [Header("옵션(신규)")]
        [Tooltip("고정(Base) 옵션 텍스트")]
        [SerializeField] private TextMeshProUGUI textBaseOption;
        [Tooltip("랜덤(Random) 옵션 텍스트")]
        [SerializeField] private TextMeshProUGUI textRandomOption;

        [Tooltip("아이템 설명")]
        [SerializeField] private TextMeshProUGUI textDescription;

        [Tooltip("아이템 판매가")]
        [SerializeField] private TextMeshProUGUI textSalePrice;

        [Header("스킬 아이템일 경우")]
        [Tooltip("스킬 Need Mp를 보여줄 이미지 오브젝트")]
        [SerializeField] private Image imageSkillMp;
        [Tooltip("스킬 Need Mp를 보여줄 스프라이트")]
        [SerializeField] private Sprite[] spriteSkillMp;
        [Tooltip("선택된 아이템이 없을 때, 투명한 이미지로 보여주기")]
        [SerializeField] private Sprite spriteBlank;

        private Dictionary<ItemConstants.Category, Action> _categoryUIHandlers;

        private TableItem _tableItem;
        private TableItemUse _tableItemUse;
        private TableItemUseAction _tableItemUseAction;
        private StruckTableItem _currentStruckTableItem;
        private long _currentInstanceId;
        private LocalizationManager _localizationManager;
        // 아이템 정보창 아이콘 이미지를 Addressables Sprite로 바인딩하는 컴포넌트
        private UIAddressableIconBinder _iconImageBinder;

        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.ItemInfo;
            if (TableLoaderManager.Instance == null) return;
            _tableItem = TableLoaderManager.Instance.TableItem;
            _tableItemUse = TableLoaderManager.Instance.TableItemUse;
            _tableItemUseAction = TableLoaderManager.Instance.TableItemUseAction;
            base.Awake();
            InitializeCategoryUIHandlers();
        }

        protected override void Start()
        {
            base.Start();
            BindLocalizationManager();
        }

        protected void OnEnable()
        {
            BindLocalizationManager();
        }

        protected void OnDisable()
        {
            if (_localizationManager == null) return;
            _localizationManager.OnChangeLocale -= HandleLocaleChanged;
        }

        private void InitializeCategoryUIHandlers()
        {
            _categoryUIHandlers = new Dictionary<ItemConstants.Category, Action>
            {
                { ItemConstants.Category.Weapon, SetWeaponUI },
                { ItemConstants.Category.Armor, SetArmorUI },
                { ItemConstants.Category.Potion, SetPotionUI },
            };
        }

        /// <summary>
        /// 개별 파라미터로 전달된 아이템 정보를 현재 정보창에 표시합니다.
        /// </summary>
        /// <param name="itemUid">표시할 아이템 Uid 입니다.</param>
        /// <param name="instanceId">표시할 아이템 인스턴스 Id 입니다.</param>
        /// <param name="icon">정보창 위치 계산의 기준이 되는 아이콘 오브젝트입니다.</param>
        /// <param name="type">정보창 위치 계산 방식입니다.</param>
        /// <param name="iconSlotSize">기준 아이콘 슬롯 크기입니다.</param>
        /// <param name="pivot">고정 위치 계산에 사용할 피벗 값입니다.</param>
        /// <param name="position">고정 위치 계산에 사용할 위치 값입니다.</param>
        public void SetItemUid(int itemUid, long instanceId, GameObject icon, PositionType type, Vector2 iconSlotSize,
            Vector2? pivot = null, Vector3? position = null)
        {
            SetItemInfo(new UIWindowItemInfoRequest
            {
                ItemUid = itemUid,
                InstanceId = instanceId,
                AnchorObject = icon,
                PositionType = type,
                IconSlotSize = iconSlotSize,
                Pivot = pivot,
                Position = position,
            });
        }

        /// <summary>
        /// 요청 객체로 전달된 아이템 정보를 현재 정보창에 표시합니다.
        /// </summary>
        /// <param name="request">아이템 정보창 표시에 필요한 문맥 정보입니다.</param>
        public void SetItemInfo(UIWindowItemInfoRequest request)
        {
            if (!TryBindItemInfo(request))
            {
                return;
            }

            SetSpriteIcon();
            RefreshTexts();
            SetCategoryUI();
            SetImageSkillMp();
            Show(true);

            Vector2 finalPivot = request.Pivot ?? Vector2.zero;
            Vector3 finalPosition = request.Position ?? Vector3.zero;
            SetPosition(request.AnchorObject, request.PositionType, request.IconSlotSize, finalPivot, finalPosition);
        }

        /// <summary>
        /// 현재 아이템 정보와 화면에 표시된 동적 내용을 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 아이콘의 비동기 바인딩 요청을 먼저 무효화하여, 초기화 이후 이전 아이템 Sprite가 뒤늦게 적용되지 않도록 합니다.
        /// 정보창의 표시 상태는 변경하지 않으므로 후보 아이템이 없는 선택 화면에서도 빈 정보창 레이아웃을 유지할 수 있습니다.
        /// </remarks>
        public void ClearItemInfo()
        {
            _currentStruckTableItem = null;
            _currentInstanceId = 0;

            UIAddressableIconBinder binder = ResolveIconImageBinder();
            binder?.Clear();
            if (imageIcon != null)
            {
                // 바인더의 fallback Sprite 설정과 관계없이 아이템 정보 영역은 완전히 비웁니다.
                imageIcon.sprite = spriteBlank;
            }

            ClearText(textName);
            ClearText(textType);
            ClearText(textCategory);
            ClearText(textSubCategory);
            ClearText(textAntiFlag);
            ClearText(textBaseOption);
            ClearText(textRandomOption);
            ClearText(textDescription);
            ClearText(textSalePrice);

            SetOptionalTextVisible(textSubCategory, false);
            SetOptionalTextVisible(textAntiFlag, false);
            SetOptionalTextVisible(textBaseOption, false);
            SetOptionalTextVisible(textRandomOption, false);
            ResetImageSkillMp();
        }

        /// <summary>
        /// 지정한 텍스트 UI의 내용을 빈 문자열로 초기화합니다.
        /// </summary>
        /// <param name="text">초기화할 텍스트 UI입니다.</param>
        private static void ClearText(TextMeshProUGUI text)
        {
            if (text != null)
            {
                text.text = string.Empty;
            }
        }

        /// <summary>
        /// 아이템 데이터가 있을 때만 사용하는 선택형 텍스트 오브젝트의 표시 상태를 변경합니다.
        /// </summary>
        /// <param name="text">표시 상태를 변경할 텍스트 UI입니다.</param>
        /// <param name="show">오브젝트를 표시하면 <see langword="true"/>입니다.</param>
        private static void SetOptionalTextVisible(TextMeshProUGUI text, bool show)
        {
            if (text != null)
            {
                text.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// 요청 객체가 유효한지 확인하고 현재 표시 대상 아이템 상태를 갱신합니다.
        /// </summary>
        /// <param name="request">아이템 정보창 표시에 필요한 문맥 정보입니다.</param>
        /// <returns>표시 가능한 아이템 정보가 준비되면 true, 아니면 false 입니다.</returns>
        private bool TryBindItemInfo(UIWindowItemInfoRequest request)
        {
            if (request == null || request.AnchorObject == null || request.ItemUid <= 0)
            {
                return false;
            }

            _currentStruckTableItem = _tableItem.GetDataByUid(request.ItemUid);
            if (_currentStruckTableItem is not { Uid: > 0 })
            {
                return false;
            }

            _currentInstanceId = request.InstanceId;
            return true;
        }

        /// <summary>
        /// 아이템 정보창의 아이콘 이미지를 Addressables 아이콘 바인더로 설정합니다.
        /// </summary>
        private void SetSpriteIcon()
        {
            UIAddressableIconBinder binder = ResolveIconImageBinder();
            if (_currentStruckTableItem == null || binder == null)
            {
                binder?.Clear();
                return;
            }

            AddressableIconSpriteRequest request =
                new AddressableIconSpriteRequest(AddressableIconAtlasType.ItemIcon, _currentStruckTableItem.FileName);

            // 캐시가 준비된 경우 즉시 반영되고, 아직 로딩 전이면 완료 시점에 현재 정보창 이미지가 갱신됩니다.
            binder.Bind(request);
        }

        /// <summary>
        /// 아이템 정보창 아이콘 이미지에 사용할 Addressables 바인더를 반환합니다.
        /// 프리팹에 명시적으로 연결되지 않은 경우 아이콘 Image 오브젝트에서 찾아 자동으로 추가합니다.
        /// </summary>
        /// <returns>사용 가능한 아이콘 Sprite 바인더입니다.</returns>
        private UIAddressableIconBinder ResolveIconImageBinder()
        {
            if (_iconImageBinder != null)
            {
                return _iconImageBinder;
            }

            if (imageIcon == null)
            {
                return null;
            }

            _iconImageBinder = imageIcon.GetComponent<UIAddressableIconBinder>();
            if (_iconImageBinder == null)
            {
                _iconImageBinder = imageIcon.gameObject.AddComponent<UIAddressableIconBinder>();
            }

            return _iconImageBinder;
        }

        private void SetSalePrice()
        {
            if (_currentStruckTableItem == null || !textSalePrice) return;
            if (_currentStruckTableItem.SaleCurrencyValue <= 0) return;

            string salePriceText =
                $"{CurrencyConstants.GetNameByCurrencyType(_currentStruckTableItem.SaleCurrencyType)} {_currentStruckTableItem.SaleCurrencyValue}";

            if (_localizationManager == null)
            {
                textSalePrice.text = salePriceText;
                return;
            }

            textSalePrice.text = string.Format(
                GetFormatOrDefault(_localizationManager.GetUIWindowItemInfoByKey("Text_SellPrice")),
                salePriceText);
        }

        private void SetDescriptionText()
        {
            if (_currentStruckTableItem == null || !textDescription) return;

            var loc = _localizationManager;
            if (loc == null)
            {
                textDescription.text = _currentStruckTableItem.Description;
                return;
            }

            var args = new ItemDescriptionSmartArgs(_currentStruckTableItem, loc, string.Empty);
            string smart = loc.GetItemDescriptionSmartByKey(_currentStruckTableItem.Uid.ToString(), args);
            if (string.IsNullOrWhiteSpace(smart))
            {
                textDescription.text = _currentStruckTableItem.Description;
                return;
            }

            textDescription.text = smart;
        }

        private void SetSeparatedOptionTexts()
        {
            if (_currentStruckTableItem == null) return;
            var loc = _localizationManager;

            string baseText = ItemOptionTextBuilder.BuildBaseOptionsText(_currentStruckTableItem.Uid, loc);
            string randomText = ItemOptionTextBuilder.BuildRandomOptionsText(_currentInstanceId, loc);

            if (textBaseOption != null)
            {
                bool has = !string.IsNullOrWhiteSpace(baseText);
                textBaseOption.gameObject.SetActive(has);
                textBaseOption.text = has ? baseText : string.Empty;
            }

            if (textRandomOption != null)
            {
                bool has = !string.IsNullOrWhiteSpace(randomText);
                textRandomOption.gameObject.SetActive(has);
                textRandomOption.text = has ? randomText : string.Empty;
            }
        }

        /// <summary>
        /// Anti Flag
        /// </summary>
        private void SetAntiFlag()
        {
            if (_currentStruckTableItem == null || !textAntiFlag) return;

            string antiFlagText = _localizationManager != null
                ? _localizationManager.GetItemAntiFlagNames(_currentStruckTableItem.AntiFlag)
                : _currentStruckTableItem.AntiFlagText;

            if (string.IsNullOrWhiteSpace(antiFlagText))
            {
                textAntiFlag.gameObject.SetActive(false);
                return;
            }

            textAntiFlag.gameObject.SetActive(true);
            textAntiFlag.text = string.Format(
                GetFormatOrDefault(_localizationManager?.GetUIWindowItemInfoByKey("Text_AntiFlag")),
                antiFlagText);
        }

        /// <summary>
        /// 이름 설정하기
        /// </summary>
        private void SetName()
        {
            if (_currentStruckTableItem == null || !textName) return;

            string itemName = ItemDisplayNameUtility.GetDisplayName(_currentStruckTableItem, _localizationManager);

            textName.text = string.Format(
                GetFormatOrDefault(_localizationManager?.GetUIWindowItemInfoByKey("Text_Name")),
                itemName);
        }

        /// <summary>
        /// 타입 설정하기
        /// </summary>
        private void SetType()
        {
            if (_currentStruckTableItem == null || !textType) return;

            string typeName = _localizationManager != null
                ? _localizationManager.GetItemTypeName(_currentStruckTableItem.Type)
                : _currentStruckTableItem.Type.ToString();

            textType.text = string.Format(
                GetFormatOrDefault(_localizationManager?.GetUIWindowItemInfoByKey("Text_Type")),
                typeName);
        }

        private void SetCategoryUI()
        {
            if (_categoryUIHandlers.TryGetValue(_currentStruckTableItem.Category, out var handler))
            {
                handler?.Invoke();
            }
            else
            {
                SetDefaultUI();
            }
        }

        /// <summary>
        /// 현재 아이템이 액티브 스킬 지급 아이템이면 스킬 NeedMp 값에 맞는 이미지를 표시합니다.
        /// </summary>
        private void SetImageSkillMp()
        {
            ResetImageSkillMp();
            if (!imageSkillMp || _currentStruckTableItem == null)
                return;

            if (!TryResolveSkillNeedMp(_currentStruckTableItem.Uid, out int needMp))
                return;

            if (!TryGetSkillMpSprite(needMp, out Sprite sprite))
                return;

            imageSkillMp.sprite = sprite;
            imageSkillMp.gameObject.SetActive(true);
        }

        /// <summary>
        /// 스킬 MP 표시 이미지를 숨기고 스프라이트 참조를 초기화합니다.
        /// </summary>
        private void ResetImageSkillMp()
        {
            if (!imageSkillMp)
                return;

            imageSkillMp.sprite = spriteSkillMp[0];
        }

        /// <summary>
        /// 아이템 사용 테이블 연결을 따라 액티브 스킬 UID를 찾고, Skill Bridge에서 NeedMp 값을 조회합니다.
        /// </summary>
        /// <param name="itemUid">현재 표시 중인 아이템 UID입니다.</param>
        /// <param name="needMp">조회된 스킬 NeedMp 값입니다.</param>
        /// <returns>스킬 NeedMp 값을 찾았으면 <see langword="true"/>입니다.</returns>
        private bool TryResolveSkillNeedMp(int itemUid, out int needMp)
        {
            needMp = 0;
            if (!TryResolveGrantSkillUid(itemUid, out int skillUid))
                return false;

            return SkillBridge.NeedMpProvider.TryGetNeedMp(skillUid, out needMp);
        }

        /// <summary>
        /// item_use와 item_use_action 테이블에서 현재 아이템이 지급하는 액티브 스킬 UID를 조회합니다.
        /// </summary>
        /// <param name="itemUid">조회할 아이템 UID입니다.</param>
        /// <param name="skillUid">조회된 액티브 스킬 UID입니다.</param>
        /// <returns>액티브 스킬 지급 액션을 찾았으면 <see langword="true"/>입니다.</returns>
        private bool TryResolveGrantSkillUid(int itemUid, out int skillUid)
        {
            skillUid = 0;
            if (_tableItemUse == null || _tableItemUseAction == null)
                return false;

            if (!_tableItemUse.TryGetByItemUid(itemUid, out StruckTableItemUse itemUseInfo) || itemUseInfo == null)
                return false;

            IReadOnlyList<StruckTableItemUseAction> actions = _tableItemUseAction.GetActions(itemUseInfo.Uid);
            for (int i = 0; i < actions.Count; i++)
            {
                StruckTableItemUseAction action = actions[i];
                if (action == null || action.ActionType != ItemUseActionType.GrantSkill)
                    continue;

                skillUid = action.ParamIntA;
                return skillUid > 0;
            }

            return false;
        }

        /// <summary>
        /// NeedMp 값을 spriteSkillMp 배열 인덱스로 사용해 표시할 스프라이트를 조회합니다.
        /// </summary>
        /// <param name="needMp">스킬 사용에 필요한 MP 값입니다.</param>
        /// <param name="sprite">표시할 스프라이트입니다.</param>
        /// <returns>표시 가능한 스프라이트가 있으면 <see langword="true"/>입니다.</returns>
        private bool TryGetSkillMpSprite(int needMp, out Sprite sprite)
        {
            sprite = null;
            if (needMp <= 0 || spriteSkillMp == null || needMp >= spriteSkillMp.Length)
                return false;

            sprite = spriteSkillMp[needMp];
            return sprite != null;
        }

        /// <summary>
        /// 카테고리, 서브 카테고리 설정하기
        /// </summary>
        private void SetCategory()
        {
            if (_currentStruckTableItem == null) return;

            if (textCategory)
            {
                string categoryName = _localizationManager != null
                    ? _localizationManager.GetItemCategoryName(_currentStruckTableItem.Category)
                    : _currentStruckTableItem.Category.ToString();

                textCategory.text = string.Format(
                    GetFormatOrDefault(_localizationManager?.GetUIWindowItemInfoByKey("Text_Category")),
                    categoryName);
            }

            if (textSubCategory)
            {
                bool hasSubCategory = _currentStruckTableItem.SubCategory != ItemConstants.SubCategory.None;
                textSubCategory.gameObject.SetActive(hasSubCategory);

                if (hasSubCategory)
                {
                    string subCategoryName = _localizationManager != null
                        ? _localizationManager.GetItemSubCategoryName(_currentStruckTableItem.SubCategory)
                        : _currentStruckTableItem.SubCategory.ToString();

                    textSubCategory.text = string.Format(
                        GetFormatOrDefault(_localizationManager?.GetUIWindowItemInfoByKey("Text_SubCategory")),
                        subCategoryName);
                }
            }
        }

        private void RefreshTexts()
        {
            SetName();
            SetType();
            SetAntiFlag();
            SetCategory();
            SetSeparatedOptionTexts();
            SetDescriptionText();
            SetSalePrice();
        }

        private void HandleLocaleChanged(string _, int __)
        {
            if (_currentStruckTableItem is not { Uid: > 0 }) return;
            RefreshTexts();
        }

        private void BindLocalizationManager()
        {
            LocalizationManager manager = LocalizationManager.Instance;
            if (manager == null) return;

            if (_localizationManager != null && _localizationManager != manager)
                _localizationManager.OnChangeLocale -= HandleLocaleChanged;

            _localizationManager = manager;
            _localizationManager.OnChangeLocale -= HandleLocaleChanged;
            _localizationManager.OnChangeLocale += HandleLocaleChanged;
        }

        private static string GetFormatOrDefault(string format)
        {
            return string.IsNullOrWhiteSpace(format) ? "{0}" : format;
        }

        private void SetWeaponUI()
        {
        }

        private void SetArmorUI()
        {
        }

        private void SetPotionUI()
        {
        }

        private void SetDefaultUI()
        {
        }

        /// <summary>
        /// 위치 보정하기
        /// </summary>
        private void SetPosition(GameObject icon, PositionType type, Vector2 iconSlotSize, Vector2 pivot, Vector2 position)
        {
            RectTransform itemInfoRect = GetComponent<RectTransform>();
            if (type == PositionType.Left)
            {
                itemInfoRect.pivot = new Vector2(0, 1f);
                transform.position = new Vector3(
                    icon.transform.position.x + iconSlotSize.x / 2f,
                    icon.transform.position.y + iconSlotSize.y / 2f);
            }
            else if (type == PositionType.Right)
            {
                itemInfoRect.pivot = new Vector2(1f, 1f);
                transform.position = new Vector2(
                    icon.transform.position.x - iconSlotSize.x / 2f,
                    icon.transform.position.y + iconSlotSize.y / 2f);
            }
            else if (type == PositionType.Fixed)
            {
            }
            else
            {
                itemInfoRect.pivot = pivot;
                transform.position = position;
            }

            StartCoroutine(DelayClampToScreen(itemInfoRect));
        }

        /// <summary>
        /// 위치 보정 코루틴
        /// </summary>
        private IEnumerator DelayClampToScreen(RectTransform rectTransform)
        {
            yield return null;
            MathHelper.ClampToScreen(rectTransform);
        }
    }
}
