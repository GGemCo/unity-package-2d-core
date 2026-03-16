using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// FPS / 프레임 시간 정보를 수집하는 HUD Provider 입니다.
    /// </summary>
    [DebugHudProvider(100)]
    public sealed class FpsHud : IDebugHudProvider
    {
        private readonly Queue<float> _frameTimes = new();
        private readonly StringBuilder _builder = new(128);

        private int _historySize;
        private float _avgMs;
        private float _minMs;
        private float _maxMs;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && settings.EnableDebugHud && settings.enableFpsHud;
        }

        public float GetUpdateInterval(GGemCoSettings settings)
        {
            return settings != null ? Mathf.Max(0.05f, settings.debugHudFpsUpdateInterval) : 0.25f;
        }

        public void Reset()
        {
            _frameTimes.Clear();
            _historySize = 0;
            _avgMs = 0f;
            _minMs = 0f;
            _maxMs = 0f;
        }

        public void Tick(float elapsedSeconds)
        {
            float dtMs = Time.unscaledDeltaTime * 1000f;
            _frameTimes.Enqueue(dtMs);

            int targetHistorySize = Mathf.Max(8, GGemCoDebugHudManager.CurrentSettings != null
                ? GGemCoDebugHudManager.CurrentSettings.debugHudFpsHistorySize
                : 100);
            _historySize = targetHistorySize;

            while (_frameTimes.Count > _historySize)
            {
                _frameTimes.Dequeue();
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

            if (_frameTimes.Count <= 0)
            {
                _avgMs = 0f;
                _minMs = 0f;
                _maxMs = 0f;
                return;
            }

            _avgMs = sum / _frameTimes.Count;
            _minMs = min == float.MaxValue ? 0f : min;
            _maxMs = max;
        }

        public bool TryBuildContent(StringBuilder builder)
        {
            _builder.Clear();
            _builder.AppendLine("[FPS]");
            _builder.Append("Avg: ").Append(_avgMs > 0f ? 1000f / _avgMs : 0f).Append(" fps (").Append(_avgMs.ToString("0.0")).AppendLine(" ms)");
            _builder.Append("Best: ").Append(_minMs > 0f ? 1000f / _minMs : 0f).Append(" fps (").Append(_minMs.ToString("0.0")).AppendLine(" ms)");
            _builder.Append("Worst: ").Append(_maxMs > 0f ? 1000f / _maxMs : 0f).Append(" fps (").Append(_maxMs.ToString("0.0")).Append(" ms)");

            if (_builder.Length <= 0)
            {
                return false;
            }

            builder.Append(_builder);
            return true;
        }
    }
}
