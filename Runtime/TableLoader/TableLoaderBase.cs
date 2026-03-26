using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    public class TableLoaderBase : MonoBehaviour
    {
        protected TableRegistry registry;
        
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
        public bool TryLoadTable(string key, string content)
        {
            return registry.TryLoad(key, content);
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

        private static bool IsOptionalMissingTable(string key)
        {
            return false;
        }

        public async Task LoadDataFile(AddressableAssetInfo info)
        {
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