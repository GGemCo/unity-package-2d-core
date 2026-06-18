namespace GGemCo2DCore
{
    /// <summary>
    /// 확정 타격 메타데이터를 기준으로 공격자와 피격 대상의 HitStop을 적용합니다.
    /// </summary>
    internal static class AttackHitStopProcessor
    {
        /// <summary>
        /// 실제 데미지가 확정된 공격의 HitStop 설정을 양쪽 캐릭터에 반영합니다.
        /// </summary>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="metadataDamage">공격자와 HitStop 설정을 포함한 데미지 메타데이터입니다.</param>
        public static void Apply(CharacterBase target, MetadataDamage metadataDamage)
        {
            if (metadataDamage == null || !metadataDamage.HasAttackHitStopSettings)
                return;

            AttackHitStopSettings settings = metadataDamage.AttackHitStopSettings;
            if (!settings.HasAnyHitStop)
                return;

            CharacterBase attacker = metadataDamage.attacker != null
                ? metadataDamage.attacker.GetComponent<CharacterBase>()
                : null;
            if (attacker == null)
                return;

            CharacterBase.HitStopConfig config = attacker.GetResolvedHitStopConfig();
            if (!config.Enabled)
                return;

            int sourceSkillUid = metadataDamage.SkillUid;
            if (settings.useHitStopSelf)
            {
                float seconds = settings.ResolveSelfSeconds(config);
                if (seconds > 0f)
                {
                    attacker.ApplyHitStop(new HitStopRequest(
                        seconds,
                        pauseAnimation: config.PauseAnimation,
                        freezePhysics: config.FreezePhysics,
                        sourceSkillUid: sourceSkillUid));
                }
            }

            if (settings.useHitStopTarget && target != null)
            {
                float seconds = settings.ResolveTargetSeconds(config);
                if (seconds > 0f)
                {
                    target.ApplyHitStop(new HitStopRequest(
                        seconds,
                        pauseAnimation: config.PauseAnimation,
                        freezePhysics: config.FreezePhysics,
                        sourceSkillUid: sourceSkillUid));
                }
            }
        }
    }
}
