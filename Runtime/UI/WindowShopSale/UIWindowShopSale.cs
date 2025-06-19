using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 판매 윈도우
    /// </summary>
    public class UIWindowShopSale : UIWindow
    {
        [Tooltip("판매하기 버튼")] public Button buttonSale;
        [Tooltip("판매 총 금액 골드")] public TextMeshProUGUI textTotalPriceGold;
        [Tooltip("판매 총 금액 실버")] public TextMeshProUGUI textTotalPriceSilver;
        
        private UIWindowItemInfo uiWindowItemInfo;

        private readonly BehaviorSubject<int> totalPriceGold = new(0);
        private readonly BehaviorSubject<int> totalPriceSilver = new(0);

        public InventoryData InventoryData;
        public ShopSaleData ShopSaleData;
        public TableItem TableItem;
        
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.ShopSale;
            if (TableLoaderManager.Instance == null) return;
            base.Awake();
            buttonSale?.onClick.RemoveAllListeners();
            buttonSale?.onClick.AddListener(OnClickSale);
            
            totalPriceGold.DistinctUntilChanged()
                .CombineLatest(totalPriceSilver, (_, _) => Unit.Default)
                .Subscribe(_ => SetTotalPriceText())
                .AddTo(this);
            
            SetSetIconHandler(new SetIconHandlerShopSale());
            DragDropHandler.SetStrategy(new DragDropStrategyShopSale());
        }
        protected override void Start()
        {
            base.Start();
            TableItem = TableLoaderManager.Instance.TableItem;
            
            if (SceneGame != null && SceneGame.saveDataManager != null)
            {
                InventoryData = SceneGame.saveDataManager.Inventory;
                ShopSaleData = SceneGame.saveDataManager.ShopSale;
            }
            uiWindowItemInfo = 
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowManager.WindowUid
                    .ItemInfo);
        }
        public override bool Show(bool show)
        {
            if (!base.Show(show)) return false;
            if (show)
            {
                Initialize();
            }
            return true;
        }

        private void Initialize()
        {
            totalPriceGold.OnNext(0);
            totalPriceSilver.OnNext(0);
            foreach (var icon in icons)
            {
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                uiIcon.ClearIconInfos();
            }
        }

        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            uiWindowItemInfo.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Left, slotSize);
        }
        /// <summary>
        /// 아이템 팔기
        /// </summary>
        private void OnClickSale()
        {
            // 먼저 재화 처리한다
            SceneGame.saveDataManager.Player.AddCurrency(CurrencyConstants.Type.Gold, totalPriceGold.Value);
            SceneGame.saveDataManager.Player.AddCurrency(CurrencyConstants.Type.Silver, totalPriceSilver.Value);
            
            RemoveAndDetachIcon();
        }
        /// <summary>
        /// 총 판매 금액 계산하기
        /// </summary>
        public void ReCalculateSaleItem()
        {
            int totalGold = 0;
            int totalSilver = 0;
            foreach (var icon in icons)
            {
                UIIconItem uiIconItem = icon.GetComponent<UIIconItem>();
                if (uiIconItem == null || uiIconItem.uid <= 0 || uiIconItem.GetCount() <= 0) continue;
                CurrencyConstants.Type currencyType = uiIconItem.GetSaleCurrencyType();
                int value = uiIconItem.GetSaleCurrencyValue();
                int count = uiIconItem.GetCount();
                if (currencyType == CurrencyConstants.Type.Gold)
                {
                    totalGold += value * count;
                }
                else if (currencyType == CurrencyConstants.Type.Silver)
                {
                    totalSilver += value * count;
                }
            }
            totalPriceGold.OnNext(totalGold);
            totalPriceSilver.OnNext(totalSilver);
        }

        private void SetTotalPriceText()
        {
            textTotalPriceGold.text = $"{CurrencyConstants.GetNameGold()}: {totalPriceGold.Value}";
            textTotalPriceSilver.text = $"{CurrencyConstants.GetNameSilver()}: {totalPriceSilver.Value}";
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            SceneGame.uIWindowManager.UnRegisterIcon(uid, icon.slotIndex);
        }
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            if (show) return;
            UnRegisterAllIcons(uid);
        }
    }
}