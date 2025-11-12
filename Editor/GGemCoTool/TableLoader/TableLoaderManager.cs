using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TableLoaderManager
    {
        // 경로 → 테이블 인스턴스 (행 타입 불문)
        private readonly Dictionary<string, ITableParser> _loadedTables =
            new Dictionary<string, ITableParser>(StringComparer.Ordinal);

        private bool TryGetLoaded<T>(string filePath, out T table) where T : class, ITableParser
        {
            if (_loadedTables.TryGetValue(filePath, out var t) && t is T cast)
            {
                table = cast; return true;
            }
            table = null; return false;
        }

        private T LoadTable<T>(string filePath, bool forceReload = false)
            where T : class, ITableParser, new()
        {
            if (!forceReload && TryGetLoaded<T>(filePath, out var cached))
                return cached;

            T tableData = null;
            try
            {
                var content = AssetDatabaseLoaderManager.LoadFileText(filePath);
                if (string.IsNullOrEmpty(content))
                {
                    Debug.LogError($"[TableLoader] 테이블 내용이 없습니다. path={filePath}");
                    return null;
                }

                tableData = new T();
                tableData.LoadData(content);

                // 경로 키로 캐시에 저장(있으면 교체)
                _loadedTables[filePath] = tableData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TableLoader] 테이블 파일 읽기/파싱 중 오류. path={filePath}, ex={ex.Message}");
                return null;
            }

            return tableData;
        }

        /// <summary>
        /// 필요 시 특정 경로의 캐시를 제거(재로드 대비)
        /// </summary>
        public bool Unload(string filePath) => _loadedTables.Remove(filePath);

        /// <summary>
        /// 모든 테이블 캐시 제거(프로젝트 리임포트/일괄 재로드 용)
        /// </summary>
        public void UnloadAll() => _loadedTables.Clear();

        public TableMap LoadMapTable(bool forceReload = false)
            => LoadTable<TableMap>(ConfigAddressableTable.TableMap.Path, forceReload);
        
        public TableNpc LoadNpcTable()
        {
            return LoadTable<TableNpc>(ConfigAddressableTable.TableNpc.Path);
        }
        public TableMonster LoadMonsterTable()
        {
            return LoadTable<TableMonster>(ConfigAddressableTable.TableMonster.Path);
        }
        public TableAnimation LoadSpineTable()
        {
            return LoadTable<TableAnimation>(ConfigAddressableTable.TableAnimation.Path);
        }
        public TableItem LoadItemTable()
        {
            return LoadTable<TableItem>(ConfigAddressableTable.TableItem.Path);
        }
        public TableItemDropGroup LoadItemDropGroupTable()
        {
            return LoadTable<TableItemDropGroup>(ConfigAddressableTable.TableItemDropGroup.Path);
        }
        public TableMonsterDropRate LoadMonsterDropRateTable()
        {
            return LoadTable<TableMonsterDropRate>(ConfigAddressableTable.TableMonsterDropRate.Path);
        }

        public TableCutscene LoadCutsceneTable()
        {
            return LoadTable<TableCutscene>(ConfigAddressableTable.TableCutscene.Path);
        }

        public TableDialogue LoadDialogueTable()
        {
            return LoadTable<TableDialogue>(ConfigAddressableTable.TableDialogue.Path);
        }
        public TableQuest LoadQuestTable()
        {
            return LoadTable<TableQuest>(ConfigAddressableTable.TableQuest.Path);
        }

        public TableEffect LoadEffectTable()
        {
            return LoadTable<TableEffect>(ConfigAddressableTable.TableEffect.Path);
        }

        public TableSkill LoadSkillTable()
        {
            return LoadTable<TableSkill>(ConfigAddressableTable.TableSkill.Path);
        }

        public TableWindow LoadWindowTable()
        {
            return LoadTable<TableWindow>(ConfigAddressableTable.TableWindow.Path);
        }
        public TableAffect LoadAffectTable()
        {
            return LoadTable<TableAffect>(ConfigAddressableTable.TableAffect.Path);
        }

        public TableSound LoadSoundTable()
        {
            return LoadTable<TableSound>(ConfigAddressableTable.TableSound.Path);
        }

        public TableProjectile LoadProjectileTable()
        {
            return LoadTable<TableProjectile>(ConfigAddressableTable.TableProjectile.Path);
        }

        public TableSimulationTool LoadSimulationToolTable()
        {
            return LoadTable<TableSimulationTool>(ConfigAddressableTable.TableSimulationTool.Path);
        }

        public TableSimulationGrowth LoadSimulationGrowthTable()
        {
            return LoadTable<TableSimulationGrowth>(ConfigAddressableTable.TableSimulationGrowth.Path);
        }

        /// <summary>
        /// 툴에서 드롭다운 메뉴를 만들기 위해 사용중
        /// 사용하려면 Table 에 TryGetDataByUid 함수를 추가해야 함
        /// </summary>
        /// <param name="tableFileName"></param>
        /// <param name="table"></param>
        /// <param name="nameList"></param>
        /// <param name="structTable"></param>
        /// <param name="displayNameFunc"></param>
        /// <param name="forceReload"></param>
        /// <typeparam name="TTable"></typeparam>
        /// <typeparam name="TRow"></typeparam>
        public void LoadTableData<TTable, TRow>(
            string tableFileName,
            out TTable table,
            out List<string> nameList,
            out Dictionary<int, TRow> structTable,
            Func<TRow, string> displayNameFunc,
            bool forceReload = false)
            where TTable : DefaultTable<TRow>, new()
            where TRow : class
        {
            nameList = new List<string>();
            structTable = new Dictionary<int, TRow>();

            string path = $"{ConfigAddressablePath.Tables}/{tableFileName}.txt";
            table = LoadTable<TTable>(path, forceReload); // 제약 TTable : ITableData도 만족
            if (table == null)
            {
                Debug.LogError($"{tableFileName} 테이블을 불러오지 못 했습니다.");
                return;
            }

            int index = 0;
            foreach (var kv in table.GetDatas()) // Dictionary<int, TRow>
            {
                var row = kv.Value;
                nameList.Add(displayNameFunc(row));
                structTable.TryAdd(index++, row);
            }
        }
    }
}