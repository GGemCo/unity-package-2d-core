using UnityEngine;

namespace GGemCo2DCore
{
    public class CharacterPickUpPosition : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                GcLogger.LogError($"스프라이트 컴포넌트가 없습니다.");
                enabled = false;
                return;
            }
        }

        public void ChangePickUpSprite(Sprite sprite)
        {
            if (!_spriteRenderer || sprite == null) return;
            _spriteRenderer.sprite = sprite;
        }
    }
}