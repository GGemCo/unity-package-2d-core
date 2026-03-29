using System;
using UnityEngine.Playables;

namespace GGemCo2DCore
{
    /// <summary>
    /// 여러 컷신 이벤트 클립의 출력을 혼합(Mix)하여 처리하는 PlayableBehaviour입니다.
    /// 타임라인에서 동시에 활성화된 클립들의 상태를 종합하여 최종 결과를 반영하는 역할을 합니다.
    /// </summary>
    [Serializable]
    public class CutsceneEventMixerBehaviour : PlayableBehaviour
    {
        /// <summary>
        /// 타임라인 재생 중 매 프레임 호출되며, 입력 플레이어블들의 상태를 기반으로 이벤트를 혼합 처리합니다.
        /// 일반적으로 각 입력 클립의 가중치(weight)를 고려하여 이벤트 실행 여부를 결정할 때 사용됩니다.
        /// </summary>
        /// <param name="playable">현재 믹서 역할을 하는 플레이어블 인스턴스입니다.</param>
        /// <param name="info">현재 프레임의 재생 정보(가중치, 시간 등)를 포함합니다.</param>
        /// <param name="playerData">출력 대상과 연결된 사용자 정의 데이터입니다.</param>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
        }

        /// <summary>
        /// 플레이어블이 파괴될 때 호출됩니다.
        /// 믹서에서 사용하던 리소스 정리 또는 상태 초기화를 수행할 수 있습니다.
        /// </summary>
        /// <param name="playable">파괴되는 플레이어블 인스턴스입니다.</param>
        public override void OnPlayableDestroy(Playable playable)
        {
        }
    }
}