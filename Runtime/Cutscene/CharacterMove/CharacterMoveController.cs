using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 캐릭터 이동
    /// </summary>
    public class CharacterMoveController : CutsceneDefaultController, ICutsceneController
    {
        private Camera cam;
        private Vector2 startPosition, endPosition;
        private float characterMoveStep;
        private float characterMoveSpeed;
        private float distance;
        private float timer;
        private bool isMoving;
        private bool isFollowTarget;
        
        private float duration;

        private Transform target;
        private CharacterBase targetCharacter;

        public CharacterMoveController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterMove) yield break;
            var data = evt.characterMove;
            
            Transform character = GetTargetTransform(data.characterType, data.characterUid);
            // 현재 맵에서 없으면 스폰한다 
            if (character == null)
            {
                character = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
                if (character == null)
                {
                    character = SceneGame.Instance.CharacterManager.CreateCharacter(data.characterType, data.characterUid)?.transform;
                    if (character == null) yield break;
                    
                    character.transform.position = startPosition;
                    character.transform.SetParent(SceneGame.Instance.mapManager.GetCurrentMap()?.transform);
                    character.position = startPosition;
                    
                    CharacterBase characterBase = character.GetComponent<CharacterBase>();
                    characterBase.uid = data.characterUid;
                    // Awake, Start 함수가 호출되게 하기 위해 추가
                    yield return null;
                    character.gameObject.SetActive(false);
                    CutsceneManager.AddCharacter(data.characterType, data.characterUid, character.gameObject);
                }
            }
            
            // 캐릭터 타겟 찾기 같은 준비
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterMove) return;
            duration = evt.duration;
            var data = evt.characterMove;
            isFollowTarget = data.isFollowTarget;
            target = GetTargetTransform(data.characterType, data.characterUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
                if (target == null)
                {
                    GcLogger.LogError("이동 시킬 캐릭터가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                    return;
                }
            }
            if (target.gameObject.activeSelf == false)
            {
                target.gameObject.SetActive(true);
            }

            startPosition = data.startPosition.ToVector2();
            endPosition = data.endPosition.ToVector2();
            if (startPosition == Vector2.zero)
            {
                startPosition = target.position;
            }
            distance = Vector2.Distance(startPosition, endPosition);
            
            if (target != null)
            {
                targetCharacter = target.GetComponent<CharacterBase>();
                // step 적용
                characterMoveStep = AddressableSettingsLoader.Instance.playerSettings.statMoveStep;
                if (data.characterType != CharacterConstants.Type.Player)
                {
                    characterMoveStep = TableLoaderManager.Instance.GetCharacterMoveStep(data.characterType, data.characterUid);
                }
                // 이동 속도
                if (data.characterMoveSpeed > 0)
                {
                    targetCharacter?.SetCurrentMoveSpeed(data.characterMoveSpeed);
                    characterMoveSpeed = data.characterMoveSpeed;
                }
                // 크기 조정
                if (data.characterScale > 0)
                {
                    targetCharacter?.SetScale(data.characterScale);
                }
                // 카메라가 따라가야하는 타겟 설정
                if (isFollowTarget)
                {
                    SceneGame.Instance.cameraManager.SetFollowTarget(target);
                }
                targetCharacter?.SetStatusMoveForce();
                targetCharacter?.CharacterAnimationController?.PlayRunAnimation();
            }

            duration = distance / (characterMoveStep * (characterMoveSpeed / 100f));
            
            timer = 0f;
            UpdateFacing();
            isMoving = true;
        }
        public void Update()
        {
            if (target == null || !isMoving) return;

            timer += Time.deltaTime;
            float t = timer * characterMoveStep * (characterMoveSpeed / 100f) / distance;
            t = Mathf.Clamp01(t);

            Vector2 interpolated = Vector2.Lerp(startPosition, endPosition, t);
            target.position = new Vector3(interpolated.x, interpolated.y, target.position.z);

            if (timer > duration)
            {
                Stop();
            }
        }
        private void UpdateFacing()
        {
            if (target == null) return;

            Vector2 direction = endPosition - startPosition;

            // 좌우만 처리하는 경우
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                bool movingRight = direction.x > 0f;
                bool defaultIsRight = targetCharacter?.characterFacing == CharacterConstants.CharacterFacing.Right;

                bool shouldFlip = (movingRight != defaultIsRight);
                targetCharacter?.SetFlip(shouldFlip);
            }

            // 상하 전환도 필요하다면 추가 구현 가능
        }
        public void Stop()
        {
            targetCharacter?.Stop();
            isMoving = false;
        }
        public void End()
        {
        }
    }
}
