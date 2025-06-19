using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 움직임 처리
    /// </summary>
    public class ControllerPlayer : CharacterController
    {
        CutsceneManager cutsceneManager;
        
        public void Initialize(CutsceneManager pcutsceneManager)
        {
            cutsceneManager = pcutsceneManager;
        }
        /// <summary>
        /// 키보드 입력 처리 
        /// </summary>
        private void HandleInput()
        {
            if (TargetCharacter.IsStatusAttack()) return;
            if (TargetCharacter.IsStatusDead()) return;
            TargetCharacter.direction = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) TargetCharacter.direction += Vector3.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) TargetCharacter.direction += Vector3.down;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) TargetCharacter.direction += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) TargetCharacter.direction += Vector3.right;

            TargetCharacter.direction.Normalize();
        }
        /// <summary>
        /// 키보드 공격 처리
        /// </summary>
        private void HandleAttack()
        {
            if (TargetCharacter.IsStatusAttack()) return;
            if (TargetCharacter.IsStatusDead()) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TargetCharacter.SetStatusAttack(); // 공격 중 상태 설정
                TargetCharacter.direction = Vector3.zero; // 움직임 멈춤
                ICharacterAnimationController?.PlayAttackAnimation();
            }
        }
        private void Update()
        {
            // 연출 중이면 
            if (cutsceneManager.IsPlaying())
            {
                return;
            }
            if (TargetCharacter.IsStatusMoveForce()) return;
            
            HandleInput();
            HandleAttack();
            
            // 이동 상태 처리
            if (TargetCharacter.direction != Vector3.zero)
            {
                Run();
            }
            // 정지 상태 처리
            else
            {
                Wait();
            }
        }
    }
}