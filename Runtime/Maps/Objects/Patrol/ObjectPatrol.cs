using System;
using UnityEngine;

namespace GGemCo2DCore
{
    public class ObjectPatrol : DefaultMapObject
    {
        public PatrolData PatrolData;
        public int monsterUid;
        private BoxCollider2D _boxCollider2D;

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
            monsterUid = PatrolData.MonsterUid;
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

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = other.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            GcLogger.Log($"onTriggerEnter2D: {hitArea}");
            
        }
        public void OnTriggerExit2D(Collider2D other)
        {
            if (!other.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = other.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            GcLogger.Log($"onTriggerExit2D: {hitArea}");
        }
    }
}