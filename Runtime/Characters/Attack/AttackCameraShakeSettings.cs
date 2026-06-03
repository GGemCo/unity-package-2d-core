using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기본 공격 명중 시 재생할 카메라 Shake 설정입니다.
    /// </summary>
    /// <remarks>
    /// Control 패키지의 콤보 설정이 Core 데미지 처리 계층에 직접 의존하지 않도록,
    /// 카메라 Shake에 필요한 값만 담아 전달하는 공통 데이터 구조입니다.
    /// </remarks>
    [Serializable]
    public struct AttackCameraShakeSettings
    {
        /// <summary>
        /// 기본 공격 명중 시 카메라 Shake를 재생할지 여부입니다.
        /// </summary>
        [Tooltip("기본 공격이 실제로 명중했을 때 카메라 Shake를 재생할지 여부입니다.")]
        public bool useCameraShakeOnHit;

        /// <summary>
        /// 명중 시 재생할 카메라 Shake 프리셋입니다.
        /// </summary>
        [Tooltip("기본 공격 명중 시 재생할 카메라 Shake 프리셋입니다.")]
        public CameraShakePreset cameraShakePreset;

        /// <summary>
        /// 카메라 Shake 방향을 계산할 기준입니다.
        /// </summary>
        [Tooltip("카메라 Shake 방향을 프리셋, 고정 방향, 공격자/대상 위치 중 어떤 기준으로 계산할지 지정합니다.")]
        public CameraShakeDirectionSource cameraShakeDirectionSource;

        /// <summary>
        /// 고정 방향 카메라 Shake에서 사용할 방향입니다.
        /// </summary>
        [Tooltip("방향 기준이 FixedDirection일 때 사용할 고정 방향입니다.")]
        public Vector2 cameraShakeFixedDirection;

        /// <summary>
        /// 방향 계산 시 Y축을 제거하고 좌우 방향만 사용할지 여부입니다.
        /// </summary>
        [Tooltip("방향 계산 시 Y축을 제거하고 좌우 방향만 사용할지 여부입니다.")]
        public bool cameraShakeHorizontalOnly;

        /// <summary>
        /// 카메라 Shake를 재생할 채널입니다.
        /// </summary>
        [Tooltip("카메라 Shake를 식별하고 중단할 때 사용할 채널입니다. Default이면 BasicAttack 채널로 보정합니다.")]
        public CameraShakeChannel cameraShakeChannel;

        /// <summary>
        /// 사용하지 않는 기본 설정을 반환합니다.
        /// </summary>
        public static AttackCameraShakeSettings Disabled => new AttackCameraShakeSettings
        {
            useCameraShakeOnHit = false,
            cameraShakePreset = null,
            cameraShakeDirectionSource = CameraShakeDirectionSource.Preset,
            cameraShakeFixedDirection = Vector2.right,
            cameraShakeHorizontalOnly = true,
            cameraShakeChannel = CameraShakeChannel.BasicAttack
        };

        /// <summary>
        /// 카메라 Shake를 재생할 수 있는 유효 설정인지 여부입니다.
        /// </summary>
        public bool HasCameraShake => useCameraShakeOnHit && cameraShakePreset != null;

        /// <summary>
        /// 실제 재생에 사용할 카메라 Shake 채널을 반환합니다.
        /// </summary>
        /// <remarks>
        /// Unity 직렬화 기본값으로 <see cref="CameraShakeChannel.Default"/>가 들어온 경우에도
        /// 기본 공격 Shake가 다른 시스템의 Default 채널을 덮어쓰지 않도록 BasicAttack 채널로 보정합니다.
        /// </remarks>
        public CameraShakeChannel ResolvedChannel =>
            cameraShakeChannel == CameraShakeChannel.Default
                ? CameraShakeChannel.BasicAttack
                : cameraShakeChannel;
    }
}
