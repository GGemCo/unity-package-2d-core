using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// window 테이블의 UI 프리팹에서 명시적 클릭 사운드, 수동 선언, 애니메이션 이벤트를 분석합니다.
    /// </summary>
    internal sealed class UiSoundUsageScanner
    {
        private readonly TableWindow _tableWindow;

        /// <summary>
        /// UI 윈도우 사운드 사용처 분석기를 생성합니다.
        /// </summary>
        /// <param name="tableWindow">UI 윈도우 테이블입니다.</param>
        public UiSoundUsageScanner(TableWindow tableWindow)
        {
            _tableWindow = tableWindow;
        }

        /// <summary>
        /// 게임에서 사용하는 모든 UI 윈도우 프리팹을 분석하여 매니페스트 원본 레코드를 추가합니다.
        /// </summary>
        /// <param name="target">발견한 사용처를 추가할 결과 목록입니다.</param>
        /// <param name="result">진단 메시지를 기록할 생성 결과입니다.</param>
        public void Scan(List<SoundUsageManifestBuildRecord> target, SoundUsageManifestBuildResult result)
        {
            if (target == null || _tableWindow == null)
                return;

            IReadOnlyDictionary<int, StruckTableWindow> windows = _tableWindow.GetAll();
            if (windows == null)
                return;

            foreach (KeyValuePair<int, StruckTableWindow> pair in windows.OrderBy(item => item.Key))
            {
                StruckTableWindow window = pair.Value;
                if (window == null || window.Uid <= 0 || !window.UseInGame || string.IsNullOrWhiteSpace(window.PrefabName))
                    continue;

                string prefabPath = ResolveWindowPrefabPath(window.PrefabName, result);
                if (string.IsNullOrWhiteSpace(prefabPath))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    result?.AddWarning(
                        $"UI 윈도우 프리팹을 로드하지 못했습니다. windowUid={window.Uid}, path={prefabPath}");
                    continue;
                }

                ScanExplicitClickSounds(window, prefab, prefabPath, target);
                ScanManualDeclarations(window, prefab, prefabPath, target);
                ScanAnimationSounds(window, prefab, prefabPath, target, result);
            }
        }

        /// <summary>
        /// ClickSoundEventBroadcaster에 직접 입력된 soundUid를 UI 윈도우 범위에 추가합니다.
        /// soundUid가 0인 타입 기반 버튼 사운드는 전역 UI 공용 범위에서 관리하므로 제외합니다.
        /// </summary>
        private static void ScanExplicitClickSounds(
            StruckTableWindow window,
            GameObject prefab,
            string prefabPath,
            List<SoundUsageManifestBuildRecord> target)
        {
            ClickSoundEventBroadcaster[] broadcasters =
                prefab.GetComponentsInChildren<ClickSoundEventBroadcaster>(true);
            for (int i = 0; i < broadcasters.Length; i++)
            {
                ClickSoundEventBroadcaster broadcaster = broadcasters[i];
                if (broadcaster == null || broadcaster.soundUid <= 0)
                    continue;

                target.Add(new SoundUsageManifestBuildRecord
                {
                    ScopeType = SoundUsageManifestScopeType.UiWindow,
                    ScopeUid = window.Uid,
                    SoundUid = broadcaster.soundUid,
                    SourceType = SoundUsageManifestSourceType.UiClick,
                    SourceUid = window.Uid,
                    SourcePath = $"{prefabPath}#Click/{BuildTransformPath(prefab.transform, broadcaster.transform)}",
                    Memo = $"window={window.Name}, explicit ClickSoundEventBroadcaster",
                });
            }
        }

        /// <summary>
        /// UIWindowSoundUsageDeclaration에 수동으로 등록된 sound UID를 매니페스트 보고서에도 포함합니다.
        /// 런타임에서는 수동 선언과 매니페스트를 중복 제거하여 합치므로 같은 UID가 있어도 안전합니다.
        /// </summary>
        private static void ScanManualDeclarations(
            StruckTableWindow window,
            GameObject prefab,
            string prefabPath,
            List<SoundUsageManifestBuildRecord> target)
        {
            UIWindowSoundUsageDeclaration[] declarations =
                prefab.GetComponentsInChildren<UIWindowSoundUsageDeclaration>(true);
            for (int declarationIndex = 0; declarationIndex < declarations.Length; declarationIndex++)
            {
                UIWindowSoundUsageDeclaration declaration = declarations[declarationIndex];
                if (declaration == null || declaration.SoundUids == null)
                    continue;

                for (int soundIndex = 0; soundIndex < declaration.SoundUids.Count; soundIndex++)
                {
                    int soundUid = declaration.SoundUids[soundIndex];
                    if (soundUid <= 0)
                        continue;

                    target.Add(new SoundUsageManifestBuildRecord
                    {
                        ScopeType = SoundUsageManifestScopeType.UiWindow,
                        ScopeUid = window.Uid,
                        SoundUid = soundUid,
                        SourceType = SoundUsageManifestSourceType.UiDeclaration,
                        SourceUid = window.Uid,
                        SourcePath = $"{prefabPath}#Declaration/{BuildTransformPath(prefab.transform, declaration.transform)}",
                        Memo = $"window={window.Name}, UIWindowSoundUsageDeclaration",
                    });
                }
            }
        }

        /// <summary>
        /// UI 프리팹의 Animator 및 Spine 애니메이션 사운드 이벤트를 UI 윈도우 범위에 추가합니다.
        /// </summary>
        private static void ScanAnimationSounds(
            StruckTableWindow window,
            GameObject prefab,
            string prefabPath,
            List<SoundUsageManifestBuildRecord> target,
            SoundUsageManifestBuildResult result)
        {
            IReadOnlyList<AnimationSoundUsage> usages =
                AnimationSoundEventScanner.ScanPrefab(prefab, prefabPath, result);
            for (int i = 0; i < usages.Count; i++)
            {
                AnimationSoundUsage usage = usages[i];
                if (usage == null || usage.SoundUid <= 0)
                    continue;

                target.Add(new SoundUsageManifestBuildRecord
                {
                    ScopeType = SoundUsageManifestScopeType.UiWindow,
                    ScopeUid = window.Uid,
                    SoundUid = usage.SoundUid,
                    SourceType = SoundUsageManifestSourceType.UiAnimation,
                    SourceUid = window.Uid,
                    SourcePath = usage.SourcePath,
                    Memo = $"window={window.Name}, {usage.Memo}",
                });
            }
        }

        /// <summary>
        /// 기존 UIWindow 폴더 규칙으로 프리팹을 먼저 찾고, 실패하면 UIWindows 폴더에서 정확한 파일명을 검색합니다.
        /// </summary>
        /// <param name="prefabName">window 테이블의 PrefabName 값입니다.</param>
        /// <param name="result">중복 또는 누락 경고를 기록할 생성 결과입니다.</param>
        /// <returns>찾은 프리팹 에셋 경로이며 없으면 빈 문자열입니다.</returns>
        private static string ResolveWindowPrefabPath(
            string prefabName,
            SoundUsageManifestBuildResult result)
        {
            string folderName = prefabName.Replace("UIWindow", string.Empty);
            string expectedPath = $"{ConfigEditor.PathUIWindow}/{folderName}/{prefabName}.prefab";
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(expectedPath)))
                return expectedPath;

            string[] guids = AssetDatabase.FindAssets(
                $"{prefabName} t:Prefab",
                new[] { ConfigEditor.PathUIWindow });
            List<string> exactMatches = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        prefabName,
                        StringComparison.Ordinal))
                {
                    exactMatches.Add(path);
                }
            }

            if (exactMatches.Count == 0)
            {
                result?.AddWarning(
                    $"window 테이블의 UI 프리팹을 찾지 못했습니다. prefabName={prefabName}, expected={expectedPath}");
                return string.Empty;
            }

            exactMatches.Sort(StringComparer.Ordinal);
            if (exactMatches.Count > 1)
            {
                result?.AddWarning(
                    $"같은 이름의 UI 프리팹이 여러 개입니다. 첫 경로를 사용합니다. prefabName={prefabName}, selected={exactMatches[0]}");
            }

            return exactMatches[0];
        }

        /// <summary>
        /// 프리팹 루트에서 대상 Transform까지의 상대 계층 경로를 생성합니다.
        /// </summary>
        private static string BuildTransformPath(Transform root, Transform target)
        {
            if (target == null)
                return string.Empty;
            if (root == null || target == root)
                return target.name;

            Stack<string> names = new Stack<string>();
            Transform current = target;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                    break;
                current = current.parent;
            }

            return string.Join("/", names);
        }
    }
}
