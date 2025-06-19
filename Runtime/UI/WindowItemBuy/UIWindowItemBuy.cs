using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 구매하기 윈도우
    /// </summary>
    public class UIWindowItemBuy : UIWindow
    {
        [Header("기본오브젝트")]
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textItemName;
        [Tooltip("아이템 개수")]
        public TextMeshProUGUI textItemCount;
        [Tooltip("최종 금액")]
        public TextMeshProUGUI textTotalPrice;
        [Tooltip("나누기 슬라이드")]
        public Slider sliderSplit;
        [Tooltip("구매하기 버튼")]
        public Button buttonConfirm;
        [Tooltip("취소 버트")]
        public Button buttonCancel;

        // 내가 가지고 있는 아이템 개수
        private int maxItemCount;
        // 구매 하는 아이템 uid
        private int itemUid;
        // 구매 하는 아이템 개수
        private int buyItemCount;
        // 구매 하는 아이템의 인벤토리 slot index
        private int buyItemIndex;
        // 판매하는 아이템의 shop 테이블 정보
        private StruckTableShop struckTableShop;
        
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.ItemBuy;
            base.Awake();
            buttonConfirm.onClick.RemoveAllListeners();
            buttonCancel.onClick.RemoveAllListeners();
            sliderSplit.onValueChanged.RemoveAllListeners();
            buttonConfirm.onClick.AddListener(OnClickConfirm);
            buttonCancel.onClick.AddListener(OnClickCancel);
            sliderSplit.onValueChanged.AddListener(OnValueChanged);
            
            SetSetIconHandler(new SetIconHandlerItemBuy());
        }
        public void UpdateInfo(int iconUid, int iconCount)
        {
            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(iconUid);
            if (info == null) return;
            textItemName.text = info.Name;
            textItemCount.text = $"{iconCount / iconCount}";
            maxItemCount = iconCount;
            Show(true);
            sliderSplit.value = 0.5f;
            // 강제로 특정 값으로 이벤트 호출 (값은 그대로 유지)
            sliderSplit.onValueChanged.Invoke(sliderSplit.value);
        }
        private void OnValueChanged(float value)
        {
            if (sliderSplit == null) return;
            buyItemCount = (int)(maxItemCount * value);
            if (buyItemCount == 0)
            {
                buyItemCount = 1;
                sliderSplit.value = (float)buyItemCount / maxItemCount;
            }
            textItemCount.text = $"{buyItemCount} / {maxItemCount}";
            textTotalPrice.text = "0";
            if (struckTableShop != null)
            {
                textTotalPrice.text = $"{CurrencyConstants.GetNameByCurrencyType(struckTableShop.CurrencyType)} {struckTableShop.CurrencyValue * buyItemCount}";
            }
        }
        /// <summary>
        /// 아이템 나누기
        /// </summary>
        private void OnClickConfirm()
        {
            if (struckTableShop == null) return;
            // 구매 하기
            SceneGame.Instance.BuyItem(struckTableShop.ItemUid, struckTableShop.CurrencyType,
                struckTableShop.CurrencyValue, buyItemCount);

            struckTableShop = null;
            Show(false);
        }

        private void OnClickCancel()
        {
            struckTableShop = null;
            Show(false);
        }

        public void SetPriceInfo(StruckTableShop pstruckTableShop)
        {
            struckTableShop = pstruckTableShop;
        }
        /// <summary>
        /// 창 닫힐때 register 됬던 아이콘 정보 지워주기
        /// </summary>
        /// <param name="show"></param>
        public override void OnShow(bool show)
        {
            if (show) return;
            UnRegisterAllIcons(uid);
        }
    }
}