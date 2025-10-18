using System.Linq;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 아이콘
    /// </summary>
    public class UIIconItem : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private UIWindowItemInfo _uiWindowItemInfo;
        private StruckTableItem _struckTableItem;
        private TableItem _tableItem;
        private Player _player;

        private readonly string[] _optionTypes = new string[5];
        private readonly ConfigCommon.SuffixType[] _optionSuffixes = new ConfigCommon.SuffixType[5];
        private readonly float[] _optionValues = new float[5];
        
        protected override void Awake()
        {
            base.Awake();
            IconType = IconConstants.Type.Item;
            _struckTableItem = null;
            if (TableLoaderManager.Instance == null) return;
            _tableItem = TableLoaderManager.Instance.TableItem;
        }

        protected override void Start()
        {
            base.Start();
            _uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowConstants.WindowUid.ItemInfo);
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
            var info = _tableItem.GetDataByUid(iconUid);
            if (info == null)
            {
                GcLogger.LogError("아이콘 테이블에 없는 아이템 입니다.");
                return false;
            }
            _struckTableItem = info;

            _optionTypes[0] = _struckTableItem.OptionType1; 
            _optionTypes[1] = _struckTableItem.OptionType2; 
            _optionTypes[2] = _struckTableItem.OptionType3; 
            _optionTypes[3] = _struckTableItem.OptionType4; 
            _optionTypes[4] = _struckTableItem.OptionType5;

            _optionSuffixes[0] = _struckTableItem.OptionSuffix1;
            _optionSuffixes[1] = _struckTableItem.OptionSuffix2;
            _optionSuffixes[2] = _struckTableItem.OptionSuffix3; 
            _optionSuffixes[3] = _struckTableItem.OptionSuffix4; 
            _optionSuffixes[4] = _struckTableItem.OptionSuffix5;

            _optionValues[0] = _struckTableItem.OptionValue1;
            _optionValues[1] = _struckTableItem.OptionValue2;
            _optionValues[2] = _struckTableItem.OptionValue3;
            _optionValues[3] = _struckTableItem.OptionValue4;
            _optionValues[4] = _struckTableItem.OptionValue5;
            
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
            _uiWindowItemInfo.Show(false);
            ShowSelected(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!PossibleClick) return;
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
        /// <summary>
        /// 어펙트 옵션이 있는지 
        /// </summary>
        /// <returns></returns>
        public override bool IsAffectUid()
        {
            return _struckTableItem.StatusID1 == "AFFECT_UID";
        }
        public override int GetStatusValue1()
        {
            return _struckTableItem.StatusValue1;
        }
        public override string GetStatusId1()
        {
            return _struckTableItem.StatusID1;
        }
        public override ConfigCommon.SuffixType GetStatusSuffix1()
        {
            return _struckTableItem.StatusSuffix1;
        }
        /// <summary>
        /// status, option 에 affect 가 있는지 체크 후 어펙트 실행
        /// </summary>
        public override void CheckStatusAffect()
        {
            if (_player == null)
            {
                _player = SceneGame.Instance.player.GetComponent<Player>();
            }
            if (_struckTableItem.StatusID1 == ConfigCommon.StatusAffectId)
            {
                _player.AddAffect(_struckTableItem.StatusValue1);
            }
            if (_struckTableItem.StatusID2 == ConfigCommon.StatusAffectId)
            {
                _player.AddAffect(_struckTableItem.StatusValue2);
            }

            for (var i = 0; i < _optionTypes.Length; i++)
            {
                var option = _optionTypes[i];
                if (option != ConfigCommon.StatusAffectId) continue;
                var optionValue = (int)_optionValues[i];
                _player.AddAffect(optionValue);
            }
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
