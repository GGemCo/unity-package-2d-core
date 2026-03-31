namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 사용되는 이벤트 타입을 정의합니다.
    /// JSON 데이터에서 정수(int) 값으로 직렬화되므로, 기존 값의 변경은 호환성 문제를 유발할 수 있습니다.
    /// 새로운 타입은 기존 값 뒤에 추가해야 합니다.
    /// </summary>
    public enum CutsceneEventType
    {
        /// <summary>
        /// 카메라를 지정된 위치로 이동시킵니다.
        /// </summary>
        CameraMove = 0,

        /// <summary>
        /// 카메라 줌 인/아웃을 제어합니다.
        /// </summary>
        CameraZoom = 1,

        /// <summary>
        /// 카메라 흔들림 효과를 적용합니다.
        /// </summary>
        CameraShake = 2,

        /// <summary>
        /// 카메라의 추적 대상을 변경합니다.
        /// </summary>
        CameraChangeTarget = 3,

        /// <summary>
        /// 캐릭터를 지정된 위치로 이동시킵니다.
        /// </summary>
        CharacterMove = 4,

        /// <summary>
        /// 대사 말풍선을 표시합니다.
        /// </summary>
        DialogueBalloon = 5,

        /// <summary>
        /// 캐릭터 애니메이션을 재생합니다.
        /// </summary>
        CharacterAnimation = 6,

        /// <summary>
        /// 캐릭터 애니메이션의 재생 속도를 조정합니다.
        /// </summary>
        CharacterAnimationTimeScale = 7,

        /// <summary>
        /// 화면 페이드 인/아웃 효과를 적용합니다.
        /// </summary>
        ScreenFade = 8,

        /// <summary>
        /// 화면에 오버레이 텍스트를 표시합니다.
        /// </summary>
        OverlayText = 9,

        /// <summary>
        /// 캐릭터에 흰색 오버레이 효과를 적용합니다.
        /// </summary>
        CharacterWhiteOverlay = 10,

        /// <summary>
        /// UI 패널의 표시 상태를 제어합니다.
        /// </summary>
        UiPanel = 11,

        /// <summary>
        /// UI 윈도우의 표시 상태를 제어합니다.
        /// </summary>
        UiWindowVisibility = 12,

        /// <summary>
        /// 게임 전체의 시간 배율(Time Scale)을 변경합니다.
        /// </summary>
        TimeScale = 13
    }
    
    public enum CutsceneKeyTextOverlay
    {
        None,
        MonsterName
    }
}