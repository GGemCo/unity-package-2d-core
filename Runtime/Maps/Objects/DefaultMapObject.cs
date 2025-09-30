using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    public class DefaultMapObject : MonoBehaviour
    {
        public Vector2 directionNormalize;

        protected virtual void Awake()
        {
            InitComponents();
            InitTagSortingLayer();
        }

        protected virtual void InitComponents()
        {
            
        }
        protected virtual void InitTagSortingLayer()
        {
            tag = ConfigTags.GetValue(ConfigTags.Keys.MapObject);
            if (GetComponent<TilemapRenderer>() != null)
            {
                GetComponent<TilemapRenderer>().sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.MapObject);
            }
        }

        public bool IsStatusDead()
        {
            return false;
        }

        public float GetCurrentMoveSpeed()
        {
            return 1.0f;
        }

        public float GetCurrentAttackSpeed()
        {
            return 1.0f;
        }

        public void OnAnimationCompleteAttack()
        {
        }

        public void OnAnimationCompleteAttackEnd()
        {
        }

        public void OnAnimationCompleteDead()
        {
        }

        public void Stop()
        {
        }

        public void SetIsStartFade(bool b)
        {
        }
    }
}