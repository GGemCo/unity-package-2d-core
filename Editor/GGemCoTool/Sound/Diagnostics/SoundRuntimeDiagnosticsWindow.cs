using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Play Mode에서 사운드 범위, AudioClip 참조 카운트 및 메모리 추정값을 확인하는 디버그 창입니다.
    /// </summary>
    public sealed class SoundRuntimeDiagnosticsWindow : EditorWindow
    {
        private const string WindowTitle = "사운드 런타임 진단";

        private SoundRuntimeDiagnosticsSnapshot _snapshot;
        private Vector2 _scopeScroll;
        private Vector2 _clipScroll;
        private bool _includeRuntimeMemorySize = true;
        private string _filter = string.Empty;

        /// <summary>
        /// 사운드 런타임 진단 창을 엽니다.
        /// </summary>
        [MenuItem(
            ConfigEditor.NameToolSoundRuntimeDiagnostics,
            false,
            (int)ConfigEditor.ToolOrdering.DebugSoundRuntime)]
        public static void ShowWindow()
        {
            SoundRuntimeDiagnosticsWindow window = GetWindow<SoundRuntimeDiagnosticsWindow>(WindowTitle);
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

        /// <summary>
        /// 진단 옵션, 요약, 활성 범위 및 AudioClip 엔트리를 표시합니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "메모리 계산은 비용이 있으므로 자동 갱신하지 않습니다. 재현할 상태를 만든 뒤 새로고침 버튼을 눌러 확인하세요.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                _includeRuntimeMemorySize = EditorGUILayout.ToggleLeft(
                    "AudioClip 런타임 메모리 계산",
                    _includeRuntimeMemorySize,
                    GUILayout.Width(210f));
                _filter = EditorGUILayout.TextField("필터", _filter);
                if (GUILayout.Button("새로고침", GUILayout.Width(100f)))
                    RefreshSnapshot();
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서 사용할 수 있습니다.", MessageType.Warning);
                return;
            }

            if (_snapshot == null)
            {
                EditorGUILayout.HelpBox("새로고침을 눌러 현재 상태를 수집해주세요.", MessageType.None);
                return;
            }

            DrawSummary();
            DrawScopes();
            DrawClips();
        }

        /// <summary>
        /// 현재 AddressableLoaderSound에서 진단 스냅샷을 수집합니다.
        /// </summary>
        private void RefreshSnapshot()
        {
            AddressableLoaderSound loader = AddressableLoaderSound.Instance;
            _snapshot = loader != null
                ? loader.CreateDiagnosticsSnapshot(_includeRuntimeMemorySize)
                : null;

            if (_snapshot == null)
                ShowNotification(new GUIContent("AddressableLoaderSound를 찾지 못했습니다."));

            Repaint();
        }

        /// <summary>
        /// 현재 스냅샷의 총 Clip, 참조 수 및 메모리를 표시합니다.
        /// </summary>
        private void DrawSummary()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"수집 시각(UTC): {_snapshot.CapturedAtUtc:yyyy-MM-dd HH:mm:ss.fff}\n" +
                $"로드 Clip: {_snapshot.LoadedClipCount}, 로딩 중: {_snapshot.LoadingClipCount}, 전역 고정: {_snapshot.LegacyPinnedClipCount}\n" +
                $"범위 참조: {_snapshot.TotalScopeReferenceCount}, 재생 참조: {_snapshot.TotalPlaybackReferenceCount}\n" +
                $"AudioClip 메모리: {FormatBytes(_snapshot.TotalRuntimeMemoryBytes)}",
                MessageType.None);
        }

        /// <summary>
        /// 활성 사운드 범위의 로드 결과와 소요 시간을 표 형태로 표시합니다.
        /// </summary>
        private void DrawScopes()
        {
            EditorGUILayout.LabelField($"활성 범위 ({_snapshot.Scopes.Count})", EditorStyles.boldLabel);
            _scopeScroll = EditorGUILayout.BeginScrollView(_scopeScroll, GUILayout.Height(130f));
            IReadOnlyList<SoundScopeDiagnosticsEntry> scopes = _snapshot.Scopes;
            for (int i = 0; i < scopes.Count; i++)
            {
                SoundScopeDiagnosticsEntry scope = scopes[i];
                if (scope == null || !MatchesFilter(scope.ScopeKey))
                    continue;

                EditorGUILayout.SelectableLabel(
                    $"{scope.ScopeKey} | loaded={scope.LoadedKeyCount}, failed={scope.FailedKeyCount}, " +
                    $"load={scope.LoadDurationMilliseconds:0.###}ms, acquiredRealtime={scope.AcquiredRealtimeSeconds:0.###}",
                    GUILayout.Height(18f));
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 로드 엔트리별 범위/재생 참조, 고정 여부 및 메모리를 표시합니다.
        /// </summary>
        private void DrawClips()
        {
            EditorGUILayout.LabelField($"AudioClip 엔트리 ({_snapshot.Clips.Count})", EditorStyles.boldLabel);
            _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll);
            IReadOnlyList<SoundClipDiagnosticsEntry> clips = _snapshot.Clips;
            for (int i = 0; i < clips.Count; i++)
            {
                SoundClipDiagnosticsEntry clip = clips[i];
                if (clip == null || (!MatchesFilter(clip.AddressKey) && !MatchesFilter(clip.ClipName)))
                    continue;

                string state = clip.IsLoading ? "Loading" : clip.IsLoaded ? "Loaded" : "Empty";
                EditorGUILayout.SelectableLabel(
                    $"{clip.AddressKey} | clip={clip.ClipName}, state={state}, scope={clip.ScopeReferenceCount}, " +
                    $"playback={clip.PlaybackReferenceCount}, pinned={clip.IsLegacyPinned}, " +
                    $"length={clip.LengthSeconds:0.###}s, memory={FormatBytes(clip.RuntimeMemoryBytes)}",
                    GUILayout.Height(18f));
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 현재 필터 문자열이 대상 텍스트에 포함되는지 확인합니다.
        /// </summary>
        private bool MatchesFilter(string value)
        {
            return string.IsNullOrWhiteSpace(_filter) ||
                   (!string.IsNullOrWhiteSpace(value) &&
                    value.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 바이트 값을 B, KB, MB 단위 문자열로 변환합니다.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0L)
                return "0 B";
            if (bytes < 1024L)
                return $"{bytes} B";
            if (bytes < 1024L * 1024L)
                return $"{bytes / 1024d:0.##} KB";

            return $"{bytes / (1024d * 1024d):0.##} MB";
        }
    }
}
