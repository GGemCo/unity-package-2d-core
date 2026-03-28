using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public enum UiWindowVisibilityMode
    {
        All = 0,
        IncludeOnly = 1,
        AllExcept = 2,
    }

    [Serializable]
    public class UiWindowVisibilityData
    {
        [Header("Target")]
        [Tooltip("어떤 윈도우 집합에 표시/숨김을 적용할지 결정합니다.")]
        public UiWindowVisibilityMode mode = UiWindowVisibilityMode.All;
        [Tooltip("mode가 IncludeOnly일 때 적용할 윈도우 목록입니다.")]
        public List<UIWindowConstants.WindowUid> targetWindows = new();
        [Tooltip("mode가 AllExcept일 때 제외할 윈도우 목록입니다.")]
        public List<UIWindowConstants.WindowUid> exceptWindows = new();

        [Header("Visibility")]
        [Tooltip("On이면 보이기, Off이면 숨기기입니다.")]
        public bool show;
        [Tooltip("이 이벤트가 종료될 때 저장해둔 이전 UI 표시 상태를 복원할지 여부입니다.")]
        public bool restoreOnStop = true;
        [Tooltip("컷신 종료 시 저장해둔 이전 UI 표시 상태를 복원할지 여부입니다.")]
        public bool restoreOnCutsceneEnd = true;
    }
}
