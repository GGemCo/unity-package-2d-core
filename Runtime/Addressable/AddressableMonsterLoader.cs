using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 몬스터 프리팹 불러오기
    /// </summary>
    public class AddressableMonsterLoader : DefaultAddressableLoader
    {
        private Dictionary<int, GameObject> _prefabMonsters = new Dictionary<int, GameObject>();

        // 모든 몬스터 불러오기
        public void LoadAllMonster()
        {
            _prefabMonsters.Clear();
            
            _ = LoadByLabel("GGemCo_Character_Monster",
                (dict) =>
                {
                    // 이후 처리
                    foreach (var data in dict)
                    {
                        // Debug.Log($"Loading monster {data.Key}");
                        _prefabMonsters.Add(data.Key, data.Value);
                    }
                });
        }

        public GameObject GetMonster(int uid)
        {
            return _prefabMonsters[uid];
        }
    }
}
