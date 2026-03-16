#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
#if UNITY_2019_4_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 메모리 HUD 데이터를 계산하는 런타임 프로바이더입니다.
    /// </summary>
    public sealed class MemoryHud : IDebugHudProvider
    {
        private float _remainingTime;
        private long _monoUsed;
        private long _totalAlloc;
        private long _totalReserved;
        private long _gcBytes;
        private float _allocPerFrameAvg;
        private float _accumAlloc;
        private int _frames;
        private bool _initialized;
        private string _text = "[Memory]\nCollecting...";

        public DebugHudAnchor Anchor => DebugHudAnchor.BottomRight;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud) && DebugOptionRuntimeUtility.Resolve(settings.enableMemoryHud);
        }

        public void Initialize(GGemCoSettings settings)
        {
            _remainingTime = Mathf.Max(0.1f, settings != null ? settings.debugHudMemoryUpdateInterval : 0.5f);
            _accumAlloc = 0f;
            _frames = 0;
            _text = "[Memory]\nCollecting...";
            _initialized = true;
        }

        public void Tick(float unscaledDeltaTime, GGemCoSettings settings)
        {
            if (!_initialized)
            {
                Initialize(settings);
            }

            _accumAlloc += (float)GC.GetTotalMemory(false);
            _frames++;
            _remainingTime -= Mathf.Max(0f, unscaledDeltaTime);
            if (_remainingTime > 0f)
            {
                return;
            }

#if UNITY_2019_4_OR_NEWER
            _monoUsed = Profiler.GetMonoUsedSizeLong();
            _totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
            _totalReserved = Profiler.GetTotalReservedMemoryLong();
#else
            _monoUsed = GC.GetTotalMemory(false);
            _totalAlloc = _monoUsed;
            _totalReserved = 0;
#endif
            _gcBytes = GC.GetTotalMemory(false);
            _allocPerFrameAvg = _frames > 0 ? _accumAlloc / _frames : 0f;
            _remainingTime = Mathf.Max(0.1f, settings != null ? settings.debugHudMemoryUpdateInterval : 0.5f);
            _accumAlloc = 0f;
            _frames = 0;

            _text = $"[Memory]\n" +
                    $"Total Alloc:  {FormatBytes(_totalAlloc)}\n" +
                    $"Total Reserv: {FormatBytes(_totalReserved)}\n" +
                    $"Mono Used:    {FormatBytes(_monoUsed)}\n" +
                    $"GC Snapshot:  {FormatBytes(_gcBytes)}\n" +
                    $"GC/Frame(avg est): {FormatBytes((long)_allocPerFrameAvg)}";
        }

        public string GetText() => _text;

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB) return $"{(double)bytes / GB:0.00} GB";
            if (bytes >= MB) return $"{(double)bytes / MB:0.00} MB";
            if (bytes >= KB) return $"{(double)bytes / KB:0.00} KB";
            return $"{bytes} B";
        }
    }
}
#endif
