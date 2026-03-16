using System;
using System.Text;
using UnityEngine;
#if UNITY_2019_4_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 메모리 사용량을 수집하는 HUD Provider 입니다.
    /// </summary>
    [DebugHudProvider(200)]
    public sealed class MemoryHud : IDebugHudProvider
    {
        private readonly StringBuilder _builder = new(192);

        private long _monoUsed;
        private long _totalAllocated;
        private long _totalReserved;
        private long _gcSnapshot;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && settings.EnableDebugHud && settings.enableMemoryHud;
        }

        public float GetUpdateInterval(GGemCoSettings settings)
        {
            return settings != null ? Mathf.Max(0.1f, settings.debugHudMemoryUpdateInterval) : 1f;
        }

        public void Reset()
        {
            _monoUsed = 0L;
            _totalAllocated = 0L;
            _totalReserved = 0L;
            _gcSnapshot = 0L;
        }

        public void Tick(float elapsedSeconds)
        {
#if UNITY_2019_4_OR_NEWER
            _monoUsed = Profiler.GetMonoUsedSizeLong();
            _totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            _totalReserved = Profiler.GetTotalReservedMemoryLong();
#else
            _monoUsed = GC.GetTotalMemory(false);
            _totalAllocated = _monoUsed;
            _totalReserved = 0L;
#endif
            _gcSnapshot = GC.GetTotalMemory(false);
        }

        public bool TryBuildContent(StringBuilder builder)
        {
            _builder.Clear();
            _builder.AppendLine("[Memory]");
            _builder.Append("Total Alloc:  ").AppendLine(FormatBytes(_totalAllocated));
            _builder.Append("Total Reserv: ").AppendLine(FormatBytes(_totalReserved));
            _builder.Append("Mono Used:    ").AppendLine(FormatBytes(_monoUsed));
            _builder.Append("GC Snapshot:  ").Append(FormatBytes(_gcSnapshot));

            if (_builder.Length <= 0)
            {
                return false;
            }

            builder.Append(_builder);
            return true;
        }

        private static string FormatBytes(long bytes)
        {
            const long kilobyte = 1024;
            const long megabyte = kilobyte * 1024;
            const long gigabyte = megabyte * 1024;

            if (bytes >= gigabyte) return $"{(double)bytes / gigabyte:0.00} GB";
            if (bytes >= megabyte) return $"{(double)bytes / megabyte:0.00} MB";
            if (bytes >= kilobyte) return $"{(double)bytes / kilobyte:0.00} KB";
            return $"{bytes} B";
        }
    }
}
