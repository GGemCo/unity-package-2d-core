using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기준 종횡비와 다른 화면에서 카메라 Viewport를 구성하는 방식을 정의합니다.
    /// </summary>
    public enum CameraAspectMode
    {
        /// <summary>
        /// 모든 화면에서 기준 종횡비를 유지하고 남는 영역에 여백을 둡니다.
        /// </summary>
        Fixed = 0,

        /// <summary>
        /// 기준보다 넓은 화면에서는 세로 시야를 유지한 채 가로 시야를 확장합니다.
        /// 기준보다 좁은 화면에서는 기준 종횡비를 유지합니다.
        /// </summary>
        ExpandHorizontal = 1,
    }

    /// <summary>
    /// 카메라 흔들림 요청을 구분하고 선택적으로 중단하기 위한 채널입니다.
    /// </summary>
    public enum CameraShakeChannel
    {
        Default = 0,
        AnimationEvent = 1,
        Cutscene = 2,
        SkillDamage = 3,
        BasicAttack = 4,
    }

    /// <summary>
    /// 맵 로드 시점에 카메라의 Y 오프셋을 자동 보정할지 결정하는 정책입니다.
    /// </summary>
    public enum CameraBottomFollowOffsetPolicy
    {
        /// <summary>
        /// 인스펙터 또는 코드에서 설정한 값을 그대로 사용합니다.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// 맵 하단 경계에 카메라 하단이 맞도록 Follow Offset Y를 자동 계산합니다.
        /// </summary>
        AutoAlignToMapBottomOnMapLoad = 1,
    }

    /// <summary>
    /// 카메라 흔들림 재생에 필요한 값을 담는 요청 데이터입니다.
    /// </summary>
    public struct CameraShakeRequest
    {
        public CameraShakeType ShakeType;
        public float Duration;
        public float Strength;
        public Vector2 AxisStrength;
        public Vector2 Direction;
        public float LeftStrength;
        public float RightStrength;
        public float DownStrength;
        public float UpStrength;
        public int RepeatCount;
        public bool RandomStartPhase;
        public CameraShakeChannel Channel;
        public bool UseUnscaledTime;
        public AnimationCurve ImpulseCurve;
        public CameraShakeDecayMode DecayMode;

        /// <summary>
        /// 요청 데이터가 실제 재생 가능한 흔들림 값을 가지고 있는지 반환합니다.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (Duration <= 0f)
                {
                    return false;
                }

                if (Strength > 0f)
                {
                    return true;
                }

                return LeftStrength > 0f || RightStrength > 0f || DownStrength > 0f || UpStrength > 0f;
            }
        }

        /// <summary>
        /// 모든 방향의 세기가 같은 일반 카메라 흔들림 요청을 생성합니다.
        /// </summary>
        /// <param name="duration">흔들림 재생 시간입니다.</param>
        /// <param name="magnitude">좌우/상하 공통 세기입니다.</param>
        /// <param name="repeatCount">반복 진동 횟수입니다.</param>
        /// <param name="channel">흔들림 채널입니다.</param>
        /// <param name="useUnscaledTime">Time.timeScale 영향을 무시할지 여부입니다.</param>
        /// <returns>일반 흔들림 요청 데이터입니다.</returns>
        public static CameraShakeRequest CreateSymmetric(
            float duration,
            float magnitude,
            int repeatCount,
            CameraShakeChannel channel,
            bool useUnscaledTime = false)
        {
            float safeMagnitude = Mathf.Max(0f, magnitude);
            return new CameraShakeRequest
            {
                ShakeType = CameraShakeType.Common,
                Duration = duration,
                Strength = safeMagnitude,
                AxisStrength = Vector2.one,
                LeftStrength = safeMagnitude,
                RightStrength = safeMagnitude,
                DownStrength = safeMagnitude,
                UpStrength = safeMagnitude,
                RepeatCount = Mathf.Max(1, repeatCount),
                RandomStartPhase = true,
                Direction = Vector2.right,
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
                DecayMode = CameraShakeDecayMode.Linear,
            };
        }
    }
}
