using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIEffectPreset을 해석하여 공용 UI 연출을 실행하는 서비스입니다.
    /// </summary>
    public static class UIEffectService
    {
        private sealed class RunningEffectHandle
        {
            public int HandleId;
            public int TargetId;
            public UIEffectChannel Channel;
            public MonoBehaviour Runner;
            public Coroutine Coroutine;
            public UIEffectTarget Target;
            public Graphic FlashGraphic;
            public Color FlashBaseColor;
            public bool HasFlashBaseColor;
        }

        private sealed class TargetState
        {
            public readonly Dictionary<UIEffectChannel, List<RunningEffectHandle>> HandlesByChannel = new Dictionary<UIEffectChannel, List<RunningEffectHandle>>();
        }

        private static readonly Dictionary<int, TargetState> RunningEffects = new Dictionary<int, TargetState>();
        private static int _nextHandleId = 1;

        public static Coroutine Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, Action onComplete = null)
        {
            return Play(runner, target, preset, default, onComplete);
        }

        public static Coroutine Play(MonoBehaviour runner, UIEffectTarget target, UIEffectPreset preset, UIEffectContext context, Action onComplete = null)
        {
            if (runner == null || target == null || preset == null)
            {
                onComplete?.Invoke();
                return null;
            }

            target.AutoBind();
            if (!ApplyPlayPolicy(target, preset))
            {
                return null;
            }

            var handle = new RunningEffectHandle
            {
                HandleId = _nextHandleId++,
                TargetId = target.GetInstanceID(),
                Channel = preset.channel,
                Runner = runner,
                Target = target,
            };

            RegisterHandle(handle);
            handle.Coroutine = runner.StartCoroutine(PlayRoutine(handle, preset, context, onComplete));
            return handle.Coroutine;
        }

        public static void Stop(UIEffectTarget target)
        {
            if (target == null)
            {
                return;
            }

            int targetId = target.GetInstanceID();
            if (!RunningEffects.TryGetValue(targetId, out var targetState))
            {
                return;
            }

            var copiedHandles = new List<RunningEffectHandle>();
            foreach (var pair in targetState.HandlesByChannel)
            {
                copiedHandles.AddRange(pair.Value);
            }

            foreach (var handle in copiedHandles)
            {
                StopHandle(handle, removeFromRegistry: true);
            }
        }

        public static void Stop(UIEffectTarget target, UIEffectChannel channel)
        {
            if (target == null)
            {
                return;
            }

            int targetId = target.GetInstanceID();
            if (!RunningEffects.TryGetValue(targetId, out var targetState))
            {
                return;
            }

            if (!targetState.HandlesByChannel.TryGetValue(channel, out var handles) || handles.Count == 0)
            {
                return;
            }

            var copiedHandles = new List<RunningEffectHandle>(handles);
            foreach (var handle in copiedHandles)
            {
                StopHandle(handle, removeFromRegistry: true);
            }
        }

        public static bool IsPlaying(UIEffectTarget target, UIEffectChannel channel)
        {
            if (target == null)
            {
                return false;
            }

            int targetId = target.GetInstanceID();
            if (!RunningEffects.TryGetValue(targetId, out var targetState))
            {
                return false;
            }

            return targetState.HandlesByChannel.TryGetValue(channel, out var handles) && handles.Count > 0;
        }

        private static bool ApplyPlayPolicy(UIEffectTarget target, UIEffectPreset preset)
        {
            switch (preset.playPolicy)
            {
                case UIEffectPlayPolicy.IgnoreIfPlaying:
                    return !IsPlaying(target, preset.channel);

                case UIEffectPlayPolicy.Restart:
                    Stop(target);
                    return true;

                case UIEffectPlayPolicy.StopSameChannelAndPlay:
                    Stop(target, preset.channel);
                    return true;

                case UIEffectPlayPolicy.Parallel:
                default:
                    return true;
            }
        }

        private static void RegisterHandle(RunningEffectHandle handle)
        {
            if (!RunningEffects.TryGetValue(handle.TargetId, out var targetState))
            {
                targetState = new TargetState();
                RunningEffects.Add(handle.TargetId, targetState);
            }

            if (!targetState.HandlesByChannel.TryGetValue(handle.Channel, out var handles))
            {
                handles = new List<RunningEffectHandle>();
                targetState.HandlesByChannel.Add(handle.Channel, handles);
            }

            handles.Add(handle);
        }

        private static IEnumerator PlayRoutine(RunningEffectHandle handle, UIEffectPreset preset, UIEffectContext context, Action onComplete)
        {
            UIEffectTarget target = handle.Target;
            MonoBehaviour runner = handle.Runner;
            if (target == null || runner == null || preset == null)
            {
                Complete(handle, onComplete);
                yield break;
            }

            UIEffectScaleUtility.CacheBaseScale(target.ScaleTarget);
            UIEffectMoveUtility.CacheBasePosition(target.MoveTarget);
            UIEffectShakeUtility.CacheBasePosition(target.ShakeTarget);

            float maxDuration = 0f;
            if (preset.useFade)
            {
                maxDuration = Mathf.Max(maxDuration, preset.fadeDuration);
                var fadeOptions = UiFadeUtility.FadeOptions.Default;
                fadeOptions.useUnscaledTime = preset.useUnscaledTime;
                fadeOptions.startAlpha = preset.fadeStartAlpha >= 0f ? preset.fadeStartAlpha : null;
                fadeOptions.easeType = preset.fadeEaseType;
                fadeOptions.updateInteractableOnComplete = preset.fadeUpdateInteractableOnComplete;
                fadeOptions.updateBlocksRaycastsOnComplete = preset.fadeUpdateBlocksRaycastsOnComplete;
                fadeOptions.disableInputWhenInvisible = preset.fadeDisableInputWhenInvisible;

                if (preset.fadeStartAlpha >= 0f && UiFadeUtility.TryGetCanvasGroup(target.gameObject, true, out var canvasGroup))
                {
                    canvasGroup.alpha = Mathf.Clamp01(preset.fadeStartAlpha);
                }

                if (preset.fadeTargetAlpha >= 0.5f)
                    UiFadeUtility.FadeIn(runner, target.gameObject, preset.fadeDuration, fadeOptions, true);
                else
                    UiFadeUtility.FadeOut(runner, target.gameObject, preset.fadeDuration, fadeOptions, true);
            }

            if (preset.useMove && target.MoveTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.moveDuration);

                Vector2 basePosition = UIEffectMoveUtility.GetOrCacheBasePosition(target.MoveTarget);
                Vector2 from;
                Vector2 to;
                switch (preset.moveMode)
                {
                    case UIEffectMoveMode.FromBaseToOffset:
                        from = basePosition;
                        to = basePosition + preset.moveFromOffset;
                        break;

                    case UIEffectMoveMode.FromOffsetToBase:
                    default:
                        from = basePosition + preset.moveFromOffset;
                        to = basePosition;
                        break;
                }

                target.MoveTarget.anchoredPosition = from;

                var moveOptions = MoveOptions.Default;
                moveOptions.useUnscaledTime = preset.useUnscaledTime;
                moveOptions.easeType = preset.moveEaseType;
                moveOptions.snapToTargetOnComplete = preset.moveSnapToTargetOnComplete;
                UiMoveAnchoredPosition.MoveTo(runner, target.MoveTarget, to, preset.moveDuration, moveOptions);
            }

            if (preset.useScale && target.ScaleTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.scaleDuration);
                UIEffectScaleUtility.AnimateTo(
                    runner,
                    target.ScaleTarget,
                    preset.scaleFrom,
                    preset.scaleTo,
                    preset.scaleDuration,
                    preset.useUnscaledTime,
                    preset.scaleEaseType);
            }

            if (preset.usePunchScale && target.ScaleTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.punchDuration);
                UIEffectScaleUtility.Punch(
                    runner,
                    target.ScaleTarget,
                    preset.punchScale,
                    preset.punchDuration,
                    preset.useUnscaledTime,
                    preset.punchEaseType);
            }

            if (preset.useShake && target.ShakeTarget != null)
            {
                maxDuration = Mathf.Max(maxDuration, preset.shakeDuration);
                UIEffectShakeUtility.Shake(
                    runner,
                    target.ShakeTarget,
                    preset.shakeStrength,
                    preset.shakeDuration,
                    preset.shakeVibrato,
                    preset.useUnscaledTime);
            }

            if (preset.useFlash && target.FlashTargetGraphic != null)
            {
                handle.FlashGraphic = target.FlashTargetGraphic;
                handle.FlashBaseColor = target.FlashTargetGraphic.color;
                handle.HasFlashBaseColor = true;
                maxDuration = Mathf.Max(maxDuration, preset.flashDuration);
            }

            if (maxDuration <= 0f)
            {
                RestoreFlash(handle);
                Complete(handle, onComplete);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < maxDuration)
            {
                if (handle.Target == null || handle.Runner == null)
                {
                    break;
                }

                elapsed += preset.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                UpdateFlash(handle, preset, elapsed);
                yield return null;
            }

            RestoreFlash(handle);
            Complete(handle, onComplete);
        }

        private static void UpdateFlash(RunningEffectHandle handle, UIEffectPreset preset, float elapsed)
        {
            if (!preset.useFlash || handle.FlashGraphic == null || !handle.HasFlashBaseColor)
            {
                return;
            }

            float duration = Mathf.Max(0.0001f, preset.flashDuration);
            float t = Mathf.Clamp01(elapsed / duration);
            float pingPong = t <= 0.5f ? t * 2f : (1f - t) * 2f;
            float eased = Mathf.Clamp01(Easing.Apply(pingPong, preset.flashEaseType));

            Color flashColor = preset.flashColor;
            flashColor.a = Mathf.Clamp01(preset.flashPeakAlpha);
            handle.FlashGraphic.color = Color.LerpUnclamped(handle.FlashBaseColor, flashColor, eased);
        }

        private static void StopHandle(RunningEffectHandle handle, bool removeFromRegistry)
        {
            if (handle == null)
            {
                return;
            }

            if (removeFromRegistry)
            {
                UnregisterHandle(handle);
            }

            if (handle.Runner != null && handle.Coroutine != null)
            {
                handle.Runner.StopCoroutine(handle.Coroutine);
            }

            StopTargetAnimations(handle);
            RestoreFlash(handle);
        }

        private static void StopTargetAnimations(RunningEffectHandle handle)
        {
            if (handle?.Target == null || handle.Runner == null)
            {
                return;
            }

            if (handle.Target.CanvasGroup != null)
            {
                UiFadeUtility.StopFadeIfRunning(handle.Target.CanvasGroup, handle.Runner);
            }

            if (handle.Target.MoveTarget != null)
            {
                UiMoveAnchoredPosition.StopIfRunning(handle.Target.MoveTarget, handle.Runner);
            }

            if (handle.Target.ScaleTarget != null)
            {
                UIEffectScaleUtility.StopIfRunning(handle.Target.ScaleTarget, handle.Runner);
            }

            if (handle.Target.ShakeTarget != null)
            {
                UIEffectShakeUtility.StopIfRunning(handle.Target.ShakeTarget, handle.Runner);
            }
        }

        private static void RestoreFlash(RunningEffectHandle handle)
        {
            if (handle?.FlashGraphic == null || !handle.HasFlashBaseColor)
            {
                return;
            }

            handle.FlashGraphic.color = handle.FlashBaseColor;
        }

        private static void Complete(RunningEffectHandle handle, Action onComplete)
        {
            UnregisterHandle(handle);
            onComplete?.Invoke();
        }

        private static void UnregisterHandle(RunningEffectHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            if (!RunningEffects.TryGetValue(handle.TargetId, out var targetState))
            {
                return;
            }

            if (targetState.HandlesByChannel.TryGetValue(handle.Channel, out var handles))
            {
                handles.RemoveAll(item => item == null);
                handles.Remove(handle);
                if (handles.Count == 0)
                {
                    targetState.HandlesByChannel.Remove(handle.Channel);
                }
            }

            if (targetState.HandlesByChannel.Count == 0)
            {
                RunningEffects.Remove(handle.TargetId);
            }
        }
    }
}
