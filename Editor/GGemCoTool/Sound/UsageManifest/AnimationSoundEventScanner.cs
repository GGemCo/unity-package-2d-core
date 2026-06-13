using System;
using System.Collections.Generic;
using System.Globalization;
using GGemCo2DCore;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

#if GGEMCO_USE_SPINE
using Spine;
using Spine.Unity;
#endif

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 캐릭터 또는 UI 프리팹에 연결된 Unity AnimationClip과 Spine 애니메이션의 사운드 이벤트를 분석합니다.
    /// </summary>
    internal static class AnimationSoundEventScanner
    {
        private const string UnitySoundEventFunctionName = "OnAnimationEventSound";

        /// <summary>
        /// 프리팹 하위의 Animator와 Spine 컴포넌트에서 사용하는 대표 sound UID를 수집합니다.
        /// </summary>
        /// <param name="prefab">분석할 프리팹 에셋입니다.</param>
        /// <param name="prefabPath">진단 및 매니페스트에 기록할 프리팹 경로입니다.</param>
        /// <param name="result">분석 중 발생한 경고를 기록할 생성 결과입니다.</param>
        /// <returns>애니메이션 이벤트에서 발견한 사운드 사용 목록입니다.</returns>
        public static IReadOnlyList<AnimationSoundUsage> ScanPrefab(
            GameObject prefab,
            string prefabPath,
            SoundUsageManifestBuildResult result)
        {
            if (prefab == null)
                return Array.Empty<AnimationSoundUsage>();

            List<AnimationSoundUsage> usages = new List<AnimationSoundUsage>();
            HashSet<string> registered = new HashSet<string>(StringComparer.Ordinal);

            ScanUnityAnimatorEvents(prefab, prefabPath, usages, registered, result);
#if GGEMCO_USE_SPINE
            ScanSpineEvents(prefab, prefabPath, usages, registered, result);
#endif
            return usages;
        }

        /// <summary>
        /// 프리팹 하위 Animator가 참조하는 모든 AnimationClip의 사운드 이벤트를 수집합니다.
        /// </summary>
        private static void ScanUnityAnimatorEvents(
            GameObject prefab,
            string prefabPath,
            List<AnimationSoundUsage> target,
            HashSet<string> registered,
            SoundUsageManifestBuildResult result)
        {
            Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
            HashSet<int> scannedClipInstanceIds = new HashSet<int>();

            for (int animatorIndex = 0; animatorIndex < animators.Length; animatorIndex++)
            {
                Animator animator = animators[animatorIndex];
                RuntimeAnimatorController controller = animator != null
                    ? animator.runtimeAnimatorController
                    : null;
                if (controller == null)
                    continue;

                AnimationClip[] clips = controller.animationClips;
                if (clips == null)
                    continue;

                for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
                {
                    AnimationClip clip = clips[clipIndex];
                    if (clip == null || !scannedClipInstanceIds.Add(clip.GetInstanceID()))
                        continue;

                    AnimationEvent[] events;
                    try
                    {
                        events = AnimationUtility.GetAnimationEvents(clip);
                    }
                    catch (Exception ex)
                    {
                        result?.AddWarning(
                            $"AnimationClip 이벤트를 읽지 못했습니다. prefab={prefabPath}, clip={clip.name}, error={ex.Message}");
                        continue;
                    }

                    for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                    {
                        AnimationEvent animationEvent = events[eventIndex];
                        if (!IsSoundAnimationEvent(animationEvent))
                            continue;

                        HashSet<int> soundUids = new HashSet<int>();
                        if (animationEvent.intParameter > 0)
                            soundUids.Add(animationEvent.intParameter);

                        AppendSoundUidsFromPayload(
                            animationEvent.stringParameter,
                            soundUids,
                            result,
                            $"prefab={prefabPath}, clip={clip.name}, time={animationEvent.time:0.###}");

                        string sourcePath =
                            $"{prefabPath}#Animator/{BuildTransformPath(prefab.transform, animator.transform)}/{clip.name}@{animationEvent.time.ToString("0.###", CultureInfo.InvariantCulture)}";
                        foreach (int soundUid in soundUids)
                        {
                            AppendUsage(
                                soundUid,
                                sourcePath,
                                $"Unity AnimationClip 이벤트: {clip.name}",
                                target,
                                registered);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unity AnimationEvent가 사운드 재생 이벤트인지 확인합니다.
        /// </summary>
        /// <param name="animationEvent">검사할 AnimationEvent입니다.</param>
        /// <returns>지원하는 사운드 이벤트 함수명이면 true입니다.</returns>
        private static bool IsSoundAnimationEvent(AnimationEvent animationEvent)
        {
            if (animationEvent == null || string.IsNullOrWhiteSpace(animationEvent.functionName))
                return false;

            return string.Equals(
                       animationEvent.functionName,
                       UnitySoundEventFunctionName,
                       StringComparison.Ordinal)
                   || string.Equals(
                       animationEvent.functionName,
                       AnimationConstants.EventNameSound,
                       StringComparison.Ordinal);
        }

#if GGEMCO_USE_SPINE
        /// <summary>
        /// 프리팹 하위 SkeletonAnimation과 SkeletonGraphic의 모든 Spine 사운드 이벤트를 수집합니다.
        /// </summary>
        private static void ScanSpineEvents(
            GameObject prefab,
            string prefabPath,
            List<AnimationSoundUsage> target,
            HashSet<string> registered,
            SoundUsageManifestBuildResult result)
        {
            HashSet<int> scannedSkeletonDataAssetIds = new HashSet<int>();

            SkeletonAnimation[] skeletonAnimations =
                prefab.GetComponentsInChildren<SkeletonAnimation>(true);
            for (int i = 0; i < skeletonAnimations.Length; i++)
            {
                ScanSpineComponent(
                    prefab,
                    prefabPath,
                    skeletonAnimations[i],
                    scannedSkeletonDataAssetIds,
                    target,
                    registered,
                    result);
            }

            SkeletonGraphic[] skeletonGraphics = prefab.GetComponentsInChildren<SkeletonGraphic>(true);
            for (int i = 0; i < skeletonGraphics.Length; i++)
            {
                ScanSpineComponent(
                    prefab,
                    prefabPath,
                    skeletonGraphics[i],
                    scannedSkeletonDataAssetIds,
                    target,
                    registered,
                    result);
            }
        }

        /// <summary>
        /// 한 Spine 컴포넌트의 SkeletonDataAsset에서 사운드 이벤트를 수집합니다.
        /// </summary>
        private static void ScanSpineComponent(
            GameObject prefab,
            string prefabPath,
            Component spineComponent,
            HashSet<int> scannedSkeletonDataAssetIds,
            List<AnimationSoundUsage> target,
            HashSet<string> registered,
            SoundUsageManifestBuildResult result)
        {
            if (spineComponent == null)
                return;

            SkeletonDataAsset skeletonDataAsset = ResolveSkeletonDataAsset(spineComponent);
            if (skeletonDataAsset == null ||
                !scannedSkeletonDataAssetIds.Add(skeletonDataAsset.GetInstanceID()))
            {
                return;
            }

            SkeletonData skeletonData;
            try
            {
                skeletonData = skeletonDataAsset.GetSkeletonData(true);
            }
            catch (Exception ex)
            {
                result?.AddWarning(
                    $"Spine SkeletonData를 읽지 못했습니다. prefab={prefabPath}, component={spineComponent.name}, error={ex.Message}");
                return;
            }

            if (skeletonData == null)
                return;

            foreach (Spine.Animation animation in skeletonData.Animations)
            {
                if (animation == null)
                    continue;

                foreach (Timeline timeline in animation.Timelines)
                {
                    if (timeline is not EventTimeline eventTimeline)
                        continue;

                    for (int eventIndex = 0; eventIndex < eventTimeline.FrameCount; eventIndex++)
                    {
                        Spine.Event spineEvent = eventTimeline.Events[eventIndex];
                        if (spineEvent?.Data == null ||
                            !string.Equals(
                                spineEvent.Data.Name,
                                AnimationConstants.EventNameSound,
                                StringComparison.Ordinal))
                        {
                            continue;
                        }

                        HashSet<int> soundUids = new HashSet<int>();
                        if (spineEvent.Int > 0)
                            soundUids.Add(spineEvent.Int);

                        AppendSoundUidsFromPayload(
                            spineEvent.String,
                            soundUids,
                            result,
                            $"prefab={prefabPath}, spineAnimation={animation.Name}, time={spineEvent.Time:0.###}");

                        string sourcePath =
                            $"{prefabPath}#Spine/{BuildTransformPath(prefab.transform, spineComponent.transform)}/{animation.Name}@{spineEvent.Time.ToString("0.###", CultureInfo.InvariantCulture)}";
                        foreach (int soundUid in soundUids)
                        {
                            AppendUsage(
                                soundUid,
                                sourcePath,
                                $"Spine 애니메이션 이벤트: {animation.Name}",
                                target,
                                registered);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Spine 컴포넌트의 직렬화 필드에서 SkeletonDataAsset 참조를 안전하게 조회합니다.
        /// Spine 런타임 버전에 따른 공개 프로퍼티 차이를 피하기 위해 SerializedObject를 사용합니다.
        /// </summary>
        /// <param name="spineComponent">SkeletonAnimation 또는 SkeletonGraphic 컴포넌트입니다.</param>
        /// <returns>연결된 SkeletonDataAsset이며 없으면 null입니다.</returns>
        private static SkeletonDataAsset ResolveSkeletonDataAsset(Component spineComponent)
        {
            SerializedObject serializedObject = new SerializedObject(spineComponent);
            SerializedProperty property = serializedObject.FindProperty("skeletonDataAsset")
                                          ?? serializedObject.FindProperty("m_SkeletonDataAsset");
            return property?.objectReferenceValue as SkeletonDataAsset;
        }
#endif

        /// <summary>
        /// JSON 객체, JSON 배열, 정수 문자열로 저장된 사운드 이벤트 페이로드에서 UID를 수집합니다.
        /// </summary>
        /// <param name="payload">AnimationEvent 또는 Spine Event의 문자열 페이로드입니다.</param>
        /// <param name="target">발견한 대표 sound UID를 추가할 집합입니다.</param>
        /// <param name="result">파싱 경고를 기록할 생성 결과입니다.</param>
        /// <param name="context">경고에 표시할 원본 위치입니다.</param>
        private static void AppendSoundUidsFromPayload(
            string payload,
            HashSet<int> target,
            SoundUsageManifestBuildResult result,
            string context)
        {
            if (string.IsNullOrWhiteSpace(payload) || target == null)
                return;

            string trimmed = payload.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int directUid))
            {
                if (directUid > 0)
                    target.Add(directUid);
                return;
            }

            try
            {
                JToken token = JToken.Parse(trimmed);
                CollectSoundUidsFromToken(token, target);
            }
            catch (Exception ex)
            {
                result?.AddWarning(
                    $"사운드 AnimationEvent 페이로드를 해석하지 못했습니다. {context}, payload={payload}, error={ex.Message}");
            }
        }

        /// <summary>
        /// JSON 토큰을 재귀 순회하여 Uid 또는 SoundUid 필드와 정수 원소를 수집합니다.
        /// </summary>
        /// <param name="token">순회할 JSON 토큰입니다.</param>
        /// <param name="target">발견한 대표 sound UID를 추가할 집합입니다.</param>
        private static void CollectSoundUidsFromToken(JToken token, HashSet<int> target)
        {
            if (token == null || target == null)
                return;

            switch (token.Type)
            {
                case JTokenType.Array:
                    foreach (JToken child in token.Children())
                        CollectSoundUidsFromToken(child, target);
                    return;

                case JTokenType.Object:
                    foreach (JProperty property in ((JObject)token).Properties())
                    {
                        bool isUidProperty =
                            string.Equals(property.Name, "Uid", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(property.Name, "SoundUid", StringComparison.OrdinalIgnoreCase);
                        if (isUidProperty ||
                            property.Value.Type == JTokenType.Object ||
                            property.Value.Type == JTokenType.Array)
                        {
                            CollectSoundUidsFromToken(property.Value, target);
                        }
                    }
                    return;

                case JTokenType.Integer:
                    int integerUid = token.Value<int>();
                    if (integerUid > 0)
                        target.Add(integerUid);
                    return;

                case JTokenType.String:
                    if (int.TryParse(
                            token.Value<string>(),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int stringUid) &&
                        stringUid > 0)
                    {
                        target.Add(stringUid);
                    }
                    return;
            }
        }

        /// <summary>
        /// 유효한 사운드 사용처를 동일 원본 위치 기준으로 중복 없이 추가합니다.
        /// </summary>
        private static void AppendUsage(
            int soundUid,
            string sourcePath,
            string memo,
            List<AnimationSoundUsage> target,
            HashSet<string> registered)
        {
            if (soundUid <= 0 || target == null || registered == null)
                return;

            string key = $"{soundUid}|{sourcePath}";
            if (!registered.Add(key))
                return;

            target.Add(new AnimationSoundUsage
            {
                SoundUid = soundUid,
                SourcePath = sourcePath ?? string.Empty,
                Memo = memo ?? string.Empty,
            });
        }

        /// <summary>
        /// 프리팹 루트에서 대상 Transform까지의 상대 계층 경로를 생성합니다.
        /// </summary>
        /// <param name="root">프리팹 루트 Transform입니다.</param>
        /// <param name="target">경로를 생성할 대상 Transform입니다.</param>
        /// <returns>슬래시로 구분된 상대 계층 경로입니다.</returns>
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
