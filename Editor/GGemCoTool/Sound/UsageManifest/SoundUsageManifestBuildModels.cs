using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터 자동 분석 중 발견한 사운드 사용처 한 건입니다.
    /// </summary>
    internal sealed class SoundUsageManifestBuildRecord
    {
        public SoundUsageManifestScopeType ScopeType;
        public int ScopeUid;
        public int SoundUid;
        public SoundUsageManifestSourceType SourceType;
        public int SourceUid;
        public string SourcePath;
        public string Memo;

        /// <summary>
        /// 같은 사용처가 여러 분석 경로에서 발견되어도 한 행만 기록하도록 안정적인 중복 키를 생성합니다.
        /// </summary>
        /// <returns>범위, 사운드, 원본 정보를 조합한 중복 판정 키입니다.</returns>
        public string BuildDeduplicationKey()
        {
            return string.Join(
                "|",
                (int)ScopeType,
                ScopeUid,
                SoundUid,
                (int)SourceType,
                SourceUid,
                SourcePath ?? string.Empty);
        }
    }

    /// <summary>
    /// 한 AnimationClip 또는 Spine 애니메이션 이벤트에서 발견한 사운드 UID 정보입니다.
    /// </summary>
    internal sealed class AnimationSoundUsage
    {
        public int SoundUid;
        public string SourcePath;
        public string Memo;
    }

    /// <summary>
    /// 사운드 사용 매니페스트 생성 결과와 사용자에게 표시할 진단 메시지를 보관합니다.
    /// </summary>
    public sealed class SoundUsageManifestBuildResult
    {
        private readonly List<string> _messages = new List<string>();

        public bool Succeeded { get; internal set; }
        public string OutputPath { get; internal set; }
        public int RecordCount { get; internal set; }
        public int MapScopeCount { get; internal set; }
        public int UiWindowScopeCount { get; internal set; }
        public int GlobalScopeCount { get; internal set; }
        public int ContributorCount { get; internal set; }
        public int WarningCount { get; internal set; }
        public bool RuntimeTablePackRebuilt { get; internal set; }

        /// <summary>
        /// 생성 과정에서 수집한 경고 및 진행 메시지입니다.
        /// </summary>
        public IReadOnlyList<string> Messages => _messages;

        /// <summary>
        /// 사용자에게 보여줄 일반 진행 메시지를 추가합니다.
        /// </summary>
        /// <param name="message">추가할 메시지입니다.</param>
        internal void AddMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                _messages.Add(message);
        }

        /// <summary>
        /// 누락 또는 분석 불가 항목을 경고로 기록합니다.
        /// </summary>
        /// <param name="message">추가할 경고 메시지입니다.</param>
        internal void AddWarning(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            WarningCount++;
            _messages.Add($"[경고] {message}");
        }

        /// <summary>
        /// 예외가 발생한 생성 과정을 실패 상태로 기록합니다.
        /// </summary>
        /// <param name="message">실패 원인 메시지입니다.</param>
        internal void SetFailure(string message)
        {
            Succeeded = false;
            AddMessage($"[실패] {message}");
        }
    }
}
