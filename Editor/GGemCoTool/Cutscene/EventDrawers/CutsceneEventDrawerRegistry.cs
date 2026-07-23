using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷씬 이벤트 타입에 대응되는 Drawer를 관리하는 레지스트리입니다.
    /// 이벤트 타입에 따라 적절한 Inspector Drawer를 반환합니다.
    /// </summary>
    internal static class CutsceneEventDrawerRegistry
    {
        /// <summary>
        /// 이벤트 타입과 Drawer 매핑 테이블입니다.
        /// </summary>
        private static readonly Dictionary<CutsceneEventType, ICutsceneEventTypeDrawer> Map = new();

        /// <summary>
        /// 매핑되지 않은 이벤트 타입에 대해 사용되는 기본 Drawer입니다.
        /// </summary>
        private static readonly ICutsceneEventTypeDrawer FallbackDrawer = new CutsceneUnsupportedEventTypeDrawer();

        /// <summary>
        /// 정적 생성자에서 모든 Drawer를 등록합니다.
        /// </summary>
        static CutsceneEventDrawerRegistry()
        {
            // 기본 Drawer (단순 Property 위임)
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraMove, "cameraMove"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraZoom, "cameraZoom"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraShake, "cameraShake"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraChangeTarget, "cameraChangeTarget"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterMove, "characterMove"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterTweenMove, "characterTweenMove"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterAnimation, "characterAnimation"));
            Register(new CutsceneCharacterAnimationTimeScaleEventTypeDrawer());
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.DialogueBalloon, "dialogueBalloon"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.ScreenFade, "screenFade"));

            // 커스텀 Drawer
            Register(new CutsceneOverlayTextEventTypeDrawer());
            Register(new CutsceneCharacterWhiteOverlayEventTypeDrawer());
            Register(new CutsceneUiPanelEventTypeDrawer());
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.UiWindowVisibility, "uiWindowVisibility"));
            Register(new CutsceneTimeScaleEventTypeDrawer());
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.WorldObjectVisibility, "worldObjectVisibility"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterControlLock, "characterControlLock"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.ScreenGlitch, "screenGlitch"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterFade, "characterFade"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterAirborne, "characterAirborne"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterSpawn, "characterSpawn"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.DialogueWindow, "dialogueWindow"));
        }

        /// <summary>
        /// 지정된 이벤트 타입에 해당하는 Drawer를 반환합니다.
        /// 등록되지 않은 경우 Fallback Drawer를 반환합니다.
        /// </summary>
        /// <param name="eventType">조회할 컷씬 이벤트 타입입니다.</param>
        /// <returns>해당 이벤트를 렌더링할 Drawer 인스턴스</returns>
        public static ICutsceneEventTypeDrawer Get(CutsceneEventType eventType)
        {
            return Map.TryGetValue(eventType, out var drawer) ? drawer : FallbackDrawer;
        }

        /// <summary>
        /// Drawer를 레지스트리에 등록합니다.
        /// 동일한 이벤트 타입이 이미 존재할 경우 덮어씁니다.
        /// </summary>
        /// <param name="drawer">등록할 Drawer 인스턴스</param>
        private static void Register(ICutsceneEventTypeDrawer drawer)
        {
            // NOTE: 동일 EventType 중복 등록 시 마지막 등록이 우선됩니다.
            Map[drawer.EventType] = drawer;
        }
    }
}
