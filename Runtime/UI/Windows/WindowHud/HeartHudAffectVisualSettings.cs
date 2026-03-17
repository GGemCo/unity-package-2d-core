using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 상태 키별 하트 HUD 연출 프로필을 정의하는 설정입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "HeartHudAffectVisualSettings", menuName = "GGemCo/UI/Heart HUD Affect Visual Settings")]
    public sealed class HeartHudAffectVisualSettings : ScriptableObject
    {
        [SerializeField] private string defaultStateKey = "default";
        [SerializeField] private List<HeartHudVisualProfile> profiles = new();

        public string DefaultStateKey => defaultStateKey;

        public HeartHudVisualProfile GetProfile(string stateKey)
        {
            if (profiles == null || profiles.Count == 0)
                return null;

            string normalized = string.IsNullOrWhiteSpace(stateKey) ? defaultStateKey : stateKey.Trim();
            for (int i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                if (profile == null || string.IsNullOrWhiteSpace(profile.StateKey))
                    continue;

                if (string.Equals(profile.StateKey, normalized, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }

            if (!string.IsNullOrWhiteSpace(defaultStateKey) && !string.Equals(normalized, defaultStateKey, StringComparison.OrdinalIgnoreCase))
                return GetProfile(defaultStateKey);

            return null;
        }

        public int GetPriority(string stateKey)
        {
            var profile = GetProfile(stateKey);
            return profile != null ? profile.Priority : 0;
        }
    }

    /// <summary>
    /// 특정 Affect 상태 키에 대응하는 하트 HUD 시각 프로필입니다.
    /// </summary>
    [Serializable]
    public sealed class HeartHudVisualProfile
    {
        [SerializeField] private string stateKey = "default";
        [SerializeField] private int priority = 0;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private bool overrideBaseSprites = false;
        [SerializeField] private List<Sprite> baseSprites = new();
        [SerializeField] private bool overrideTempSprites = false;
        [SerializeField] private List<Sprite> tempSprites = new();
        [SerializeField] private bool usePulse = false;
        [SerializeField] private float pulseScaleMultiplier = 1.05f;
        [SerializeField] private float pulseSpeed = 3.0f;

        public string StateKey => stateKey;
        public int Priority => priority;
        public Color Tint => tint;
        public bool OverrideBaseSprites => overrideBaseSprites;
        public IReadOnlyList<Sprite> BaseSprites => baseSprites;
        public bool OverrideTempSprites => overrideTempSprites;
        public IReadOnlyList<Sprite> TempSprites => tempSprites;
        public bool UsePulse => usePulse;
        public float PulseScaleMultiplier => pulseScaleMultiplier;
        public float PulseSpeed => pulseSpeed;
    }
}
