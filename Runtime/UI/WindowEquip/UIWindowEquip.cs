
namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 장비 윈도우
    /// </summary>
    public class UIWindowEquip : UIWindow
    {
        private TableItem tableItem;
        public InventoryData InventoryData;
        public EquipData EquipData;
        private UIWindowItemInfo uIWindowItemInfo;
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowManager.WindowUid.Equip;
            if (TableLoaderManager.Instance == null) return;
            tableItem = TableLoaderManager.Instance.TableItem;
            base.Awake();
            
            SetSetIconHandler(new SetIconHandlerEquip());
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
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowManager.WindowUid
                    .ItemInfo);
        }
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
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
                uiIcon.ChangeInfoByUid(table.Uid);
                uiIcon.SetCount(itemCount);
            }
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            SceneGame.Instance.uIWindowManager.MoveIcon(uid, icon.index, UIWindowManager.WindowUid.Inventory, 1);
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            uIWindowItemInfo.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Right, slotSize);
        }
    }
}