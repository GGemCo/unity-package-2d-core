using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 구매 윈도우
    /// </summary>
    public class UIWindowShop : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("상점 Element 프리팹")]
        public GameObject prefabUIElementShop;
        
        private TableShop _tableShop;
        private readonly Dictionary<int, UIElementShop> _uiElementShops = new Dictionary<int, UIElementShop>();
        private int _currentShopUid;
        
        protected override void Awake()
        {
            _uiElementShops.Clear();
            uid = UIWindowConstants.WindowUid.Shop;
            if (TableLoaderManager.Instance == null) return;
            _tableShop = TableLoaderManager.Instance.TableShop;
            base.Awake();
        }
        /// <summary>
        /// 상점 uid 로 ui element shop 정보 셋팅하기
        /// </summary>
        public void SetInfoByShopUid(int shopUid)
        {
            // 같은 상점을 열었으면 업데이트 하지 않는다
            if (_currentShopUid > 0 && _currentShopUid == shopUid) return;
            // 기존 element 지우기
            int index = 0;
            foreach (var data in _uiElementShops)
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
            _uiElementShops.Clear();
            _currentShopUid = shopUid;
            
            if (AddressableLoaderSettings.Instance == null || containerIcon == null) return;
            if (prefabUIElementShop == null)
            {
                GcLogger.LogError("UIElementShop 프리팹이 없습니다.");
                return;
            }
            if (shopUid <= 0) return;
            var datas = _tableShop.GetItemByUid(shopUid);
            if (datas == null)
            {
                GcLogger.LogError("shop 테이블에 정보가 없습니다. shop Uid: " + shopUid);
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
                if (prefabUIElementShop != null)
                {
                    parent = Instantiate(prefabUIElementShop, containerIcon.gameObject.transform);
                    if (parent == null) continue;
                    UIElementShop uiElementShop = parent.GetComponent<UIElementShop>();
                    if (uiElementShop == null) continue;
                    uiElementShop.Initialize(this, index, info);
                    uiElementShop.UpdateInfos(datas[index]);
                    _uiElementShops.TryAdd(index, uiElementShop);
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
                uiIcon.ChangeInfoByUid(info.ItemUid, 1);
                // element 에서 마우스 이벤트 처리
                uiIcon.SetRaycastTarget(false);
                uiIcon.RemoveLockImage();
                
                icons[index] = icon;
                index++;
            }
            // GcLogger.Log($"풀 확장: {amount}개 아이템 추가 (총 {poolDropItem.Count}개)");
        }
        /// <summary>
        /// 슬롯 위치 정해주기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="index"></param>
        private void SetPositionUiSlot(UISlot slot, int index)
        {
            UIElementShop uiElementSkill = _uiElementShops[index];
            if (uiElementSkill == null) return;
            Vector3 position = uiElementSkill.GetIconPosition();
            if (position == Vector3.zero) return;
            slot.transform.localPosition = position;
        }
        /// <summary>
        /// npc uid 정보로 아이콘 셋팅하기
        /// 상점이 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = _tableShop.GetItemByUid(_currentShopUid);
            if (datas == null) return;
            for (int index = 0; index < maxCountIcon; index++)
            {
                if (index >= icons.Length) continue;
                var icon = icons[index];
                if (icon == null) continue;
                UIIconItem uiIcon = icon.GetComponent<UIIconItem>();
                if (uiIcon == null) continue;
                
                var info = TableLoaderManager.Instance.GetItemData(datas[index].ItemUid);
                if (info == null) continue;
                uiIcon.ChangeInfoByUid(info.Uid, 1);
                UIElementShop uiElementShop = _uiElementShops[index];
                if (uiElementShop != null)
                {
                    uiElementShop.UpdateInfos(datas[index]);
                }
            }
        }

        /// <summary>
        /// 상점 Uid 로 윈도우 오픈하기
        /// </summary>
        /// <param name="shopUid"></param>
        public void ShowByUid(int shopUid)
        {
            if (shopUid <= 0) return;
            SetInfoByShopUid(shopUid);
            Show(true);
        }
    }
}