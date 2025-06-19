using UnityEngine;

namespace GGemCo.Scripts
{
    public class UIBackgroundScroll : MonoBehaviour
    {
        public float scrollSpeedX = 0.002f;
        public float scrollSpeedY = 0.002f;
        public Material material;
        private Vector2 uvOffset = Vector2.zero;

        void Update()
        {
            if (gameObject.activeSelf != true) return;
            
            // 45도 방향으로 이동
            uvOffset += new Vector2(scrollSpeedX, scrollSpeedY) * Time.deltaTime;

            // Material의 텍스처 오프셋을 업데이트하여 이동 효과 적용
            material.mainTextureOffset = uvOffset;
        }
    }
}
