using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Npc 움직임 처리
    /// </summary>
    public class ControllerNpc : CharacterController
    {
        private Npc npc;
        private ICharacterAnimationController iCharacterAnimationController;
        private Vector2 minBounds, maxBounds; // 타일맵의 최소/최대 경계
        private Vector2 mapSize;
        
        protected override void Awake()
        {
            base.Awake();
            npc = GetComponent<Npc>();
            // 타일맵의 경계를 가져오는 코드 (직접 설정 가능)
            minBounds = new Vector2(0f, 0f); // 좌측 하단 경계
        }

        protected override void Start()
        {
            base.Start();
            iCharacterAnimationController = npc.CharacterAnimationController;
            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            mapSize.x = size.x;
            mapSize.y = size.y;
            iCharacterAnimationController?.PlayWaitAnimation();
        }
    }
}