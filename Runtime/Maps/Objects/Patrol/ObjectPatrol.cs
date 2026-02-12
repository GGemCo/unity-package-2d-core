using UnityEngine;

namespace GGemCo2DCore
{
    public class ObjectPatrol : DefaultMapObject
    {
        public PatrolData PatrolData;
        public GameObject parentMonsterObject;
        
        private BoxCollider2D _boxCollider2D;
        private Monster _parentMonster;

        public void SetParentMonster(GameObject value)
        {
            parentMonsterObject = value;  
            _parentMonster = parentMonsterObject.GetComponent<Monster>();
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

        public void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null) return;
            if (_parentMonster.IsStatusDead()) return;

            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            
            // Player 전투 상태로
            Player player = collision.GetComponentInParent<Player>();
            if (player == null) return;
            player.SetBattleStatusInBattle();
            
            // 몬스터 전투 상태로
            if (!parentMonsterObject)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다. ");
                return;
            }
            // 몬스터가 데미지를 입었을 때와 같은 처리를 한다.
            _parentMonster.OnDamage(player.gameObject);
        }
        public void OnTriggerExit2D(Collider2D collision)
        {
            if (collision == null) return;
            if (_parentMonster.IsStatusDead()) return;

            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            
            // 플레이어 전투 종료 상태로
            Player player = collision.GetComponentInParent<Player>();
            if (player == null) return;
            player.SetBattleStatusNone();
            
            // 몬스터 전투 종료 상태로
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