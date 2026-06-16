using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 전투 피드백을 MP 획득 보상으로 변환하고 실제 MP 증가를 실행하는 Core 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// 이 컴포넌트는 보상 수치를 직접 결정하지 않습니다. 같은 오브젝트의 <see cref="IMpGainRuleProvider"/>가
    /// 게임별 보상 규칙을 반환하고, 이 컨트롤러는 중복 지급 방지, 보정 Provider 적용, MP 증가 실행만 담당합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterMpGainController : MonoBehaviour,
        IOutgoingAttackHitFeedbackSink,
        IPlayerGuardSuccessFeedbackSink
    {
        private readonly List<RewardHistory> _rewardHistories = new(8);
        private IMpGainRuleProvider[] _ruleProviders;
        private IMpGainBonusProvider[] _bonusProviders;
        private ICharacterMpGainReceiver _mpGainReceiver;
        private CharacterStat _characterStat;

        /// <summary>
        /// 필요한 런타임 참조를 캐시합니다.
        /// </summary>
        private void Awake()
        {
            CacheRuntimeReferences();
        }

        /// <summary>
        /// 활성화 시점에 뒤늦게 부착된 Provider를 다시 확인합니다.
        /// </summary>
        private void OnEnable()
        {
            CacheRuntimeReferences();
        }

        /// <summary>
        /// 같은 오브젝트에 부착된 MP 보상 Provider와 지급 Receiver를 다시 캐시합니다.
        /// </summary>
        public void RefreshRuntimeReferences()
        {
            CacheRuntimeReferences();
        }

        /// <summary>
        /// 공격자가 실제 타격 결과를 확정받았을 때 MP 보상 규칙을 평가합니다.
        /// </summary>
        /// <param name="feedback">공격자에게 전달된 타격 확정 피드백입니다.</param>
        public void NotifyOutgoingAttackHitResolved(in OutgoingAttackHitFeedback feedback)
        {
            if (feedback.Attacker != gameObject)
            {
                return;
            }

            var context = new MpGainContext(
                feedback.Attacker,
                feedback.Target,
                feedback.MetadataDamage,
                MpGainTrigger.OutgoingAttackHit,
                feedback.Outcome,
                default,
                feedback.Time);

            TryApplyMpGain(in context);
        }

        /// <summary>
        /// 플레이어가 가드 성공 결과를 확정받았을 때 MP 보상 규칙을 평가합니다.
        /// </summary>
        /// <param name="feedback">플레이어 가드 성공 피드백입니다.</param>
        public void NotifyPlayerGuardSuccess(in PlayerGuardSuccessFeedback feedback)
        {
            if (feedback.Defender != gameObject)
            {
                return;
            }

            var context = new MpGainContext(
                feedback.Defender,
                feedback.Attacker,
                feedback.MetadataDamage,
                MpGainTrigger.PlayerGuardSuccess,
                default,
                feedback.Outcome,
                feedback.Time);

            TryApplyMpGain(in context);
        }

        /// <summary>
        /// 런타임 의존성을 현재 오브젝트 기준으로 캐시합니다.
        /// </summary>
        private void CacheRuntimeReferences()
        {
            _ruleProviders = GetComponents<IMpGainRuleProvider>();
            _bonusProviders = GetComponents<IMpGainBonusProvider>();
            _mpGainReceiver = GetComponent<ICharacterMpGainReceiver>();

            if (!_characterStat)
            {
                _characterStat = GetComponent<CharacterStat>();
            }
        }

        /// <summary>
        /// 등록된 규칙 Provider에서 MP 보상을 받아 실제 지급까지 처리합니다.
        /// </summary>
        /// <param name="context">MP 획득 판정 컨텍스트입니다.</param>
        private void TryApplyMpGain(in MpGainContext context)
        {
            CacheRuntimeReferences();
            if (_ruleProviders == null || _ruleProviders.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _ruleProviders.Length; i++)
            {
                IMpGainRuleProvider provider = _ruleProviders[i];
                if (provider == null ||
                    !provider.TryGetMpGainReward(in context, out MpGainReward reward) ||
                    !reward.IsValid)
                {
                    continue;
                }

                TryApplyReward(in context, reward);
                return;
            }
        }

        /// <summary>
        /// 단일 MP 보상에 중복 지급 정책과 획득량 보정을 적용한 뒤 MP를 지급합니다.
        /// </summary>
        /// <param name="context">MP 획득 판정 컨텍스트입니다.</param>
        /// <param name="reward">지급할 MP 보상 정보입니다.</param>
        private void TryApplyReward(in MpGainContext context, MpGainReward reward)
        {
            if (ShouldSkipRewardForSameAttack(context.MetadataDamage, reward))
            {
                return;
            }

            int resolvedAmount = ResolveMpGainAmount(reward.Amount);
            if (resolvedAmount <= 0)
            {
                return;
            }

            if (TryAddMp(resolvedAmount))
            {
                MarkRewarded(context.MetadataDamage, reward.Kind);
            }
        }

        /// <summary>
        /// 같은 공격 판정에서 이미 같은 종류의 보상을 지급했는지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">타격 또는 가드 판정 메타데이터입니다.</param>
        /// <param name="reward">검사할 보상 정보입니다.</param>
        /// <returns>이번 보상을 건너뛰어야 하면 <see langword="true"/>입니다.</returns>
        private bool ShouldSkipRewardForSameAttack(MetadataDamage metadataDamage, MpGainReward reward)
        {
            if (reward.AllowMultipleRewardsPerAttack)
            {
                return false;
            }

            if (!TryGetRewardHistory(reward.Kind, out RewardHistory history))
            {
                return false;
            }

            int attackId = metadataDamage != null ? metadataDamage.AttackId : 0;
            if (attackId > 0)
            {
                return history.LastAttackId == attackId;
            }

            return history.LastFrame == Time.frameCount;
        }

        /// <summary>
        /// 현재 공격 판정에서 지정한 보상 종류가 지급되었음을 기록합니다.
        /// </summary>
        /// <param name="metadataDamage">타격 또는 가드 판정 메타데이터입니다.</param>
        /// <param name="kind">지급 완료한 보상 종류입니다.</param>
        private void MarkRewarded(MetadataDamage metadataDamage, MpGainRewardKind kind)
        {
            int attackId = metadataDamage != null ? metadataDamage.AttackId : 0;
            int index = FindRewardHistoryIndex(kind);
            RewardHistory history = index >= 0 ? _rewardHistories[index] : new RewardHistory(kind);

            if (attackId > 0)
            {
                history.LastAttackId = attackId;
            }

            history.LastFrame = Time.frameCount;

            if (index >= 0)
            {
                _rewardHistories[index] = history;
            }
            else
            {
                _rewardHistories.Add(history);
            }
        }

        /// <summary>
        /// 지정한 보상 종류의 지급 이력 인덱스를 찾습니다.
        /// </summary>
        /// <param name="kind">조회할 보상 종류입니다.</param>
        /// <returns>지급 이력 인덱스입니다. 없으면 -1입니다.</returns>
        private int FindRewardHistoryIndex(MpGainRewardKind kind)
        {
            for (int i = 0; i < _rewardHistories.Count; i++)
            {
                if (_rewardHistories[i].Kind == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 지정한 보상 종류의 지급 이력을 조회합니다.
        /// </summary>
        /// <param name="kind">조회할 보상 종류입니다.</param>
        /// <param name="history">조회된 지급 이력입니다.</param>
        /// <returns>지급 이력을 찾으면 <see langword="true"/>입니다.</returns>
        private bool TryGetRewardHistory(MpGainRewardKind kind, out RewardHistory history)
        {
            int index = FindRewardHistoryIndex(kind);
            if (index < 0)
            {
                history = default;
                return false;
            }

            history = _rewardHistories[index];
            return true;
        }

        /// <summary>
        /// MP 획득량 보정 Provider를 순서대로 적용합니다.
        /// </summary>
        /// <param name="amount">보정 전 기본 MP 획득량입니다.</param>
        /// <returns>보정 후 실제 지급할 MP 획득량입니다.</returns>
        private int ResolveMpGainAmount(int amount)
        {
            int resolvedAmount = Mathf.Max(0, amount);
            if (resolvedAmount <= 0 || _bonusProviders == null)
            {
                return resolvedAmount;
            }

            for (int i = 0; i < _bonusProviders.Length; i++)
            {
                IMpGainBonusProvider provider = _bonusProviders[i];
                if (provider == null)
                {
                    continue;
                }

                resolvedAmount = Mathf.Max(0, provider.EvaluateBonusMp(resolvedAmount));
                if (resolvedAmount <= 0)
                {
                    return 0;
                }
            }

            return resolvedAmount;
        }

        /// <summary>
        /// 게임별 MP 수신 포트 또는 Core 기본 CharacterStat을 통해 MP를 증가시킵니다.
        /// </summary>
        /// <param name="amount">증가시킬 MP 양입니다.</param>
        /// <returns>실제로 MP가 변경되었으면 <see langword="true"/>입니다.</returns>
        private bool TryAddMp(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (_mpGainReceiver != null && _mpGainReceiver.TryAddMp(amount))
            {
                return true;
            }

            if (_characterStat == null)
            {
                return false;
            }

            long before = _characterStat.CurrentMp.Value;
            _characterStat.AddMp(amount);
            return _characterStat.CurrentMp.Value != before;
        }

        private struct RewardHistory
        {
            public readonly MpGainRewardKind Kind;
            public int LastAttackId;
            public int LastFrame;

            public RewardHistory(MpGainRewardKind kind)
            {
                Kind = kind;
                LastAttackId = int.MinValue;
                LastFrame = -1;
            }
        }
    }
}
