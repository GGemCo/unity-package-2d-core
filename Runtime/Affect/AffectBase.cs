using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 개별 어펙트 인스턴스: 스탯, 비주얼, 타이머를 자체 관리
    /// </summary>
    public class AffectBase
    {
        // --- 정적 캐시(할당 절감) ---
        private static readonly Dictionary<float, WaitForSeconds> _waitCache = new();
        private static WaitForSeconds GetWait(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            if (_waitCache.TryGetValue(seconds, out var w)) return w;
            w = new WaitForSeconds(seconds);
            _waitCache[seconds] = w;
            return w;
        }

        // --- 의존성/상태 ---
        public int Uid { get; }
        public string Group { get; }
        public float Duration { get; private set; }

        protected readonly CharacterBase character;
        private readonly EffectManager _effectManager;
        private readonly List<ConfigCommon.StruckStatus> _buffs;
        private readonly System.Action<int> _onCompleted; // 컨트롤러에 종료 통보

        private DefaultEffect _effect;
        private Coroutine _timer;

        public AffectBase(
            CharacterBase character,
            EffectManager effectManager,
            int uid,
            string group,
            List<ConfigCommon.StruckStatus> buffs,
            float duration,
            System.Action<int> onCompleted)
        {
            this.character     = character;
            _effectManager = effectManager;
            Uid            = uid;
            Group          = group;
            _buffs         = buffs ?? new List<ConfigCommon.StruckStatus>(0);
            Duration       = duration;
            _onCompleted   = onCompleted;
        }

        // 훅 추가
        protected virtual void OnBeforeApply(StruckTableAffect info) {}
        protected virtual void OnAfterApply(StruckTableAffect info) {}
        protected virtual void OnBeforeStop() {}
        
        /// <summary>스탯/이펙트 적용 및 타이머 시작</summary>
        public void Apply(StruckTableAffect affectInfo)
        {
            if (character == null) return;

            // 서브클래스 사전 처리
            OnBeforeApply(affectInfo);

            // 1) 스탯
            if (_buffs.Count > 0)
            {
                character.ApplyStatModifiers(_buffs);
                character.RecalculateStats();
            }

            // 2) 비주얼 이펙트 (옵션)
            if (affectInfo != null && affectInfo.EffectUid > 0 && _effectManager != null)
            {
                _effect = _effectManager.CreateEffect(affectInfo.EffectUid);
                if (_effect != null)
                {
                    _effect.SetCreateCharacter(character);
                    // 스케일/소팅 이전
                    _effect.SetScale(affectInfo.EffectScale);
                    _effect.SetDuration(Duration);
                    _effect.SetFollowCharacter(character);
                    _effect.SetPositionY(affectInfo.EffectPositionY);
                    _effect.SetPositionYType(affectInfo.EffectPositionYType);
                    _effect.SetSortingLayer(affectInfo.EffectSortingLayer);
                    _effect.transform.localPosition = Vector3.zero;
                }
            }

            // 서브클래스 사후 처리
            OnAfterApply(affectInfo);

            // 3) 타이머 시작 (기존 그대로)
            StartTimer();
        }

        /// <summary>지속시간을 갱신(재적용 정책)</summary>
        public void Refresh(float newDuration)
        {
            Duration = Mathf.Max(0f, newDuration);
            RestartTimerOnly();
        }

        /// <summary>수동 종료(스탯/이펙트/타이머 정리)</summary>
        public void Stop()
        {
            // 서브클래스 정리(가장 먼저 호출)
            OnBeforeStop();
            
            // 타이머 정지
            if (_timer != null && character != null)
            {
                character.StopCoroutine(_timer);
                _timer = null;
            }

            // 스탯 회수
            if (_buffs.Count > 0 && character != null)
            {
                character.RemoveStatModifiers(_buffs);
                character.RecalculateStats();
            }

            // 이펙트 정리(정책에 따라 OnEndAnimationComplete vs DestroyForce)
            if (_effect != null)
            {
                _effect.OnEndAnimationComplete();
                _effect = null;
            }
        }

        private void StartTimer()
        {
            if (character == null) return;
            if (_timer != null)
            {
                character.StopCoroutine(_timer);
                _timer = null;
            }
            _timer = character.StartCoroutine(CoDuration());
        }

        private void RestartTimerOnly()
        {
            if (character == null) return;
            if (_timer != null)
            {
                character.StopCoroutine(_timer);
                _timer = null;
            }
            _timer = character.StartCoroutine(CoDuration());
        }

        private IEnumerator CoDuration()
        {
            yield return GetWait(Duration); // 스케일드 시간 기준 대기. 필요 시 Realtime 변형 가능
            // 만료: 컨트롤러에 종료 통보(컨트롤러가 Stop 호출 및 인덱스 정리)
            _onCompleted?.Invoke(Uid);
        }
    }
}
