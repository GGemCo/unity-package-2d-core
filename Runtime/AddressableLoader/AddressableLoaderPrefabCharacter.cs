using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables를 통해 캐릭터(몬스터/NPC) 프리팹을 로드하고 캐시하는 로더입니다.
    /// </summary>
    /// <remarks>
    /// - 맵 단위(라벨)로 일괄 로드하거나, 몬스터 UID 단위(키)로 단건 로드할 수 있습니다.
    /// - 로드된 프리팹은 내부 딕셔너리에 캐시되며, <see cref="Release"/> 호출 시 Addressables 릴리즈를 수행합니다.
    /// </remarks>
    public class AddressableLoaderPrefabCharacter
    {
        /// <summary>
        /// 로드된 캐릭터 프리팹 캐시입니다. (key: Addressables 키, value: 프리팹 GameObject)
        /// </summary>
        private readonly Dictionary<string, GameObject> _prefabCharacters = new Dictionary<string, GameObject>();

        /// <summary>
        /// 현재 씬 컨텍스트(게임 씬) 참조입니다.
        /// </summary>
        private SceneGame _sceneGame;

        /// <summary>
        /// 로더에 게임 씬 컨텍스트를 주입합니다.
        /// </summary>
        /// <param name="sceneGame">현재 게임 씬 컨텍스트입니다.</param>
        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
        }

        /// <summary>
        /// 지정된 맵 정보의 폴더명을 기반으로, 해당 맵에서 사용하는 캐릭터 프리팹을 라벨로 일괄 로드합니다.
        /// </summary>
        /// <param name="mapTableInfo">맵 폴더명 등 라벨 생성에 필요한 맵 테이블 정보입니다.</param>
        /// <returns>비동기 로드 작업을 나타내는 Task입니다.</returns>
        /// <exception cref="Exception">Addressables 로드 중 예외가 발생하면 재throw합니다.</exception>
        public async Task LoadCharacterByMap(StruckTableMap mapTableInfo)
        {
            try
            {
                // 맵 단위 로드는 "현재 맵에 필요한 프리팹 캐시"를 새로 구성하는 의미이므로 초기화합니다.
                _prefabCharacters.Clear();

                var label = ConfigAddressableMap.GetLabel(mapTableInfo.FolderName);
                Dictionary<string, GameObject> prefabCharacters =
                    await AddressableLoaderController.LoadByLabelAsync<GameObject>(label);

                foreach (var data in prefabCharacters)
                {
                    // Debug.Log($"Loading monster {data.Key}");
                    // 라벨 로더가 반환한 키를 그대로 캐시에 저장합니다.
                    _prefabCharacters.Add(data.Key, data.Value);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// Spine UID를 기반으로 NPC 캐릭터 프리팹을 캐시에서 조회합니다.
        /// </summary>
        /// <param name="spineUid">애니메이션(Spine) 데이터 조회에 사용하는 UID입니다.</param>
        /// <returns>캐시에 존재하면 NPC 프리팹을, 없으면 null을 반환합니다.</returns>
        public GameObject GetCharacterNpc(int spineUid)
        {
            var info = TableLoaderManager.Instance.GetAnimationData(spineUid);
            if (info == null) return null;

            string key = $"{ConfigAddressableKey.Character}_Npc_{info.Uid}";
            return _prefabCharacters.GetValueOrDefault(key);
        }

        /// <summary>
        /// Spine UID를 기반으로 몬스터 캐릭터 프리팹을 캐시에서 조회합니다.
        /// </summary>
        /// <param name="spineUid">애니메이션(Spine) 데이터 조회에 사용하는 UID입니다.</param>
        /// <returns>캐시에 존재하면 몬스터 프리팹을, 없으면 null을 반환합니다.</returns>
        public GameObject GetCharacterMonster(int spineUid)
        {
            var info = TableLoaderManager.Instance.GetAnimationData(spineUid);
            if (info == null) return null;

            string key = $"{ConfigAddressableKey.Character}_Monster_{info.Uid}";
            return _prefabCharacters.GetValueOrDefault(key);
        }

        /// <summary>
        /// 캐시에 저장된 모든 프리팹을 Addressables에서 릴리즈하고 캐시를 비웁니다.
        /// </summary>
        public void Release()
        {
            foreach (GameObject obj in _prefabCharacters.Values)
            {
                AddressableLoaderController.Release(obj);
            }

            _prefabCharacters.Clear();
        }

        /// <summary>
        /// 몬스터 UID에 해당하는 캐릭터 프리팹을 Addressables 키로 단건 로드하여 캐시에 저장합니다.
        /// </summary>
        /// <param name="monsterUid">몬스터 캐릭터를 식별하는 UID입니다.</param>
        /// <returns>비동기 로드 작업을 나타내는 Task입니다.</returns>
        /// <remarks>
        /// 이미 캐시에 존재하고 값이 null이 아니라면 재로드하지 않습니다.
        /// </remarks>
        /// <exception cref="Exception">Addressables 로드 중 예외가 발생하면 로깅 후 재throw합니다.</exception>
        public async Task LoadCharacterByMonsterUid(int monsterUid)
        {
            try
            {
                var key = $"{ConfigAddressableKey.Character}_Monster_{monsterUid}";

                // 이미 유효한 프리팹이 캐시에 있다면 그대로 사용합니다.
                if (_prefabCharacters.TryGetValue(key, out var prefabCharacter))
                {
                    if (prefabCharacter != null)
                        return;
                }

                prefabCharacter = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);
                if (prefabCharacter == null)
                {
                    GcLogger.LogError($"[CharacterLoader] Failed to load character prefab. key={key}");
                    return;
                }

                // Add는 “중복은 버그”일 때, dict[key] = value는 “중복은 자연스러운 흐름”일 때 사용합니다.
                // 여기서는 단건 로드가 반복 호출될 수 있으므로 인덱서 할당으로 갱신/삽입합니다.
                _prefabCharacters[key] = prefabCharacter;
            }
            catch (Exception e)
            {
                GcLogger.LogException(e);
                throw;
            }
        }
    }
}
