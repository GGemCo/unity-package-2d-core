
namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 장비 윈도우
    /// </summary>
    public class UIWindowEquip : UIWindow
    {
        private static readonly UISlotAcceptRule LegacyDefaultAcceptRule = new UISlotAcceptRule
        {
            mode = UISlotAcceptMode.Rule,
            allowedIconTypes = new[] { IconConstants.Type.Item },
            allowedItemTypes = new[] { ItemConstants.Type.Equip },
            failMessageKey = "Equip_InvalidSlot"
        };

        private TableItem tableItem;
        public InventoryData InventoryData;
        public EquipData EquipData;
        private UIWindowItemInfo uIWindowItemInfo;
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.Equip;
            if (TableLoaderManager.Instance == null) return;
            tableItem = TableLoaderManager.Instance.TableItem;
            base.Awake();
            
            IconPoolManager.SetSetIconHandler(new SetIconHandlerEquip());
            DragDropHandler.SetStrategy(new DragDropStrategyEquip());
        }
        protected override void Start()
        {
            base.Start();
            if (SceneGame != null && SceneGame.saveDataManager != null)
            {
                EquipData = SceneGame.saveDataManager.Equip;
                InventoryData = SceneGame.saveDataManager.Inventory;
            }
            uIWindowItemInfo =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid
                    .ItemInfo);
        }
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            base.OnShow(show);
            if (!show) return;
            LoadIcons();
        }
        /// <summary>
        /// 저장되어있는 아이템 정보로 아이콘 셋팅하기
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.saveDataManager.Equip.GetAllItemCounts();
            if (datas == null) return;
            foreach (var info in datas)
            {
                int index = info.Key;
                if (index >= icons.Length) continue;
                var icon = icons[index];
                if (icon == null) continue;
                UIIconItem uiIcon = icon.GetComponent<UIIconItem>();
                if (uiIcon == null) continue;
                SaveDataIcon structInventoryIcon = info.Value;
                int itemUid = structInventoryIcon.Uid;
                int itemCount = structInventoryIcon.Count;

                if (itemUid <= 0 || itemCount <= 0)
                {
                    uiIcon.ClearIconInfos();
                    continue;
                }
                var table = tableItem.GetDataByUid(itemUid);
                if (table == null || table.Uid <= 0) continue;
                uiIcon.ChangeInfoByUid(table.Uid, itemCount, iconInstanceId: structInventoryIcon.InstanceId);
            }
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            SceneGame.Instance.uIWindowManager.MoveIcon(uid, icon.index, UIWindowConstants.WindowUid.Inventory, 1);
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="show"></param>
        /// <param name="icon"></param>
        public override void ShowItemInfo(bool show, UIIcon icon = null)
        {
            if (show)
            {
                if (icon == null) return;
                uIWindowItemInfo.SetItemUid(icon.uid, icon.instanceId, icon.gameObject, UIWindowItemInfo.PositionType.Right, slotSize);
            }
            else
            {
                uIWindowItemInfo.Show(false);
            }
        }

        /// <summary>
        /// 기존 장비창 프리팹이 아직 Inspector 규칙으로 옮겨지지 않았어도
        /// "장비 아이템 + 부위 일치" 제약이 유지되도록 코드 fallback 을 제공합니다.
        /// </summary>
        protected override UISlotAcceptRule GetFallbackAcceptRule(int slotIndex)
        {
            if (slotIndex < 0)
                return LegacyDefaultAcceptRule;

            return new UISlotAcceptRule
            {
                mode = UISlotAcceptMode.Rule,
                allowedIconTypes = LegacyDefaultAcceptRule.allowedIconTypes,
                allowedItemTypes = LegacyDefaultAcceptRule.allowedItemTypes,
                allowedPartsTypes = new[] { (ItemConstants.PartsType)slotIndex },
                failMessageKey = "Equip_InvalidSlot"
            };
        }
    }
}
