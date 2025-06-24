using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo.Editor
{
    public class TableLoaderManager
    {
        private readonly Dictionary<string, DefaultTable> _loadedTables = new Dictionary<string, DefaultTable>();
        
        // 공통적인 로드 메서드로, 제네릭 타입과 파일명을 받아 로드
        private T LoadTable<T>(string filePath) where T : DefaultTable, new()
        {
            T tableData = null;
            try
            {
                string content = AssetDatabaseLoaderManager.LoadFileText(filePath);
                if (!string.IsNullOrEmpty(content))
                {
                    tableData = new T();
                    tableData.LoadData(content);
                }
                else
                {
                    GcLogger.LogError($"테이블 내용이 없습니다. {filePath}");
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"테이블 파일을 읽는중 오류 발생. {filePath}: {ex.Message}");
            }
            _loadedTables.TryAdd(filePath, tableData);
            return tableData;
        }

        public TableMap LoadMapTable()
        {
            return LoadTable<TableMap>(ConfigAddressableTable.TableMap.Path);
        }
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

        /// <summary>
        /// 툴에서 드롭다운 메뉴를 만들기 위해 사용중
        /// 사용하려면 Table 에 TryGetDataByUid 함수를 추가해야 함
        /// </summary>
        /// <param name="tableFileName"></param>
        /// <param name="table"></param>
        /// <param name="nameList"></param>
        /// <param name="structTable"></param>
        /// <param name="displayNameFunc"></param>
        /// <typeparam name="TTable"></typeparam>
        /// <typeparam name="TStruct"></typeparam>
        public void LoadTableData<TTable, TStruct>(string tableFileName, 
            out TTable table,
            out List<string> nameList, 
            out Dictionary<int, TStruct> structTable,
            Func<TStruct, string> displayNameFunc) 
            where TTable : DefaultTable, new()
            where TStruct : class 
        {
            nameList = new List<string>();
            structTable = new Dictionary<int, TStruct>();
            table = _loadedTables.GetValueOrDefault(tableFileName) as TTable ?? LoadTable<TTable>(tableFileName);

            if (table == null)
            {
                GcLogger.LogError($"{tableFileName} 테이블을 불러오지 못 했습니다.");
                return;
            }
 
            Dictionary<int, Dictionary<string, string>> monsterDictionary = table.GetDatas();
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                if (!table.TryGetDataByUid(outerPair.Key, out var rawInfo)) continue;
                if (rawInfo is not TStruct casted) continue;
                nameList.Add(displayNameFunc(casted));
                structTable.TryAdd(index++, casted);
            }
        }
    }
}