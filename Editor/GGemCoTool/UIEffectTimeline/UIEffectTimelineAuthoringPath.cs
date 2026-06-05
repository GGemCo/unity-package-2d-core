using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 Timeline 원본과 RuntimeSequence 산출물의 UID 기반 경로 규칙을 관리합니다.
    /// </summary>
    internal static class UIEffectTimelineAuthoringPath
    {
        /// <summary>
        /// UI 효과 Timeline 원본 에셋을 저장하는 기본 폴더입니다.
        /// </summary>
        public const string TimelineFolder = "Assets/Editor/UIEffectTimeline";

        /// <summary>
        /// Timeline 에셋 파일 확장자입니다.
        /// </summary>
        private const string TimelineExtension = ".playable";

        /// <summary>
        /// UI 효과 UID를 기준으로 Timeline 원본 파일명을 생성합니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <returns>UID 기반 Timeline 원본 파일명입니다.</returns>
        public static string GetTimelineFileName(int uiEffectUid)
        {
            return $"TimelineUIEffect_{uiEffectUid}{TimelineExtension}";
        }

        /// <summary>
        /// UI 효과 UID를 기준으로 Timeline 원본 에셋의 권장 경로를 생성합니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <returns>Unity 프로젝트 기준 Timeline 원본 경로입니다.</returns>
        public static string GetTimelineAssetPath(int uiEffectUid)
        {
            return NormalizeAssetPath($"{TimelineFolder}/{GetTimelineFileName(uiEffectUid)}");
        }

        /// <summary>
        /// UI 효과 UID를 기준으로 RuntimeSequence 에셋 파일명을 생성합니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <returns>UID 기반 RuntimeSequence 파일명입니다.</returns>
        public static string GetRuntimeSequenceFileName(int uiEffectUid)
        {
            return $"UIEffectRuntimeSequence_{uiEffectUid}.asset";
        }

        /// <summary>
        /// UI 효과 UID를 기준으로 RuntimeSequence 에셋 저장 경로를 생성합니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <returns>Unity 프로젝트 기준 RuntimeSequence 경로입니다.</returns>
        public static string GetRuntimeSequenceAssetPath(int uiEffectUid)
        {
            return NormalizeAssetPath($"{ConfigAddressablePath.UIEffect.RuntimeSequence}/{GetRuntimeSequenceFileName(uiEffectUid)}");
        }

        /// <summary>
        /// UI 효과 UID를 기준으로 Addressables RuntimeSequence Key를 생성합니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <returns>RuntimeSequence Addressables Key입니다.</returns>
        public static string GetRuntimeSequenceKey(int uiEffectUid)
        {
            return ConfigAddressableKey.GetUIEffectRuntimeSequenceKey(uiEffectUid);
        }

        /// <summary>
        /// 권장 경로 또는 동일 폴더 하위의 단일 후보를 기준으로 Timeline 원본 에셋을 찾습니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <param name="timeline">찾은 Timeline 에셋입니다.</param>
        /// <param name="assetPath">찾은 Timeline 에셋의 Unity 프로젝트 경로입니다.</param>
        /// <param name="candidateCount">후보 개수입니다. 중복 후보 경고에 사용합니다.</param>
        /// <returns>Timeline 에셋을 하나로 확정했으면 true를 반환합니다.</returns>
        public static bool TryFindTimeline(int uiEffectUid, out TimelineAsset timeline, out string assetPath, out int candidateCount)
        {
            timeline = null;
            assetPath = null;
            candidateCount = 0;

            if (uiEffectUid <= 0)
                return false;

            string expectedPath = GetTimelineAssetPath(uiEffectUid);
            timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(expectedPath);
            if (timeline != null)
            {
                assetPath = expectedPath;
                candidateCount = 1;
                return true;
            }

            if (!AssetDatabase.IsValidFolder(TimelineFolder))
                return false;

            string expectedFileNameWithoutExtension = Path.GetFileNameWithoutExtension(GetTimelineFileName(uiEffectUid));
            string[] guids = AssetDatabase.FindAssets($"{expectedFileNameWithoutExtension} t:TimelineAsset", new[] { TimelineFolder });
            var candidatePaths = new List<string>();

            foreach (string guid in guids)
            {
                string path = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));
                if (!IsMatchingTimelinePath(uiEffectUid, path))
                    continue;

                candidatePaths.Add(path);
            }

            candidateCount = candidatePaths.Count;
            if (candidatePaths.Count != 1)
                return false;

            assetPath = candidatePaths[0];
            timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            return timeline != null;
        }

        /// <summary>
        /// 지정한 에셋 경로가 UID 기반 Timeline 파일명 규칙에 맞는지 확인합니다.
        /// </summary>
        /// <param name="uiEffectUid">UI 효과 UID입니다.</param>
        /// <param name="assetPath">검사할 Unity 프로젝트 기준 에셋 경로입니다.</param>
        /// <returns>경로가 규칙에 맞으면 true를 반환합니다.</returns>
        private static bool IsMatchingTimelinePath(int uiEffectUid, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalizedPath = NormalizeAssetPath(assetPath);
            string normalizedFolder = NormalizeAssetPath(TimelineFolder);
            if (!normalizedPath.StartsWith($"{normalizedFolder}/", StringComparison.OrdinalIgnoreCase))
                return false;

            string expectedFileName = GetTimelineFileName(uiEffectUid);
            return string.Equals(Path.GetFileName(normalizedPath), expectedFileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Unity 에셋 경로 비교가 안정적으로 동작하도록 경로 구분자를 정규화합니다.
        /// </summary>
        /// <param name="assetPath">정규화할 에셋 경로입니다.</param>
        /// <returns>슬래시 구분자로 정규화된 에셋 경로입니다.</returns>
        private static string NormalizeAssetPath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : assetPath.Replace('\\', '/').TrimEnd('/');
        }
    }
}
