using System.Collections;
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

        private string _color;
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
            _color = "";
            _originalScaleX = transform.localScale.x;
            if (_characterRenderer == null)
            {
                _characterRenderer = GetComponent<Renderer>();
            }
        }

        protected void Start()
        {
            if (!string.IsNullOrEmpty(_color))
            {
                EffectAnimationController.SetEffectColor($"#{_color}");
            }
            else if (!string.IsNullOrEmpty(_struckTableEffect.Color))
            {
                EffectAnimationController.SetEffectColor($"#{_struckTableEffect.Color}");
            }
            
            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            _mapSizeHeight = size.y;
            UpdateSortingOrder();
            
            if (_duration > 0)
            {
                StartCoroutine(RemoveEffectDuration(_duration));
            }
            // 셋팅 후 플레이
            EffectAnimationController.Play(_duration);
        }

        public void Initialize(StruckTableEffect struckTableEffect)
        {
            _struckTableEffect = struckTableEffect;
        }
        private IEnumerator RemoveEffectDuration(float f)
        {
            yield return new WaitForSeconds(f);
            OnEndAnimationComplete();
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
        }
        /// <summary>
        /// 방향 처리
        /// </summary>
        /// <param name="dirX"></param>
        private void SetDirection(float dirX)
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
        // /// <summary>
        // /// 이펙트 end 애니메이션 처리
        // /// </summary>
        // public void SetEnd()
        // {
        //     bool result = EffectAnimationController.PlayEndAnimation();
        //     // end 애니메이션이 있으면 end 애니메이션을 플레이하고 종료, 없으면 강제 종료
        //     if (!result)
        //         OnEndAnimationComplete();
        // }
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
        public void SetFlip(bool shouldFlip)
        {
            float dirX = shouldFlip ? -1 : 1;
            SetDirection(dirX);
        }
        public void OnEndAnimationComplete()
        {
            StopAllCoroutines();
            Destroy(gameObject);
            OnEffectDestroy?.Invoke();
        }

        public void PlayEndAnimation()
        {
            EffectAnimationController.PlayEnd();
        }

        public void SetColor(string color)
        {
            _color = color;
        }
    }
}