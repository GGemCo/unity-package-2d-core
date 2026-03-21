using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TableLoaderManager : TableLoaderManagerBase
    {
        public static TableMap LoadMapTable(bool forceReload = true)
            => LoadTable<TableMap>(ConfigAddressableTable.TableMap.Path, forceReload);
        
        public static TableNpc LoadNpcTable(bool forceReload = true)
        {
            return LoadTable<TableNpc>(ConfigAddressableTable.TableNpc.Path, forceReload);
        }
        public static TableMonster LoadMonsterTable(bool forceReload = true)
        {
            return LoadTable<TableMonster>(ConfigAddressableTable.TableMonster.Path, forceReload);
        }
        public static TableAnimation LoadSpineTable(bool forceReload = true)
        {
            return LoadTable<TableAnimation>(ConfigAddressableTable.TableAnimation.Path, forceReload);
        }
        public static TableItem LoadItemTable(bool forceReload = true)
        {
            return LoadTable<TableItem>(ConfigAddressableTable.TableItem.Path, forceReload);
        }
        
        public static TableItemUse LoadItemUseTable(bool forceReload = true)
            => LoadTable<TableItemUse>(ConfigAddressableTable.TableItemUse.Path, forceReload);

        public static TableItemUseAction LoadItemUseActionTable(bool forceReload = true)
            => LoadTable<TableItemUseAction>(ConfigAddressableTable.TableItemUseAction.Path, forceReload);
public static TableItemDropGroup LoadItemDropGroupTable(bool forceReload = true)
        {
            return LoadTable<TableItemDropGroup>(ConfigAddressableTable.TableItemDropGroup.Path, forceReload);
        }
        public static TableMonsterDropRate LoadMonsterDropRateTable(bool forceReload = true)
        {
            return LoadTable<TableMonsterDropRate>(ConfigAddressableTable.TableMonsterDropRate.Path, forceReload);
        }

        public static TableCutscene LoadCutsceneTable(bool forceReload = true)
        {
            return LoadTable<TableCutscene>(ConfigAddressableTable.TableCutscene.Path, forceReload);
        }

        public static TableDialogue LoadDialogueTable(bool forceReload = true)
        {
            return LoadTable<TableDialogue>(ConfigAddressableTable.TableDialogue.Path, forceReload);
        }
        public static TableQuest LoadQuestTable(bool forceReload = true)
        {
            return LoadTable<TableQuest>(ConfigAddressableTable.TableQuest.Path, forceReload);
        }

        public static TableVfx LoadVfxTable(bool forceReload = true)
        {
            return LoadTable<TableVfx>(ConfigAddressableTable.TableVfx.Path, forceReload);
        }

        public static TableVfx LoadEffectTable(bool forceReload = true)
        {
            return LoadVfxTable(forceReload);
        }


        public static TableCrowdControl LoadCrowdControlTable(bool forceReload = true)
        {
            return LoadTable<TableCrowdControl>(ConfigAddressableTable.TableCrowdControl.Path, forceReload);
        }


        public static TableWindow LoadWindowTable(bool forceReload = true)
        {
            return LoadTable<TableWindow>(ConfigAddressableTable.TableWindow.Path, forceReload);
        }

        public static TableSound LoadSoundTable(bool forceReload = true)
        {
            return LoadTable<TableSound>(ConfigAddressableTable.TableSound.Path, forceReload);
        }

        public static TableProjectile LoadProjectileTable(bool forceReload = true)
        {
            return LoadTable<TableProjectile>(ConfigAddressableTable.TableProjectile.Path, forceReload);
        }

        public static TableSimulationTool LoadSimulationToolTable(bool forceReload = true)
        {
            return LoadTable<TableSimulationTool>(ConfigAddressableTable.TableSimulationTool.Path, forceReload);
        }

        public static TableSimulationGrowth LoadSimulationGrowthTable(bool forceReload = true)
        {
            return LoadTable<TableSimulationGrowth>(ConfigAddressableTable.TableSimulationGrowth.Path, forceReload);
        }
        public static TableAnimation LoadAnimationTable(bool forceReload = true)
        {
            return LoadTable<TableAnimation>(ConfigAddressableTable.TableAnimation.Path, forceReload);
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
        public static void LoadTableData<TTable, TRow>(
            string tableFileName,
            out TTable table,
            out List<string> nameList,
            out Dictionary<int, TRow> structTable,
            Func<TRow, string> displayNameFunc,
            bool forceReload = true)
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