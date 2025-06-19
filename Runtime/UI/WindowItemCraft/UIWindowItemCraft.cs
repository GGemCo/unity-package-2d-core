using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    public class UIWindowItemCraft : UIWindow
    {
        [Header("기본속성")]
        [Tooltip("제작 리스트 Element 프리팹")]
        public GameObject prefabUIElementCraft;
        [Tooltip("재료 재료 Element 프리팹")]
        public GameObject prefabElementMaterial;
        [Tooltip("재료 아이템 element 를 담을 panel")]
        public GameObject containerMaterial;
        [Tooltip("제작 확률")]
        public TextMeshProUGUI textRate;
        [Tooltip("제작 금액")]
        public TextMeshProUGUI textNeedCurrency;
        [Tooltip("제작 결과")]
        public TextMeshProUGUI textCraftResult;
        [Tooltip("제작 버튼")]
        public Button buttonCraft;
        
        public TableItemCraft TableItemCraft;
        private TableItem tableItem;
        public readonly Dictionary<int, UIElementItemCraft> UIElementItemCrafts = new Dictionary<int, UIElementItemCraft>();
        
        private UIWindowItemInfo uiWindowItemInfo;
        private UIWindowInventory uiWindowInventory;
        
        private InventoryData inventoryData;
        private StruckTableItemCraft struckTableItemCraft;
        
        // 재료 최대 개수. item_upgrade 테이블에 있는 컬럼수와 맞아야 한다
        private const int MaxElementCount = 4;
        private readonly List<UIElementMaterial> elementMaterials = new List<UIElementMaterial>();
        
        protected override void Awake()
        {
            struckTableItemCraft = null;
            uid = UIWindowManager.WindowUid.ItemCraft;
            if (TableLoaderManager.Instance != null)
            {
                TableItemCraft = TableLoaderManager.Instance.TableItemCraft;
                tableItem = TableLoaderManager.Instance.TableItem;
            }
            elementMaterials.Clear();
            base.Awake();

            SetSetIconHandler(new SetIconHandlerItemCraft());
            
            // 재료 element 초기 생성하기
            if (prefabElementMaterial != null)
            {
                for (int i = 0; i < MaxElementCount; i++)
                {
                    GameObject elementMaterial = Instantiate(prefabElementMaterial, containerMaterial.transform);
                    elementMaterials.Add(elementMaterial.GetComponent<UIElementMaterial>());
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
        protected override void Start()
        {
            base.Start();
            uiWindowItemInfo =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowManager.WindowUid
                    .ItemInfo);
            uiWindowInventory =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid
                    .Inventory);
            inventoryData = SceneGame.saveDataManager.Inventory;
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
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
        /// 강화 정보 초기화하기
        /// </summary>
        private void InitializeInfo()
        {
            struckTableItemCraft = null;
            // 재료 정보 초기화
            ClearMaterials();
            if (textRate != null)
            {
                textRate.gameObject.SetActive(false);
            }
            if (textNeedCurrency != null)
            {
                textNeedCurrency.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// 제작 정보 셋팅
        /// </summary>
        /// <param name="craftUid"></param>
        public void SetInfo(int craftUid)
        {
            InitializeInfo();
            
            var info = TableItemCraft.GetDataByUid(craftUid);
            if (info == null)
            {
                GcLogger.LogError("item_craft 테이블에 정보가 없습니다. craft uid: " + craftUid);
                return;
            }
            var sourceInfo = tableItem.GetDataByUid(info.ResultItemUid);
            if (sourceInfo == null)
            {
                GcLogger.LogError("제작하는 아이템 정보가 없습니다. item uid:"+info.ResultItemUid);
                return;
            }
            
            struckTableItemCraft = info;
            if (textRate != null)
            {
                textRate.gameObject.SetActive(true);
                textRate.text = $"강화 확률: {info.Rate}%";
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

            if (index < 0 || index >= elementMaterials.Count) return;

            var material = elementMaterials[index];
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
            foreach (var elementMaterial in elementMaterials)
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
        public void SetPositionUiSlot(UISlot slot, int index)
        {
            UIElementItemCraft uiElementItemCraft = UIElementItemCrafts[index];
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
            if (struckTableItemCraft == null)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("제작할 아이템을 선택해주세요.");
                return;
            }
            // 재료 체크
            foreach (var elementMaterial in elementMaterials)
            {
                bool result = elementMaterial.CheckHaveCount();
                if (!result)
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("재료가 부족합니다.");
                    return;
                }
            }
            // 재화 체크
            var resultCommon = SceneGame.saveDataManager.Player.CheckNeedCurrency(struckTableItemCraft.NeedCurrencyType,
                struckTableItemCraft.NeedCurrencyValue);
            if (resultCommon.Code == ResultCommon.Type.Fail)
            {
                SceneGame.systemMessageManager.ShowMessageWarning(resultCommon.Message);
                return;
            }
            // 재료 개수 빼주기
            foreach (var elementMaterial in elementMaterials)
            {
                if (elementMaterial == null) continue;
                var materialInfo = elementMaterial.GetItemUidCount();
                if (materialInfo.Item1 == 0 || materialInfo.Item2 == 0) continue;
                ResultCommon resultMaterial = inventoryData.MinusItem(materialInfo.Item1, materialInfo.Item2);
                uiWindowInventory.SetIcons(resultMaterial);
            }

            // 확률 체크
            if (struckTableItemCraft.Rate <= 0)
            {
                GcLogger.LogError("item_upgrade 테이블에 확률값이 잘 못되었습니다. rate: "+struckTableItemCraft.Rate);
                return;
            }
            bool updateResult = false;
            int random = Random.Range(0, 100);
            if (random < struckTableItemCraft.Rate)
            {
                updateResult = true;
            }
            
            // 성공, 실패 체크 
            if (updateResult)
            {
                // 제작 처리, inventoryData 에 item uid 추가하기
                var resultUpgrade = inventoryData.AddItem(struckTableItemCraft.ResultItemUid, 1);
                uiWindowInventory.SetIcons(resultUpgrade);
            }
            if (textCraftResult != null)
            {
                textCraftResult.gameObject.SetActive(true);
                if (updateResult)
                {
                    textCraftResult.text = "제작에 성공하였습니다.";
                    textCraftResult.color = Color.blue;
                }
                else
                {
                    textCraftResult.text = "제작에 실패하였습니다.";
                    textCraftResult.color = Color.red;
                }
            }
            // 정보 갱신하기
            SetInfo(struckTableItemCraft.Uid);
        }

    }
}