using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 캐릭터 이동
    /// </summary>
    public class CharacterAnimationController : CutsceneDefaultController, ICutsceneController
    {
        private Camera cam;
        
        private bool isFollowTarget;
        private CharacterConstants.Type characterType;
        private int characterUid;
        private float characterScale;
        private Vec2 spawnPosition;
        private bool isFlip;
        
        private string animationName;
        private bool animationLoop;
        private float animationTimeScale;
        
        private float timer;
        private float duration;
        private bool isAnimation;

        private Transform target;
        private CharacterBase targetCharacter;

        public CharacterAnimationController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        private void SetParameter(CutsceneEvent evt)
        {
            duration = evt.duration;
            var data = evt.characterAnimation;
            isFollowTarget = data.isFollowTarget;
            characterType = data.characterType;
            characterUid = data.characterUid;
            characterScale = data.characterScale;
            spawnPosition = data.spawnPosition;
            isFlip = data.isFlip;
            
            animationName = data.animationName;
            animationLoop = data.animationLoop;
            animationTimeScale = data.animationTimeScale;
        }
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimation) yield break;
            SetParameter(evt);
            
            Transform character = GetTargetTransform(characterType, characterUid);
            // 현재 맵에서 없으면 스폰한다 
            if (character == null)
            {
                character = CutsceneManager.GetCharacter(characterType, characterUid);
                if (character == null)
                {
                    character = SceneGame.Instance.CharacterManager.CreateCharacter(characterType, characterUid)?.transform;
                    if (character == null) yield break;

                    if (spawnPosition.ToVector2() != Vector2.zero)
                    {
                        character.transform.position = spawnPosition.ToVector2();
                    }

                    character.transform.SetParent(SceneGame.Instance.mapManager.GetCurrentMap()?.transform);
                    
                    CharacterBase characterBase = character.GetComponent<CharacterBase>();
                    characterBase.uid = characterUid;
                    // Awake, Start 함수가 호출되게 하기 위해 추가
                    yield return null;
                    character.gameObject.SetActive(false);
                    CutsceneManager.AddCharacter(characterType, characterUid, character.gameObject);
                }
            }
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimation) return;
            SetParameter(evt);
            
            target = GetTargetTransform(characterType, characterUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(characterType, characterUid);
                if (target == null)
                {
                    GcLogger.LogError("이동 시킬 캐릭터가 없습니다. type: " + characterType + "/ uid: " + characterUid);
                    return;
                }
            }
            if (target.gameObject.activeSelf == false)
            {
                target.gameObject.SetActive(true);
            }
            
            if (target != null)
            {
                targetCharacter = target.GetComponent<CharacterBase>();
                // 크기 조정
                if (characterScale > 0)
                {
                    targetCharacter?.SetScale(characterScale);
                }
                // 위치 조정
                if (spawnPosition.ToVector2() != Vector2.zero)
                {
                    target.transform.position = spawnPosition.ToVector2();
                }
                targetCharacter?.SetFlip(isFlip);
                // 카메라가 따라가야하는 타겟 설정
                if (isFollowTarget)
                {
                    SceneGame.Instance.cameraManager.SetFollowTarget(target);
                }
                targetCharacter?.SetStatusMoveForce();
                if (animationName != "")
                {
                    targetCharacter?.CharacterAnimationController?.PlayCharacterAnimation(animationName,
                        animationLoop, animationTimeScale);
                }
            }
            timer = 0f;
            isAnimation = true;
        }
        public void Update()
        {
            if (!isAnimation) return;
            
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            
            if (timer > duration)
            {
                Stop();
            }
        }
        public void Stop()
        {
            targetCharacter?.Stop();
            isAnimation = false;
        }
        public void End()
        {
        }
    }
}
