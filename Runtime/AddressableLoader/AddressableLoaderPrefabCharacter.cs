using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 프리팹 불러오기
    /// </summary>
    public class AddressableLoaderPrefabCharacter
    {
        private Dictionary<string, GameObject> _prefabCharacters = new Dictionary<string, GameObject>();
        private SceneGame _sceneGame;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
        }
        // 모든 몬스터 불러오기
        public async Task LoadCharacterByMap(StruckTableMap mapTableInfo)
        {
            try
            {
                _prefabCharacters.Clear();

                Dictionary<string, GameObject> prefabCharacters = await AddressableLoaderController.LoadByLabelAsync<GameObject>(ConfigAddressableMap.GetLabel(mapTableInfo.FolderName));
                foreach (var data in prefabCharacters)
                {
                    // Debug.Log($"Loading monster {data.Key}");
                    _prefabCharacters.Add(data.Key, data.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public GameObject GetCharacterNpc(int spineUid)
        {
            var info = TableLoaderManager.Instance.TableAnimation.GetDataByUid(spineUid);
            if (info == null) return null;
            string key = $"{ConfigAddressables.KeyCharacter}_Npc_{info.Uid}";
            return _prefabCharacters.GetValueOrDefault(key);
        }

        public GameObject GetCharacterMonster(int spineUid)
        {
            var info = TableLoaderManager.Instance.TableAnimation.GetDataByUid(spineUid);
            if (info == null) return null;
            string key = $"{ConfigAddressables.KeyCharacter}_Monster_{info.Uid}";
            return _prefabCharacters.GetValueOrDefault(key);
        }

        public void Release()
        {
            foreach (GameObject obj in _prefabCharacters.Values)
            {
                AddressableLoaderController.Release(obj);
            }
            _prefabCharacters.Clear();
        }
    }
}
