namespace GGemCo2DCore
{
    public class StruckAnimationEventComplete
    {
        public bool ForceStop { get; set; } = false;
    }
    
    /// <summary>
    /// AnimationEvent VFX 생성 위치 기준입니다.
    /// </summary>
    public enum AnimationEventVfxPositionBasis
    {
        /// <summary>
        /// 이벤트를 발생시킨 오브젝트의 월드 위치를 기준으로 VFX를 생성합니다.
        /// 기존 AnimationEvent VFX와 가장 가까운 기본값입니다.
        /// </summary>
        EventObject = 0,

        /// <summary>
        /// 이벤트를 발생시킨 캐릭터의 머리 높이를 기준으로 VFX를 생성합니다.
        /// 캐릭터를 찾을 수 없으면 EventObject와 동일하게 동작합니다.
        /// </summary>
        EventCharacterHead = 1,

        /// <summary>
        /// 이벤트를 발생시킨 캐릭터의 HitArea 내부에서 임의 월드 위치를 선택해 VFX를 생성합니다.
        /// 캐릭터 또는 HitArea를 찾을 수 없으면 EventObject와 동일하게 동작합니다.
        /// </summary>
        CharacterHitArea = 2,
    }

    /// <summary>
    /// AnimationEvent VFX가 이벤트 발생 캐릭터의 Flip 상태를 반영하는 방식입니다.
    /// </summary>
    public enum AnimationEventVfxFlipPolicy
    {
        /// <summary>
        /// 캐릭터 Flip 상태를 VFX에 반영하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// VFX 생성 시점에만 캐릭터 Flip 상태를 반영합니다.
        /// 기존 AnimationEvent VFX와 가장 가까운 기본값입니다.
        /// </summary>
        EventCharacterOnSpawn = 1,

        /// <summary>
        /// VFX가 이벤트 발생 캐릭터의 위치와 Flip 상태를 계속 따라갑니다.
        /// 지속형 또는 부착형 VFX에 사용하는 것을 권장합니다.
        /// </summary>
        EventCharacterFollow = 2,
    }

    /// <summary>
    /// AnimationEvent에서 VFX를 생성할 때 사용하는 JSON 데이터입니다.
    /// </summary>
    public class StruckAnimationEventVfx
    {
        /// <summary>생성할 VFX 대표 Uid입니다.</summary>
        public int Uid { get; set; }

        /// <summary>VFX 스케일 오버라이드입니다. 0보다 클 때만 적용됩니다.</summary>
        public float Scale { get; set; } = 1.0f;

        /// <summary>VFX 지속 시간 오버라이드입니다. 0이면 VFX 데이터 기본 정책을 사용합니다.</summary>
        public float Duration { get; set; } = 0;

        /// <summary>VFX 색상 오버라이드입니다. RRGGBB 또는 RRGGBBAA 형식을 사용합니다.</summary>
        public string Color { get; set; } = "FFFFFF";

        /// <summary>VFX를 생성할 위치 기준입니다.</summary>
        public AnimationEventVfxPositionBasis PositionBasis { get; set; } = AnimationEventVfxPositionBasis.EventObject;

        /// <summary>기준 위치에 더할 X축 월드 오프셋입니다.</summary>
        public float OffsetX { get; set; } = 0f;

        /// <summary>기준 위치에 더할 Y축 월드 오프셋입니다.</summary>
        public float OffsetY { get; set; } = 0f;

        /// <summary>기준 위치에 더할 Z축 월드 오프셋입니다.</summary>
        public float OffsetZ { get; set; } = 0f;

        /// <summary>
        /// true이면 이벤트 발생 캐릭터가 좌우 반전된 상태일 때 OffsetX도 반전합니다.
        /// 캐릭터 기준 오른쪽/왼쪽 배치가 필요한 VFX에서 사용합니다.
        /// </summary>
        public bool MirrorOffsetXByCharacterFlip { get; set; } = true;

        /// <summary>이벤트 발생 캐릭터의 Flip 상태를 VFX에 반영하는 방식입니다.</summary>
        public AnimationEventVfxFlipPolicy FlipPolicy { get; set; } = AnimationEventVfxFlipPolicy.EventCharacterOnSpawn;
    }
    public class StruckAnimationEventCameraShake
    {
        public float Duration { get; set; }
        public float Magnitude { get; set; }
        public float LeftStrength { get; set; } = 0f;
        public float RightStrength { get; set; } = 0f;
        public float DownStrength { get; set; } = 0f;
        public float UpStrength { get; set; } = 0f;
        public int RepeatCount { get; set; } = 3;
        public bool UseUnscaledTime { get; set; } = false;

        public float GetLeftStrength()
        {
            return LeftStrength > 0f ? LeftStrength : Magnitude;
        }

        public float GetRightStrength()
        {
            return RightStrength > 0f ? RightStrength : Magnitude;
        }

        public float GetDownStrength()
        {
            return DownStrength > 0f ? DownStrength : Magnitude;
        }

        public float GetUpStrength()
        {
            return UpStrength > 0f ? UpStrength : Magnitude;
        }

        public int GetRepeatCount()
        {
            return RepeatCount > 0 ? RepeatCount : 3;
        }
    }
    public class StruckAnimationEventSkill
    {
        public int Uid { get; set; }
        public int Level { get; set; }
    }
    public class StruckAnimationEventSound
    {
        public int Uid { get; set; }
    }
    public class StruckAnimationEventAttack
    {
        // 공격 받는 대상에게 affect 걸기
        public int TargetAffectUid { get; set; } = 0;
        public int TargetCrowdControlUid { get; set; } = 0;
    }
    public class StruckAnimationEventUseTool
    {
        public int Uid { get; set; }
    }

    /// <summary>
    /// 백스탭/대시/회피 트레일(AnimationEvent) 설정.
    /// - AnimationEvent의 string 파라미터에 아래 JSON을 넣어 런타임 오버라이드를 적용할 수 있습니다.
    /// 예) {"DurationSeconds":0.25,"SpawnIntervalSeconds":0.03,"GhostLifetimeSeconds":0.25,"ColorHex":"66A9FF","SortingOrderOffset":-1}
    /// </summary>
    public sealed class StruckAnimationEventBackstepTrail
    {
        /// <summary>트레일을 자동으로 중지할 시간(0이면 자동 중지 없음).</summary>
        public float DurationSeconds { get; set; } = 0f;

        /// <summary>잔상 생성 주기(0이면 컴포넌트 기본값 사용).</summary>
        public float SpawnIntervalSeconds { get; set; } = 0f;

        /// <summary>잔상 수명(0이면 컴포넌트 기본값 사용).</summary>
        public float GhostLifetimeSeconds { get; set; } = 0f;

        /// <summary>
        /// 잔상 색상(Hex). "RRGGBB" 또는 "#RRGGBB", "RRGGBBAA" 형식 지원.
        /// 비어있으면 컴포넌트 기본값 사용.
        /// </summary>
        public string ColorHex { get; set; } = null;

        /// <summary>SortingOrder 오프셋(값이 지정되면 오버라이드).</summary>
        public int? SortingOrderOffset { get; set; } = null;
    }

    public enum AnimationMotionEventAction
    {
        Trigger = 0,
        Start = 1,
        Cancel = 2,
    }

    public sealed class StruckAnimationEventCrowdControl
    {
        public int CrowdControlUid { get; set; } = 0;
        public bool UseSelfAsSource { get; set; } = true;
    }

    public sealed class StruckAnimationEventMotion
    {
        public AnimationMotionEventAction Action { get; set; } = AnimationMotionEventAction.Trigger;
        public MotionChannel Channel { get; set; } = MotionChannel.Skill;
        public MotionKind Kind { get; set; } = MotionKind.Linear;
        public float Distance { get; set; } = 0f;
        public float Duration { get; set; } = 0f;
        public float Height { get; set; } = 0f;
        public bool UseFacingDirection { get; set; } = true;
        public float DirectionX { get; set; } = 0f;
        public float DirectionY { get; set; } = 0f;
        public bool StopAtEnd { get; set; } = true;
        public bool UseMovePosition { get; set; } = true;
        public bool AllowReplace { get; set; } = true;
        public float HoldSecondsAfter { get; set; } = 0f;
        public Easing.EaseType EaseType { get; set; } = Easing.EaseType.Linear;
        public MotionArcMode ArcMode { get; set; } = MotionArcMode.LegacyTimeSine;
        public Easing.EaseType RiseEaseType { get; set; } = Easing.EaseType.Linear;
        public Easing.EaseType FallEaseType { get; set; } = Easing.EaseType.Linear;
        public float ApexHoldNormalized { get; set; } = 0f;
        public float RiseRatioNormalized { get; set; } = 0.5f;
        public float FallRatioNormalized { get; set; } = 0.5f;
    }
    
    
    /// <summary>
    /// 공격/연출 프레임에서 단발 잔상을 캡처하는 AnimationEvent 설정.
    /// 예) {"GhostLifetimeSeconds":2.0,"ColorHex":"4AA3FF","Alpha":0.45,"SortingOrderOffset":-1}
    /// </summary>
    public sealed class StruckAnimationEventAfterimageSnapshot
    {
        /// <summary>잔상 수명(0이면 컴포넌트 기본값 사용).</summary>
        public float GhostLifetimeSeconds { get; set; } = 0f;

        /// <summary>잔상 색상 HTML Hex. 예) "4AA3FF" 또는 "#4AA3FF"</summary>
        public string ColorHex { get; set; } = string.Empty;

        /// <summary>잔상 알파(0~1). 음수면 색상에 포함된 알파 또는 컴포넌트 기본값 사용.</summary>
        public float Alpha { get; set; } = -1f;

        /// <summary>원본 SpriteRenderer 대비 sortingOrder 보정값.</summary>
        public int? SortingOrderOffset { get; set; } = null;
    }

    public static class AnimationConstants
    {
        private const string Prefix = "GGemCoAniEvent";
        public const string EventNameAttack = Prefix+"Attack";
        public const string EventNameSound = Prefix+"Sound";
        public const string EventNameCameraShake = Prefix+"CameraShake";
        public const string EventNameVfx = Prefix+"Vfx";
        public const string EventNamePlayerHit = Prefix+"PlayerHit";
        public const string EventNameSkill = Prefix+"Skill";
        
        public const string EventNameJumpUp = Prefix+"JumpUp";
        public const string EventNameJumpFall = Prefix+"JumpFall";
        public const string EventNameJumpEnd = Prefix+"JumpEnd";
        
        public const string EventNameDashPlay = Prefix+"DashPlay";
        public const string EventNameDashEnd = Prefix+"DashEnd";
        
        public const string EventNameMotion = Prefix+"Motion";
        public const string EventNameCrowdControl = Prefix+"CrowdControl";

        public const string EventNameUseTool = Prefix+"UseTool";

        public const string EventNameStartBackstepTrail = Prefix+"StartBackstepTrail";
        public const string EventNameStopBackstepTrail = Prefix+"StopBackstepTrail";
        public const string EventNameCaptureAfterimageSnapshot = Prefix+"CaptureAfterimageSnapshot";
    }
}
