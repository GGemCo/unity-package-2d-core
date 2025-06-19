using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 캐릭터 관리 매니저
    /// </summary>
    public class CharacterManager
    {
        private readonly List<GameObject> characters = new List<GameObject>();
        private TableNpc tableNpc;
        private TableMonster tableMonster;
        private TableAnimation tableAnimation;

        public void Initialize(TableNpc pTableNpc, TableMonster pTableMonster, TableAnimation pTableAnimation)
        {
            tableNpc = pTableNpc;
            tableMonster = pTableMonster;
            tableAnimation = pTableAnimation;
        }

        /// <summary>
        /// 캐릭터 만들기
        /// </summary>
        /// <param name="characterType"></param>
        /// <param name="prefab"></param>
        /// <param name="regenData"></param>
        /// <returns></returns>
        private GameObject CreateCharacter(CharacterConstants.Type characterType, GameObject prefab, CharacterRegenData regenData = null)
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
#if GGEMCO_USE_SPINE
            CharacterAnimationControllerSpine characterAnimationControllerSpine =
                characterObj.AddComponent<CharacterAnimationControllerSpine>();
            ICharacterAnimationController iCharacterAnimationController =
                characterAnimationControllerSpine.GetComponent<ICharacterAnimationController>();
#else
            CharacterAnimationControllerSprite characterAnimationControllerSprite =
             characterObj.AddComponent<CharacterAnimationControllerSprite>();
            ICharacterAnimationController iCharacterAnimationController =
             characterAnimationControllerSprite.GetComponent<ICharacterAnimationController>();
#endif
            CharacterBase characterBase = characterObj.GetComponent<CharacterBase>();
            if (regenData != null)
            {
                characterBase.uid = regenData.Uid;
                characterBase.gameObject.transform.position =
                        new Vector3(regenData.x, regenData.y, characterObj.transform.position.z);
            }

            characterBase.CharacterAnimationController = iCharacterAnimationController;
            characters.Add(characterObj);
            return characterObj;
        }

        public GameObject CreatePlayer()
        {
            GameObject prefab = Resources.Load<GameObject>(ConfigCommon.PathPlayerPrefab);
            if (prefab == null)
            {
                GcLogger.LogError("플레이어 프리팹이 없습니다. path:"+ConfigCommon.PathPlayerPrefab);
                return null;
            }
            return CreateCharacter(CharacterConstants.Type.Player, prefab);
        }

        public GameObject CreateNpc(int uid, CharacterRegenData regenData = null)
        {
            if (uid <= 0) return null;
            var infoNpc = tableNpc.GetDataByUid(uid);
            if (infoNpc == null) return null;
            GameObject prefab = tableAnimation.GetPrefab(infoNpc.SpineUid);
            if (prefab == null) return null;
            
            GameObject npc = CreateCharacter(CharacterConstants.Type.Npc, prefab, regenData);
            if (npc == null) return null;
            
            var info = tableNpc.GetDataByUid(uid);
            if (info == null) return null;
            
            npc.GetComponent<Npc>()?.SetScale(info.Scale);
            
            return npc;
        }

        public GameObject CreateMonster(int uid, CharacterRegenData regenData = null)
        {
            if (uid <= 0) return null;
            var infoMonster = tableMonster.GetDataByUid(uid);
            if (infoMonster == null) return null;
            GameObject prefab = tableAnimation.GetPrefab(infoMonster.SpineUid);
            if (prefab == null) return null;
            
            GameObject monster = CreateCharacter(CharacterConstants.Type.Monster, prefab, regenData);
            if (monster == null) return null;
            
            var info = tableMonster.GetDataByUid(uid);
            if (info == null) return null;
            
            monster.GetComponent<Monster>()?.SetScale(info.Scale);
            
            return monster;
        }

        public GameObject CreateCharacter(CharacterConstants.Type type, int characterUid)
        {
            return type switch
            {
                CharacterConstants.Type.Player => CreatePlayer(),
                CharacterConstants.Type.Npc => CreateNpc(characterUid),
                CharacterConstants.Type.Monster => CreateMonster(characterUid),
                _ => null
            };
        }
        public void RemoveCharacter(GameObject character)
        {
            if (!characters.Contains(character)) return;
            characters.Remove(character);
            Object.Destroy(character.gameObject);
        }

        public void OnDestroy()
        {
        }
    }
}