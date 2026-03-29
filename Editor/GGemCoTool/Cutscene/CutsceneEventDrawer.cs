using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="CutsceneEvent"/>를 Unity Inspector에 표시하기 위한 커스텀 프로퍼티 드로어입니다.
    /// </summary>
    /// <remarks>
    /// 이벤트 타입 필드를 먼저 표시한 뒤,
    /// 선택된 타입에 대응하는 Payload 드로어를 통해 세부 데이터를 렌더링합니다.
    /// 타입 변경 시 필요한 Payload가 없으면 자동 초기화를 시도합니다.
    /// </remarks>
    [CustomPropertyDrawer(typeof(CutsceneEvent))]
    public class CutsceneEventDrawer : PropertyDrawer
    {
        /// <summary>
        /// 각 Inspector 라인 사이에 사용하는 수직 간격입니다.
        /// </summary>
        internal const float VerticalSpacing = 2f;

        /// <summary>
        /// 현재 이벤트 타입에 필요한 Payload가 없을 때 표시하는 경고 메시지입니다.
        /// </summary>
        private const string MissingPayloadMessage =
            "현재 이벤트 타입에 필요한 데이터 payload 가 없습니다. 클립을 다시 선택하거나 타입을 다시 설정해서 payload 를 초기화해주세요.";

        /// <summary>
        /// 대상 프로퍼티의 Inspector GUI를 그립니다.
        /// </summary>
        /// <param name="position">현재 프로퍼티가 그려질 영역입니다.</param>
        /// <param name="property">그릴 대상 <see cref="CutsceneEvent"/> 프로퍼티입니다.</param>
        /// <param name="label">Inspector에 표시할 레이블입니다.</param>
        /// <remarks>
        /// 처리 순서는 다음과 같습니다.
        /// 1. 이벤트 타입 필드를 표시합니다.
        /// 2. 타입 변경이 감지되면 해당 타입에 맞는 Payload를 초기화합니다.
        /// 3. 활성 Payload가 없으면 경고 메시지를 출력합니다.
        /// 4. 타입별 전용 드로어를 통해 상세 필드를 렌더링합니다.
        /// </remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProp = property.FindPropertyRelative("type");
            var eventType = (CutsceneEventType)typeProp.enumValueIndex;

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(current, typeProp);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                eventType = (CutsceneEventType)typeProp.enumValueIndex;
                CutsceneEventPayloadEditorUtility.EnsurePayloadForTypeChange(property, eventType);
            }

            current.y += current.height + VerticalSpacing;

            if (!CutsceneEventPayloadEditorUtility.HasActivePayload(property, eventType))
            {
                EditorGUI.HelpBox(current, MissingPayloadMessage, MessageType.Warning);
                EditorGUI.EndProperty();
                return;
            }

            var drawer = CutsceneEventDrawerRegistry.Get(eventType);
            drawer.Draw(current, property);

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 대상 프로퍼티를 표시하는 데 필요한 전체 높이를 계산합니다.
        /// </summary>
        /// <param name="property">높이를 계산할 대상 <see cref="CutsceneEvent"/> 프로퍼티입니다.</param>
        /// <param name="label">Inspector에 표시할 레이블입니다.</param>
        /// <returns>현재 이벤트 타입과 Payload 상태를 반영한 Inspector 높이입니다.</returns>
        /// <remarks>
        /// Payload가 없으면 경고 HelpBox 높이를 포함하고,
        /// Payload가 있으면 등록된 타입별 드로어의 높이를 추가합니다.
        /// </remarks>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");
            var eventType = (CutsceneEventType)typeProp.enumValueIndex;

            float baseHeight = EditorGUIUtility.singleLineHeight * 2f + 6f;
            if (!CutsceneEventPayloadEditorUtility.HasActivePayload(property, eventType))
            {
                return baseHeight + EditorStyles.helpBox.CalcHeight(
                    new GUIContent(MissingPayloadMessage),
                    EditorGUIUtility.currentViewWidth - 80f);
            }

            return baseHeight + CutsceneEventDrawerRegistry.Get(eventType).GetHeight(property);
        }
    }
}