﻿using System;
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
        private readonly List<GameObject> _characters = new List<GameObject>();
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;

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
            _characters.Add(characterObj);
            return characterObj;
        }

        public async Task<GameObject> CreatePlayer()
        {
            try
            {
                string key = $"{ConfigAddressables.KeyPrefabPlayer}";
                GameObject prefab = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);
                if (prefab) return CreateCharacter(CharacterConstants.Type.Player, prefab);
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
            if (!prefab)
            {
                prefab = _addressableLoaderPrefabCharacter?.GetCharacterNpc(infoNpc.SpineUid);
                if (!prefab) return null;
            }

            GameObject npc = CreateCharacter(CharacterConstants.Type.Npc, prefab, regenData);
            if (!npc) return null;
            
            var info = _tableNpc?.GetDataByUid(uid);
            if (info == null) return null;
            
            npc.GetComponent<Npc>()?.SetScale(info.Scale);
            
            return npc;
        }
        public GameObject CreateMonster(int uid, CharacterRegenData regenData = null, GameObject prefab = null)
        {
            if (uid <= 0) return null;
            var infoMonster = _tableMonster?.GetDataByUid(uid);
            if (infoMonster == null) return null;
            if (!prefab)
            {
                prefab = _addressableLoaderPrefabCharacter?.GetCharacterMonster(infoMonster.SpineUid);
                if (!prefab) return null;
            }
            
            GameObject monster = CreateCharacter(CharacterConstants.Type.Monster, prefab, regenData);
            if (!monster) return null;
            
            var info = _tableMonster?.GetDataByUid(uid);
            if (info == null) return null;
            
            monster.GetComponent<Monster>()?.SetScale(info.Scale);
            
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
            _characters.Remove(character);
            Object.Destroy(character.gameObject);
        }

        public void OnDestroy()
        {
        }
    }
}