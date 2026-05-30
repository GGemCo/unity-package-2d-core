using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class MetadataDamage
    {
        public long damage;
        public GameObject attacker;
        public ConfigCommon.DamageType damageType;
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
        public DirectionalCameraShakeMode DamageCameraShakeDirectionMode = DirectionalCameraShakeMode.PresetRaw;

        /// <summary>
        /// 이번 타격이 추가로 누적할 속성 게이지 목록입니다.
        /// </summary>
        public ElementGaugeApplication[] ElementGaugeApplications;

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

        public void TakeDamage(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null) return;
            if (SceneGame.Instance.CutsceneManager.IsPlaying()) return;
            if (_characterBase.IsStatusDead() || _characterBase.IsDeathPending)
            {
                // 사망 전 액션이 진행 중이면 추가 피격으로 사망 플로우가 중복 실행되지 않도록 막습니다.
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
            // 속성 데미지일때, 저항값 처리
            if (damageType != ConfigCommon.DamageType.None)
            {
                if (damageType == ConfigCommon.DamageType.Fire)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistFire.Value) / 100f));
                    damageTextColor = Color.red;
                }
                else if (damageType == ConfigCommon.DamageType.Cold)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistCold.Value) / 100f));
                    damageTextColor = Color.blue;
                }
                else if (damageType == ConfigCommon.DamageType.Lightning)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistLightning.Value) / 100f));
                    damageTextColor = Color.yellow;
                }
                else if (damageType == ConfigCommon.DamageType.Poison)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistPoison.Value) / 100f));
                    damageTextColor = Color.green;
                }

                if (damage <= 0)
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
            bool hasGuardFeedbackText = false;
            bool isGuardResolved = false;
            bool overrideAfterDamageCrowdControlByGuard = false;
            List<CrowdControlRuntimeData> guardCrowdControlRuntimeList = null;
            var guardResolver = _characterBase.GetComponent<IIncomingHitGuardResolver>();

            if (guardResolver != null)
            {
                metadataDamage.damage = damage;
                if (guardResolver.TryResolveIncomingHit(metadataDamage, out var guardResult) && guardResult.IsResolved)
                {
                    damage = guardResult.RemainingDamage < 0 ? 0 : guardResult.RemainingDamage;
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

                    if (!string.IsNullOrEmpty(guardResult.FeedbackText))
                    {
                        MetadataDamageText guardText = new MetadataDamageText
                        {
                            Damage = 0,
                            Color = guardResult.FeedbackColor == default ? Color.cyan : guardResult.FeedbackColor,
                            SpecialDamageText = guardResult.FeedbackText,
                            WorldPosition = damageTextPosition,
                        };
                        SceneGame.Instance.damageTextManager.ShowDamageText(guardText);
                        hasGuardFeedbackText = true;
                    }

                    NotifyIncomingHitCombatFeedback(
                        metadataDamage,
                        ResolveCombatOutcomeByGuardResult(guardResult));
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
            TryPlayDamageCameraShake(metadataDamage);

            if (!hasGuardFeedbackText)
            {
                MetadataDamageText metadataDamageText2 = new MetadataDamageText
                {
                    Damage = damage,
                    Color = damageTextColor,
                    WorldPosition = damageTextPosition
                };
                SceneGame.Instance.damageTextManager.ShowDamageText(metadataDamageText2);
            }

            _characterBase.TryPlaySpriteWhiteOverlayOnHit();
            TryPlayIncomingHitVfxByTrigger(IncomingHitVfxTriggerType.OnDamageConfirmed);
            
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
                TryPlayPlayerHudDamageFeedback(metadataDamage);
                
                bool shouldPlayDamageReaction = !suppressHitReactionByGuard;
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
                        _characterBase.CharacterAnimationController.PlayDamageAnimation();
                    }
                }
                _characterBase.OnDamage(attacker);
                
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

            // 속성 데미지 게이지 처리
            var elementGaugeController = _characterBase.ElementGaugeController;
            if (elementGaugeController != null && metadataDamage.ElementGaugeApplications != null && metadataDamage.ElementGaugeApplications.Length > 0)
            {
                elementGaugeController.ApplyGauge(metadataDamage.ElementGaugeApplications, metadataDamage.attacker);
            }

            if (elementGaugeController != null)
            {
                elementGaugeController.HandleAfterIncomingDamage(metadataDamage);
                TryFinalizeDeathAfterElementGauge(metadataDamage);
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
        /// 속성 게이지 후처리로 HP가 0 이하가 되었을 때 사망 처리를 확정합니다.
        /// </summary>
        /// <param name="metadataDamage">사망 원인과 연출 정보를 포함한 데미지 메타데이터입니다.</param>
        /// <remarks>
        /// 일반 데미지 차감이 아니라 속성 게이지 처리에서 사망이 확정되는 경우에도
        /// 최초 데미지의 사망 연출 요청을 유지해야 전용 사망 연출이 누락되지 않습니다.
        /// </remarks>
        private void TryFinalizeDeathAfterElementGauge(MetadataDamage metadataDamage)
        {
            if (_characterBase == null || _characterBase.IsStatusDead())
                return;

            if (_characterBase.BaseHp < 0 && _characterBase.CurrentHp.Value <= 0)
            {
                _characterBase.CurrentHp.OnNext(1);
                return;
            }

            if (_characterBase.CurrentHp.Value > 0)
                return;

            NotifyIncomingHitActionCancelers(IncomingHitCancelReason.Death);
            _characterBase.CurrentMp.OnNext(0);
            _characterBase.Dead(
                CharacterConstants.DieReasonType.Battle,
                metadataDamage != null ? metadataDamage.attacker : null,
                playDeadAnimation: true,
                deathPresentation: metadataDamage != null ? metadataDamage.DeathPresentation : null);
        }


        private void TryPlayPlayerHudDamageFeedback(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
            {
                return;
            }

            if (!(_characterBase is Player player))
            {
                return;
            }

            if (metadataDamage.attacker != null)
            {
                player.PlayStaminaDamageFeedbackFromAttacker(metadataDamage.attacker.transform);
                return;
            }

            player.PlayDefaultStaminaDamageFeedback();
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

            scene.VfxManager.CreateVfx(spawnRequest);

            if (settings.minIntervalSeconds > 0f)
            {
                _nextIncomingHitVfxPlayableTimesByKey[cooldownKey] = Time.time + settings.minIntervalSeconds;
            }

            return true;
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
                metadataDamage.DamageCameraShakeDirectionMode,
                CameraShakeChannel.SkillDamage);

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
