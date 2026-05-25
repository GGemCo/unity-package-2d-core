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
        /// 플레이어가 살아있는 몬스터의 패트롤 영역에 들어오면 전투 상태와 자동 이동 추적 대상을 설정합니다.
        /// </summary>
        /// <param name="collision">패트롤 영역에 진입한 Collider입니다.</param>
        public void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null) return;
            if (_parentMonster == null || _parentMonster.IsStatusDead()) return;

            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            
            // Player 전투 상태로
            Player player = collision.GetComponentInParent<Player>();
            if (player == null) return;
            player.SetBattleStatusInBattle();
            player.SetAutoMoveTargetMonster(parentMonsterObject);
            
            // 몬스터 전투 상태로
            if (!parentMonsterObject)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다. ");
                return;
            }
            // 몬스터가 데미지를 입었을 때와 같은 처리를 한다.
            _parentMonster.OnDamage(player.gameObject);
        }

        /// <summary>
        /// 플레이어가 패트롤 영역에서 나가면 플레이어 쪽 전투/자동이동 추적 상태를 우선 정리합니다.
        /// 몬스터가 이미 사망한 뒤에도 플레이어 상태는 반드시 정리되어야 하므로, 몬스터 사망 여부는 플레이어 정리 이후에만 사용합니다.
        /// </summary>
        /// <param name="collision">패트롤 영역에서 이탈한 Collider입니다.</param>
        public void OnTriggerExit2D(Collider2D collision)
        {
            if (collision == null) return;

            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            
            // 플레이어 전투 종료 상태로
            Player player = collision.GetComponentInParent<Player>();
            if (player == null) return;
            player.SetBattleStatusNone();
            player.ClearAutoMoveTargetMonster(parentMonsterObject);
            
            // 몬스터가 이미 사망했거나 연결이 끊긴 경우, 플레이어 상태 정리까지만 수행한다.
            if (_parentMonster == null || _parentMonster.IsStatusDead()) return;
            if (!parentMonsterObject)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다. ");
                return;
            }
            _parentMonster.SetAggro(false);
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
