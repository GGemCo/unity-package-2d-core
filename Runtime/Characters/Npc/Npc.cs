#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Npc 기본 클레스
    /// </summary>
    public class Npc : CharacterBase
    {
        private GameObject containerNpcName;
        private TagNameNpc tagNameNpc;
        private NpcQuestController npcQuestController;

        protected override void Awake()
        {
            base.Awake();
            // 퀘스트 관리
            npcQuestController = gameObject.AddComponent<NpcQuestController>();
        }
        protected override void Start()
        {
            base.Start();
            
            CreateTagName();
        }
        /// <summary>
        /// 아이템 이름 tag 만들기
        /// </summary>
        private void CreateTagName()
        {
            GameObject prefabTagNameNpc =
                AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabTextNpcNameTag);
            if (prefabTagNameNpc == null) return;
            if (containerNpcName == null)
            {
                containerNpcName = SceneGame.Instance.containerDropItemName;
            }
            GameObject objectTagNameItem = Instantiate(prefabTagNameNpc, containerNpcName.transform);
            if (objectTagNameItem == null) return;
            tagNameNpc = objectTagNameItem.GetComponent<TagNameNpc>();
            if (tagNameNpc == null) return;
            tagNameNpc.Initialize(gameObject);
        }

        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.Npc);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected override void InitComponents()
        {
            base.InitComponents();
            ComponentController.AddRigidbody2D(gameObject);
            
            // attack range
            GameObject attackRange = new GameObject("AttackRange");
            CharacterAttackRange characterAttackRange = attackRange.AddComponent<CharacterAttackRange>();
            characterAttackRange.Initialize(this);
            
            Vector2 offset = Vector2.zero;
            Vector2 size = new Vector2(0,0);
            colliderCheckCharacter = ComponentController.AddCapsuleCollider2D(attackRange, true, offset, size);
            
            gameObject.AddComponent<ControllerNpc>();
            
        }
        /// <summary>
        /// 테이블에서 가져온 npc 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            base.InitializeByTable();
            if (TableLoaderManager.Instance == null) return;
            if (uid <= 0) return;
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            var info = tableLoaderManager.TableNpc.GetDataByUid(uid);
            // GcLogger.Log("InitializationStat uid: "+uid+" / info.uid: "+info.uid+" / StatMoveSpeed: "+info.statMoveSpeed);
            if (info.Uid > 0)
            {
                const int statAtk = 0;
                const int statDef = 0;
                const int statHp = 0;
                const int statMp = 0;
                const int statMoveSpeed = 100;
                const int statAttackSpeed = 0;
                const int statRegistFire = 0;
                const int statRegistCold = 0;
                const int statRegistLightning = 0;
                SetBaseInfos(statAtk, statDef, statHp, statMp, statMoveSpeed, statAttackSpeed, statRegistFire,
                    statRegistCold, statRegistLightning);
                float scale = info.Scale;
                SetScale(scale);
            }
        }

        /// <summary>
        /// regen_data 의 정보 셋팅
        /// </summary>
        protected override void InitializeByRegenData()
        {
            // 맵 배치툴로 저장한 정보가 있을 경우 
            if (CharacterRegenData == null) return;
            // UpdateDirection() 에서 초기 방향 처리를 위해 추가
            direction = new Vector3(CharacterRegenData.IsFlip?1:-1, 0, 0);
            directionPrev = new Vector3(CharacterRegenData.IsFlip?1:-1, 0, 0);
            SetFlip(CharacterRegenData.IsFlip);
        }
        
        protected void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                SceneGame.Instance.InteractionManager.SetInfo(this);
            }
        }
        protected void OnTriggerExit2D(Collider2D collision)
        {
            // 에디터에서 플레이 종료시 OnTriggerExit2D 함수를 호출하지 않도록 처리 
#if UNITY_EDITOR
            if (UnityEditorHelper.GetIsExitingPlayMode())
                return;
#endif
            
            if (collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                SceneGame.Instance.InteractionManager.RemoveCurrentNpc();
                SceneGame.Instance.InteractionManager.EndInteraction();
            }
        }
        /// <summary>
        /// Destroy 되었을때 태그 지워주기
        /// </summary>
        private void OnDestroy()
        {
            if (tagNameNpc == null) return;
            Destroy(tagNameNpc.gameObject);
        }

        public void UpdateQuestInfo()
        {
            npcQuestController?.LoadQuest();
        }

        public List<NpcQuestData> GetQuestInfos()
        {
            if (npcQuestController == null) return null;
            return npcQuestController.GetQuestInfos();
        }
    }
}
