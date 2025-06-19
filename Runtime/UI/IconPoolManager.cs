using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이콘 pool 매니저
    /// 아이콘 생성, 세팅, 클리어
    /// </summary>
    public class IconPoolManager
    {
        private readonly UIWindow window;
        private ISlotIconBuildStrategy buildStrategy;
        private ISetIconHandler setIconHandler;

        public IconPoolManager(UIWindow window)
        {
            this.window = window;
            // 기본 전략
            buildStrategy = new DefaultSlotIconBuildStrategy();
        }
        /// <summary>
        /// 별도 아이콘 생성 전략 설정
        /// </summary>
        /// <param name="strategy"></param>
        public void SetBuildStrategy(ISlotIconBuildStrategy strategy)
        {
            buildStrategy = strategy;
        }
        /// <summary>
        /// 아이콘 세팅 핸들러 설정
        /// </summary>
        /// <param name="handler"></param>
        public void SetSetIconHandler(ISetIconHandler handler)
        {
            setIconHandler = handler;
        }
        public void Initialize()
        {
            window.slots = new GameObject[window.maxCountIcon];
            window.icons = new GameObject[window.maxCountIcon];

            buildStrategy?.BuildSlotsAndIcons(window, window.containerIcon, window.maxCountIcon,
                window.iconType, window.slotSize, window.iconSize, window.slots, window.icons);
        }
        /// <summary>
        /// slot index 로 아이콘 가져오기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public UIIcon GetIcon(int index) => window.icons[index]?.GetComponent<UIIcon>();
        public UISlot GetSlot(int index) => window.slots[index]?.GetComponent<UISlot>();
        /// <summary>
        /// icon uid 로 아이콘 가져오기
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public UIIcon GetIconByUid(int uid)
        {
            if (window.icons.Length == 0)
            {
                GcLogger.LogError("아이콘이 없습니다.");
                return null;
            }
            foreach (var icon in window.icons)
            {
                var uiIcon = icon?.GetComponent<UIIcon>();
                if (uiIcon?.uid == uid)
                    return uiIcon;
            }
            return null;
        }
        /// <summary>
        /// 아이콘 지우기
        /// </summary>
        /// <param name="slotIndex"></param>
        public void DetachIcon(int slotIndex)
        {
            if (window.icons.Length <= 0) return;
            var icon = window.icons[slotIndex];
            var uiIcon = icon?.GetComponent<UIIcon>();
            if (uiIcon != null)
            {
                uiIcon.ClearIconInfos();
            }
            
            // 아이콘 정보 세팅 후, 전략으로 후처리
            setIconHandler?.OnDetachIcon(window, slotIndex);
        }
        /// <summary>
        /// 아이콘 셋팅하기
        /// </summary>
        /// <param name="slotIndex">슬롯 index</param>
        /// <param name="uid">고유번호</param>
        /// <param name="count">개수</param>
        /// <param name="level">레벨</param>
        /// <param name="learn">배우기 여부 Y/N</param>
        /// <returns></returns>
        public UIIcon SetIcon(int slotIndex, int uid, int count, int level = 0, bool learn = false)
        {
            GameObject icon = window.icons[slotIndex];
            if (icon == null)
            {
                GcLogger.LogError("슬롯에 아이콘이 없습니다. slot index: " +slotIndex);
                return null;
            }
            UIIcon uiIcon = icon.GetComponent<UIIcon>();
            if (uiIcon == null)
            {
                GcLogger.LogError("슬롯에 UIIcon 이 없습니다. slot index: " +slotIndex);
                return null;
            }
            if (uiIcon == null) return null;

            if (count <= 0)
            {
                DetachIcon(slotIndex);
                return null;
            }
            uiIcon.window = window;
            uiIcon.windowUid = window.uid;
            uiIcon.ChangeInfoByUid(uid, count, level, learn);
            
            // 아이콘 정보 세팅 후, 전략으로 후처리
            setIconHandler?.OnSetIcon(window, slotIndex, uid, count, level, learn);
            return uiIcon;
        }
        /// <summary>
        /// 모든 icon Un Register 처리 하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="toWindowUid"></param>
        public void UnRegisterAllIcons(UIWindowManager.WindowUid fromWindowUid, UIWindowManager.WindowUid toWindowUid = UIWindowManager.WindowUid.Inventory)
        {
            foreach (var icon in window.icons)
            {
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null || uiIcon.uid <= 0 || uiIcon.GetCount() <= 0) continue;
                SceneGame.Instance.uIWindowManager.UnRegisterIcon(fromWindowUid, uiIcon.slotIndex, toWindowUid);
            }
        }
    }
}
