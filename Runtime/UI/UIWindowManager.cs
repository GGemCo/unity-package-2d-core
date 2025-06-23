using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 윈도우 관리 매니저
    /// </summary>
    public class UIWindowManager : MonoBehaviour
    {
        // 윈도우 고유번호 
        public enum WindowUid 
        {
            None,
            Hud,
            Inventory,
            ItemInfo,
            Equip,
            PlayerInfo,
            ItemSplit,
            PlayerBuffInfo,
            QuickSlot,
            Skill,
            SkillInfo,
            InteractionDialogue,
            Shop,
            ItemBuy,
            Stash,
            ShopSale,
            ItemUpgrade,
            ItemSalvage,
            ItemCraft,
            Dialogue,
            HudQuest,
            QuestReward
        }
        [Header("기본속성")]
        [Tooltip("윈도우 리스트")]
        [SerializeField] private UIWindow[] uiWindows;

        private void Awake()
        {
            InitializationTableInfo();
        }
        /// <summary>
        /// 각 윈도우에 table 정보 연결하기
        /// </summary>
        private void InitializationTableInfo()
        {
            if (TableLoaderManager.Instance == null) return;
            TableWindow tableWindow = TableLoaderManager.Instance.TableWindow;
            var tables = tableWindow.GetDatas();
            if (tables == null) return;
            // datas를 Ordering 컬럼 기준으로 정렬된 새로운 Dictionary 만들기
            var orderedDatas = tables
                .OrderBy(kv => int.Parse(kv.Value["Ordering"])) // Ordering 값 기준으로 정렬
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var table in orderedDatas)
            {
                int uid = table.Key;
                if (uid == 0) continue;
                StruckTableWindow info = tableWindow.GetDataByUid(uid);
                if (info == null || info.Uid <= 0) continue;
                UIWindow window = uiWindows[uid].gameObject.GetComponent<UIWindow>();
                if (window == null) continue;
                window.SetTableWindow(info);
                window.transform.SetSiblingIndex(info.Ordering);
            }
        }
        /// <summary>
        /// 윈도우 보임/안보임 처리 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="show"></param>
        public void ShowWindow(WindowUid uid, bool show)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(uid);
            if (uiWindow == null) {
                GcLogger.LogError("UIWindow 컴포넌트가 없습니다. uid:"+uid);
                return;
            }

            uiWindow.Show(show);
        }
        /// <summary>
        /// 특정 윈도우에서 아이콘 가져오기
        /// </summary>
        /// <param name="srcWindowUid"></param>
        /// <param name="srcIndex"></param>
        /// <returns></returns>
        private UIIcon GetIconByWindowUid(WindowUid srcWindowUid, int srcIndex)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(srcWindowUid);
            if (uiWindow == null)
            {
                GcLogger.LogError("UIWindow 컴포넌트가 없습니다. uid:"+srcWindowUid);
                return null;
            }
            return uiWindow.GetIconByIndex(srcIndex);
        }
        /// <summary>
        /// UIWindow 찾기 
        /// </summary>
        /// <param name="windowUid"></param>
        /// <returns></returns>
        public T GetUIWindowByUid<T>(WindowUid windowUid) where T : UIWindow
        {
            if (uiWindows.Length <= (int)windowUid) return null;
            UIWindow uiWindow = uiWindows[(int)windowUid];
            if (uiWindow == null)
            {
                GcLogger.LogError("UIWindow 컴포넌트가 없습니다. uid:"+windowUid);
                return null;
            }

            return uiWindow as T;
        }
        /// <summary>
        /// 특정 윈도우에서 아이콘 지우기
        /// </summary>
        /// <param name="windowUid"></param>
        /// <param name="slotIndex"></param>
        public void RemoveIcon(WindowUid windowUid, int slotIndex)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(windowUid);
            if (uiWindow == null)
            {
                GcLogger.LogError("UIWindow 컴포넌트가 없습니다. uid:"+windowUid);
                return;
            }
            uiWindow.DetachIcon(slotIndex);
        }
        /// <summary>
        /// 윈도우가 활성화 되어있는지 체크
        /// </summary>
        /// <param name="windowUid"></param>
        /// <returns></returns>
        public bool IsShowByWindowUid(WindowUid windowUid)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(windowUid);
            if (uiWindow == null) return false;
            return uiWindow.gameObject.activeSelf;
        }

        public void MoveIcon(WindowUid fromWindowUid, int fromIndex, WindowUid toWindowUid, int toCount, int toIndex = -1)
        {
            UIWindow fromWindow = GetUIWindowByUid<UIWindow>(fromWindowUid);
            UIWindow toWindow = GetUIWindowByUid<UIWindow>(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:"+fromWindowUid+"/to window:"+toWindowUid);
                return;
            }
            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null)
            {
                GcLogger.LogError("from Icon 또는 to Icon 값이 잘 못 되었습니다. from Index:"+fromIndex);
                return;
            }
            int itemUid = fromIcon.uid;
            fromWindow.SetIconCount(fromIndex, fromIcon.uid, fromIcon.GetCount() - toCount);
            if (toIndex >= 0)
            {
                // 그 위치에 아이콘이 있으면 되돌려준다
                var icon = toWindow.GetIconByIndex(toIndex);
                if (icon != null && icon.uid > 0 && icon.GetCount() > 0)
                {
                    fromWindow.SetIconCount(icon.uid, icon.GetCount());
                }
                toWindow.SetIconCount(toIndex, itemUid, toCount);
            }
            else
            {
                toWindow.SetIconCount(itemUid, toCount);    
            }
        }
        /// <summary>
        /// 등록 해제하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toWindowUid"></param>
        public void UnRegisterIcon(WindowUid fromWindowUid, int fromIndex, WindowUid toWindowUid = WindowUid.Inventory)
        {
            UIWindow fromWindow = GetUIWindowByUid<UIWindow>(fromWindowUid);
            UIWindow toWindow = GetUIWindowByUid<UIWindow>(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:"+fromWindowUid+"/to window:"+toWindowUid);
                return;
            }
            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null)
            {
                GcLogger.LogError("from Icon 또는 to Icon 값이 잘 못 되었습니다. from Index:"+fromIndex);
                return;
            }
            var info = fromIcon.GetParentInfo();
            WindowUid parentWindowUid = info.Item1;
            if (parentWindowUid == WindowUid.None) return;
            int parentIconIndex = info.Item2;
            UIWindow parent = GetUIWindowByUid<UIWindow>(parentWindowUid);
            var parentIcon = parent.GetIconByIndex(parentIconIndex);
            if (parentIcon != null)
            {
                parentIcon.SetIconLock(false);
            }
            
            fromWindow.DetachIcon(fromIndex);
            
        }
        /// <summary>
        /// 아이콘 등록하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toWindowUid"></param>
        /// <param name="toCount"></param>
        /// <param name="toIndex"></param>
        public void RegisterIcon(WindowUid fromWindowUid, int fromIndex, WindowUid toWindowUid, int toCount, int toIndex = -1)
        {
            UIWindow fromWindow = GetUIWindowByUid<UIWindow>(fromWindowUid);
            UIWindow toWindow = GetUIWindowByUid<UIWindow>(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:"+fromWindowUid+"/to window:"+toWindowUid);
                return;
            }
            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null)
            {
                GcLogger.LogError("from Icon 또는 to Icon 값이 잘 못 되었습니다. from Index:"+fromIndex);
                return;
            }

            fromIcon.SetIconLock(true);
            int itemUid = fromIcon.uid;
            
            if (toIndex >= 0)
            {
                // 그 위치에 아이콘이 있으면 되돌려준다
                var icon = toWindow.GetIconByIndex(toIndex);
                if (icon != null && icon.uid > 0 && icon.GetCount() > 0)
                {
                    fromWindow.SetIconCount(icon.uid, icon.GetCount());
                }
                UIIcon uiIcon = toWindow.SetIconCountReturnIcon(toIndex, itemUid, toCount);
                if (uiIcon != null)
                {
                    uiIcon.SetParentInfo(fromWindowUid, fromIndex);
                }
            }
            else
            {
                UIIcon uiIcon = toWindow.SetIconCountReturnIcon(itemUid, toCount);
                if (uiIcon != null)
                {
                    uiIcon.SetParentInfo(fromWindowUid, fromIndex);
                }
            }
        }
        /// <summary>
        /// 모든 윈도우 닫기
        /// </summary>
        /// <param name="exceptWindowUids">제외할 윈도우 uid</param>
        public void CloseAll(List<WindowUid> exceptWindowUids = null)
        {
            foreach (var window in uiWindows)
            {
                if (window == null) continue;
                if (window.GetDefaultActive()) continue;
                if (!window.gameObject.activeSelf) continue;
                if (exceptWindowUids is { Count: > 0 } && exceptWindowUids.Contains(window.uid)) continue;
                window.Show(false);
            }
        }
        /// <summary>
        /// UIWindow 스크립트 추가하기
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        private UIWindow AddUIComponent(string className)
        {
            if (className == "") return null;
            GameObject go = GameObject.Find(className);
            if (go == null)
            {
                GcLogger.LogError($"{className} 게임 오브젝트를 찾지 못 했습니다.");
                return null;
            }

            // 문자열 → Type
            Type type = Type.GetType($"GGemCo.Scripts.{className}");
            if (type == null)
            {
                GcLogger.LogError($"{className} 스크립트를 찾지 못 했습니다. 네임스페이스 설정을 확인해주세요.");
                return null;
            }

            // AddComponent(Type)
            if (go.GetComponent(type) == null)
            {
                // go.AddComponent(type);
                GcLogger.LogError($"{className} 컴포넌트가 없습니다.");
                return null;
            }
            else
            {
                // GcLogger.Log($"{className} already has component {className}");
            }
            return go.GetComponent<UIWindow>();
        }
        private void OnDestroy()
        {
            
        }
    }
}
