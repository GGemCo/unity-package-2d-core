using UnityEngine;

namespace GGemCo2DCore
{
    public class MetadataDamage
    {
        public long damage;
        public GameObject attacker;
        public SkillConstants.DamageType damageType;
        // 데미지 받는 대상에 적용되는 어펙트 uid 
        public int affectUid;
    }
    /// <summary>
    /// 캐릭터 데미지 처리
    /// </summary>
    public class CharacterDamageController
    {
        private CharacterBase _characterBase;
        private float _delayDestroyMonster;
        public void Initialize(CharacterBase characterBase)
        {
            _characterBase = characterBase;
            if (!_characterBase)
            {
                GcLogger.LogError($"CharacterBase가 없습니다.");
                return;
            }
            _delayDestroyMonster = AddressableLoaderSettings.Instance.settings.delayDestroyMonster;
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
            SkillConstants.DamageType damageType = metadataDamage.damageType;
            GameObject attacker = metadataDamage.attacker;
            int affectUid = metadataDamage.affectUid;
            
            // 데미지 텍스트 색상 설정
            Color damageTextColor = Color.white;
            Vector3 damageTextPosition = _characterBase.transform.position + new Vector3(0,
                _characterBase.GetHeight() * Mathf.Abs(_characterBase.originalScaleX), 0);
            // 속성 데미지일때, 저항값 처리
            if (damageType != SkillConstants.DamageType.None)
            {
                if (damageType == SkillConstants.DamageType.Fire)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistFire.Value) / 100f));
                    damageTextColor = Color.red;
                }
                else if (damageType == SkillConstants.DamageType.Cold)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistCold.Value) / 100f));
                    damageTextColor = Color.blue;
                }
                else if (damageType == SkillConstants.DamageType.Lightning)
                {
                    damage = (long)(damage * ((100f - _characterBase.TotalRegistLightning.Value) / 100f));
                    damageTextColor = Color.yellow;
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

            long remainHp = _characterBase.CurrentHp.Value - damage;
            // -1 이면 죽지 않는다
            if (_characterBase.BaseHp < 0)
            {
                remainHp = 1;
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
                _characterBase.SetStatusDead();
                Object.Destroy(_characterBase.gameObject, _delayDestroyMonster);

                _characterBase.OnDead();
            }
            else
            {
                if (_characterBase.IsStatusKnockback())
                {
                }
                else
                {
                    // 순서 중요.
                    _characterBase.SetStatusDamage();
                    _characterBase.CharacterAnimationController.PlayDamageAnimation();
                }
                _characterBase.OnDamage(attacker);
                
                if (affectUid > 0)
                {
                    _characterBase.AddAffect(affectUid);
                }
            }
            _characterBase.CurrentHp.OnNext(remainHp);
        }
    }
}