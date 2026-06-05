using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 TimelineAsset을 <see cref="UIEffectRuntimeSequence"/>로 베이크하는 클래스입니다.
    /// </summary>
    internal static class UIEffectTimelineBaker
    {
        private struct BakedEntry
        {
            public UIEffectRuntimeEvent runtimeEvent;
            public UIEffectPayloadBase payload;
        }

        /// <summary>
        /// TimelineAsset의 UI 효과 Clip들을 RuntimeSequence 에셋으로 변환합니다.
        /// </summary>
        /// <param name="timelineAsset">원본 TimelineAsset입니다.</param>
        /// <param name="outputPath">생성 또는 갱신할 RuntimeSequence 에셋 경로입니다.</param>
        /// <returns>베이크된 RuntimeSequence입니다.</returns>
        public static UIEffectRuntimeSequence Bake(TimelineAsset timelineAsset, string outputPath)
        {
            if (timelineAsset == null || string.IsNullOrWhiteSpace(outputPath))
            {
                return null;
            }

            if (!outputPath.EndsWith(".asset"))
            {
                outputPath += ".asset";
            }

            EnsureDirectory(outputPath);
            UIEffectRuntimeSequence sequence = AssetDatabase.LoadAssetAtPath<UIEffectRuntimeSequence>(outputPath);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<UIEffectRuntimeSequence>();
                AssetDatabase.CreateAsset(sequence, outputPath);
            }

            RemoveOldPayloads(outputPath);

            List<BakedEntry> entries = CollectEntries(timelineAsset);
            entries = entries.OrderBy(item => item.runtimeEvent.startTime).ThenBy(item => item.runtimeEvent.order).ToList();

            var payloads = new List<UIEffectPayloadBase>();
            var events = new List<UIEffectRuntimeEvent>();
            for (int i = 0; i < entries.Count; i++)
            {
                BakedEntry entry = entries[i];
                UIEffectRuntimeEvent runtimeEvent = entry.runtimeEvent;
                runtimeEvent.payloadIndex = i;
                events.Add(runtimeEvent);

                UIEffectPayloadBase payload = entry.payload;
                payload.name = $"{i:000}_{runtimeEvent.type}_{payload.targetKey}";
                AssetDatabase.AddObjectToAsset(payload, sequence);
                payloads.Add(payload);
            }

            sequence.sequenceKey = Path.GetFileNameWithoutExtension(outputPath);
            sequence.duration = Mathf.Max((float)timelineAsset.duration, events.Count > 0 ? events.Max(item => item.endTime) : 0f);
            sequence.events = events.ToArray();
            sequence.payloads = payloads.ToArray();

            EditorUtility.SetDirty(sequence);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return sequence;
        }

        /// <summary>
        /// TimelineAsset에서 UIEffectTrack의 Clip들을 순회해 베이크 항목을 수집합니다.
        /// </summary>
        private static List<BakedEntry> CollectEntries(TimelineAsset timelineAsset)
        {
            var entries = new List<BakedEntry>();
            int order = 0;
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is not UIEffectTrack)
                {
                    continue;
                }

                foreach (TimelineClip timelineClip in track.GetClips())
                {
                    if (timelineClip.asset is not UIEffectClipBase clip)
                    {
                        continue;
                    }

                    UIEffectPayloadBase payload = UIEffectTimelinePayloadFactory.CreatePayload(clip);
                    if (payload == null)
                    {
                        continue;
                    }

                    entries.Add(new BakedEntry
                    {
                        runtimeEvent = new UIEffectRuntimeEvent
                        {
                            type = clip.EventType,
                            startTime = Mathf.Max(0f, (float)timelineClip.start),
                            endTime = Mathf.Max(0f, (float)timelineClip.end),
                            order = order++,
                            payloadIndex = -1,
                        },
                        payload = payload,
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// 기존 RuntimeSequence 하위에 있던 Payload 서브 에셋을 제거합니다.
        /// </summary>
        private static void RemoveOldPayloads(string outputPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(outputPath);
            foreach (Object asset in assets)
            {
                if (asset is UIEffectPayloadBase)
                {
                    Object.DestroyImmediate(asset, true);
                }
            }
        }

        /// <summary>
        /// 출력 에셋 폴더가 없으면 생성합니다.
        /// </summary>
        private static void EnsureDirectory(string outputPath)
        {
            string directory = Path.GetDirectoryName(outputPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory) || AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string[] parts = directory.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
