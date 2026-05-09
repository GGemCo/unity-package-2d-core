using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// 개별 Addressables txt 테이블 목록을 순차 로드하는 게임 로딩 스텝입니다.
    /// </summary>
    /// <remarks>
    /// 런타임 테이블 팩이 없을 때 fallback 경로로도 사용됩니다.
    /// </remarks>
    public sealed class TableLoadStep : GameLoadStepBase
    {
        private readonly TableLoaderBase _tableLoader;
        private readonly List<AddressableAssetInfo> _tables;

        /// <summary>
        /// 개별 테이블 로딩 스텝을 생성합니다.
        /// </summary>
        /// <param name="order">실행 순서입니다.</param>
        /// <param name="id">로딩 스텝 식별자입니다.</param>
        /// <param name="localizedKey">진행률 UI에 표시할 로컬라이징 키입니다.</param>
        /// <param name="tableLoader">테이블 파서 레지스트리를 보유한 로더입니다.</param>
        /// <param name="tables">로드할 개별 테이블 목록입니다.</param>
        public TableLoadStep(int order,
            string id,
            string localizedKey,
            TableLoaderBase tableLoader,
            List<AddressableAssetInfo> tables)
            : base(id, order, localizedKey)
        {
            _tableLoader = tableLoader;
            _tables = tables ?? new List<AddressableAssetInfo>();
        }

        /// <summary>
        /// 테이블 목록을 순서대로 로드하고 진행률을 갱신합니다.
        /// </summary>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        public override IEnumerator Run()
        {
            if (_tableLoader == null)
            {
                GcLogger.LogError("[TableLoadStep] TableLoader가 없습니다.");
                progress = 1f;
                yield break;
            }

            if (_tables.Count == 0)
            {
                progress = 1f;
                yield break;
            }

            int fileCount = _tables.Count;
            for (int i = 0; i < fileCount; i++)
            {
                AddressableAssetInfo info = _tables[i];
                Task task = _tableLoader.LoadDataFile(info);

                while (!task.IsCompleted)
                {
                    progress = (float)i / fileCount;
                    yield return null;
                }

                if (task.IsCanceled)
                {
                    GcLogger.LogWarning($"[TableLoadStep] 테이블 로드가 취소되었습니다. key={info?.Key}");
                }
                else if (task.IsFaulted)
                {
                    GcLogger.LogError($"[TableLoadStep] 테이블 로드 중 예외가 발생했습니다. key={info?.Key}, error={task.Exception?.GetBaseException().Message}");
                }

                progress = (float)(i + 1) / fileCount;
            }

            progress = 1f;
        }
    }
}
