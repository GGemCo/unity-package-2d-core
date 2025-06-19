using System;
using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEngine;

namespace GGemCo.Editor
{
    public class TableLoaderManager
    {
        private Dictionary<string, DefaultTable> loadedTables = new Dictionary<string, DefaultTable>();
        
        // 공통적인 로드 메서드로, 제네릭 타입과 파일명을 받아 로드
        private T LoadTable<T>(string fileName) where T : DefaultTable, new()
        {
            T tableData = null;
            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"Tables/{fileName}");
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        tableData = new T();
                        tableData.LoadData(content);
                    }
                    else
                    {
                        GcLogger.LogError($"테이블 내용이 없습니다. Tables/{fileName}");
                    }
                }
                else
                {
                    GcLogger.LogError($"테이블 파일을 찾을 수 없습니다. Tables/{fileName}");
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"테이블 파일을 읽는중 오류 발생. Tables/{fileName}: {ex.Message}");
            }
            loadedTables.TryAdd(fileName, tableData);
            return tableData;
        }

        public TableMap LoadMapTable()
        {
            return LoadTable<TableMap>(ConfigTableFileName.Map);
        }
        public TableNpc LoadNpcTable()
        {
            return LoadTable<TableNpc>(ConfigTableFileName.Npc);
        }
        public TableMonster LoadMonsterTable()
        {
            return LoadTable<TableMonster>(ConfigTableFileName.Monster);
        }
        public TableAnimation LoadSpineTable()
        {
            return LoadTable<TableAnimation>(ConfigTableFileName.Animation);
        }
        public TableItem LoadItemTable()
        {
            return LoadTable<TableItem>(ConfigTableFileName.Item);
        }
        public TableItemDropGroup LoadItemDropGroupTable()
        {
            return LoadTable<TableItemDropGroup>(ConfigTableFileName.ItemDropGroup);
        }
        public TableMonsterDropRate LoadMonsterDropRateTable()
        {
            return LoadTable<TableMonsterDropRate>(ConfigTableFileName.MonsterDropRate);
        }

        public TableCutscene LoadCutsceneTable()
        {
            return LoadTable<TableCutscene>(ConfigTableFileName.Cutscene);
        }

        public TableDialogue LoadDialogueTable()
        {
            return LoadTable<TableDialogue>(ConfigTableFileName.Dialogue);
        }
        public TableQuest LoadQuestTable()
        {
            return LoadTable<TableQuest>(ConfigTableFileName.Quest);
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
            table = loadedTables.GetValueOrDefault(tableFileName) as TTable;
            if (table == null)
            {
                table = LoadTable<TTable>(tableFileName);
            }

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
                if (rawInfo is TStruct casted)
                {
                    nameList.Add(displayNameFunc(casted));
                    structTable.TryAdd(index++, casted);
                }
            }
        }
    }
}