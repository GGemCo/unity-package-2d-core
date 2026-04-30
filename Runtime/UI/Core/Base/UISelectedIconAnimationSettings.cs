using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 선택 아이콘 이미지에 연결된 2D 애니메이션 재생 설정입니다.
    /// </summary>
    [Serializable]
    public class UISelectedIconAnimationSettings
    {
        /// <summary>
        /// 선택 아이콘 이미지의 <see cref="Animation2dController"/>에서 재생할 애니메이션 클립 이름입니다.
        /// </summary>
        [Tooltip("선택 아이콘 이미지의 Animation2dController에서 재생할 애니메이션 클립 이름입니다.")]
        public string animationName = "Select";

        /// <summary>
        /// 선택 아이콘 이미지 애니메이션을 루프로 재생할지 여부입니다.
        /// </summary>
        [Tooltip("선택 아이콘 이미지 애니메이션을 루프로 재생할지 여부입니다.")]
        public bool isLoop = true;

        /// <summary>
        /// 재생할 애니메이션 이름이 설정되어 있는지 반환합니다.
        /// </summary>
        public bool HasAnimation => !string.IsNullOrWhiteSpace(animationName);
    }
}
