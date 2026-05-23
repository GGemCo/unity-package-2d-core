using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 생성/파괴, 애니메이션 컨트롤러 부착, 스폰/파괴 이벤트 브로드캐스트를 담당하는 매니저
    /// - Player/Monster: 공용 생성 경로 (CreateCharacter)
    /// - NPC: 세부 타입(Npc, NpcObject 등) 분기 전용 경로 (CreateNpc)
    /// - Monster: 재생성 비용을 줄이기 위해 풀 기반 재사용 경로(Rent/Return)를 지원합니다.
    /// </summary>
    public class CharacterManager
    {
        public static event Action<CharacterBase> OnCharacterSpawned;
        public static event Action<CharacterBase> OnCharacterDestroyed;

        private readonly List<GameObject> _characters = new List<GameObject>();
        private readonly Dictionary<int, Stack<Monster>> _monsterPoolByUid = new Dictionary<int, Stack<Monster>>();

        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;
        private AnimationEventMediator _animationEventMediator;
        private Transform _monsterPoolRoot;

        public void Initialize(
            TableNpc pTableNpc,
            TableMonster pTableMonster,
            TableAnimation pTableAnimation,
            AddressableLoaderPrefabCharacter addressableLoaderPrefabCharacter)
        {
            _tableNpc = pTableNpc;
            _tableMonster = pTableMonster;
            _tableAnimation = pTableAnimation;
            _addressableLoaderPrefabCharacter = addressableLoaderPrefabCharacter;
        }

        private Transform EnsureMonsterPoolRoot()
        {
            if (_monsterPoolRoot != null)
                return _monsterPoolRoot;

            var go = GameObject.Find("__MonsterPoolRoot__");
            if (go == null)
            {
                go = new GameObject("__MonsterPoolRoot__");
                go.SetActive(false);
            }

            _monsterPoolRoot = go.transform;
            return _monsterPoolRoot;
        }

        private GameObject CreateCharacter(
            CharacterConstants.Type characterType,
            ConfigCommon.AnimationController animationController,
            GameObject prefab,
            CharacterRegenData regenData = null)
        {
            if (prefab == null)
            {
                GcLogger.LogError($"CreateCharacter failed: prefab is null. type:{characterType}");
                return null;
            }

            GameObject characterObj = Object.Instantiate(prefab);

            try
            {
                switch (characterType)
                {
                    case CharacterConstants.Type.Player:
                    {
                        var player = characterObj.AddComponent<Player>();
                        player.type = CharacterConstants.Type.Player;
                        break;
                    }
                    case CharacterConstants.Type.Monster:
                    {
                        var monster = characterObj.AddComponent<Monster>();
                        monster.type = CharacterConstants.Type.Monster;
                        monster.CharacterRegenData = regenData;
                        monster.SetPoolManaged(true);
                        break;
                    }
                    case CharacterConstants.Type.Npc:
                        GcLogger.LogWarning("CreateCharacter(Type.Npc) is not supported. Use CreateNpc instead.");
                        Object.Destroy(characterObj);
                        return null;
                }

                var iAnim = SetupAnimationController(characterObj, animationController);
                if (iAnim == null)
                {
                    GcLogger.LogError($"CreateCharacter failed: animation controller is null. type:{characterType}, controller:{animationController}");
                    Object.Destroy(characterObj);
                    return null;
                }

                var characterBase = characterObj.GetComponent<CharacterBase>();
                if (characterBase == null)
                {
                    GcLogger.LogError("CreateCharacter failed: CharacterBase not found on prefab.");
                    Object.Destroy(characterObj);
                    return null;
                }

                if (regenData != null)
                {
                    characterBase.uid = regenData.Uid;
                    characterObj.transform.position = new Vector3(
                        regenData.x, regenData.y, characterObj.transform.position.z);
                }

                characterBase.CharacterAnimationController = iAnim;
                characterBase.RefreshCharacterBodyCollision();
                TrySetupSpriteWhiteOverlay(characterType, characterObj, characterBase);

                _characters.Add(characterObj);
                OnCharacterSpawned?.Invoke(characterBase);
                return characterObj;
            }
            catch (Exception ex)
            {
                GcLogger.LogException(ex);
                if (characterObj) Object.Destroy(characterObj);
                return null;
            }
        }

        private void TrySetupSpriteWhiteOverlay(
            CharacterConstants.Type characterType,
            GameObject characterObj,
            CharacterBase characterBase)
        {
            if (characterObj == null || characterBase == null)
            {
                return;
            }

            if (!characterBase.TryEnsureSpriteWhiteOverlayController())
            {
                return;
            }

            var controller = characterObj.GetComponent<SpriteWhiteOverlayController>();
            if (controller == null)
            {
                return;
            }

            switch (characterType)
            {
                case CharacterConstants.Type.Player:
                {
                    var playerSettings = AddressableLoaderSettings.Instance != null
                        ? AddressableLoaderSettings.Instance.playerSettings
                        : null;

                    if (playerSettings == null)
                    {
                        return;
                    }

                    controller.Configure(
                        playerSettings.spriteWhiteOverlayColor,
                        playerSettings.spriteWhiteOverlayMaterial,
                        refreshTargets: true);
                    break;
                }
                case CharacterConstants.Type.Monster:
                {
                    var monsterSettings = AddressableLoaderSettings.Instance != null
                        ? AddressableLoaderSettings.Instance.monsterSettings
                        : null;

                    if (monsterSettings == null)
                    {
                        return;
                    }

                    controller.Configure(
                        monsterSettings.spriteWhiteOverlayColor,
                        monsterSettings.spriteWhiteOverlayMaterial,
                        refreshTargets: true);
                    break;
                }
            }
        }

        private ICharacterAnimationController SetupAnimationController(
            GameObject obj,
            ConfigCommon.AnimationController controllerType)
        {
            if (obj == null) return null;

            ICharacterAnimationController animController = null;

#if GGEMCO_USE_SPINE
            if (controllerType == ConfigCommon.AnimationController.Spine)
            {
                RemoveAllComponents<CharacterAnimationControllerSprite>(obj);
                var ctrl = EnsureSingleComponent<CharacterAnimationControllerSpine>(obj);
                animController = ctrl;

                var spineCtrl = obj.GetComponent<Spine2dController>();
                if (spineCtrl != null && _animationEventMediator != null)
                {
                    spineCtrl.EventListener = _animationEventMediator;
                }
            }
#endif
            if (controllerType == ConfigCommon.AnimationController.Sprite)
            {
#if GGEMCO_USE_SPINE
                RemoveAllComponents<CharacterAnimationControllerSpine>(obj);
#endif
                var ctrl = EnsureSingleComponent<CharacterAnimationControllerSprite>(obj);
                animController = ctrl;

                var animatorCtrl = obj.GetComponent<Animation2dController>();
                if (animatorCtrl != null && _animationEventMediator != null)
                {
                    animatorCtrl.EventListener = _animationEventMediator;
                }
            }

            return animController;
        }

        /// <summary>
        /// 대상 오브젝트에서 지정한 타입의 컴포넌트를 하나만 남기고 정리합니다.
        /// </summary>
        /// <typeparam name="T">정리할 컴포넌트 타입입니다.</typeparam>
        /// <param name="obj">정리 대상 오브젝트입니다.</param>
        /// <returns>유지된 단일 컴포넌트입니다. 없으면 새로 추가한 컴포넌트를 반환합니다.</returns>
        private static T EnsureSingleComponent<T>(GameObject obj) where T : Component
        {
            if (obj == null)
                return null;

            var components = obj.GetComponents<T>();
            if (components == null || components.Length == 0)
                return obj.AddComponent<T>();

            for (int i = 1; i < components.Length; i++)
            {
                if (components[i] != null)
                    Object.Destroy(components[i]);
            }

            return components[0];
        }

        /// <summary>
        /// 대상 오브젝트에 붙은 지정 타입 컴포넌트를 모두 제거합니다.
        /// </summary>
        /// <typeparam name="T">제거할 컴포넌트 타입입니다.</typeparam>
        /// <param name="obj">제거 대상 오브젝트입니다.</param>
        private static void RemoveAllComponents<T>(GameObject obj) where T : Component
        {
            if (obj == null)
                return;

            var components = obj.GetComponents<T>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    Object.Destroy(components[i]);
            }
        }

        /// <summary>
        /// 더미 캐릭터 생성에 필요한 원본 테이블 정보(애니메이션 UID, 스케일)를 조회합니다.
        /// </summary>
        /// <param name="sourceType">원본 캐릭터 타입입니다. Monster/Npc만 지원합니다.</param>
        /// <param name="sourceUid">원본 캐릭터 UID입니다.</param>
        /// <param name="animationUid">조회된 애니메이션 UID입니다.</param>
        /// <param name="scale">조회된 스케일 값입니다.</param>
        /// <returns>조회 성공 시 <see langword="true"/>를 반환합니다.</returns>
        private bool TryResolveDummySourceProfile(
            CharacterConstants.Type sourceType,
            int sourceUid,
            out int animationUid,
            out float scale)
        {
            animationUid = 0;
            scale = 1f;

            if (sourceUid <= 0)
                return false;

            switch (sourceType)
            {
                case CharacterConstants.Type.Npc:
                {
                    var infoNpc = _tableNpc?.GetDataByUid(sourceUid);
                    if (infoNpc == null)
                        return false;

                    animationUid = infoNpc.AnimationUid;
                    scale = infoNpc.Scale;
                    return true;
                }
                case CharacterConstants.Type.Monster:
                {
                    var infoMonster = _tableMonster?.GetDataByUid(sourceUid);
                    if (infoMonster == null)
                        return false;

                    animationUid = infoMonster.AnimationUid;
                    scale = infoMonster.Scale;
                    return true;
                }
                default:
                    GcLogger.LogWarning($"CreateDummyCharacter failed: unsupported sourceType={sourceType}");
                    return false;
            }
        }

        /// <summary>
        /// 더미 생성에 사용할 캐릭터 프리팹을 타입과 애니메이션 UID 기준으로 조회합니다.
        /// </summary>
        /// <param name="sourceType">원본 캐릭터 타입입니다.</param>
        /// <param name="animationUid">원본 애니메이션 UID입니다.</param>
        /// <returns>조회된 프리팹입니다. 없으면 <see langword="null"/>입니다.</returns>
        private GameObject ResolveDummyPrefab(CharacterConstants.Type sourceType, int animationUid)
        {
            if (_addressableLoaderPrefabCharacter == null)
                return null;

            return sourceType switch
            {
                CharacterConstants.Type.Npc => _addressableLoaderPrefabCharacter.GetCharacterNpc(animationUid),
                CharacterConstants.Type.Monster => _addressableLoaderPrefabCharacter.GetCharacterMonster(animationUid),
                _ => null
            };
        }

        /// <summary>
        /// 스킬 더미 전용 캐릭터를 생성합니다.
        /// 더미는 <see cref="CharacterBase"/> 기반이며, 원본 테이블의 애니메이션 컨트롤러 타입을 따라
        /// Spine/Sprite 중 하나만 유지하도록 구성합니다.
        /// </summary>
        /// <param name="sourceType">원본 캐릭터 타입입니다. Monster/Npc만 지원합니다.</param>
        /// <param name="sourceUid">원본 캐릭터 UID입니다.</param>
        /// <param name="regenData">생성 위치/리젠 데이터입니다.</param>
        /// <param name="prefab">외부에서 지정한 프리팹입니다. null이면 테이블 기반으로 조회합니다.</param>
        /// <returns>생성된 더미 오브젝트입니다. 실패하면 <see langword="null"/>입니다.</returns>
        public GameObject CreateDummyCharacter(
            CharacterConstants.Type sourceType,
            int sourceUid,
            CharacterRegenData regenData = null,
            GameObject prefab = null)
        {
            if (!TryResolveDummySourceProfile(sourceType, sourceUid, out int animationUid, out float scale))
                return null;

            var animationInfo = _tableAnimation?.GetDataByUid(animationUid);
            if (animationInfo == null)
                return null;

            if (prefab == null)
            {
                prefab = ResolveDummyPrefab(sourceType, animationUid);
                if (prefab == null)
                {
                    GcLogger.LogError($"CreateDummyCharacter failed: prefab is null. sourceType={sourceType}, animationUid={animationUid}");
                    return null;
                }
            }

            GameObject dummyObject = Object.Instantiate(prefab);
            try
            {
                CharacterBase characterBase = dummyObject.GetComponent<CharacterBase>();
                DummyCharacter dummyCharacter = characterBase as DummyCharacter;

                if (characterBase == null)
                {
                    dummyCharacter = dummyObject.AddComponent<DummyCharacter>();
                    characterBase = dummyCharacter;
                }
                else if (dummyCharacter == null)
                {
                    GcLogger.LogWarning(
                        $"CreateDummyCharacter: existing CharacterBase detected ({characterBase.GetType().Name}). " +
                        "Runtime type will be overridden to None.");
                }

                dummyCharacter?.ConfigureSource(sourceType, sourceUid);
                characterBase.type = CharacterConstants.Type.None;
                characterBase.uid = sourceUid;
                characterBase.CharacterRegenData = regenData;
                characterBase.SetScale(scale);

                if (regenData != null)
                {
                    dummyObject.transform.position = new Vector3(
                        regenData.x,
                        regenData.y,
                        dummyObject.transform.position.z);
                }

                var iAnim = SetupAnimationController(dummyObject, animationInfo.Controller);
                if (iAnim == null)
                {
                    GcLogger.LogError($"CreateDummyCharacter failed: wrong animation controller. uid:{sourceUid}, controller:{animationInfo.Controller}");
                    Object.Destroy(dummyObject);
                    return null;
                }

                characterBase.CharacterAnimationController = iAnim;
                _characters.Add(dummyObject);
                return dummyObject;
            }
            catch (Exception ex)
            {
                GcLogger.LogException(ex);
                if (dummyObject) Object.Destroy(dummyObject);
                return null;
            }
        }

        public async Task<GameObject> CreatePlayer()
        {
            try
            {
                string key = $"{ConfigAddressableKey.PrefabPlayer}";
                GameObject prefab = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);

                if (prefab)
                {
                    return CreateCharacter(
                        CharacterConstants.Type.Player,
                        AddressableLoaderSettings.Instance.playerSettings.animationController,
                        prefab);
                }

                GcLogger.LogError("플레이어 프리팹이 없습니다. path:" + ConfigCommon.PathPlayerPrefab);
                return null;
            }
            catch (Exception e)
            {
                GcLogger.LogException(e);
                return null;
            }
        }

        public GameObject CreateNpc(int uid, CharacterRegenData regenData = null, GameObject prefab = null)
        {
            if (uid <= 0) return null;

            var infoNpc = _tableNpc?.GetDataByUid(uid);
            if (infoNpc == null) return null;

            var animationInfo = _tableAnimation?.GetDataByUid(infoNpc.AnimationUid);
            if (animationInfo == null) return null;

            if (prefab == null)
            {
                prefab = _addressableLoaderPrefabCharacter?.GetCharacterNpc(infoNpc.AnimationUid);
                if (prefab == null)
                {
                    GcLogger.LogError($"animation prefab이 없습니다. AnimationUid: {infoNpc.AnimationUid}");
                    return null;
                }
            }

            GameObject npcObj = Object.Instantiate(prefab);
            try
            {
                Npc npcComponent = npcObj.AddComponent<Npc>();
                npcComponent.type = CharacterConstants.Type.Npc;

                var characterBase = npcObj.GetComponent<CharacterBase>();
                if (characterBase == null)
                {
                    GcLogger.LogError("CreateNpc failed: CharacterBase not found on npc prefab.");
                    Object.Destroy(npcObj);
                    return null;
                }

                if (regenData != null)
                {
                    npcObj.transform.position = new Vector3(
                        regenData.x, regenData.y, npcObj.transform.position.z);
                }

                var iAnim = SetupAnimationController(npcObj, animationInfo.Controller);
                if (iAnim == null)
                {
                    GcLogger.LogError($"CreateNpc failed: wrong animation controller. uid:{uid}, controller:{animationInfo.Controller}");
                    Object.Destroy(npcObj);
                    return null;
                }

                characterBase.CharacterAnimationController = iAnim;
                characterBase.uid = uid;
                characterBase.SetScale(infoNpc.Scale);
                characterBase.RefreshCharacterBodyCollision();

                _characters.Add(npcObj);
                OnCharacterSpawned?.Invoke(characterBase);
                return npcObj;
            }
            catch (Exception ex)
            {
                GcLogger.LogException(ex);
                if (npcObj) Object.Destroy(npcObj);
                return null;
            }
        }

        public GameObject CreateMonster(int uid, CharacterRegenData regenData = null, GameObject prefab = null)
        {
            if (uid <= 0) return null;

            var infoMonster = _tableMonster?.GetDataByUid(uid);
            if (infoMonster == null) return null;

            var animationInfo = _tableAnimation?.GetDataByUid(infoMonster.AnimationUid);
            if (animationInfo == null) return null;

            if (prefab == null)
            {
                prefab = _addressableLoaderPrefabCharacter?.GetCharacterMonster(infoMonster.AnimationUid);
                if (prefab == null) return null;
            }

            var monster = CreateCharacter(
                CharacterConstants.Type.Monster,
                animationInfo.Controller,
                prefab,
                regenData);

            if (!monster) return null;

            var characterBase = monster.GetComponent<CharacterBase>();
            if (characterBase)
            {
                characterBase.uid = uid;
                characterBase.SetScale(infoMonster.Scale);
                characterBase.RefreshCharacterBodyCollision();
            }

            return monster;
        }

        public GameObject RentMonster(int uid, CharacterRegenData regenData = null, GameObject prefab = null)
        {
            if (uid <= 0)
                return null;

            Monster pooledMonster = null;
            if (_monsterPoolByUid.TryGetValue(uid, out var bucket))
            {
                while (bucket.Count > 0 && pooledMonster == null)
                {
                    pooledMonster = bucket.Pop();
                }
            }

            if (pooledMonster == null)
            {
                return CreateMonster(uid, regenData, prefab);
            }

            var pooledObject = pooledMonster.gameObject;
            pooledMonster.CancelPendingPoolReturn();
            pooledObject.transform.SetParent(null, worldPositionStays: false);
            pooledMonster.PrepareForPoolRent(uid, regenData);
            pooledObject.SetActive(true);
            CharacterBase characterBase = pooledObject.GetComponent<CharacterBase>();
            characterBase?.RefreshCharacterBodyCollision();
            characterBase?.Stop();
            return pooledObject;
        }

        public bool ReturnMonsterToPool(Monster monster)
        {
            if (monster == null)
                return false;

            int uid = monster.uid;
            if (uid <= 0)
                return false;

            monster.CancelPendingPoolReturn();
            monster.PrepareForPoolReturn();

            if (!_monsterPoolByUid.TryGetValue(uid, out var bucket))
            {
                bucket = new Stack<Monster>();
                _monsterPoolByUid.Add(uid, bucket);
            }

            var poolRoot = EnsureMonsterPoolRoot();
            poolRoot.gameObject.SetActive(true);
            monster.transform.SetParent(poolRoot, worldPositionStays: false);
            monster.gameObject.SetActive(false);
            bucket.Push(monster);
            poolRoot.gameObject.SetActive(false);

            if (SceneGame.Instance != null && SceneGame.Instance.mapManager != null)
            {
                SceneGame.Instance.mapManager.OnMonsterReturnedToPool(monster.vid);
            }

            return true;
        }

        public GameObject CreateCharacter(CharacterConstants.Type type, int characterUid)
        {
            if (type == CharacterConstants.Type.Player)
            {
                _ = CreatePlayer();
                return null;
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
            if (character == null) return;
            if (!_characters.Contains(character)) return;

            var ch = character.GetComponent<CharacterBase>();
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
