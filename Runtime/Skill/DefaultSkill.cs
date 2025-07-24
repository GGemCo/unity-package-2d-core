using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 컨트롤러에서 생성한 스킬
    /// 데미지
    ///     즉시 적용하는 데미지 처리
    ///     tick time 당 데미지 처리
    /// 어펙트
    ///     데미지 받으면 발동
    ///     즉시 발동
    ///     어펙트는 개별로 작동하기 때문에, 자체적으로 데미지 처리 
    /// 이펙트
    ///     이펙트는 지정된 시간동안 보여주는 기능만. 오로지 그래픽적 기능만. 
    ///     타겟에 생성
    ///     범위에 생성
    ///     데미지 콜백 필요
    /// </summary>
    public class DefaultSkill : MonoBehaviour
    {
        // 사용하는 캐릭터
        private CharacterBase _attacker;
        // 스킬 적용 대상
        private CharacterBase _target;

        private PolygonCollider2D _polyCollider2D;
        private CapsuleCollider2D _capsuleCollider2D;
        private Vector3 _direction;

        private StruckTableSkill _struckTableSkill;
        private TableEffect _tableEffect;

        public void Initialize(CharacterBase character, int skillUid, int skillLevel)
        {
            _attacker = character;
            _struckTableSkill = TableLoaderManager.Instance.TableSkill.GetDataByUidLevel(skillUid, skillLevel);
            _tableEffect = TableLoaderManager.Instance.TableEffect;

            if (_struckTableSkill.Duration > 0)
                StartCoroutine(RemoveEffectDuration(_struckTableSkill.Duration));
            else if (_struckTableSkill.CoolTime > 0)
                StartCoroutine(RemoveEffectDuration(_struckTableSkill.CoolTime));
            
            ComponentController.AddRigidbody2D(gameObject);
        }

        private void Start()
        {
            if (!TryInitializeTarget()) return;

            ApplyVisualEffect();
            ApplyProjectile();
            ApplyInitialAffect();
            ApplySkillCost();

            if (_struckTableSkill.TargetType == SkillConstants.TargetType.Range)
                StartCoroutine(AffectByTickTimeOnce());
        }

        private void ApplyProjectile()
        {
            if (_struckTableSkill.ProjectileUid <= 0) return;
            var info = TableLoaderManager.Instance.TableProjectile.GetDataByUid(_struckTableSkill.ProjectileUid);
            if (info == null) return;
            StartCoroutine(CreateProjectile(info));
        }

        private IEnumerator CreateProjectile(StruckTableProjectile info)
        {
            if (!_target || info == null) yield break;
            
            for (int i = 0; i < info.Count; i++)
            {
                DefaultProjectile projectile = ProjectileManager.CreateProjectile(info.Uid);
                projectile?.SetFromCharacter(_attacker);
                projectile?.SetDamage(_struckTableSkill.DamageValue);
                float positionX =
                    Random.Range(_target.transform.position.x - info.TargetPositionRangeX,
                        _target.transform.position.x + info.TargetPositionRangeX);
                float positionY = _target.GetRandomPositionYInHitArea();
                if (info.TargetType == ProjectileConstants.TargetType.Fixed)
                {
                    projectile?.Launch(_target);
                }
                else
                {
                    // 직선형일때는 타겟 x 좌표를 범위로 하지 않는다. 
                    if (info.ArcHeightMin == 0 && info.ArcHeightMax == 0)
                    {
                        positionX = _target.transform.position.x;
                    }

                    // positionY = mapSettings.projectilePositionY;
                    projectile?.Launch(new Vector2(positionX, positionY));
                }
                yield return new WaitForSeconds(info.SecDelayByOne);
            }
        }

        /// <summary>
        /// 타겟 지정하기
        /// </summary>
        /// <returns></returns>
        private bool TryInitializeTarget()
        {
            if (_struckTableSkill.Target == SkillConstants.Target.Player)
            {
                _target = SceneGame.Instance.player.GetComponent<CharacterBase>();
            }
            else if (_struckTableSkill.Target == SkillConstants.Target.Monster)
            {
                _target = SceneGame.Instance.mapManager.GetNearByMonsterDistance(_struckTableSkill.Distance);
            }

            if (_target == null)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("Skill_NoTarget");//"타겟이 없습니다."
                DestroySkill();
                return false;
            }
            if (_target.IsStatusDead())
            {
                DestroySkill();
                return false;
            }

            return true;
        }
        /// <summary>
        /// 마력 사용하기
        /// </summary>
        private void ApplySkillCost()
        {
            _attacker.MinusMp(_struckTableSkill.NeedMp);
        }
        /// <summary>
        /// 이펙트 표현하기
        /// </summary>
        private void ApplyVisualEffect()
        {
            if (_struckTableSkill.EffectUid <= 0) return;

            if (_struckTableSkill.TargetType == SkillConstants.TargetType.Range && _struckTableSkill.DamageRange > 0)
            {
                SpawnRangeEffect(_target.transform.position);
            }
            else
            {
                var effect = EffectManager.CreateEffect(_struckTableSkill.EffectUid);
                if (effect == null) return;
                var effectInfo = _tableEffect.GetDataByUid(_struckTableSkill.EffectUid);
                if (effectInfo == null) return;
                float effectScale = _struckTableSkill.EffectScale > 0 ? _struckTableSkill.EffectScale : 1;
                effect.SetScale(effectScale);
                if (_struckTableSkill.Duration > 0)
                {
                    if (_struckTableSkill.Duration > _struckTableSkill.CoolTime)
                    {
                        GcLogger.LogWarning($"Skill Uid: {_struckTableSkill.Uid}, Level: {_struckTableSkill.Level}, Name: {_struckTableSkill.Name}. Duration: {_struckTableSkill.Duration} > CoolTime: {_struckTableSkill.CoolTime}. ");
                    }
                    effect.SetDuration(_struckTableSkill.Duration);
                }

                effect.transform.position = _target.transform.position;
                transform.position = _target.transform.position;
            }
        }
        /// <summary>
        /// 범위 이펙트 표현하기
        /// </summary>
        /// <param name="targetPos"></param>
        private void SpawnRangeEffect(Vector3 targetPos)
        {
            var effectInfo = _tableEffect.GetDataByUid(_struckTableSkill.EffectUid);
            if (effectInfo == null) return;

            float effectScale = _struckTableSkill.EffectScale > 0 ? _struckTableSkill.EffectScale : 1;
            float effectSize = effectInfo.Width * effectScale;
            float radiusX = _struckTableSkill.DamageRange;
            float radiusY = radiusX / 2f;

            float currentRadiusX = effectSize;
            float currentRadiusY = effectSize * (radiusY / radiusX);

            while (currentRadiusX <= radiusX)
            {
                int count = Mathf.RoundToInt((2 * Mathf.PI * currentRadiusX) / effectSize);

                for (int i = 0; i < count; i++)
                {
                    float angle = (i / (float)count) * 360f;
                    float radian = angle * Mathf.Deg2Rad;

                    float posX = Mathf.Cos(radian) * currentRadiusX + Random.Range(-10, 10);
                    float posY = Mathf.Sin(radian) * currentRadiusY + Random.Range(-10, 10);

                    Vector3 spawnPosition = targetPos + new Vector3(posX, posY, 0);

                    var effect = EffectManager.CreateEffect(_struckTableSkill.EffectUid);
                    effect.SetScale(effectScale);
                    effect.SetDuration(_struckTableSkill.Duration);
                    effect.transform.position = spawnPosition;
                }

                currentRadiusX += effectSize;
                currentRadiusY += effectSize * (radiusY / radiusX);
            }

            // 콜라이더 설정
            _polyCollider2D = ComponentController.AddPolygonCollider2D(gameObject, true, Vector2.zero, CreateEllipsePoints(radiusX - 10f, radiusY - 10f, 20));
            if (_struckTableSkill.DamageValue > 0 && _struckTableSkill.TickTime > 0)
                StartCoroutine(DamageByTickTime());

            transform.position = targetPos;
        }
        /// <summary>
        /// 어펙트 효과 적용하기
        /// </summary>
        private void ApplyInitialAffect()
        {
            if (_struckTableSkill.TargetType == SkillConstants.TargetType.Fixed && _struckTableSkill.AffectUid > 0)
            {
                _target.AddAffect(_struckTableSkill.AffectUid);
            }
        }
        /// <summary>
        /// 타원 충돌 체크 point 만들기
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="vertexCount"></param>
        /// <returns></returns>
        private Vector2[] CreateEllipsePoints(float a, float b, int vertexCount)
        {
            Vector2[] points = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float angle = (i / (float)vertexCount) * Mathf.PI * 2;
                points[i] = new Vector2(Mathf.Cos(angle) * a, Mathf.Sin(angle) * b);
            }
            return points;
        }
        /// <summary>
        /// 타원형 충돌 범위에 있는 몬스터 찾기
        /// </summary>
        /// <returns></returns>
        private List<CharacterBase> GetMonsterInCollider()
        {
            List<CharacterBase> list = new List<CharacterBase>();
            if (_polyCollider2D == null) return list;

            ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
            Collider2D[] results = new Collider2D[100];
            int count = Physics2D.OverlapCollider(_polyCollider2D, filter, results);

            for (int i = 0; i < count; i++)
            {
                CharacterHitArea area = results[i].GetComponent<CharacterHitArea>();
                if (area && results[i].CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster)))
                    list.Add(area.target);
            }

            return list;
        }
        /// <summary>
        /// tick time 마다 데미지 주기
        /// </summary>
        /// <returns></returns>
        private IEnumerator DamageByTickTime()
        {
            yield return null;
            while (true)
            {
                foreach (var character in GetMonsterInCollider())
                {
                    character.TakeDamage(_struckTableSkill.DamageValue, _attacker.gameObject, _struckTableSkill.DamageType);
                }
                yield return new WaitForSeconds(_struckTableSkill.TickTime);
            }
        }
        /// <summary>
        /// tick time 후 어펙트 적용하기
        /// </summary>
        /// <returns></returns>
        private IEnumerator AffectByTickTimeOnce()
        {
            yield return null;
            foreach (var character in GetMonsterInCollider())
            {
                if (_struckTableSkill.AffectUid > 0)
                    character.AddAffect(_struckTableSkill.AffectUid);
            }
        }
        /// <summary>
        /// 스킬 duration 종료 후 처리 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private IEnumerator RemoveEffectDuration(float time)
        {
            yield return new WaitForSeconds(time);
            DestroySkill();
        }
        /// <summary>
        /// 프로젝타일이 타겟과 충돌했는지 체크 
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) return;
            if (_struckTableSkill.TickTime > 0) return;

            CharacterHitArea area = collision.GetComponent<CharacterHitArea>();
            if (area == null || area.target != _target) return;

            area.target.TakeDamage(_struckTableSkill.DamageValue, _attacker.gameObject, _struckTableSkill.DamageType);

            if (_struckTableSkill.Duration <= 0)
            {
                _target = null;
            }
        }
        /// <summary>
        /// 스킬 destroy 처리
        /// </summary>
        private void DestroySkill()
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}
