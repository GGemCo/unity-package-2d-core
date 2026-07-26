using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    public class TableLoaderBase : MonoBehaviour
    {
        protected TableRegistry registry;
        private readonly HashSet<string> _loadedTableKeys =
            new(StringComparer.OrdinalIgnoreCase);

        private bool EnsureInitialized()
        {
            if (registry != null) return true;

            var manager = CompatObjectFind.FindFirst<TableLoaderManager>();
            if (manager == null)
            {
                GcLogger.LogWarning("[TableLoaderManager] Instance not found.");
                return false;
            }

            registry ??= new TableRegistry();
            return true;
        }

        public bool RegistryTable(ITableParser tableParser)
        {
            if (!EnsureInitialized())
                return false;

            registry.Register(tableParser);
            return true;
        }
        /// <summary>
        /// 등록된 테이블 파서에 원문 콘텐츠를 전달해 런타임 캐시를 구성합니다.
        /// </summary>
        /// <param name="key">파서 등록에 사용된 논리 테이블 이름입니다.</param>
        /// <param name="content">파싱할 테이블 원문입니다.</param>
        /// <returns>등록된 파서가 있고 로드 요청을 전달했으면 true를 반환합니다.</returns>
        public bool TryLoadTable(string key, string content)
        {
            if (!EnsureInitialized())
                return false;

            bool loaded = registry.TryLoad(key, content);
            if (loaded && !string.IsNullOrWhiteSpace(key))
            {
                _loadedTableKeys.Add(key);
            }

            return loaded;
        }

        /// <summary>
        /// 현재 로더 수명 동안 지정한 논리 테이블이 한 번 이상 정상 주입되었는지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 논리 테이블 이름입니다.</param>
        /// <returns>팩 또는 개별 파일에서 테이블을 정상 로드했으면 <see langword="true"/>입니다.</returns>
        public bool IsTableLoaded(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && _loadedTableKeys.Contains(key);
        }

        /// <summary>
        /// Addressables에 등록된 런타임 테이블 팩을 로드하고 포함된 테이블 원문을 각 파서에 주입합니다.
        /// </summary>
        /// <param name="info">테이블 팩 Addressables 정보입니다.</param>
        /// <returns>팩 로드와 테이블 주입이 성공하면 true를 반환합니다.</returns>
        public async Task<bool> LoadDataPack(AddressableAssetInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Key))
            {
                GcLogger.LogWarning("[TableLoader] 테이블 팩 정보가 없습니다.");
                return false;
            }

            if (!EnsureInitialized())
                return false;

            // 팩이 아직 생성되지 않은 개발 환경에서는 기존 개별 테이블 로딩으로 되돌아갈 수 있도록 존재 여부만 확인합니다.
            var locationsHandle = Addressables.LoadResourceLocationsAsync(info.Key);
            await locationsHandle.Task;

            if (!locationsHandle.Status.Equals(AsyncOperationStatus.Succeeded) || locationsHandle.Result.Count == 0)
            {
                Addressables.Release(locationsHandle);
                GcLogger.LogWarning($"[TableLoader] 테이블 팩이 Addressables에 등록되지 않았습니다. fallback으로 전환합니다. key={info.Key}");
                return false;
            }

            Addressables.Release(locationsHandle);

            var handle = Addressables.LoadAssetAsync<TextAsset>(info.Key);
            var asset = await handle.Task;

            if (!handle.Status.Equals(AsyncOperationStatus.Succeeded) || asset == null)
            {
                Addressables.Release(handle);
                GcLogger.LogWarning($"[TableLoader] 테이블 팩을 로드하지 못했습니다. key={info.Key}");
                return false;
            }

            byte[] bytes = asset.bytes;
            Addressables.Release(handle);

            if (!RuntimeTablePackCodec.TryDecode(bytes, out RuntimeTablePack pack, out string error))
            {
                GcLogger.LogError($"[TableLoader] 테이블 팩 해석에 실패했습니다. key={info.Key}, error={error}");
                return false;
            }

            if (!string.IsNullOrEmpty(info.Etc1) &&
                !string.Equals(info.Etc1, pack.PackageId, StringComparison.OrdinalIgnoreCase))
            {
                GcLogger.LogWarning($"[TableLoader] 테이블 팩 패키지 식별자가 다릅니다. expected={info.Etc1}, actual={pack.PackageId}");
            }

            int loadedCount = 0;
            for (int i = 0; i < pack.Entries.Count; i++)
            {
                RuntimeTablePackEntry entry = pack.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.TableName))
                {
                    GcLogger.LogWarning($"[TableLoader] 테이블 팩 엔트리의 테이블 이름이 비어 있습니다. pack={info.Key}, index={i}");
                    continue;
                }

                if (!TryLoadTable(entry.TableName, entry.Content))
                {
                    GcLogger.LogWarning($"[TableLoader] 등록되지 않은 테이블 키입니다. table={entry.TableName}, pack={info.Key}");
                    continue;
                }

                loadedCount++;
            }

            return loadedCount > 0;
        }

        /// <summary>
        /// 제네릭을 사용하여 Addressables에서 설정을 로드하는 함수
        /// </summary>
        private async Task<string> LoadTextAsync(string key)
        {
            // 키가 Addressables에 등록되어 있는지 확인
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            await locationsHandle.Task;

            if (!locationsHandle.Status.Equals(AsyncOperationStatus.Succeeded) || locationsHandle.Result.Count == 0)
            {
                if (!IsOptionalMissingTable(key))
                {
                    GcLogger.LogError($"[AddressableSettingsLoader] '{key}' 가 Addressables에 등록되지 않았습니다. '{key}' 를 생성한 후 {ConfigDefine.NameSDK}Tool > 기본 셋팅하기 메뉴를 열고 Addressable 추가하기 버튼을 클릭해주세요.");
                }

                Addressables.Release(locationsHandle);
                return null;
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(key);
            var asset = await handle.Task;
            Addressables.Release(locationsHandle);

            string content = asset != null ? asset.text : null;

            // 여기가 핵심: 사용 직후 해제
            Addressables.Release(handle);

            return content;
        }

        /// <summary>
        /// 누락되어도 로딩 오류로 취급하지 않을 선택 테이블인지 확인합니다.
        /// map_sound, monster_combat_profile과 일부 신규 variant 테이블은 데이터 파일이 생성되기 전까지 선택 사항으로 처리합니다.
        /// </summary>
        /// <param name="key">Addressables 테이블 키입니다.</param>
        /// <returns>누락을 허용하면 true를 반환합니다.</returns>
        private static bool IsOptionalMissingTable(string key)
        {
            return key == ConfigAddressableTable.TableMapSound.Key
                   || key == ConfigAddressableTable.TableMonsterCombatProfile.Key
                   || key == ConfigAddressableTable.TableSoundUsageManifest.Key
                   || key == ConfigAddressableTable.TableProjectileLinear.Key
                   || key == ConfigAddressableTable.TableProjectileArc.Key
                   || key == ConfigAddressableTable.TableProjectilePath.Key
                   || key == ConfigAddressableTable.TableProjectileLinearThenSegments.Key
                   || key == ConfigAddressableTable.TableSoundBgm.Key
                   || key == ConfigAddressableTable.TableSoundAmbient.Key
                   || key == ConfigAddressableTable.TableSoundSfx.Key
                   || key == ConfigAddressableTable.TableSoundVariant.Key
                   || key == ConfigAddressableTable.TableVfx.Key
                   || key == ConfigAddressableTable.TableVfxVariant.Key;
        }

        /// <summary>
        /// 개별 Addressables txt 테이블을 로드해 등록된 테이블 파서에 전달합니다.
        /// </summary>
        /// <param name="info">개별 테이블 Addressables 정보입니다.</param>
        public async Task LoadDataFile(AddressableAssetInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Key))
            {
                GcLogger.LogWarning("[TableLoader] 개별 테이블 정보가 없습니다.");
                return;
            }

            var content = await LoadTextAsync(info.Key);
            if (string.IsNullOrEmpty(content)) return;

            if (!TryLoadTable(info.Etc1, content))
                GcLogger.LogWarning($"[TableLoader] Unregistered table key: {info.Etc1}");
        }
        // ===============================
        // Generic Helper (공통 로깅/널 처리)
        // ===============================
        protected TRow GetData<TTable, TRow>(
            TTable table,
            int uid,
            string label,
            Func<TTable, int, TRow> getFunc,
            bool logIfMissing = true)
            where TRow : class
        {
            if (table == null)
            {
                if (logIfMissing)
                    GcLogger.LogWarning($"[Table] {label} table is null.");
                return null;
            }

            var row = getFunc(table, uid);
            if (row == null && logIfMissing)
                GcLogger.LogWarning($"[Table] {label} not found. uid={uid}");
            return row;
        }

        protected bool TryGetData<TTable, TRow>(
            TTable table,
            int uid,
            out TRow row,
            string label,
            Func<TTable, int, TRow> getFunc,
            bool logIfMissing = false)
            where TRow : class
        {
            row = GetData(table, uid, label, getFunc, logIfMissing);
            return row != null;
        }
    }
}
