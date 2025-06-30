using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowItemCraft : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("제작 리스트 Element 프리팹")]
        public GameObject prefabUIElementCraft;
        [Tooltip("재료 재료 Element 프리팹")]
        public GameObject prefabElementMaterial;
        [Tooltip("재료 아이템 Element 를 담을 panel")]
        public GameObject containerMaterial;
        [Tooltip("제작 확률")]
        public TextMeshProUGUI textRate;
        [Tooltip("제작 금액")]
        public TextMeshProUGUI textNeedCurrency;
        [Tooltip("제작 결과")]
        public TextMeshProUGUI textCraftResult;
        [Tooltip("제작 버튼")]
        public Button buttonCraft;

        private TableItemCraft _tableItemCraft;
        private TableItem _tableItem;
        private readonly Dictionary<int, UIElementItemCraft> _uiElementItemCrafts = new Dictionary<int, UIElementItemCraft>();
        
        private UIWindowItemInfo _uiWindowItemInfo;
        private UIWindowInventory _uiWindowInventory;
        
        private InventoryData _inventoryData;
        private StruckTableItemCraft _selectedCtruckTableItemCraft;
        
        // 재료 최대 개수. item_upgrade 테이블에 있는 컬럼수와 맞아야 한다
        private const int MaxElementCount = 4;
        private readonly List<UIElementMaterial> _elementMaterials = new List<UIElementMaterial>();

        private int _currentItemCraftUid;
        
        protected override void Awake()
        {
            _currentItemCraftUid = 0;
            _selectedCtruckTableItemCraft = null;
            uid = UIWindowConstants.WindowUid.ItemCraft;
            if (TableLoaderManager.Instance != null)
            {
                _tableItemCraft = TableLoaderManager.Instance.TableItemCraft;
                _tableItem = TableLoaderManager.Instance.TableItem;
            }
            _elementMaterials.Clear();
            base.Awake();

            SetSetIconHandler(new SetIconHandlerItemCraft());
            
            // 재료 element 초기 생성하기
            if (prefabElementMaterial != null)
            {
                for (int i = 0; i < MaxElementCount; i++)
                {
                    GameObject elementMaterial = Instantiate(prefabElementMaterial, containerMaterial.transform);
                    _elementMaterials.Add(elementMaterial.GetComponent<UIElementMaterial>());
                    elementMaterial.SetActive(false);
                }
            }
            if (buttonCraft != null)
            {
                buttonCraft.onClick.RemoveAllListeners();
                buttonCraft.onClick.AddListener(OnClickCraft);
            }
            if (textCraftResult != null)
            {
                textCraftResult.gameObject.SetActive(false);
            }
            InitializeInfo();
        }

        /// <summary>
        /// 아이템 제작 uid 로 ui element craft 정보 셋팅하기
        /// </summary>
        public void SetInfoByItemCraftUid(int itemCraftUid)
        {
            // 같은 상점을 열었으면 업데이트 하지 않는다
            if (_currentItemCraftUid > 0 && _currentItemCraftUid == itemCraftUid) return;
            // 기존 element 지우기
            int index = 0;
            foreach (var data in _uiElementItemCrafts)
            {
                Destroy(data.Value.gameObject);
                if (slots[index])
                {
                    Destroy(slots[index].gameObject);
                }
                if (icons[index])
                {
                    Destroy(icons[index].gameObject);
                }
                index++;
            }
            slots = null;
            icons = null;
            _uiElementItemCrafts.Clear();
            _currentItemCraftUid = itemCraftUid;
            if (AddressableLoaderSettings.Instance == null || containerIcon == null) return;
            if (prefabUIElementCraft == null)
            {
                GcLogger.LogError("prefabUIElementCraft 프리팹이 없습니다.");
                return;
            }
            if (itemCraftUid <= 0) return;
            var datas = _tableItemCraft.GetDataByUid(itemCraftUid);
            if (datas == null)
            {
                GcLogger.LogError("item_craft 테이블에 정보가 없습니다. item_craft Uid: " + itemCraftUid);
                return;
            }
            maxCountIcon = datas.Count;
            if (datas.Count <= 0) return;
            slots = new GameObject[maxCountIcon];
            icons = new GameObject[maxCountIcon];
            GameObject iconItem = ConfigResources.IconItem.Load();
            GameObject slot = ConfigResources.Slot.Load();
            if (iconItem == null) return;
            
            index = 0;
            foreach (var info in datas)
            {
                GameObject parent = gameObject;
                // UI Element 프리팹이 있으면 만든다.
                if (prefabUIElementCraft != null)
                {
                    parent = Instantiate(prefabUIElementCraft, containerIcon.gameObject.transform);
                    if (parent == null) continue;
                    UIElementItemCraft uiElementItemCraft = parent.GetComponent<UIElementItemCraft>();
                    if (uiElementItemCraft == null) continue;
                    uiElementItemCraft.Initialize(this, index, info);
                    uiElementItemCraft.UpdateInfos(datas[index]);
                    _uiElementItemCrafts.TryAdd(index, uiElementItemCraft);
                }
                
                GameObject slotObject = Instantiate(slot, parent.transform);
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                uiSlot.Initialize(this, uid, index, slotSize);
                SetPositionUiSlot(uiSlot, index);
                slots[index] = slotObject;
                
                GameObject icon = Instantiate(iconItem, slotObject.transform);
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                // deactivate 상태에서는 awake 가 호출되지 않는다.
                uiIcon.Initialize(this, uid, index, index, iconSize, slotSize);
                // count  1로 초기화
                uiIcon.ChangeInfoByUid(info.ResultItemUid, 1);
                // element 에서 마우스 이벤트 처리
                uiIcon.SetRaycastTarget(false);
                uiIcon.RemoveLockImage();
                
                icons[index] = icon;
                index++;
            }
        }

        protected override void Start()
        {
            base.Start();
            _uiWindowItemInfo =
                SceneGame?.uIWindowManager?.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid
                    .ItemInfo);
            _uiWindowInventory =
                SceneGame?.uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid
                    .Inventory);
            _inventoryData = SceneGame?.saveDataManager?.Inventory;
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            // if (icon == null) return;
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            _uiWindowItemInfo?.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Left, slotSize);
        }
        /// <summary>
        /// 강화 정보 초기화하기
        /// </summary>
        private void InitializeInfo()
        {
            _currentItemCraftUid = 0;
            _selectedCtruckTableItemCraft = null;
            // 재료 정보 초기화
            ClearMaterials();
            textRate?.gameObject.SetActive(false);
            textNeedCurrency?.gameObject.SetActive(false);
        }
        /// <summary>
        /// 제작 정보 셋팅
        /// </summary>
        /// <param name="info"></param>
        public void SetInfo(StruckTableItemCraft info)
        {
            InitializeInfo();
            
            // var info = TableItemCraft.GetDataByUid(craftUid);
            // if (info == null)
            // {
            //     GcLogger.LogError("item_craft 테이블에 정보가 없습니다. craft uid: " + craftUid);
            //     return;
            // }
            var sourceInfo = _tableItem.GetDataByUid(info.ResultItemUid);
            if (sourceInfo == null)
            {
                GcLogger.LogError("제작하는 아이템 정보가 없습니다. item uid:"+info.ResultItemUid);
                return;
            }
            
            _selectedCtruckTableItemCraft = info;
            if (textRate != null)
            {
                textRate.gameObject.SetActive(true);
                textRate.text = $"Rate: {info.Rate}%"; // 강화 확률:
            }
            if (textNeedCurrency != null)
            {
                textNeedCurrency.gameObject.SetActive(true);
                textNeedCurrency.text = $"{CurrencyConstants.GetNameByCurrencyType(info.NeedCurrencyType)}: {info.NeedCurrencyValue}";
            }
            
            SetMaterialInfo(0, info.NeedItemUid1, info.NeedItemCount1);
            SetMaterialInfo(1, info.NeedItemUid2, info.NeedItemCount2);
            SetMaterialInfo(2, info.NeedItemUid3, info.NeedItemCount3);
            SetMaterialInfo(3, info.NeedItemUid4, info.NeedItemCount4);
        }
        /// <summary>
        /// 제작에 필요한 재료 정보 셋팅
        /// </summary>
        /// <param name="index"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        private void SetMaterialInfo(int index, int itemUid, int itemCount)
        {
            if (itemUid <= 0 || itemCount <= 0) return;

            if (index < 0 || index >= _elementMaterials.Count) return;

            var material = _elementMaterials[index];
            material?.InitializeSetInfo(itemUid, itemCount, this);
        }
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            if (show) return;
            // 윈도우가 닫힐때
            InitializeInfo();
        }
        /// <summary>
        /// 재료 정보 지워주기
        /// </summary>
        private void ClearMaterials()
        {
            foreach (var elementMaterial in _elementMaterials)
            {
                elementMaterial.gameObject.SetActive(false);
                elementMaterial.ClearInfo();
            }
        }
        /// <summary>
        /// 슬롯 위치 정해주기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="index"></param>
        private void SetPositionUiSlot(UISlot slot, int index)
        {
            UIElementItemCraft uiElementItemCraft = _uiElementItemCrafts[index];
            if (uiElementItemCraft == null) return;
            Vector3 position = uiElementItemCraft.GetIconPosition();
            if (position == Vector3.zero) return;
            slot.transform.localPosition = position;
        }
        /// <summary>
        /// 제작하기
        /// </summary>
        private void OnClickCraft()
        {
            if (textCraftResult != null)
            {
                textCraftResult.gameObject.SetActive(false);
            }
            if (_selectedCtruckTableItemCraft == null)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("Please select an item to craft.");//"제작할 아이템을 선택해주세요."
                return;
            }
            // 재료 체크
            foreach (var elementMaterial in _elementMaterials)
            {
                bool result = elementMaterial.CheckHaveCount();
                if (!result)
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Not enough materials.");//"재료가 부족합니다."
                    return;
                }
            }
            // 재화 체크
            var resultCommon = SceneGame.saveDataManager.Player.CheckNeedCurrency(_selectedCtruckTableItemCraft.NeedCurrencyType,
                _selectedCtruckTableItemCraft.NeedCurrencyValue);
            if (resultCommon.Code == ResultCommon.Type.Fail)
            {
                SceneGame.systemMessageManager.ShowMessageWarning(resultCommon.Message);
                return;
            }
            // 재료 개수 빼주기
            foreach (var elementMaterial in _elementMaterials)
            {
                if (elementMaterial == null) continue;
                var materialInfo = elementMaterial.GetItemUidCount();
                if (materialInfo.Item1 == 0 || materialInfo.Item2 == 0) continue;
                ResultCommon resultMaterial = _inventoryData.MinusItem(materialInfo.Item1, materialInfo.Item2);
                _uiWindowInventory.SetIcons(resultMaterial);
            }

            // 확률 체크
            if (_selectedCtruckTableItemCraft.Rate <= 0)
            {
                GcLogger.LogError("item_upgrade 테이블에 확률값이 잘 못되었습니다. rate: "+_selectedCtruckTableItemCraft.Rate);
                return;
            }
            bool updateResult = false;
            int random = Random.Range(0, 100);
            if (random < _selectedCtruckTableItemCraft.Rate)
            {
                updateResult = true;
            }
            
            // 성공, 실패 체크 
            if (updateResult)
            {
                // 제작 처리, inventoryData 에 item uid 추가하기
                var resultUpgrade = _inventoryData.AddItem(_selectedCtruckTableItemCraft.ResultItemUid, 1);
                _uiWindowInventory.SetIcons(resultUpgrade);
            }
            if (textCraftResult != null)
            {
                textCraftResult.gameObject.SetActive(true);
                if (updateResult)
                {
                    textCraftResult.text = "Success"; //"제작에 성공하였습니다.";
                    textCraftResult.color = Color.blue;
                }
                else
                {
                    textCraftResult.text = "Fail"; //""제작에 실패하였습니다.";
                    textCraftResult.color = Color.red;
                }
            }
            // 정보 갱신하기
            SetInfo(_selectedCtruckTableItemCraft);
        }

    }
}