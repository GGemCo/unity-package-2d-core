using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 넉백: 지정 시간 동안 노크백 애니메이션을 재생하고 이동을 봉인
    /// - 기본 애니메이션 상태명: "knockback"
    /// - affectInfo.StatusSuffix가 존재하면 그 값을 상태명으로 사용(예: "Knockback_Heavy")
    /// - 이동 봉인은 Rigidbody2D.constraints를 임시 Freeze
    /// </summary>
    public sealed class AffectKnockBack : AffectBase
    {
        // Rigidbody2D 캐시
        private Rigidbody2D _rb2d;
        private RigidbodyConstraints2D _originalConstraints;
        private Vector2 _originalVelocity;

        // 선택: 상태명 캐시(적용 시 결정)
        private string _animStateName = "knockback";
        
        /// <summary>
        /// 기반 생성자 명시 호출
        /// </summary>
        /// <param name="character"></param>
        /// <param name="effectManager"></param>
        /// <param name="uid"></param>
        /// <param name="group"></param>
        /// <param name="buffs"></param>
        /// <param name="duration"></param>
        /// <param name="onCompleted"></param>
        public AffectKnockBack(
            CharacterBase character,
            EffectManager effectManager,
            int uid,
            string group,
            System.Collections.Generic.List<ConfigCommon.StruckStatus> buffs,
            float duration,
            System.Action<int> onCompleted)
            : base(character, effectManager, uid, group, buffs, duration, onCompleted)
        { }
        
        // --- 훅 구현 ---
        protected override void OnBeforeApply(StruckTableAffect info)
        {
            // 넉백 상태 적용
            character.SetStatusKnockback();
            // 애니메이션 재생
            character.CharacterAnimationController.PlayCharacterAnimation(_animStateName);
        }

        protected override void OnAfterApply(StruckTableAffect info)
        {
            // 특별 처리 없음(확장 포인트)
        }

        protected override void OnBeforeStop()
        {
            // 이동 봉인 해제(원래 제약 복구, 속도는 0 유지)
            character.Stop(true);
            if (_rb2d)
            {
                _rb2d.constraints = _originalConstraints;
                // 보통 넉백 종료 후에는 속도를 0으로 유지하는 편이 안전함
                _rb2d.linearVelocity = Vector2.zero;
            }

            // 애니메이션 종료는 상태머신 설계에 따름
            // - Trigger는 1회성이라 별도 해제 불필요
            // - CrossFade로 진입한 경우, Idle 등으로의 전이는 상위 StateMachine 전이에 맡김
        }
    }
}
