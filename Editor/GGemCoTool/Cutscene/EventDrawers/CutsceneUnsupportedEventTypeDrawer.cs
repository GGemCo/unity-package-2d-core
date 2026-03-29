using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 지원되지 않는 <see cref="CutsceneEventType"/>에 대해 경고 UI를 표시하는 Fallback Drawer입니다.
    /// 레지스트리에 등록되지 않은 이벤트 타입이 들어온 경우 사용자에게 편집 불가 상태를 알립니다.
    /// </summary>
    internal sealed class CutsceneUnsupportedEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        /// <summary>
        /// 실제 이벤트와 매핑되지 않는 Fallback 식별값입니다.
        /// 정상 등록 대상이 아닌 Drawer임을 나타내기 위해 음수 값을 사용합니다.
        /// </summary>
        public CutsceneEventType EventType => (CutsceneEventType)(-1);

        /// <summary>
        /// 지원되지 않는 이벤트 타입이라는 경고 메시지를 Inspector에 표시합니다.
        /// </summary>
        /// <param name="position">UI를 그릴 영역(Rect)</param>
        /// <param name="eventProperty">현재 편집 중인 컷씬 이벤트 프로퍼티</param>
        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            EditorGUI.HelpBox(position, "지원되지 않는 CutsceneEventType 입니다.", MessageType.Warning);
        }

        /// <summary>
        /// 경고 HelpBox를 표시하는 데 필요한 높이를 계산합니다.
        /// </summary>
        /// <param name="eventProperty">현재 편집 중인 컷씬 이벤트 프로퍼티</param>
        /// <returns>HelpBox 렌더링에 필요한 높이</returns>
        public float GetHeight(SerializedProperty eventProperty)
        {
            // NOTE: 80f는 Inspector 내부 여백을 고려한 보정값입니다.
            return EditorStyles.helpBox.CalcHeight(
                new GUIContent("지원되지 않는 CutsceneEventType 입니다."),
                EditorGUIUtility.currentViewWidth - 80f);
        }
    }
}