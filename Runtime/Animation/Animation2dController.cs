using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Unity Animator 기반의 2D 캐릭터 애니메이션 재생 및 AnimationEvent 전달을 담당하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// - Animator 상태 재생(Play)과 속도(TimeScale) 제어를 제공합니다.
    /// - AnimationEvent(Inspector 등록)를 수신하는 메서드들을 통해 외부 리스너(<see cref="IAnimationEventListener"/>)로 이벤트를 전달합니다.
    /// - AnimationClip의 이벤트 시간을 빠르게 조회할 수 있도록 클립별 이벤트 시간 캐시를 구성합니다.
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public class Animation2dController : MonoBehaviour
    {
        /// <summary>
        /// AnimationEvent를 외부 시스템으로 전달받는 리스너입니다.
        /// </summary>
        public IAnimationEventListener EventListener { get; set; }

        /// <summary>
        /// 대상 <see cref="UnityEngine.Animator"/> 컴포넌트입니다.
        /// </summary>
        protected Animator Animator;

        /// <summary>
        /// 현재 RuntimeAnimatorController에 포함된 AnimationClip 목록입니다.
        /// </summary>
        private AnimationClip[] _animationClips;

        /// <summary>
        /// 클립별 이벤트 시간 캐시입니다.
        /// - Key: AnimationClip instance id
        /// - Value: (event functionName -> earliest time)
        /// </summary>
        private readonly Dictionary<int, Dictionary<string, float>> _clipEventTimeCache = new();

        /// <summary>
        /// 자식 SpriteRenderer 캐시입니다.
        /// - Key: GameObject name
        /// - Value: SpriteRenderer
        /// </summary>
        private readonly Dictionary<string, SpriteRenderer> _spriteRenderers = new();

        private bool _isInitialized;
        private RuntimeAnimatorController _cachedRuntimeAnimatorController;
        
        /// <summary>
        /// 초기화 시 자식 SpriteRenderer를 캐싱하고, Animator 및 클립/이벤트 캐시를 구성합니다.
        /// </summary>
        /// <remarks>
        /// RuntimeAnimatorController가 없으면 애니메이션 기능이 동작하지 않으므로 오류 로그를 남기고 반환합니다.
        /// </remarks>
        protected virtual void Awake()
        {
            EnsureInitialized();
        }
        
        private void EnsureInitialized()
        {
            if (!Animator)
                Animator = GetComponent<Animator>();

            if (!Animator)
                return;

            var runtimeController = Animator.runtimeAnimatorController;
            if (!runtimeController)
                return;

            bool controllerChanged = _cachedRuntimeAnimatorController != runtimeController;
            if (_isInitialized && !controllerChanged)
                return;

            _cachedRuntimeAnimatorController = runtimeController;
            _animationClips = runtimeController.animationClips;

            _clipEventTimeCache.Clear();
            BuildClipEventTimeCache(_animationClips);

            _spriteRenderers.Clear();
            foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!spriteRenderer)
                    continue;

                _spriteRenderers[spriteRenderer.gameObject.name] = spriteRenderer;
            }

            _isInitialized = true;
        }
        
        /// <summary>
        /// 지정한 이름의 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="animationName">재생할 Animator State(또는 Clip) 이름입니다.</param>
        /// <param name="loop">
        /// 루프 여부 플래그입니다.
        /// <para>NOTE: 실제 Loop 설정은 Animator Controller 상태 설정에서 관리하는 것을 전제로 합니다.</para>
        /// </param>
        /// <param name="timeScale">재생 속도 배율입니다. 0 이하이면 1로 보정합니다.</param>
        /// <param name="addAnimations">
        /// 메인 애니메이션 이후 순차 재생할 추가 애니메이션 목록입니다.
        /// <para>값이 있으면 코루틴으로 순차 재생됩니다.</para>
        /// </param>
        /// <param name="startTime">
        /// 시작 시간(초)입니다.
        /// <para>현재 구현에서는 Animator.Play에 직접 반영되지 않습니다(호출부 호환을 위한 파라미터로 보입니다).</para>
        /// </param>
        /// <param name="endTime">
        /// 종료 시간(초)입니다.
        /// <para>현재 구현에서는 Animator.Play에 직접 반영되지 않습니다(호출부 호환을 위한 파라미터로 보입니다).</para>
        /// </param>
        /// <param name="forceReset">
        /// 강제로 다시 처음 부터 재생할 때 사용합니다.
        /// <para></para>
        /// </param>
        public void PlayAnimation(
            string animationName,
            bool loop = false,
            float timeScale = 1.0f,
            List<StruckAddAnimation> addAnimations = null,
            float startTime = 0,
            float endTime = 0,
            bool forceReset = false)
        {
            if (!Animator) return;

            if (!GetClipByName(animationName))
            {
                GcLogger.LogError($"애니메이션 clip 이 없습니다. clip name: {animationName}");
                return;
            }

            // AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            // if (state.IsName(animationName))
            // {
            //     GcLogger.Log("PlayAnimation 같은 이름 플레이 중");
            // }
            // GcLogger.Log($"PlayAnimation {gameObject.name} | {animationName} | {loop} | {timeScale}");
            // timeScale이 0 이하인 경우 정상 재생을 위해 1로 보정합니다.
            if (timeScale <= 0)
            {
                timeScale = 1.0f;
            }

            Animator.speed = timeScale;
            if (forceReset)
                Animator.Play(animationName, 0, 0);
            else
                Animator.Play(animationName);
            // animator.Play(animationName, 0,0f);
            Animator.Update(0); // 즉시 반영(옵션)

            // Loop 설정은 Animator Controller의 상태 설정에서 해야 합니다.
            // addAnimations 처리: 순차 재생이 필요하면 코루틴으로 재생합니다.
            if (addAnimations is { Count: > 0 })
            {
                StartCoroutine(PlayAddAnimations(animationName, addAnimations));
            }
        }

        /// <summary>
        /// 추가 애니메이션 목록을 순차 재생하는 코루틴입니다.
        /// </summary>
        /// <param name="startAnimationName">시작 애니메이션 이름 입니다.</param>
        /// <param name="addAnimations">순차 재생할 추가 애니메이션 목록입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator<WaitForSeconds> PlayAddAnimations(string startAnimationName, List<StruckAddAnimation> addAnimations)
        {
            if (!string.IsNullOrEmpty(startAnimationName))
            {
                float clipLength = GetAnimationDuration(startAnimationName, false);
                yield return new WaitForSeconds(clipLength / Animator.speed);
            }
            
            foreach (var add in addAnimations)
            {
                if (!GetClipByName(add.AnimationName))
                {
                    GcLogger.LogWarning($"AddAnimation: 애니메이션 없음: {add.AnimationName}");
                    continue;
                }

                if (add.Delay > 0)
                    yield return new WaitForSeconds(add.Delay);

                // 클립 길이를 구해, 현재 speed를 반영한 시간만큼 대기합니다.
                float clipLength = GetAnimationDuration(add.AnimationName, false);

                Animator.speed = add.TimeScale > 0 ? add.TimeScale : 1.0f;
                Animator.Play(add.AnimationName, 0);
                Animator.Update(0); // 즉시 반영

                yield return new WaitForSeconds(clipLength / Animator.speed);
            }
        }

        /// <summary>
        /// 애니메이션 재생을 정지합니다(Animator speed를 0으로 설정).
        /// </summary>
        protected void StopAnimation()
        {
            if (!Animator) return;
            Animator.speed = 0;
        }

        /// <summary>
        /// 현재 레이어(0)의 AnimatorStateInfo를 기준으로 재생 중인지 여부를 반환합니다.
        /// </summary>
        /// <returns>재생 가능한 상태 길이가 0보다 크면 true를 반환합니다.</returns>
        protected bool IsPlaying()
        {
            if (!Animator) return false;
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            return state.length > 0;
        }

        /// <summary>
        /// 현재 오브젝트의 Renderer bounds를 기반으로 높이(Y 크기)를 구합니다.
        /// </summary>
        /// <returns>월드 단위 높이입니다.</returns>
        private float GetHeight()
        {
            Bounds bounds = GetComponent<Renderer>().bounds;
            return bounds.size.y;
        }

        /// <summary>
        /// 현재 오브젝트의 Renderer bounds를 기반으로 너비(X 크기)를 구합니다.
        /// </summary>
        /// <returns>월드 단위 너비입니다.</returns>
        private float GetWidth()
        {
            Bounds bounds = GetComponent<Renderer>().bounds;
            return bounds.size.x;
        }

        /// <summary>
        /// 현재 오브젝트의 크기(너비, 높이)를 반환합니다.
        /// </summary>
        /// <returns>(width, height) 벡터입니다.</returns>
        protected Vector2 GetSize()
        {
            return new Vector2(GetWidth(), GetHeight());
        }

        /// <summary>
        /// 지정된 슬롯 이미지 교체를 수행합니다.
        /// </summary>
        /// <param name="changeImages">교체할 슬롯/이미지 정보 목록입니다.</param>
        /// <remarks>
        /// TODO: 구현 필요(프로젝트의 슬롯 시스템/리깅 구조에 맞춰 작성).
        /// </remarks>
        protected void ChangeImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
        }

        /// <summary>
        /// 지정된 슬롯 이미지 제거를 수행합니다.
        /// </summary>
        /// <param name="changeImages">제거할 슬롯/이미지 정보 목록입니다.</param>
        /// <remarks>
        /// TODO: 구현 필요(프로젝트의 슬롯 시스템/리깅 구조에 맞춰 작성).
        /// </remarks>
        protected void RemoveImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
        }

        /// <summary>
        /// 지정한 애니메이션 클립의 재생 길이를 반환합니다.
        /// </summary>
        /// <param name="animationName">길이를 조회할 애니메이션 이름입니다.</param>
        /// <param name="isMilliseconds">true면 밀리초(ms), false면 초(s) 단위로 반환합니다.</param>
        /// <param name="showDebug">애니메이션 클립이 없을 때, 디버그 로그를 출력할 것인지</param>
        /// <returns>클립 길이(단위는 <paramref name="isMilliseconds"/>에 따름). 찾지 못하면 0을 반환합니다.</returns>
        protected float GetAnimationDuration(string animationName, bool isMilliseconds = true, bool showDebug = true)
        {
            EnsureInitialized();

            if (_animationClips == null || _animationClips.Length == 0)
            {
                if (showDebug)
                    GcLogger.LogWarning($"Animation clips are not initialized. AnimationName: {animationName}");
                return 0f;
            }

            foreach (var clip in _animationClips)
            {
                if (clip != null && clip.name == animationName)
                    return isMilliseconds ? clip.length * 1000f : clip.length;
            }

            if (showDebug)
                GcLogger.LogWarning($"애니메이션 클립을 찾을 수 없습니다. AnimationName: {animationName}");

            return 0f;
        }

        /// <summary>
        /// 자식 SpriteRenderer 전체의 색상을 설정합니다.
        /// </summary>
        /// <param name="color">적용할 색상입니다.</param>
        protected void SetColor(Color color)
        {
            foreach (var sr in _spriteRenderers.Values)
            {
                sr.color = color;
            }
        }

        /// <summary>
        /// HTML 색상 문자열(예: "#RRGGBB", "#RRGGBBAA")을 파싱해 자식 SpriteRenderer 전체 색상을 설정합니다.
        /// </summary>
        /// <param name="colorHex">HTML 색상 문자열입니다.</param>
        protected void SetColor(string colorHex)
        {
            if (ColorUtility.TryParseHtmlString(colorHex, out var color))
            {
                SetColor(color);
            }
        }

        /// <summary>
        /// 애니메이션 완료 이벤트 콜백입니다.
        /// </summary>
        /// <remarks>
        /// AnimationEvent(Inspector에서 이벤트 등록)에서 호출되도록 설계되었습니다.
        /// 파생 클래스에서 완료 처리가 필요하면 override 합니다.
        /// </remarks>
        public void GGemCoAniEventComplete(string json)
        {
            EventListener?.OnAnimationEventComplete(json, gameObject);
        }

        /// <summary>
        /// 이펙트(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">이펙트 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventVfx(string json)
        {
            EventListener?.OnAnimationEventVfx(json, gameObject);
        }

        /// <summary>
        /// 공격(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">공격 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventAttack(string json)
        {
            EventListener?.OnAnimationEventAttack(json, gameObject);
        }

        /// <summary>
        /// 카메라 흔들림(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">카메라 흔들림 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventCameraShake(string json)
        {
            EventListener?.OnAnimationEventCameraShake(json);
        }

        /// <summary>
        /// 사운드(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">사운드 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventSound(string json)
        {
            EventListener?.OnAnimationEventSound(json);
        }

        /// <summary>
        /// 스킬(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">스킬 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventSkill(string json)
        {
            EventListener?.OnAnimationEventSkill(json, gameObject);
        }

        /// <summary>
        /// 모션(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">모션 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventMotion(string json)
        {
            EventListener?.OnAnimationEventMotion(json, gameObject);
        }

        /// <summary>
        /// CrowdControl(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">CrowdControl 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventCrowdControl(string json)
        {
            EventListener?.OnAnimationEventCrowdControl(json, gameObject);
        }

        /// <summary>
        /// 도구 사용(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">도구 사용 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventUseTool(string json)
        {
            EventListener?.OnAnimationEventUseTool(json, gameObject);
        }

        /// <summary>
        /// 씨앗 사용(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        /// <param name="json">씨앗 사용 파라미터(JSON 문자열)입니다.</param>
        public void GGemCoAniEventUseSeed(string json)
        {
            EventListener?.OnAnimationEventUseSeed(json, gameObject);
        }

        /// <summary>
        /// 가드 종료(AnimationEvent) 이벤트를 리스너로 전달합니다.
        /// </summary>
        public void GGemCoAniEventGuardEnd()
        {
            EventListener?.OnAnimationEventGuardEnd(gameObject);
        }

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 시작 이벤트입니다.
        /// </summary>
        /// <param name="json">
        /// AnimationEvent string 파라미터(JSON)입니다.
        /// 런타임 설정을 오버라이드하는 용도로 사용될 수 있습니다.
        /// </param>
        public void GGemCoAniEventStartBackstepTrail(string json)
        {
            EventListener?.OnAnimationEventStartBackstepTrail(json, gameObject);
        }

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 종료 이벤트입니다.
        /// </summary>
        /// <param name="json">AnimationEvent string 파라미터(JSON)입니다.</param>
        public void GGemCoAniEventStopBackstepTrail(string json)
        {
            EventListener?.OnAnimationEventStopBackstepTrail(json, gameObject);
        }


        /// <summary>
        /// 현재 프레임의 Sprite를 단발 잔상으로 1회 캡처하는 이벤트입니다.
        /// </summary>
        /// <param name="json">AnimationEvent string 파라미터(JSON)입니다.</param>
        public void GGemCoAniEventCaptureAfterimageSnapshot(string json)
        {
            EventListener?.OnAnimationEventCaptureAfterimageSnapshot(json, gameObject);
        }

#if GGEMCO_2D_CONTROL
        /// <summary>
        /// 점프 시작 애니메이션의 마지막 프레임에서 호출되는 이벤트입니다.
        /// </summary>
        public void GGemCoAniEventJumpUp()
        {
            EventListener?.OnAnimationEventJump(gameObject, AnimationConstants.EventNameJumpUp);
        }

        /// <summary>
        /// 점프 정점에서 낙하로 전환되는 애니메이션의 마지막 프레임에서 호출되는 이벤트입니다.
        /// </summary>
        public void GGemCoAniEventJumpFall()
        {
            EventListener?.OnAnimationEventJump(gameObject, AnimationConstants.EventNameJumpFall);
        }

        /// <summary>
        /// 점프 착지 후 종료 애니메이션의 마지막 프레임에서 호출되는 이벤트입니다.
        /// </summary>
        public void GGemCoAniEventJumpEnd()
        {
            EventListener?.OnAnimationEventJump(gameObject, AnimationConstants.EventNameJumpEnd);
        }

        /// <summary>
        /// 대시 중 애니메이션의 마지막 프레임에서 호출되는 이벤트입니다.
        /// </summary>
        public void GGemCoAniEventDashPlay()
        {
            EventListener?.OnAnimationEventDash(gameObject, AnimationConstants.EventNameDashPlay);
        }

        /// <summary>
        /// 대시 종료 후 종료 애니메이션의 마지막 프레임에서 호출되는 이벤트입니다.
        /// </summary>
        public void GGemCoAniEventDashEnd()
        {
            EventListener?.OnAnimationEventDash(gameObject, AnimationConstants.EventNameDashEnd);
        }
#endif

        /// <summary>
        /// 이름으로 AnimationClip을 검색합니다.
        /// </summary>
        /// <param name="animationName">검색할 클립 이름입니다.</param>
        /// <returns>찾으면 AnimationClip, 없으면 null을 반환합니다.</returns>
        protected AnimationClip GetClipByName(string animationName)
        {
            EnsureInitialized();

            if (_animationClips == null || _animationClips.Length == 0)
            {
                GcLogger.LogWarning($"Animation clips are not initialized. AnimationName: {animationName}");
                return null;
            }

            foreach (var clip in _animationClips)
            {
                if (clip.name == animationName)
                {
                    return clip;
                }
            }

            return null;
        }

        /// <summary>
        /// 현재 레이어에서 재생 중인 첫 번째 AnimationClip을 반환합니다.
        /// </summary>
        /// <param name="layerIndex">Animator 레이어 인덱스입니다.</param>
        /// <returns>현재 레이어의 첫 번째 클립입니다.</returns>
        /// <remarks>
        /// AnimatorClipInfo 배열의 0번째를 사용하므로, 클립이 없을 수 있는 상황에서는 예외가 날 수 있습니다.
        /// 호출 전 해당 레이어에 클립이 존재하는지 보장하는 흐름에서 사용해야 합니다.
        /// </remarks>
        private AnimationClip GetCurrentAnimationClip(int layerIndex)
        {
            return Animator.GetCurrentAnimatorClipInfo(layerIndex)[0].clip;
        }

        /// <summary>
        /// 현재 재생 중인 클립의 루프 여부를 설정합니다.
        /// </summary>
        /// <param name="shouldLoop">true면 Loop, false면 Once로 설정합니다.</param>
        /// <param name="layerIndex">대상 레이어 인덱스입니다.</param>
        /// <remarks>
        /// AnimationClip.wrapMode 변경은 런타임/프로젝트 설정에 따라 기대대로 동작하지 않을 수 있습니다.
        /// 루프 정책은 기본적으로 Animator Controller 설정에서 관리하는 것을 권장합니다.
        /// </remarks>
        protected void SetAnimationLoop(bool shouldLoop, int layerIndex = 0)
        {
            AnimationClip currentClip = GetCurrentAnimationClip(layerIndex);
            if (currentClip == null) return;

            currentClip.wrapMode = shouldLoop ? WrapMode.Loop : WrapMode.Once;
        }

        /// <summary>
        /// 클립별 AnimationEvent(functionName)의 "가장 이른 시간"을 캐시에 구성합니다.
        /// </summary>
        /// <param name="clips">캐싱할 AnimationClip 목록입니다.</param>
        private void BuildClipEventTimeCache(AnimationClip[] clips)
        {
            _clipEventTimeCache.Clear();
            if (clips == null) return;

            foreach (var clip in clips)
            {
                if (clip == null) continue;

                var map = new Dictionary<string, float>(StringComparer.Ordinal);
                var events = GetEvents(clip);

                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    if (evt == null) continue;

                    var fn = evt.functionName;
                    if (string.IsNullOrEmpty(fn)) continue;

                    // 동일 functionName이 여러 번 존재하면, 가장 이른 시간을 유지합니다.
                    if (!map.TryGetValue(fn, out var prev) || evt.time < prev)
                        map[fn] = evt.time;
                }

                _clipEventTimeCache[clip.GetInstanceID()] = map;
            }
        }

        /// <summary>
        /// AnimationClip에서 AnimationEvent 목록을 안전하게 가져옵니다.
        /// </summary>
        /// <param name="clip">대상 클립입니다.</param>
        /// <returns>null 안전성을 보장한 이벤트 배열입니다.</returns>
        private static AnimationEvent[] GetEvents(AnimationClip clip)
        {
            if (clip == null) return Array.Empty<AnimationEvent>();

            // Runtime-safe.
            return clip.events ?? Array.Empty<AnimationEvent>();
        }

        /// <summary>
        /// 지정한 애니메이션 클립에서 특정 이벤트(functionName)의 발생 시간을 반환합니다.
        /// </summary>
        /// <param name="aniName">이벤트 시간을 조회할 애니메이션(클립) 이름입니다.</param>
        /// <param name="eventName">조회할 AnimationEvent의 functionName 입니다.</param>
        /// <param name="exceptEventName">
        /// 제외 목록(레거시 파라미터)입니다.
        /// <para>목록에 <paramref name="eventName"/>이 포함되어 있으면 -1을 반환합니다.</para>
        /// </param>
        /// <returns>이벤트 시간(초). 찾지 못하거나 제외 대상이면 -1을 반환합니다.</returns>
        private float GetEventTime(string aniName, string eventName, List<string> exceptEventName = null)
        {
            if (!Animator) return -1f;
            if (string.IsNullOrEmpty(aniName) || string.IsNullOrEmpty(eventName)) return -1f;

            var clip = GetClipByName(aniName);
            if (clip == null) return -1f;

            // Exclusion list support (legacy parameter).
            if (exceptEventName is { Count: > 0 })
            {
                for (int i = 0; i < exceptEventName.Count; i++)
                {
                    if (string.Equals(exceptEventName[i], eventName, StringComparison.Ordinal))
                        return -1f;
                }
            }

            if (!_clipEventTimeCache.TryGetValue(clip.GetInstanceID(), out var map))
            {
                // 런타임에 animator controller가 교체된 경우 캐시 미스가 발생할 수 있습니다.
                BuildClipEventTimeCache(new[] { clip });
                _clipEventTimeCache.TryGetValue(clip.GetInstanceID(), out map);
            }

            if (map != null && map.TryGetValue(eventName, out var t))
                return t;

            // Fallback: 직접 스캔(드물게만 발생하는 것을 기대)
            var events = GetEvents(clip);
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                if (evt != null && evt.functionName == eventName)
                    return evt.time;
            }

            return -1f;
        }

        /// <summary>
        /// loop_start / loop_end AnimationEvent를 이용해 지정 duration 동안 자연스러운 루프 재생을 구성합니다.
        /// </summary>
        /// <param name="animationName">루프 이벤트를 포함한 애니메이션 이름입니다.</param>
        /// <param name="duration">총 재생 시간(초)입니다. 0 이하이면 일반 1회 재생으로 처리합니다.</param>
        /// <remarks>
        /// loop_start, loop_end 이벤트가 있어야 하며, 다음과 같은 구간으로 나뉜다고 가정합니다.
        /// <code>
        ///             loop_start        loop_end
        /// /---------------/---------------/---------------/
        /// </code>
        /// </remarks>
        protected void PlayAnimationWidthLoopEvent(string animationName, float duration)
        {
            float eventTimeLoopStart = GetEventTime(animationName, "loop_start");
            if (eventTimeLoopStart < 0)
            {
                GcLogger.LogWarning($"check loop_start event {animationName}");
                return;
            }

            float eventTimeLoopEnd = GetEventTime(animationName, "loop_end");
            if (eventTimeLoopEnd < 0)
            {
                GcLogger.LogWarning($"check loop_end event {animationName}");
                return;
            }

            float aniDurationTime = GetAnimationDuration(animationName, false);
            if (aniDurationTime == 0)
            {
                GcLogger.LogWarning($"check animation duration {animationName}");
                return;
            }

            //  startDuration    loopDuration     endDuration
            //---------------/---------------/---------------/
            float startDuration = eventTimeLoopEnd;
            float loopDuration = eventTimeLoopEnd - eventTimeLoopStart;
            float endDuration = aniDurationTime - eventTimeLoopEnd;

            // duration이 없는 경우: 일반 1회 재생
            if (duration <= 0)
            {
                PlayAnimation(animationName);
            }
            // duration이 start+end보다 크면: 루프 구간을 여러 번 이어 붙여 맞춥니다.
            else if (startDuration + endDuration < duration)
            {
                var realLoopDuration = duration - startDuration - endDuration;
                var loopCnt = realLoopDuration / loopDuration;
                var loopCntCeil = Math.Ceiling(realLoopDuration / loopDuration);

                // 루프 반복 횟수(ceil)에 맞추기 위해 타임스케일을 조정합니다.
                float newTimeScale = (float)loopCntCeil / loopCnt;

                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();

                // loop 구간 반복 추가
                for (var i = 0; i < loopCntCeil; i++)
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(animationName, false, 0, newTimeScale, eventTimeLoopStart, eventTimeLoopEnd);

                    newAddAnimations.Add(struckAddAnimation);
                }

                // end 구간 추가(필요 시)
                if (!Mathf.Approximately(aniDurationTime, eventTimeLoopStart))
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(animationName, false, 0, 1, eventTimeLoopEnd, aniDurationTime);

                    newAddAnimations.Add(struckAddAnimation);
                }

                // start 구간 재생 + addAnimations로 loop/end를 이어 재생
                PlayAnimation(animationName, false, 1, newAddAnimations, 0, eventTimeLoopEnd);
            }
            // duration이 너무 작으면: 전체를 스케일하여 1회 재생
            else
            {
                PlayAnimation(animationName, false, aniDurationTime / duration);
            }
        }

        /// <summary>
        /// 현재 RuntimeAnimatorController의 모든 클립 길이를 조회합니다.
        /// </summary>
        /// <returns>Key: clip name, Value: clip length(초) 사전입니다.</returns>
        public Dictionary<string, float> GetAnimationAllLength()
        {
            Dictionary<string, float> clipLength = new Dictionary<string, float>();
            if (!Animator || !Animator.runtimeAnimatorController) return clipLength;
            
            EnsureInitialized();

            if (_animationClips == null || _animationClips.Length == 0)
            {
                GcLogger.LogWarning($"Animation clips are not initialized.");
                return clipLength;
            }
            
            foreach (var clip in _animationClips)
            {
                if (!clipLength.ContainsKey(clip.name))
                    clipLength.Add(clip.name, Mathf.Max(0f, clip.length));
            }

            return clipLength;
        }
    }
}