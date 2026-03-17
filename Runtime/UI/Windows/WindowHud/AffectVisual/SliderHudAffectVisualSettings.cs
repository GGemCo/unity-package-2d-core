using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Slider 기반 HUD 연출용 Affect 시각 상태 설정입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SliderHudAffectVisualSettings", menuName = "GGemCo/UI/Slider HUD Affect Visual Settings")]
    public sealed class SliderHudAffectVisualSettings : HudAffectVisualSettings
    {
        [SerializeField] private List<SliderHudVisualProfile> profiles = new();

        protected override HudAffectVisualProfileBase GetProfile(string stateKey)
        {
            if (profiles == null || profiles.Count == 0)
                return null;

            string normalized = NormalizeStateKey(stateKey);
            for (int i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (profile == null || string.IsNullOrWhiteSpace(profile.StateKey))
                    continue;

                if (string.Equals(profile.StateKey, normalized, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }

            return null;
        }
    }

    /// <summary>
    /// Slider 기반 HUD에 적용할 Affect 시각 프로필입니다.
    /// </summary>
    [Serializable]
    public sealed class SliderHudVisualProfile : HudAffectVisualProfileBase
    {
        [SerializeField] private Color fillColor = Color.white;
        [SerializeField] private Color backgroundColor = Color.white;
        [SerializeField] private Color handleColor = Color.white;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private bool usePulse = false;
        [SerializeField] private float pulseScaleMultiplier = 1.05f;
        [SerializeField] private float pulseSpeed = 3f;

        public Color FillColor => fillColor;
        public Color BackgroundColor => backgroundColor;
        public Color HandleColor => handleColor;
        public Color TextColor => textColor;
        public bool UsePulse => usePulse;
        public float PulseScaleMultiplier => pulseScaleMultiplier;
        public float PulseSpeed => pulseSpeed;
    }
}
