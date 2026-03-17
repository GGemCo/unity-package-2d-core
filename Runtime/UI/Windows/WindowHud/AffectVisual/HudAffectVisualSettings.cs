using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 상태 키를 HUD 시각 프로필로 해석하는 공용 설정 베이스입니다.
    /// </summary>
    public abstract class HudAffectVisualSettings : ScriptableObject
    {
        [SerializeField] private string defaultStateKey = "default";

        public string DefaultStateKey => defaultStateKey;

        public int GetPriority(string stateKey)
        {
            var profile = GetProfile(stateKey);
            return profile != null ? profile.Priority : 0;
        }

        public HudAffectVisualProfileBase GetProfileOrDefault(string stateKey)
        {
            string normalized = NormalizeStateKey(stateKey);
            var profile = GetProfile(normalized);
            if (profile != null)
                return profile;

            if (!string.IsNullOrWhiteSpace(defaultStateKey) &&
                !string.Equals(normalized, defaultStateKey, StringComparison.OrdinalIgnoreCase))
            {
                return GetProfile(defaultStateKey);
            }

            return null;
        }

        protected string NormalizeStateKey(string stateKey)
        {
            return string.IsNullOrWhiteSpace(stateKey) ? defaultStateKey : stateKey.Trim();
        }

        protected abstract HudAffectVisualProfileBase GetProfile(string stateKey);
    }
}
