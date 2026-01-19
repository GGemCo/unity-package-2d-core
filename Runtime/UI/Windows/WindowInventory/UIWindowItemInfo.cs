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
        [Tooltip("아이템 설명")]
        public TextMeshProUGUI textDescription;
        [Tooltip("아이템 판매가")]
        public TextMeshProUGUI textSalePrice;
        
        [Header("메인옵션")]
        [Tooltip("옵션 이름")]
        public TextMeshProUGUI textStatus1;
        private float _valueStatus1;
        public TextMeshProUGUI textStatus2;
        private float _valueStatus2;
        
        [Header("서브옵션")]
        public TextMeshProUGUI[] textOptions;
        [HideInInspector] public float[] valueOptions;
        
        private Dictionary<ItemConstants.Category, Action> _categoryUIHandlers;
        
        private StruckTableItem _currentStruckTableItem;
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

        public void SetItemUid(int itemUid, GameObject icon, PositionType type, Vector2 iconSlotSize, Vector2? pivot = null, Vector3? position = null)
        {
            if (icon == null || itemUid <= 0) return;
            _currentStruckTableItem = tableItem.GetDataByUid(itemUid);
            if (_currentStruckTableItem is not { Uid: > 0 }) return;
            
            SetName();
            SetType();
            SetAntiFlag();
            SetCategory();
            SetDescription();
            SetSalePrice();
            SetStatusOptions();
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

        private void SetDescription()
        {
            if (_currentStruckTableItem == null) return;
            textDescription.text = $"{_currentStruckTableItem.Description}";
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
        private void SetStatusOptions()
        {
            SetTextMeshPro(textStatus1, _currentStruckTableItem.StatusID1, _currentStruckTableItem.StatusSuffix1, _currentStruckTableItem.StatusValue1);
            SetTextMeshPro(textStatus2, _currentStruckTableItem.StatusID2, _currentStruckTableItem.StatusSuffix2, _currentStruckTableItem.StatusValue2);

            string[] optionTypes = 
            {
                _currentStruckTableItem.OptionType1, 
                _currentStruckTableItem.OptionType2, 
                _currentStruckTableItem.OptionType3, 
                _currentStruckTableItem.OptionType4, 
                _currentStruckTableItem.OptionType5
            };
            
            ConfigCommon.SuffixType[] optionSuffixes = 
            {
                _currentStruckTableItem.OptionSuffix1, 
                _currentStruckTableItem.OptionSuffix2, 
                _currentStruckTableItem.OptionSuffix3, 
                _currentStruckTableItem.OptionSuffix4, 
                _currentStruckTableItem.OptionSuffix5
            };

            float[] optionValues = 
            {
                _currentStruckTableItem.OptionValue1, 
                _currentStruckTableItem.OptionValue2, 
                _currentStruckTableItem.OptionValue3, 
                _currentStruckTableItem.OptionValue4, 
                _currentStruckTableItem.OptionValue5
            };

            for (int i = 0; i < textOptions.Length; i++)
            {
                SetTextMeshPro(textOptions[i], optionTypes[i], optionSuffixes[i], optionValues[i]);
                valueOptions[i] = optionValues[i];
            }
        }

        private void SetTextMeshPro(TextMeshProUGUI textMesh, string statusId, ConfigCommon.SuffixType suffixType, float value)
        {
            textMesh.gameObject.SetActive(false);
            if (string.IsNullOrEmpty(statusId)) return;
            if (statusId == ConfigCommon.StatusAffectId)
            {
                // value에는 AffectUid가 들어옵니다. (float -> int 변환)
                int affectUid = Mathf.RoundToInt(value);
                if (affectUid <= 0) return;

                string desc = AffectBridge.DescriptionProvider.GetDescription(affectUid);
                if (string.IsNullOrWhiteSpace(desc)) return;

                textMesh.gameObject.SetActive(true);
                textMesh.text = desc; // 여러 줄 설명 그대로 출력
            }
            else
            {
                string statusName = GetStatusName(statusId);
                if (string.IsNullOrEmpty(statusName))
                {
                    return;
                }

                string valueText = GetValueText(suffixType, value);
                textMesh.gameObject.SetActive(true);
                textMesh.text = $"{statusName}: {valueText}";
            }
        }

        private string GetValueText(ConfigCommon.SuffixType suffixType, float value)
        {
            string valueText = $"{value}";
            foreach (var suffix in ItemConstants.StatusSuffixFormats.Keys)
            {
                if (suffixType == suffix)
                {
                    valueText = string.Format(ItemConstants.StatusSuffixFormats[suffix], value);
                    break; // 첫 번째로 매칭된 값만 적용
                }
            }

            return valueText;
        }

        private string GetStatusName(string statusId)
        {
            if (string.IsNullOrEmpty(statusId)) return "";

            // 테이블 로딩이 완료된 상태라면, Stat/DamageType/State 중 어디에 속하는지 우선 조회합니다.
            var tlm = TableLoaderManager.Instance;
            if (tlm != null)
            {
                var stat = tlm.TableStat?.GetDataById(statusId);
                if (stat != null && !string.IsNullOrEmpty(stat.Name))
                    return stat.Name;

                var damage = tlm.TableDamageType?.GetDataById(statusId);
                if (damage != null && !string.IsNullOrEmpty(damage.Name))
                    return damage.Name;

                var state = tlm.TableState?.GetDataById(statusId);
                if (state != null && !string.IsNullOrEmpty(state.Name))
                    return state.Name;
            }

            // 마지막 fallback: StatusName 테이블 직접 조회
            return _localizationManager != null
                ? _localizationManager.GetStatusNameByKey(statusId)
                : statusId;
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
            // statusID1 에 affect_uid 일 경우는 예외처리
            if (_currentStruckTableItem.StatusID1 == ConfigCommon.StatusAffectId) return;
            
            textStatus1.gameObject.SetActive(true);
            // textStatus1.text = $"Recovery: {currentStruckTableItem.StatusValue1}"; // 회복량
            textStatus1.text = string.Format(_localizationManager.GetUIWindowItemInfoByKey("Text_Recovery"), _currentStruckTableItem.StatusValue1);
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