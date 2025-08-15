using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public sealed class TableLoadStep : GameLoadStepBase
    {
        private readonly TableLoaderManager _tableLoader;
        private readonly List<AddressableAssetInfo> _tables;

        public TableLoadStep(int order,
            string localizedKey,
            TableLoaderManager tableLoader,
            List<AddressableAssetInfo> tables)
            : base("core.table", order, localizedKey)
        {
            _tableLoader = tableLoader;
            _tables = tables ?? new List<AddressableAssetInfo>();
        }

        public override IEnumerator Run()
        {
            if (_tables.Count == 0)
            {
                progress = 1f;
                yield break;
            }

            int fileCount = _tables.Count;
            for (int i = 0; i < fileCount; i++)
            {
                var info = _tables[i];
                yield return _tableLoader.LoadDataFile(info);

                // per-file 비율
                progress = (float)(i + 1) / fileCount;
            }
            progress = 1f;
        }
    }
}