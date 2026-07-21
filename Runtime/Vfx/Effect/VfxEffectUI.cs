using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class VfxEffectUI : VfxAnimationControllerSprite
    {
        [Tooltip("자동 재생 여부")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("재생 속도")]
        [SerializeField] private  float timeScale;
        [Tooltip("크기")]
        [SerializeField] private  Vector3 scale = Vector3.one;

        private Coroutine _oneShotCoroutine;
        private int _oneShotVersion;

        /// <summary>
        /// 활성화 시 애니메이션 캐시를 복구하고 자동 재생 설정을 적용합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            if (!autoStart) return;
            InitializeScale();
            Play(-1f, timeScale);
        }

        /// <summary>
        /// 오브젝트가 비활성화될 때 진행 중인 one-shot 재생과 완료 콜백을 정리합니다.
        /// </summary>
        protected override void OnDisable()
        {
            CancelOneShotEffect();
            base.OnDisable();
        }

        public void PlayEffect(bool forceReset = false)
        {
            CancelOneShotEffect();
            InitializeScale();
            Play(-1f, timeScale, forceReset);
        }

        /// <summary>
        /// start 애니메이션 클립만 첫 프레임부터 한 번 재생합니다.
        /// </summary>
        /// <param name="forceReset">
        /// 같은 start 상태를 재생 중이어도 첫 프레임부터 다시 재생하려면 <see langword="true"/>입니다.
        /// </param>
        /// <returns>start 클립이 존재하여 재생을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// start 이후 play/end 클립을 연결하지 않습니다.
        /// 클립 재생 완료 후에는 Animator가 start 클립의 마지막 프레임을 유지합니다.
        /// </remarks>
        public bool PlayStartClipOnce(bool forceReset = true)
        {
            CancelOneShotEffect();
            InitializeScale();

            const string clipName = IVfxAnimationController.KeyClipNameStart;
            if (GetClipByName(clipName) == null)
            {
                return false;
            }

            // PlayAnimation이 기존 순차 재생 코루틴을 정리하므로
            // 파괴 연출 도중 복구되어도 start 클립만 독립적으로 다시 재생됩니다.
            PlayAnimation(clipName, timeScale: timeScale, forceReset: forceReset);
            return true;
        }
        
        /// <summary>
        /// end 애니메이션 클립만 첫 프레임부터 한 번 재생합니다.
        /// </summary>
        /// <param name="forceReset">
        /// 같은 end 상태를 재생 중이어도 첫 프레임부터 다시 재생하려면 <see langword="true"/>입니다.
        /// </param>
        /// <returns>end 클립이 존재하여 재생을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool PlayEndClipOnce(bool forceReset = true)
        {
            CancelOneShotEffect();
            InitializeScale();

            const string clipName = IVfxAnimationController.KeyClipNameEnd;
            if (GetClipByName(clipName) == null)
            {
                return false;
            }

            // PlayAnimation이 기존 순차 재생 코루틴을 정리하므로
            // 다른 연출 도중 호출되어도 end 클립만 독립적으로 다시 재생됩니다.
            PlayAnimation(clipName, timeScale: timeScale, forceReset: forceReset);
            return true;
        }

        /// <summary>
        /// UI 이펙트를 1회만 재생하고, 예상 재생 시간을 초 단위로 반환합니다.
        /// </summary>
        /// <param name="forceReset">같은 상태 재생 중이어도 첫 프레임부터 다시 재생할지 여부입니다.</param>
        /// <returns>
        /// 1회 재생 시작에 성공하면 예상 재생 시간(초)을 반환합니다.
        /// 재생 가능한 클립이 없으면 0을 반환합니다.
        /// </returns>
        public float PlayOneShotEffect(bool forceReset = true)
        {
            CancelOneShotEffect();
            InitializeScale();
            bool started = Play(0f, timeScale, forceReset);
            if (!started)
            {
                return 0f;
            }

            return GetEstimatedOneShotDuration();
        }

        /// <summary>
        /// start, play, end 클립을 실제 Animator 상태 완료 시점까지 순차 재생합니다.
        /// </summary>
        /// <param name="onCompleted">마지막 클립 재생이 완료된 뒤 호출할 콜백입니다.</param>
        /// <param name="forceReset">첫 클립을 첫 프레임부터 다시 재생하려면 <see langword="true"/>입니다.</param>
        /// <returns>재생 가능한 클립이 있어 one-shot 재생을 시작했으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// Animator State Speed와 Speed Multiplier가 적용된 실제 진행률을 관찰합니다.
        /// 새 재생 요청이나 비활성화로 취소되면 완료 콜백을 호출하지 않습니다.
        /// </remarks>
        public bool PlayOneShotEffect(Action<VfxEffectUI> onCompleted, bool forceReset = true)
        {
            CancelOneShotEffect();
            InitializeScale();

            bool hasStart = GetClipByName(IVfxAnimationController.KeyClipNameStart) != null;
            bool hasPlay = GetClipByName(IVfxAnimationController.KeyClipNamePlay) != null;
            bool hasEnd = GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null;
            if (!hasStart && !hasPlay && !hasEnd)
            {
                return false;
            }

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            int version = _oneShotVersion;
            _oneShotCoroutine = StartCoroutine(PlayOneShotSequence(
                hasStart,
                hasPlay,
                hasEnd,
                forceReset,
                version,
                onCompleted));
            return true;
        }

        /// <summary>
        /// 진행 중인 완료 콜백 기반 one-shot 재생을 취소합니다.
        /// </summary>
        /// <remarks>
        /// 재생 버전을 증가시켜 이미 시작된 이전 재생의 완료 콜백도 무효화합니다.
        /// </remarks>
        public void CancelOneShotEffect()
        {
            _oneShotVersion++;
            if (_oneShotCoroutine == null) return;

            StopCoroutine(_oneShotCoroutine);
            _oneShotCoroutine = null;
        }

        /// <summary>
        /// 존재하는 start, play, end 클립을 순서대로 재생하고 실제 완료 후 콜백을 호출합니다.
        /// </summary>
        /// <param name="hasStart">start 클립 존재 여부입니다.</param>
        /// <param name="hasPlay">play 클립 존재 여부입니다.</param>
        /// <param name="hasEnd">end 클립 존재 여부입니다.</param>
        /// <param name="forceReset">첫 클립 강제 재시작 여부입니다.</param>
        /// <param name="version">재생 취소 판정에 사용할 버전입니다.</param>
        /// <param name="onCompleted">전체 재생 완료 콜백입니다.</param>
        /// <returns>Unity 코루틴 열거자입니다.</returns>
        private IEnumerator PlayOneShotSequence(
            bool hasStart,
            bool hasPlay,
            bool hasEnd,
            bool forceReset,
            int version,
            Action<VfxEffectUI> onCompleted)
        {
            bool isFirstClip = true;

            if (hasStart)
            {
                yield return PlayClipAndWaitForCompletion(
                    IVfxAnimationController.KeyClipNameStart,
                    timeScale,
                    forceReset,
                    version);
                if (!IsOneShotVersionValid(version)) yield break;
                isFirstClip = false;
            }

            if (hasPlay)
            {
                // 기존 Play(0) 정책과 동일하게 start가 없을 때만 UI timeScale을 첫 play 클립에 적용합니다.
                float playTimeScale = isFirstClip ? timeScale : 1f;
                // 순차 진입 클립은 풀 재사용 시 남아 있을 수 있는 이전 normalizedTime을 사용하지 않도록 강제 초기화합니다.
                yield return PlayClipAndWaitForCompletion(
                    IVfxAnimationController.KeyClipNamePlay,
                    playTimeScale,
                    !isFirstClip || forceReset,
                    version);
                if (!IsOneShotVersionValid(version)) yield break;
                isFirstClip = false;
            }

            if (hasEnd)
            {
                // start/play가 없는 end 단독 구성에서만 UI timeScale을 적용합니다.
                float endTimeScale = isFirstClip ? timeScale : 1f;
                // 이전 one-shot의 end 마지막 프레임에서 시작하지 않도록 순차 진입 시 첫 프레임으로 초기화합니다.
                yield return PlayClipAndWaitForCompletion(
                    IVfxAnimationController.KeyClipNameEnd,
                    endTimeScale,
                    !isFirstClip || forceReset,
                    version);
                if (!IsOneShotVersionValid(version)) yield break;
            }

            _oneShotCoroutine = null;
            onCompleted?.Invoke(this);
        }

        /// <summary>
        /// 지정한 Animator 상태를 재생하고 한 사이클이 실제로 완료될 때까지 대기합니다.
        /// </summary>
        /// <param name="clipName">재생할 Animator 상태 및 클립 이름입니다.</param>
        /// <param name="clipTimeScale">Animator 전체 재생 속도입니다.</param>
        /// <param name="forceReset">첫 프레임부터 강제로 다시 재생할지 여부입니다.</param>
        /// <param name="version">재생 취소 판정에 사용할 버전입니다.</param>
        /// <returns>Unity 코루틴 열거자입니다.</returns>
        private IEnumerator PlayClipAndWaitForCompletion(
            string clipName,
            float clipTimeScale,
            bool forceReset,
            int version)
        {
            PlayAnimation(clipName, timeScale: clipTimeScale, forceReset: forceReset);

            bool observedState = false;
            while (IsOneShotVersionValid(version))
            {
                if (!Animator || !isActiveAndEnabled || !gameObject.activeInHierarchy)
                {
                    yield break;
                }

                AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(clipName))
                {
                    observedState = true;
                    if (stateInfo.normalizedTime >= 1f)
                    {
                        yield break;
                    }
                }
                else if (observedState)
                {
                    // Animator 전이가 설정된 경우 해당 상태를 벗어난 시점을 완료로 간주합니다.
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 지정한 재생 버전이 현재 one-shot 요청과 일치하고 계속 실행 가능한지 확인합니다.
        /// </summary>
        /// <param name="version">검사할 one-shot 재생 버전입니다.</param>
        /// <returns>재생을 계속할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsOneShotVersionValid(int version)
        {
            return version == _oneShotVersion &&
                   Animator != null &&
                   isActiveAndEnabled &&
                   gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 현재 클립 구성(start/play/end)과 timeScale 설정을 기준으로
        /// 1회 재생 시 예상 소요 시간을 계산합니다.
        /// </summary>
        /// <returns>예상 소요 시간(초)입니다.</returns>
        public float GetEstimatedOneShotDuration()
        {
            const float epsilon = 0.0001f;
            float safeTimeScale = Mathf.Abs(timeScale) > epsilon ? Mathf.Abs(timeScale) : 1f;

            bool hasStart = GetClipByName(IVfxAnimationController.KeyClipNameStart) != null;
            bool hasPlay = GetClipByName(IVfxAnimationController.KeyClipNamePlay) != null;
            bool hasEnd = GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null;

            float durationStart = hasStart ? GetAnimationDuration(IVfxAnimationController.KeyClipNameStart, false, false) : 0f;
            float durationPlay = hasPlay ? GetAnimationDuration(IVfxAnimationController.KeyClipNamePlay, false, false) : 0f;
            float durationEnd = hasEnd ? GetAnimationDuration(IVfxAnimationController.KeyClipNameEnd, false, false) : 0f;

            if (hasStart)
            {
                // duration=0 경로에서 start는 timeScale을 적용하고, add(play/end)는 기본 배속으로 재생된다.
                return (durationStart / safeTimeScale) + durationPlay + durationEnd;
            }

            if (hasPlay)
            {
                // start가 없으면 play가 시작 클립이 되며 timeScale이 적용된다.
                return (durationPlay / safeTimeScale) + durationEnd;
            }

            if (hasEnd)
            {
                // start/play가 없으면 end가 시작 클립이 되며 timeScale이 적용된다.
                return durationEnd / safeTimeScale;
            }

            return 0f;
        }

        private void InitializeScale()
        {
            if (scale == Vector3.zero || scale == Vector3.one) return;
            transform.localScale = scale;
        }
    }
}
