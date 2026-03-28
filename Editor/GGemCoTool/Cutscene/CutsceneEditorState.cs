using GGemCo2DCore;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CutsceneEditorWindow에서 유지하는 선택/표시 상태를 모아둔 컨테이너입니다.
    /// Window 본체는 레이아웃 조립에 집중하고, 실제 상태 보관은 이 클래스로 위임합니다.
    /// </summary>
    internal sealed class CutsceneEditorState
    {
        public StruckTableCutscene SelectedCutscene { get; set; }
        public Vector2 Scroll { get; set; }
        public string LastReloadMessage { get; set; } = string.Empty;
        public string LastActionMessage { get; set; } = string.Empty;
        public TextAsset SelectedJson { get; set; }
        public TimelineAsset SelectedTimelineAsset { get; set; }
    }
}
