using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 기본 형태의 Cutscene 이벤트를 위한 Inspector Drawer입니다.
    /// 지정된 프로퍼티를 그대로 노출하는 단순 위임형 UI를 제공합니다.
    /// </summary>
    internal sealed class CutsceneDefaultEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        /// <summary>
        /// 이벤트 데이터가 저장된 SerializedProperty의 상대 경로 이름입니다.
        /// </summary>
        private readonly string _propertyName;

        /// <summary>
        /// 이 Drawer가 담당하는 컷씬 이벤트 타입입니다.
        /// </summary>
        public CutsceneEventType EventType { get; }

        /// <summary>
        /// <see cref="CutsceneDefaultEventTypeDrawer"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="eventType">이 Drawer가 처리할 컷씬 이벤트 타입입니다.</param>
        /// <param name="propertyName">이벤트 데이터가 저장된 프로퍼티 이름입니다.</param>
        public CutsceneDefaultEventTypeDrawer(CutsceneEventType eventType, string propertyName)
        {
            EventType = eventType;
            _propertyName = propertyName;
        }

        /// <summary>
        /// Inspector에서 해당 이벤트의 UI를 그립니다.
        /// 내부 프로퍼티를 그대로 위임하여 렌더링합니다.
        /// </summary>
        /// <param name="position">UI를 그릴 영역(Rect)</param>
        /// <param name="eventProperty">컷씬 이벤트의 SerializedProperty</param>
        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var payloadProperty = eventProperty.FindPropertyRelative(_propertyName);
            if (payloadProperty == null)
            {
                // TODO: 잘못된 propertyName이 전달된 경우. 필요 시 로그 출력 고려
                return;
            }

            // 내부 프로퍼티를 Unity 기본 PropertyField로 렌더링
            EditorGUI.PropertyField(position, payloadProperty, true);
        }

        /// <summary>
        /// Inspector에서 해당 이벤트 UI의 높이를 계산합니다.
        /// 내부 프로퍼티의 높이를 그대로 반환합니다.
        /// </summary>
        /// <param name="eventProperty">컷씬 이벤트의 SerializedProperty</param>
        /// <returns>렌더링에 필요한 전체 높이</returns>
        public float GetHeight(SerializedProperty eventProperty)
        {
            var payloadProperty = eventProperty.FindPropertyRelative(_propertyName);

            // 프로퍼티가 존재하면 해당 높이 반환, 없으면 최소 한 줄 높이 반환
            return payloadProperty != null
                ? EditorGUI.GetPropertyHeight(payloadProperty, true)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}