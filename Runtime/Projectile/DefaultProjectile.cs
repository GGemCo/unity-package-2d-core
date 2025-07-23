using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 발사체 클래스
    /// </summary>
    public class DefaultProjectile : MonoBehaviour
    {
        private float speed;
        private int arcHeight;
        private const float PositionThreshold = 0.1f; // 좌표 타겟일 때 도달 판정

        private Vector2 startPoint;
        private Vector2 targetPoint;
        private CharacterBase targetObject = null;
        private float journeyLength;
        private float startTime;
        private bool initialized = false;
        private StruckTableProjectile struckTableProjectile;
        private DefaultEffect effectProjectile;
        private CharacterBase fromCharacter;
        private StruckTableEffect struckTableEffect;
        private bool shouldFlip;
        private Vector3 direction;
        private Vector2 previousPosition;
        
        // 데미지 처리
        private long _damage;
        
        public void Initialize(MetadataProjectile metadataProjectile)
        {
            if (metadataProjectile == null) return;
            DefaultEffect effect = metadataProjectile.Effect;
            StruckTableProjectile info = metadataProjectile.Info;
            effectProjectile = effect;
            // 충돌 후 처리해야 되기 때문에 무한 loop 로 설정
            effectProjectile.SetDuration(-1);
            
            struckTableProjectile = info;
            speed = info.MoveSpeed;
            arcHeight = info.ArcHeightMin;
            if (info.ArcHeightMin != info.ArcHeightMax)
            {
                arcHeight = Random.Range(info.ArcHeightMin, info.ArcHeightMax);
            }

            if (info.EffectScale > 0)
            {
                effectProjectile.SetScale(info.EffectScale);
            }
            struckTableEffect = TableLoaderManager.Instance.TableEffect.GetDataByUid(info.EffectUid);
            
            Rigidbody2D rigidbody2d = ComponentController.AddRigidbody2D(gameObject);
            rigidbody2d.bodyType = RigidbodyType2D.Kinematic;
            
            Vector2 offset = Vector2.zero;
            Vector2 size = Vector2.zero;
            if (struckTableProjectile != null && struckTableProjectile.ColliderSize != Vector2.zero)
            {
                size = struckTableProjectile.ColliderSize;
            }
            ComponentController.AddCapsuleCollider2D(gameObject, true, offset, size);
        }
        private void SetStartPoint()
        {
            startPoint = transform.position;
            if (fromCharacter)
            {
                startPoint = fromCharacter.gameObject.transform.position;
            }
            if (struckTableProjectile.StartPosition != Vector2.zero)
            {
                startPoint += struckTableProjectile.StartPosition;
            }
        }
        public void Launch(Vector2 targetPos)
        {
            targetObject = null;
            SetStartPoint();
            targetPoint = targetPos;
            journeyLength = Vector2.Distance(startPoint, targetPoint);
            startTime = Time.time;
            direction = (targetPoint - startPoint).normalized;
            UpdateEffectFlip();
            effectProjectile.SetRotation(targetPoint - startPoint, direction);
            transform.position = startPoint;
            initialized = true;
        }
        public void Launch(CharacterBase targetObj)
        {
            if (targetObj == null)
            {
                GcLogger.LogWarning("Projectile launched with null target!");
                Destroy(gameObject);
                return;
            }

            targetObject = targetObj;
            Launch(new Vector2(targetObj.transform.position.x, targetObj.GetRandomPositionYInHitArea()));
        }

        private void UpdateEffectFlip()
        {
            shouldFlip = false;
            
            if (struckTableEffect.DefaultDirection == ConfigCommon.DirectionType.Right && targetPoint.x < startPoint.x)
                shouldFlip = true;
            else if (struckTableEffect.DefaultDirection == ConfigCommon.DirectionType.Left && targetPoint.x > startPoint.x)
                shouldFlip = true;
            
            effectProjectile.SetFlip(shouldFlip);
        }
        private void Update()
        {
            if (!initialized) return;

            // todo 거리가 달라도 일정한 속도로 날아가게 
            float distCovered = (Time.time - startTime) * speed;
            float fraction = distCovered / journeyLength;
            if (fraction > 1f)
            {
                Destroy(gameObject);
                return;
            }
            Vector2 newPos = Vector2.Lerp(startPoint, targetPoint, fraction);

            if (arcHeight > 0f)
            {
                float height = arcHeight * 4 * (fraction - fraction * fraction);
                newPos.y += height;
            }
            // 이동 처리
            transform.position = newPos;
            
            // 방향 계산 → 회전 적용
            Vector2 moveDir = newPos - previousPosition;
            direction = (newPos - startPoint).normalized;
            if (moveDir.sqrMagnitude > 0.0001f) // 0 방지
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                // 기본 방향이 "왼쪽(-X 방향)"일 경우, 90도 보정
                if (direction.x < 0)
                {
                    angle += 180;
                }
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            previousPosition = newPos;
            
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
            if (!Camera.main) return false;
            // 현재 카메라의 뷰포트 내에 있는지 확인
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);
            return screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // GcLogger.Log(collision.name);
            if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                CharacterHitArea area = collision.GetComponent<CharacterHitArea>();
                if (area)
                {
                    OnHitTarget(area);
                }
            }
            else if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster)))
            {
                CharacterHitArea area = collision.GetComponent<CharacterHitArea>();
                if (area)
                {
                    OnHitTarget(area);
                }
            }
            else if (initialized && collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
            {
                GcLogger.Log("Projectile Destroy by MapGround");
                Destroy(gameObject);
            }
        }

        private void OnHitTarget(CharacterHitArea area)
        {
            // GcLogger.Log("Projectile hit target!");
            // TODO: 데미지, 이펙트 등
            ShowHitEffect();
            if (area)
            {
                area.target?.TakeDamage(_damage, fromCharacter.gameObject, SkillConstants.DamageType.Physic);
            }
        }

        private void ShowHitEffect()
        {
            // Hit 이펙트가 따로 있으면 Projectile 은 바로 Destroy 한다.
            if (struckTableProjectile.HitEffectUid > 0) 
            {
                Destroy(gameObject);
                var effect = EffectManager.CreateEffect(struckTableProjectile.HitEffectUid);
                if (!effect) return;
                effect.transform.position = transform.position;
                // 발사체가 flip 되면 hit 이펙트도 flip 처리
                effect.SetFlip(shouldFlip);
            }
            // Hit 이펙트가 따로 없으면, Effect 오브젝트의 End 애니메이션을 실행한다.
            else
            {
                effectProjectile.SetEnd();
            }
        }

        public void SetFromCharacter(CharacterBase characterBase)
        {
            fromCharacter = characterBase;
        }

        public void SetDamage(long damage)
        {
            _damage = damage;
        }
    }
}