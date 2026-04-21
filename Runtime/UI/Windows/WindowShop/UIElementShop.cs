using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 구매 리스트 element
    /// </summary>
    public class UIElementShop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("기본속성")]
        [Tooltip("아이콘 위치")]
        public Vector3 iconPosition;
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textName;
        [Tooltip("구매 가격")]
        public TextMeshProUGUI textPrice;
        [Tooltip("구매하기 버튼")]
        public Button buttonBuy;
        
        [Header("마우스 이벤트 On/Off")]
        [Tooltip("활성화 할 경우, 아이템에서 마우스 오버하면, 정보창이 나타납니다.")]
        [SerializeField] private bool usePointerEnterEvent = true;
        [Tooltip("활성화 할 경우, 아이템에서 마우스 아웃하면, 정보창이 사라집니다.")]
        [SerializeField] private bool usePointerExitEvent = true;
        [Tooltip("활성화 할 경우, 아이템에서 마우스 클릭하면, 정보창이 나타납니다.")]
        [SerializeField] private bool usePointerClickEvent = false;
        
        private UIWindowShop _uiWindowShop;
        private UIWindowItemBuy _uIWindowItemBuy;
        private UIWindowItemInfo _uIWindowItemInfo;
        
        private StruckTableShop _struckTableShop;
        private TableItem _tableItem;
        private PlayerData _playerData;
        private int _slotIndex;

        private void Start()
        {
            EnsureWindows();
        }

        private void EnsureWindows()
        {
            _uIWindowItemBuy ??= 
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemBuy>(UIWindowConstants.WindowUid
                    .ItemBuy);
            _uIWindowItemInfo ??= 
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid
                    .ItemInfo);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="uiWindowShop"></param>
        /// <param name="slotIndex"></param>
        /// <param name="struckTableShop"></param>
        public void Initialize(UIWindowShop uiWindowShop, int slotIndex, StruckTableShop struckTableShop)
        {
            _playerData = SceneGame.Instance.saveDataManager.Player;
            _struckTableShop = struckTableShop;
            _slotIndex = slotIndex;
            if (buttonBuy != null)
            {
                buttonBuy.onClick.AddListener(OnClickBuy);
            }

            _uiWindowShop = uiWindowShop;
            _tableItem = TableLoaderManager.Instance.TableItem;
            
            UpdateInfos(struckTableShop);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// </summary>
        public void UpdateInfos(StruckTableShop struckTableShop)
        {
            _struckTableShop = struckTableShop;
            if (_struckTableShop == null)
            {
                GcLogger.LogError($"shop 테이블에 정보가 없습니다. struckTableItem is null");
                return;
            }
            var info = _tableItem.GetDataByUid(_struckTableShop.ItemUid);
            if (info == null)
            {
                GcLogger.LogError($"item 테이블에 정보가 없습니다. item uid: {_struckTableShop.ItemUid}");
                return;
            }

            if (textName != null)
            {
                textName.text = info.Name;
            }
            if (textPrice != null) 
                textPrice.text = $"{_struckTableShop.CurrencyType} {_struckTableShop.CurrencyValue}";
            if (buttonBuy)
                buttonBuy.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 구매하기
        /// </summary>
        public void OnClickBuy()
        {
            // 여러개 살 수 있는지
                // 팝업창 띄어서 개수 정하기
                // 골드가 충분하지 체크
            if (_struckTableShop.MaxBuyCount > 1)
            {
                // 구매할 수 있는 최대 수량으로 등록
                int count = (int)_playerData.GetPossibleBuyCount(_struckTableShop.CurrencyType, _struckTableShop.CurrencyValue);
                if (count <= 0)
                {
                    SceneGame.Instance.systemMessageManager.ShowWarningCurrency(_struckTableShop.CurrencyType);
                    return;
                }
                var info = _tableItem.GetDataByUid(_struckTableShop.ItemUid);
                if (info != null && count > info.MaxOverlayCount)
                {
                    count = info.MaxOverlayCount;
                }
                
                _uIWindowItemBuy?.SetPriceInfo(_struckTableShop);
                SceneGame.Instance.uIWindowManager.RegisterIcon(_uiWindowShop.uid, _slotIndex, UIWindowConstants.WindowUid.ItemBuy, count);
            }
            // 한번에 하나만 살 수 있는지
            // 골드가 충분하지 체크
            else
            {
                SceneGame.Instance.BuyItem(_struckTableShop.ItemUid, _struckTableShop.CurrencyType,
                    _struckTableShop.CurrencyValue);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!usePointerEnterEvent) return;
            SetSelected(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!usePointerClickEvent) return;
            SetSelected(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!usePointerExitEvent) return;
            _uIWindowItemInfo.Show(false);
        }

        public Vector3 GetIconPosition() => iconPosition;

        public void SetSelected(bool value)
        {
            if (value)
            {
                _uiWindowShop.SetSelectItem(this);
                
                EnsureWindows();
                _uIWindowItemInfo.SetItemUid(_struckTableShop.ItemUid, 0, gameObject, UIWindowItemInfo.PositionType.Fixed,
                    _uiWindowShop.containerIcon.cellSize);
            }
            else
            {
                
            }
        }

        public (CurrencyConstants.Type, int) GetPrice()
        {
            if (_struckTableShop == null) return (CurrencyConstants.Type.None, 0);
            return (_struckTableShop.CurrencyType, _struckTableShop.CurrencyValue);
        }
    }
}