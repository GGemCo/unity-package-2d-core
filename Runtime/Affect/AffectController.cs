using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터에서 사용하는 어펙트 컨트롤러
    /// CharacterBase.cs 에서 생성된다.
    /// </summary>
    public class AffectController
    {
        private readonly CharacterBase _character;
        private readonly TableAffect _tableAffect;
        private readonly TableEffect _tableEffect;
        private readonly EffectManager _effectManager;

        // uid -> active affect
        private readonly Dictionary<int, ActiveAffect> _actives = new();

        // group -> uid (동일 Group은 1개만 허용)
        private readonly Dictionary<string, int> _groupIndex = new(System.StringComparer.Ordinal);

        // WaitForSeconds 캐시(동일 duration 재사용)
        private static readonly Dictionary<float, WaitForSeconds> _waitCache = new();

        private static WaitForSeconds GetWait(float seconds)
        {
            if (seconds <= 0f) seconds = 0f;
            if (_waitCache.TryGetValue(seconds, out var w)) return w;
            w = new WaitForSeconds(seconds);
            _waitCache[seconds] = w;
            return w;
        }

        private static bool IsNoneGroup(string group)
        {
            return string.IsNullOrWhiteSpace(group) ||
                   group.Equals("None", System.StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ActiveAffect
        {
            public int Uid;
            public string Group; // 빈 값/None 일 수 있음
            public List<ConfigCommon.StruckStatus> Buffs;
            public DefaultEffect Effect;
            public Coroutine Timer;
            public float Duration;
        }

        public AffectController(CharacterBase characterBase)
        {
            if (TableLoaderManager.Instance == null) return;
            _character = characterBase;
            _tableAffect = TableLoaderManager.Instance.TableAffect;
            _tableEffect  = TableLoaderManager.Instance.TableEffect;
            _effectManager = SceneGame.Instance.EffectManager;
        }

        /// <summary>
        /// 어펙트 적용하기
        /// </summary>
        public void ApplyAffect(int affectUid, float duration = 0)
        {
            var info = _tableAffect.GetDataByUid(affectUid);
            if (info == null)
            {
                GcLogger.LogError("affect 테이블에 없는 어펙트 입니다. affect Uid: " + affectUid);
                return;
            }
            
            if (duration <= 0)
            {
                duration  = info.Duration;    
            }
            var statusId  = info.StatusID;
            var suffix    = info.StatusSuffix;
            var value     = info.Value;
            var group     = info.Group;
            var buffs = new List<ConfigCommon.StruckStatus> { new(statusId, suffix, value) };

            // 1) 그룹 규칙: Group이 None/빈 값이 아니라면 동일 Group 선제 제거
            if (!IsNoneGroup(group) && _groupIndex.TryGetValue(group, out var uidInGroup))
            {
                // 기존 동일 그룹 어펙트 제거
                RemoveAffect(uidInGroup);
            }

            // 2) 동일 UID 재적용 시 정리(지속시간 초기화 의미 포함)
            if (_actives.ContainsKey(info.Uid))
            {
                RemoveAffect(info.Uid);
            }

            // 3) 신규 적용
            _character.ApplyStatModifiers(buffs);
            _character.RecalculateStats();

            DefaultEffect createdEffect = null;
            if (info.EffectUid > 0 && _effectManager != null)
            {
                createdEffect = _effectManager.CreateEffect(info.EffectUid);
                if (createdEffect != null)
                {
                    createdEffect.SetCreateCharacter(_character); // scale 이전
                    createdEffect.SetScale(info.EffectScale);
                    createdEffect.SetDuration(duration);
                    createdEffect.SetFollowCharacter(_character);
                    createdEffect.SetPositionY(info.EffectPositionY);
                    createdEffect.SetPositionYType(info.EffectPositionYType);
                    createdEffect.SetSortingLayer(info.EffectSortingLayer);
                    createdEffect.transform.localPosition = Vector3.zero;
                }
            }

            var timer = _character.StartCoroutine(RemoveAfterDuration(info.Uid, duration));

            var active = new ActiveAffect
            {
                Uid = info.Uid,
                Group = group,
                Buffs = buffs,
                Effect = createdEffect,
                Timer = timer,
                Duration = duration
            };
            _actives[info.Uid] = active;

            if (!IsNoneGroup(group))
            {
                _groupIndex[group] = info.Uid;
            }
        }

        private IEnumerator RemoveAfterDuration(int affectUid, float duration)
        {
            yield return GetWait(duration);
            RemoveAffect(affectUid);
        }

        /// <summary>
        /// 단일 어펙트 제거(버프/디버프 및 이펙트/코루틴 포함)
        /// </summary>
        public void RemoveAffect(int affectUid)
        {
            if (!_actives.TryGetValue(affectUid, out var active))
                return;

            // 1) 코루틴 정지
            if (active.Timer != null && _character != null)
            {
                _character.StopCoroutine(active.Timer);
            }

            // 2) 스탯 되돌리기
            if (active.Buffs is { Count: > 0 })
            {
                _character?.RemoveStatModifiers(active.Buffs);
                _character?.RecalculateStats();
            }

            // 3) 시각 이펙트 종료
            if (active.Effect != null)
            {
                // 애니메이션 종료 루틴 호출(프로젝트 정책에 맞게 DestroyForce로 바꿔도 무방)
                active.Effect.OnEndAnimationComplete();
            }

            // 4) 인덱스/캐시 정리
            if (!IsNoneGroup(active.Group))
            {
                // group→uid가 나 자신인 경우만 삭제
                if (_groupIndex.TryGetValue(active.Group, out var mapped) && mapped == affectUid)
                    _groupIndex.Remove(active.Group);
            }

            _actives.Remove(affectUid);
        }

        /// <summary>
        /// 캐릭터가 죽으면 모든 어펙트 지우기
        /// </summary>
        public void RemoveAllAffects()
        {
            // 코루틴/버프/이펙트 전부 정리
            foreach (var kv in _actives)
            {
                var a = kv.Value;
                if (a.Timer != null && _character != null)
                    _character.StopCoroutine(a.Timer);

                if (a.Buffs is { Count: > 0 })
                    _character?.RemoveStatModifiers(a.Buffs);

                if (a.Effect != null)
                    a.Effect.DestroyForce();
            }

            _character?.RecalculateStats();

            _actives.Clear();
            _groupIndex.Clear();
        }
    }
}
