using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 사운드 행이 담당하는 역할을 정의합니다.
    /// </summary>
    public enum MapSoundRole
    {
        [Tooltip("사운드 대표 테이블의 Type을 기준으로 역할을 자동 판정합니다.")]
        None,

        [Tooltip("맵의 배경 음악으로 재생합니다.")]
        Bgm,

        [Tooltip("맵의 환경음으로 재생합니다.")]
        Ambient,

        [Tooltip("재생하지 않고 맵 범위에서 AudioClip만 미리 로드합니다.")]
        PreloadOnly,
    }
}
