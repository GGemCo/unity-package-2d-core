using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public static class EditorPopupUtils
    {
        /// <summary>
        /// Uid 기반 드롭다운을 그리고 선택된 값으로 Uid를 갱신합니다.
        /// 사용하려는 테이블의 구조체에 IUidName 를 상속받아야 합니다.
        /// </summary>
        public static void DrawUidPopup<T>(
            string label,
            ref int selectedIndex,
            List<string> nameList,
            Dictionary<int, T> dataDict,
            ref int targetUid,
            Rect rect,
            ref float y
        ) where T : IUidName // 사용하려는 테이블의 구조체에 IUidName 를 상속받아야 합니다.
        {
            if (targetUid > 0)
            {
                var i = targetUid;
                selectedIndex = nameList.FindIndex(x => x.Contains(i.ToString()));
                if (selectedIndex == -1) selectedIndex = 0;
            }

            selectedIndex = EditorGUI.Popup(new Rect(rect.x, y, rect.width, 18), label, selectedIndex, nameList.ToArray());
            if (dataDict.TryGetValue(selectedIndex, out var data))
            {
                targetUid = data.Uid;
            }
            else
            {
                targetUid = 0;
            }

            y += 20;
        }
    }
}