using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 아이콘
    /// </summary>
    public class UIIconItem : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("마우스 이벤트 On/Off")]
        [Tooltip("마우스 오버 시 정보창 표시 여부")]
        [SerializeField] private bool usePointerEnterEvent = true;

        [Tooltip("마우스 아웃 시 정보창 숨김 여부")]
        [SerializeField] private bool usePointerExitEvent = true;

        [Tooltip("마우스 클릭 시 정보창 표시 여부")]
        [SerializeField] private bool usePointerClickEvent = false;
        [Tooltip("아이템이 현재 선택 문맥에서 장착 중임을 표시할 이미지")]
        [SerializeField] private Image imageEquipped;
        [Tooltip("아이템이 현재 선택 문맥에서 장착 중임을 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI textEquipped;
        
        private StruckTableItem _struckTableItem;
        private TableItem _tableItem;
        private Player _player;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            IconType = IconConstants.Type.Item;
            _tableItem ??= TableLoaderManager.Instance.TableItem;
            SetEquippedState(false);
        }

        /// <summary>
        /// Shows whether this inventory icon is already used by the current selection context.
        /// This is only a visual badge and does not lock drag or click behavior.
        /// </summary>
        public void SetEquippedState(bool equipped)
        {
            if (imageEquipped != null)
            {
                imageEquipped.gameObject.SetActive(equipped);
            }

            if (textEquipped != null)
            {
                textEquipped.gameObject.SetActive(equipped);
            }

            // 장착 표식과 개수 텍스트 색상을 같은 장착 상태로 맞춥니다.
            SetTextCountEquippedState(equipped);
        }

        /// <summary>
        /// Clears the visual equipped badge when the item slot becomes empty.
        /// </summary>
        public override void ClearIconInfos()
        {
            base.ClearIconInfos();
            SetEquippedState(false);
        }

        /// <summary>
        /// 다른 uid 로 변경하기
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="iconIsLearn"></param>
        /// <param name="remainCoolTime"></param>
        /// <param name="iconInstanceId"></param>
        /// <param name="iconType"></param>
        public override bool ChangeInfoByUid(int iconUid, int iconCount = 0, int iconLevel = 0,
            bool iconIsLearn = false, int remainCoolTime = 0, long iconInstanceId = 0,
            IconConstants.Type iconType = IconConstants.Type.None)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime, iconInstanceId,
                    iconType)) return false;
            var info = _tableItem.GetDataByUid(iconUid);
            if (info == null)
            {
                GcLogger.LogError("아이콘 테이블에 없는 아이템 입니다.");
                return false;
            }

            _struckTableItem = info;

            UpdateInfo();
            return true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!usePointerEnterEvent) return;
            // GcLogger.Log("OnPointerEnter "+eventData);
            
            window.ShowItemInfo(true, this);
            ShowOverImage(true);
            HandlePointerEnterEffect(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!usePointerExitEvent) return;
            // GcLogger.Log("OnPointerExit "+eventData);
            window.ShowItemInfo(false);
            ShowOverImage(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (usePointerClickEvent)
            {
                window.ShowItemInfo(true, this);
                // 직접 선택 상태를 바꾸지 않고, 부모 윈도우를 통해 단일 선택 규칙을 적용합니다.
                if (window)
                    window.SetSelectedIcon(index);
                HandlePointerEnterEffect(eventData);
            }
            if (!PossibleClick) return;
            if (IsLock()) return;
            if(eventData.button == PointerEventData.InputButton.Left)
            {
                if (!window) return;
                window.SetSelectedIcon(index);
                HandlePointerClickEffect(eventData);
            }
            else if(eventData.button == PointerEventData.InputButton.Middle)
            {
            }
            else if(eventData.button == PointerEventData.InputButton.Right)
            {
                if (uid <= 0 || GetCount() <= 0) return;
                window.OnRightClick(this);
            }
        }
        /// <summary>
        /// 아이콘 이미지 경로 가져오기 
        /// </summary>
        /// <returns></returns>
        protected override string GetIconImagePath()
        {
            return _struckTableItem?.FileName;
        }
        /// <summary>
        /// 장착 가능한 타입 인지 체크 
        /// </summary>
        /// <returns></returns>
        public bool IsTypeEquip()
        {
            return _struckTableItem.Type == ItemConstants.Type.Equip;
        }
        /// <summary>
        /// 장착 가능한 부위 아이템인지 체크 
        /// </summary>
        /// <param name="toEquipIndex">착용하려는 부위 slot index</param>
        /// <returns></returns>
        public bool IsEquipParts(int toEquipIndex)
        {
            return (int)_struckTableItem.PartsID == toEquipIndex;
        }
        /// <summary>
        /// 착용 부위 type 가져오기
        /// </summary>
        /// <returns></returns>
        public override ItemConstants.PartsType GetPartsType()
        {
            if (_struckTableItem == null) return ItemConstants.PartsType.None; 
            return _struckTableItem.PartsID;
        }

        public override ItemConstants.Type GetItemType()
        {
            return _struckTableItem != null ? _struckTableItem.Type : ItemConstants.Type.None;
        }

        public override ItemConstants.Category GetItemCategory()
        {
            return _struckTableItem != null ? _struckTableItem.Category : ItemConstants.Category.None;
        }

        public override ItemConstants.SubCategory GetItemSubCategory()
        {
            return _struckTableItem != null ? _struckTableItem.SubCategory : ItemConstants.SubCategory.None;
        }

        public override ItemConstants.PartsType GetItemPartsType()
        {
            return _struckTableItem != null ? _struckTableItem.PartsID : ItemConstants.PartsType.None;
        }
        
        public override bool IsEquipType()
        {
            return IconType == IconConstants.Type.Item && _struckTableItem.Type == ItemConstants.Type.Equip;
        }

        public override bool IsPotionType()
        {
            return IconType == IconConstants.Type.Item && _struckTableItem.Type == ItemConstants.Type.Consumable &&
                   _struckTableItem.Category == ItemConstants.Category.Potion;
        }

        public override bool IsHpPotionType()
        {
            return IsPotionType() && _struckTableItem.SubCategory == ItemConstants.SubCategory.RecoverHp;
        }
        public override bool IsMpPotionType()
        {
            return IsPotionType() && _struckTableItem.SubCategory == ItemConstants.SubCategory.RecoverMp;
        }
        public override bool IsToolType()
        {
            return _struckTableItem.IsTool();
        }
        public override bool IsSeedType()
        {
            return _struckTableItem.IsSeed();
        }
        public override bool IsAntiFlag(ItemConstants.AntiFlag antiFlag)
        {
            return _struckTableItem.AntiFlag.Any(flag => flag == antiFlag);
        }
        /// <summary>
        /// 상점 판매 재화 타입 가져오기
        /// </summary>
        /// <returns></returns>
        public CurrencyConstants.Type GetSaleCurrencyType()
        {
            return _struckTableItem.SaleCurrencyType;
        }
        /// <summary>
        /// 상점 판매가격 가져오기
        /// </summary>
        /// <returns></returns>
        public int GetSaleCurrencyValue()
        {
            return _struckTableItem.SaleCurrencyValue;
        }
        public override float GetCoolTime()
        {
            return _struckTableItem.CoolTime;
        }

        public int GetUpgrade()
        {
            return _struckTableItem.Upgrade;
        }
        public override int GetPartsSlotIndex()
        {
            if (_struckTableItem == null) return -1;
            if (_struckTableItem.PartsID == ItemConstants.PartsType.None) return -1;
            return (int)_struckTableItem.PartsID;
        }
    }
}
