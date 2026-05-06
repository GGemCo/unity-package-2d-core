using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드 오브젝트 표시 상태 제어 대상을 선택하는 방식을 정의합니다.
    /// </summary>
    [Serializable]
    public enum WorldObjectVisibilityTargetMode
    {
        /// <summary>
        /// 현재 검색 범위 안의 모든 컷신 표시 대상에 적용합니다.
        /// </summary>
        All = 0,

        /// <summary>
        /// 지정한 그룹 키에 속한 대상에만 적용합니다.
        /// </summary>
        IncludeOnly = 1,

        /// <summary>
        /// 지정한 그룹 키를 제외한 모든 대상에 적용합니다.
        /// </summary>
        AllExcept = 2,
    }

    /// <summary>
    /// 월드 오브젝트 표시 상태를 실제로 적용하는 방식을 정의합니다.
    /// </summary>
    [Serializable]
    public enum WorldObjectVisibilityApplyMode
    {
        /// <summary>
        /// Renderer.enabled만 변경하여 오브젝트의 로직과 충돌 상태는 유지합니다.
        /// </summary>
        RendererOnly = 0,

        /// <summary>
        /// CutsceneVisibilityTarget이 붙은 GameObject의 활성 상태를 변경합니다.
        /// </summary>
        GameObjectActive = 1,
    }

    /// <summary>
    /// 컷신 중 월드 오브젝트의 표시 상태를 변경하기 위한 데이터입니다.
    /// </summary>
    [Serializable]
    public class WorldObjectVisibilityData
    {
        [Header("Target")]
        [Tooltip("표시 상태를 변경할 월드 오브젝트 대상을 선택하는 방식입니다.")]
        public WorldObjectVisibilityTargetMode targetMode = WorldObjectVisibilityTargetMode.IncludeOnly;

        [Tooltip("targetMode가 IncludeOnly일 때 표시 상태를 변경할 그룹 키 목록입니다.")]
        public List<string> targetGroupKeys = new List<string> { "Default" };

        [Tooltip("targetMode가 AllExcept일 때 표시 상태 변경에서 제외할 그룹 키 목록입니다.")]
        public List<string> exceptGroupKeys = new List<string>();

        [Tooltip("현재 맵 하위가 아니라 씬 전체에서 CutsceneVisibilityTarget을 검색할지 여부입니다.")]
        public bool searchEntireScene;

        [Tooltip("비활성 오브젝트에 포함된 CutsceneVisibilityTarget도 검색할지 여부입니다.")]
        public bool includeInactiveTargets = true;

        [Header("Visibility")]
        [Tooltip("On이면 보이게 하고 Off이면 숨깁니다.")]
        public bool show;

        [Tooltip("Renderer만 끌지, GameObject 활성 상태까지 변경할지 결정합니다.")]
        public WorldObjectVisibilityApplyMode applyMode = WorldObjectVisibilityApplyMode.RendererOnly;

        [Tooltip("클립 지속 시간이 끝났을 때 이전 표시 상태를 복원할지 여부입니다.")]
        public bool restoreOnStop;

        [Tooltip("컷신 종료 시 이전 표시 상태를 복원할지 여부입니다.")]
        public bool restoreOnCutsceneEnd = true;
    }
}
