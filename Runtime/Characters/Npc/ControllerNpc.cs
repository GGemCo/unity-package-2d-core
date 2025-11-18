using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Npc 움직임 처리
    /// </summary>
    public class ControllerNpc : CharacterBaseController
    {
        private Npc npc;
        
        protected override void Awake()
        {
            base.Awake();
            npc = GetComponent<Npc>();
            // 타일맵의 경계를 가져오는 코드 (직접 설정 가능)
            minBounds = new Vector2(0f, 0f); // 좌측 하단 경계
        }
    }
}