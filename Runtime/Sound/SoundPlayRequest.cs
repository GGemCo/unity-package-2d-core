using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 사운드 재생 요청에 사용할 공용 옵션 클래스입니다.
    /// </summary>
    [Serializable]
    public sealed class SoundPlayRequest
    {
        /// <summary>
        /// 재생할 sound 테이블의 대표 UID입니다.
        /// </summary>
        public int soundUid;

        /// <summary>
        /// 요청 단위로 루프 여부를 덮어쓸지 여부입니다.
        /// </summary>
        public bool useLoopOverride;

        /// <summary>
        /// <see cref="useLoopOverride"/>가 켜져 있을 때 적용할 루프 재생 여부입니다.
        /// </summary>
        public bool loop;

        /// <summary>
        /// 요청 단위로 재생 유지 시간을 덮어쓸지 여부입니다.
        /// </summary>
        public bool useDurationOverride;

        /// <summary>
        /// <see cref="useDurationOverride"/>가 켜져 있을 때 사용할 재생 유지 시간(초)입니다.
        /// </summary>
        [Min(0f)]
        public float durationSeconds;

        /// <summary>
        /// 이 요청으로 시작한 사운드를 어떤 기준으로 정지할지 결정합니다.
        /// </summary>
        public SoundPlaybackStopPolicy stopPolicy = SoundPlaybackStopPolicy.Auto;

        /// <summary>
        /// 요청에 유효한 sound UID가 들어 있는지 확인합니다.
        /// </summary>
        public bool IsValid => soundUid > 0;

        /// <summary>
        /// 사운드 재생 요청 값을 생성합니다.
        /// </summary>
        /// <param name="soundUid">재생할 sound 테이블의 대표 UID입니다.</param>
        /// <param name="loop">루프 재생 여부입니다.</param>
        /// <param name="useLoopOverride">테이블의 Loop 값 대신 요청의 루프 값을 사용할지 여부입니다.</param>
        /// <param name="durationSeconds">재생 유지 시간(초)입니다.</param>
        /// <param name="useDurationOverride">요청의 재생 유지 시간을 사용할지 여부입니다.</param>
        /// <param name="stopPolicy">사운드 정지 정책입니다.</param>
        /// <returns>생성된 사운드 재생 요청입니다.</returns>
        public static SoundPlayRequest Create(
            int soundUid,
            bool loop = false,
            bool useLoopOverride = false,
            float durationSeconds = 0f,
            bool useDurationOverride = false,
            SoundPlaybackStopPolicy stopPolicy = SoundPlaybackStopPolicy.Auto)
        {
            return new SoundPlayRequest
            {
                soundUid = soundUid,
                useLoopOverride = useLoopOverride,
                loop = loop,
                useDurationOverride = useDurationOverride,
                durationSeconds = Mathf.Max(0f, durationSeconds),
                stopPolicy = stopPolicy,
            };
        }

        /// <summary>
        /// 현재 요청 값을 복사합니다.
        /// </summary>
        /// <returns>동일한 값을 가진 새 요청 인스턴스입니다.</returns>
        public SoundPlayRequest Clone()
        {
            return Create(soundUid, loop, useLoopOverride, durationSeconds, useDurationOverride, stopPolicy);
        }

        /// <summary>
        /// 지정한 지속 시간을 적용한 복사본을 생성합니다.
        /// </summary>
        /// <param name="duration">재생 유지 시간(초)입니다.</param>
        /// <returns>지속 시간이 덮어써진 새 요청 인스턴스입니다.</returns>
        public SoundPlayRequest CloneWithDuration(float duration)
        {
            return Create(soundUid, loop, useLoopOverride, duration, useDurationOverride: true, stopPolicy);
        }

        /// <summary>
        /// 외부 핸들 정지 전까지 루프 재생되는 요청 복사본을 생성합니다.
        /// </summary>
        /// <returns>루프와 핸들 정지 정책이 적용된 새 요청 인스턴스입니다.</returns>
        public SoundPlayRequest CloneLoopUntilHandleStopped()
        {
            return Create(
                soundUid,
                loop: true,
                useLoopOverride: true,
                durationSeconds: 0f,
                useDurationOverride: false,
                stopPolicy: SoundPlaybackStopPolicy.ByHandle);
        }

        /// <summary>
        /// 루프 오버라이드 값을 nullable 형태로 반환합니다.
        /// </summary>
        /// <returns>오버라이드가 켜져 있으면 루프 값, 아니면 null입니다.</returns>
        public bool? ResolveLoopOverride()
        {
            return useLoopOverride ? loop : null;
        }

        /// <summary>
        /// 요청에 사용할 재생 유지 시간을 반환합니다.
        /// </summary>
        /// <param name="fallbackSeconds">오버라이드가 꺼져 있을 때 사용할 기본 시간입니다.</param>
        /// <returns>최종 재생 유지 시간(초)입니다.</returns>
        public float ResolveDuration(float fallbackSeconds = 0f)
        {
            return useDurationOverride
                ? Mathf.Max(0f, durationSeconds)
                : Mathf.Max(0f, fallbackSeconds);
        }
    }
}
