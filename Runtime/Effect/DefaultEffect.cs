using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class DefaultEffect : MonoBehaviour
    {
        public IEffectAnimationController effectAnimationController;
        
        // 생성한 캐릭터
        private CharacterBase _character;
        // 타겟 캐릭터
        private CharacterBase _targetCharacter;
        // 유지 시간
        private float _duration;
        // 색상
        private string _color;
        // 방향
        private Vector3 _direction;
        // 원래 크기
        private float _originalScaleX;
        // 맵 height 값. sorting order 계산에 사용
        private float _mapSizeHeight;
        private CharacterBase _followCharacter;
        private float _positionY;
        private ConfigCommon.PositionYType _positionYType;
        
        private Renderer _effectRenderer;
        private RectTransform _effectRectTransform;
        private Animator _animator;
        private Coroutine _coroutineTickTimeDamage;
        private StruckTableEffect _struckTableEffect;
        
        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnEffectDestroy;
        
        protected void Awake()
        {
            _color = "";
            _originalScaleX = transform.localScale.x;
            if (_effectRenderer == null)
            {
                _effectRenderer = GetComponent<Renderer>();
            }
            if (_effectRectTransform == null)
            {
                _effectRectTransform = GetComponent<RectTransform>();
            }
        }

        public void Initialize(StruckTableEffect struckTableEffect)
        {
            _struckTableEffect = struckTableEffect;
        }

        protected void Start()
        {
            if (!string.IsNullOrEmpty(_color))
            {
                effectAnimationController.SetEffectColor($"#{_color}");
            }
            else if (!string.IsNullOrEmpty(_struckTableEffect.Color))
            {
                effectAnimationController.SetEffectColor($"#{_struckTableEffect.Color}");
            }

            SetSize(_struckTableEffect.Width, _struckTableEffect.Height);

            if (SceneGame.Instance.mapManager)
            {
                Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
                _mapSizeHeight = size.y;   
            }
            UpdateSortingOrder();
            
            if (_duration > 0)
            {
                StartCoroutine(RemoveEffectDuration(_duration));
            }
            // 셋팅 후 플레이
            effectAnimationController.Play(_duration);
        }

        public void SetSize(float width, float height)
        {
            if (width <= 0 || height <= 0) return;
            
            if (_effectRectTransform != null)
            {
                _effectRectTransform.sizeDelta = new Vector2(width, height);    
            }
            else if (_effectRenderer != null)
            {
                bool flipped = transform.localScale.x < 0;
                float signX = flipped ? -1f : 1f;

                Bounds b = _effectRenderer.bounds;
                if (b.size.x <= 0 || b.size.y <= 0) return;

                float scaleX = width  / b.size.x;
                float scaleY = height / b.size.y;
                // todo. 필요한 곳에서 Scale을 변경하고 있다. 좀 더 고민해보자. 
                // transform.localScale = new Vector3(
                //     Mathf.Abs(transform.localScale.x * scaleX) * signX,
                //     Mathf.Abs(transform.localScale.y * scaleY),
                //     transform.localScale.z
                // );
            }
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
        
            if (_effectRenderer)
                _effectRenderer.sortingOrder = baseSortingOrder;
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
        /// 회전 처리
        /// </summary>
        /// <param name="directionByTarget"></param>
        /// <param name="vector2"></param>
        public void SetRotation(Vector2 directionByTarget, Vector2 vector2)
        {
            if (!_struckTableEffect.NeedRotation) return;
            
            float angle = Mathf.Atan2(directionByTarget.y, directionByTarget.x) * Mathf.Rad2Deg;
            // 기본 방향이 "왼쪽(-X 방향)"일 경우, 180도 보정
            if (_struckTableEffect.DefaultDirection == ConfigCommon.DirectionType.Left)
            {
                if (vector2.x < 0)
                {
                    angle += 180;
                }
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
            if (scale <= 0) return;
            transform.localScale = new Vector2(scale, scale);
            _originalScaleX = transform.localScale.x;
        }
        /// <summary>
        /// 방향 처리
        /// </summary>
        /// <param name="dirX"></param>
        private void SetDirection(float dirX)
        {
            transform.localScale = new Vector3(_originalScaleX * dirX, transform.localScale.y, transform.localScale.z);
        }
        public void SetFlip(bool shouldFlip)
        {
            float dirX = shouldFlip ? -1 : 1;
            SetDirection(dirX);
            OnSetFlip(dirX);
        }

        protected virtual void OnSetFlip(float dirX)
        {
        }

        public void OnEndAnimationComplete()
        {
            StopAllCoroutines();
            Destroy(gameObject);
            OnEffectDestroy?.Invoke();
        }

        public bool TryPlayEndAnimation(DelegateEffectDestroy onEffectDestroy = null)
        {
            if (effectAnimationController == null || !effectAnimationController.HasEndAnimation())
                return false;

            if (onEffectDestroy != null)
                OnEffectDestroy += onEffectDestroy;

            PlayEndAnimation();
            return true;
        }

        public void PlayEndAnimation()
        {
            effectAnimationController.PlayEnd();
        }

        public void SetColor(string color)
        {
            _color = color;
        }

        public void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
            if (_effectRenderer == null) return;
            _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(sortingLayer);
        }
        public void SetSortingOrder(int sortingOrder)
        {
            if (_effectRenderer == null) return;
            _effectRenderer.sortingOrder = sortingOrder;
        }

        public void SetFollowCharacter(CharacterBase character)
        {
            _followCharacter = character;
        }

        public void SetPositionY(float y)
        {
            _positionY = y;
        }

        public void SetPositionYType(ConfigCommon.PositionYType type)
        {
            _positionYType = type;
        }

        public void SetCreateCharacter(GameObject character)
        {
            SetCreateCharacter(character.GetComponent<CharacterBase>());
        }
        public void SetCreateCharacter(CharacterBase character)
        {
            _character = character;
            transform.position = character.transform.position;
            SetFlip(_character.IsFlipped());
        }

        protected virtual void Update()
        {
            if (_followCharacter != null)
            {
                transform.position = _followCharacter.transform.position;
                if (_positionY > 0)
                {
                    transform.position += new Vector3(0, _positionY, 0);
                }

                if (_positionYType == ConfigCommon.PositionYType.CharacterHeight)
                {
                    // Follow 대상이 있으면 그 캐릭터의 Height를 기준으로 한다.
                    // (CreateCharacter는 flip/기본 위치 설정에 사용되지만, Follow 시점에는 follow가 우선된다.)
                    var heightOwner = _followCharacter != null ? _followCharacter : _character;
                    if (heightOwner != null)
                        transform.position += new Vector3(0, heightOwner.GetHeightByScale(), 0);
                }
            }
        }
    }
}