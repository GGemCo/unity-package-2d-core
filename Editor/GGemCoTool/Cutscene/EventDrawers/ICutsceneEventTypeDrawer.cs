using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 특정 <see cref="CutsceneEventType"/>에 대응하는 에디터 드로어의 공통 계약을 정의합니다.
    /// 각 구현체는 대상 이벤트 타입에 맞는 인스펙터 UI 그리기와 높이 계산을 담당합니다.
    /// </summary>
    internal interface ICutsceneEventTypeDrawer
    {
        /// <summary>
        /// 현재 드로어가 처리하는 컷신 이벤트 타입을 가져옵니다.
        /// </summary>
        CutsceneEventType EventType { get; }

        /// <summary>
        /// 지정한 위치에 이벤트 프로퍼티에 대한 커스텀 UI를 그립니다.
        /// </summary>
        /// <param name="position">에디터 GUI를 그릴 영역입니다.</param>
        /// <param name="eventProperty">그리기 대상이 되는 이벤트 직렬화 프로퍼티입니다.</param>
        void Draw(Rect position, SerializedProperty eventProperty);

        /// <summary>
        /// 이벤트 프로퍼티를 그리는 데 필요한 UI 높이를 계산합니다.
        /// </summary>
        /// <param name="eventProperty">높이를 계산할 대상 이벤트 직렬화 프로퍼티입니다.</param>
        /// <returns>에디터 레이아웃에서 사용할 총 높이입니다.</returns>
        float GetHeight(SerializedProperty eventProperty);
    }
}