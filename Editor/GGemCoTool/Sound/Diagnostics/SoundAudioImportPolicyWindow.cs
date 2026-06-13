using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 사운드 종류에 맞는 AudioImporter 설정을 검사하고 선택적으로 일괄 적용하는 에디터 창입니다.
    /// </summary>
    public sealed class SoundAudioImportPolicyWindow : EditorWindow
    {
        private sealed class AuditEntry
        {
            public StruckTableSoundResource Row;
            public string AssetPath;
            public AudioClip Clip;
            public AudioImporter Importer;
            public AudioImporterSampleSettings RecommendedSampleSettings;
            public bool RecommendedPreloadAudioData;
            public bool RecommendedLoadInBackground;
            public string Summary;
        }

        private const string WindowTitle = "사운드 Import 정책";
        private readonly List<AuditEntry> _entries = new List<AuditEntry>();
        private Vector2 _scrollPosition;
        private float _shortSfxPcmThresholdSeconds = 1f;
        private float _streamingVorbisQuality = 0.7f;
        private int _missingAssetCount;

        /// <summary>
        /// 사운드 AudioImporter 정책 검사 창을 엽니다.
        /// </summary>
        [MenuItem(
            ConfigEditor.NameToolSoundAudioImportPolicy,
            false,
            (int)ConfigEditor.ToolOrdering.SoundAudioImportPolicy)]
        public static void ShowWindow()
        {
            SoundAudioImportPolicyWindow window = GetWindow<SoundAudioImportPolicyWindow>(WindowTitle);
            window.minSize = new Vector2(760f, 500f);
            window.Show();
        }

        /// <summary>
        /// 정책 옵션, 검사/적용 버튼 및 불일치 목록을 표시합니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "권장 시작점: BGM/환경음은 Streaming+Vorbis, 짧은 SFX는 DecompressOnLoad+PCM, " +
                "그 외 SFX는 DecompressOnLoad+ADPCM입니다. 실제 대상 플랫폼에서는 Unity Profiler로 다시 확인하세요.",
                MessageType.Info);

            _shortSfxPcmThresholdSeconds = EditorGUILayout.Slider(
                "짧은 SFX 기준(초)",
                _shortSfxPcmThresholdSeconds,
                0.1f,
                5f);
            _streamingVorbisQuality = EditorGUILayout.Slider(
                "Streaming Vorbis Quality",
                _streamingVorbisQuality,
                0.1f,
                1f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("현재 설정 검사", GUILayout.Height(32f)))
                    Audit();

                using (new EditorGUI.DisabledScope(_entries.Count == 0))
                {
                    if (GUILayout.Button("불일치 항목에 권장 정책 적용", GUILayout.Height(32f)))
                        ApplyRecommendedSettings();
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                $"불일치: {_entries.Count}, 에셋 누락: {_missingAssetCount}",
                _entries.Count == 0 && _missingAssetCount == 0 ? MessageType.Info : MessageType.Warning);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _entries.Count; i++)
            {
                AuditEntry entry = _entries[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"{entry.Row.Type} / resourceUid={entry.Row.Uid} / soundUid={entry.Row.SoundUid}",
                        EditorStyles.boldLabel);
                    EditorGUILayout.SelectableLabel(entry.AssetPath, GUILayout.Height(18f));
                    EditorGUILayout.LabelField(entry.Summary, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("에셋 선택", GUILayout.Width(90f)))
                    {
                        Selection.activeObject = entry.Clip;
                        EditorGUIUtility.PingObject(entry.Clip);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 사운드 테이블의 모든 실제 리소스와 AudioImporter 설정을 비교합니다.
        /// </summary>
        private void Audit()
        {
            _entries.Clear();
            _missingAssetCount = 0;
            IReadOnlyList<StruckTableSoundResource> rows =
                SoundEditorAssetUtility.CollectResourceRows(true);

            for (int i = 0; i < rows.Count; i++)
            {
                StruckTableSoundResource row = rows[i];
                string assetPath = SoundEditorAssetUtility.ResolveAssetPath(row);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (clip == null || importer == null)
                {
                    _missingAssetCount++;
                    Debug.LogWarning(
                        $"[SoundImportPolicy] AudioClip 또는 AudioImporter를 찾지 못했습니다. resourceUid={row.Uid}, path={assetPath}");
                    continue;
                }

                ResolveRecommendedSettings(
                    row,
                    clip,
                    out AudioImporterSampleSettings sampleSettings,
                    out bool preloadAudioData,
                    out bool loadInBackground);
                AudioImporterSampleSettings current = importer.defaultSampleSettings;
                if (IsMatching(importer, current, sampleSettings, preloadAudioData, loadInBackground))
                    continue;

                _entries.Add(new AuditEntry
                {
                    Row = row,
                    AssetPath = assetPath,
                    Clip = clip,
                    Importer = importer,
                    RecommendedSampleSettings = sampleSettings,
                    RecommendedPreloadAudioData = preloadAudioData,
                    RecommendedLoadInBackground = loadInBackground,
                    Summary =
                        $"현재: {current.loadType}/{current.compressionFormat}, preload={current.preloadAudioData}, background={importer.loadInBackground}\n" +
                        $"권장: {sampleSettings.loadType}/{sampleSettings.compressionFormat}, preload={preloadAudioData}, background={loadInBackground}",
                });
            }

            ShowNotification(new GUIContent($"검사 완료: 불일치 {_entries.Count}개"));
            Repaint();
        }

        /// <summary>
        /// 검사에서 발견한 불일치 AudioImporter에 권장 정책을 적용하고 다시 임포트합니다.
        /// </summary>
        private void ApplyRecommendedSettings()
        {
            if (_entries.Count == 0)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                WindowTitle,
                $"불일치 AudioClip {_entries.Count}개의 Import 설정을 변경하고 다시 임포트합니다.",
                "적용",
                "취소");
            if (!confirmed)
                return;

            int appliedCount = 0;
            try
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    AuditEntry entry = _entries[i];
                    if (entry?.Importer == null)
                        continue;

                    EditorUtility.DisplayProgressBar(
                        WindowTitle,
                        entry.AssetPath,
                        (float)i / _entries.Count);

                    AudioImporterSampleSettings sampleSettings = entry.RecommendedSampleSettings;
                    
                    sampleSettings.preloadAudioData = entry.RecommendedPreloadAudioData;
                    entry.Importer.defaultSampleSettings = sampleSettings;
                    entry.Importer.loadInBackground = entry.RecommendedLoadInBackground;

                    entry.Importer.SaveAndReimport();
                    appliedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Audit();
            Debug.Log($"[SoundImportPolicy] 권장 Import 정책 적용 완료. count={appliedCount}");
        }

        /// <summary>
        /// 사운드 종류와 길이를 기준으로 권장 기본 Sample 설정과 로딩 정책을 계산합니다.
        /// </summary>
        private void ResolveRecommendedSettings(
            StruckTableSoundResource row,
            AudioClip clip,
            out AudioImporterSampleSettings sampleSettings,
            out bool preloadAudioData,
            out bool loadInBackground)
        {
            sampleSettings = row.Type == SoundConstants.Type.Sfx
                ? ResolveSfxSettings(clip)
                : ResolveStreamingSettings();
            preloadAudioData = row.Type == SoundConstants.Type.Sfx;
            loadInBackground = row.Type != SoundConstants.Type.Sfx;
        }

        /// <summary>
        /// BGM과 환경음에 사용할 Streaming/Vorbis 설정을 생성합니다.
        /// </summary>
        private AudioImporterSampleSettings ResolveStreamingSettings()
        {
            return new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.Streaming,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = _streamingVorbisQuality,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
            };
        }

        /// <summary>
        /// SFX 길이에 따라 PCM 또는 ADPCM 기반 DecompressOnLoad 설정을 생성합니다.
        /// </summary>
        private AudioImporterSampleSettings ResolveSfxSettings(AudioClip clip)
        {
            bool usePcm = clip != null && clip.length <= _shortSfxPcmThresholdSeconds;
            return new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.DecompressOnLoad,
                compressionFormat = usePcm
                    ? AudioCompressionFormat.PCM
                    : AudioCompressionFormat.ADPCM,
                quality = 1f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
            };
        }

        /// <summary>
        /// 현재 AudioImporter 설정이 권장 정책과 같은지 비교합니다.
        /// </summary>
        private static bool IsMatching(
            AudioImporter importer,
            AudioImporterSampleSettings current,
            AudioImporterSampleSettings recommended,
            bool preloadAudioData,
            bool loadInBackground)
        {
            return current.loadType == recommended.loadType &&
                   current.compressionFormat == recommended.compressionFormat &&
                   (recommended.compressionFormat != AudioCompressionFormat.Vorbis ||
                    Mathf.Approximately(current.quality, recommended.quality)) &&
                   current.preloadAudioData == preloadAudioData &&
                   importer.loadInBackground == loadInBackground;
        }
    }
}
