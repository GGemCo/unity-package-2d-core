using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷신 이벤트 Payload의 생성, 검사 및 SerializedProperty 기반 실제 객체 접근을 지원하는 에디터 유틸리티입니다.
    /// </summary>
    internal static class CutsceneEventPayloadEditorUtility
    {
        /// <summary>
        /// Reflection으로 멤버를 조회할 때 사용하는 기본 플래그입니다.
        /// </summary>
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// 이벤트 타입과 해당 Payload 필드명을 매핑합니다.
        /// </summary>
        private static readonly IReadOnlyDictionary<CutsceneEventType, string> PayloadFieldNames =
            new Dictionary<CutsceneEventType, string>
            {
                { CutsceneEventType.CameraMove, nameof(CutsceneEvent.cameraMove) },
                { CutsceneEventType.CameraZoom, nameof(CutsceneEvent.cameraZoom) },
                { CutsceneEventType.CameraShake, nameof(CutsceneEvent.cameraShake) },
                { CutsceneEventType.CameraChangeTarget, nameof(CutsceneEvent.cameraChangeTarget) },
                { CutsceneEventType.CharacterMove, nameof(CutsceneEvent.characterMove) },
                { CutsceneEventType.CharacterAnimation, nameof(CutsceneEvent.characterAnimation) },
                { CutsceneEventType.CharacterAnimationTimeScale, nameof(CutsceneEvent.characterAnimationTimeScale) },
                { CutsceneEventType.DialogueBalloon, nameof(CutsceneEvent.dialogueBalloon) },
                { CutsceneEventType.ScreenFade, nameof(CutsceneEvent.screenFade) },
                { CutsceneEventType.OverlayText, nameof(CutsceneEvent.overlayText) },
                { CutsceneEventType.CharacterWhiteOverlay, nameof(CutsceneEvent.characterWhiteOverlay) },
                { CutsceneEventType.UiPanel, nameof(CutsceneEvent.uiPanel) },
                { CutsceneEventType.UiWindowVisibility, nameof(CutsceneEvent.uiWindowVisibility) },
                { CutsceneEventType.TimeScale, nameof(CutsceneEvent.timeScale) },
                { CutsceneEventType.WorldObjectVisibility, nameof(CutsceneEvent.worldObjectVisibility) },
                { CutsceneEventType.CharacterControlLock, nameof(CutsceneEvent.characterControlLock) },
                { CutsceneEventType.ScreenGlitch, nameof(CutsceneEvent.screenGlitch) },
                { CutsceneEventType.CharacterFade, nameof(CutsceneEvent.characterFade) },
                { CutsceneEventType.CharacterAirborne, nameof(CutsceneEvent.characterAirborne) },
            };

        /// <summary>
        /// 이벤트 타입 변경 시 해당 타입에 맞는 Payload를 생성 또는 보정합니다.
        /// </summary>
        /// <param name="eventProperty">대상 이벤트의 SerializedProperty입니다.</param>
        /// <param name="eventType">변경할 이벤트 타입입니다.</param>
        /// <returns>Payload를 정상적으로 보정했으면 true, 그렇지 않으면 false입니다.</returns>
        public static bool EnsurePayloadForTypeChange(SerializedProperty eventProperty, CutsceneEventType eventType)
        {
            if (eventProperty == null)
            {
                return false;
            }

            if (!TryGetCutsceneEvent(eventProperty, out var targetObject, out var cutsceneEvent))
            {
                return false;
            }

            Undo.RecordObject(targetObject, "Change Cutscene Event Type");
            cutsceneEvent.EnsureDataForType(eventType);
            EditorUtility.SetDirty(targetObject);
            eventProperty.serializedObject.Update();
            return true;
        }

        /// <summary>
        /// 클립 내 모든 이벤트의 Payload를 검사하고, 누락된 경우 생성합니다.
        /// </summary>
        /// <param name="clip">검사 대상 컷신 이벤트 클립입니다.</param>
        /// <returns>하나 이상의 이벤트가 변경되었으면 true입니다.</returns>
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

                if (HasActivePayload(cutsceneEvent))
                {
                    continue;
                }

                cutsceneEvent.EnsureDataForType();
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(clip);
            }

            return changed;
        }

        /// <summary>
        /// SerializedProperty 기준으로 지정한 이벤트 타입의 Payload가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="eventProperty">검사할 이벤트 SerializedProperty입니다.</param>
        /// <param name="eventType">검사할 이벤트 타입입니다.</param>
        /// <returns>해당 Payload가 존재하면 true입니다.</returns>
        public static bool HasActivePayload(SerializedProperty eventProperty, CutsceneEventType eventType)
        {
            if (eventProperty == null)
            {
                return false;
            }

            if (!TryGetPayloadFieldName(eventType, out var fieldName))
            {
                return false;
            }

            var payloadProperty = eventProperty.FindPropertyRelative(fieldName);
            return payloadProperty != null && payloadProperty.hasVisibleChildren;
        }

        /// <summary>
        /// 런타임 이벤트 객체 기준으로 활성 Payload가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="cutsceneEvent">검사할 컷신 이벤트입니다.</param>
        /// <returns>현재 타입에 대응하는 Payload 필드 값이 존재하면 true입니다.</returns>
        private static bool HasActivePayload(CutsceneEvent cutsceneEvent)
        {
            if (cutsceneEvent == null)
            {
                return false;
            }

            if (!TryGetPayloadFieldName(cutsceneEvent.type, out var fieldName))
            {
                return false;
            }

            var field = typeof(CutsceneEvent).GetField(fieldName, InstanceMemberFlags);
            return field != null && field.GetValue(cutsceneEvent) != null;
        }

        /// <summary>
        /// 이벤트 타입에 대응하는 Payload 필드명을 가져옵니다.
        /// </summary>
        /// <param name="eventType">조회할 이벤트 타입입니다.</param>
        /// <param name="fieldName">조회된 Payload 필드명입니다.</param>
        /// <returns>매핑된 필드명이 존재하면 true입니다.</returns>
        private static bool TryGetPayloadFieldName(CutsceneEventType eventType, out string fieldName)
        {
            return PayloadFieldNames.TryGetValue(eventType, out fieldName);
        }

        /// <summary>
        /// SerializedProperty로부터 실제 CutsceneEvent 객체와 소유 객체를 추출합니다.
        /// </summary>
        /// <param name="property">대상 SerializedProperty입니다.</param>
        /// <param name="targetObject">속성이 속한 Unity 오브젝트입니다.</param>
        /// <param name="cutsceneEvent">추출된 컷신 이벤트입니다.</param>
        /// <returns>추출에 성공하면 true입니다.</returns>
        private static bool TryGetCutsceneEvent(
            SerializedProperty property,
            out UnityEngine.Object targetObject,
            out CutsceneEvent cutsceneEvent)
        {
            targetObject = null;
            cutsceneEvent = null;

            if (property?.serializedObject == null)
            {
                return false;
            }

            targetObject = property.serializedObject.targetObject;
            if (targetObject == null)
            {
                return false;
            }

            cutsceneEvent = GetValueFromPropertyPath(targetObject, property.propertyPath) as CutsceneEvent;
            return cutsceneEvent != null;
        }

        /// <summary>
        /// SerializedProperty 경로를 따라 실제 객체 그래프를 탐색하여 값을 반환합니다.
        /// </summary>
        /// <param name="root">탐색 시작 루트 객체입니다.</param>
        /// <param name="propertyPath">Unity SerializedProperty 경로입니다.</param>
        /// <returns>경로 해석 결과 객체이며, 실패 시 null입니다.</returns>
        private static object GetValueFromPropertyPath(object root, string propertyPath)
        {
            if (root == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                return null;
            }

            object current = root;
            foreach (var token in EnumeratePropertyPathTokens(propertyPath))
            {
                current = GetMemberValue(current, token);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// Unity property path를 탐색 가능한 토큰 시퀀스로 변환합니다.
        /// </summary>
        /// <param name="propertyPath">원본 property path입니다.</param>
        /// <returns>분해된 경로 토큰 열거입니다.</returns>
        private static IEnumerable<string> EnumeratePropertyPathTokens(string propertyPath)
        {
            string normalizedPath = propertyPath.Replace(".Array.data[", "[");
            return normalizedPath.Split('.');
        }

        /// <summary>
        /// 단일 멤버 토큰을 해석하여 필드/프로퍼티 또는 리스트 요소 값을 반환합니다.
        /// </summary>
        /// <param name="source">현재 탐색 중인 객체입니다.</param>
        /// <param name="memberToken">멤버 이름 또는 인덱스 토큰입니다.</param>
        /// <returns>조회된 값이며 실패 시 null입니다.</returns>
        private static object GetMemberValue(object source, string memberToken)
        {
            if (source == null || string.IsNullOrWhiteSpace(memberToken))
            {
                return null;
            }

            if (!TryParseIndexedToken(memberToken, out var memberName, out var index))
            {
                return GetFieldOrPropertyValue(source, memberToken);
            }

            var collectionObject = string.IsNullOrEmpty(memberName)
                ? source
                : GetFieldOrPropertyValue(source, memberName);

            if (collectionObject is not IList list)
            {
                return null;
            }

            return index >= 0 && index < list.Count
                ? list[index]
                : null;
        }

        /// <summary>
        /// 인덱서 형태의 멤버 토큰을 파싱합니다.
        /// </summary>
        /// <param name="memberToken">파싱할 토큰입니다.</param>
        /// <param name="memberName">인덱서 앞의 멤버명입니다.</param>
        /// <param name="index">파싱된 인덱스입니다.</param>
        /// <returns>인덱서 토큰 파싱에 성공하면 true입니다.</returns>
        private static bool TryParseIndexedToken(string memberToken, out string memberName, out int index)
        {
            memberName = null;
            index = -1;

            int startBracketIndex = memberToken.IndexOf('[');
            if (startBracketIndex < 0)
            {
                return false;
            }

            int endBracketIndex = memberToken.IndexOf(']', startBracketIndex);
            if (endBracketIndex <= startBracketIndex)
            {
                return false;
            }

            memberName = memberToken.Substring(0, startBracketIndex);
            string indexText = memberToken.Substring(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1);
            return int.TryParse(indexText, out index);
        }

        /// <summary>
        /// 지정한 객체에서 필드 또는 프로퍼티 값을 Reflection으로 조회합니다.
        /// </summary>
        /// <param name="source">조회 대상 객체입니다.</param>
        /// <param name="memberName">조회할 멤버 이름입니다.</param>
        /// <returns>조회된 값이며, 멤버가 없으면 null입니다.</returns>
        private static object GetFieldOrPropertyValue(object source, string memberName)
        {
            if (source == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type currentType = source.GetType();
            while (currentType != null)
            {
                var field = currentType.GetField(memberName, InstanceMemberFlags);
                if (field != null)
                {
                    return field.GetValue(source);
                }

                var property = currentType.GetProperty(memberName, InstanceMemberFlags);
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
