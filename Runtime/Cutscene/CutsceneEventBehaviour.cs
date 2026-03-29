using System;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타임라인 재생 중 컷신 이벤트 목록을 보관하고 실행 상태를 추적하는 PlayableBehaviour입니다.
    /// 재생 구간에 맞춰 이벤트를 한 번만 발생시키는 용도로 사용됩니다.
    /// </summary>
    [Serializable]
    public class CutsceneEventBehaviour : PlayableBehaviour
    {
        /// <summary>
        /// 타임라인에서 처리할 컷신 이벤트 목록입니다.
        /// </summary>
        public List<CutsceneEvent> events;

        /// <summary>
        /// 현재 재생 중인 컷신과 연결된 데이터입니다.
        /// </summary>
        private CutsceneData _data;

        /// <summary>
        /// 각 이벤트의 실행 여부를 인덱스 기준으로 추적하는 배열입니다.
        /// </summary>
        private bool[] _fired;

        /// <summary>
        /// 플레이어블 그래프가 시작될 때 호출됩니다.
        /// 컷신 이벤트 실행을 위한 초기 상태를 구성할 때 사용할 수 있습니다.
        /// </summary>
        /// <param name="playable">현재 동작 중인 플레이어블 인스턴스입니다.</param>
        public override void OnGraphStart(Playable playable)
        {
        }

        /// <summary>
        /// 타임라인 재생 중 매 프레임 호출되며, 현재 재생 시점에 맞는 컷신 이벤트를 처리합니다.
        /// playerData를 통해 외부 재생 대상과 연동할 수 있습니다.
        /// </summary>
        /// <param name="playable">현재 처리 중인 플레이어블 인스턴스입니다.</param>
        /// <param name="info">현재 프레임의 재생 정보입니다.</param>
        /// <param name="playerData">플레이어블 출력에 바인딩된 외부 데이터입니다.</param>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
        }
    }
}