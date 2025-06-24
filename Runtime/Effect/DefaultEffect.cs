using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class DefaultEffect : MonoBehaviour
    {
        public IEffectAnimationController EffectAnimationController;
        
        // 이펙트 시작 애니 클립 이름
        private const string CLIP_NAME_START = "start";
        // 루프 되는 클립 이름
        private const string CLIP_NAME_PLAY = "play";
        // 없어지는 애니 클립 이름
        private const string CLIP_NAME_END = "end";
        
        // 유지 시간
        private float duration;
        // 발사한 캐릭터
        private CharacterBase character;
        // 타겟 캐릭터
        private CharacterBase targetCharacter;
        // 방향
        private Vector3 direction;
        // 원래 크기
        private float originalScaleX;
        // 맵 height 값. sorting order 계산에 사용
        private float mapSizeHeight;
        
        private Renderer characterRenderer;
        private Animator animator;
        private StruckTableSkill struckTableSkill;
        private Coroutine coroutineTickTimeDamage;
        private StruckTableEffect struckTableEffect;
        
        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnEffectDestroy;
        
        protected void Awake()
        {
            originalScaleX = transform.localScale.x;
            if (characterRenderer == null)
            {
                characterRenderer = GetComponent<Renderer>();
            }
        }

        protected void Start()
        {
            List<StruckAddAnimation> addAnimations = new List<StruckAddAnimation>
                { new (CLIP_NAME_PLAY, true, 0, 1f) };
            EffectAnimationController.PlayEffectAnimation(CLIP_NAME_START, false, 1, addAnimations);

            if (struckTableEffect.Color != "")
            {
                EffectAnimationController.SetEffectColor($"#{struckTableEffect.Color}");
            }
            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            mapSizeHeight = size.y;
            UpdateSortingOrder();
        }

        public void Initialize(StruckTableEffect pstruckTableEffect)
        {
            struckTableEffect = pstruckTableEffect;
        }
        private IEnumerator RemoveEffectDuration(float f)
        {
            yield return new WaitForSeconds(f);
            EffectAnimationController.PlayEffectAnimation(CLIP_NAME_END);
        }
        // /// <summary>
        // /// 애니메이션이 끝나면 호출되는 콜백 함수
        // /// </summary>
        // /// <param name="entry"></param>
        // protected void OnAnimationComplete(TrackEntry entry)
        // {
        //     if (entry.Animation.Name == CLIP_NAME_END)
        //     {
        //         StopAllCoroutines();
        //         Destroy(gameObject);
        //         OnEffectDestroy?.Invoke();
        //     }
        // }
        /// <summary>
        /// 캐릭터 순서. sorting order 처리 
        /// </summary>
        private void UpdateSortingOrder()
        {
            int baseSortingOrder = MathHelper.GetSortingOrder(mapSizeHeight, transform.position.y);
        
            characterRenderer.sortingOrder = baseSortingOrder;
        }
        /// <summary>
        /// 지속 시간 설정
        /// </summary>
        /// <param name="f"></param>
        public void SetDuration(float f)
        {
            duration = f;
            if (duration > 0)
            {
                StartCoroutine(RemoveEffectDuration(duration));
            }
        }
        /// <summary>
        /// 방향 처리
        /// </summary>
        /// <param name="dirX"></param>
        public void SetDirection(float dirX)
        {
            transform.localScale = new Vector3(originalScaleX * dirX, transform.localScale.y, transform.localScale.z);
        }
        /// <summary>
        /// 회전 처리
        /// </summary>
        /// <param name="directionByTarget"></param>
        /// <param name="vector2"></param>
        public void SetRotation(Vector2 directionByTarget, Vector2 vector2)
        {
            if (!struckTableEffect.NeedRotation) return;
            
            float angle = Mathf.Atan2(directionByTarget.y, directionByTarget.x) * Mathf.Rad2Deg;
            // 기본 방향이 "왼쪽(-X 방향)"일 경우, 90도 보정
            if (vector2.x < 0)
            {
                angle += 180;
            }

            // Transform의 Z축 회전 적용
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        /// <summary>
        /// 이펙트 end 애니메이션 처리
        /// </summary>
        public void SetEnd()
        {
            EffectAnimationController.PlayEffectAnimation(CLIP_NAME_END);
        }

        public void DestroyForce()
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }

        public void SetScale(float scale)
        {
            transform.localScale = new Vector2(scale, scale);
            originalScaleX = transform.localScale.x;
        }
    }
}