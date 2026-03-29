using GGemCo2DCore;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CutsceneEditorWindow에서 사용하는 선택 상태 및 UI 표시 상태를 보관하는 컨테이너입니다.
    /// </summary>
    /// <remarks>
    /// EditorWindow는 UI 렌더링과 사용자 입력 처리에 집중하고,
    /// 실제 상태 데이터는 이 클래스를 통해 관리합니다.
    /// 상태를 분리함으로써 테스트 용이성과 유지보수성을 향상시킵니다.
    /// </remarks>
    internal sealed class CutsceneEditorState
    {
        /// <summary>
        /// 현재 선택된 컷신 데이터입니다.
        /// </summary>
        /// <remarks>
        /// 리스트에서 선택된 항목을 기준으로 Timeline 생성/수정 등의 작업에 사용됩니다.
        /// </remarks>
        public StruckTableCutscene SelectedCutscene { get; set; }

        /// <summary>
        /// 컷신 목록 또는 UI 스크롤 영역의 현재 스크롤 위치입니다.
        /// </summary>
        public Vector2 Scroll { get; set; }

        /// <summary>
        /// 마지막 리로드 작업 결과 메시지입니다.
        /// </summary>
        /// <remarks>
        /// 데이터 재로드 또는 갱신 이후 사용자에게 표시됩니다.
        /// </remarks>
        public string LastReloadMessage { get; set; } = string.Empty;

        /// <summary>
        /// 마지막 사용자 액션(Import/Export 등)의 결과 메시지입니다.
        /// </summary>
        /// <remarks>
        /// 작업 성공/실패 여부를 UI에 피드백하기 위해 사용됩니다.
        /// </remarks>
        public string LastActionMessage { get; set; } = string.Empty;

        /// <summary>
        /// 현재 선택된 JSON 에셋입니다.
        /// </summary>
        /// <remarks>
        /// Timeline 생성(Import) 시 입력 데이터로 사용됩니다.
        /// </remarks>
        public TextAsset SelectedJson { get; set; }

        /// <summary>
        /// 현재 선택된 Timeline 에셋입니다.
        /// </summary>
        /// <remarks>
        /// JSON으로 Export하거나 편집 대상 Timeline으로 사용됩니다.
        /// </remarks>
        public TimelineAsset SelectedTimelineAsset { get; set; }
    }
}