using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에 배치된 몬스터 패트롤 영역 데이터를 보관하고 에디터/런타임 배치 정보를 동기화합니다.
    /// </summary>
    /// <remarks>
    /// 전투 시작은 <see cref="MonsterDetectionSensor2D"/>와 Threat 시스템이 담당합니다.
    /// 이 오브젝트는 플레이어 Trigger 진입/이탈로 전투 Threat를 등록하지 않습니다.
    /// </remarks>
    public class ObjectPatrol : DefaultMapObject
    {
        public PatrolData patrolData;
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
            _parentMonster?.ConfigureEncounter(patrolData);
        }

        /// <inheritdoc />
        protected override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.MapObjectPatrol);
            GetComponent<SpriteRenderer>().sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.MapObject);
        }

        /// <inheritdoc />
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
        
        /// <summary>
        /// 런타임 생성 이후 직렬화된 PatrolData를 실제 Transform과 Collider에 반영합니다.
        /// </summary>
        private void Start()
        {
            InitializeByData();
        }
        
        /// <summary>
        /// PatrolData 값을 기준으로 패트롤 영역의 위치, 회전, Collider 크기를 적용합니다.
        /// </summary>
        private void InitializeByData()
        {
            if (patrolData == null) return;
            transform.position = new Vector3(patrolData.x, patrolData.y, patrolData.z);
            transform.eulerAngles = new Vector3(patrolData.rotationX, patrolData.rotationY, patrolData.rotationZ);
            _boxCollider2D.size = new Vector2(patrolData.boxColliderSizeX, patrolData.boxColliderSizeY);
            _boxCollider2D.offset = new Vector2(patrolData.boxColliderOffsetX, patrolData.boxColliderOffsetY);

            // 런타임 로더가 부모 연결 이후 PatrolData를 주입하는 경우에도 최신 Encounter ID를 반영합니다.
            _parentMonster?.ConfigureEncounter(patrolData);
        }
        
        /// <summary>
        /// 맵 에디터에서 생성된 패트롤 오브젝트의 기본 태그, Collider, 직렬화 데이터를 초기화합니다.
        /// </summary>
        public void InitializeByMapEditor()
        {
            InitTagSortingLayer();
            InitComponents();
            InitializeByData();
        }
#if UNITY_EDITOR
        /// <summary>
        /// 에디터 Scene 뷰에서 패트롤 영역 Collider를 표시합니다.
        /// </summary>
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
