#if UNITY_EDITOR
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>모든 설정 스텝의 공통 베이스</summary>
    public abstract class SetupStepBase
    {
        [Tooltip("이 스텝을 실행할지 여부")]
        public readonly bool enabledStep = true;

        [Tooltip("작업 순서(낮을수록 먼저 실행)")]
        public readonly int order = 0;

        [TextArea, Tooltip("스텝 설명/메모")]
        public string description;

        /// <summary>사전 검증</summary>
        public virtual bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>실행 본체</summary>
        public abstract void Execute(EditorSetupContext ctx);
        
    }
}
#endif