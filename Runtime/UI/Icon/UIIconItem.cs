using System.Linq;
using UnityEngine.EventSystems;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 아이콘
    /// </summary>
    public class UIIconItem : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private UIWindowItemInfo uiWindowItemInfo;
        private StruckTableItem struckTableItem;
        private TableItem tableItem;
        private Player player;

        private readonly string[] optionTypes = new string[5];
        private readonly ConfigCommon.SuffixType[] optionSuffixes = new ConfigCommon.SuffixType[5];
        private readonly float[] optionValues = new float[5];
        
        protected override void Awake()
        {
            base.Awake();
            IconType = IconConstants.Type.Item;
            struckTableItem = null;
            if (TableLoaderManager.Instance == null) return;
            tableItem = TableLoaderManager.Instance.TableItem;
        }

        protected override void Start()
        {
            base.Start();
            uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowManager.WindowUid.ItemInfo);
        }
        /// <summary>
        /// 다른 uid 로 변경하기
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="iconIsLearn"></param>
        /// <param name="remainCoolTime"></param>
        public override bool ChangeInfoByUid(int iconUid, int iconCount = 0, int iconLevel = 0, bool iconIsLearn = false, int remainCoolTime = 0)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime)) return false;
            var info = tableItem.GetDataByUid(iconUid);
            if (info == null)
            {
                GcLogger.LogError("아이콘 테이블에 없는 아이템 입니다.");
                return false;
            }
            struckTableItem = info;

            optionTypes[0] = struckTableItem.OptionType1; 
            optionTypes[1] = struckTableItem.OptionType2; 
            optionTypes[2] = struckTableItem.OptionType3; 
            optionTypes[3] = struckTableItem.OptionType4; 
            optionTypes[4] = struckTableItem.OptionType5;

            optionSuffixes[0] = struckTableItem.OptionSuffix1;
            optionSuffixes[1] = struckTableItem.OptionSuffix2;
            optionSuffixes[2] = struckTableItem.OptionSuffix3; 
            optionSuffixes[3] = struckTableItem.OptionSuffix4; 
            optionSuffixes[4] = struckTableItem.OptionSuffix5;

            optionValues[0] = struckTableItem.OptionValue1;
            optionValues[1] = struckTableItem.OptionValue2;
            optionValues[2] = struckTableItem.OptionValue3;
            optionValues[3] = struckTableItem.OptionValue4;
            optionValues[4] = struckTableItem.OptionValue5;
            
            UpdateInfo();
            return true;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            // GcLogger.Log("OnPointerEnter "+eventData);
            window.ShowItemInfo(this);
            ShowSelected(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // GcLogger.Log("OnPointerExit "+eventData);
            uiWindowItemInfo.Show(false);
            ShowSelected(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!possibleClick) return;
            if (IsLock()) return;
            if(eventData.button == PointerEventData.InputButton.Left)
            {
                if (!window) return;
                window.SetSelectedIcon(index);
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
            return struckTableItem?.ImagePath;
        }
        /// <summary>
        /// 장착 가능한 타입 인지 체크 
        /// </summary>
        /// <returns></returns>
        public bool IsTypeEquip()
        {
            return struckTableItem.Type == ItemConstants.Type.Equip;
        }
        /// <summary>
        /// 장착 가능한 부위 아이템인지 체크 
        /// </summary>
        /// <param name="toEquipIndex">착용하려는 부위 slot index</param>
        /// <returns></returns>
        public bool IsEquipParts(int toEquipIndex)
        {
            return (int)struckTableItem.PartsID == toEquipIndex;
        }
        /// <summary>
        /// 착용 부위 type 가져오기
        /// </summary>
        /// <returns></returns>
        public override ItemConstants.PartsType GetPartsType()
        {
            return struckTableItem.PartsID;
        }
        
        public override bool IsEquipType()
        {
            return IconType == IconConstants.Type.Item && struckTableItem.Type == ItemConstants.Type.Equip;
        }

        public override bool IsPotionType()
        {
            return IconType == IconConstants.Type.Item && struckTableItem.Type == ItemConstants.Type.Consumable &&
                   struckTableItem.Category == ItemConstants.Category.Potion;
        }

        public override bool IsHpPotionType()
        {
            return IsPotionType() && struckTableItem.SubCategory == ItemConstants.SubCategory.RecoverHp;
        }
        public override bool IsMpPotionType()
        {
            return IsPotionType() && struckTableItem.SubCategory == ItemConstants.SubCategory.RecoverMp;
        }
        /// <summary>
        /// 어펙트 옵션이 있는지 
        /// </summary>
        /// <returns></returns>
        public override bool IsAffectUid()
        {
            return struckTableItem.StatusID1 == "AFFECT_UID";
        }
        public override int GetStatusValue1()
        {
            return struckTableItem.StatusValue1;
        }
        public override string GetStatusId1()
        {
            return struckTableItem.StatusID1;
        }
        public override ConfigCommon.SuffixType GetStatusSuffix1()
        {
            return struckTableItem.StatusSuffix1;
        }
        /// <summary>
        /// status, option 에 affect 가 있는지 체크 후 어펙트 실행
        /// </summary>
        public override void CheckStatusAffect()
        {
            if (player == null)
            {
                player = SceneGame.Instance.player.GetComponent<Player>();
            }
            if (struckTableItem.StatusID1 == ConfigCommon.StatusAffectId)
            {
                player.AddAffect(struckTableItem.StatusValue1);
            }
            if (struckTableItem.StatusID2 == ConfigCommon.StatusAffectId)
            {
                player.AddAffect(struckTableItem.StatusValue2);
            }

            for (var i = 0; i < optionTypes.Length; i++)
            {
                var option = optionTypes[i];
                if (option != ConfigCommon.StatusAffectId) continue;
                var optionValue = (int)optionValues[i];
                player.AddAffect(optionValue);
            }
        }
        
        public override bool IsAntiFlag(ItemConstants.AntiFlag antiFlag)
        {
            return struckTableItem.AntiFlag.Any(flag => flag == antiFlag);
        }
        /// <summary>
        /// 상점 판매 재화 타입 가져오기
        /// </summary>
        /// <returns></returns>
        public CurrencyConstants.Type GetSaleCurrencyType()
        {
            return struckTableItem.SaleCurrencyType;
        }
        /// <summary>
        /// 상점 판매가격 가져오기
        /// </summary>
        /// <returns></returns>
        public int GetSaleCurrencyValue()
        {
            return struckTableItem.SaleCurrencyValue;
        }
        public override float GetCoolTime()
        {
            return struckTableItem.CoolTime;
        }

        public int GetUpgrade()
        {
            return struckTableItem.Upgrade;
        }
    }
}
