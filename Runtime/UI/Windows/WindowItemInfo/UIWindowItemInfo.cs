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
        public enum PositionType
        {
            None,
            Left,
            Right,
            /// <summary>
            /// 배치한 위치에 고정
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

        private Dictionary<ItemConstants.Category, Action> _categoryUIHandlers;

        private TableItem _tableItem;
        private StruckTableItem _currentStruckTableItem;
        private long _currentInstanceId;
        private LocalizationManager _localizationManager;

        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.ItemInfo;
            if (TableLoaderManager.Instance == null) return;
            _tableItem = TableLoaderManager.Instance.TableItem;
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

        public void SetItemUid(int itemUid, long instanceId, GameObject icon, PositionType type, Vector2 iconSlotSize,
            Vector2? pivot = null, Vector3? position = null)
        {
            if (icon == null || itemUid <= 0) return;
            _currentStruckTableItem = _tableItem.GetDataByUid(itemUid);
            if (_currentStruckTableItem is not { Uid: > 0 }) return;

            _currentInstanceId = instanceId;

            SetSpriteIcon();
            RefreshTexts();
            SetCategoryUI();
            Show(true);

            Vector2 finalPivot = pivot ?? Vector2.zero;
            Vector3 finalPosition = position ?? Vector3.zero;
            SetPosition(icon, type, iconSlotSize, finalPivot, finalPosition);
        }

        /// <summary>
        /// 아이템 아이콘 이미지 설정
        /// </summary>
        private void SetSpriteIcon()
        {
            if (_currentStruckTableItem == null || !imageIcon) return;
            imageIcon.sprite = AddressableLoaderItem.Instance.GetImageIconItemByName(_currentStruckTableItem.FileName);
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

            string itemName = _currentStruckTableItem.Name;
            if (_localizationManager != null)
            {
                string localized = _localizationManager.GetItemNameByKey(_currentStruckTableItem.Uid.ToString());
                if (!string.IsNullOrWhiteSpace(localized))
                    itemName = localized;
            }

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
