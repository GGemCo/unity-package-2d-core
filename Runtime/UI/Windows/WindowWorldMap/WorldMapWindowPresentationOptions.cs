using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 윈도우가 어떤 목적의 화면으로 표시되는지 구분합니다.
    /// </summary>
    public enum WorldMapWindowMode
    {
        /// <summary>일반 월드맵 화면입니다.</summary>
        Default = 0,

        /// <summary>휴식처 간 워프를 위한 월드맵 화면입니다.</summary>
        Warp = 1,

        /// <summary>Inspector에서 직접 조합한 사용자 정의 표시 정책입니다.</summary>
        Custom = 2,

        /// <summary>맵 클리어 종료 흐름에서 표시되는 월드맵 화면입니다.</summary>
        MapClearExit = 3,
    }

    /// <summary>
    /// 월드맵 노드 타입을 Inspector에서 다중 선택하기 위한 플래그입니다.
    /// </summary>
    [Flags]
    public enum WorldMapNodeTypeFilter
    {
        /// <summary>어떤 노드 타입도 포함하지 않습니다.</summary>
        None = 0,

        /// <summary>일반 노드를 포함합니다.</summary>
        Normal = 1 << 0,

        /// <summary>시작 노드를 포함합니다.</summary>
        Start = 1 << 1,

        /// <summary>보스 노드를 포함합니다.</summary>
        Boss = 1 << 2,

        /// <summary>휴식처 노드를 포함합니다.</summary>
        Rest = 1 << 3,

        /// <summary>상점 노드를 포함합니다.</summary>
        Shop = 1 << 4,

        /// <summary>숨김 노드를 포함합니다.</summary>
        Hidden = 1 << 5,

        /// <summary>모든 노드 타입을 포함합니다.</summary>
        All = Normal | Start | Boss | Rest | Shop | Hidden,
    }

    /// <summary>
    /// 월드맵 윈도우의 표시, 선택, 이동 판정 정책을 Inspector에서 조절하기 위한 옵션 묶음입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapWindowPresentationOptions
    {
        /// <summary>현재 적용할 표시 정책의 대표 모드입니다.</summary>
        [Tooltip("현재 적용할 표시 정책의 대표 모드입니다. Custom은 아래 값을 직접 조합할 때 사용합니다.")]
        public WorldMapWindowMode mode = WorldMapWindowMode.Default;

        /// <summary>취소하기 버튼을 표시할지 여부입니다.</summary>
        [Tooltip("취소하기 버튼을 표시할지 여부입니다.")]
        public bool showCancelButton = true;

        /// <summary>연결선을 모두 숨길지 여부입니다.</summary>
        [Tooltip("연결선을 모두 숨길지 여부입니다.")]
        public bool hideAllEdges;

        /// <summary>선택된 노드와 연결된 선을 강조할지 여부입니다.</summary>
        [Tooltip("선택된 노드와 연결된 선을 강조할지 여부입니다.")]
        public bool highlightSelectedEdges = true;

        /// <summary>노드 포인트 상태 이미지를 표시할지 여부입니다.</summary>
        [Tooltip("노드 포인트 상태 이미지를 표시할지 여부입니다.")]
        public bool showNodePointState = true;

        /// <summary>노드 포인트 상태를 표시할 노드 타입입니다.</summary>
        [Tooltip("노드 포인트 상태를 표시할 노드 타입입니다.")]
        public WorldMapNodeTypeFilter pointStateNodeTypes = WorldMapNodeTypeFilter.All;

        /// <summary>강조해서 표시할 노드 타입입니다.</summary>
        [Tooltip("강조해서 표시할 노드 타입입니다.")]
        public WorldMapNodeTypeFilter emphasizedNodeTypes = WorldMapNodeTypeFilter.All;

        /// <summary>강조 대상이 아닌 노드에 적용할 투명도입니다.</summary>
        [Range(0f, 1f)]
        [Tooltip("강조 대상이 아닌 노드에 적용할 투명도입니다.")]
        public float dimmedNodeAlpha = 0.1f;

        /// <summary>선택 가능한 노드 타입입니다.</summary>
        [Tooltip("선택 가능한 노드 타입입니다.")]
        public WorldMapNodeTypeFilter selectableNodeTypes = WorldMapNodeTypeFilter.All;

        /// <summary>이동 버튼으로 이동 가능한 노드 타입입니다.</summary>
        [Tooltip("이동 버튼으로 이동 가능한 노드 타입입니다.")]
        public WorldMapNodeTypeFilter warpableNodeTypes = WorldMapNodeTypeFilter.All;

        /// <summary>이동 가능 판정에 현재 맵과의 연결 여부를 요구할지 여부입니다.</summary>
        [Tooltip("이동 가능 판정에 현재 맵과의 연결 여부를 요구할지 여부입니다.")]
        public bool requireAdjacencyToWarp = true;

        /// <summary>이동 가능 판정에 방문 기록을 요구할지 여부입니다.</summary>
        [Tooltip("이동 가능 판정에 방문 기록을 요구할지 여부입니다.")]
        public bool requireVisitedToWarp;

        /// <summary>
        /// 지정한 모드의 기본 표시 정책을 생성합니다.
        /// </summary>
        /// <param name="mode">생성할 표시 정책 모드입니다.</param>
        /// <returns>모드에 맞는 표시 정책 옵션입니다.</returns>
        public static WorldMapWindowPresentationOptions Create(WorldMapWindowMode mode)
        {
            WorldMapWindowPresentationOptions options = new WorldMapWindowPresentationOptions();
            options.ApplyPreset(mode);
            return options;
        }

        /// <summary>
        /// 일반 월드맵 표시 정책을 생성합니다.
        /// </summary>
        /// <returns>일반 월드맵 기본 옵션입니다.</returns>
        public static WorldMapWindowPresentationOptions CreateDefault()
        {
            return Create(WorldMapWindowMode.Default);
        }

        /// <summary>
        /// 지정한 모드의 기본값을 현재 옵션에 적용합니다.
        /// </summary>
        /// <param name="presetMode">적용할 표시 정책 모드입니다.</param>
        public void ApplyPreset(WorldMapWindowMode presetMode)
        {
            mode = presetMode;
            switch (presetMode)
            {
                case WorldMapWindowMode.Warp:
                    ApplyWarpPreset();
                    break;
                case WorldMapWindowMode.MapClearExit:
                    ApplyMapClearExitPreset();
                    break;
                default:
                    ApplyDefaultPreset();
                    break;
            }
        }

        /// <summary>
        /// 지정한 노드 타입이 필터에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="filter">검사할 노드 타입 필터입니다.</param>
        /// <param name="nodeType">확인할 월드맵 노드 타입입니다.</param>
        /// <returns>노드 타입이 필터에 포함되어 있으면 true입니다.</returns>
        public static bool ContainsNodeType(WorldMapNodeTypeFilter filter, WorldMapNodeType nodeType)
        {
            return (filter & ToFilter(nodeType)) != 0;
        }

        /// <summary>
        /// 월드맵 노드 타입을 필터 플래그 값으로 변환합니다.
        /// </summary>
        /// <param name="nodeType">변환할 월드맵 노드 타입입니다.</param>
        /// <returns>노드 타입에 대응하는 필터 플래그입니다.</returns>
        private static WorldMapNodeTypeFilter ToFilter(WorldMapNodeType nodeType)
        {
            switch (nodeType)
            {
                case WorldMapNodeType.Start:
                    return WorldMapNodeTypeFilter.Start;
                case WorldMapNodeType.Boss:
                    return WorldMapNodeTypeFilter.Boss;
                case WorldMapNodeType.Rest:
                    return WorldMapNodeTypeFilter.Rest;
                case WorldMapNodeType.Shop:
                    return WorldMapNodeTypeFilter.Shop;
                case WorldMapNodeType.Hidden:
                    return WorldMapNodeTypeFilter.Hidden;
                default:
                    return WorldMapNodeTypeFilter.Normal;
            }
        }

        /// <summary>
        /// 일반 월드맵 표시 정책 기본값을 적용합니다.
        /// </summary>
        private void ApplyDefaultPreset()
        {
            showCancelButton = true;
            hideAllEdges = false;
            highlightSelectedEdges = true;
            showNodePointState = true;
            pointStateNodeTypes = WorldMapNodeTypeFilter.All;
            emphasizedNodeTypes = WorldMapNodeTypeFilter.All;
            dimmedNodeAlpha = 0.1f;
            selectableNodeTypes = WorldMapNodeTypeFilter.All;
            warpableNodeTypes = WorldMapNodeTypeFilter.All;
            requireAdjacencyToWarp = true;
            requireVisitedToWarp = false;
        }

        /// <summary>
        /// 휴식처 워프 월드맵 표시 정책 기본값을 적용합니다.
        /// </summary>
        private void ApplyWarpPreset()
        {
            showCancelButton = true;
            hideAllEdges = false;
            highlightSelectedEdges = false;
            showNodePointState = true;
            pointStateNodeTypes = WorldMapNodeTypeFilter.Rest;
            emphasizedNodeTypes = WorldMapNodeTypeFilter.Rest;
            dimmedNodeAlpha = 0.1f;
            selectableNodeTypes = WorldMapNodeTypeFilter.Rest;
            warpableNodeTypes = WorldMapNodeTypeFilter.Rest;
            requireAdjacencyToWarp = false;
            requireVisitedToWarp = true;
        }

        /// <summary>
        /// 맵 클리어 종료 월드맵 표시 정책 기본값을 적용합니다.
        /// 일반 월드맵의 노드 및 이동 정책은 유지하고 이전 화면으로 돌아가는 취소 버튼만 숨깁니다.
        /// </summary>
        private void ApplyMapClearExitPreset()
        {
            ApplyDefaultPreset();
            mode = WorldMapWindowMode.MapClearExit;
            showCancelButton = false;
        }
    }
}
