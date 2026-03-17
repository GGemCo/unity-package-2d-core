using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 기반 HUD 시각 상태의 공용 프로필 베이스입니다.
    /// </summary>
    [Serializable]
    public abstract class HudAffectVisualProfileBase
    {
        [SerializeField] private string stateKey = "default";
        [SerializeField] private int priority = 0;

        public string StateKey => stateKey;
        public int Priority => priority;
    }
}
