using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class MetadataDamage
    {
        public long damage;
        public GameObject attacker;
        public ConfigCommon.DamageType damageType;

        /// <summary>
        /// 전체 데미지를 구성하는 속성별 데미지 분해 결과입니다.
        /// </summary>
        /// <remarks>
        /// 기존 호환 필드인 <see cref="damage"/>는 최종 전체 데미지 합계를 유지하고,
        /// 속성 게이지와 속성별 후처리는 이 분해 결과의 파트를 기준으로 처리합니다.
        /// </remarks>
        public DamageCalculationBreakdown DamageBreakdown;

        /// <summary>
        /// 공격자의 기본 속성 데미지 스탯을 별도 데미지 파트로 포함할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// 일반 공격처럼 물리 기본 피해에 화염/냉기/번개/독 추가 피해가 얹히는 공격에서 사용합니다.
        /// DOT, Affect 직접 피해, 이미 속성 파트가 명시된 스킬은 중복 적용을 피하기 위해 기본값 false를 유지합니다.
        /// </remarks>
        public bool IncludeAttackerElementDamageParts;

        /// <summary>
        /// 이번 데미지가 지속 피해(Damage over Time)인지 여부입니다.
        /// </summary>
        /// <remarks>
        /// 지속 피해는 실시간 공격 입력으로 발생한 즉시 타격이 아니라 상태 효과의 Tick 결과이므로,
        /// 가드/저스트 가드 판정 대상에서 제외하기 위해 사용합니다.
        /// </remarks>
        public bool IsDamageOverTime;
        // 데미지 받는 대상에 적용되는 어펙트 uid 
        public int affectUid;
        // 데미지 받는 대상에 적용되는 CC uid
        public int crowdControlUid;

        // ---- Hit Reaction (optional) ----
        /// <summary>
        /// 특정 공격이 “경직 무시 스택”을 얼마나 줄이는지(0이면 줄이지 않음)
        /// </summary>
        public int StaggerStackDamage;

        /// <summary>
        /// 스택이 0이 되었을 때 발생시키고 싶은 리액션 타입
        /// </summary>
        public CharacterConstants.HitReactionType HitReactionType = CharacterConstants.HitReactionType.None;

        /// <summary>
        /// 스택과 무관하게 강제로 리액션을 발생시키는지(선택)
        /// </summary>
        public bool ForceHitReaction;

        /// <summary>
        /// 다단 히트(연타) 구분용 공격 ID(선택)
        /// </summary>
        public int AttackId;

        /// <summary>
        /// 현재 데미지가 어떤 스킬에서 발생했는지 추적하기 위한 스킬 UID입니다.
        /// </summary>
        public int SkillUid;

        /// <summary>
        /// 스킬 타격이 실제로 적중했을 때 지급할 MP입니다.
        /// </summary>
        /// <remarks>
        /// Skill 패키지의 개별 타격 이벤트가 값을 채우고, Core의 MP 획득 Provider가 이 값을 읽어 보상을 지급합니다.
        /// 0 이하이면 스킬 타격 MP 보상을 지급하지 않습니다.
        /// </remarks>
        public int SkillHitMpGain;

        /// <summary>
        /// 같은 AttackId 안에서 스킬 타격 MP 보상을 반복 지급할지 여부입니다.
        /// </summary>
        public bool AllowMultipleSkillHitMpGainPerAttack;

        /// <summary>
        /// 이번 데미지에서 일반 피격 상태 전환과 피격 애니메이션을 억제할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// 지속 피해나 환경 피해처럼 HP 변화는 필요하지만 캐릭터가 매 틱 피격 모션을 재생하면 어색한 경우에 사용합니다.
        /// </remarks>
        public bool SuppressDamageReaction;

        /// <summary>
        /// 이번 데미지에서 피격 시각 효과를 억제할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// 지속 피해처럼 피격 이펙트가 매 틱 반복되면 과한 경우에 사용합니다. 데미지 텍스트와 HP 처리는 유지됩니다.
        /// </remarks>
        public bool SuppressHitEffect;

        /// <summary>
        /// 즉시 CC는 아니지만, 데미지 처리 직후 AfterDamage CC가 이어질 예정인지 여부입니다.
        /// 일반 피격 모션을 먼저 재생하면 HitStop/CC 전환이 어색해질 수 있을 때 사용합니다.
        /// </summary>
        public bool HasPendingAfterDamageCrowdControl;

        /// <summary>
        /// 실제 데미지 확정 시 재생할 카메라 Shake Preset 입니다.
        /// </summary>
        public CameraShakePreset DamageCameraShakePreset;

        /// <summary>
        /// 시전자/대상 기준 방향을 어떤 방식으로 카메라 Shake 요청으로 변환할지 지정합니다.
        /// </summary>
        public CameraShakeDirectionSource DamageCameraShakeDirectionSource = CameraShakeDirectionSource.Preset;

        /// <summary>
        /// 고정 방향 카메라 Shake에서 사용할 방향입니다.
        /// </summary>
        public Vector2 DamageCameraShakeFixedDirection = Vector2.right;

        /// <summary>
        /// 방향 계산 시 Y축을 제거하고 좌우 방향만 사용할지 여부입니다.
        /// </summary>
        public bool DamageCameraShakeHorizontalOnly = true;

        /// <summary>
        /// 데미지 카메라 Shake를 재생할 채널입니다.
        /// </summary>
        public CameraShakeChannel DamageCameraShakeChannel = CameraShakeChannel.SkillDamage;

        /// <summary>
        /// 현재 데미지를 발생시킨 Affect UID입니다.
        /// </summary>
        /// <remarks>
        /// <see cref="affectUid"/>는 피격 대상에게 새로 적용할 Affect UID이므로,
        /// 데미지 원인 추적용 UID와 의미가 충돌하지 않도록 별도 필드로 분리합니다.
        /// </remarks>
        public int SourceAffectUid;

        /// <summary>
        /// 이번 데미지로 사망했을 때 사용할 사망 연출 요청입니다.
        /// </summary>
        /// <remarks>
        /// Affect, Skill 등 상위 패키지는 Core에 직접 원인 타입을 노출하지 않고
        /// 이 범용 요청으로 변환해 전달합니다.
        /// </remarks>
        public DeathPresentationRequest DeathPresentation;

        /// <summary>
        /// 이번 데미지 확정 후 적용할 공격 HitStop 설정이 있는지 여부입니다.
        /// </summary>
        public bool HasAttackHitStopSettings;

        /// <summary>
        /// 이번 데미지 확정 후 공격자와 피격 대상에게 적용할 HitStop 설정입니다.
        /// </summary>
        public AttackHitStopSettings AttackHitStopSettings;

        /// <summary>
        /// 이번 데미지가 플레이어 기본 공격 콤보에서 발생했는지 여부입니다.
        /// </summary>
        public bool IsBasicAttackCombo;

        /// <summary>
        /// 이번 데미지를 발생시킨 기본 공격 콤보 인덱스입니다.
        /// </summary>
        public int BasicAttackComboIndex = -1;

        /// <summary>
        /// 기본 공격 콤보 전체 개수입니다.
        /// </summary>
        public int BasicAttackComboCount;

        /// <summary>
        /// 이번 데미지가 기본 공격 콤보의 마지막 단계에서 발생했는지 여부입니다.
        /// </summary>
        public bool IsLastBasicAttackCombo;

        /// <summary>
        /// 이번 공격이 가드와 상호작용하는 방식입니다.
        /// </summary>
        public GuardInteractionMode GuardInteractionMode = GuardInteractionMode.Normal;

        /// <summary>
        /// 공격 방어 타입별 가드 설정을 조회할 때 사용하는 공격 등급입니다.
        /// </summary>
        public GuardAttackType GuardAttackType = GuardAttackType.Normal;

        /// <summary>
        /// 가드 브레이크 공격이 저스트 가드 타이밍에 들어왔을 때의 처리 정책입니다.
        /// </summary>
        public GuardBreakJustGuardPolicy GuardBreakJustGuardPolicy = GuardBreakJustGuardPolicy.JustGuardCanBlock;

        /// <summary>
        /// 가드 브레이크 시 실제 HP에 적용할 데미지 배율입니다.
        /// 0이면 HP 피해 없이 가드만 파괴하고, 1이면 원래 데미지를 모두 적용합니다.
        /// </summary>
        public float GuardBreakDamageMultiplier;

        /// <summary>
        /// 가드 브레이크 시 추가로 차감할 스태미나입니다. 0이면 추가 차감하지 않습니다.
        /// </summary>
        public long GuardBreakStaminaCost;

        /// <summary>
        /// 가드 브레이크 시 우선 재생할 VFX UID입니다. 0이면 방어 설정의 기본 VFX를 사용합니다.
        /// </summary>
        public int GuardBreakVfxUid;

        /// <summary>
        /// 가드 브레이크 시 표시할 피드백 텍스트입니다. 비어 있으면 기본 텍스트를 사용합니다.
        /// </summary>
        public string GuardBreakFeedbackText;

        public List<int> ResolvedOnHitCrowdControls;
    }
    /// <summary>
    /// 캐릭터 데미지 처리
    /// </summary>
    public class CharacterDamageController
    {
        private const int PlayerIncomingHitVfxCooldownKey = -1;

        private CharacterBase _characterBase;
        private ControllerMonsterSuperArmor _controllerMonsterSuperArmor;
        private float _monsterGroggyAffectDuration;
        private int _monsterGroggyAffectUid;
        
        private Color _textColorDamageMonster;
        private Color _textColorDamagePlayer;
        private Color _textColorHeal;
        private GGemCoPlayerSettings _playerSettings;
        private GGemCoMonsterSettings _monsterSettings;
        private readonly Dictionary<int, float> _nextIncomingHitVfxPlayableTimesByKey = new Dictionary<int, float>();
        private bool _suppressNextIncomingHitAnimationEventVfx;
        
        /// <summary>
        /// 데미지 컨트롤러를 초기화하고, 몬스터 슈퍼아머 설정을 함께 주입합니다.
        /// </summary>
        /// <param name="characterBase">데미지 처리를 담당할 대상 캐릭터입니다.</param>
        /// <remarks>
        /// 슈퍼아머 컨트롤러 생성 시 설정을 함께 전달하여
        /// 내부 _config 누락으로 인한 기본값 오동작을 방지합니다.
        /// </remarks>
        public void Initialize(CharacterBase characterBase)
        {
            _characterBase = characterBase;
            if (!_characterBase)
            {
                GcLogger.LogError($"CharacterBase가 없습니다.");
                return;
            }
            
            AddressableLoaderSettings loaderSettings = AddressableLoaderSettings.Instance;
            GGemCoMonsterSettings monsterSettings = loaderSettings != null ? loaderSettings.monsterSettings : null;
            _monsterSettings = monsterSettings;

            _controllerMonsterSuperArmor = new ControllerMonsterSuperArmor();
            _controllerMonsterSuperArmor.Initialize(_characterBase, monsterSettings);
            
            _controllerMonsterSuperArmor.BreakTriggered += OnSuperArmorBreak;

            if (monsterSettings)
            {
                _monsterGroggyAffectDuration = monsterSettings.monsterGroggyAffectDuration;
                _monsterGroggyAffectUid = monsterSettings.monsterGroggyAffectUid;
            }

            if (loaderSettings != null && loaderSettings.settings)
            {
                _textColorDamageMonster = loaderSettings.settings.textColorDamageMonster;
                _textColorDamagePlayer = loaderSettings.settings.textColorDamagePlayer;
                _textColorHeal = loaderSettings.settings.textColorHeal;
            }

            _playerSettings = loaderSettings != null ? loaderSettings.playerSettings : null;
            _nextIncomingHitVfxPlayableTimesByKey.Clear();
        }

        public void Dispose()
        {
            _controllerMonsterSuperArmor.BreakTriggered -= OnSuperArmorBreak;
        }

        private void NotifyIncomingHitCombatFeedback(MetadataDamage metadataDamage, MonsterSkillCombatOutcome outcome)
        {
            if (metadataDamage == null)
                return;

            var attacker = metadataDamage.attacker;
            // 스킬 데미지 처리
            if (attacker == null || metadataDamage.SkillUid <= 0)
                return;

            var feedback = new IncomingHitCombatFeedback(
                attacker,
                _characterBase != null ? _characterBase.gameObject : null,
                metadataDamage.SkillUid,
                metadataDamage.AttackId,
                outcome,
                Time.time);

            var behaviours = attacker.GetComponents<MonoBehaviour>();
            if (behaviours == null || behaviours.Length == 0)
                return;

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitCombatFeedbackSink sink)
                {
                    sink.NotifyIncomingHitResolved(in feedback);
                }
            }
        }

        /// <summary>
        /// 실제 타격 확정 결과를 공격자 오브젝트에 되돌려줍니다.
        /// </summary>
        /// <param name="metadataDamage">타격에 사용된 데미지 메타데이터입니다.</param>
        /// <param name="outcome">최종 전투 결과입니다.</param>
        private void NotifyOutgoingAttackHitFeedback(MetadataDamage metadataDamage, MonsterSkillCombatOutcome outcome)
        {
            if (metadataDamage == null)
                return;

            GameObject attacker = metadataDamage.attacker;
            if (attacker == null)
                return;

            var feedback = new OutgoingAttackHitFeedback(
                attacker,
                _characterBase != null ? _characterBase.gameObject : null,
                metadataDamage,
                outcome,
                Time.time);

            var behaviours = attacker.GetComponents<MonoBehaviour>();
            if (behaviours == null || behaviours.Length == 0)
                return;

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IOutgoingAttackHitFeedbackSink sink)
                {
                    sink.NotifyOutgoingAttackHitResolved(in feedback);
                }
            }
        }

        /// <summary>
        /// 데미지 타입에 따라 데미지 텍스트 색상을 결정합니다.
        /// </summary>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <param name="defaultColor">타입 색상이 없을 때 사용할 기본 색상입니다.</param>
        /// <returns>데미지 텍스트에 사용할 색상입니다.</returns>
        private static Color ResolveDamageTextColor(ConfigCommon.DamageType damageType, Color defaultColor)
        {
            switch (damageType)
            {
                case ConfigCommon.DamageType.Fire:
                    return Color.red;
                case ConfigCommon.DamageType.Cold:
                    return Color.blue;
                case ConfigCommon.DamageType.Lightning:
                    return Color.yellow;
                case ConfigCommon.DamageType.Poison:
                    return Color.green;
                default:
                    return defaultColor;
            }
        }

        public void TakeDamage(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null) return;
            _suppressNextIncomingHitAnimationEventVfx = false;

            if (SceneGame.Instance.CutsceneManager.IsPlaying()) return;
            if (_characterBase.IsStatusDead() || _characterBase.IsDeathPending)
            {
                // 사망 전 액션이 진행 중이면 추가 피격으로 사망 플로우가 중복 실행되지 않도록 막습니다.
                return;
            }

            if (!_characterBase.CanReceiveDamage(metadataDamage))
            {
                NotifyIncomingHitCombatFeedback(metadataDamage, MonsterSkillCombatOutcome.Immune);
                NotifyOutgoingAttackHitFeedback(metadataDamage, MonsterSkillCombatOutcome.Immune);
                return;
            }

            long damage = metadataDamage.damage;
            ConfigCommon.DamageType damageType = metadataDamage.damageType;
            GameObject attacker = metadataDamage.attacker;
            int affectUid = metadataDamage.affectUid;
            int crowdControlUid = metadataDamage.crowdControlUid;
            List<int> resolvedOnHitCrowdControls = metadataDamage.ResolvedOnHitCrowdControls;

            // 데미지 텍스트 색상 설정
            Color damageTextColor = _textColorDamageMonster;
            if (_characterBase.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                damageTextColor = _textColorDamagePlayer;
            }
            Vector3 damageTextPosition = _characterBase.transform.position + new Vector3(0,
                _characterBase.GetHeight() * Mathf.Abs(_characterBase.originalScaleX), 0);
            damageTextColor = ResolveDamageTextColor(damageType, damageTextColor);

            CalculateManager calculateManager = CalculateManager.GetActive();
            if (calculateManager != null)
            {
                CharacterBase attackerCharacter = attacker != null ? attacker.GetComponentInParent<CharacterBase>() : null;
                DamageCalculationBreakdown outgoingBreakdown = metadataDamage.DamageBreakdown ??
                                                               calculateManager.CreateOutgoingDamageBreakdown(
                                                                   damage,
                                                                   damageType,
                                                                   attackerCharacter,
                                                                   metadataDamage.IncludeAttackerElementDamageParts);
                DamageCalculationBreakdown incomingBreakdown = calculateManager.CalculateIncomingDamageBreakdown(outgoingBreakdown, _characterBase);
                metadataDamage.DamageBreakdown = incomingBreakdown;
                damage = incomingBreakdown.TotalFinalDamage;
                damageType = incomingBreakdown.RepresentativeDamageType;
                metadataDamage.damage = damage;
                metadataDamage.damageType = damageType;

                if (damage <= 0L && HasAnyImmuneDamagePart(incomingBreakdown))
                {
                    MetadataDamageText metadataDamageText = new MetadataDamageText
                    {
                        Damage = damage,
                        Color = Color.yellow,
                        SpecialDamageText = "immune",
                        WorldPosition = damageTextPosition,
                        FontSize = 20
                    };
                    SceneGame.Instance.damageTextManager.ShowDamageText(metadataDamageText);
                    NotifyIncomingHitCombatFeedback(metadataDamage, MonsterSkillCombatOutcome.Immune);
                }
            }
            else
            {
                damage = damage > 0L ? damage : 0L;
                metadataDamage.damage = damage;
            }

            var incomingHitActionCanceler = _characterBase.GetComponent<IIncomingHitActionCanceler>();
            if (damage <= 0)
            {
                if (crowdControlUid > 0)
                {
                    NotifyIncomingHitActionCancelers(IncomingHitCancelReason.Damage);
                    _characterBase.ApplyCrowdControl(crowdControlUid, attacker);
                }
                return;
            }

            bool suppressHitReactionByGuard = false;
            bool hasGuardFeedback = false;
            bool isGuardResolved = false;
            bool overrideAfterDamageCrowdControlByGuard = false;
            bool hasRequestedPlayerHudStaminaFeedback = false;
            List<CrowdControlRuntimeData> guardCrowdControlRuntimeList = null;
            var guardResolver = _characterBase.GetComponent<IIncomingHitGuardResolver>();

            if (guardResolver != null && ShouldEvaluateGuardResolution(metadataDamage))
            {
                metadataDamage.damage = damage;
                if (guardResolver.TryResolveIncomingHit(metadataDamage, out var guardResult) && guardResult.IsResolved)
                {
                    damage = guardResult.RemainingDamage < 0 ? 0 : guardResult.RemainingDamage;
                    metadataDamage.damage = damage;
                    metadataDamage.DamageBreakdown = ScaleDamageBreakdownFinalDamage(metadataDamage.DamageBreakdown, damage);
                    suppressHitReactionByGuard = guardResult.SuppressHitReaction;
                    isGuardResolved = true;

                    if (ShouldOverrideAfterDamageCrowdControlByGuard(guardResult.Outcome))
                    {
                        overrideAfterDamageCrowdControlByGuard = true;
                        // 가드 판정이 확정된 경우에는 스킬 메타데이터 CC를 비우고,
                        // GuardAttackType 규칙에서 계산된 CC만 최종 반영합니다.
                        crowdControlUid = 0;
                        metadataDamage.crowdControlUid = 0;
                        resolvedOnHitCrowdControls?.Clear();
                        metadataDamage.ResolvedOnHitCrowdControls = resolvedOnHitCrowdControls;
                    }

                    if (guardResult.CrowdControlUid > 0)
                    {
                        AppendGuardCrowdControlRuntimeData(ref guardCrowdControlRuntimeList, guardResult);
                        AppendCrowdControlUid(ref resolvedOnHitCrowdControls, guardResult.CrowdControlUid);
                        metadataDamage.ResolvedOnHitCrowdControls = resolvedOnHitCrowdControls;
                    }

                    if (HasGuardFeedbackPresentation(guardResult))
                    {
                        MetadataDamageText guardText = new MetadataDamageText
                        {
                            Damage = 0,
                            Color = guardResult.FeedbackColor == default ? Color.cyan : guardResult.FeedbackColor,
                            SpecialDamageText = guardResult.FeedbackText,
                            ImageSprite = guardResult.FeedbackSprite,
                            ImageSize = guardResult.FeedbackSpriteSize,
                            WorldPosition = ResolveGuardFeedbackWorldPosition(damageTextPosition, guardResult),
                            UiEffectUid = guardResult.FeedbackUiEffectUid,
                        };
                        ApplyGuardFeedbackRandomXRange(guardText, guardResult);
                        SceneGame.Instance.damageTextManager.ShowDamageText(guardText);
                        hasGuardFeedback = true;
                    }

                    NotifyIncomingHitCombatFeedback(
                        metadataDamage,
                        ResolveCombatOutcomeByGuardResult(guardResult));

                    hasRequestedPlayerHudStaminaFeedback =
                        TryPlayPlayerHudGuardSuccessFeedback(metadataDamage, guardResult);
                }
            }

            if (overrideAfterDamageCrowdControlByGuard)
            {
                // 가드 결과(가드/저스트 가드/가드 브레이크)로 판정이 확정된 경우에는
                // SkillDamageClip의 AfterDamage CC 대신
                // GuardAttackType 규칙에서 계산된 CC만 적용합니다.
                metadataDamage.ResolvedOnHitCrowdControls = resolvedOnHitCrowdControls;
            }

            if (damage <= 0)
            {
                ApplyZeroDamageGuardCrowdControls(
                    metadataDamage,
                    attacker,
                    crowdControlUid,
                    resolvedOnHitCrowdControls,
                    guardCrowdControlRuntimeList,
                    isGuardResolved);
                return;
            }

            // Item Bonus HP(소모형 추가 최대 HP)부터 먼저 차감
            //  - 0이 되면 즉시 소멸(외부에서 UI/저장 갱신 처리)
            long remainingDamage = _characterBase.ConsumeHpTempItem(damage);
            // 런타임 스킬 Temp HP(비저장 보호막) 소모
            remainingDamage = _characterBase.ConsumeHpTempRuntime(remainingDamage);
            // 패시브 임시 HP 소모
            remainingDamage = _characterBase.ConsumeHpTempPassive(remainingDamage);

            // 남은 데미지를 Base HP에서 차감
            long remainHp = _characterBase.CurrentHp.Value - remainingDamage;
            // -1 이면 죽지 않는다
            if (_characterBase.BaseHp < 0)
            {
                remainHp = 1;
            }

            // 외부 시스템(예: 보스 페이즈 전환)이 최종 HP를 보정할 수 있는 확장 지점입니다.
            long adjustedHp = ResolveFinalHpOnIncomingHit(metadataDamage, remainHp);
            bool isHpAdjusted = adjustedHp != remainHp;
            remainHp = adjustedHp;

            // 보정 결과가 현재 HP와 같거나 더 크면 이번 피격은 흡수된 것으로 간주하고 종료합니다.
            // (연출/상태 반응 중복을 막기 위해 즉시 반환)
            if (isHpAdjusted && remainHp >= _characterBase.CurrentHp.Value)
            {
                _characterBase.CurrentHp.OnNext(remainHp);
                return;
            }

            // 타격 확정: 공격자에게 OnHit(코팅/부여형 버프 등) 트리거를 전달한다.
            if (attacker != null)
            {
                AffectRuntimeBridge.NotifyOnHit(attacker, _characterBase.gameObject);
            }

            NotifyIncomingHitCombatFeedback(metadataDamage, MonsterSkillCombatOutcome.Hit);
            NotifyOutgoingAttackHitFeedback(metadataDamage, MonsterSkillCombatOutcome.Hit);
            TryPlayDamageCameraShake(metadataDamage);

            if (!hasGuardFeedback)
            {
                MetadataDamageText metadataDamageText2 = new MetadataDamageText
                {
                    Damage = damage,
                    Color = damageTextColor,
                    WorldPosition = damageTextPosition
                };
                SceneGame.Instance.damageTextManager.ShowDamageText(metadataDamageText2);
            }

            TryPlayIncomingHitEffects(metadataDamage);
            
            if (remainHp <= 0)
            {
                if (crowdControlUid > 0)
                {
                    _characterBase.ApplyCrowdControl(crowdControlUid, metadataDamage.attacker);
                }
                // 사망 처리 전에 입력 액션을 먼저 정리해 후속 입력이 잠기지 않도록 합니다.
                NotifyIncomingHitActionCancelers(IncomingHitCancelReason.Death);

                // 사망했을 때, UI 표현을 위해 0으로 처리
                remainHp = 0;
                _characterBase.CurrentMp.OnNext(0);
                bool playDeadAnimation = true;
                // CC 가 있으면 CC 처리 후 사망 처리
                if ((resolvedOnHitCrowdControls != null && resolvedOnHitCrowdControls.Count > 0) || crowdControlUid > 0)
                {
                    playDeadAnimation = false;
                }
                _characterBase.Dead(CharacterConstants.DieReasonType.Battle, attacker, playDeadAnimation, metadataDamage.DeathPresentation);
            }
            else
            {
                if (!hasRequestedPlayerHudStaminaFeedback)
                    TryPlayPlayerHudDamageFeedback(metadataDamage);
                
                bool shouldPlayDamageReaction = !suppressHitReactionByGuard && !metadataDamage.SuppressDamageReaction;
                CharacterConstants.HitReactionType hitReactionType = CharacterConstants.HitReactionType.None;
                
                // StaggerResistanceController가 있고, 이번 타격이 스태거 판정에 관여하는 경우에만
                // “피격 모션/상태 전환”을 결정합니다.
                if (_controllerMonsterSuperArmor != null && _controllerMonsterSuperArmor.IsEnableSuperArmor() && 
                    metadataDamage.HitReactionType != CharacterConstants.HitReactionType.None &&
                    (metadataDamage.ForceHitReaction || metadataDamage.StaggerStackDamage > 0))
                {
                    var hit = new HitPayload(
                        metadataDamage.StaggerStackDamage,
                        metadataDamage.HitReactionType,
                        metadataDamage.ForceHitReaction,
                        metadataDamage.AttackId);

                    // 외부 상태(무적/컷씬 등)로 리액션을 막고 싶으면 여기에서 true를 전달
                    // (현재 흐름에서는 컷씬은 상단에서 리턴되므로 기본 false)
                    var decision = _controllerMonsterSuperArmor.ApplyHit(in hit, ignoreReactionByStatus: false);
                    shouldPlayDamageReaction = decision.ShouldReact;
                    hitReactionType = decision.ReactionType;
                }

                // 넉백 상태에서는 별도 시스템이 상태/연출을 담당하므로 여기서는 피격 모션을 막는다.
                if (shouldPlayDamageReaction)
                {
                    // 피격 상태/CC 적용 전에 입력 액션을 먼저 정리해 가드/점프/대시 상태가 남지 않도록 합니다.
                    NotifyIncomingHitActionCancelers(IncomingHitCancelReason.Damage);

                    if (hitReactionType == CharacterConstants.HitReactionType.Flinch)
                    {
                        _characterBase.CharacterAnimationController.PlayAnimationGroggy();
                    }
                    // CC 처리가 있을 때 
                    else if (crowdControlUid > 0)
                    {
                        _characterBase.ApplyCrowdControl(crowdControlUid, metadataDamage.attacker, true);
                    }
                    else if (!metadataDamage.HasPendingAfterDamageCrowdControl)
                    {
                        // 순서 중요.
                        _characterBase.SetStatusDamage();
                        _suppressNextIncomingHitAnimationEventVfx = metadataDamage.SuppressHitEffect;
                        _characterBase.CharacterAnimationController.PlayDamageAnimation();
                    }
                }
                _characterBase.OnDamageResolved(metadataDamage);
                
                if (affectUid > 0)
                {
                    _characterBase.AddAffect(affectUid, metadataDamage.attacker);
                }
            }

            bool isEndCharacterStop = false;
            // CC 가 있으면 CC 처리 후 사망 처리
            if ((resolvedOnHitCrowdControls != null && resolvedOnHitCrowdControls.Count > 0) || crowdControlUid > 0)
            {
                isEndCharacterStop = true;
            }
            ApplyResolvedCrowdControlSequence(
                metadataDamage,
                metadataDamage.attacker,
                resolvedOnHitCrowdControls,
                guardCrowdControlRuntimeList,
                isEndCharacterStop);
            
            _characterBase.CurrentHp.OnNext(remainHp);

            ApplyConfirmedAttackHitStop(metadataDamage);
        }

        /// <summary>
        /// 데미지 분해 결과에 면역 처리된 파트가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="breakdown">확인할 데미지 분해 결과입니다.</param>
        /// <returns>면역 처리된 데미지 파트가 하나 이상 있으면 true입니다.</returns>
        private static bool HasAnyImmuneDamagePart(DamageCalculationBreakdown breakdown)
        {
            if (breakdown == null || !breakdown.HasParts)
                return false;

            IReadOnlyList<DamagePartResult> parts = breakdown.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsImmune)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 데미지가 가드/저스트 가드 판정 대상인지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">피격 처리에 사용되는 데미지 메타데이터입니다.</param>
        /// <returns>가드 판정을 수행해야 하면 true, 지속 피해처럼 가드 대상이 아니면 false입니다.</returns>
        /// <remarks>
        /// 지속 피해는 상태 효과 Tick에 의해 누적되는 피해이므로 플레이어의 가드 입력으로 막지 않습니다.
        /// 상위 패키지가 명시 플래그를 설정하지 않은 경우에도 모든 데미지 파트가 Dot이면 방어적으로 제외합니다.
        /// </remarks>
        private static bool ShouldEvaluateGuardResolution(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
                return false;

            if (metadataDamage.IsDamageOverTime)
                return false;

            DamageCalculationBreakdown breakdown = metadataDamage.DamageBreakdown;
            if (breakdown == null || !breakdown.HasParts)
                return true;

            IReadOnlyList<DamagePartResult> parts = breakdown.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].IsDot)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 가드 등 후처리로 변경된 최종 데미지에 맞춰 파트별 최종 데미지를 비례 보정합니다.
        /// </summary>
        /// <param name="source">보정 전 데미지 분해 결과입니다.</param>
        /// <param name="targetFinalDamage">후처리까지 반영된 전체 최종 데미지입니다.</param>
        /// <returns>전체 최종 데미지에 맞춰 보정된 데미지 분해 결과입니다.</returns>
        /// <remarks>
        /// 가드가 총 피해만 줄이는 현재 구조에서 속성 게이지가 가드 전 피해량으로 누적되지 않도록 보정합니다.
        /// 마지막 유효 파트에는 반올림 오차를 몰아 전체 합계가 정확히 맞도록 합니다.
        /// </remarks>
        private static DamageCalculationBreakdown ScaleDamageBreakdownFinalDamage(
            DamageCalculationBreakdown source,
            long targetFinalDamage)
        {
            if (source == null || !source.HasParts)
                return source;

            long sourceFinalDamage = source.TotalFinalDamage;
            if (sourceFinalDamage == targetFinalDamage)
                return source;

            var scaled = new DamageCalculationBreakdown();
            IReadOnlyList<DamagePartResult> parts = source.Parts;
            long safeTargetFinalDamage = targetFinalDamage > 0L ? targetFinalDamage : 0L;
            long assignedFinalDamage = 0L;
            int lastPositivePartIndex = FindLastPositiveDamagePartIndex(parts);

            for (int i = 0; i < parts.Count; i++)
            {
                DamagePartResult part = parts[i];
                long scaledFinalDamage = 0L;

                if (sourceFinalDamage > 0L && part.FinalDamage > 0L && safeTargetFinalDamage > 0L)
                {
                    scaledFinalDamage = i == lastPositivePartIndex
                        ? safeTargetFinalDamage - assignedFinalDamage
                        : (long)System.Math.Round(part.FinalDamage * (double)safeTargetFinalDamage / sourceFinalDamage);
                    if (scaledFinalDamage < 0L)
                        scaledFinalDamage = 0L;
                    assignedFinalDamage += scaledFinalDamage;
                }

                scaled.AddPart(new DamagePartResult(
                    part.RawDamage,
                    scaledFinalDamage,
                    part.DamageType,
                    ScalePartAttackerElementDamage(part, scaledFinalDamage),
                    part.IsImmune,
                    part.AppliedDefaultDamage,
                    part.IsDot));
            }

            return scaled;
        }

        /// <summary>
        /// 가드 후 최종 파트 데미지에 맞춰 공격자 속성 데미지 기준값도 함께 보정합니다.
        /// </summary>
        /// <param name="part">보정 전 데미지 파트입니다.</param>
        /// <param name="scaledFinalDamage">보정된 최종 파트 데미지입니다.</param>
        /// <returns>게이지 누적 기준으로 사용할 보정된 공격자 속성 데미지입니다.</returns>
        private static long ScalePartAttackerElementDamage(in DamagePartResult part, long scaledFinalDamage)
        {
            if (part.AttackerElementDamage <= 0L)
                return 0L;

            if (part.FinalDamage <= 0L)
                return scaledFinalDamage > 0L ? part.AttackerElementDamage : 0L;

            if (scaledFinalDamage <= 0L)
                return 0L;

            double ratio = scaledFinalDamage / (double)part.FinalDamage;
            double scaled = part.AttackerElementDamage * ratio;
            if (scaled <= 0d)
                return 0L;
            if (scaled >= long.MaxValue)
                return long.MaxValue;

            return (long)System.Math.Round(scaled);
        }

        /// <summary>
        /// 최종 데미지가 있는 마지막 파트 인덱스를 찾습니다.
        /// </summary>
        /// <param name="parts">검색할 데미지 파트 목록입니다.</param>
        /// <returns>최종 데미지가 있는 마지막 파트 인덱스입니다. 없으면 -1입니다.</returns>
        private static int FindLastPositiveDamagePartIndex(IReadOnlyList<DamagePartResult> parts)
        {
            if (parts == null)
                return -1;

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                if (parts[i].FinalDamage > 0L)
                    return i;
            }

            return -1;
        }


        /// <summary>
        /// 데미지가 실제로 확정된 뒤 공격 메타데이터에 포함된 HitStop 설정을 적용합니다.
        /// </summary>
        /// <param name="metadataDamage">이번 피격 처리에 사용한 데미지 메타데이터입니다.</param>
        /// <remarks>
        /// 공격 애니메이션 시작 시점이 아니라 실제 데미지 확정 이후에 호출하여,
        /// 빗맞은 공격이나 무효 처리된 공격에서 HitStop이 발생하지 않도록 합니다.
        /// </remarks>
        private void ApplyConfirmedAttackHitStop(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null || !metadataDamage.HasAttackHitStopSettings)
                return;

            AttackHitStopSettings hitStopSettings = metadataDamage.AttackHitStopSettings;
            if (!hitStopSettings.HasAnyHitStop)
                return;

            CharacterBase attackerCharacter = metadataDamage.attacker != null
                ? metadataDamage.attacker.GetComponent<CharacterBase>()
                : null;
            if (attackerCharacter == null)
                return;

            CharacterBase.HitStopConfig hitStopConfig = attackerCharacter.GetResolvedHitStopConfig();
            if (!hitStopConfig.Enabled)
                return;

            int sourceSkillUid = metadataDamage.SkillUid;
            if (hitStopSettings.useHitStopSelf)
            {
                float selfSeconds = hitStopSettings.ResolveSelfSeconds(hitStopConfig);
                if (selfSeconds > 0f)
                {
                    attackerCharacter.ApplyHitStop(new HitStopRequest(
                        selfSeconds,
                        pauseAnimation: hitStopConfig.PauseAnimation,
                        freezePhysics: hitStopConfig.FreezePhysics,
                        sourceSkillUid: sourceSkillUid));
                }
            }

            if (hitStopSettings.useHitStopTarget && _characterBase != null)
            {
                float targetSeconds = hitStopSettings.ResolveTargetSeconds(hitStopConfig);
                if (targetSeconds > 0f)
                {
                    _characterBase.ApplyHitStop(new HitStopRequest(
                        targetSeconds,
                        pauseAnimation: hitStopConfig.PauseAnimation,
                        freezePhysics: hitStopConfig.FreezePhysics,
                        sourceSkillUid: sourceSkillUid));
                }
            }
        }

        /// <summary>
        /// 가드 결과로 데미지가 0이 되었을 때도 설정된 Crowd Control을 적용합니다.
        /// </summary>
        /// <param name="metadataDamage">원본 피격 메타데이터입니다.</param>
        /// <param name="attacker">공격자 오브젝트입니다.</param>
        /// <param name="crowdControlUid">데미지 메타데이터에 직접 지정된 단일 Crowd Control UID입니다.</param>
        /// <param name="resolvedOnHitCrowdControls">OnHit 또는 가드 결과로 수집된 Crowd Control UID 목록입니다.</param>
        /// <param name="guardCrowdControlRuntimeList">가드 결과에서 생성한 1회성 Crowd Control 런타임 데이터 목록입니다.</param>
        /// <param name="isGuardResolved">이번 피격이 가드 시스템에서 처리되었는지 여부입니다.</param>
        private void ApplyZeroDamageGuardCrowdControls(
            MetadataDamage metadataDamage,
            GameObject attacker,
            int crowdControlUid,
            List<int> resolvedOnHitCrowdControls,
            List<CrowdControlRuntimeData> guardCrowdControlRuntimeList,
            bool isGuardResolved)
        {
            if (!isGuardResolved)
                return;

            if (crowdControlUid > 0)
            {
                _characterBase.ApplyCrowdControl(crowdControlUid, attacker);
            }

            ApplyResolvedCrowdControlSequence(
                metadataDamage,
                attacker,
                resolvedOnHitCrowdControls,
                guardCrowdControlRuntimeList,
                isEndCharacterStop: true);
        }

        /// <summary>
        /// 가드 결과 CC를 테이블 원본에서 복제하고 1회성 애니메이션 오버라이드를 적용해 목록에 추가합니다.
        /// </summary>
        /// <param name="guardCrowdControlRuntimeList">생성된 런타임 CC 목록입니다.</param>
        /// <param name="guardResult">가드 판정 결과입니다.</param>
        private static void AppendGuardCrowdControlRuntimeData(
            ref List<CrowdControlRuntimeData> guardCrowdControlRuntimeList,
            GuardResolutionResult guardResult)
        {
            if (guardResult.CrowdControlUid <= 0)
                return;

            CrowdControlRuntimeData source = TableLoaderManager.Instance != null
                ? TableLoaderManager.Instance.GetCrowdControlRuntimeData(guardResult.CrowdControlUid, logIfMissing: false)
                : null;
            if (source == null)
                return;

            CrowdControlRuntimeData cloned = source.Clone();
            cloned.AnimationOverride = guardResult.CrowdControlAnimationOverride;

            guardCrowdControlRuntimeList ??= new List<CrowdControlRuntimeData>(1);
            guardCrowdControlRuntimeList.Add(cloned);
        }

        /// <summary>
        /// 가드 결과 CC가 있으면 런타임 데이터 목록을 우선 적용하고, 없으면 기존 UID 목록을 적용합니다.
        /// </summary>
        /// <param name="metadataDamage">원본 피격 메타데이터입니다.</param>
        /// <param name="attacker">공격자 오브젝트입니다.</param>
        /// <param name="resolvedOnHitCrowdControls">UID 기반 Crowd Control 목록입니다.</param>
        /// <param name="guardCrowdControlRuntimeList">가드 결과에서 생성한 런타임 Crowd Control 목록입니다.</param>
        /// <param name="isEndCharacterStop">CC 완료 후 CharacterBase.Stop 호출 여부입니다.</param>
        private void ApplyResolvedCrowdControlSequence(
            MetadataDamage metadataDamage,
            GameObject attacker,
            List<int> resolvedOnHitCrowdControls,
            List<CrowdControlRuntimeData> guardCrowdControlRuntimeList,
            bool isEndCharacterStop)
        {
            if (guardCrowdControlRuntimeList != null && guardCrowdControlRuntimeList.Count > 0)
            {
                metadataDamage.ResolvedOnHitCrowdControls = resolvedOnHitCrowdControls;
                _characterBase.ApplyCrowdControlSequence(guardCrowdControlRuntimeList, attacker, isEndCharacterStop);
                return;
            }

            if (resolvedOnHitCrowdControls == null || resolvedOnHitCrowdControls.Count == 0)
                return;

            metadataDamage.ResolvedOnHitCrowdControls = resolvedOnHitCrowdControls;
            _characterBase.ApplyCrowdControlSequence(resolvedOnHitCrowdControls, attacker, isEndCharacterStop);
        }

        /// <summary>
        /// Crowd Control UID를 결과 목록에 추가합니다.
        /// </summary>
        /// <param name="resolvedOnHitCrowdControls">UID를 추가할 결과 목록입니다.</param>
        /// <param name="crowdControlUid">추가할 Crowd Control UID입니다.</param>
        private static void AppendCrowdControlUid(ref List<int> resolvedOnHitCrowdControls, int crowdControlUid)
        {
            if (crowdControlUid <= 0)
                return;

            resolvedOnHitCrowdControls ??= new List<int>(1);
            resolvedOnHitCrowdControls.Add(crowdControlUid);
        }

        /// <summary>
        /// 가드 판정 결과를 공격자에게 전달할 전투 결과로 변환합니다.
        /// </summary>
        /// <param name="guardResult">가드 판정 결과입니다.</param>
        /// <returns>공격자 측 시스템이 소비할 전투 결과입니다.</returns>
        private static MonsterSkillCombatOutcome ResolveCombatOutcomeByGuardResult(GuardResolutionResult guardResult)
        {
            switch (guardResult.Outcome)
            {
                case GuardResolutionOutcome.GuardBroken:
                    return MonsterSkillCombatOutcome.GuardBroken;
                case GuardResolutionOutcome.JustGuarded:
                    return MonsterSkillCombatOutcome.JustGuarded;
                case GuardResolutionOutcome.Guarded:
                    return MonsterSkillCombatOutcome.Guarded;
                default:
                    return guardResult.IsJustGuard
                        ? MonsterSkillCombatOutcome.JustGuarded
                        : MonsterSkillCombatOutcome.Guarded;
            }
        }

        /// <summary>
        /// 가드 판정 결과일 때 AfterDamage Crowd Control을 가드 규칙 결과로 대체할지 여부를 반환합니다.
        /// </summary>
        /// <param name="outcome">가드 판정 최종 결과입니다.</param>
        /// <returns>
        /// 일반 가드/저스트 가드/가드 브레이크로 판정이 확정된 경우 <see langword="true"/>입니다.
        /// </returns>
        private static bool ShouldOverrideAfterDamageCrowdControlByGuard(GuardResolutionOutcome outcome)
        {
            return outcome == GuardResolutionOutcome.Guarded ||
                   outcome == GuardResolutionOutcome.JustGuarded ||
                   outcome == GuardResolutionOutcome.GuardBroken;
        }

        /// <summary>
        /// 가드 판정 결과에 화면에 표시할 텍스트 또는 스프라이트 피드백이 있는지 확인합니다.
        /// </summary>
        /// <param name="guardResult">가드 판정 결과입니다.</param>
        /// <returns>텍스트 또는 스프라이트 피드백이 있으면 <see langword="true"/>입니다.</returns>
        private static bool HasGuardFeedbackPresentation(GuardResolutionResult guardResult)
        {
            return !string.IsNullOrEmpty(guardResult.FeedbackText) || guardResult.FeedbackSprite != null;
        }

        /// <summary>
        /// 가드 피드백 표시 위치를 가드 판정 결과의 X 좌표 정책에 맞춰 계산합니다.
        /// </summary>
        /// <param name="defaultWorldPosition">기존 데미지 텍스트가 사용하던 기본 월드 위치입니다.</param>
        /// <param name="guardResult">가드 판정 결과와 피드백 위치 정책입니다.</param>
        /// <returns>최종 가드 피드백 표시 월드 위치입니다.</returns>
        private Vector3 ResolveGuardFeedbackWorldPosition(
            Vector3 defaultWorldPosition,
            GuardResolutionResult guardResult)
        {
            if (!guardResult.UseDefenderXForFeedback || _characterBase == null)
                return defaultWorldPosition;

            // Y/Z는 기존 피격 위치 기준을 유지하고, X만 방어자 위치 기준으로 고정합니다.
            defaultWorldPosition.x = _characterBase.transform.position.x + guardResult.FeedbackDefenderXOffset;
            return defaultWorldPosition;
        }

        /// <summary>
        /// 가드 피드백 전용 랜덤 X 범위 오버라이드를 플로팅 표시 요청에 적용합니다.
        /// </summary>
        /// <param name="request">플로팅 표시 요청입니다.</param>
        /// <param name="guardResult">가드 판정 결과와 랜덤 X 범위 정책입니다.</param>
        private static void ApplyGuardFeedbackRandomXRange(
            MetadataDamageText request,
            GuardResolutionResult guardResult)
        {
            if (request == null || !guardResult.OverrideFeedbackRandomXRange)
                return;

            request.RandomXRange = Mathf.Max(0f, guardResult.FeedbackRandomXRange);
        }

        /// <summary>
        /// 플레이어의 가드 성공 결과에 맞춰 스테미나 HUD 충격 피드백을 재생합니다.
        /// </summary>
        /// <param name="metadataDamage">가드 판정에 사용된 타격 메타데이터입니다.</param>
        /// <param name="guardResult">가드 판정 결과입니다.</param>
        /// <returns>스테미나 HUD 피드백 재생을 요청했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryPlayPlayerHudGuardSuccessFeedback(
            MetadataDamage metadataDamage,
            GuardResolutionResult guardResult)
        {
            if (!ShouldPlayPlayerHudGuardSuccessFeedback(guardResult))
                return false;

            return TryPlayPlayerHudDamageFeedback(metadataDamage);
        }

        /// <summary>
        /// 플레이어 스테미나 HUD에 가드 성공 충격 피드백을 재생해야 하는 결과인지 확인합니다.
        /// </summary>
        /// <param name="guardResult">가드 판정 결과입니다.</param>
        /// <returns>일반 가드 또는 가드 브레이크 성공이면 <see langword="true"/>를 반환합니다.</returns>
        private static bool ShouldPlayPlayerHudGuardSuccessFeedback(GuardResolutionResult guardResult)
        {
            if (!guardResult.IsResolved)
                return false;

            return guardResult.Outcome == GuardResolutionOutcome.Guarded ||
                   guardResult.Outcome == GuardResolutionOutcome.GuardBroken;
        }

        /// <summary>
        /// 플레이어 스테미나 HUD의 충격 피드백을 공격자 방향 기준으로 재생합니다.
        /// </summary>
        /// <param name="metadataDamage">공격자 정보를 포함한 타격 메타데이터입니다.</param>
        /// <returns>스테미나 HUD 피드백 재생을 요청했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryPlayPlayerHudDamageFeedback(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
            {
                return false;
            }

            if (!(_characterBase is Player player))
            {
                return false;
            }

            if (metadataDamage.attacker != null)
            {
                player.PlayStaminaDamageFeedbackFromAttacker(metadataDamage.attacker.transform);
                return true;
            }

            player.PlayDefaultStaminaDamageFeedback();
            return true;
        }

        /// <summary>
        /// 확정 데미지에 대한 피격 시각 효과를 재생합니다.
        /// </summary>
        /// <param name="metadataDamage">현재 피격 처리에 사용한 데미지 메타데이터입니다.</param>
        /// <remarks>
        /// Affect Damage Modifier처럼 HP 변화만 필요하고 피격 플래시/VFX는 생략해야 하는 경우
        /// <see cref="MetadataDamage.SuppressHitEffect"/>를 통해 이 경로를 차단합니다.
        /// </remarks>
        private void TryPlayIncomingHitEffects(MetadataDamage metadataDamage)
        {
            if (metadataDamage != null && metadataDamage.SuppressHitEffect)
                return;

            _characterBase.TryPlaySpriteWhiteOverlayOnHit();
            TryPlayIncomingHitVfxByTrigger(IncomingHitVfxTriggerType.OnDamageConfirmed);
        }

        /// <summary>
        /// 기존 플레이어 전용 피격 VFX 재생 API와의 호환을 유지합니다.
        /// </summary>
        /// <param name="triggerType">기존 플레이어 설정 기준의 호출 트리거 타입입니다.</param>
        /// <remarks>
        /// 내부에서는 캐릭터 공통 피격 VFX 트리거 타입으로 변환한 뒤 같은 재생 경로를 사용합니다.
        /// </remarks>
        internal void TryPlayPlayerIncomingHitVfxByTrigger(GGemCoPlayerSettings.IncomingHitVfxTriggerType triggerType)
        {
            TryPlayIncomingHitVfxByTrigger(IncomingHitVfxSettings.ConvertTriggerType(triggerType));
        }

        /// <summary>
        /// 설정된 트리거 타입에 따라 캐릭터 피격 VFX 재생을 시도합니다.
        /// </summary>
        /// <param name="triggerType">현재 호출 경로의 트리거 타입입니다.</param>
        /// <remarks>
        /// 플레이어는 기존 단일 설정을 사용하고, 몬스터는 <see cref="GGemCoMonsterSettings.incomingHitVfxList"/>에
        /// 등록된 여러 설정을 순서대로 검사하여 조건에 맞는 VFX를 재생합니다.
        /// </remarks>
        internal void TryPlayIncomingHitVfxByTrigger(IncomingHitVfxTriggerType triggerType)
        {
            if (_characterBase == null)
            {
                return;
            }

            if (triggerType == IncomingHitVfxTriggerType.OnAnimationEventHit &&
                _suppressNextIncomingHitAnimationEventVfx)
            {
                _suppressNextIncomingHitAnimationEventVfx = false;
                return;
            }

            if (_characterBase is Player)
            {
                TryPlayPlayerIncomingHitVfx(triggerType);
                return;
            }

            if (_characterBase is Monster)
            {
                TryPlayMonsterIncomingHitVfx(triggerType);
            }
        }

        /// <summary>
        /// 플레이어 설정에 저장된 단일 피격 VFX 재생을 시도합니다.
        /// </summary>
        /// <param name="triggerType">현재 호출 경로의 트리거 타입입니다.</param>
        /// <remarks>
        /// 플레이어 ScriptableObject의 기존 직렬화 타입을 유지하기 위해 런타임에서 공통 설정 타입으로 변환합니다.
        /// </remarks>
        private void TryPlayPlayerIncomingHitVfx(IncomingHitVfxTriggerType triggerType)
        {
            if (_playerSettings == null && AddressableLoaderSettings.Instance != null)
            {
                _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            }

            if (_playerSettings == null)
            {
                return;
            }

            IncomingHitVfxSettings settings = IncomingHitVfxSettings.FromPlayerSettings(_playerSettings.incomingHitVfx);
            TryPlayIncomingHitVfxSettings(settings, PlayerIncomingHitVfxCooldownKey, triggerType);
        }

        /// <summary>
        /// 몬스터 설정에 등록된 피격 VFX 목록을 순회하며 재생을 시도합니다.
        /// </summary>
        /// <param name="triggerType">현재 호출 경로의 트리거 타입입니다.</param>
        /// <remarks>
        /// 각 VFX 항목은 독립된 최소 재생 간격을 가집니다.
        /// 따라서 한 VFX가 쿨타임 중이어도 다른 VFX는 조건이 맞으면 재생될 수 있습니다.
        /// </remarks>
        private void TryPlayMonsterIncomingHitVfx(IncomingHitVfxTriggerType triggerType)
        {
            if (_monsterSettings == null && AddressableLoaderSettings.Instance != null)
            {
                _monsterSettings = AddressableLoaderSettings.Instance.monsterSettings;
            }

            if (_monsterSettings == null || _monsterSettings.incomingHitVfxList == null)
            {
                return;
            }

            for (int i = 0; i < _monsterSettings.incomingHitVfxList.Count; i++)
            {
                TryPlayIncomingHitVfxSettings(_monsterSettings.incomingHitVfxList[i], i, triggerType);
            }
        }

        /// <summary>
        /// 피격 VFX 설정 1개에 대한 조건을 검사하고 실제 VFX 생성을 요청합니다.
        /// </summary>
        /// <param name="settings">검사할 피격 VFX 설정입니다.</param>
        /// <param name="cooldownKey">최소 재생 간격을 구분하기 위한 키입니다.</param>
        /// <param name="triggerType">현재 호출 경로의 트리거 타입입니다.</param>
        /// <returns>VFX 생성 요청을 보냈으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryPlayIncomingHitVfxSettings(
            IncomingHitVfxSettings settings,
            int cooldownKey,
            IncomingHitVfxTriggerType triggerType)
        {
            if (!settings.enabled)
            {
                return false;
            }

            StruckAnimationEventVfx vfxPayload = settings.GetRuntimeVfx();
            if (vfxPayload == null || vfxPayload.Uid <= 0)
            {
                return false;
            }

            // 설정된 트리거 정책과 현재 호출 경로가 다르면 재생하지 않습니다.
            if (!IsIncomingHitVfxTriggerMatched(settings.triggerType, triggerType))
            {
                return false;
            }

            if (settings.minIntervalSeconds > 0f &&
                _nextIncomingHitVfxPlayableTimesByKey.TryGetValue(cooldownKey, out float nextPlayableTime) &&
                Time.time < nextPlayableTime)
            {
                return false;
            }

            SceneGame scene = SceneGame.Instance;
            if (scene == null || scene.VfxManager == null)
            {
                return false;
            }

            // AnimationEvent VFX와 동일한 payload 변환 경로를 사용해 위치/Flip/Offset 정책 중복을 제거합니다.
            VfxSpawnRequest spawnRequest = VfxSpawnRequest.FromAnimationEvent(vfxPayload, _characterBase.gameObject);
            spawnRequest.Owner = _characterBase;
            spawnRequest.Target = _characterBase;
            spawnRequest.OwnerGameObject = _characterBase.gameObject;
            spawnRequest.ForceOneShot = !IncomingHitVfxSettings.IsFollowVfx(vfxPayload);
            ApplyIncomingHitVfxFollowMode(ref spawnRequest, settings, vfxPayload);

            scene.VfxManager.CreateVfx(spawnRequest);

            if (settings.minIntervalSeconds > 0f)
            {
                _nextIncomingHitVfxPlayableTimesByKey[cooldownKey] = Time.time + settings.minIntervalSeconds;
            }

            return true;
        }

        /// <summary>
        /// 피격 VFX 설정에 지정된 Follow 모드와 Follow 위치 기준 정책을 생성 요청에 반영합니다.
        /// </summary>
        /// <param name="spawnRequest">수정할 VFX 생성 요청입니다.</param>
        /// <param name="settings">현재 피격 VFX 설정입니다.</param>
        /// <param name="vfxPayload">실제 재생에 사용할 VFX payload입니다.</param>
        /// <remarks>
        /// <see cref="IncomingHitVfxSettings.followMode"/>가 지정되어 있으면 해당 값을 우선 사용하고,
        /// 값이 없으면 기존 <see cref="AnimationEventVfxFlipPolicy.EventCharacterFollow"/> 정책을 호환 처리합니다.
        /// Follow 위치 기준은 피격 설정 값을 우선 사용하고, 기본값이면 VFX payload 값을 사용합니다.
        /// </remarks>
        private void ApplyIncomingHitVfxFollowMode(
            ref VfxSpawnRequest spawnRequest,
            IncomingHitVfxSettings settings,
            StruckAnimationEventVfx vfxPayload)
        {
            VfxConstants.FollowMode resolvedFollowMode = settings.GetRuntimeFollowMode(vfxPayload);
            if (resolvedFollowMode == VfxConstants.FollowMode.None)
            {
                spawnRequest.ForceOneShot = true;
                return;
            }

            spawnRequest.FollowTarget = _characterBase;
            spawnRequest.FollowModeOverride = resolvedFollowMode;
            spawnRequest.FollowAnchorModeOverride = settings.GetRuntimeFollowAnchorMode(vfxPayload);
            spawnRequest.ForceOneShot = false;
        }

        /// <summary>
        /// 설정된 피격 VFX 트리거 정책과 현재 호출 트리거의 일치 여부를 반환합니다.
        /// </summary>
        /// <param name="configuredTriggerType">설정 자산에 저장된 트리거 정책입니다.</param>
        /// <param name="currentTriggerType">현재 실행 중인 트리거 경로입니다.</param>
        /// <returns>정책이 현재 트리거를 허용하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsIncomingHitVfxTriggerMatched(
            IncomingHitVfxTriggerType configuredTriggerType,
            IncomingHitVfxTriggerType currentTriggerType)
        {
            switch (configuredTriggerType)
            {
                case IncomingHitVfxTriggerType.OnDamageConfirmed:
                    return currentTriggerType == IncomingHitVfxTriggerType.OnDamageConfirmed;
                case IncomingHitVfxTriggerType.OnAnimationEventHit:
                    return currentTriggerType == IncomingHitVfxTriggerType.OnAnimationEventHit;
                case IncomingHitVfxTriggerType.Both:
                    return currentTriggerType == IncomingHitVfxTriggerType.OnDamageConfirmed
                           || currentTriggerType == IncomingHitVfxTriggerType.OnAnimationEventHit;
                default:
                    // 신규 enum 값이 추가되기 전 구버전 데이터와의 호환을 위해 기본 경로를 유지합니다.
                    return currentTriggerType == IncomingHitVfxTriggerType.OnDamageConfirmed;
            }
        }

        private void TryPlayDamageCameraShake(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
            {
                return;
            }

            if (metadataDamage.DamageCameraShakePreset == null)
            {
                return;
            }

            CameraManager cameraManager = SceneGame.Instance != null ? SceneGame.Instance.cameraManager : null;
            if (cameraManager == null)
            {
                return;
            }

            Transform attackerTransform = metadataDamage.attacker != null ? metadataDamage.attacker.transform : null;
            CameraShakeRequest request = DirectionalCameraShakeUtility.CreateRequest(
                metadataDamage.DamageCameraShakePreset,
                attackerTransform,
                _characterBase != null ? _characterBase.transform : null,
                metadataDamage.DamageCameraShakeDirectionSource,
                metadataDamage.DamageCameraShakeFixedDirection,
                metadataDamage.DamageCameraShakeHorizontalOnly,
                metadataDamage.DamageCameraShakeChannel);

            cameraManager.PlayShake(request);
        }

        public void EnableSuperArmor(bool enable)
        {
            _controllerMonsterSuperArmor.EnableSuperArmor(enable);
        }
        /// <summary>
        /// 슈퍼 아머가 0이 되었을 때 한번 호출 
        /// </summary>
        /// <param name="hitReactionType"></param>
        private void OnSuperArmorBreak(CharacterConstants.HitReactionType hitReactionType)
        {
            // GcLogger.LogError($"그로기 상태");
            if (_monsterGroggyAffectUid <= 0)
            {
                GcLogger.LogError($"{nameof(GGemCoMonsterSettings)}에 monsterGroggyAffectUid, monsterGroggyAffectDuration 값을 설정해주세요.");
                return;
            }
            _characterBase.AddAffect(_monsterGroggyAffectUid, _monsterGroggyAffectDuration);
        }
        
        private void NotifyIncomingHitActionCancelers(IncomingHitCancelReason reason)
        {
            if (_characterBase == null)
                return;

            var behaviours = _characterBase.GetComponents<MonoBehaviour>();
            if (behaviours == null || behaviours.Length == 0)
                return;

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitActionCanceler canceler)
                {
                    canceler.CancelActionsOnIncomingHit(reason);
                }
            }
        }

        /// <summary>
        /// 피격 계산으로 도출된 최종 HP에 대해 외부 보정기를 순차 적용합니다.
        /// </summary>
        /// <param name="metadataDamage">현재 피격 메타데이터입니다.</param>
        /// <param name="proposedHp">Core 계산 기준 최종 HP입니다.</param>
        /// <returns>보정이 반영된 최종 HP입니다.</returns>
        private long ResolveFinalHpOnIncomingHit(MetadataDamage metadataDamage, long proposedHp)
        {
            if (_characterBase == null)
                return proposedHp;

            long resolvedHp = proposedHp;
            var behaviours = _characterBase.GetComponents<MonoBehaviour>();
            if (behaviours == null || behaviours.Length == 0)
                return resolvedHp;

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IIncomingHitFinalHpResolver resolver)
                {
                    resolvedHp = resolver.ResolveFinalHpOnIncomingHit(resolvedHp, metadataDamage);
                }
            }

            return resolvedHp;
        }
    }
}
