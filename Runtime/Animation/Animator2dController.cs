using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
#if GGEMCO_USE_SPINE != true
    public class StruckChangeSlotImage
    {
        public readonly string RendererName;
        public readonly Sprite Sprite;

        public StruckChangeSlotImage(string rendererName, Sprite sprite)
        {
            RendererName = rendererName;
            Sprite = sprite;
        }
    }

    public class StruckAddAnimation
    {
        public readonly string AnimationName;
        public readonly bool Loop;
        public readonly float Delay;
        public readonly float TimeScale;

        public StruckAddAnimation(string animationName, bool loop, float delay, float timeScale)
        {
            AnimationName = animationName;
            Loop = loop;
            Delay = delay;
            TimeScale = timeScale;
        }
    }

    /// <summary>
    /// Animator 기반 2D 캐릭터 컨트롤러
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class Animator2dController : MonoBehaviour
    {
        protected Animator Animator;
        private AnimationClip[] animationClips;

        private readonly Dictionary<string, SpriteRenderer> spriteRenderers = new();

        protected virtual void Awake()
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderers[sr.gameObject.name] = sr;
            }
            Animator = GetComponent<Animator>();
            if (!Animator || !Animator.runtimeAnimatorController)
            {
                Debug.LogError("Animator component 가 없습니다.");
                return;
            }
            animationClips = Animator.runtimeAnimatorController.animationClips;
        }

        protected virtual void Start()
        {
        }

        protected void PlayAnimation(string animationName, bool loop = false, float timeScale = 1.0f,
            List<StruckAddAnimation> addAnimations = null)
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
            if (timeScale <= 0)
            {
                timeScale = 1.0f;
            }
            Animator.speed = timeScale;
            Animator.Play(animationName);
            // animator.Play(animationName, 0,0f);
            Animator.Update(0); // 즉시 반영 (옵션)

            // Loop 설정은 Animator Controller의 상태 설정에서 해야 합니다
            // addAnimations는 코루틴 또는 상태머신 동기화를 통해 처리 가능
        }

        protected void StopAnimation()
        {
            if (!Animator) return;
            Animator.speed = 0;
        }

        protected bool IsPlaying()
        {
            if (!Animator) return false;
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            return state.length > 0;
        }

        protected float GetHeight()
        {
            Bounds bounds = GetComponent<Renderer>().bounds;
            return bounds.size.y;
        }

        private float GetWidth()
        {
            Bounds bounds = GetComponent<Renderer>().bounds;
            return bounds.size.x;
        }

        protected Vector2 GetSize()
        {
            return new Vector2(GetWidth(), GetHeight());
        }

        protected void ChangeImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
        }

        protected void RemoveImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
        }

        public float GetAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            // Animator에서 직접 길이를 구하는 방법은 제한적임. AnimationClip 참조 필요
            AnimationClip[] clips = Animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name == animationName)
                {
                    return isMilliseconds ? clip.length * 1000f : clip.length;
                }
            }

            Debug.LogWarning($"애니메이션 클립을 찾을 수 없습니다. AnimationName: {animationName}");
            return 0f;
        }

        protected void SetColor(Color color)
        {
            foreach (var sr in spriteRenderers.Values)
            {
                sr.color = color;
            }
        }

        protected void SetColor(string colorHex)
        {
            if (ColorUtility.TryParseHtmlString(colorHex, out var color))
            {
                SetColor(color);
            }
        }

        // 이벤트 콜백은 애니메이션 이벤트(Inspector에서 이벤트 등록)에서 호출해야 함
        public virtual void OnAnimationComplete()
        {
        }
        public virtual void OnAnimationEventPlayEffect(int effectUid)
        {
        }
        public virtual void OnAnimationEventAttack()
        {
        }
        public virtual void OnAnimationEventProjectile(int projectileUid)
        {
        }
        public virtual void OnAnimationEventCameraShake(float intensity)
        {
        }

        public virtual void OnAnimationEventSound(string soundName)
        {
        }

        public AnimationClip GetClipByName(string animationName)
        {
            if (animationClips == null) return null;
            foreach (var clip in animationClips)
            {
                if (clip.name == animationName)
                {
                    return clip;
                }
            }

            return null;
        }
    }
#endif
}