using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneEventDrawerRegistry
    {
        private static readonly Dictionary<CutsceneEventType, ICutsceneEventTypeDrawer> Map = new();
        private static readonly ICutsceneEventTypeDrawer FallbackDrawer = new CutsceneUnsupportedEventTypeDrawer();

        static CutsceneEventDrawerRegistry()
        {
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraMove, "cameraMove"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraZoom, "cameraZoom"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraShake, "cameraShake"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CameraChangeTarget, "cameraChangeTarget"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterMove, "characterMove"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterAnimation, "characterAnimation"));
            Register(new CutsceneCharacterAnimationTimeScaleEventTypeDrawer());
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.DialogueBalloon, "dialogueBalloon"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.ScreenFade, "screenFade"));
            Register(new CutsceneOverlayTextEventTypeDrawer());
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.CharacterWhiteOverlay, "characterWhiteOverlay"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.UiPanel, "uiPanel"));
            Register(new CutsceneDefaultEventTypeDrawer(CutsceneEventType.UiWindowVisibility, "uiWindowVisibility"));
            Register(new CutsceneTimeScaleEventTypeDrawer());
        }

        public static ICutsceneEventTypeDrawer Get(CutsceneEventType eventType)
        {
            return Map.TryGetValue(eventType, out var drawer) ? drawer : FallbackDrawer;
        }

        private static void Register(ICutsceneEventTypeDrawer drawer)
        {
            Map[drawer.EventType] = drawer;
        }
    }
}
