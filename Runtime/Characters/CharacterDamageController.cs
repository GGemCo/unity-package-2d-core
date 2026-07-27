using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 피격 요청에 필요한 데미지, 연출, 가드, 후속 처리 정보를 전달합니다.
    /// </summary>
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

        /// <summary>
        /// 데미지 요청 큐에 보관할 수 있도록 현재 메타데이터의 독립 복사본을 생성합니다.
        /// </summary>
        /// <returns>참조형 하위 데이터가 복제된 데미지 메타데이터입니다.</returns>
        /// <remarks>
        /// 데미지 처리 중에는 최종 데미지, 대표 데미지 타입, Crowd Control 목록 등이 갱신됩니다.
        /// 큐에 원본 참조를 저장하면 호출자가 가진 인스턴스나 중첩 데미지 요청이 서로 영향을 줄 수 있으므로,
        /// 데미지 처리 진입 시점의 스냅샷을 사용합니다.
        /// </remarks>
        public MetadataDamage Clone()
        {
            return new MetadataDamage
            {
                damage = damage,
                attacker = attacker,
                damageType = damageType,
                DamageBreakdown = DamageBreakdown != null ? DamageBreakdown.Clone() : null,
                IncludeAttackerElementDamageParts = IncludeAttackerElementDamageParts,
                IsDamageOverTime = IsDamageOverTime,
                affectUid = affectUid,
                crowdControlUid = crowdControlUid,
                StaggerStackDamage = StaggerStackDamage,
                HitReactionType = HitReactionType,
                ForceHitReaction = ForceHitReaction,
                AttackId = AttackId,
                SkillUid = SkillUid,
                SkillHitMpGain = SkillHitMpGain,
                AllowMultipleSkillHitMpGainPerAttack = AllowMultipleSkillHitMpGainPerAttack,
                SuppressDamageReaction = SuppressDamageReaction,
                SuppressHitEffect = SuppressHitEffect,
                HasPendingAfterDamageCrowdControl = HasPendingAfterDamageCrowdControl,
                DamageCameraShakePreset = DamageCameraShakePreset,
                DamageCameraShakeDirectionSource = DamageCameraShakeDirectionSource,
                DamageCameraShakeFixedDirection = DamageCameraShakeFixedDirection,
                DamageCameraShakeHorizontalOnly = DamageCameraShakeHorizontalOnly,
                DamageCameraShakeChannel = DamageCameraShakeChannel,
                SourceAffectUid = SourceAffectUid,
                DeathPresentation = DeathPresentation != null ? DeathPresentation.Clone() : null,
                HasAttackHitStopSettings = HasAttackHitStopSettings,
                AttackHitStopSettings = AttackHitStopSettings,
                IsBasicAttackCombo = IsBasicAttackCombo,
                BasicAttackComboIndex = BasicAttackComboIndex,
                BasicAttackComboCount = BasicAttackComboCount,
                IsLastBasicAttackCombo = IsLastBasicAttackCombo,
                GuardInteractionMode = GuardInteractionMode,
                GuardAttackType = GuardAttackType,
                GuardBreakJustGuardPolicy = GuardBreakJustGuardPolicy,
                GuardBreakDamageMultiplier = GuardBreakDamageMultiplier,
                GuardBreakStaminaCost = GuardBreakStaminaCost,
                GuardBreakVfxUid = GuardBreakVfxUid,
                GuardBreakFeedbackText = GuardBreakFeedbackText,
                ResolvedOnHitCrowdControls = ResolvedOnHitCrowdControls != null
                    ? new List<int>(ResolvedOnHitCrowdControls)
                    : null,
            };
        }
    }
    /// <summary>
    /// 캐릭터 피격 파이프라인의 실행 순서를 조정하는 오케스트레이터입니다.
    /// </summary>
    public class CharacterDamageController
    {
        private readonly DamageRequestQueue _damageRequestQueue = new DamageRequestQueue();
        private readonly CharacterIncomingHitVfxController _incomingHitVfxController =
            new CharacterIncomingHitVfxController();

        private CharacterBase _characterBase;
        private ControllerMonsterSuperArmor _controllerMonsterSuperArmor;
        private float _monsterGroggyAffectDuration;
        private int _monsterGroggyAffectUid;
        
        private Color _textColorDamageMonster;
        private Color _textColorDamagePlayer;
        
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
            _controllerMonsterSuperArmor = new ControllerMonsterSuperArmor();
            _controllerMonsterSuperArmor.Initialize(_characterBase, monsterSettings);
            
            _controllerMonsterSuperArmor.BreakTriggered += OnSuperArmorBreak;
            _controllerMonsterSuperArmor.RestoredToMax += OnSuperArmorRestoredToMax;

            if (monsterSettings)
            {
                _monsterGroggyAffectDuration = monsterSettings.monsterGroggyAffectDuration;
                _monsterGroggyAffectUid = monsterSettings.monsterGroggyAffectUid;
            }

            if (loaderSettings != null && loaderSettings.settings)
            {
                _textColorDamageMonster = loaderSettings.settings.textColorDamageMonster;
                _textColorDamagePlayer = loaderSettings.settings.textColorDamagePlayer;
            }

            _incomingHitVfxController.Initialize(
                _characterBase,
                loaderSettings != null ? loaderSettings.playerSettings : null,
                monsterSettings);
            _damageRequestQueue.Initialize(_characterBase.name, ProcessDamageNow);
        }

        /// <summary>
        /// 이벤트 구독과 런타임 요청 상태를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            _damageRequestQueue.Clear();
            _incomingHitVfxController.ResetRuntimeState();

            if (_controllerMonsterSuperArmor != null)
            {
                _controllerMonsterSuperArmor.BreakTriggered -= OnSuperArmorBreak;
                _controllerMonsterSuperArmor.RestoredToMax -= OnSuperArmorRestoredToMax;
                _controllerMonsterSuperArmor.Dispose();
                _controllerMonsterSuperArmor = null;
            }
        }

        /// <summary>
        /// 데미지 처리에 연결된 시간 기반 런타임 상태를 갱신합니다.
        /// </summary>
        /// <param name="now">현재 스케일 적용 게임 시간입니다.</param>
        public void Tick(float now)
        {
            _controllerMonsterSuperArmor?.Tick(now);
        }

        /// <summary>
        /// 사망이나 풀 반환 전에 예약된 슈퍼아머 최대 복구를 취소합니다.
        /// </summary>
        internal void CancelPendingSuperArmorRestore()
        {
            _controllerMonsterSuperArmor?.CancelPendingRestoreToMax();
        }

        /// <summary>
        /// 외부 전투 정책에서 전달한 요청으로 현재 캐릭터의 슈퍼아머를 차감합니다.
        /// </summary>
        /// <param name="request">적용할 슈퍼아머 차감 요청입니다.</param>
        /// <param name="result">실제 차감 및 브레이크 처리 결과입니다.</param>
        /// <returns>슈퍼아머가 실제로 차감되었으면 <see langword="true"/>입니다.</returns>
        public bool TryApplySuperArmorDamage(
            in SuperArmorDamageRequest request,
            out SuperArmorDamageResult result)
        {
            result = SuperArmorDamageResult.None;
            if (_controllerMonsterSuperArmor == null || !_characterBase || !_characterBase.IsMonster())
            {
                return false;
            }

            return _controllerMonsterSuperArmor.TryApplySuperArmorDamage(in request, out result);
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

        /// <summary>
        /// 데미지 요청을 큐에 등록하고 현재 처리 중이 아니면 순차 처리합니다.
        /// </summary>
        /// <param name="metadataDamage">처리할 데미지 메타데이터입니다.</param>
        /// <remarks>
        /// 피격 처리 중 Affect OnHit, 속성 게이지 반복 입력, 반사 데미지처럼 다시 데미지가 발생할 수 있습니다.
        /// 중첩 호출을 즉시 처리하면 바깥쪽 피격 처리에서 미리 계산한 HP가 안쪽 데미지 결과를 덮어쓸 수 있으므로,
        /// 재진입 요청은 큐에 보관한 뒤 현재 요청의 최종 HP 반영이 끝난 다음 순서대로 처리합니다.
        /// </remarks>
        public void TakeDamage(MetadataDamage metadataDamage)
        {
            _damageRequestQueue.EnqueueAndDrain(metadataDamage);
        }

        /// <summary>
        /// 큐에서 꺼낸 단일 데미지 요청을 실제 피격 파이프라인으로 처리합니다.
        /// </summary>
        /// <param name="metadataDamage">큐에서 꺼낸 데미지 메타데이터 스냅샷입니다.</param>
        private void ProcessDamageNow(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null) return;
            _incomingHitVfxController.BeginDamageRequest();

            if (SceneGame.Instance.CutsceneManager.IsPlaying()) return;
            if (_characterBase.IsStatusDead() || _characterBase.IsDeathPending)
            {
                // 사망 전 액션이 진행 중이면 추가 피격으로 사망 플로우가 중복 실행되지 않도록 막습니다.
                return;
            }

            if (!_characterBase.CanReceiveDamage(metadataDamage))
            {
                CombatHitFeedbackNotifier.NotifyIncoming(
                    _characterBase,
                    metadataDamage,
                    MonsterSkillCombatOutcome.Immune);
                CombatHitFeedbackNotifier.NotifyOutgoing(
                    _characterBase,
                    metadataDamage,
                    MonsterSkillCombatOutcome.Immune);
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

                if (damage <= 0L &&
                    CharacterDamageCalculationUtility.HasAnyImmuneDamagePart(incomingBreakdown))
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
                    CombatHitFeedbackNotifier.NotifyIncoming(
                        _characterBase,
                        metadataDamage,
                        MonsterSkillCombatOutcome.Immune);
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
                    IncomingHitExtensionResolver.NotifyActionCancelers(
                        _characterBase,
                        IncomingHitCancelReason.Damage);
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

            if (guardResolver != null &&
                CharacterDamageCalculationUtility.ShouldEvaluateGuardResolution(metadataDamage))
            {
                metadataDamage.damage = damage;
                if (guardResolver.TryResolveIncomingHit(metadataDamage, out var guardResult) && guardResult.IsResolved)
                {
                    damage = guardResult.RemainingDamage < 0 ? 0 : guardResult.RemainingDamage;
                    metadataDamage.damage = damage;
                    metadataDamage.DamageBreakdown =
                        CharacterDamageCalculationUtility.ScaleFinalDamage(
                            metadataDamage.DamageBreakdown,
                            damage);
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

                    CombatHitFeedbackNotifier.NotifyIncoming(
                        _characterBase,
                        metadataDamage,
                        ResolveCombatOutcomeByGuardResult(guardResult));
                    CombatHapticFeedbackRouter.NotifyGuardResolved(
                        _characterBase,
                        in guardResult);

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

            // 외부 시스템의 치명타 보호를 먼저 검사한 뒤 보스 페이즈 같은 최종 HP 보정을 적용합니다.
            long adjustedHp = IncomingHitExtensionResolver.ResolveFinalHp(
                _characterBase,
                metadataDamage,
                remainHp,
                out bool wasLethalProtected,
                out IncomingHitLethalProtectionResult lethalProtectionResult);
            if (wasLethalProtected)
            {
                // 잘못된 외부 구현이 회복이나 사망을 유발하지 않도록 현재 HP와 1 사이로 제한합니다.
                long currentHp = Math.Max(1L, _characterBase.CurrentHp.Value);
                adjustedHp = Math.Clamp(adjustedHp, 1L, currentHp);
            }

            bool isHpAdjusted = adjustedHp != remainHp;
            remainHp = adjustedHp;

            // 보정 결과가 현재 HP와 같거나 더 크면 이번 피격은 흡수된 것으로 간주하고 종료합니다.
            // (연출/상태 반응 중복을 막기 위해 즉시 반환)
            // 치명타 보호는 공격 성공 피드백을 유지해야 하므로 이 조기 반환에서 제외합니다.
            if (!wasLethalProtected &&
                isHpAdjusted &&
                remainHp >= _characterBase.CurrentHp.Value)
            {
                _characterBase.CurrentHp.OnNext(remainHp);
                return;
            }

            // 타격 확정: 즉시 타격에 한해 공격자 OnHit와 속성 게이지 누적력을 처리합니다.
            if (attacker != null &&
                CharacterDamageCalculationUtility.ShouldProcessConfirmedAttackHit(metadataDamage))
            {
                CharacterBase attackerCharacter = attacker.GetComponentInParent<CharacterBase>();
                ElementGaugeOnHitApplier.Apply(attackerCharacter, _characterBase, metadataDamage);
                AffectRuntimeBridge.NotifyOnHit(attacker, _characterBase.gameObject);
            }

            CombatHapticFeedbackRouter.NotifyMonsterHitConfirmed(
                _characterBase,
                metadataDamage);
            CombatHitFeedbackNotifier.NotifyIncoming(
                _characterBase,
                metadataDamage,
                MonsterSkillCombatOutcome.Hit);
            CombatHitFeedbackNotifier.NotifyOutgoing(
                _characterBase,
                metadataDamage,
                MonsterSkillCombatOutcome.Hit);
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
                IncomingHitExtensionResolver.NotifyActionCancelers(
                    _characterBase,
                    IncomingHitCancelReason.Death);

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
                
                bool suppressActionCancelByLethalProtection =
                    wasLethalProtected && lethalProtectionResult.SuppressActionCancel;
                bool suppressDamageReactionByLethalProtection =
                    wasLethalProtected && lethalProtectionResult.SuppressDamageReaction;
                bool shouldPlayDamageReaction =
                    !suppressHitReactionByGuard &&
                    !metadataDamage.SuppressDamageReaction &&
                    !suppressDamageReactionByLethalProtection;
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
                    if (!suppressActionCancelByLethalProtection)
                    {
                        IncomingHitExtensionResolver.NotifyActionCancelers(
                            _characterBase,
                            IncomingHitCancelReason.Damage);
                    }

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
                        _incomingHitVfxController.SetSuppressNextAnimationEventVfx(
                            metadataDamage.SuppressHitEffect);
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

            AttackHitStopProcessor.Apply(_characterBase, metadataDamage);
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
            _incomingHitVfxController.TryPlay(IncomingHitVfxTriggerType.OnDamageConfirmed);
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
            _incomingHitVfxController.TryPlay(triggerType);
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
            _incomingHitVfxController.TryPlay(triggerType);
        }

        /// <summary>
        /// 확정 데미지에 설정된 방향성 카메라 흔들림을 재생합니다.
        /// </summary>
        /// <param name="metadataDamage">카메라 흔들림 설정과 공격자 정보를 가진 데미지 메타데이터입니다.</param>
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

        /// <summary>
        /// 현재 캐릭터의 슈퍼아머 활성 상태를 변경합니다.
        /// </summary>
        /// <param name="enable">슈퍼아머를 활성화하려면 <see langword="true"/>입니다.</param>
        public void EnableSuperArmor(bool enable)
        {
            _controllerMonsterSuperArmor.EnableSuperArmor(enable);
        }

        /// <summary>
        /// 컨트롤러에서 발생한 슈퍼아머 최대 복구 이벤트를 캐릭터 공개 이벤트로 전달합니다.
        /// </summary>
        /// <param name="currentValue">복구된 현재 슈퍼아머 값입니다.</param>
        /// <param name="maxValue">복구 시점의 최대 슈퍼아머 값입니다.</param>
        private void OnSuperArmorRestoredToMax(int currentValue, int maxValue)
        {
            if (!_characterBase) return;
            _characterBase.NotifySuperArmorRestoredToMax(currentValue, maxValue);
        }

        /// <summary>
        /// 슈퍼아머가 소진되었을 때 그로기 Affect를 적용합니다.
        /// </summary>
        /// <param name="hitReactionType">슈퍼아머를 소진시킨 피격 리액션 타입입니다.</param>
        private void OnSuperArmorBreak(CharacterConstants.HitReactionType hitReactionType)
        {
            // GcLogger.LogError($"그로기 상태");
            if (_monsterGroggyAffectUid <= 0 ||
                _monsterGroggyAffectDuration <= 0f ||
                float.IsNaN(_monsterGroggyAffectDuration) ||
                float.IsInfinity(_monsterGroggyAffectDuration))
            {
                GcLogger.LogError($"{nameof(GGemCoMonsterSettings)}에 monsterGroggyAffectUid, monsterGroggyAffectDuration 값을 설정해주세요.");
                return;
            }
            _characterBase.AddAffect(_monsterGroggyAffectUid, _monsterGroggyAffectDuration);
        }
        
    }
}
