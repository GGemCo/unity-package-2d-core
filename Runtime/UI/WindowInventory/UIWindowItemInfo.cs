using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
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
        private float valueStatus1;
        public TextMeshProUGUI textStatus2;
        private float valueStatus2;
        
        [Header("서브옵션")]
        public TextMeshProUGUI[] textOptions;
        public float[] valueOptions;
        
        private Dictionary<ItemConstants.Category, Action> categoryUIHandlers;
        
        private StruckTableItem currentStruckTableItem;
        private TableStatus tableStatus;
        
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.ItemInfo;
            if (TableLoaderManager.Instance == null) return;
            tableItem = TableLoaderManager.Instance.TableItem;
            tableStatus = TableLoaderManager.Instance.TableStatus;
            base.Awake();
            InitializeCategoryUIHandlers();
        }
        private void InitializeCategoryUIHandlers()
        {
            categoryUIHandlers = new Dictionary<ItemConstants.Category, Action>
            {
                { ItemConstants.Category.Weapon, SetWeaponUI },
                { ItemConstants.Category.Armor, SetArmorUI },
                { ItemConstants.Category.Potion, SetPotionUI },
            };
        }

        public void SetItemUid(int itemUid, GameObject icon, PositionType type, Vector2 iconSlotSize, Vector2? pivot = null, Vector3? position = null)
        {
            if (icon == null || itemUid <= 0) return;
            currentStruckTableItem = tableItem.GetDataByUid(itemUid);
            if (currentStruckTableItem is not { Uid: > 0 }) return;
            
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
            if (currentStruckTableItem == null) return;
            textSalePrice.text =
                $"상점 판매가: {CurrencyConstants.GetNameByCurrencyType(currentStruckTableItem.SaleCurrencyType)} {currentStruckTableItem.SaleCurrencyValue}";
        }

        private void SetDescription()
        {
            if (currentStruckTableItem == null) return;
            textDescription.text = $"{currentStruckTableItem.Description}";
        }

        private void SetAntiFlag()
        {
            if (currentStruckTableItem == null) return;
            string antiFlag = "";
            foreach (var t in currentStruckTableItem.AntiFlag)
            {
                if (antiFlag != "")
                {
                    antiFlag += ",";
                }
                antiFlag += ItemConstants.GetAntiFlagName(t);
            }

            if (antiFlag == "")
            {
                textAntiFlag.gameObject.SetActive(false);
                return;
            }

            textAntiFlag.gameObject.SetActive(true);
            textAntiFlag.text = $"{antiFlag}";
        }

        /// <summary>
        /// 이름 설정하기
        /// </summary>
        private void SetName()
        {
            if (currentStruckTableItem == null) return;
            textName.text = $"이름: {currentStruckTableItem.Name}";
        }
        /// <summary>
        /// 타입 설정하기
        /// </summary>
        private void SetType()
        {
            if (currentStruckTableItem == null) return;
            textType.text = $"타입: {currentStruckTableItem.Type}";
        }
        
        private void SetCategoryUI()
        {
            if (categoryUIHandlers.TryGetValue(currentStruckTableItem.Category, out var handler))
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
            if (currentStruckTableItem == null) return;
            textCategory.text = $"카테고리: {currentStruckTableItem.Category}";
            textSubCategory.text = $"서브카테고리: {currentStruckTableItem.SubCategory}";
        }
        private void SetStatusOptions()
        {
            SetTextMeshPro(textStatus1, currentStruckTableItem.StatusID1, currentStruckTableItem.StatusSuffix1, currentStruckTableItem.StatusValue1);
            SetTextMeshPro(textStatus2, currentStruckTableItem.StatusID2, currentStruckTableItem.StatusSuffix2, currentStruckTableItem.StatusValue2);

            string[] optionTypes = 
            {
                currentStruckTableItem.OptionType1, 
                currentStruckTableItem.OptionType2, 
                currentStruckTableItem.OptionType3, 
                currentStruckTableItem.OptionType4, 
                currentStruckTableItem.OptionType5
            };
            
            ConfigCommon.SuffixType[] optionSuffixes = 
            {
                currentStruckTableItem.OptionSuffix1, 
                currentStruckTableItem.OptionSuffix2, 
                currentStruckTableItem.OptionSuffix3, 
                currentStruckTableItem.OptionSuffix4, 
                currentStruckTableItem.OptionSuffix5
            };

            float[] optionValues = 
            {
                currentStruckTableItem.OptionValue1, 
                currentStruckTableItem.OptionValue2, 
                currentStruckTableItem.OptionValue3, 
                currentStruckTableItem.OptionValue4, 
                currentStruckTableItem.OptionValue5
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
                var info = TableLoaderManager.Instance.TableAffect.GetDataByUid((int)value);
                if (info == null)
                {
                    GcLogger.LogError("어펙트 테이블에 없는 어펙트 입니다. affect Uid: "+value);
                    return;
                }
                textMesh.gameObject.SetActive(true);
                textMesh.text = $"{info.Duration} 초 동안 {GetStatusName(info.StatusID)} {GetValueText(info.StatusSuffix, info.Value)} 가 발동합니다.";
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
            // string cleanedId = ItemConstants.StatusSuffixFormats.Aggregate(statusId, (current, suffix) => current.Replace(suffix.Key, ""));
            var info = tableStatus.GetDataById(statusId);
            return info?.Name ?? "";
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
            if (currentStruckTableItem.StatusID1 == ConfigCommon.StatusAffectId) return;
            
            textStatus1.gameObject.SetActive(true);
            textStatus1.text = $"회복량 : {currentStruckTableItem.StatusValue1}";
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