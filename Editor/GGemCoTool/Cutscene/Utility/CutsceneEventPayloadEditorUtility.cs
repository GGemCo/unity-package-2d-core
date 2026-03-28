using System;
using System.Collections;
using System.Reflection;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneEventPayloadEditorUtility
    {
        public static bool EnsurePayloadForTypeChange(SerializedProperty eventProperty, CutsceneEventType eventType)
        {
            if (eventProperty == null)
            {
                return false;
            }

            if (!TryGetCutsceneEvent(eventProperty, out var targetObject, out var cutsceneEvent) || cutsceneEvent == null)
            {
                return false;
            }

            Undo.RecordObject(targetObject, "Change Cutscene Event Type");
            cutsceneEvent.EnsureDataForType(eventType);
            EditorUtility.SetDirty(targetObject);
            eventProperty.serializedObject.Update();
            return true;
        }

        public static bool EnsurePayloadsForClip(CutsceneEventClip clip)
        {
            if (clip == null || clip.events == null)
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < clip.events.Count; i++)
            {
                var cutsceneEvent = clip.events[i];
                if (cutsceneEvent == null)
                {
                    cutsceneEvent = new CutsceneEvent();
                    clip.events[i] = cutsceneEvent;
                    changed = true;
                }

                if (!HasActivePayload(cutsceneEvent))
                {
                    cutsceneEvent.EnsureDataForType();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(clip);
            }

            return changed;
        }

        public static bool HasActivePayload(SerializedProperty eventProperty, CutsceneEventType eventType)
        {
            string fieldName = GetPayloadFieldName(eventType);
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            var payloadProp = eventProperty.FindPropertyRelative(fieldName);
            return payloadProp != null && payloadProp.hasVisibleChildren;
        }

        private static bool HasActivePayload(CutsceneEvent cutsceneEvent)
        {
            if (cutsceneEvent == null)
            {
                return false;
            }

            string fieldName = GetPayloadFieldName(cutsceneEvent.type);
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            var field = typeof(CutsceneEvent).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null && field.GetValue(cutsceneEvent) != null;
        }

        private static string GetPayloadFieldName(CutsceneEventType eventType)
        {
            return eventType switch
            {
                CutsceneEventType.CameraMove => "cameraMove",
                CutsceneEventType.CameraZoom => "cameraZoom",
                CutsceneEventType.CameraShake => "cameraShake",
                CutsceneEventType.CameraChangeTarget => "cameraChangeTarget",
                CutsceneEventType.CharacterMove => "characterMove",
                CutsceneEventType.CharacterAnimation => "characterAnimation",
                CutsceneEventType.CharacterAnimationTimeScale => "characterAnimationTimeScale",
                CutsceneEventType.DialogueBalloon => "dialogueBalloon",
                CutsceneEventType.ScreenFade => "screenFade",
                CutsceneEventType.OverlayText => "overlayText",
                CutsceneEventType.CharacterWhiteOverlay => "characterWhiteOverlay",
                CutsceneEventType.UiPanel => "uiPanel",
                CutsceneEventType.UiWindowVisibility => "uiWindowVisibility",
                CutsceneEventType.TimeScale => "timeScale",
                _ => string.Empty,
            };
        }

        private static bool TryGetCutsceneEvent(SerializedProperty property, out UnityEngine.Object targetObject, out CutsceneEvent cutsceneEvent)
        {
            targetObject = property.serializedObject.targetObject;
            cutsceneEvent = GetValueFromPropertyPath(targetObject, property.propertyPath) as CutsceneEvent;
            return targetObject != null && cutsceneEvent != null;
        }

        private static object GetValueFromPropertyPath(object root, string propertyPath)
        {
            if (root == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return null;
            }

            object current = root;
            string normalizedPath = propertyPath.Replace(".Array.data[", "[");
            string[] parts = normalizedPath.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                current = GetMemberValue(current, parts[i]);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static object GetMemberValue(object source, string memberToken)
        {
            if (source == null || string.IsNullOrWhiteSpace(memberToken))
            {
                return null;
            }

            int bracketIndex = memberToken.IndexOf('[');
            if (bracketIndex >= 0)
            {
                string memberName = memberToken.Substring(0, bracketIndex);
                object listObject = string.IsNullOrEmpty(memberName) ? source : GetFieldOrPropertyValue(source, memberName);
                if (listObject is IList list)
                {
                    int endBracketIndex = memberToken.IndexOf(']', bracketIndex);
                    if (endBracketIndex > bracketIndex && int.TryParse(memberToken.Substring(bracketIndex + 1, endBracketIndex - bracketIndex - 1), out int index))
                    {
                        return index >= 0 && index < list.Count ? list[index] : null;
                    }
                }

                return null;
            }

            return GetFieldOrPropertyValue(source, memberToken);
        }

        private static object GetFieldOrPropertyValue(object source, string memberName)
        {
            if (source == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type currentType = source.GetType();
            while (currentType != null)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo field = currentType.GetField(memberName, flags);
                if (field != null)
                {
                    return field.GetValue(source);
                }

                PropertyInfo property = currentType.GetProperty(memberName, flags);
                if (property != null)
                {
                    return property.GetValue(source);
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
