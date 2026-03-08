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
        }

        public void Dispose()
        {
            _controllerMonsterSuperArmor.BreakTriggered -= OnSuperArmorBreak;
        }

        public void TakeDamage(MetadataDamage metadataDamage)
        {
            if (SceneGame.Instance.CutsceneManager.IsPlaying()) return;
            if (_characterBase.IsStatusDead())
            {
                // GcLogger.Log("monster dead");
                return;
            }

            long damage = metadataDamage.damage;
            if (damage <= 0) return;
            ConfigCommon.DamageType damageType = metadataDamage.damageType;
            GameObject attacker = metadataDamage.attacker;
            int affectUid = metadataDamage.affectUid;
            int crowdControlUid = metadataDamage.crowdControlUid;

            // 데미지 텍스트 색상 설정
            Color damageTextColor = Color.white;
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
                }
            }
            if (damage <= 0) return;

            // Item Bonus HP(소모형 추가 최대 HP)부터 먼저 차감
            //  - 0이 되면 즉시 소멸(외부에서 UI/저장 갱신 처리)
            long remainingDamage = _characterBase.ConsumeHpTempItem(damage);
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

            if (_characterBase.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                damageTextColor = Color.red;
            }
            MetadataDamageText metadataDamageText2 = new MetadataDamageText
            {
                Damage = damage,
                Color = damageTextColor,
                WorldPosition = damageTextPosition
            };
            SceneGame.Instance.damageTextManager.ShowDamageText(metadataDamageText2);
            
            if (remainHp <= 0)
            {
                // 사망했을 때, UI 표현을 위해 0으로 처리
                remainHp = 0;
                _characterBase.CurrentMp.OnNext(0);
                _characterBase.Dead(CharacterConstants.DieReasonType.Battle, attacker);
            }
            else
            {
                bool shouldPlayDamageReaction = true;
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
                    if (hitReactionType == CharacterConstants.HitReactionType.Flinch)
                    {
                        _characterBase.CharacterAnimationController.PlayAnimationGroggy();
                    }
                    // CC 처리가 있을 때 
                    else if (crowdControlUid > 0)
                    {
                        _characterBase.ApplyCrowdControl(crowdControlUid, metadataDamage.attacker);
                    }
                    else
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
            _characterBase.CurrentHp.OnNext(remainHp);
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
    }
}