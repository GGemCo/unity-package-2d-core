using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 맵
    /// </summary>
    public class DefaultMap : MonoBehaviour
    {
        private int chapterNumber;
        private string chapterName;
        private MapConstants.Type mapType;
        private MapConstants.SubType mapSubType;
        private Tilemap tilemap;
        // 맵에 배치된 몬스터
        protected readonly Dictionary<int, GameObject> Monsters = new Dictionary<int, GameObject>();
        // 맵에 배치된 npc
        protected readonly Dictionary<int, GameObject> Npcs = new Dictionary<int, GameObject>();

        protected virtual void Awake()
        {
            tilemap = GetComponent<Tilemap>();
            
            InitComponents();
            InitTagSortingLayer();
        }

        public virtual void InitComponents()
        {
            
        }

        public virtual void InitTagSortingLayer()
        {
            tag = ConfigTags.GetValue(ConfigTags.Keys.Map);
            GetComponent<TilemapRenderer>().sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.MapTerrain);
        }

        public virtual void Initialize(int uid, string name, MapConstants.Type type, MapConstants.SubType subType)
        {
            chapterNumber = uid;
            chapterName = name;
            mapType = type;
            mapSubType = subType;
        }
        /// <summary>
        /// 같은 장의 맵인지 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsSameChapter(int value)
        {
            return chapterNumber == value;
        }
        /// <summary>
        /// 현재 맵에 연결된 장 가져오기
        /// </summary>
        /// <returns></returns>
        public int GetChapterNumber()
        {
            return chapterNumber;
        }
        /// <summary>
        /// 맵 사이즈 구하기
        /// </summary>
        /// <returns></returns>
        public Vector2 GetMapSize()
        {
            if (tilemap == null)
            {
                tilemap = GetComponent<Tilemap>();
            }
            // 실제 타일이 배치된 경계를 추적하기 위한 변수들
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            // 타일맵의 모든 셀을 순회하며 타일이 있는 위치를 확인
            foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                {
                    min = Vector3Int.Min(min, pos);
                    max = Vector3Int.Max(max, pos);
                }
            }

            // 타일이 있는 범위의 크기를 계산
            Vector3Int size = max - min + Vector3Int.one;

            // 셀 크기를 고려하여 실제 월드 공간에서의 크기 계산
            Vector3 cellSize = tilemap.cellSize;
            float totalWidth = size.x * cellSize.x;
            float totalHeight = size.y * cellSize.y;

            // Logger.Log("Total Tilemap Width: " + totalWidth + ", Total Tilemap Height: " + totalHeight);

            return new Vector2(totalWidth, totalHeight);
        }
        /// <summary>
        /// vid 값으로 몬스터 찾기  
        /// </summary>
        /// <param name="vid"></param>
        /// <returns></returns>
        public CharacterRegenData GetNpcDataByVid(int vid)
        {
            GameObject npc = Npcs.GetValueOrDefault(vid);
            if (npc == null) return null;
            Npc myNpc = npc.GetComponent<Npc>();
            if (myNpc == null) return null;
            return myNpc.CharacterRegenData;
        }
        /// <summary>
        /// 맵에 배치된 npc 중에 uid 로 가져오기
        /// </summary>
        /// <param name="npcUid"></param>
        /// <returns></returns>
        public Npc GetNpcByUid(int npcUid)
        {
            foreach (var data in Npcs)
            {
                Npc npc = data.Value?.GetComponent<Npc>();
                if (npc == null) continue;
                if (npc.uid == npcUid)
                {
                    return npc;
                }
            }
            return null;
        }
        public Monster GetMonsterByUid(int monsterUid)
        {
            foreach (var data in Monsters)
            {
                Monster monster = data.Value?.GetComponent<Monster>();
                if (monster == null) continue;
                if (monster.uid == monsterUid)
                {
                    return monster;
                }
            }
            return null;
        }
        protected void LateUpdate()
        {
            // 카메라가 이동할 때마다 컬링 범위 및 오브젝트 상태 갱신
            CalculateCullingBounds();
        }

        protected virtual void CalculateCullingBounds()
        {
        }
        /// <summary>
        /// npc를 스폰하면서 List 에 추가하기
        /// </summary>
        /// <param name="vid"></param>
        /// <param name="npc"></param>
        public void AddNpc(int vid, GameObject npc)
        {
            if (npc == null) return;
            Npcs.Add(vid, npc);
        }
        /// <summary>
        /// 몬스터를 스폰하면서 List 에 추가하기
        /// </summary>
        /// <param name="vid"></param>
        /// <param name="monster"></param>
        public void AddMonster(int vid, GameObject monster)
        {
            if (monster == null) return;
            Monsters.Add(vid, monster);
        }
        /// <summary>
        /// vid 값으로 몬스터 찾기  
        /// </summary>
        /// <param name="vid"></param>
        /// <returns></returns>
        public CharacterRegenData GetMonsterDataByVid(int vid)
        {
            GameObject monster = Monsters.GetValueOrDefault(vid);
            if (monster == null) return null;
            Monster myMonster = monster.GetComponent<Monster>();
            if (myMonster == null) return null;
            return myMonster.CharacterRegenData;
        }
        /// <summary>
        /// 플레이어 기준 range 안에서 가장 가까운 몬스터 찾기
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Monster GetNearByMonsterDistance(int range)
        {
            Monster closeMonster = null;
            float closestDistance = float.MaxValue;
            Vector3 playerPosition = SceneGame.Instance.player.transform.position;
            foreach (var data in Monsters)
            {
                GameObject monster = data.Value;
                if (monster == null) continue;
                Monster myMonster = monster.GetComponent<Monster>();
                if (myMonster == null || myMonster.IsStatusDead() || !myMonster.gameObject.activeSelf) continue;
                
                // 거리 계산
                float distance = Vector2.Distance(playerPosition, monster.transform.position);
                if (distance > range) continue;

                // 가장 가까운 NPC 업데이트
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closeMonster = myMonster;
                }
            }
            return closeMonster;
        }

    }
}
