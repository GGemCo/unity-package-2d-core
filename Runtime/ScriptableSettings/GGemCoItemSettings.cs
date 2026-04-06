using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Item.FileName, menuName = ConfigScriptableObject.Item.MenuName, order = ConfigScriptableObject.Item.Ordering)]
    public class GGemCoItemSettings : ScriptableObject
    {
        [Header("Drop Item Visual")]
        [Tooltip("item_visual 테이블에 데이터가 없을 때 사용할 기본 드랍 표시 방식")]
        public ItemConstants.DropVisualType defaultDropVisualType = ItemConstants.DropVisualType.Sprite;

        [Tooltip("item_visual 테이블 행이 없을 때 item.txt의 FileName을 사용한 Sprite 표시로 대체할지 여부")]
        public bool useSpriteFallbackWhenTableMissing = true;

        [Tooltip("Vfx 표시가 선택되었지만 Vfx Uid가 없거나 로드 실패 시 Sprite 표시로 대체할지 여부")]
        public bool useSpriteFallbackWhenVfxMissing = true;

        [Tooltip("Vfx 표시를 사용할 때 기존 SpriteRenderer를 숨길지 여부")]
        public bool hideSpriteRendererWhenUsingVfx = true;

        [Tooltip("item_visual 테이블에서 Scale이 0 이하일 때 사용할 기본 배율")]
        [Min(0.01f)] public float defaultVisualScale = 1f;

        [Tooltip("item_visual 테이블에서 OffsetY 값을 비워두었을 때 사용할 기본 Y 오프셋")]
        public float defaultVisualOffsetY = 0f;

        private void Reset()
        {
            defaultDropVisualType = ItemConstants.DropVisualType.Sprite;
            useSpriteFallbackWhenTableMissing = true;
            useSpriteFallbackWhenVfxMissing = true;
            hideSpriteRendererWhenUsingVfx = true;
            defaultVisualScale = 1f;
            defaultVisualOffsetY = 0f;
        }

        private void OnEnable()
        {
            if (defaultVisualScale <= 0f)
                defaultVisualScale = 1f;
        }
    }
}
