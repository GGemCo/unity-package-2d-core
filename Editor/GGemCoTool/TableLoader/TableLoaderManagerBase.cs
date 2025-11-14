using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TableLoaderManagerBase
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

        protected T LoadTable<T>(string filePath, bool forceReload = false)
            where T : class, ITableParser, new()
        {
            if (!forceReload && TryGetLoaded<T>(filePath, out var cached))
                return cached;

            T tableData;
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
    }
}