namespace GGemCo2DCore
{
    /// <summary>
    /// SoundResolver가 선택한 최종 재생 대상과 요청별 보정값을 담는 읽기 전용 결과 구조체입니다.
    /// </summary>
    public readonly struct ResolvedSound
    {
        public readonly int RequestedSoundUid;
        public readonly int ResourceUid;
        public readonly SoundConstants.Type Type;
        public readonly string FileName;
        public readonly float Volume;
        public readonly float Pitch;
        public readonly bool Loop;
        public readonly float FadeDuration;
        public readonly bool ShouldPlay;
        public readonly StruckTableSound Sound;
        public readonly StruckTableSoundResource Resource;

        /// <summary>
        /// 최종 재생할 사운드 정보를 생성합니다.
        /// </summary>
        /// <param name="requestedSoundUid">외부에서 요청한 대표 sound UID입니다.</param>
        /// <param name="resourceUid">실제 리소스 테이블 UID입니다.</param>
        /// <param name="type">사운드 타입입니다.</param>
        /// <param name="fileName">실제 AudioClip 파일명입니다.</param>
        /// <param name="volume">최종 AudioSource 볼륨 배율입니다.</param>
        /// <param name="pitch">최종 AudioSource 피치입니다.</param>
        /// <param name="loop">루프 재생 여부입니다.</param>
        /// <param name="fadeDuration">페이드 시간입니다.</param>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="resource">실제 리소스 행입니다. 레거시 sound.txt 직접 재생이면 null입니다.</param>
        /// <param name="shouldPlay">무음 후보를 선택했을 때 false입니다.</param>
        public ResolvedSound(
            int requestedSoundUid,
            int resourceUid,
            SoundConstants.Type type,
            string fileName,
            float volume,
            float pitch,
            bool loop,
            float fadeDuration,
            StruckTableSound sound,
            StruckTableSoundResource resource,
            bool shouldPlay = true)
        {
            RequestedSoundUid = requestedSoundUid;
            ResourceUid = resourceUid;
            Type = type;
            FileName = fileName;
            Volume = volume;
            Pitch = pitch;
            Loop = loop;
            FadeDuration = fadeDuration;
            Sound = sound;
            Resource = resource;
            ShouldPlay = shouldPlay;
        }

        /// <summary>
        /// 무음 후보 선택 결과를 생성합니다.
        /// </summary>
        /// <param name="requestedSoundUid">외부에서 요청한 대표 sound UID입니다.</param>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <returns>재생하지 않는 결과입니다.</returns>
        public static ResolvedSound Silent(int requestedSoundUid, StruckTableSound sound)
        {
            return new ResolvedSound(requestedSoundUid, 0, sound != null ? sound.Type : SoundConstants.Type.None,
                string.Empty, 0f, 1f, false, 0f, sound, null, false);
        }

        /// <summary>
        /// 기존 해석 결과의 루프 재생 여부만 요청 단위 값으로 교체합니다.
        /// </summary>
        /// <param name="loop">요청 단위로 적용할 루프 재생 여부입니다.</param>
        /// <returns>루프 재생 여부가 교체된 새 해석 결과입니다.</returns>
        public ResolvedSound WithLoop(bool loop)
        {
            return new ResolvedSound(RequestedSoundUid, ResourceUid, Type, FileName, Volume, Pitch,
                loop, FadeDuration, Sound, Resource, ShouldPlay);
        }
    }
}
