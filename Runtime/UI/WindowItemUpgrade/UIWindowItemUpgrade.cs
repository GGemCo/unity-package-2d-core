using System.Collections.Generic;
#if GGEMCO_USE_SPINE
using Spine;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 강화 윈도우
    /// preLoadSlots 의 Element 0 에는 강화 하는 아이템 슬롯, Element 1 에는 강화 결과 아이템 슬롯 프리팹을 연결한다.
    /// </summary>
    public class UIWindowItemUpgrade : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("강화 정보")]
        [Tooltip("강화하는 아이템 이름")]
        public TextMeshProUGUI textItemName;
        [Tooltip("강화 확률")]
        public TextMeshProUGUI textRate;
        [Tooltip("강화에 필요한 재화")]
        public TextMeshProUGUI textNeedCurrency;
        [Tooltip("강화 속성 1")]
        public TextMeshProUGUI textStatusID1;
        [Tooltip("강화 속성 2")]
        public TextMeshProUGUI textStatusID2;
        [Tooltip("강화 결과")]
        public TextMeshProUGUI textResult;
        
        [Header("재료 설정")]
        [Tooltip("재료 아이템 프리팹")]
        public GameObject prefabElementMaterial;
        [Tooltip("재료 아이템 element 를 담을 panel")]
        public GameObject containerMaterial;
        
        [Header("")]
        [Tooltip("강화하기 버튼")]
        public Button buttonUpgrade;
#if GGEMCO_USE_SPINE
        [Tooltip("강화 이펙트 오브젝트")] public Spine2dUIController effectItemUpgrade;
#endif
        private TableItem _tableItem;
        private TableStatus _tableStatus;
        private TableItemUpgrade _tableItemUpgrade;
        private StruckTableItemUpgrade _struckTableItemUpgrade;
        
        private UIWindowItemInfo _uiWindowItemInfo;
        private UIWindowInventory _uiWindowInventory;

        private InventoryData _inventoryData;
        
        // 재료 최대 개수. item_upgrade 테이블에 있는 컬럼수와 맞아야 한다
        private const int MaxElementCount = 4;
        private readonly List<UIElementMaterial> _elementMaterials = new List<UIElementMaterial>();
        // 성공, 실패 결과
        private bool _updateResult;
        private const int SourceIconSlotIndex = 0;
        private const int ResultIconSlotIndex = 1;
        
        protected override void Awake()
        {
            _updateResult = false;
            _elementMaterials.Clear();
            uid = UIWindowConstants.WindowUid.ItemUpgrade;
            if (TableLoaderManager.Instance == null) return;
            _tableItem = TableLoaderManager.Instance.TableItem;
            _tableItemUpgrade = TableLoaderManager.Instance.TableItemUpgrade;
            _tableStatus = TableLoaderManager.Instance.TableStatus;
            base.Awake();
            SetSetIconHandler(new SetIconHandlerItemUpgrade());
            DragDropHandler.SetStrategy(new DragDropStrategyItemUpgrade());

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

            if (buttonUpgrade != null)
            {
                buttonUpgrade.onClick.RemoveAllListeners();
                buttonUpgrade.onClick.AddListener(OnClickUpgrade);
            }
            
            ShowTextResult(false);

            InitializeInfo();
        }
        protected override void Start()
        {
            base.Start();
            _uiWindowItemInfo =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid
                    .ItemInfo);
            _uiWindowInventory =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid
                    .Inventory);
            _inventoryData = SceneGame.saveDataManager.Inventory;
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            // 업그레이드 결과 아이콘을 우클릭했을때는 아무 처리도 하지 않는다.
            if (icon.slotIndex > SourceIconSlotIndex) return;
            SceneGame.Instance.uIWindowManager.UnRegisterIcon(uid, icon.slotIndex);
            DetachIcon(ResultIconSlotIndex);
            ShowTextResult(false);
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            _uiWindowItemInfo.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Left, slotSize);
        }

        public void ShowTextResult(bool show)
        {
            textResult?.gameObject.SetActive(show);
        }

        public void InitializeText()
        {
            if (textItemName != null)
            {
                textItemName.gameObject.SetActive(false);
            }
            if (textRate != null)
            {
                textRate.gameObject.SetActive(false);
            }
            if (textNeedCurrency != null)
            {
                textNeedCurrency.gameObject.SetActive(false);
            }
            if (textStatusID1 != null)
            {
                textStatusID1.gameObject.SetActive(false);
            }
            if (textStatusID2 != null)
            {
                textStatusID2.gameObject.SetActive(false); 
            }
        }
        /// <summary>
        /// 강화 정보 초기화하기
        /// </summary>
        private void InitializeInfo()
        {
            // 강화 테이블 정보 초기화
            _struckTableItemUpgrade = null;
            // 재료 정보 초기화
            ClearMaterials();
            InitializeText();
            DetachIcon(ResultIconSlotIndex);
        }
        public void SetInfo(UIIcon icon)
        {
            InitializeInfo();
            
            if (icon == null) return;
            var info = _tableItemUpgrade.GetDataBySourceItemUid(icon.uid);
            if (info == null)
            {
                GcLogger.LogError("item_upgrade 테이블에 정보가 없습니다. item uid: " + icon.uid);
                return;
            }
            var sourceInfo = _tableItem.GetDataByUid(info.SourceItemUid);
            if (sourceInfo == null)
            {
                GcLogger.LogError("강화하는 아이템 정보가 없습니다. item uid:"+info.SourceItemUid);
                return;
            }

            if (sourceInfo.Upgrade >= info.MaxUpgrade)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("This item can no longer be enhanced.");//"더이상 강화 할 수 없습니다."
                return;
            }
            var resultInfo = _tableItem.GetDataByUid(info.ResultItemUid);
            if (resultInfo == null)
            {
                GcLogger.LogError("강화 후 아이템 정보가 없습니다. item uid:"+info.ResultItemUid);
                return;
            }

            _struckTableItemUpgrade = info;

            UIIcon resultItemIcon = GetIconByIndex(ResultIconSlotIndex);
            if (resultItemIcon == null) return;
            resultItemIcon.SetDrag(false);
            resultItemIcon.ChangeInfoByUid(info.ResultItemUid, 1);

            if (textItemName != null)
            {
                textItemName.gameObject.SetActive(true);
                textItemName.text = sourceInfo.Name;
            }
            if (textRate != null)
            {
                textRate.gameObject.SetActive(true);
                textRate.text = $"Rate: {info.Rate}%"; // 강화 확률
            }
            if (textNeedCurrency != null)
            {
                textNeedCurrency.gameObject.SetActive(true);
                textNeedCurrency.text = $"{CurrencyConstants.GetNameByCurrencyType(info.NeedCurrencyType)}: {info.NeedCurrencyValue}";
            }

            if (textStatusID1 != null && (sourceInfo.StatusID1 != "" || resultInfo.StatusID1 != ""))
            {
                textStatusID1.gameObject.SetActive(true);
                textStatusID1.text = $"{GetStatusName(sourceInfo.StatusID1)} : {sourceInfo.StatusValue1} -> {resultInfo.StatusValue1}";
            }
            if (textStatusID2 != null && (sourceInfo.StatusID2 != "" || resultInfo.StatusID2 != ""))
            {
                textStatusID2.gameObject.SetActive(true); 
                textStatusID2.text =
                    $"{GetStatusName(sourceInfo.StatusID2)} : {sourceInfo.StatusValue2} -> {resultInfo.StatusValue2}";
            }

            SetMaterialInfo(0, info.NeedItemUid1, info.NeedItemCount1);
            SetMaterialInfo(1, info.NeedItemUid2, info.NeedItemCount2);
            SetMaterialInfo(2, info.NeedItemUid3, info.NeedItemCount3);
            SetMaterialInfo(3, info.NeedItemUid4, info.NeedItemCount4);
        }
        private string GetStatusName(string statusId)
        {
            if (string.IsNullOrEmpty(statusId)) return "";
            var info = _tableStatus.GetDataById(statusId);
            return info?.Name ?? "";
        }
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
            ShowTextResult(false);
            // 윈도우가 닫힐때
            UnRegisterAllIcons(uid);
            InitializeInfo();
        }
        /// <summary>
        /// 재료 정보 지워주기
        /// </summary>
        public void ClearMaterials()
        {
            foreach (var elementMaterial in _elementMaterials)
            {
                elementMaterial.gameObject.SetActive(false);
                elementMaterial.ClearInfo();
            }
        }

        private void OnClickUpgrade()
        {
            if (_struckTableItemUpgrade == null)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("Please select an item to enhance.");//"강화할 아이템을 선택해주세요."
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
            var resultCommon = SceneGame.saveDataManager.Player.CheckNeedCurrency(_struckTableItemUpgrade.NeedCurrencyType,
                _struckTableItemUpgrade.NeedCurrencyValue);
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
            if (_struckTableItemUpgrade.Rate <= 0)
            {
                GcLogger.LogError("item_upgrade 테이블에 확률값이 잘 못되었습니다. rate: "+_struckTableItemUpgrade.Rate);
                return;
            }
            // 인벤토리에 아이템 체크
            UIIcon icon = GetIconByIndex(SourceIconSlotIndex);
            var parent = icon.GetParentInfo();
            if (parent.Item1 == UIWindowConstants.WindowUid.None || parent.Item2 < 0)
            {
                GcLogger.LogError("인벤토리에 있는 아이템 정보가 잘 못 되었습니다.");
                return;
            }

            ShowTextResult(false);
            
            // 확률 계산
            _updateResult = false;
            int random = Random.Range(0, 100);
            if (random < _struckTableItemUpgrade.Rate)
            {
                _updateResult = true;
            }
#if GGEMCO_USE_SPINE            
            // 이펙트 실행
            if (effectItemUpgrade != null)
            {
                List<StruckAddAnimation> addAnimations = new List<StruckAddAnimation>
                {
                    new("play", false, 0, 1.0f),
                    new("play", false, 0, 1.0f),
                };
                TrackEntry entry = effectItemUpgrade.PlayAnimation("play", false, 1.0f, addAnimations);
                entry.Complete += OnAnimationComplete;
            }
            else
            {
                OnAnimationComplete();
            }
#else
            OnAnimationComplete();
#endif
        }
        /// <summary>
        /// 강화 연출 스파인 애니메이션이 종료된 후 UI에 결과를 반영합니다.
        /// </summary>
        /// <param name="e"></param>
#if GGEMCO_USE_SPINE           
        private void OnAnimationComplete(TrackEntry e = null)
#else
        private void OnAnimationComplete()
#endif
        {
            ShowTextResult(true);
            if (_updateResult)
            {
                textResult.text = "Success"; // "강화에 성공하였습니다.";
                textResult.color = Color.blue;
            }
            else
            {
                textResult.text = "Fail"; //"강화에 실패하였습니다.";
                textResult.color = Color.red;
            }
            // 인벤토리에 아이템 체크
            UIIcon icon = GetIconByIndex(SourceIconSlotIndex);
            var parent = icon.GetParentInfo();
            // 성공, 실패 체크 
            if (_updateResult)
            {
                // 강화 처리, inventoryData 에서 item uid 바꿔주기
                var resultUpgrade = _inventoryData.UpgradeItem(parent.Item2, _struckTableItemUpgrade.ResultItemUid);
                _uiWindowInventory.SetIcons(resultUpgrade);
            
                // 기존 정보에서 업그레이드 된 아이콘으로 다시 셋팅하기
                SceneGame.uIWindowManager.UnRegisterIcon(UIWindowConstants.WindowUid.ItemUpgrade, SourceIconSlotIndex);
            
                var inventoryIcon = _uiWindowInventory.GetIconByIndex(parent.Item2) as UIIconItem;
                if (inventoryIcon == null) return;
                if (inventoryIcon.GetUpgrade() >= _struckTableItemUpgrade.MaxUpgrade)
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("강화수치가 최대치 입니다.\n더이상 강화 할 수 없습니다.");
                    InitializeInfo();
                    return;
                }
                SceneGame.uIWindowManager.RegisterIcon(UIWindowConstants.WindowUid.Inventory, parent.Item2, uid, 1);
            }
            else
            {
                SceneGame.uIWindowManager.UnRegisterIcon(UIWindowConstants.WindowUid.ItemUpgrade, SourceIconSlotIndex);
                SceneGame.uIWindowManager.RegisterIcon(UIWindowConstants.WindowUid.Inventory, parent.Item2, uid, 1);
            }
        }

        public int GetResultIconSlotIndex() => ResultIconSlotIndex;
        public int GetSourceIconSlotIndex() => SourceIconSlotIndex;
    }
}