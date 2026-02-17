
namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 적용(ActionType.ApplyAffect)
    /// - Core는 Affect 패키지를 직접 참조하지 않기 위해 Reflection 브리지를 사용합니다.
    /// - Affect 미설치 시 실패 처리(설정 누락을 빠르게 발견하기 위함)
    /// </summary>
    public sealed class ItemUseActionApplyAffect : IItemUseAction
    {
        private readonly int _affectUid;
        private readonly float _durationOverrideSeconds;

        public ItemUseActionApplyAffect(int affectUid, float durationOverrideSeconds)
        {
            _affectUid = affectUid;
            _durationOverrideSeconds = durationOverrideSeconds;
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx == null) return ResultCommon.Fail("ItemUse_InvalidContext");
            if (_affectUid <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            // Affect 패키지 미설치 여부 체크
            if (!AffectRuntimeBridge.HasAffectRuntime())
                return ResultCommon.Fail("ItemUse_NoAffectPackage");

            // TargetObject가 없으면 Player로 자동 대체되지만, 둘 다 없으면 실패
            if (ctx.TargetObject == null && ctx.Player == null)
                return ResultCommon.Fail("ItemUse_NoTarget");

            return ResultCommon.SuccessWithIcons(null);
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            var target = ctx.TargetObject != null ? ctx.TargetObject : (ctx.Player != null ? ctx.Player.gameObject : null);
            if (target == null) return ResultCommon.Fail("ItemUse_NoTarget");

            // Source는 Player(자기 자신)로 둔다.
            var source = ctx.Player != null ? ctx.Player.gameObject : null;
            AffectRuntimeBridge.ApplyAffect(target, _affectUid, source, _durationOverrideSeconds);
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
