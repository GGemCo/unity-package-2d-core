using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 베이크된 <see cref="UIEffectRuntimeSequence"/>를 시간 순서대로 실행하는 런타임 플레이어입니다.
    /// </summary>
    public sealed class UIEffectTimelinePlayer : MonoBehaviour
    {
        private readonly Dictionary<UIEffectTimelineEventType, IUIEffectTimelineExecutor> _executors = new Dictionary<UIEffectTimelineEventType, IUIEffectTimelineExecutor>();
        private readonly List<UIEffectTarget> _playedTargets = new List<UIEffectTarget>();
        private Coroutine _runningCoroutine;
        private IUIEffectTimelineTargetResolver _resolver;

        private void Awake()
        {
            RegisterDefaultExecutors();
        }

        /// <summary>
        /// 외부 Resolver를 등록합니다. 등록하지 않으면 현재 씬의 <see cref="UIEffectTimelineTargetRegistry"/>를 검색합니다.
        /// </summary>
        /// <param name="resolver">targetKey 해석에 사용할 Resolver입니다.</param>
        public void SetResolver(IUIEffectTimelineTargetResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// UI 효과 런타임 시퀀스를 기본 문맥으로 재생합니다.
        /// </summary>
        /// <param name="sequence">재생할 시퀀스입니다.</param>
        public void Play(UIEffectRuntimeSequence sequence)
        {
            Play(sequence, UIEffectTimelineContext.Default);
        }

        /// <summary>
        /// UI 효과 런타임 시퀀스를 지정한 문맥으로 재생합니다.
        /// </summary>
        /// <param name="sequence">재생할 시퀀스입니다.</param>
        /// <param name="context">재생 문맥입니다.</param>
        public void Play(UIEffectRuntimeSequence sequence, UIEffectTimelineContext context)
        {
            Stop();
            _playedTargets.Clear();
            if (sequence == null)
            {
                return;
            }

            _runningCoroutine = StartCoroutine(PlayRoutine(sequence, context));
        }

        /// <summary>
        /// 현재 재생 중인 UI 효과 타임라인을 중지합니다.
        /// </summary>
        public void Stop()
        {
            if (_runningCoroutine != null)
            {
                StopCoroutine(_runningCoroutine);
                _runningCoroutine = null;
            }

            StopPlayedTargets();
        }

        /// <summary>
        /// 기본 Fade/Move/Scale/Shake/Flash Executor를 등록합니다.
        /// </summary>
        private void RegisterDefaultExecutors()
        {
            RegisterExecutor(new UIEffectFadeExecutor());
            RegisterExecutor(new UIEffectMoveExecutor());
            RegisterExecutor(new UIEffectScaleExecutor());
            RegisterExecutor(new UIEffectShakeExecutor());
            RegisterExecutor(new UIEffectFlashExecutor());
        }

        /// <summary>
        /// 이벤트 종류별 Executor를 등록하거나 교체합니다.
        /// </summary>
        /// <param name="executor">등록할 Executor입니다.</param>
        public void RegisterExecutor(IUIEffectTimelineExecutor executor)
        {
            if (executor == null)
            {
                return;
            }

            _executors[executor.EventType] = executor;
        }

        /// <summary>
        /// 시퀀스 이벤트를 시작 시간 순서대로 실행합니다.
        /// </summary>
        private IEnumerator PlayRoutine(UIEffectRuntimeSequence sequence, UIEffectTimelineContext context)
        {
            IUIEffectTimelineTargetResolver resolver = _resolver ?? new UIEffectTimelineSceneTargetResolver();
            UIEffectRuntimeEvent[] events = sequence.events ?? new UIEffectRuntimeEvent[0];
            float elapsed = 0f;

            for (int i = 0; i < events.Length; i++)
            {
                UIEffectRuntimeEvent runtimeEvent = events[i];
                float waitTime = Mathf.Max(0f, runtimeEvent.startTime - elapsed);
                while (waitTime > 0f)
                {
                    float deltaTime = context.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    waitTime -= deltaTime;
                    elapsed += deltaTime;
                    yield return null;
                }

                PlayEvent(sequence, runtimeEvent, resolver, context);
            }

            float remaining = Mathf.Max(0f, sequence.duration - elapsed);
            while (remaining > 0f)
            {
                float deltaTime = context.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                remaining -= deltaTime;
                elapsed += deltaTime;
                yield return null;
            }

            _runningCoroutine = null;
        }

        /// <summary>
        /// 단일 런타임 이벤트를 해석하여 대상 UI에 효과를 시작합니다.
        /// </summary>
        private void PlayEvent(
            UIEffectRuntimeSequence sequence,
            UIEffectRuntimeEvent runtimeEvent,
            IUIEffectTimelineTargetResolver resolver,
            UIEffectTimelineContext context)
        {
            if (sequence.payloads == null || runtimeEvent.payloadIndex < 0 || runtimeEvent.payloadIndex >= sequence.payloads.Length)
            {
                return;
            }

            UIEffectPayloadBase payload = sequence.payloads[runtimeEvent.payloadIndex];
            if (payload == null || resolver == null || !resolver.TryResolve(payload.targetKey, out UIEffectTarget target) || target == null)
            {
                return;
            }

            target.AutoBind();
            if (!_playedTargets.Contains(target))
            {
                _playedTargets.Add(target);
            }

            if (!ApplyPlayPolicy(target, payload.channel, payload.playPolicy))
            {
                return;
            }

            if (_executors.TryGetValue(runtimeEvent.type, out IUIEffectTimelineExecutor executor))
            {
                executor.Play(this, target, payload, runtimeEvent.Duration, context);
            }
        }


        /// <summary>
        /// 현재 플레이어가 시작했던 대상 UI 효과를 중지합니다.
        /// </summary>
        private void StopPlayedTargets()
        {
            foreach (UIEffectTarget target in _playedTargets)
            {
                if (target == null)
                {
                    continue;
                }

                if (target.CanvasGroup != null)
                {
                    UiFadeUtility.StopFadeIfRunning(target.CanvasGroup, this);
                }

                if (target.MoveTarget != null)
                {
                    UiMoveAnchoredPosition.StopIfRunning(target.MoveTarget, this);
                }

                if (target.ScaleTarget != null)
                {
                    UIEffectScaleUtility.StopIfRunning(target.ScaleTarget, this);
                }

                if (target.ShakeTarget != null)
                {
                    UIEffectShakeUtility.StopIfRunning(target.ShakeTarget, this);
                }

                UIEffectService.Stop(target);
            }

            _playedTargets.Clear();
        }

        /// <summary>
        /// 같은 대상/채널에 이미 재생 중인 효과가 있을 때 Payload 정책을 적용합니다.
        /// </summary>
        private static bool ApplyPlayPolicy(UIEffectTarget target, UIEffectChannel channel, UIEffectPlayPolicy playPolicy)
        {
            switch (playPolicy)
            {
                case UIEffectPlayPolicy.IgnoreIfPlaying:
                    return !UIEffectService.IsPlaying(target, channel);

                case UIEffectPlayPolicy.Restart:
                    UIEffectService.Stop(target);
                    return true;

                case UIEffectPlayPolicy.StopSameChannelAndPlay:
                    UIEffectService.Stop(target, channel);
                    return true;

                case UIEffectPlayPolicy.Parallel:
                default:
                    return true;
            }
        }
    }
}
