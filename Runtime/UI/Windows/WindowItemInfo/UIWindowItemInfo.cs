using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
        }
        private TableItem tableItem;
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("기본정보")]
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textName;
        [Tooltip("아이템 타입")]
        public TextMeshProUGUI textType;
        [Tooltip("아이템 카테고리")]
        public TextMeshProUGUI textCategory;
        [Tooltip("아이템 서브카테고리")]
        public TextMeshProUGUI textSubCategory;
        [Tooltip("아이템 Anti Flag")]
        public TextMeshProUGUI textAntiFlag;

        [Header("옵션(신규)")]
        [Tooltip("고정(Base) 옵션 텍스트")]
        public TextMeshProUGUI textBaseOption;
        [Tooltip("랜덤(Random) 옵션 텍스트")]
        public TextMeshProUGUI textRandomOption;
        
        [Tooltip("아이템 설명")]
        public TextMeshProUGUI textDescription;
        
        [Tooltip("아이템 판매가")]
        public TextMeshProUGUI textSalePrice;
        
        private Dictionary<ItemConstants.Category, Action> _categoryUIHandlers;
        
        private StruckTableItem _currentStruckTableItem;
        private long _currentInstanceId;
        private LocalizationManager _localizationManager;
        
        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.ItemInfo;
            if (TableLoaderManager.Instance == null) return;
            tableItem = TableLoaderManager.Instance.TableItem;
            base.Awake();
            InitializeCategoryUIHandlers();
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

        protected override void Start()
        {
            base.Start();
            _localizationManager = LocalizationManager.Instance;
        }

        public void SetItemUid(int itemUid, long instanceId, GameObject icon, PositionType type, Vector2 iconSlotSize,
            Vector2? pivot = null, Vector3? position = null)
        {
            if (icon == null || itemUid <= 0) return;
            _currentStruckTableItem = tableItem.GetDataByUid(itemUid);
            if (_currentStruckTableItem is not { Uid: > 0 }) return;

            _currentInstanceId = instanceId;
            
            SetName();
            SetType();
            SetAntiFlag();
            SetCategory();
            SetSeparatedOptionTexts();
            SetDescriptionText();
            SetSalePrice();
            SetCategoryUI();
            Show(true);
            // active 된 후 위치 조정한다.
            
            // null 체크 후 기본값 대입 (예: pivot이 null이면 Vector2.zero 사용)
            Vector2 finalPivot = pivot ?? Vector2.zero;
            Vector3 finalPosition = position ?? Vector3.zero;
            SetPosition(icon, type, iconSlotSize, finalPivot, finalPosition);
        }

        private void SetSalePrice()
        {
            if (_currentStruckTableItem == null) return;
            if (_currentStruckTableItem.SaleCurrencyValue <= 0) return;
            textSalePrice.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_SellPrice"), $"{CurrencyConstants.GetNameByCurrencyType(_currentStruckTableItem.SaleCurrencyType)} {_currentStruckTableItem.SaleCurrencyValue}");
        }

        private void SetDescriptionText()
        {
            if (_currentStruckTableItem == null) return;
            // ItemDescription(=GGemCo_Item_Description)는 "아이템 서술/설명" 전용으로 사용한다.
            // 옵션 텍스트(Base/Random)는 별도 UI(TextBaseOption/TextRandomOption)에 바인딩한다.
            var loc = _localizationManager;
            if (loc == null)
            {
                textDescription.text = _currentStruckTableItem.Description;
                return;
            }

            // 기존 Smart String 인자 구조와의 호환을 위해 Options는 빈 문자열로 전달한다.
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
        /// TableItem 에서 미리 파싱 처리한다.
        /// </summary>
        private void SetAntiFlag()
        {
            if (_currentStruckTableItem == null) return;
            if (string.IsNullOrEmpty(_currentStruckTableItem.AntiFlagText))
            {
                textAntiFlag.gameObject.SetActive(false);
                return;
            }

            textAntiFlag.gameObject.SetActive(true);
            textAntiFlag.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_AntiFlag"), _currentStruckTableItem.AntiFlagText);
        }

        /// <summary>
        /// 이름 설정하기
        /// </summary>
        private void SetName()
        {
            if (_currentStruckTableItem == null) return;
            textName.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_Name"),
                _localizationManager.GetItemNameByKey(_currentStruckTableItem.Uid.ToString()));
        }
        /// <summary>
        /// 타입 설정하기
        /// </summary>
        private void SetType()
        {
            if (_currentStruckTableItem == null) return;
            textType.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_Type"), _currentStruckTableItem.Type);
        }
        
        private void SetCategoryUI()
        {
            if (_categoryUIHandlers.TryGetValue(_currentStruckTableItem.Category, out var handler))
            {
                handler?.Invoke();
            }
            else
            {
                SetDefaultUI(); // 기본 UI 설정
            }
        }

        /// <summary>
        /// 카테고리, 서브 카테고리 설정하기
        /// </summary>
        private void SetCategory()
        {
            if (_currentStruckTableItem == null) return;
            textCategory.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_Category"), _currentStruckTableItem.Category);
            textSubCategory.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_SubCategory"), _currentStruckTableItem.SubCategory);
        }
        // 카테고리별 UI 설정 함수
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
        /// <param name="icon"></param>
        /// <param name="type"></param>
        /// <param name="iconSlotSize"></param>
        /// <param name="pivot"></param>
        /// <param name="position"></param>
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
            else
            {
                itemInfoRect.pivot = pivot;
                transform.position = position;
            }

            // 화면 밖 체크 & 보정
            StartCoroutine(DelayClampToScreen(itemInfoRect));
        }
        /// <summary>
        /// 위치 보정 코루틴
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        private IEnumerator DelayClampToScreen(RectTransform rectTransform)
        {
            yield return null; // 한 프레임 대기
            MathHelper.ClampToScreen(rectTransform);
        }
    }
}