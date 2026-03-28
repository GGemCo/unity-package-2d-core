
namespace GGemCo2DCore
{
    /// <summary>
    /// 연출 - Const 값
    /// json 에 type 값이 int로 저장되기 때문에, 값을 변경하면 안됨
    /// </summary>
    public enum CutsceneEventType
    {
        CameraMove = 0,
        CameraZoom = 1,
        CameraShake = 2,
        CameraChangeTarget = 3,
        CharacterMove = 4,
        DialogueBalloon = 5,
        CharacterAnimation = 6,
        CharacterAnimationTimeScale = 7,
        ScreenFade = 8,
        OverlayText = 9,
        CharacterWhiteOverlay = 10,
        UiPanel = 11,
        UiWindowVisibility = 12,
        TimeScale = 13
    }
}