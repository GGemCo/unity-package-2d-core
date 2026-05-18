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

        public List<int> ResolvedOnHitCrowdControls;
    }
    /// <summary>
    /// 캐릭터 데미지 처리
    /// </summary>
    public class CharacterDamageController
    {
        private CharacterBase _characterBase;
        private ControllerMonsterSuperArmor _controllerMonsterSuperArmor;
        private float _monsterGroggyAffectDuration;
        private int _monsterGroggyAffectUid;
        
        private Color _textColorDamageMonster;
        private Color _textColorDamagePlayer;
        private Color _textColorHeal;
        private GGemCoPlayerSettings _playerSettings;
        private float _nextPlayerHitVfxPlayableTime;
        
        public void Initialize(CharacterBase characterBase)
        {
            _characterBase = characterBase;
            if (!_characterBase)
            {
                GcLogger.LogError($"CharacterBase가 없습니다.");
                return;
            }
            _controllerMonsterSuperArmor = new ControllerMonsterSuperArmor();
            _controllerMonsterSuperArmor.Initialize(_characterBase);
            
            _controllerMonsterSuperArmor.BreakTriggered += OnSuperArmorBreak;

            if (AddressableLoaderSettings.Instance.monsterSettings)
            {
                _monsterGroggyAffectDuration = AddressableLoaderSettings.Instance.monsterSettings.monsterGroggyAffectDuration;
                _monsterGroggyAffectUid = AddressableLoaderSettings.Instance.monsterSettings.monsterGroggyAffectUid;
            }

            if (AddressableLoaderSettings.Instance.settings)
            {
                _textColorDamageMonster = AddressableLoaderSettings.Instance.settings.textColorDamageMonster;
                _textColorDamagePlayer = AddressableLoaderSettings.Instance.settings.textColorDamagePlayer;
                _textColorHeal = AddressableLoaderSettings.Instance.settings.textColorHeal;
            }

            _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            _nextPlayerHitVfxPlayableTime = 0f;
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
            var guardResolver = _characterBase.GetComponent<IIncomingHitGuardResolver>();

            if (guardResolver != null)
            {
                metadataDamage.damage = damage;
                if (guardResolver.TryResolveIncomingHit(metadataDamage, out var guardResult) && guardResult.IsResolved)
                {
                    damage = guardResult.RemainingDamage < 0 ? 0 : guardResult.RemainingDamage;
                    suppressHitReactionByGuard = guardResult.SuppressHitReaction;

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
                        guardResult.IsJustGuard ? MonsterSkillCombatOutcome.JustGuarded : MonsterSkillCombatOutcome.Guarded);
                }
            }

            if (damage <= 0)
            {
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
            TryPlayPlayerIncomingHitVfx();
            
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
            _characterBase.ApplyCrowdControlSequence(resolvedOnHitCrowdControls, metadataDamage.attacker, isEndCharacterStop);
            
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
        /// 플레이어 피격 확정 시 설정된 VFX를 재생합니다.
        /// </summary>
        /// <remarks>
        /// 실제 데미지가 0보다 큰 타격에만 호출되며, 설정된 최소 간격을 만족할 때만 재생합니다.
        /// </remarks>
        private void TryPlayPlayerIncomingHitVfx()
        {
            if (!(_characterBase is Player))
            {
                return;
            }

            if (_playerSettings == null && AddressableLoaderSettings.Instance != null)
            {
                _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            }

            if (_playerSettings == null)
            {
                return;
            }

            GGemCoPlayerSettings.IncomingHitVfxSettings settings = _playerSettings.incomingHitVfx;
            if (!settings.enabled || settings.vfxUid <= 0)
            {
                return;
            }

            if (settings.minIntervalSeconds > 0f && Time.time < _nextPlayerHitVfxPlayableTime)
            {
                return;
            }

            SceneGame scene = SceneGame.Instance;
            if (scene == null || scene.VfxManager == null)
            {
                return;
            }

            var spawnRequest = new VfxSpawnRequest
            {
                VfxUid = settings.vfxUid,
                Owner = _characterBase,
                Target = _characterBase,
                FollowTarget = settings.followTarget ? _characterBase : null,
                WorldPosition = _characterBase.transform.position,
                PositionOffset = settings.positionOffset,
                PositionYType = settings.positionYType,
                ScaleOverride = settings.scaleOverride,
                DurationOverride = settings.durationOverride,
                ForceOneShot = !settings.followTarget
            };

            if (settings.hasSortingLayerOverride)
            {
                spawnRequest.SortingLayerOverride = settings.sortingLayerKey;
            }

            if (settings.hasSortingOrderOverride)
            {
                spawnRequest.SortingOrderOverride = settings.sortingOrder;
            }

            scene.VfxManager.CreateVfx(spawnRequest);

            if (settings.minIntervalSeconds > 0f)
            {
                _nextPlayerHitVfxPlayableTime = Time.time + settings.minIntervalSeconds;
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
    }
}
