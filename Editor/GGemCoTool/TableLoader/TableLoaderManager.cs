using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TableLoaderManager : TableLoaderManagerBase
    {
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