using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TableLoaderManagerBase
    {
        // 경로 → 테이블 인스턴스 (행 타입 불문)
        private static readonly Dictionary<string, ITableParser> LoadedTables =
            new Dictionary<string, ITableParser>(StringComparer.Ordinal);

        private static bool TryGetLoaded<T>(string filePath, out T table) where T : class, ITableParser
        {
            if (LoadedTables.TryGetValue(filePath, out var t) && t is T cast)
            {
                table = cast; return true;
            }
            table = null; return false;
        }

        public static T LoadTable<T>(string filePath, bool forceReload = false)
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
                LoadedTables[filePath] = tableData;
                Debug.Log($"[TableLoader] 테이블 내용 교체. path={filePath}");
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
        public static bool Unload(string filePath) => LoadedTables.Remove(filePath);

        /// <summary>
        /// 모든 테이블 캐시 제거(프로젝트 리임포트/일괄 재로드 용)
        /// </summary>
        public static void UnloadAll() => LoadedTables.Clear();
    }
}