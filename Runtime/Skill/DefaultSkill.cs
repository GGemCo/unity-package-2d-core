﻿using System.Collections;
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
        private DefaultEffect _arrowDefaultEffect;

        private StruckTableSkill _struckTableSkill;
        private TableEffect _tableEffect;

        public void Initialize(CharacterBase character, int skillUid, int skillLevel)
        {
            _attacker = character;
            _struckTableSkill = TableLoaderManager.Instance.TableSkill.GetDataByUidLevel(skillUid, skillLevel);
            _tableEffect = TableLoaderManager.Instance.TableEffect;

            if (_struckTableSkill.Duration > 0)
                StartCoroutine(RemoveEffectDuration(_struckTableSkill.Duration));

            ComponentController.AddRigidbody2D(gameObject);
        }

        private void Start()
        {
            if (!TryInitializeTarget()) return;

            ApplyVisualEffect();
            ApplyInitialAffect();
            ApplySkillCost();

            if (_struckTableSkill.TargetType == SkillConstants.TargetType.Range)
                StartCoroutine(AffectByTickTimeOnce());
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
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("There is no target.");//"타겟이 없습니다."
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
                Vector3 from = _attacker.transform.position + Vector3.up * _attacker.GetHeightByScale() / 2f;
                Vector3 to = _target.transform.position + Vector3.up * _target.GetHeightByScale() / 2f;
                SpawnProjectileEffect(from, to);
            }
        }
        /// <summary>
        /// 범위 이펙트 표현하기
        /// </summary>
        /// <param name="targetPos"></param>
        private void SpawnRangeEffect(Vector3 targetPos)
        {
            var effectInfo = _tableEffect.GetDataByUid(_struckTableSkill.EffectUid);

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
        /// 프로젝타일 생성
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void SpawnProjectileEffect(Vector3 from, Vector3 to)
        {
            _arrowDefaultEffect = EffectManager.CreateEffect(_struckTableSkill.EffectUid);
            if (_arrowDefaultEffect == null) return;
            // SetParent 보다 먼저 scale 을 바꿔야 한다.
            if (_struckTableSkill.EffectScale > 0)
            {
                _arrowDefaultEffect.SetScale(_struckTableSkill.EffectScale);
            }
            _arrowDefaultEffect.transform.SetParent(transform);

            _direction = (to - from).normalized;
            transform.position = _struckTableSkill.EffectMoveSpeed > 0 ? from : to;

            float dirX = _direction.x >= 0 ? -1 : 1;
            _arrowDefaultEffect.SetDirection(dirX);
            _arrowDefaultEffect.SetRotation(to - from, _direction);
            _arrowDefaultEffect.OnEffectDestroy += OnArrowEffectDestroy;

            var effectInfo = _tableEffect.GetDataByUid(_struckTableSkill.EffectUid);
            Vector2 size = new Vector2(effectInfo.ColliderSize.x * _arrowDefaultEffect.transform.localScale.x,
                effectInfo.ColliderSize.y * _arrowDefaultEffect.transform.localScale.y);
            _capsuleCollider2D = ComponentController.AddCapsuleCollider2D(gameObject, true, Vector2.zero, size);
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
        /// 타겟에 따른 skill 포지션 처리
        /// </summary>
        private void Update()
        {
            if (_target == null || _struckTableSkill.EffectMoveSpeed <= 0) return;

            if (_struckTableSkill.Target == SkillConstants.Target.Player &&
                _struckTableSkill.TargetType == SkillConstants.TargetType.Fixed)
            {
                transform.position = _attacker.transform.position;
            }
            else
            {
                transform.position += _direction * (_struckTableSkill.EffectMoveSpeed * Time.deltaTime);
            }
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
                _arrowDefaultEffect?.SetEnd();
            }
        }
        /// <summary>
        /// 발사체 이펙트가 타겟과 충돌 후 end 애니메이션을 하고 destroy 처리 
        /// </summary>
        private void OnArrowEffectDestroy() => DestroySkill();
        /// <summary>
        /// 스킬 destroy 처리
        /// </summary>
        private void DestroySkill()
        {
            StopAllCoroutines();
            if (_arrowDefaultEffect != null)
            {
                _arrowDefaultEffect.OnEffectDestroy -= OnArrowEffectDestroy;
            }
            Destroy(gameObject);
        }
    }
}
