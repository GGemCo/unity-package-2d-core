using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타일맵 기반 맵의 기본 동작을 제공하며, 맵에 배치된 몬스터와 NPC 목록을 관리합니다.
    /// </summary>
    public class DefaultMap : MonoBehaviour
    {
        /// <summary>
        /// 현재 맵에 대응되는 테이블 데이터입니다.
        /// </summary>
        protected StruckTableMap struckTableMap;

        private Tilemap _tilemap;

        /// <summary>
        /// 맵에 배치된 몬스터를 VID 기준으로 보관하는 사전입니다.
        /// </summary>
        protected readonly Dictionary<int, GameObject> Monsters = new Dictionary<int, GameObject>();

        /// <summary>
        /// 맵에 배치된 NPC를 VID 기준으로 보관하는 사전입니다.
        /// </summary>
        protected readonly Dictionary<int, GameObject> Npcs = new Dictionary<int, GameObject>();

        /// <summary>
        /// 타일맵 컴포넌트를 캐시하고 맵 구성 요소 및 태그, 정렬 레이어를 초기화합니다.
        /// </summary>
        protected virtual void Awake()
        {
            _tilemap = GetComponent<Tilemap>();

            InitComponents();
            InitTagSortingLayer();
        }

        /// <summary>
        /// 파생 클래스에서 맵에 필요한 추가 컴포넌트를 초기화합니다.
        /// </summary>
        public virtual void InitComponents()
        {

        }

        /// <summary>
        /// 맵 오브젝트의 태그와 타일맵 렌더러의 정렬 레이어를 기본 맵 설정으로 지정합니다.
        /// </summary>
        public virtual void InitTagSortingLayer()
        {
            tag = ConfigTags.GetValue(ConfigTags.Keys.Map);
            GetComponent<TilemapRenderer>().sortingLayerName =
                ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.Map_Ground);
        }

        /// <summary>
        /// 현재 맵에서 사용할 테이블 데이터를 설정합니다.
        /// </summary>
        /// <param name="currentMapTableData">현재 맵에 대응되는 테이블 데이터입니다.</param>
        public virtual void Initialize(StruckTableMap currentMapTableData)
        {
            struckTableMap = currentMapTableData;
        }

        /// <summary>
        /// VID에 해당하는 NPC의 리젠 데이터를 반환합니다.
        /// </summary>
        /// <param name="vid">조회할 NPC의 VID입니다.</param>
        /// <returns>NPC의 리젠 데이터이며, 대상이 없거나 NPC 컴포넌트가 없으면 <c>null</c>입니다.</returns>
        public CharacterRegenData GetNpcDataByVid(int vid)
        {
            GameObject npc = Npcs.GetValueOrDefault(vid);
            if (npc == null) return null;
            Npc myNpc = npc.GetComponent<Npc>();
            if (myNpc == null) return null;
            return myNpc.CharacterRegenData;
        }

        /// <summary>
        /// 현재 맵에 등록된 NPC 목록을 반환합니다.
        /// </summary>
        /// <returns>VID를 키로 사용하는 NPC GameObject 사전입니다.</returns>
        public Dictionary<int, GameObject> GetNpcs()
        {
            return Npcs;
        }

        /// <summary>
        /// 현재 맵에 등록된 몬스터 목록을 반환합니다.
        /// </summary>
        /// <returns>VID를 키로 사용하는 몬스터 GameObject 사전입니다.</returns>
        public Dictionary<int, GameObject> GetMonsters()
        {
            return Monsters;
        }

        /// <summary>
        /// UID가 일치하는 NPC를 현재 맵의 NPC 목록에서 찾아 반환합니다.
        /// </summary>
        /// <param name="npcUid">조회할 NPC의 UID입니다.</param>
        /// <returns>UID가 일치하는 NPC이며, 찾지 못하면 <c>null</c>입니다.</returns>
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

        /// <summary>
        /// UID가 일치하는 몬스터를 현재 맵의 몬스터 목록에서 찾아 반환합니다.
        /// </summary>
        /// <param name="monsterUid">조회할 몬스터의 UID입니다.</param>
        /// <returns>UID가 일치하는 몬스터이며, 찾지 못하면 <c>null</c>입니다.</returns>
        public Monster GetMonsterByUid(int monsterUid)
        {
            foreach (var data in Monsters)
            {
                if (data.Value == null) continue;
                Monster monster = data.Value.GetComponent<Monster>();
                if (monster == null) continue;
                if (monster.uid == monsterUid)
                {
                    return monster;
                }
            }

            return null;
        }

        /// <summary>
        /// 매 프레임 종료 시점에 카메라 기준 컬링 범위와 오브젝트 상태를 갱신합니다.
        /// </summary>
        protected void LateUpdate()
        {
            CalculateCullingBounds();
        }

        /// <summary>
        /// 파생 클래스에서 카메라 위치나 맵 상태에 따른 컬링 범위를 계산합니다.
        /// </summary>
        protected virtual void CalculateCullingBounds()
        {
        }

        /// <summary>
        /// 생성된 NPC를 VID 기준 목록에 추가합니다.
        /// </summary>
        /// <param name="vid">등록할 NPC의 VID입니다.</param>
        /// <param name="npc">등록할 NPC GameObject입니다.</param>
        public void AddNpc(int vid, GameObject npc)
        {
            if (npc == null) return;
            Npcs[vid] = npc;
        }

        /// <summary>
        /// 생성된 몬스터를 VID 기준 목록에 추가하거나 기존 항목을 갱신합니다.
        /// </summary>
        /// <param name="vid">등록할 몬스터의 VID입니다.</param>
        /// <param name="monster">등록할 몬스터 GameObject입니다.</param>
        public void AddMonster(int vid, GameObject monster)
        {
            if (monster == null) return;
            Monsters[vid] = monster;
        }

        /// <summary>
        /// 지정한 VID에 해당하는 몬스터를 목록에서 제거합니다.
        /// </summary>
        /// <param name="vid">제거할 몬스터의 VID입니다.</param>
        public void RemoveMonster(int vid)
        {
            Monsters.Remove(vid);
        }

        /// <summary>
        /// 현재 등록된 몬스터 항목의 복사본 목록을 반환합니다.
        /// </summary>
        /// <returns>VID와 몬스터 GameObject 쌍으로 구성된 목록입니다.</returns>
        public List<KeyValuePair<int, GameObject>> GetMonsterEntries()
        {
            return new List<KeyValuePair<int, GameObject>>(Monsters);
        }

        /// <summary>
        /// 현재 맵에 등록된 모든 몬스터 항목을 제거합니다.
        /// </summary>
        public void ClearMonsters()
        {
            Monsters.Clear();
        }

        /// <summary>
        /// VID에 해당하는 몬스터의 리젠 데이터를 반환합니다.
        /// </summary>
        /// <param name="vid">조회할 몬스터의 VID입니다.</param>
        /// <returns>몬스터의 리젠 데이터이며, 대상이 없거나 몬스터 컴포넌트가 없으면 <c>null</c>입니다.</returns>
        public CharacterRegenData GetMonsterDataByVid(int vid)
        {
            GameObject monster = Monsters.GetValueOrDefault(vid);
            if (monster == null) return null;
            Monster myMonster = monster.GetComponent<Monster>();
            if (myMonster == null) return null;
            return myMonster.CharacterRegenData;
        }

        /// <summary>
        /// 플레이어를 기준으로 지정 범위 안에 있는 가장 가까운 생존 몬스터를 반환합니다.
        /// </summary>
        /// <param name="range">탐색할 최대 거리입니다.</param>
        /// <returns>범위 안에서 가장 가까운 몬스터이며, 조건에 맞는 대상이 없으면 <c>null</c>입니다.</returns>
        public Monster GetNearByMonsterDistance(int range)
        {
            if (!SceneGame.Instance || !SceneGame.Instance.player) return null;
            
            Monster closeMonster = null;
            float closestDistance = float.MaxValue;
            Vector3 playerPosition = SceneGame.Instance.player.transform.position;
            foreach (var data in Monsters)
            {
                GameObject monster = data.Value;
                if (monster == null) continue;
                Monster myMonster = monster.GetComponent<Monster>();
                if (myMonster == null || myMonster.IsStatusDead() || !myMonster.gameObject.activeSelf) continue;

                float distance = Vector2.Distance(playerPosition, monster.transform.position);
                if (distance > range) continue;

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closeMonster = myMonster;
                }
            }

            return closeMonster;
        }

        /// <summary>
        /// 지정 위치를 기준으로 현재 맵에 등록된 가장 가까운 생존 몬스터를 검색합니다.
        /// </summary>
        /// <param name="origin">검색 기준 위치입니다.</param>
        /// <param name="includeInactive">비활성화된 몬스터를 검색 대상에 포함할지 여부입니다.</param>
        /// <param name="maxDistance">검색 최대 거리입니다. 0 이하이면 거리 제한 없이 검색합니다.</param>
        /// <param name="monster">검색된 가장 가까운 생존 몬스터입니다.</param>
        /// <returns>조건에 맞는 몬스터를 찾으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryFindNearestAliveMonster(
            Vector2 origin,
            bool includeInactive,
            float maxDistance,
            out Monster monster)
        {
            monster = null;
            float closestSqrDistance = float.MaxValue;
            float maxSqrDistance = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;

            foreach (var data in Monsters)
            {
                GameObject monsterObject = data.Value;
                if (monsterObject == null) continue;
                if (!includeInactive && !monsterObject.activeInHierarchy) continue;

                Monster candidate = monsterObject.GetComponent<Monster>();
                if (candidate == null || candidate.IsStatusDead()) continue;

                Vector2 delta = (Vector2)candidate.transform.position - origin;
                float sqrDistance = delta.sqrMagnitude;
                if (sqrDistance > maxSqrDistance || sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                monster = candidate;
            }

            return monster != null;
        }

        /// <summary>
        /// 현재 맵이 속한 챕터 번호를 반환합니다.
        /// </summary>
        /// <returns>맵 테이블 데이터가 있으면 챕터 번호, 없으면 0입니다.</returns>
        public int GetChapterNumber()
        {
            if (struckTableMap == null) return 0;
            return struckTableMap.Chapter;
        }

        /// <summary>
        /// 현재 맵의 이름을 반환합니다.
        /// </summary>
        /// <returns>맵 테이블 데이터가 있으면 맵 이름, 없으면 빈 문자열입니다.</returns>
        public string GetMapName()
        {
            if (struckTableMap == null) return "";
            return struckTableMap.Name;
        }
    }
}
