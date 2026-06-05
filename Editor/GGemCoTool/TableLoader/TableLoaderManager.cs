using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TableLoaderManager : TableLoaderManagerBase
    {
        public static TableMap LoadMapTable(bool forceReload = true)
            => LoadTable<TableMap>(ConfigAddressableTable.TableMap.Path, forceReload);

        /// <summary>
        /// 에디터 환경에서 map_entry_rule 테이블을 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 맵 입장 규칙 테이블입니다.</returns>
        public static TableMapEntryRule LoadMapEntryRuleTable(bool forceReload = true)
        {
            return LoadTable<TableMapEntryRule>(ConfigAddressableTable.TableMapEntryRule.Path, forceReload);
        }
        
        public static TableNpc LoadNpcTable(bool forceReload = true)
        {
            return LoadTable<TableNpc>(ConfigAddressableTable.TableNpc.Path, forceReload);
        }
        public static TableMonster LoadMonsterTable(bool forceReload = true)
        {
            return LoadTable<TableMonster>(ConfigAddressableTable.TableMonster.Path, forceReload);
        }
        public static TableMonsterPhase LoadMonsterPhaseTable(bool forceReload = true)
        {
            return LoadTable<TableMonsterPhase>(ConfigAddressableTable.TableMonsterPhase.Path, forceReload);
        }
        public static TableAnimation LoadSpineTable(bool forceReload = true)
        {
            return LoadTable<TableAnimation>(ConfigAddressableTable.TableAnimation.Path, forceReload);
        }
        public static TableItem LoadItemTable(bool forceReload = true)
        {
            return LoadTable<TableItem>(ConfigAddressableTable.TableItem.Path, forceReload);
        }

        public static TableItemVisual LoadItemVisualTable(bool forceReload = true)
            => LoadTable<TableItemVisual>(ConfigAddressableTable.TableItemVisual.Path, forceReload);
        
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

        /// <summary>
        /// 에디터 환경에서 ui_effect 테이블을 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 UI 효과 테이블입니다.</returns>
        public static TableUIEffect LoadUIEffectTable(bool forceReload = true)
        {
            return LoadTable<TableUIEffect>(ConfigAddressableTable.TableUIEffect.Path, forceReload);
        }

        public static TableDialogue LoadDialogueTable(bool forceReload = true)
        {
            return LoadTable<TableDialogue>(ConfigAddressableTable.TableDialogue.Path, forceReload);
        }
        public static TableQuest LoadQuestTable(bool forceReload = true)
        {
            return LoadTable<TableQuest>(ConfigAddressableTable.TableQuest.Path, forceReload);
        }

        /// <summary>
        /// 에디터 환경에서 license 테이블을 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 라이센스 테이블입니다.</returns>
        public static TableLicense LoadLicenseTable(bool forceReload = true)
        {
            return LoadTable<TableLicense>(ConfigAddressableTable.TableLicense.Path, forceReload);
        }

        public static TableVfx LoadVfxTable(bool forceReload = true)
            => TryLoadOptionalTable<TableVfx>(ConfigAddressableTable.TableVfx.Path, forceReload);

        public static TableVfxEffect LoadVfxEffectTable(bool forceReload = true)
            => LoadTable<TableVfxEffect>(ConfigAddressableTable.TableVfxEffect.Path, forceReload);

        public static TableVfxParticle LoadVfxParticleTable(bool forceReload = true)
            => LoadTable<TableVfxParticle>(ConfigAddressableTable.TableVfxParticle.Path, forceReload);

        public static TableVfxVariant LoadVfxVariantTable(bool forceReload = true)
            => TryLoadOptionalTable<TableVfxVariant>(ConfigAddressableTable.TableVfxVariant.Path, forceReload);

        /// <summary>
        /// 에디터 환경에서 laser 테이블을 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 laser 테이블입니다.</returns>
        public static TableLaser LoadLaserTable(bool forceReload = true)
            => LoadTable<TableLaser>(ConfigAddressableTable.TableLaser.Path, forceReload);

        public static Dictionary<int, VfxRuntimeData> LoadVfxRuntimeData(bool forceReload = true)
        {
            var merged = new Dictionary<int, VfxRuntimeData>();
            MergeVfxRows(merged, LoadVfxEffectTable(forceReload)?.GetDatas());
            MergeVfxRows(merged, LoadVfxParticleTable(forceReload)?.GetDatas());
            return merged;
        }

        private static void MergeVfxRows(Dictionary<int, VfxRuntimeData> target, IReadOnlyDictionary<int, StruckTableVfxEffect> source)
        {
            if (target == null || source == null)
                return;

            foreach (KeyValuePair<int, StruckTableVfxEffect> pair in source)
                target[pair.Key] = VfxRuntimeDataFactory.Create(pair.Value);
        }

        private static void MergeVfxRows(Dictionary<int, VfxRuntimeData> target, IReadOnlyDictionary<int, StruckTableVfxParticle> source)
        {
            if (target == null || source == null)
                return;

            foreach (KeyValuePair<int, StruckTableVfxParticle> pair in source)
                target[pair.Key] = VfxRuntimeDataFactory.Create(pair.Value);
        }

        public static TableCrowdControl LoadCrowdControlTable(bool forceReload = true)
        {
            return LoadTable<TableCrowdControl>(ConfigAddressableTable.TableCrowdControl.Path, forceReload);
        }

        public static TableCrowdControlKnockBack LoadCrowdControlKnockBackTable(bool forceReload = true)
        {
            return LoadTable<TableCrowdControlKnockBack>(ConfigAddressableTable.TableCrowdControlKnockBack.Path, forceReload);
        }

        public static TableCrowdControlKnockDown LoadCrowdControlKnockDownTable(bool forceReload = true)
        {
            return LoadTable<TableCrowdControlKnockDown>(ConfigAddressableTable.TableCrowdControlKnockDown.Path, forceReload);
        }

        public static TableCrowdControlKnockUp LoadCrowdControlKnockUpTable(bool forceReload = true)
        {
            return LoadTable<TableCrowdControlKnockUp>(ConfigAddressableTable.TableCrowdControlKnockUp.Path, forceReload);
        }

        public static TableWindow LoadWindowTable(bool forceReload = true)
        {
            return LoadTable<TableWindow>(ConfigAddressableTable.TableWindow.Path, forceReload);
        }

        public static TableSound LoadSoundTable(bool forceReload = true)
        {
            return LoadTable<TableSound>(ConfigAddressableTable.TableSound.Path, forceReload);
        }

        public static TableSoundBgm LoadSoundBgmTable(bool forceReload = true)
            => TryLoadOptionalTable<TableSoundBgm>(ConfigAddressableTable.TableSoundBgm.Path, forceReload);

        public static TableSoundAmbient LoadSoundAmbientTable(bool forceReload = true)
            => TryLoadOptionalTable<TableSoundAmbient>(ConfigAddressableTable.TableSoundAmbient.Path, forceReload);

        public static TableSoundSfx LoadSoundSfxTable(bool forceReload = true)
            => TryLoadOptionalTable<TableSoundSfx>(ConfigAddressableTable.TableSoundSfx.Path, forceReload);

        public static TableSoundVariant LoadSoundVariantTable(bool forceReload = true)
            => TryLoadOptionalTable<TableSoundVariant>(ConfigAddressableTable.TableSoundVariant.Path, forceReload);

        /// <summary>
        /// 아직 생성되지 않은 선택 테이블은 오류 없이 null로 반환합니다.
        /// </summary>
        /// <typeparam name="TTable">로드할 테이블 타입입니다.</typeparam>
        /// <param name="assetPath">Unity 프로젝트 기준 테이블 경로입니다.</param>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 테이블 또는 null입니다.</returns>
        private static TTable TryLoadOptionalTable<TTable>(string assetPath, bool forceReload)
            where TTable : class, ITableParser, new()
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.Combine(projectRoot ?? string.Empty, assetPath);
            if (!File.Exists(fullPath))
                return null;

            return LoadTable<TTable>(assetPath, forceReload);
        }

        /// <summary>
        /// 에디터 환경에서 projectile.txt 공용 Row와 projectile_linear/arc/path 상세 Row를 하나의 조회 테이블로 병합해 로드합니다.
        /// - 공용 Row가 없으면 상세 Row만으로는 병합하지 않습니다.
        /// - 상세 Row가 없으면 공용 Row의 기본값 또는 레거시 상세 컬럼 값을 유지합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>병합된 Projectile 테이블입니다. 로드된 Row가 없으면 null을 반환합니다.</returns>
        public static TableProjectile LoadProjectileTable(bool forceReload = true)
        {
            var merged = new TableProjectile();

            MergeProjectileRows(merged, TryLoadProjectilePart<TableProjectile>(ConfigAddressableTable.TableProjectile.Path, forceReload));
            merged.MergePathDetails(TryLoadProjectilePart<TableProjectilePath>(ConfigAddressableTable.TableProjectilePath.Path, forceReload)?.GetDatas());
            merged.MergeArcDetails(TryLoadProjectilePart<TableProjectileArc>(ConfigAddressableTable.TableProjectileArc.Path, forceReload)?.GetDatas());
            merged.MergeLinearDetails(TryLoadProjectilePart<TableProjectileLinear>(ConfigAddressableTable.TableProjectileLinear.Path, forceReload)?.GetDatas());
            merged.MergeLinearThenSegmentsDetails(TryLoadProjectilePart<TableProjectileLinearThenSegments>(ConfigAddressableTable.TableProjectileLinearThenSegments.Path, forceReload)?.GetDatas());

            return merged.GetCount() > 0 ? merged : null;
        }

        /// <summary>
        /// projectile_linear 테이블을 단독 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 projectile_linear 테이블입니다.</returns>
        public static TableProjectileLinear LoadProjectileLinearTable(bool forceReload = true)
            => LoadTable<TableProjectileLinear>(ConfigAddressableTable.TableProjectileLinear.Path, forceReload);

        /// <summary>
        /// projectile_arc 테이블을 단독 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 projectile_arc 테이블입니다.</returns>
        public static TableProjectileArc LoadProjectileArcTable(bool forceReload = true)
            => LoadTable<TableProjectileArc>(ConfigAddressableTable.TableProjectileArc.Path, forceReload);

        /// <summary>
        /// projectile_path 테이블을 단독 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 projectile_path 테이블입니다.</returns>
        public static TableProjectilePath LoadProjectilePathTable(bool forceReload = true)
            => LoadTable<TableProjectilePath>(ConfigAddressableTable.TableProjectilePath.Path, forceReload);

        /// <summary>
        /// projectile_linear_then_segments 테이블을 단독 로드합니다.
        /// </summary>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 projectile_linear_then_segments 테이블입니다.</returns>
        public static TableProjectileLinearThenSegments LoadProjectileLinearThenSegmentsTable(bool forceReload = true)
            => LoadTable<TableProjectileLinearThenSegments>(ConfigAddressableTable.TableProjectileLinearThenSegments.Path, forceReload);

        /// <summary>
        /// Projectile 계열 테이블 캐시를 모두 해제합니다.
        /// </summary>
        public static void UnloadProjectileTables()
        {
            TableLoaderManagerBase.Unload(ConfigAddressableTable.TableProjectile.Path);
            TableLoaderManagerBase.Unload(ConfigAddressableTable.TableProjectileLinear.Path);
            TableLoaderManagerBase.Unload(ConfigAddressableTable.TableProjectileArc.Path);
            TableLoaderManagerBase.Unload(ConfigAddressableTable.TableProjectilePath.Path);
            TableLoaderManagerBase.Unload(ConfigAddressableTable.TableProjectileLinearThenSegments.Path);
        }

        /// <summary>
        /// 테이블 파일이 존재할 때만 Projectile 부분 테이블을 로드합니다.
        /// </summary>
        /// <typeparam name="TTable">로드할 Projectile 테이블 타입입니다.</typeparam>
        /// <param name="assetPath">Unity 프로젝트 기준 테이블 경로입니다.</param>
        /// <param name="forceReload">캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 테이블 또는 null입니다.</returns>
        private static TTable TryLoadProjectilePart<TTable>(string assetPath, bool forceReload)
            where TTable : class, ITableParser, new()
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.Combine(projectRoot ?? string.Empty, assetPath);
            if (!File.Exists(fullPath))
                return null;

            return LoadTable<TTable>(assetPath, forceReload);
        }

        /// <summary>
        /// 부분 Projectile 테이블의 Row를 병합 테이블에 추가합니다.
        /// </summary>
        /// <param name="target">병합 대상 테이블입니다.</param>
        /// <param name="source">병합할 부분 테이블입니다.</param>
        private static void MergeProjectileRows(TableProjectile target, DefaultTable<StruckTableProjectile> source)
        {
            if (target == null || source == null)
                return;

            target.MergeFrom(source.GetDatas());
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
            table = LoadTable<TTable>(path, forceReload);
            if (table == null)
            {
                Debug.LogError($"{tableFileName} 테이블을 불러오지 못 했습니다.");
                return;
            }

            int index = 0;
            foreach (var kv in table.GetDatas())
            {
                var row = kv.Value;
                nameList.Add(displayNameFunc(row));
                structTable.TryAdd(index++, row);
            }
        }
    }
}
