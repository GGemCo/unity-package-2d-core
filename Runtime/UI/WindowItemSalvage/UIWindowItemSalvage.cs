using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 분해하기 윈도우
    /// </summary>
    public class UIWindowItemSalvage : UIWindow
    {
        [Header("기본 속성")] [Tooltip("분해 아이콘 개수. Max Count Icon 개수에서 이 개수를 뺀 나머지가 결과 아이콘 개수")]
        public int salvageIconCount;
        [Tooltip("분해 결과 아이콘 conatiner")] public GameObject containerResult;
        [Tooltip("분해에 필요한 총 골드 량")] public TextMeshProUGUI textTotalGold;
        [Tooltip("분해에 필요한 총 실버 량")] public TextMeshProUGUI textTotalSilver;
        [Tooltip("분해하기 버튼")] public Button buttonSalvage;

        private TableItemSalvage tableItemSalvage;
        private UIWindowItemInfo uiWindowItemInfo;
        private UIWindowInventory uiWindowInventory;
        
        private readonly BehaviorSubject<int> totalPriceGold = new(0);
        private readonly BehaviorSubject<int> totalPriceSilver = new(0);
        private readonly Dictionary<int, int> resultItems = new Dictionary<int, int>();
        protected override void Awake()
        {
            resultItems.Clear();
            uid = UIWindowManager.WindowUid.ItemSalvage;
            base.Awake();

            totalPriceGold.DistinctUntilChanged()
                .CombineLatest(totalPriceSilver, (_, _) => Unit.Default)
                .Subscribe(_ => SetTotalPriceText())
                .AddTo(this);
            SetSetIconHandler(new SetIconHandlerItemSalvage());
            DragDropHandler.SetStrategy(new DragDropStrategyItemSalvage());
            InitializeInfo();
            if (buttonSalvage != null)
            {
                buttonSalvage.onClick.RemoveAllListeners();
                buttonSalvage.onClick.AddListener(OnClickSalvage);
            }
        }
        protected override void Start()
        {
            base.Start();
            tableItemSalvage = TableLoaderManager.Instance.TableItemSalvage;
            uiWindowItemInfo = 
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowManager.WindowUid
                    .ItemInfo);
            uiWindowInventory = 
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid
                    .Inventory);
        }

        /// <summary>
        /// 분해하기 container 에 아이콘 비어 있는 자리가 있는지 체크 
        /// </summary>
        /// <returns></returns>
        public bool CheckSalvagePossibleCount()
        {
            int empty = 0;
            foreach (var icon in icons)
            {
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon != null && uiIcon.slotIndex < salvageIconCount && uiIcon.uid <= 0)
                {
                    empty++;
                }
            }

            return empty > 0;
        }

        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            SceneGame.uIWindowManager.UnRegisterIcon(uid, icon.slotIndex);
            UpdateResultInfos();
        }

        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            if (show) return;
            UnRegisterAllIcons(uid);
            // 분해 결과 아이콘 지워주기
            DetachAllIcons();
        }

        private void InitializeInfo()
        {
            totalPriceGold.OnNext(0);
            totalPriceSilver.OnNext(0);
        }
        /// <summary>
        /// 분해할 아이콘이 등록될때, 분해결과 아이콘 갱신하기 
        /// </summary>
        public void UpdateResultInfos()
        {
            int totalGold = 0;
            int totalSilver = 0;
            resultItems.Clear();
            foreach (var icon in icons)
            {
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null || uiIcon.uid <= 0 || uiIcon.GetCount() <= 0) continue;
                // 분해 결과 icon 정보는 지워준다.
                // Detach 를 하면 UpdateResultInfos 함수가 호출되버린다.
                if (uiIcon.slotIndex >= salvageIconCount)
                {
                    uiIcon.ClearIconInfos();
                    continue;
                }
                var info = tableItemSalvage.GetDataBySourceItemUid(uiIcon.uid);
                if (info == null)
                {
                    GcLogger.LogError("분해 테이블에 정보가 없습니다. item uid:" + uiIcon.uid);
                    return;
                }
                // 분해 비용 계산하기
                if (info.NeedCurrencyType == CurrencyConstants.Type.Gold)
                {
                    totalGold += info.NeedCurrencyValue;
                }
                else if (info.NeedCurrencyType == CurrencyConstants.Type.Silver)
                {
                    totalSilver += info.NeedCurrencyValue;
                }
                // 분해 결과 아이템 저장
                if (!resultItems.ContainsKey(info.ResultItemUid))
                {
                    resultItems.TryAdd(info.ResultItemUid, info.ResultItemCount);
                }
                else
                {
                    resultItems[info.ResultItemUid] += info.ResultItemCount;
                }
            }
            // 분해 결과 icon 정보 업데이트 하기
            int resultIconSlotIndex = salvageIconCount;
            foreach (var info in resultItems)
            {
                int itemUid = info.Key;
                int itemCount = info.Value;
                UIIcon uiIcon = GetIconByIndex(resultIconSlotIndex);
                uiIcon.ChangeInfoByUid(itemUid, itemCount);
                uiIcon.SetDrag(false);
                resultIconSlotIndex++;
            }
            totalPriceGold.OnNext(totalGold);
            totalPriceSilver.OnNext(totalSilver);
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
        /// 분해 비용 텍스트 처리
        /// </summary>
        private void SetTotalPriceText()
        {
            textTotalGold.text = $"{CurrencyConstants.GetNameGold()}: {totalPriceGold.Value}";
            textTotalSilver.text = $"{CurrencyConstants.GetNameSilver()}: {totalPriceSilver.Value}";
        }
        /// <summary>
        /// 분해하기
        /// </summary>
        private void OnClickSalvage()
        {
            // 먼저 필요한 재화 모두 체크한다
            Dictionary<CurrencyConstants.Type, int> needCurrency = new Dictionary<CurrencyConstants.Type, int>
            {
                { CurrencyConstants.Type.Gold, totalPriceGold.Value },
                { CurrencyConstants.Type.Silver, totalPriceSilver.Value }
            };
            ResultCommon resultCommon = SceneGame.saveDataManager.Player.CheckNeedCurrency(needCurrency);
            if (resultCommon.Code == ResultCommon.Type.Fail)
            {
                SceneGame.systemMessageManager.ShowMessageWarning(resultCommon.Message);
                return;
            }
            // 재화 사용처리 하기
            SceneGame.saveDataManager.Player.MinusCurrency(CurrencyConstants.Type.Gold, totalPriceGold.Value);
            SceneGame.saveDataManager.Player.MinusCurrency(CurrencyConstants.Type.Silver, totalPriceSilver.Value);

            // 결과 아이템 인벤토리에 넣어주기
            foreach (var info in resultItems)
            {
                int itemUid = info.Key;
                int itemCount = info.Value;
                resultCommon = SceneGame.saveDataManager.Inventory.AddItem(itemUid, itemCount);
                uiWindowInventory.SetIcons(resultCommon);
            }
            
            RemoveAndDetachIcon();
        }
    }
}