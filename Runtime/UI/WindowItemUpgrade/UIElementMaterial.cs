using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 강화 재료 element
    /// </summary>
    public class UIElementMaterial : MonoBehaviour
    {
        [Header("재료 아이콘")]
        public UIIcon uIIcon;
        [Header("재료 개수 텍스트")]
        public TextMeshProUGUI textItemCount;

        [Header("재료 개수 텍스트 색상")] public Color colorItemCount = Color.white;
        [Header("재료 개수 텍스트 부족할때 색상")] public Color colorItemCountLack = Color.red;
        private TableItem tableItem;
        private InventoryData inventoryData;

        private int currentItemUid;
        private int currentNeedCount;
        
        private UIWindow uiWindowParent;

        /// <summary>
        /// 초기 설정 해주고 active 해준다.
        /// </summary>
        /// <param name="itemUid"></param>
        /// <param name="needCount"></param>
        /// <param name="parentWindow"></param>
        public void InitializeSetInfo(int itemUid, int needCount, UIWindow parentWindow)
        {
            uiWindowParent = parentWindow;
            currentItemUid = itemUid;
            currentNeedCount = needCount;
            gameObject.SetActive(true);
        }
        /// <summary>
        /// 활성화 될때 이미지, 텍스트를 설정한다.
        /// </summary>
        public void OnEnable()
        {
            if (currentItemUid <= 0) return;
            tableItem = TableLoaderManager.Instance.TableItem;
            inventoryData = SceneGame.Instance.saveDataManager.Inventory;
            
            var info = tableItem.GetDataByUid(currentItemUid);
            if (info == null)
            {
                GcLogger.LogError("item 테이블에 정보가 없습니다. item uid : " + currentItemUid);
                return;
            }

            uIIcon.ChangeInfoByUid(currentItemUid, 1);
            uIIcon.window = uiWindowParent;
            uIIcon.windowUid = UIWindowManager.WindowUid.ItemUpgrade;
            uIIcon.SetDrag(false);
            uIIcon.SetClick(false);

            UpdateCount();
        }
        /// <summary>
        /// 강화 후 내가 가지고 있는 재료 count 업데이트 해주기
        /// </summary>
        private void UpdateCount()
        {
            if (textItemCount == null) return;
            textItemCount.color = colorItemCount;
            int haveCount = inventoryData.GetCountByItemUid(currentItemUid);
            if (haveCount < currentNeedCount)
            {
                textItemCount.color = colorItemCountLack;
            }

            textItemCount.text = $"{haveCount}/{currentNeedCount}";
        }

        public void ClearInfo()
        {
        }
        /// <summary>
        /// 필요한 재료 개수를 가지고 있는지 체크
        /// </summary>
        /// <returns></returns>
        public bool CheckHaveCount()
        {
            if (currentItemUid <= 0) return true;
            int haveCount = inventoryData.GetCountByItemUid(currentItemUid);
            return haveCount >= currentNeedCount;
        }
        /// <summary>
        /// 재료 item uid, 필요한 개수
        /// </summary>
        /// <returns></returns>
        public (int, int) GetItemUidCount()
        {
            return (currentItemUid, currentNeedCount);
        }
    }
}