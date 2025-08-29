using UnityEngine;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 움직임 처리
    /// </summary>
    public class ControllerPlayer : CharacterBaseController
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
#if GGEMCO_2D_CONTROL
#else
            if (targetCharacter.IsStatusAttack()) return;
            if (targetCharacter.IsStatusDead()) return;
            targetCharacter.directionNormalize = Vector3.zero;
            
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) targetCharacter.directionNormalize += Vector3.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) targetCharacter.directionNormalize += Vector3.down;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) targetCharacter.directionNormalize += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) targetCharacter.directionNormalize += Vector3.right;
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) targetCharacter.directionNormalize += Vector3.up; 
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) targetCharacter.directionNormalize += Vector3.down; 
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) targetCharacter.directionNormalize += Vector3.left; 
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) targetCharacter.directionNormalize += Vector3.right; 
#endif

            targetCharacter.directionNormalize.Normalize();
#endif
        }
        /// <summary>
        /// 키보드 공격 처리
        /// </summary>
        private void HandleAttack()
        {
#if GGEMCO_2D_CONTROL
#else
            if (targetCharacter.IsStatusAttack()) return;
            if (targetCharacter.IsStatusDead()) return;
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKeyDown(KeyCode.Space))
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
#endif
            {
                targetCharacter.SetStatusAttack(); // 공격 중 상태 설정
                targetCharacter.directionNormalize = Vector3.zero; // 움직임 멈춤
                iCharacterAnimationController?.PlayAttackAnimation();
            }
#endif
        }
        private void Update()
        {
#if GGEMCO_2D_CONTROL
#else
            if (!CheckPossibleControl()) return;
            // 연출 중이면 
            if (_cutsceneManager.IsPlaying())
            {
                return;
            }
            
            HandleInput();
            HandleAttack();
            
            // 이동 상태 처리
            if (targetCharacter.directionNormalize != Vector3.zero)
            {
                Run();
            }
            // 정지 상태 처리
            else
            {
                Wait();
            }
#endif
        }
    }
}