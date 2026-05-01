using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 캔버스의 기본 배치 영역과 실제 오버레이/입력 영역을 함께 보관하는 프레임 정보입니다.
    /// </summary>
    internal readonly struct WorldMapCanvasFrame
    {
        /// <summary>중앙 캔버스의 기본 배치 Rect입니다.</summary>
        public readonly Rect HostRect;

        /// <summary>배경 이미지와 Grid가 그려지는 실제 그래프 Rect입니다.</summary>
        public readonly Rect GraphRect;

        /// <summary>노드/연결선 오버레이까지 포함한 입력 우선순위 Rect입니다.</summary>
        public readonly Rect InteractionRect;

        /// <summary>
        /// 캔버스 프레임을 초기화합니다.
        /// </summary>
        /// <param name="hostRect">중앙 캔버스의 기본 배치 Rect입니다.</param>
        /// <param name="graphRect">배경 이미지와 Grid가 그려지는 실제 그래프 Rect입니다.</param>
        /// <param name="interactionRect">노드/연결선 오버레이까지 포함한 입력 우선순위 Rect입니다.</param>
        public WorldMapCanvasFrame(Rect hostRect, Rect graphRect, Rect interactionRect)
        {
            HostRect = hostRect;
            GraphRect = graphRect;
            InteractionRect = interactionRect;
        }
    }
}
