using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Cutscene Timeline <-> Json 변환 공용 유틸리티입니다.
    /// </summary>
    internal static class CutsceneTimelineJsonUtility
    {
        private static readonly JsonSerializerSettings CutsceneJsonSettings = CreateCutsceneJsonSettings();

        public static bool TryCreateTimelineFromJsonAsset(TextAsset jsonAsset, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            timelineAsset = null;
            error = null;

            if (jsonAsset == null)
            {
                error = "JSON 파일이 선택되지 않았습니다.";
                return false;
            }

            try
            {
                var cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(jsonAsset.text, CutsceneJsonSettings);
                if (cutsceneData == null)
                {
                    error = "Json 파싱 결과가 비어 있습니다.";
                    return false;
                }

                return TryCreateTimelineFromData(cutsceneData, timelineAssetPath, out timelineAsset, out error);
            }
            catch (Exception e)
            {
                error = $"Json 파싱 실패: {e.Message}";
                return false;
            }
        }

        public static bool TryCreateTimelineFromData(CutsceneData cutsceneData, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            timelineAsset = null;
            error = null;

            if (cutsceneData == null)
            {
                error = "CutsceneData가 null 입니다.";
                return false;
            }

            if (cutsceneData.events == null)
            {
                error = "이벤트 목록이 없습니다.";
                return false;
            }

            try
            {
                EnsureFolderExistsForAssetPath(timelineAssetPath);
                DeleteAssetIfExists(timelineAssetPath);

                timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timelineAsset, timelineAssetPath);

                var trackMap = new Dictionary<CutsceneEventType, TrackAsset>();
                foreach (var cutsceneEvent in cutsceneData.events)
                {
                    if (cutsceneEvent == null)
                    {
                        continue;
                    }

                    cutsceneEvent.EnsureDataForType();

                    TrackAsset track;
                    if (!trackMap.TryGetValue(cutsceneEvent.type, out track))
                    {
                        track = timelineAsset.CreateTrack<CutsceneEventTrack>(null, $"{cutsceneEvent.type} Track");
                        trackMap.Add(cutsceneEvent.type, track);
                    }

                    var clip = track.CreateClip<CutsceneEventClip>();
                    clip.start = cutsceneEvent.time;
                    clip.duration = cutsceneEvent.duration > 0f ? cutsceneEvent.duration : 1.0f;

                    var clipAsset = clip.asset as CutsceneEventClip;
                    if (clipAsset == null)
                    {
                        continue;
                    }

                    clipAsset.events.Clear();
                    clipAsset.SetEvent(CloneEvent(cutsceneEvent));
                    EditorUtility.SetDirty(clipAsset);
                }

                EditorUtility.SetDirty(timelineAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                error = $"Timeline 생성 실패: {e.Message}";
                return false;
            }
        }

        public static bool TryExportTimelineToJson(TimelineAsset timeline, string jsonPath, out CutsceneData data, out string error)
        {
            data = null;
            error = null;

            if (timeline == null)
            {
                error = "TimelineAsset이 선택되지 않았습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                error = "Json 저장 경로가 비어 있습니다.";
                return false;
            }

            try
            {
                var events = CollectEventsFromTimeline(timeline, out error);
                if (events == null)
                {
                    return false;
                }

                data = new CutsceneData
                {
                    duration = events.Count > 0 ? events[events.Count - 1].time + events[events.Count - 1].duration : 0f,
                    events = events
                };

                var directory = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(data, Formatting.Indented, CutsceneJsonSettings);

                File.WriteAllText(jsonPath, json);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                error = $"Json 저장 실패: {e.Message}";
                return false;
            }
        }

        private static List<CutsceneEvent> CollectEventsFromTimeline(TimelineAsset timeline, out string error)
        {
            error = null;
            var events = new List<CutsceneEvent>();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (!(track is CutsceneEventTrack) || track.muted)
                {
                    continue;
                }

                foreach (var clip in track.GetClips())
                {
                    var cutsceneClip = clip.asset as CutsceneEventClip;
                    if (cutsceneClip == null || cutsceneClip.events == null)
                    {
                        continue;
                    }

                    foreach (var cutsceneEvent in cutsceneClip.events)
                    {
                        if (cutsceneEvent == null)
                        {
                            continue;
                        }

                        cutsceneEvent.EnsureDataForType();
                        if (!ValidateEvent(cutsceneEvent, out error))
                        {
                            return null;
                        }

                        var copy = CloneEvent(cutsceneEvent);
                        copy.time = (float)clip.start;
                        copy.duration = (float)clip.duration;
                        events.Add(copy);
                    }
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));
            return events;
        }

        private static bool ValidateEvent(CutsceneEvent cutsceneEvent, out string error)
        {
            error = null;

            if (cutsceneEvent.type == CutsceneEventType.CharacterMove &&
                cutsceneEvent.characterMove != null &&
                cutsceneEvent.characterMove.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            if (cutsceneEvent.type == CutsceneEventType.CameraChangeTarget &&
                cutsceneEvent.cameraChangeTarget != null &&
                cutsceneEvent.cameraChangeTarget.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            return true;
        }

        private static CutsceneEvent CloneEvent(CutsceneEvent source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new CutsceneEvent
            {
                time = source.time,
                duration = source.duration,
                type = source.type,
                cameraMove = source.type == CutsceneEventType.CameraMove ? CloneCameraMoveData(source.cameraMove) : null,
                cameraZoom = source.type == CutsceneEventType.CameraZoom ? CloneCameraZoomData(source.cameraZoom) : null,
                cameraShake = source.type == CutsceneEventType.CameraShake ? CloneCameraShakeData(source.cameraShake) : null,
                cameraChangeTarget = source.type == CutsceneEventType.CameraChangeTarget ? CloneCameraChangeTargetData(source.cameraChangeTarget) : null,
                characterMove = source.type == CutsceneEventType.CharacterMove ? CloneCharacterMoveData(source.characterMove) : null,
                characterAnimation = source.type == CutsceneEventType.CharacterAnimation ? CloneCharacterAnimationData(source.characterAnimation) : null,
                dialogueBalloon = source.type == CutsceneEventType.DialogueBalloon ? CloneDialogueBalloonData(source.dialogueBalloon) : null,
                screenFade = source.type == CutsceneEventType.ScreenFade ? CloneScreenFadeData(source.screenFade) : null,
                overlayText = source.type == CutsceneEventType.OverlayText ? CloneOverlayTextData(source.overlayText) : null,
                characterWhiteOverlay = source.type == CutsceneEventType.CharacterWhiteOverlay ? CloneCharacterWhiteOverlayData(source.characterWhiteOverlay) : null,
                uiPanel = source.type == CutsceneEventType.UiPanel ? CloneUiPanelData(source.uiPanel) : null,
            };

            clone.EnsureDataForType();
            return clone;
        }

        private static CameraMoveData CloneCameraMoveData(CameraMoveData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraMoveData
            {
                startPosition = source.startPosition,
                endPosition = source.endPosition,
                endTargetPlayer = source.endTargetPlayer,
                easing = source.easing,
            };
        }

        private static CameraZoomData CloneCameraZoomData(CameraZoomData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraZoomData
            {
                startSize = source.startSize,
                endSize = source.endSize,
                easing = source.easing,
            };
        }

        private static CameraShakeData CloneCameraShakeData(CameraShakeData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraShakeData
            {
                duration = source.duration,
                shakeIntensity = source.shakeIntensity,
                leftStrength = source.leftStrength,
                rightStrength = source.rightStrength,
                downStrength = source.downStrength,
                upStrength = source.upStrength,
                repeatCount = source.repeatCount,
                useUnscaledTime = source.useUnscaledTime,
            };
        }

        private static CameraChangeTargetData CloneCameraChangeTargetData(CameraChangeTargetData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraChangeTargetData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
            };
        }

        private static CharacterMoveData CloneCharacterMoveData(CharacterMoveData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterMoveData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                characterMoveSpeed = source.characterMoveSpeed,
                startPosition = source.startPosition,
                endPosition = source.endPosition,
            };
        }

        private static CharacterAnimationData CloneCharacterAnimationData(CharacterAnimationData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterAnimationData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                spawnPosition = source.spawnPosition,
                isFlip = source.isFlip,
                animationName = source.animationName,
                animationLoop = source.animationLoop,
                animationTimeScale = source.animationTimeScale,
            };
        }

        private static DialogueBalloonData CloneDialogueBalloonData(DialogueBalloonData source)
        {
            if (source == null)
            {
                return null;
            }

            return new DialogueBalloonData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                message = source.message,
                fontSize = source.fontSize,
            };
        }

        private static ScreenFadeData CloneScreenFadeData(ScreenFadeData source)
        {
            if (source == null)
            {
                return null;
            }

            return new ScreenFadeData
            {
                color = source.color,
                fromAlpha = source.fromAlpha,
                toAlpha = source.toAlpha,
                holdFinalState = source.holdFinalState,
                useUnscaledTime = source.useUnscaledTime,
                easing = source.easing,
                renderMode = source.renderMode,
                sortingLayerName = source.sortingLayerName,
                orderInLayer = source.orderInLayer,
                planeDistance = source.planeDistance,
            };
        }

        private static OverlayTextData CloneOverlayTextData(OverlayTextData source)
        {
            if (source == null)
            {
                return null;
            }

            return new OverlayTextData
            {
                text = source.text,
                anchoredPosition = source.anchoredPosition,
                sizeDelta = source.sizeDelta,
                fontSize = source.fontSize,
                textColor = source.textColor,
                maxAlpha = source.maxAlpha,
                fadeIn = source.fadeIn,
                fadeOut = source.fadeOut,
                easing = source.easing,
                useUnscaledTime = source.useUnscaledTime,
            };
        }

        private static CharacterWhiteOverlayData CloneCharacterWhiteOverlayData(CharacterWhiteOverlayData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterWhiteOverlayData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
                color = source.color,
                fromStrength = source.fromStrength,
                toStrength = source.toStrength,
                restoreOnStop = source.restoreOnStop,
                refreshTargetsOnTrigger = source.refreshTargetsOnTrigger,
                useUnscaledTime = source.useUnscaledTime,
                easing = source.easing,
            };
        }


        private static UiPanelData CloneUiPanelData(UiPanelData source)
        {
            if (source == null)
            {
                return null;
            }

            return new UiPanelData
            {
                panelId = source.panelId,
                createIfMissing = source.createIfMissing,
                destroyOnStop = source.destroyOnStop,
                hideOnStop = source.hideOnStop,
                anchorMin = source.anchorMin,
                anchorMax = source.anchorMax,
                pivot = source.pivot,
                fromAnchoredPosition = source.fromAnchoredPosition,
                toAnchoredPosition = source.toAnchoredPosition,
                fromSizeDelta = source.fromSizeDelta,
                toSizeDelta = source.toSizeDelta,
                siblingIndex = source.siblingIndex,
                fromColor = source.fromColor,
                toColor = source.toColor,
                fromAlpha = source.fromAlpha,
                toAlpha = source.toAlpha,
                raycastTarget = source.raycastTarget,
                easing = source.easing,
                useUnscaledTime = source.useUnscaledTime,
            };
        }

        private static JsonSerializerSettings CreateCutsceneJsonSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new UnityColorJsonConverter(),
                },
            };
        }

        private static void EnsureFolderExistsForAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var normalizedPath = assetPath.Replace("\\", "/");
            var directoryPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            var segments = directoryPath.Replace("\\", "/").Split('/');
            if (segments.Length == 0 || segments[0] != "Assets")
            {
                return;
            }

            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = segments[i];
                var combined = $"{current}/{next}";
                if (!AssetDatabase.IsValidFolder(combined))
                {
                    AssetDatabase.CreateFolder(current, next);
                }

                current = combined;
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private sealed class UnityColorJsonConverter : JsonConverter<Color>
        {
            public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("r");
                writer.WriteValue(value.r);
                writer.WritePropertyName("g");
                writer.WriteValue(value.g);
                writer.WritePropertyName("b");
                writer.WriteValue(value.b);
                writer.WritePropertyName("a");
                writer.WriteValue(value.a);
                writer.WriteEndObject();
            }

            public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return default;
                }

                var token = JToken.Load(reader);
                if (token.Type != JTokenType.Object)
                {
                    return existingValue;
                }

                return new Color(
                    token.Value<float?>("r") ?? existingValue.r,
                    token.Value<float?>("g") ?? existingValue.g,
                    token.Value<float?>("b") ?? existingValue.b,
                    token.Value<float?>("a") ?? existingValue.a);
            }
        }
    }
}
