using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo.Scripts
{
    public class ObjectWall : DefaultMapObject
    {
        protected override void InitComponents()
        {
            base.InitComponents();
            if (GetComponent<TilemapCollider2D>() == null)
            {
                ComponentController.AddTileMapCollider2D(gameObject, false, Vector2.zero);
            }
        }
        protected override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            gameObject.layer = LayerMask.NameToLayer(ConfigLayer.GetValue(ConfigLayer.Keys.TileMapWall));
        }
    }
}