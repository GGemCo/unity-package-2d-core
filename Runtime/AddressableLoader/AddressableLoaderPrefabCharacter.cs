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
    /// - 맵 단위(라벨)로 일괄 로드하거나, 캐릭터 UID 단위(키)로 단건 로드할 수 있습니다.
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
        /// 캐릭터 타입과 애니메이션 UID를 기반으로 프리팹 Addressables 키를 생성합니다.
        /// </summary>
        /// <param name="type">캐릭터 타입입니다.</param>
        /// <param name="animationUid">애니메이션 UID입니다.</param>
        /// <returns>생성된 키를 반환하며, 지원하지 않는 타입이면 빈 문자열을 반환합니다.</returns>
        private static string BuildCharacterPrefabKey(CharacterConstants.Type type, int animationUid)
        {
            if (animationUid <= 0)
            {
                return string.Empty;
            }

            return type switch
            {
                CharacterConstants.Type.Npc => $"{ConfigAddressableKey.Character}_Npc_{animationUid}",
                CharacterConstants.Type.Monster => $"{ConfigAddressableKey.Character}_Monster_{animationUid}",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 캐릭터 타입/UID에서 프리팹 로드 키를 계산합니다.
        /// </summary>
        /// <param name="type">대상 캐릭터 타입입니다.</param>
        /// <param name="characterUid">대상 캐릭터 UID입니다.</param>
        /// <param name="key">계산된 Addressables 키입니다.</param>
        /// <returns>키 계산에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryResolveCharacterPrefabKey(
            CharacterConstants.Type type,
            int characterUid,
            out string key)
        {
            key = string.Empty;
            if (characterUid <= 0)
            {
                return false;
            }

            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            if (tableLoaderManager == null)
            {
                return false;
            }

            int animationUid = 0;
            switch (type)
            {
                case CharacterConstants.Type.Npc:
                    animationUid = tableLoaderManager.GetNpcData(characterUid, logIfMissing: false)?.AnimationUid ?? 0;
                    break;
                case CharacterConstants.Type.Monster:
                    animationUid = tableLoaderManager.GetMonsterData(characterUid, logIfMissing: false)?.AnimationUid ?? 0;
                    break;
                default:
                    return false;
            }

            key = BuildCharacterPrefabKey(type, animationUid);
            return !string.IsNullOrWhiteSpace(key);
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
                    // 라벨 로더가 반환한 키를 그대로 캐시에 저장합니다.
                    _prefabCharacters[data.Key] = data.Value;
                }
            }
            catch (Exception e)
            {
                GcLogger.LogException(e);
                throw;
            }
        }

        /// <summary>
        /// Spine UID(애니메이션 UID)를 기반으로 NPC 캐릭터 프리팹을 캐시에서 조회합니다.
        /// </summary>
        /// <param name="spineUid">애니메이션 데이터 조회에 사용하는 UID입니다.</param>
        /// <returns>캐시에 존재하면 NPC 프리팹을, 없으면 <see langword="null"/>을 반환합니다.</returns>
        public GameObject GetCharacterNpc(int spineUid)
        {
            var info = TableLoaderManager.Instance.GetAnimationData(spineUid, logIfMissing: false);
            if (info == null)
            {
                return null;
            }

            string key = BuildCharacterPrefabKey(CharacterConstants.Type.Npc, info.Uid);
            return _prefabCharacters.GetValueOrDefault(key);
        }

        /// <summary>
        /// Spine UID(애니메이션 UID)를 기반으로 몬스터 캐릭터 프리팹을 캐시에서 조회합니다.
        /// </summary>
        /// <param name="spineUid">애니메이션 데이터 조회에 사용하는 UID입니다.</param>
        /// <returns>캐시에 존재하면 몬스터 프리팹을, 없으면 <see langword="null"/>을 반환합니다.</returns>
        public GameObject GetCharacterMonster(int spineUid)
        {
            var info = TableLoaderManager.Instance.GetAnimationData(spineUid, logIfMissing: false);
            if (info == null)
            {
                return null;
            }

            string key = BuildCharacterPrefabKey(CharacterConstants.Type.Monster, info.Uid);
            return _prefabCharacters.GetValueOrDefault(key);
        }

        /// <summary>
        /// 캐릭터 타입/UID에 대응하는 프리팹이 캐시에 존재하도록 보장합니다.
        /// 캐시에 없으면 Addressables에서 로드한 뒤 캐시에 저장합니다.
        /// </summary>
        /// <param name="type">보장할 캐릭터 타입입니다. Npc/Monster만 지원합니다.</param>
        /// <param name="characterUid">보장할 캐릭터 UID입니다.</param>
        /// <returns>프리팹이 캐시에 준비되면 <see langword="true"/>를 반환합니다.</returns>
        public async Task<bool> EnsureCharacterPrefabLoaded(CharacterConstants.Type type, int characterUid)
        {
            if (!TryResolveCharacterPrefabKey(type, characterUid, out string key))
            {
                GcLogger.LogError(
                    $"[CharacterLoader] Failed to resolve prefab key. type={type}, uid={characterUid}");
                return false;
            }

            if (_prefabCharacters.TryGetValue(key, out GameObject cachedPrefab) && cachedPrefab != null)
            {
                return true;
            }

            GameObject loadedPrefab = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);
            if (loadedPrefab == null)
            {
                GcLogger.LogError(
                    $"[CharacterLoader] Failed to load character prefab. type={type}, uid={characterUid}, key={key}");
                return false;
            }

            _prefabCharacters[key] = loadedPrefab;
            return true;
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
        /// 몬스터 UID에 해당하는 캐릭터 프리팹이 캐시에 존재하도록 보장합니다.
        /// </summary>
        /// <param name="monsterUid">몬스터 캐릭터를 식별하는 UID입니다.</param>
        /// <returns>비동기 로드 작업을 나타내는 Task입니다.</returns>
        /// <exception cref="Exception">Addressables 로드 중 예외가 발생하면 로깅 후 재throw합니다.</exception>
        public async Task LoadCharacterByMonsterUid(int monsterUid)
        {
            try
            {
                bool loaded = await EnsureCharacterPrefabLoaded(CharacterConstants.Type.Monster, monsterUid);
                if (!loaded)
                {
                    if (TryResolveCharacterPrefabKey(
                            CharacterConstants.Type.Monster,
                            monsterUid,
                            out string key))
                    {
                        GcLogger.LogError(
                            $"[CharacterLoader] Failed to ensure monster prefab. uid={monsterUid}, key={key}");
                    }
                    else
                    {
                        GcLogger.LogError(
                            $"[CharacterLoader] Failed to ensure monster prefab. uid={monsterUid}");
                    }
                }
            }
            catch (Exception e)
            {
                GcLogger.LogException(e);
                throw;
            }
        }
    }
}
