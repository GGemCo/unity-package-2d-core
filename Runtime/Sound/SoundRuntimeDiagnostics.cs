using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 로드된 AudioClip 한 건의 런타임 참조 상태를 나타냅니다.
    /// </summary>
    [Serializable]
    public sealed class SoundClipDiagnosticsEntry
    {
        public string AddressKey;
        public string ClipName;
        public int ScopeReferenceCount;
        public int PlaybackReferenceCount;
        public bool IsLegacyPinned;
        public bool IsLoaded;
        public bool IsLoading;
        public float LengthSeconds;
        public long RuntimeMemoryBytes;
    }

    /// <summary>
    /// 활성 사운드 범위 임대 한 건의 런타임 상태를 나타냅니다.
    /// </summary>
    [Serializable]
    public sealed class SoundScopeDiagnosticsEntry
    {
        public string ScopeKey;
        public int LoadedKeyCount;
        public int FailedKeyCount;
        public double LoadDurationMilliseconds;
        public float AcquiredRealtimeSeconds;
    }

    /// <summary>
    /// 현재 사운드 로더와 범위 매니저의 진단 스냅샷입니다.
    /// </summary>
    [Serializable]
    public sealed class SoundRuntimeDiagnosticsSnapshot
    {
        public DateTime CapturedAtUtc;
        public int LoadedClipCount;
        public int LoadingClipCount;
        public int LegacyPinnedClipCount;
        public int TotalScopeReferenceCount;
        public int TotalPlaybackReferenceCount;
        public long TotalRuntimeMemoryBytes;
        public IReadOnlyList<SoundClipDiagnosticsEntry> Clips = Array.Empty<SoundClipDiagnosticsEntry>();
        public IReadOnlyList<SoundScopeDiagnosticsEntry> Scopes = Array.Empty<SoundScopeDiagnosticsEntry>();
    }
}
