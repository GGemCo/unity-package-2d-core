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
    /// </summary>
    public class CharacterManager
    {
        // 외부 확장용 생성/파괴 이벤트 (Core는 구독자에 대해 알지 못함)
        public static event Action<CharacterBase> OnCharacterSpawned;   // 생성 직후 1회
        public static event Action<CharacterBase> OnCharacterDestroyed; // Destroy 직전 1회

        private readonly List<GameObject> _characters = new List<GameObject>();

        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;
        private AnimationEventMediator _animationEventMediator;

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

        /// <summary>
        /// 내부 공용: Player/Monster 전용 생성 경로.
        /// NPC는 세부 타입(Npc/NpcObject/기타) 분기가 필요하므로 CreateNpc()를 사용한다.
        /// </summary>
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
                        break;
                    }
                    case CharacterConstants.Type.Npc:
                        // NPC는 세부 타입 분기 필요. 여기로 들어오지 않도록 CreateNpc 사용.
                        GcLogger.LogWarning("CreateCharacter(Type.Npc) is not supported. Use CreateNpc instead.");
                        Object.Destroy(characterObj);
                        return null;
                }

                // 애니메이션 컨트롤러 세팅
                var iAnim = SetupAnimationController(characterObj, animationController);
                if (iAnim == null)
                {
                    GcLogger.LogError($"CreateCharacter failed: animation controller is null. type:{characterType}, controller:{animationController}");
                    Object.Destroy(characterObj);
                    return null;
                }

                // 공통 Base 세팅
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

        /// <summary>
        /// 애니메이션 컨트롤러/이벤트 중개자 연결(SRP 분리)
        /// </summary>
        private ICharacterAnimationController SetupAnimationController(
            GameObject obj,
            ConfigCommon.AnimationController controllerType)
        {
            if (obj == null) return null;

            ICharacterAnimationController animController = null;

#if GGEMCO_USE_SPINE
            if (controllerType == ConfigCommon.AnimationController.Spine)
            {
                var ctrl = obj.AddComponent<CharacterAnimationControllerSpine>();
                animController = ctrl.GetComponent<ICharacterAnimationController>();

                var spineCtrl = obj.GetComponent<Spine2dController>();
                if (spineCtrl != null && _animationEventMediator != null)
                {
                    spineCtrl.EventListener = _animationEventMediator;
                }
            }
#endif
            if (controllerType == ConfigCommon.AnimationController.Sprite)
            {
                var ctrl = obj.AddComponent<CharacterAnimationControllerSprite>();
                animController = ctrl.GetComponent<ICharacterAnimationController>();

                var animatorCtrl = obj.GetComponent<Animation2dController>();
                if (animatorCtrl != null && _animationEventMediator != null)
                {
                    animatorCtrl.EventListener = _animationEventMediator;
                }
            }

            return animController;
        }

        /// <summary>
        /// 플레이어 생성 (Addressables 로드 포함)
        /// </summary>
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

        /// <summary>
        /// NPC 생성: 세부 타입(Object/Functional/Event/Default 등)에 따라 컴포넌트 분기
        /// </summary>
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
                // 1) 세부 타입에 맞는 NPC 컴포넌트 부착
                Npc npcComponent = npcObj.AddComponent<Npc>();

                npcComponent.type = CharacterConstants.Type.Npc;

                // 2) 공통 Base 세팅
                var characterBase = npcObj.GetComponent<CharacterBase>();
                if (characterBase == null)
                {
                    GcLogger.LogError("CreateNpc failed: CharacterBase not found on npc prefab.");
                    Object.Destroy(npcObj);
                    return null;
                }

                if (regenData != null)
                {
                    characterBase.uid = regenData.Uid;
                    npcObj.transform.position = new Vector3(
                        regenData.x, regenData.y, npcObj.transform.position.z);
                }

                // 3) 애니메이션 컨트롤러 부착
                var iAnim = SetupAnimationController(npcObj, animationInfo.Controller);
                if (iAnim == null)
                {
                    GcLogger.LogError($"CreateNpc failed: wrong animation controller. uid:{uid}, controller:{animationInfo.Controller}");
                    Object.Destroy(npcObj);
                    return null;
                }

                characterBase.CharacterAnimationController = iAnim;

                // 4) 데이터 기반 스케일 적용
                npcObj.GetComponent<Npc>()?.SetScale(infoNpc.Scale);

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

        /// <summary>
        /// 몬스터 생성
        /// </summary>
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

            monster.GetComponent<Monster>()?.SetScale(infoMonster.Scale);
            return monster;
        }

        /// <summary>
        /// 타입+UID로 캐릭터 생성(편의 함수)
        /// </summary>
        public GameObject CreateCharacter(CharacterConstants.Type type, int characterUid)
        {
            if (type == CharacterConstants.Type.Player)
            {
                _ = CreatePlayer(); // 비동기 시작 (호출자는 별도 await 가능)
                return null;
            }

            return type switch
            {
                CharacterConstants.Type.Npc     => CreateNpc(characterUid),
                CharacterConstants.Type.Monster => CreateMonster(characterUid),
                _ => null
            };
        }

        /// <summary>
        /// 캐릭터 제거 (Destroy는 프레임 종료 시점에 수행)
        /// </summary>
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
            // 필요 시 정리 로직 추가
        }

        public void SetAnimationEventMediator(AnimationEventMediator mediator)
        {
            _animationEventMediator = mediator;
        }
    }
}
