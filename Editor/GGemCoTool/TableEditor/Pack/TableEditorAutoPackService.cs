using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 조건부 Auto Pack 요청의 처리 상태입니다.
    /// </summary>
    public enum TableEditorAutoPackStatus
    {
        /// <summary>원본 테이블이 변경되지 않아 실행하지 않았습니다.</summary>
        SkippedUnchanged = 0,

        /// <summary>Auto Pack 설정이 비활성화되어 실행하지 않았습니다.</summary>
        SkippedDisabled = 1,

        /// <summary>런타임 테이블 pack 생성에 성공했습니다.</summary>
        Built = 2,

        /// <summary>런타임 테이블 pack 생성에 실패했습니다.</summary>
        Failed = 3,
    }

    /// <summary>
    /// 조건부 Auto Pack 요청의 상태와 사용자 안내 메시지를 제공합니다.
    /// </summary>
    public sealed class TableEditorAutoPackResult
    {
        /// <summary>
        /// Auto Pack 처리 상태입니다.
        /// </summary>
        public TableEditorAutoPackStatus Status { get; }

        /// <summary>
        /// 처리 결과를 설명하는 메시지입니다.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 실제 pack 빌드를 시도했는지 여부입니다.
        /// </summary>
        public bool WasAttempted =>
            Status == TableEditorAutoPackStatus.Built ||
            Status == TableEditorAutoPackStatus.Failed;

        /// <summary>
        /// 실제 pack 빌드에 성공했는지 여부입니다.
        /// </summary>
        public bool BuildSucceeded =>
            Status == TableEditorAutoPackStatus.Built;

        /// <summary>
        /// 조건부 Auto Pack 결과를 생성합니다.
        /// </summary>
        /// <param name="status">처리 상태입니다.</param>
        /// <param name="message">사용자 안내 메시지입니다.</param>
        public TableEditorAutoPackResult(
            TableEditorAutoPackStatus status,
            string message)
        {
            Status = status;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// TableEditor 저장 후 선택된 테이블이 속한 패키지의 런타임 테이블 pack 생성을 중계합니다.
    /// </summary>
    public static class TableEditorAutoPackService
    {
        private static List<ITableEditorPackBuildProvider> _providers;

        /// <summary>
        /// 원본 변경 여부와 공유 Auto Pack 설정을 확인한 후 Table Key에 맞는 pack을 재생성합니다.
        /// </summary>
        /// <param name="tableKey">TableEditorRegistry에 등록된 테이블 키입니다.</param>
        /// <param name="sourceChanged">원본 테이블 파일이 실제로 변경되었는지 여부입니다.</param>
        /// <returns>실행 생략, 성공 또는 실패 상태를 포함하는 결과입니다.</returns>
        public static TableEditorAutoPackResult TryBuildIfEnabled(
            string tableKey,
            bool sourceChanged)
        {
            if (!sourceChanged)
            {
                return new TableEditorAutoPackResult(
                    TableEditorAutoPackStatus.SkippedUnchanged,
                    "원본 테이블 변경사항이 없어 Auto Pack을 실행하지 않았습니다.");
            }

            if (!TableEditorAutoPackSettings.IsEnabled)
            {
                return new TableEditorAutoPackResult(
                    TableEditorAutoPackStatus.SkippedDisabled,
                    "Auto Pack 설정이 비활성화되어 pack을 생성하지 않았습니다.");
            }

            if (string.IsNullOrWhiteSpace(tableKey))
            {
                return new TableEditorAutoPackResult(
                    TableEditorAutoPackStatus.Failed,
                    "Auto Pack 대상 테이블 키가 없습니다.");
            }

            try
            {
                TableEditorTableDefinition tableDefinition =
                    TableEditorRegistry.FindByKey(tableKey);
                if (tableDefinition == null)
                {
                    return new TableEditorAutoPackResult(
                        TableEditorAutoPackStatus.Failed,
                        $"Auto Pack 대상 테이블 정의를 찾지 못했습니다. table={tableKey}");
                }

                bool built = TryBuildForTable(
                    tableDefinition,
                    out string message);
                return new TableEditorAutoPackResult(
                    built
                        ? TableEditorAutoPackStatus.Built
                        : TableEditorAutoPackStatus.Failed,
                    message);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return new TableEditorAutoPackResult(
                    TableEditorAutoPackStatus.Failed,
                    $"Auto Pack 처리 중 예외가 발생했습니다. table={tableKey}, error={ex.Message}");
            }
        }

        /// <summary>
        /// 지정한 테이블 정의를 처리할 수 있는 Provider를 찾아 런타임 테이블 pack을 재생성합니다.
        /// </summary>
        /// <param name="tableDefinition">저장이 완료된 테이블 정의입니다.</param>
        /// <param name="message">처리 결과를 설명하는 메시지입니다.</param>
        /// <returns>pack 생성에 성공하면 true를 반환합니다.</returns>
        public static bool TryBuildForTable(TableEditorTableDefinition tableDefinition, out string message)
        {
            message = string.Empty;
            if (tableDefinition == null)
            {
                message = "자동 pack 대상 테이블 정보가 없습니다.";
                return false;
            }

            IReadOnlyList<ITableEditorPackBuildProvider> providers = GetProviders();
            for (int i = 0; i < providers.Count; i++)
            {
                ITableEditorPackBuildProvider provider = providers[i];
                if (provider == null || !provider.CanBuild(tableDefinition))
                    continue;

                try
                {
                    bool built = provider.TryBuild(out string providerMessage);
                    message = string.IsNullOrWhiteSpace(providerMessage)
                        ? $"{provider.PackageName} 런타임 테이블 pack 생성 결과: {built}"
                        : providerMessage;
                    return built;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    message = $"{provider.PackageName} 런타임 테이블 pack 생성 중 예외가 발생했습니다. {ex.Message}";
                    return false;
                }
            }

            message = $"자동 pack Provider를 찾지 못했습니다. package={tableDefinition.PackageName}, table={tableDefinition.TableKey}";
            return false;
        }

        /// <summary>
        /// 현재 Editor 도메인에 등록된 pack Provider 목록을 우선순위 기준으로 조회합니다.
        /// </summary>
        /// <returns>정렬된 pack Provider 목록입니다.</returns>
        private static IReadOnlyList<ITableEditorPackBuildProvider> GetProviders()
        {
            if (_providers != null)
                return _providers;

            _providers = new List<ITableEditorPackBuildProvider>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ITableEditorPackBuildProvider>())
            {
                if (type == null || type.IsAbstract || type.IsInterface)
                    continue;

                try
                {
                    if (Activator.CreateInstance(type) is ITableEditorPackBuildProvider provider)
                        _providers.Add(provider);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TableEditor] 자동 pack Provider를 생성하지 못했습니다. type={type.FullName}, error={ex.Message}");
                }
            }

            _providers.Sort(static (left, right) =>
            {
                int order = left.Order.CompareTo(right.Order);
                if (order != 0)
                    return order;

                return string.Compare(left.PackageName, right.PackageName, StringComparison.OrdinalIgnoreCase);
            });

            return _providers;
        }
    }

    /// <summary>
    /// TableEditor 자동 pack 기능에서 패키지별 pack 생성 정보를 제공하는 Editor 전용 Provider 계약입니다.
    /// </summary>
    public interface ITableEditorPackBuildProvider
    {
        /// <summary>
        /// Provider 실행 우선순위입니다. 숫자가 낮을수록 먼저 검사합니다.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Provider가 담당하는 패키지 표시 이름입니다.
        /// </summary>
        string PackageName { get; }

        /// <summary>
        /// 저장된 테이블이 이 Provider의 pack 생성 대상인지 확인합니다.
        /// </summary>
        /// <param name="tableDefinition">저장이 완료된 테이블 정의입니다.</param>
        /// <returns>처리 대상이면 true를 반환합니다.</returns>
        bool CanBuild(TableEditorTableDefinition tableDefinition);

        /// <summary>
        /// 담당 패키지의 런타임 테이블 pack을 재생성합니다.
        /// </summary>
        /// <param name="message">처리 결과 메시지입니다.</param>
        /// <returns>pack 생성에 성공하면 true를 반환합니다.</returns>
        bool TryBuild(out string message);
    }

    /// <summary>
    /// 런타임 테이블 pack 생성 Provider의 공통 비교와 실행 흐름을 제공합니다.
    /// </summary>
    public abstract class TableEditorPackBuildProviderBase : ITableEditorPackBuildProvider
    {
        /// <summary>
        /// Provider 실행 우선순위입니다. 숫자가 낮을수록 먼저 검사합니다.
        /// </summary>
        public virtual int Order => 0;

        /// <summary>
        /// Provider가 담당하는 패키지 표시 이름입니다.
        /// </summary>
        public abstract string PackageName { get; }

        /// <summary>
        /// 런타임 pack 내부에 기록할 패키지 식별자입니다.
        /// </summary>
        protected abstract string PackageId { get; }

        /// <summary>
        /// 생성할 런타임 테이블 pack의 Addressables 정보입니다.
        /// </summary>
        protected abstract AddressableAssetInfo PackInfo { get; }

        /// <summary>
        /// pack에 포함할 개별 테이블 Addressables 목록입니다.
        /// </summary>
        protected abstract IReadOnlyList<AddressableAssetInfo> Tables { get; }

        /// <summary>
        /// 저장된 테이블이 현재 Provider의 담당 패키지에 속하는지 확인합니다.
        /// </summary>
        /// <param name="tableDefinition">저장이 완료된 테이블 정의입니다.</param>
        /// <returns>담당 패키지이면 true를 반환합니다.</returns>
        public virtual bool CanBuild(TableEditorTableDefinition tableDefinition)
        {
            return tableDefinition != null
                   && string.Equals(tableDefinition.PackageName, PackageName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 담당 패키지의 전체 테이블 목록을 읽어 런타임 테이블 pack 파일을 다시 생성합니다.
        /// </summary>
        /// <param name="message">처리 결과 메시지입니다.</param>
        /// <returns>pack 생성에 성공하면 true를 반환합니다.</returns>
        public bool TryBuild(out string message)
        {
            bool built = RuntimeTablePackBuilder.Build(PackageId, PackInfo, Tables);
            message = built
                ? $"{PackageName} 런타임 테이블 pack을 재생성했습니다."
                : $"{PackageName} 런타임 테이블 pack 생성에 실패했습니다.";
            return built;
        }
    }

    /// <summary>
    /// Core 패키지 테이블 저장 후 Core 런타임 테이블 pack을 재생성하는 Provider입니다.
    /// </summary>
    internal sealed class TableEditorCorePackBuildProvider : TableEditorPackBuildProviderBase
    {
        /// <summary>
        /// Provider가 담당하는 패키지 표시 이름입니다.
        /// </summary>
        public override string PackageName => "Core";

        /// <summary>
        /// Core 런타임 pack 내부에 기록할 패키지 식별자입니다.
        /// </summary>
        protected override string PackageId => ConfigAddressableTablePack.PackageCore;

        /// <summary>
        /// Core 런타임 테이블 pack의 Addressables 정보입니다.
        /// </summary>
        protected override AddressableAssetInfo PackInfo => ConfigAddressableTablePack.Core;

        /// <summary>
        /// Core 런타임 pack에 포함할 개별 테이블 목록입니다.
        /// </summary>
        protected override IReadOnlyList<AddressableAssetInfo> Tables => ConfigAddressableTable.All;
    }
}
