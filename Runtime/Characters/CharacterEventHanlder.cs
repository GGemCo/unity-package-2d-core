using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공격 애니메이션 끝났을 때
    /// </summary>
    public sealed class EventArgsAnimationAttack : EventArgs
    {
        // 외부에서 처리했으면 true
        public bool Handled { get; set; }
    }

    public delegate void EventHandlerAnimationCompleteAttack(CharacterBase sender, EventArgsAnimationAttack e);
    
    /// <summary>
    /// 공격 End 애니메이션 끝났을 때
    /// </summary>
    public sealed class EventArgsAnimationAttackEnd : EventArgs
    {
        public bool Handled { get; set; }
    }

    public delegate void EventHandlerAnimationCompleteAttackEnd(CharacterBase sender, EventArgsAnimationAttackEnd e);

    /// <summary>
    /// Stop() 처리 후 
    /// </summary>
    public sealed class EventArgsOnStop : EventArgs
    {
        public bool Handled { get; set; }
    }

    public delegate void EventHandlerOnStop(CharacterBase sender, EventArgsOnStop e);
    
    /// <summary>
    /// Jump 관련 애니메이션 이벤트 발생시 
    /// </summary>
    public sealed class EventArgsOnAnimationEventJump : EventArgs
    {
        public bool Handled { get; set; }
        public string EventName { get; set; }
    }

    public delegate void EventHandlerOnAnimationEventJump(CharacterBase sender, EventArgsOnAnimationEventJump e);
    /// <summary>
    /// 대시 관련 애니메이션 이벤트 발생시 
    /// </summary>
    public sealed class EventArgsOnAnimationEventDash : EventArgs
    {
        public bool Handled { get; set; }
        public string EventName { get; set; }
    }

    public delegate void EventHandlerOnAnimationEventDash(CharacterBase sender, EventArgsOnAnimationEventDash e);
}