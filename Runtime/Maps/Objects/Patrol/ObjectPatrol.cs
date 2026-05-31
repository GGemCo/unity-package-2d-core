using UnityEngine;

namespace GGemCo2DCore
{
    public class ObjectPatrol : DefaultMapObject
    {
        public PatrolData PatrolData;
        public GameObject parentMonsterObject;
        
        private BoxCollider2D _boxCollider2D;
        private Monster _parentMonster;

        /// <summary>
        /// 패트롤 영역을 소유한 몬스터를 설정합니다.
        /// </summary>
        /// <param name="value">패트롤 영역과 연결할 몬스터 오브젝트입니다.</param>
        public void SetParentMonster(GameObject value)
        {
            parentMonsterObject = value;  
            _parentMonster = parentMonsterObject != null ? parentMonsterObject.GetComponent<Monster>() : null;
        } 
        protected override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.MapObjectPatrol);
            GetComponent<SpriteRenderer>().sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.MapObject);
        }
        protected override void InitComponents()
        {
            base.InitComponents();
            _boxCollider2D = GetComponent<BoxCollider2D>();
            if (_boxCollider2D == null)
            {
                _boxCollider2D = ComponentController.AddBoxCollider2D(gameObject, false, Vector2.zero, Vector2.zero);
            }
            _boxCollider2D.isTrigger = true;
        }
        
        private void Start()
        {
            InitializeByData();
        }
        
        /// <summary>
        /// PatrolData 값을 기준으로 패트롤 영역의 위치, 회전, Collider 크기를 적용합니다.
        /// </summary>
        private void InitializeByData()
        {
            if (PatrolData == null) return;
            transform.position = new Vector3(PatrolData.X, PatrolData.Y, PatrolData.Z);
            transform.eulerAngles = new Vector3(PatrolData.RotationX, PatrolData.RotationY, PatrolData.RotationZ);
            _boxCollider2D.size = new Vector2(PatrolData.BoxColliderSizeX, PatrolData.BoxColliderSizeY);
            _boxCollider2D.offset = new Vector2(PatrolData.BoxColliderOffsetX, PatrolData.BoxColliderOffsetY);
        }
        
        public void InitializeByMapEditor()
        {
            InitTagSortingLayer();
            InitComponents();
            InitializeByData();
        }

        /// <summary>
        /// 플레이어가 패트롤 영역에 들어오면 소유 몬스터에게 감지 사실만 전달합니다.
        /// </summary>
        /// <param name="collision">패트롤 영역에 진입한 Collider입니다.</param>
        /// <remarks>
        /// 패트롤 오브젝트는 감지 역할만 담당합니다.
        /// 선공/후공 전투 시작 여부는 몬스터의 <see cref="CharacterConstants.AttackType"/> 정책에서 결정합니다.
        /// </remarks>
        public void OnTriggerEnter2D(Collider2D collision)
        {
            if (!TryGetDetectedPlayer(collision, out Player player)) return;
            if (!TryGetParentMonster(out Monster monster)) return;

            monster.OnDetectedPlayerByPatrol(player);
        }

        /// <summary>
        /// 플레이어가 패트롤 영역에서 나가면 소유 몬스터에게 감지 이탈 사실만 전달합니다.
        /// </summary>
        /// <param name="collision">패트롤 영역에서 이탈한 Collider입니다.</param>
        /// <remarks>
        /// 전투 종료 여부는 몬스터가 전투 시작 원인을 기준으로 판단합니다.
        /// 플레이어가 몬스터를 공격해서 시작된 전투는 패트롤 영역 이탈만으로 종료하지 않습니다.
        /// </remarks>
        public void OnTriggerExit2D(Collider2D collision)
        {
            if (!TryGetDetectedPlayer(collision, out Player player)) return;
            if (!TryGetParentMonster(out Monster monster, allowDeadMonster: true)) return;

            monster.OnLostPlayerByPatrol(player);
        }

        /// <summary>
        /// 패트롤 Trigger와 충돌한 Collider에서 플레이어를 찾습니다.
        /// </summary>
        /// <param name="collision">패트롤 영역과 충돌한 Collider입니다.</param>
        /// <param name="player">찾은 플레이어 컴포넌트입니다.</param>
        /// <returns>유효한 플레이어를 찾으면 <see langword="true"/>입니다.</returns>
        private static bool TryGetDetectedPlayer(Collider2D collision, out Player player)
        {
            player = null;
            if (collision == null) return false;

            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return false;

            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return false;

            player = collision.GetComponentInParent<Player>();
            return player != null;
        }

        /// <summary>
        /// 패트롤 영역을 소유한 몬스터를 반환합니다.
        /// </summary>
        /// <param name="monster">패트롤 영역을 소유한 몬스터입니다.</param>
        /// <param name="allowDeadMonster">사망한 몬스터도 반환할지 여부입니다.</param>
        /// <returns>유효한 몬스터를 찾으면 <see langword="true"/>입니다.</returns>
        private bool TryGetParentMonster(out Monster monster, bool allowDeadMonster = false)
        {
            monster = _parentMonster;
            if (monster == null && parentMonsterObject != null)
            {
                monster = parentMonsterObject.GetComponent<Monster>();
                _parentMonster = monster;
            }

            if (monster == null)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다. ");
                return false;
            }

            if (!allowDeadMonster && monster.IsStatusDead())
            {
                return false;
            }

            return true;
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            Gizmos.color = Color.green;

            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(col.offset, col.size);

            Gizmos.matrix = matrix;
        }
#endif

    }
}
