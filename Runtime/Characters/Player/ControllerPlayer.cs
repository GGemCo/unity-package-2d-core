using UnityEngine;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 움직임 처리
    /// </summary>
    public class ControllerPlayer : CharacterController
    {
        private CutsceneManager _cutsceneManager;
        
        public void Initialize(CutsceneManager cutsceneManager)
        {
            _cutsceneManager = cutsceneManager;
        }
        /// <summary>
        /// 키보드 입력 처리 
        /// </summary>
        private void HandleInput()
        {
            if (TargetCharacter.IsStatusAttack()) return;
            if (TargetCharacter.IsStatusDead()) return;
            TargetCharacter.direction = Vector3.zero;
            
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) TargetCharacter.direction += Vector3.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) TargetCharacter.direction += Vector3.down;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) TargetCharacter.direction += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) TargetCharacter.direction += Vector3.right;
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) TargetCharacter.direction += Vector3.up; 
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) TargetCharacter.direction += Vector3.down; 
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) TargetCharacter.direction += Vector3.left; 
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) TargetCharacter.direction += Vector3.right; 
#endif

            TargetCharacter.direction.Normalize();
        }
        /// <summary>
        /// 키보드 공격 처리
        /// </summary>
        private void HandleAttack()
        {
            if (TargetCharacter.IsStatusAttack()) return;
            if (TargetCharacter.IsStatusDead()) return;
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKeyDown(KeyCode.Space))
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
#endif
            {
                TargetCharacter.SetStatusAttack(); // 공격 중 상태 설정
                TargetCharacter.direction = Vector3.zero; // 움직임 멈춤
                ICharacterAnimationController?.PlayAttackAnimation();
            }
        }
        private void Update()
        {
            // 연출 중이면 
            if (_cutsceneManager.IsPlaying())
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