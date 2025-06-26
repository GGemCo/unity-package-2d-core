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
        
        // 유지 시간
        private float _duration;
        // 발사한 캐릭터
        private CharacterBase _character;
        // 타겟 캐릭터
        private CharacterBase _targetCharacter;
        // 방향
        private Vector3 _direction;
        // 원래 크기
        private float _originalScaleX;
        // 맵 height 값. sorting order 계산에 사용
        private float _mapSizeHeight;
        
        private Renderer _characterRenderer;
        private Animator _animator;
        private StruckTableSkill _struckTableSkill;
        private Coroutine _coroutineTickTimeDamage;
        private StruckTableEffect _struckTableEffect;
        
        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnEffectDestroy;
        
        protected void Awake()
        {
            _originalScaleX = transform.localScale.x;
            if (_characterRenderer == null)
            {
                _characterRenderer = GetComponent<Renderer>();
            }
        }

        protected void Start()
        {
            List<StruckAddAnimation> addAnimations = new List<StruckAddAnimation>
                { new (IEffectAnimationController.KeyClipNamePlay, true, 0, 1f) };
            EffectAnimationController.PlayEffectAnimation(IEffectAnimationController.KeyClipNameStart, false, 1, addAnimations);

            if (_struckTableEffect.Color != "")
            {
                EffectAnimationController.SetEffectColor($"#{_struckTableEffect.Color}");
            }
            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            _mapSizeHeight = size.y;
            UpdateSortingOrder();
        }

        public void Initialize(StruckTableEffect pstruckTableEffect)
        {
            _struckTableEffect = pstruckTableEffect;
        }
        private IEnumerator RemoveEffectDuration(float f)
        {
            yield return new WaitForSeconds(f);
            EffectAnimationController.PlayEffectAnimation(IEffectAnimationController.KeyClipNameEnd);
        }
        /// <summary>
        /// 캐릭터 순서. sorting order 처리 
        /// </summary>
        private void UpdateSortingOrder()
        {
            int baseSortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
        
            _characterRenderer.sortingOrder = baseSortingOrder;
        }
        /// <summary>
        /// 지속 시간 설정
        /// </summary>
        /// <param name="f"></param>
        public void SetDuration(float f)
        {
            _duration = f;
            if (_duration > 0)
            {
                StartCoroutine(RemoveEffectDuration(_duration));
            }
        }
        /// <summary>
        /// 방향 처리
        /// </summary>
        /// <param name="dirX"></param>
        public void SetDirection(float dirX)
        {
            transform.localScale = new Vector3(_originalScaleX * dirX, transform.localScale.y, transform.localScale.z);
        }
        /// <summary>
        /// 회전 처리
        /// </summary>
        /// <param name="directionByTarget"></param>
        /// <param name="vector2"></param>
        public void SetRotation(Vector2 directionByTarget, Vector2 vector2)
        {
            if (!_struckTableEffect.NeedRotation) return;
            
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
            EffectAnimationController.PlayEffectAnimation(IEffectAnimationController.KeyClipNameEnd);
        }

        public void Destroy()
        {
            StopAllCoroutines();
            Destroy(gameObject);
            OnEffectDestroy?.Invoke();
        }
        public void DestroyForce()
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }

        public void SetScale(float scale)
        {
            transform.localScale = new Vector2(scale, scale);
            _originalScaleX = transform.localScale.x;
        }
    }
}