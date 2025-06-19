
namespace GGemCo.Scripts
{
    /// <summary>
    /// 창고 윈도우
    /// </summary>
    public class UIWindowStash : UIWindow
    {
        private UIWindowItemInfo uiWindowItemInfo;

        public StashData StashData;
        public InventoryData InventoryData;

        public TableItem TableItem;
        
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.Stash;
            base.Awake();
            SetSetIconHandler(new SetIconHandlerStash());
            DragDropHandler.SetStrategy(new DragDropStrategyStash());
        }
        protected override void Start()
        {
            base.Start();
            if (SceneGame != null && SceneGame.saveDataManager != null)
            {
                StashData = SceneGame.saveDataManager.Stash;
                InventoryData = SceneGame.saveDataManager.Inventory;
            }

            TableItem = TableLoaderManager.Instance.TableItem;
            uiWindowItemInfo = 
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
            var datas = SceneGame.saveDataManager.Stash.GetAllItemCounts();
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
                var table = TableItem.GetDataByUid(itemUid);
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
            SceneGame.Instance.uIWindowManager.MoveIcon(UIWindowManager.WindowUid.Stash, icon.index, UIWindowManager.WindowUid.Inventory, icon.GetCount());
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            uiWindowItemInfo.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Left, slotSize);
        }
    }
}