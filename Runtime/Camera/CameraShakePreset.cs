using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = "CameraShakePreset", menuName = "GGemCo/Core/카메라 Shake Preset", order = 0)]
    public sealed class CameraShakePreset : ScriptableObject
    {
        [Header("설명")]
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Shake")]
        [Tooltip("전체 Shake 재생 시간(초)")]
        [SerializeField] private float duration = 0.2f;
        [Tooltip("좌우/상하 파형 반복 횟수")]
        [SerializeField] private int repeatCount = 3;
        [Tooltip("왼쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float leftStrength = 0.1f;
        [Tooltip("오른쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float rightStrength = 0.1f;
        [Tooltip("아래쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float downStrength = 0.05f;
        [Tooltip("위쪽으로 흔들릴 때 최대 세기")]
        [SerializeField] private float upStrength = 0.05f;
        [Tooltip("Time.timeScale 영향을 무시하고 흔들림을 재생할지 여부")]
        [SerializeField] private bool useUnscaledTime;

        public string Description => description;
        public float Duration => duration;
        public int RepeatCount => repeatCount;
        public float LeftStrength => leftStrength;
        public float RightStrength => rightStrength;
        public float DownStrength => downStrength;
        public float UpStrength => upStrength;
        public bool UseUnscaledTime => useUnscaledTime;

        public CameraShakeRequest ToRequest(CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            return new CameraShakeRequest
            {
                Duration = Mathf.Max(0f, duration),
                RepeatCount = Mathf.Max(1, repeatCount),
                LeftStrength = Mathf.Max(0f, leftStrength),
                RightStrength = Mathf.Max(0f, rightStrength),
                DownStrength = Mathf.Max(0f, downStrength),
                UpStrength = Mathf.Max(0f, upStrength),
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
            };
        }
    }
}
