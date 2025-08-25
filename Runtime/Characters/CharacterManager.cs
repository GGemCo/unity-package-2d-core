using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 관리 매니저
    /// </summary>
    public class CharacterManager
    {
        // (신규) 외부 확장용 스폰/파괴 이벤트 — Core는 누가 구독하는지 모릅니다.
        public static event Action<CharacterBase> OnCharacterSpawned;   // 생성 직후 1회
        public static event Action<CharacterBase> OnCharacterDestroyed; // Destroy 직전 1회
        
        private readonly List<GameObject> _characters = new List<GameObject>();
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;
        private AnimationEventMediator _animationEventMediator;
        
        public void Initialize(TableNpc pTableNpc, TableMonster pTableMonster, TableAnimation pTableAnimation, AddressableLoaderPrefabCharacter addressableLoaderPrefabCharacter)
        {
            _tableNpc = pTableNpc;
            _tableMonster = pTableMonster;
            _tableAnimation = pTableAnimation;
            _addressableLoaderPrefabCharacter = addressableLoaderPrefabCharacter;
        }

        /// <summary>
        /// 캐릭터 만들기
        /// </summary>
        /// <param name="characterType"></param>
        /// <param name="animationController"></param>
        /// <param name="prefab"></param>
        /// <param name="regenData"></param>
        /// <returns></returns>
        private GameObject CreateCharacter(CharacterConstants.Type characterType,
            ConfigCommon.AnimationController animationController, GameObject prefab,
            CharacterRegenData regenData = null)
        {
            GameObject characterObj = Object.Instantiate(prefab);
            switch (characterType)
            {
                case CharacterConstants.Type.Player:
                    Player player = characterObj.AddComponent<Player>();
                    player.type = CharacterConstants.Type.Player;
                    break;
                case CharacterConstants.Type.Monster:
                    Monster monster = characterObj.AddComponent<Monster>();
                    monster.type = CharacterConstants.Type.Monster;
                    monster.CharacterRegenData = regenData;

                    break;
                case CharacterConstants.Type.Npc:
                    Npc npc = characterObj.AddComponent<Npc>();
                    npc.type = CharacterConstants.Type.Npc;
                    break;
            }

            ICharacterAnimationController iCharacterAnimationController = null;
#if GGEMCO_USE_SPINE
            if (animationController == ConfigCommon.AnimationController.Spine)
            {
                CharacterAnimationControllerSpine characterAnimationControllerSpine =
                    characterObj.AddComponent<CharacterAnimationControllerSpine>();
                iCharacterAnimationController =
                    characterAnimationControllerSpine.GetComponent<ICharacterAnimationController>();
                
                // Spine2dController 에 EventListener 설정
                var spineController = characterObj.GetComponent<Spine2dController>();
                if (spineController != null && _animationEventMediator != null)
                {
                    spineController.EventListener = _animationEventMediator;
                }
            }
#endif
            if (animationController == ConfigCommon.AnimationController.Sprite)
            {
                CharacterAnimationControllerSprite characterAnimationControllerSprite =
                    characterObj.AddComponent<CharacterAnimationControllerSprite>();
                iCharacterAnimationController =
                    characterAnimationControllerSprite.GetComponent<ICharacterAnimationController>();
                
                // Animator2dController 에 EventListener 설정
                var animatorController = characterObj.GetComponent<Animation2dController>();
                if (animatorController != null && _animationEventMediator != null)
                {
                    animatorController.EventListener = _animationEventMediator;
                }
            }

            if (iCharacterAnimationController == null)
            {
                GcLogger.LogError($"wrong animation controller. animationController: {animationController}");
                return null;
            }

            CharacterBase characterBase = characterObj.GetComponent<CharacterBase>();
            if (regenData != null)
            {
                characterBase.uid = regenData.Uid;
                characterBase.gameObject.transform.position =
                    new Vector3(regenData.x, regenData.y, characterObj.transform.position.z);
            }

            characterBase.CharacterAnimationController = iCharacterAnimationController;
            _characters.Add(characterObj);
            
            // (신규) 스폰 이벤트: 모든 초기화가 끝난 직후 알림
            OnCharacterSpawned?.Invoke(characterBase);
            
            return characterObj;
        }

        public async Task<GameObject> CreatePlayer()
        {
            try
            {
                string key = $"{ConfigAddressables.KeyPrefabPlayer}";
                GameObject prefab = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);

                if (prefab)
                    return CreateCharacter(CharacterConstants.Type.Player,
                        AddressableLoaderSettings.Instance.playerSettings.animationController, prefab);
                
                GcLogger.LogError("플레이어 프리팹이 없습니다. path:"+ConfigCommon.PathPlayerPrefab);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public GameObject CreateNpc(int uid, CharacterRegenData regenData = null, GameObject prefab = null)
        {
            if (uid <= 0) return null;
            var infoNpc = _tableNpc?.GetDataByUid(uid);
            if (infoNpc == null) return null;
            var animationInfo = _tableAnimation?.GetDataByUid(infoNpc.AnimationUid);
            if (animationInfo == null) return null;
            if (!prefab)
            {
                prefab = _addressableLoaderPrefabCharacter?.GetCharacterNpc(infoNpc.AnimationUid);
                if (!prefab) return null;
            }

            GameObject npc = CreateCharacter(CharacterConstants.Type.Npc, animationInfo.Controller, prefab, regenData);
            if (!npc) return null;
            
            npc.GetComponent<Npc>()?.SetScale(infoNpc.Scale);
            
            return npc;
        }
        public GameObject CreateMonster(int uid, CharacterRegenData regenData = null, GameObject prefab = null)
        {
            if (uid <= 0) return null;
            var infoMonster = _tableMonster?.GetDataByUid(uid);
            if (infoMonster == null) return null;
            var animationInfo = _tableAnimation?.GetDataByUid(infoMonster.AnimationUid);
            if (animationInfo == null) return null;
            if (!prefab)
            {
                prefab = _addressableLoaderPrefabCharacter?.GetCharacterMonster(infoMonster.AnimationUid);
                if (!prefab) return null;
            }
            
            GameObject monster = CreateCharacter(CharacterConstants.Type.Monster, animationInfo.Controller, prefab, regenData);
            if (!monster) return null;
            
            monster.GetComponent<Monster>()?.SetScale(infoMonster.Scale);
            
            return monster;
        }

        public GameObject CreateCharacter(CharacterConstants.Type type, int characterUid)
        {
            if (type == CharacterConstants.Type.Player)
            {
                _ = CreatePlayer();
            }
            return type switch
            {
                CharacterConstants.Type.Npc => CreateNpc(characterUid),
                CharacterConstants.Type.Monster => CreateMonster(characterUid),
                _ => null
            };
        }
        public void RemoveCharacter(GameObject character)
        {
            if (!_characters.Contains(character)) return;

            // (신규) 파괴 이벤트: 리스트 제거/Destroy 직전에 알림
            var ch = character != null ? character.GetComponent<CharacterBase>() : null;
            if (ch != null)
                OnCharacterDestroyed?.Invoke(ch);

            _characters.Remove(character);
            Object.Destroy(character.gameObject);
        }

        public void OnDestroy()
        {
        }
        public void SetAnimationEventMediator(AnimationEventMediator mediator)
        {
            _animationEventMediator = mediator;
        }
    }
}