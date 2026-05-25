using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대화 말풍선 월드 오프셋 정책 계산을 공통으로 제공합니다.
    /// 컷신 말풍선과 인터랙션 말풍선이 같은 X 반전 규칙을 사용하도록 중복 로직을 분리합니다.
    /// </summary>
    public static class DialogueBalloonWorldOffsetUtility
    {
        /// <summary>
        /// 월드 오프셋 X 정책을 안전한 런타임 값으로 보정합니다.
        /// UseProjectPolicy는 프로젝트 기본값을 다시 참조할 수 없는 최종 계산 단계에서 KeepOriginal로 처리합니다.
        /// </summary>
        /// <param name="policy">검사할 월드 오프셋 X 정책입니다.</param>
        /// <returns>최종 계산에 사용할 수 있는 안전한 월드 오프셋 X 정책입니다.</returns>
        public static DialogueBalloonWorldOffsetXPolicy GetSafeWorldOffsetXPolicy(DialogueBalloonWorldOffsetXPolicy policy)
        {
            return policy switch
            {
                DialogueBalloonWorldOffsetXPolicy.KeepOriginal => DialogueBalloonWorldOffsetXPolicy.KeepOriginal,
                DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing => DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing,
                DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy => DialogueBalloonWorldOffsetXPolicy.KeepOriginal,
                _ => DialogueBalloonWorldOffsetXPolicy.KeepOriginal
            };
        }

        /// <summary>
        /// 화자 좌우 방향과 정책을 반영해 월드 오프셋 X 값을 계산합니다.
        /// 화자 방향을 알 수 없으면 입력 오프셋을 그대로 유지해 기존 배치를 보존합니다.
        /// </summary>
        /// <param name="offsetX">기본 오프셋 X 값입니다.</param>
        /// <param name="policy">적용할 월드 오프셋 X 정책입니다.</param>
        /// <param name="hasSpeakerFacing">화자의 좌우 방향을 판별했는지 여부입니다.</param>
        /// <param name="isFacingRight">화자가 오른쪽을 바라보는지 여부입니다.</param>
        /// <returns>정책이 반영된 월드 오프셋 X 값입니다.</returns>
        public static float ResolveOffsetXByPolicy(
            float offsetX,
            DialogueBalloonWorldOffsetXPolicy policy,
            bool hasSpeakerFacing,
            bool isFacingRight)
        {
            DialogueBalloonWorldOffsetXPolicy safePolicy = GetSafeWorldOffsetXPolicy(policy);
            if (safePolicy != DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing || !hasSpeakerFacing)
            {
                return offsetX;
            }

            return isFacingRight ? offsetX : -offsetX;
        }

        /// <summary>
        /// 화자 좌우 방향과 정책을 반영해 월드 오프셋 벡터의 X 값만 보정합니다.
        /// Y/Z 값은 원본 값을 그대로 유지합니다.
        /// </summary>
        /// <param name="offset">기본 월드 오프셋입니다.</param>
        /// <param name="policy">적용할 월드 오프셋 X 정책입니다.</param>
        /// <param name="hasSpeakerFacing">화자의 좌우 방향을 판별했는지 여부입니다.</param>
        /// <param name="isFacingRight">화자가 오른쪽을 바라보는지 여부입니다.</param>
        /// <returns>X 정책이 반영된 월드 오프셋입니다.</returns>
        public static Vector3 ResolveOffsetByPolicy(
            Vector3 offset,
            DialogueBalloonWorldOffsetXPolicy policy,
            bool hasSpeakerFacing,
            bool isFacingRight)
        {
            offset.x = ResolveOffsetXByPolicy(offset.x, policy, hasSpeakerFacing, isFacingRight);
            return offset;
        }
    }
}
