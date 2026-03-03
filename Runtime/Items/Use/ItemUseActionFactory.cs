namespace GGemCo2DCore
{
    /// <summary>
    /// item_use_action Row → 런타임 Action 인스턴스 변환
    /// - ActionType 추가 시: Action 클래스 추가 + 여기 분기 추가
    /// </summary>
    public static class ItemUseActionFactory
    {
        public static IItemUseAction Create(StruckTableItemUseAction row)
        {
            if (row == null) return null;

            switch (row.ActionType)
            {
                case ItemUseActionType.AddExp:
                    // ParamIntA = exp (정수) (향후 long 필요 시 ParamStringA로 확장)
                    return new ItemUseActionAddExp(row.ParamIntA);

                case ItemUseActionType.AddStatPoints:
                    // ParamIntA = points
                    return new ItemUseActionAddStatPoints(row.ParamIntA);

                case ItemUseActionType.GrantSkill:
                    // ParamIntA = skillUid, ParamIntB = level
                    // ParamStringA/B = 옵션 문자열 (예: "dup=LevelUp;altKind=Exp;altValue=100")
                    return new ItemUseActionGrantSkill(row.ParamIntA, row.ParamIntB, row.ParamStringA, row.ParamStringB);
                
                case ItemUseActionType.AddHp:
                    // ParamIntA = amount
                    return new ItemUseActionAddHp(row.ParamIntA);

                case ItemUseActionType.AddMp:
                    // ParamIntA = amount
                    return new ItemUseActionAddMp(row.ParamIntA);

                case ItemUseActionType.AddItemBonusHp:
                    // ParamIntA = amount
                    return new ItemUseActionAddItemBonusHp(row.ParamIntA);

                case ItemUseActionType.ApplyAffect:
                    // ParamIntA = affectUid, ParamFloatA = durationOverrideSeconds(<=0이면 기본)
                    return new ItemUseActionApplyAffect(row.ParamIntA, row.ParamFloatA);

                case ItemUseActionType.None:
                default:
                    return null;
            }
        }
    }
}
