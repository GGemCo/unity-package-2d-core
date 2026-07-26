using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// 패키지별 런타임 테이블 팩을 우선 로드하는 게임 로딩 스텝입니다.
    /// </summary>
    /// <remarks>
    /// 팩이 없거나 로드에 실패하면 기존 개별 txt 테이블 로딩으로 되돌아가도록 구성할 수 있습니다.
    /// </remarks>
    public sealed class TablePackLoadStep : GameLoadStepBase
    {
        private readonly TableLoaderBase _tableLoader;
        private readonly AddressableAssetInfo _tablePack;
        private readonly List<AddressableAssetInfo> _fallbackTables;
        private readonly bool _fallbackToIndividualTables;
        private const float FallbackStartProgress = 0.25f;

        /// <summary>
        /// 테이블 팩 로딩 스텝을 생성합니다.
        /// </summary>
        /// <param name="id">로딩 스텝 식별자입니다.</param>
        /// <param name="order">실행 순서입니다.</param>
        /// <param name="localizedKey">진행률 UI에 표시할 로컬라이징 키입니다.</param>
        /// <param name="tableLoader">테이블 파서 레지스트리를 보유한 로더입니다.</param>
        /// <param name="tablePack">우선 로드할 런타임 테이블 팩 Addressables 정보입니다.</param>
        /// <param name="fallbackTables">팩 로드 실패 시 사용할 기존 개별 테이블 목록입니다.</param>
        /// <param name="fallbackToIndividualTables">팩 로드 실패 시 개별 테이블 로딩으로 되돌아갈지 여부입니다.</param>
        public TablePackLoadStep(
            string id,
            int order,
            string localizedKey,
            TableLoaderBase tableLoader,
            AddressableAssetInfo tablePack,
            List<AddressableAssetInfo> fallbackTables,
            bool fallbackToIndividualTables = true)
            : base(id, order, localizedKey)
        {
            _tableLoader = tableLoader;
            _tablePack = tablePack;
            _fallbackTables = fallbackTables ?? new List<AddressableAssetInfo>();
            _fallbackToIndividualTables = fallbackToIndividualTables;
        }

        /// <summary>
        /// 테이블 팩을 로드하고, 실패 시 선택적으로 개별 테이블 로딩을 수행합니다.
        /// </summary>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        public override IEnumerator Run()
        {
            if (_tableLoader == null)
            {
                GcLogger.LogError("[TablePackLoadStep] TableLoader가 없습니다.");
                progress = 1f;
                yield break;
            }

            bool packLoaded = false;
            if (_tablePack != null)
            {
                Task<bool> packTask = _tableLoader.LoadDataPack(_tablePack);
                while (!packTask.IsCompleted)
                {
                    // 팩은 단일 Addressables 요청이므로 세부 진행률 대신 스텝이 살아 있음을 표시합니다.
                    progress = FallbackStartProgress;
                    yield return null;
                }

                if (packTask.IsCanceled)
                {
                    GcLogger.LogWarning($"[TablePackLoadStep] 테이블 팩 로드가 취소되었습니다. pack={_tablePack?.Key}");
                }
                else if (packTask.IsFaulted)
                {
                    GcLogger.LogError($"[TablePackLoadStep] 테이블 팩 로드 중 예외가 발생했습니다. {packTask.Exception?.GetBaseException().Message}");
                }
                else
                {
                    packLoaded = packTask.Result;
                }
            }

            if (packLoaded)
            {
                // 기존 팩이 새로 추가된 선택 테이블을 아직 포함하지 않을 수 있으므로,
                // 팩에서 주입되지 않은 항목만 개별 Addressables로 보완합니다.
                yield return LoadFallbackTables(onlyMissing: true);
                progress = 1f;
                yield break;
            }

            if (!_fallbackToIndividualTables || _fallbackTables.Count == 0)
            {
                progress = 1f;
                yield break;
            }

            GcLogger.LogWarning($"[TablePackLoadStep] 테이블 팩을 사용할 수 없어 개별 테이블 로딩으로 전환합니다. pack={_tablePack?.Key}");
            yield return LoadFallbackTables(onlyMissing: false);
            progress = 1f;
        }

        /// <summary>
        /// 기존 개별 txt 테이블 목록을 순차 로드합니다.
        /// </summary>
        /// <param name="onlyMissing">이미 팩에서 로드된 테이블을 건너뛸지 여부입니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator LoadFallbackTables(bool onlyMissing)
        {
            int fileCount = _fallbackTables.Count;
            for (int i = 0; i < fileCount; i++)
            {
                AddressableAssetInfo info = _fallbackTables[i];
                if (info == null)
                {
                    progress = CalculateFallbackProgress(i + 1, fileCount);
                    continue;
                }

                string tableKey = !string.IsNullOrWhiteSpace(info.Etc1)
                    ? info.Etc1
                    : info.Key;
                if (onlyMissing && _tableLoader.IsTableLoaded(tableKey))
                {
                    progress = CalculateFallbackProgress(i + 1, fileCount);
                    continue;
                }

                Task task = _tableLoader.LoadDataFile(info);

                while (!task.IsCompleted)
                {
                    progress = CalculateFallbackProgress(i, fileCount);
                    yield return null;
                }

                if (task.IsCanceled)
                {
                    GcLogger.LogWarning($"[TablePackLoadStep] 개별 테이블 로드가 취소되었습니다. key={info?.Key}");
                }
                else if (task.IsFaulted)
                {
                    GcLogger.LogError($"[TablePackLoadStep] 개별 테이블 로드 중 예외가 발생했습니다. key={info?.Key}, error={task.Exception?.GetBaseException().Message}");
                }

                progress = CalculateFallbackProgress(i + 1, fileCount);
            }
        }

        /// <summary>
        /// 팩 로드 실패 후 fallback 개별 로딩에서 진행률이 뒤로 가지 않도록 보정합니다.
        /// </summary>
        /// <param name="loadedCount">완료된 개별 테이블 수입니다.</param>
        /// <param name="totalCount">전체 개별 테이블 수입니다.</param>
        /// <returns>0~1 사이의 보정된 진행률입니다.</returns>
        private static float CalculateFallbackProgress(int loadedCount, int totalCount)
        {
            if (totalCount <= 0)
                return 1f;

            float ratio = (float)loadedCount / totalCount;
            return FallbackStartProgress + ratio * (1f - FallbackStartProgress);
        }
    }
}
