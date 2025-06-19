using UnityEngine;

namespace GGemCo.Scripts
{
    public class ObjectWarp : DefaultMapObject
    {
        public WarpData WarpData;
        public int toMapUid; // 워프할 uid
        public Vector3 toMapPlayerSpawnPosition; // 워프될 곳에 플레이어가 스폰될 위치
        private BoxCollider2D boxCollider2D;

        protected override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.MapObjectWarp);
            GetComponent<SpriteRenderer>().sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.MapObject);
        }
        protected override void InitComponents()
        {
            base.InitComponents();
            boxCollider2D = GetComponent<BoxCollider2D>();
            if (boxCollider2D == null)
            {
                boxCollider2D = ComponentController.AddBoxCollider2D(gameObject, false, Vector2.zero, Vector2.zero);
            }
        }

        private void Start()
        {
            InitializeByWarpData();
        }

        private void InitializeByWarpData()
        {
            if (WarpData == null) return;
            toMapUid = WarpData.ToMapUid;
            toMapPlayerSpawnPosition = new Vector3(WarpData.ToX, WarpData.ToY, WarpData.ToZ);
            transform.position = new Vector3(WarpData.x, WarpData.y, WarpData.z);
            transform.eulerAngles = new Vector3(WarpData.RotationX, WarpData.RotationY, WarpData.RotationZ);
            boxCollider2D.size = new Vector2(WarpData.BoxColliderSizeX, WarpData.BoxColliderSizeY);
            boxCollider2D.offset = new Vector2(WarpData.BoxColliderOffsetX, WarpData.BoxColliderOffsetY);
        }
    }
}