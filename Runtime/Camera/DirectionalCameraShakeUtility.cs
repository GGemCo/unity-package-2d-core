using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 방향성 카메라 Shake에서 방향을 어디서 계산할지 결정하는 정책입니다.
    /// </summary>
    public enum CameraShakeDirectionSource
    {
        /// <summary>
        /// 프리셋에 정의된 기본 Shake 요청을 그대로 사용합니다.
        /// </summary>
        Preset = 0,

        /// <summary>
        /// 요청에 직접 지정된 고정 방향을 사용합니다.
        /// </summary>
        FixedDirection = 1,

        /// <summary>
        /// 시전자 위치에서 대상 위치로 향하는 방향을 사용합니다.
        /// </summary>
        CasterToTarget = 2,

        /// <summary>
        /// 대상 위치에서 시전자 위치로 향하는 방향을 사용합니다.
        /// </summary>
        TargetToCaster = 3,
    }

    /// <summary>
    /// 시전자와 대상 정보를 카메라 Shake 재생 요청으로 변환하는 유틸리티입니다.
    /// </summary>
    public static class DirectionalCameraShakeUtility
    {
        /// <summary>
        /// 프리셋과 방향 정책을 조합하여 카메라 Shake 요청을 생성합니다.
        /// </summary>
        /// <param name="preset">재생할 카메라 Shake 프리셋입니다.</param>
        /// <param name="caster">스킬 또는 공격을 실행한 시전자 Transform입니다.</param>
        /// <param name="target">실제 데미지를 받은 대상 Transform입니다.</param>
        /// <param name="directionSource">Shake 방향을 계산할 기준입니다.</param>
        /// <param name="fixedDirection">고정 방향 정책에서 사용할 방향입니다.</param>
        /// <param name="horizontalOnly">계산된 방향에서 Y축을 제거하고 좌우 방향만 사용할지 여부입니다.</param>
        /// <param name="channel">Shake를 식별하고 중단할 때 사용할 채널입니다.</param>
        /// <returns>카메라 매니저가 재생할 수 있는 Shake 요청 데이터입니다.</returns>
        public static CameraShakeRequest CreateRequest(
            CameraShakePreset preset,
            Transform caster,
            Transform target,
            CameraShakeDirectionSource directionSource,
            Vector2 fixedDirection,
            bool horizontalOnly,
            CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            if (preset == null)
            {
                return default;
            }

            if (directionSource == CameraShakeDirectionSource.Preset)
            {
                return preset.ToRequest(channel);
            }

            if (!TryResolveDirection(caster, target, directionSource, fixedDirection, horizontalOnly, out Vector2 direction))
            {
                return preset.ToRequest(channel);
            }

            return preset.ToDirectionalRequest(channel, direction);
        }

        /// <summary>
        /// 방향 정책에 따라 실제 Shake 방향 벡터를 계산합니다.
        /// </summary>
        /// <param name="caster">스킬 또는 공격을 실행한 시전자 Transform입니다.</param>
        /// <param name="target">실제 데미지를 받은 대상 Transform입니다.</param>
        /// <param name="directionSource">Shake 방향을 계산할 기준입니다.</param>
        /// <param name="fixedDirection">고정 방향 정책에서 사용할 방향입니다.</param>
        /// <param name="horizontalOnly">계산된 방향에서 Y축을 제거하고 좌우 방향만 사용할지 여부입니다.</param>
        /// <param name="direction">정규화된 방향 벡터입니다.</param>
        /// <returns>유효한 방향 계산에 성공했으면 true입니다.</returns>
        private static bool TryResolveDirection(
            Transform caster,
            Transform target,
            CameraShakeDirectionSource directionSource,
            Vector2 fixedDirection,
            bool horizontalOnly,
            out Vector2 direction)
        {
            direction = Vector2.zero;

            switch (directionSource)
            {
                case CameraShakeDirectionSource.FixedDirection:
                    direction = fixedDirection;
                    break;
                case CameraShakeDirectionSource.CasterToTarget:
                    if (caster == null || target == null)
                    {
                        return false;
                    }

                    direction = target.position - caster.position;
                    break;
                case CameraShakeDirectionSource.TargetToCaster:
                    if (caster == null || target == null)
                    {
                        return false;
                    }

                    direction = caster.position - target.position;
                    break;
                default:
                    return false;
            }

            if (horizontalOnly)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude <= 0.0001f && caster != null && target != null)
            {
                direction = new Vector2(Mathf.Sign(target.position.x - caster.position.x), 0f);
                if (directionSource == CameraShakeDirectionSource.TargetToCaster)
                {
                    direction = -direction;
                }
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }
    }
}
