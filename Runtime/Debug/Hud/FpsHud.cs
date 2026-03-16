#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// FPS/FrameTime HUD 데이터를 계산하는 런타임 프로바이더입니다.
    /// </summary>
    public sealed class FpsHud : IDebugHudProvider
    {
        private readonly Queue<float> _frameTimes = new();
        private float _timeLeft;
        private float _avgMs;
        private float _minMs = float.MaxValue;
        private float _maxMs;
        private bool _initialized;
        private string _text = "[FPS]\nCollecting...";

        public DebugHudAnchor Anchor => DebugHudAnchor.TopRight;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud) && DebugOptionRuntimeUtility.Resolve(settings.enableFpsHud);
        }

        public void Initialize(GGemCoSettings settings)
        {
            _frameTimes.Clear();
            _avgMs = 0f;
            _minMs = float.MaxValue;
            _maxMs = 0f;
            _timeLeft = Mathf.Max(0.1f, settings != null ? settings.debugHudFpsUpdateInterval : 0.5f);
            _text = "[FPS]\nCollecting...";
            _initialized = true;
        }

        public void Tick(float unscaledDeltaTime, GGemCoSettings settings)
        {
            if (!_initialized)
            {
                Initialize(settings);
            }

            float dtMs = Mathf.Max(0f, unscaledDeltaTime) * 1000f;
            _frameTimes.Enqueue(dtMs);

            int historySize = settings != null ? Mathf.Max(8, settings.debugHudFpsHistorySize) : 100;
            while (_frameTimes.Count > historySize)
            {
                _frameTimes.Dequeue();
            }

            _timeLeft -= Mathf.Max(0f, unscaledDeltaTime);
            if (_timeLeft > 0f)
            {
                return;
            }

            float sum = 0f;
            float min = float.MaxValue;
            float max = 0f;
            foreach (float ms in _frameTimes)
            {
                sum += ms;
                if (ms < min) min = ms;
                if (ms > max) max = ms;
            }

            _avgMs = _frameTimes.Count > 0 ? sum / _frameTimes.Count : 0f;
            _minMs = min == float.MaxValue ? 0f : min;
            _maxMs = max;
            _timeLeft = Mathf.Max(0.1f, settings != null ? settings.debugHudFpsUpdateInterval : 0.5f);

            _text = $"[FPS]\n" +
                    $"Avg: {(_avgMs > 0f ? 1000f / _avgMs : 0f):0.0} fps ({_avgMs:0.0} ms)\n" +
                    $"Best: {(_minMs > 0f ? 1000f / _minMs : 0f):0.0} fps ({_minMs:0.0} ms)\n" +
                    $"Worst: {(_maxMs > 0f ? 1000f / _maxMs : 0f):0.0} fps ({_maxMs:0.0} ms)";
        }

        public string GetText() => _text;
    }
}
#endif
