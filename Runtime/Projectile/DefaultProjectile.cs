using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 발사체 클래스
    /// </summary>
    public class DefaultProjectile : MonoBehaviour
    {
        private float _speed;
        private int _arcHeight;
        private const float PositionThreshold = 0.1f; // 좌표 타겟일 때 도달 판정

        private Vector2 _startPoint;
        private Vector2 _targetPoint;
        private CharacterBase _targetObject = null;
        private float _journeyLength;
        private float _startTime;
        private bool _initialized = false;
        private StruckTableProjectile _struckTableProjectile;
        private DefaultEffect _effectProjectile;
        private CharacterBase _fromCharacter;
        private StruckTableEffect _struckTableEffect;
        private bool _shouldFlip;
        private Vector3 _direction;
        private Vector2 _previousPosition;
        
        // 데미지 처리
        private long _damage;
        private EffectManager _effectManager;

        public void Initialize(StruckTableProjectile info)
        {
            if (info == null) return;
            
            _struckTableProjectile = info;
            _speed = info.MoveSpeed;
            _arcHeight = info.ArcHeightMin;
            if (info.ArcHeightMin != info.ArcHeightMax)
            {
                _arcHeight = Random.Range(info.ArcHeightMin, info.ArcHeightMax);
            }
            
            Rigidbody2D rigidbody2d = ComponentController.AddRigidbody2D(gameObject);
            rigidbody2d.bodyType = RigidbodyType2D.Kinematic;
            
            Vector2 offset = Vector2.zero;
            Vector2 size = Vector2.zero;
            if (_struckTableProjectile != null && _struckTableProjectile.ColliderSize != Vector2.zero)
            {
                size = _struckTableProjectile.ColliderSize;
            }
            ComponentController.AddCapsuleCollider2D(gameObject, true, offset, size);
        }
        private void Start()
        {
            _effectManager = SceneGame.Instance.EffectManager;
            DefaultEffect effect = _effectManager.CreateEffect(_struckTableProjectile.EffectUid);
            if (!effect) return;
            effect.gameObject.transform.SetParent(gameObject.transform);
            _effectProjectile = effect;
            // 충돌 후 처리해야 되기 때문에 무한 loop 로 설정
            _effectProjectile.SetDuration(-1);
            if (_struckTableProjectile.EffectScale > 0)
            {
                _effectProjectile.SetScale(_struckTableProjectile.EffectScale);
            }
            _struckTableEffect = TableLoaderManager.Instance.GetEffectData(_struckTableProjectile.EffectUid);
            UpdateEffectFlip();
            _effectProjectile.SetRotation(_targetPoint - _startPoint, _direction);
            _effectProjectile.transform.localPosition = Vector3.zero;
        }

        private void SetStartPoint()
        {
            _startPoint = transform.position;
            if (_fromCharacter)
            {
                _startPoint = _fromCharacter.gameObject.transform.position;
            }
            if (_struckTableProjectile.StartPosition != Vector2.zero)
            {
                _startPoint += _struckTableProjectile.StartPosition;
            }
        }
        public void Launch(Vector2 targetPos)
        {
            _targetObject = null;
            SetStartPoint();
            _targetPoint = targetPos;
            _journeyLength = Vector2.Distance(_startPoint, _targetPoint);
            _startTime = Time.time;
            _direction = (_targetPoint - _startPoint).normalized;
            transform.position = _startPoint;
            _initialized = true;
        }
        public void Launch(CharacterBase targetObj)
        {
            if (targetObj == null)
            {
                GcLogger.LogWarning("Projectile launched with null target!");
                Destroy(gameObject);
                return;
            }

            _targetObject = targetObj;
            Launch(new Vector2(targetObj.transform.position.x, targetObj.GetRandomPositionYInHitArea()));
        }

        private void UpdateEffectFlip()
        {
            _shouldFlip = false;
            
            if (_struckTableEffect.DefaultDirection == ConfigCommon.DirectionType.Right && _targetPoint.x < _startPoint.x)
                _shouldFlip = true;
            else if (_struckTableEffect.DefaultDirection == ConfigCommon.DirectionType.Left && _targetPoint.x > _startPoint.x)
                _shouldFlip = true;
            
            _effectProjectile.SetFlip(_shouldFlip);
        }
        private void Update()
        {
            if (!_initialized) return;

            // todo 거리가 달라도 일정한 속도로 날아가게 
            float distCovered = (Time.time - _startTime) * _speed;
            float fraction = distCovered / _journeyLength;
            if (fraction > 1f)
            {
                Destroy(gameObject);
                return;
            }
            Vector2 newPos = Vector2.Lerp(_startPoint, _targetPoint, fraction);

            if (_arcHeight > 0f)
            {
                float height = _arcHeight * 4 * (fraction - fraction * fraction);
                newPos.y += height;
            }
            // 이동 처리
            transform.position = newPos;
            
            // 방향 계산 → 회전 적용
            Vector2 moveDir = newPos - _previousPosition;
            _direction = (newPos - _startPoint).normalized;
            if (moveDir.sqrMagnitude > 0.0001f) // 0 방지
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                // 기본 방향이 "왼쪽(-X 방향)"일 경우, 90도 보정
                if (_direction.x < 0)
                {
                    angle += 180;
                }
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            _previousPosition = newPos;
            
            // 좌표 타겟일 경우 도달 감지
            // if (targetObject == null && Vector2.Distance(transform.position, targetPoint) <= PositionThreshold)
            // {
            //     OnHitTarget();
            // }
            
            // 화면 밖으로 벗어났는지 확인
            if (!IsInCameraView())
            {
                // 스킬에 벗어났다고 알림
                // associatedSkill.OnProjectileOutOfBounds();
                GcLogger.Log($"Projectile out camera view.");
                Destroy(gameObject); // projectile 삭제
            }
        }
        // 카메라 화면 내에 있는지 확인하는 메서드
        private bool IsInCameraView()
        {
            if (!SceneGame.Instance.mainCamera) return false;
            // 현재 카메라의 뷰포트 내에 있는지 확인
            Vector3 screenPoint = SceneGame.Instance.mainCamera.WorldToViewportPoint(transform.position);
            return screenPoint.x is >= 0 and <= 1 && screenPoint.y is >= 0 and <= 1;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // GcLogger.Log(collision.name);
            if (_fromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster)) && 
                collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                CharacterHitArea area = collision.GetComponent<CharacterHitArea>();
                if (area)
                {
                    OnHitTarget(area);
                }
            }
            else if (_fromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)) && 
                     collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster)))
            {
                CharacterHitArea area = collision.GetComponent<CharacterHitArea>();
                if (area)
                {
                    OnHitTarget(area);
                }
            }
            else if (_initialized && collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
            {
                GcLogger.Log("Projectile Destroy by MapGround");
                Destroy(gameObject);
            }
        }

        private void OnHitTarget(CharacterHitArea area)
        {
            // GcLogger.Log("Projectile hit target!");
            ShowHitEffect();
            if (area)
            {
                MetadataDamage metadataDamage = new MetadataDamage
                {
                    damage = _damage,
                    attacker = _fromCharacter.gameObject,
                    damageType = SkillConstants.DamageType.Physic,
                };
                area.target?.TakeDamage(metadataDamage);
            }
        }

        private void ShowHitEffect()
        {
            // Hit 이펙트가 따로 있으면 Projectile 은 바로 Destroy 한다.
            if (_struckTableProjectile.HitEffectUid > 0) 
            {
                Destroy(gameObject);
                var effect = _effectManager.CreateEffect(_struckTableProjectile.HitEffectUid);
                if (!effect) return;
                effect.SetCreateCharacter(_fromCharacter);
                effect.transform.position = transform.position;
                // 발사체가 flip 되면 hit 이펙트도 flip 처리
                effect.SetFlip(_shouldFlip);
            }
            // Hit 이펙트가 따로 없으면, Effect 오브젝트의 End 애니메이션을 실행한다.
            else
            {
                _effectProjectile.PlayEndAnimation();
            }
        }

        public void SetFromCharacter(CharacterBase characterBase)
        {
            _fromCharacter = characterBase;
        }

        public void SetDamage(long damage)
        {
            _damage = damage;
        }
    }
}