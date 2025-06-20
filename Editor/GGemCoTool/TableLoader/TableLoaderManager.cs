using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo.Scripts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Editor
{
    public class TableLoaderManager
    {
        private Dictionary<string, DefaultTable> loadedTables = new Dictionary<string, DefaultTable>();

        private async Task<T> LoadTableAsync<T>(string addressKey) where T : DefaultTable, new()
        {
            addressKey = $"GGemCo_Table_{addressKey}";
            if (loadedTables.TryGetValue(addressKey, out var cached))
                return cached as T;

            T tableData = null;
            try
            {
                AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(addressKey);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    string content = handle.Result.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        tableData = new T();
                        tableData.LoadData(content);
                        loadedTables.TryAdd(addressKey, tableData);
                    }
                    else
                    {
                        GcLogger.LogError($"테이블 내용이 없습니다. Addressables Key: {addressKey}");
                    }
                }
                else
                {
                    GcLogger.LogError($"테이블 로드 실패: Addressables Key: {addressKey}");
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"테이블 로드 중 오류 발생: {addressKey} - {ex.Message}");
            }

            return tableData;
        }

        public Task<TableItem> LoadItemTableAsync() => LoadTableAsync<TableItem>(ConfigTableFileName.Item);
        public Task<TableMap> LoadMapTableAsync() => LoadTableAsync<TableMap>(ConfigTableFileName.Map);
        public Task<TableNpc> LoadNpcTableAsync() => LoadTableAsync<TableNpc>(ConfigTableFileName.Npc);
        public Task<TableMonster> LoadMonsterTableAsync() => LoadTableAsync<TableMonster>(ConfigTableFileName.Monster);
        public Task<TableAnimation> LoadSpineTableAsync() => LoadTableAsync<TableAnimation>(ConfigTableFileName.Animation);
        public Task<TableItemDropGroup> LoadItemDropGroupTableAsync() => LoadTableAsync<TableItemDropGroup>(ConfigTableFileName.ItemDropGroup);
        public Task<TableMonsterDropRate> LoadMonsterDropRateTableAsync() => LoadTableAsync<TableMonsterDropRate>(ConfigTableFileName.MonsterDropRate);
        public Task<TableCutscene> LoadCutsceneTableAsync() => LoadTableAsync<TableCutscene>(ConfigTableFileName.Cutscene);
        public Task<TableDialogue> LoadDialogueTableAsync() => LoadTableAsync<TableDialogue>(ConfigTableFileName.Dialogue);
        public Task<TableQuest> LoadQuestTableAsync() => LoadTableAsync<TableQuest>(ConfigTableFileName.Quest);

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
        public async Task LoadTableDataAsync<TTable, TStruct>(
            string tableFileName,
            Action<TTable, List<string>, Dictionary<int, TStruct>> onLoaded,
            Func<TStruct, string> displayNameFunc)
            where TTable : DefaultTable, new()
            where TStruct : class
        {
            var nameList = new List<string>();
            var structTable = new Dictionary<int, TStruct>();

            TTable table = loadedTables.GetValueOrDefault($"GGemCo_Table_{tableFileName}") as TTable;
            if (table == null)
            {
                table = await LoadTableAsync<TTable>(tableFileName);
            }

            if (table == null)
            {
                GcLogger.LogError($"{tableFileName} 테이블을 불러오지 못 했습니다.");
                onLoaded?.Invoke(null, nameList, structTable);
                return;
            }

            Dictionary<int, Dictionary<string, string>> dataDictionary = table.GetDatas();
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dataDictionary)
            {
                if (!table.TryGetDataByUid(outerPair.Key, out var rawInfo)) continue;
                if (rawInfo is TStruct casted)
                {
                    nameList.Add(displayNameFunc(casted));
                    structTable.TryAdd(index++, casted);
                }
            }

            onLoaded?.Invoke(table, nameList, structTable);
        }
    }
}