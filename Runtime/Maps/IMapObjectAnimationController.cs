using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 애니메이션 관리
    /// </summary>
    public interface IMapObjectAnimationController
    {
        void PlayMapObjectAnimation(string animationName, bool loop = false, float timeScale = 1f);
        bool HasAnimation(string stateName);
        Dictionary<string, float> GetAnimationAllLength();
    }
}