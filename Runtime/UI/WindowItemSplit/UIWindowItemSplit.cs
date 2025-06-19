using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 인벤토리 아이템 나누기 윈도우
    /// </summary>
    public class UIWindowItemSplit : UIWindow
    {
        [Header("기본오브젝트")]
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textItemName;
        [Tooltip("아이템 개수")]
        public TextMeshProUGUI textItemCount;
        [Tooltip("나누기 슬라이드")]
        public Slider sliderSplit;
        [Tooltip("나누기 버튼")]
        public Button buttonConfirm;
        [Tooltip("취소 버트")]
        public Button buttonCancel;

        // 내가 가지고 있는 아이템 개수
        private int maxItemCount;
        // 나눌려고 하는 아이템 uid
        private int itemUid;
        // 나눌려고 하는 아이템 개수
        private int splitItemCount;
        
        private UIWindowInventory uiWindowInventory;
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.ItemSplit;
            base.Awake();
            buttonConfirm.onClick.RemoveAllListeners();
            buttonCancel.onClick.RemoveAllListeners();
            sliderSplit.onValueChanged.RemoveAllListeners();
            buttonConfirm.onClick.AddListener(OnClickConfirm);
            buttonCancel.onClick.AddListener(OnClickCancel);
            sliderSplit.onValueChanged.AddListener(OnValueChanged);
            SetSetIconHandler(new SetIconHandlerItemSplit());
        }
        protected override void Start()
        {
            base.Start();
            
            uiWindowInventory =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid
                    .Inventory);
        }
        /// <summary>
        /// Set Icon 되었을때 정보 업데이트 해주기
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        public void UpdateInfo(int iconUid, int iconCount)
        {
            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(iconUid);
            if (info == null) return;
            itemUid = iconUid;
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
            splitItemCount = (int)(maxItemCount * value);
            if (splitItemCount == 0)
            {
                splitItemCount = 1;
                sliderSplit.value = (float)splitItemCount / maxItemCount;
            }
            textItemCount.text = $"{splitItemCount} / {maxItemCount}";
        }
        /// <summary>
        /// 아이템 나누기
        /// </summary>
        private void OnClickConfirm()
        {
            if (uiWindowInventory == null) return;
            var icon = GetIconByIndex(0);
            if (icon == null) return;
            var infoParent = icon.GetParentInfo();
            if (infoParent.Item1 == UIWindowManager.WindowUid.None)
            {
                GcLogger.LogError("부모 아이콘 정보가 없습니다.");
                return;
            }
            int splitItemIndex = infoParent.Item2;
            ResultCommon result = SceneGame.Instance.saveDataManager.Inventory.SplitItem(splitItemIndex, itemUid, maxItemCount, splitItemCount);
            uiWindowInventory.SetIcons(result);
            Show(false);
        }

        private void OnClickCancel()
        {
            Show(false);
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