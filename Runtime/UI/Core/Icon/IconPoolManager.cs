using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 pool 매니저
    /// 아이콘 생성, 세팅, 클리어
    /// </summary>
    public class IconPoolManager
    {
        private UIWindow _window;
        private ISlotIconBuildStrategy _buildStrategy;
        private ISetIconHandler _setIconHandler;

        /// <summary>
        /// Awake 호출
        /// </summary>
        /// <param name="window"></param>
        public void Initialize(UIWindow window)
        {
            _window = window;
            // 기본 전략
            _buildStrategy = new DefaultSlotIconBuildStrategy();
            
            // 커스텀 전략 설정 지점
            var strategy = GetSlotIconBuildStrategy();
            if (strategy != null)
                SetBuildStrategy(strategy);

            // 사용하지 않는 index 가 있을 수 있으므로 미리 만들어 두어야 건너 띄어도 문제가 없다.
            // maxCountIcon 이 0 일때, 예외처리
            if (_window.maxCountIcon == 0 && _window.preLoadSlots.Length > 0)
            {
                _window.maxCountIcon = _window.preLoadSlots.Length;
            }
            _window.slots = new GameObject[_window.maxCountIcon];
            _window.icons = new GameObject[_window.maxCountIcon];

            _buildStrategy?.BuildSlotsAndIcons(_window, _window.containerIcon, _window.maxCountIcon,
                _window.iconType, _window.slotSize, _window.iconSize, _window.slots, _window.icons);
            _window.RefreshInactiveSlotStates();
        }
        /// <summary>
        /// 별도 아이콘 생성 전략 설정
        /// </summary>
        /// <param name="strategy"></param>
        private void SetBuildStrategy(ISlotIconBuildStrategy strategy)
        {
            _buildStrategy = strategy;
        }
        /// <summary>
        /// 아이콘 세팅 핸들러 설정
        /// </summary>
        /// <param name="handler"></param>
        public void SetSetIconHandler(ISetIconHandler handler)
        {
            _setIconHandler = handler;
        }

        /// <summary>
        /// 커스텀 빌드 전략을 반환.
        /// 우선순위:
        /// 1) PreLoad 전용 전략
        /// 2) Registry 에 등록된 외부 전략 (다른 패키지 포함)
        /// 3) Core 내부 기본 매핑 (Skill, ItemSalvage 등)
        /// </summary>
        private ISlotIconBuildStrategy GetSlotIconBuildStrategy()
        {
            if (_window == null)
                return null;

            // 1) PreLoadSlots 를 사용하는 경우는 예외적으로 고정 전략 사용
            // if (_window.preLoadSlots != null && _window.preLoadSlots.Length > 0)
            //     return new SlotIconBuildStrategyPreLoad();

            // 2) 레지스트리에 등록된 외부/커스텀 전략 우선 사용
            var registered = SlotIconBuildStrategyRegistry.Create(_window);
            if (registered != null)
                return registered;

            // 3) Core 패키지에 하드코딩된 기본 전략 (기존 코드 유지)
            return _window.uid switch
            {
                UIWindowConstants.WindowUid.ItemSalvage => new SlotIconBuildStrategyItemSalvage(),
                // UIWindowConstants.WindowUid.QuestReward => new SlotIconBuildStrategyQuestReward(),
                _                                       => null,
            };
        }
        /// <summary>
        /// slot index 로 아이콘 가져오기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public UIIcon GetIcon(int index)
        {
            foreach (var gameObjectIcon in _window.icons)
            {
                UIIcon uiIcon = gameObjectIcon?.GetComponent<UIIcon>();
                if (uiIcon != null && uiIcon.index == index) return uiIcon;
            }
            GcLogger.LogError($"{_window}에 {nameof(UIIcon)}이 없습니다. index: {index}");
            return null;
        }
        public UISlot GetSlot(int index)
        {
            foreach (var gameObjectSlot in _window.slots)
            {
                UISlot uiSlot = gameObjectSlot?.GetComponent<UISlot>();
                if (uiSlot != null && uiSlot.Index == index) return uiSlot;
            }
            GcLogger.LogError($"{_window}에 {nameof(UISlot)}이 없습니다. index: {index}");
            return null; 
        }
        /// <summary>
        /// icon uid 로 아이콘 가져오기
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public UIIcon GetIconByUid(int uid)
        {
            if (_window.icons.Length == 0)
            {
                GcLogger.LogError($"{nameof(_window.icons)} 개수가 0 입니다.");
                return null;
            }
            foreach (var icon in _window.icons)
            {
                var uiIcon = icon?.GetComponent<UIIcon>();
                if (uiIcon?.uid == uid)
                    return uiIcon;
            }
            GcLogger.LogError($"{_window}에 {nameof(UIIcon)}이 없습니다. uid: {uid}");
            return null;
        }
        /// <summary>
        /// 아이콘 정보 지울때만 호출. Move는 둘다 바로 Set하기 때문에 DetachIcon 호출되지 않음
        /// </summary>
        /// <param name="slotIndex"></param>
        public void DetachIcon(int slotIndex)
        {
            if (_window.icons.Length <= 0) return;
            var uiIcon = GetIcon(slotIndex);
            IconConstants.Type iconType = IconConstants.Type.None;
            if (uiIcon != null)
            {
                iconType = uiIcon.GetIconType();
                uiIcon.ClearIconInfos();
            }
            
            // 선택 표시 지워주기
            _window.RemoveSelectedIcon();
            
            if (QuickSlotSetIconStrategyRegistry.TryGet(iconType, out var strategy))
            {
                strategy.OnDetachIcon(_window, slotIndex);
                return;
            }
            // 아이콘 정보 세팅 후, 전략으로 후처리
            _setIconHandler?.OnDetachIcon(_window, slotIndex);
        }
        /// <summary>
        /// 아이콘 셋팅하기
        /// </summary>
        /// <param name="slotIndex">슬롯 index</param>
        /// <param name="uid">고유번호</param>
        /// <param name="count">개수</param>
        /// <param name="level">레벨</param>
        /// <param name="learn">배우기 여부 Y/N</param>
        /// <param name="instanceId">랜덤 아이템 옵션 고유번호</param>
        /// <param name="iconType">아이콘 타입 변경할 경우 입력</param>
        /// <returns></returns>
        public UIIcon SetIcon(int slotIndex, int uid, int count, int level = 0, bool learn = false, long instanceId = 0, IconConstants.Type iconType = IconConstants.Type.None)
        {
            UIIcon uiIcon = GetIcon(slotIndex);
            
            if (GcLogger.IsNull(uiIcon, "")) return null;

            if (_window.IsSlotInactive(slotIndex))
            {
                uiIcon.SetInactiveState(true);
                return null;
            }

            if (count <= 0)
            {
                DetachIcon(slotIndex);
                return null;
            }
            uiIcon.window = _window;
            uiIcon.windowUid = _window.uid;
            uiIcon.ChangeInfoByUid(uid, count, level, learn, 0, instanceId, iconType);
            
            if (QuickSlotSetIconStrategyRegistry.TryGet(iconType, out var strategy))
            {
                strategy.OnSetIcon(_window, slotIndex, uid, count, level, learn, iconType);
                return uiIcon;
            }
            // 아이콘 정보 세팅 후, 전략으로 후처리
            _setIconHandler?.OnSetIcon(_window, slotIndex, uid, count, level, learn, iconType);
            return uiIcon;
        }
        /// <summary>
        /// 모든 icon Un Register 처리 하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="toWindowUid"></param>
        public void UnRegisterAllIcons(UIWindowConstants.WindowUid fromWindowUid, UIWindowConstants.WindowUid toWindowUid = UIWindowConstants.WindowUid.Inventory)
        {
            foreach (var icon in _window.icons)
            {
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null || uiIcon.uid <= 0 || uiIcon.GetCount() <= 0) continue;
                SceneGame.Instance.uIWindowManager.UnRegisterIcon(fromWindowUid, uiIcon.slotIndex, toWindowUid);
            }
        }
        /// <summary>
        /// 빈 슬롯 찾기
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < _window.maxCountIcon; i++)
            {
                if (_window.IsSlotInactive(i))
                    continue;

                UIIcon uiIcon = GetIcon(i);
                if (uiIcon == null) continue;
                if (uiIcon.uid <= 0 || uiIcon.GetCount() <= 0)
                    return i;
            }
            return -1;
        }
        
        public void SetIconCount(int iconUid, int iconCount, long instanceId = 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                SceneGame.Instance.popupManager.ShowPopupError("Window_NoEmptySpace");//"윈도우에 빈 공간이 없습니다."
                return;
            }
            SetIcon(emptySlot, iconUid, iconCount, 0, false, instanceId);
        }

        public UIIcon SetIconCountReturnIcon(int iconUid, int iconCount, long instanceId = 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                SceneGame.Instance.popupManager.ShowPopupError("Window_NoEmptySpace");//"윈도우에 빈 공간이 없습니다."
                return null;
            }
            return SetIcon(emptySlot, iconUid, iconCount, 0, false, instanceId);
        }

        public void ResetMaxCountIcon(int maxCountIcon)
        {
            if (_window.slots.Length > 0)
            {
                foreach (var slot in _window.slots)
                {
                    Object.Destroy(slot.gameObject);
                }
                _window.slots = null;
            }
            if (_window.icons.Length > 0)
            {
                foreach (var icon in _window.icons)
                {
                    Object.Destroy(icon.gameObject);
                }
                _window.icons = null;
            }
            _window.slots = new GameObject[maxCountIcon];
            _window.icons = new GameObject[maxCountIcon];
            
            _buildStrategy?.BuildSlotsAndIcons(_window, _window.containerIcon, maxCountIcon,
                _window.iconType, _window.slotSize, _window.iconSize, _window.slots, _window.icons);
            _window.RefreshInactiveSlotStates();
        }

        public void SetIcons(ResultCommon result)
        {
            if (result.ResultIcons == null || result.ResultIcons.Count <= 0) return;
            foreach (var icon in result.ResultIcons)
            {
                var iconType = (IconConstants.Type)icon.IconType;
                SetIcon(icon.SlotIndex, icon.Uid, icon.Count, icon.Level, icon.IsLearned, icon.InstanceId, iconType);
            }
        }

        public void RemoveAndDetachIcon()
        {
            var uIWindowManager = SceneGame.Instance.uIWindowManager;
            foreach (var icon in _window.icons)
            {
                UIIconItem uiIconItem = icon.GetComponent<UIIconItem>();
                if (uiIconItem == null || uiIconItem.uid <= 0 || uiIconItem.GetCount() <= 0) continue;
                var parentInfo = uiIconItem.GetParentInfo();
                // 등록 되었던것을 빼준다.
                DetachIcon(uiIconItem.slotIndex);
                // 인벤토리에서 지워준다.
                if (parentInfo.Item1 != UIWindowConstants.WindowUid.None)
                {
                    uIWindowManager.RemoveIcon(parentInfo.Item1, parentInfo.Item2);
                }
            }
        }

        public void DetachAllIcons()
        {
            foreach (var icon in _window.icons)
            {
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (!uiIcon) continue;
                DetachIcon(uiIcon.slotIndex);
            }
        }
    }
}
