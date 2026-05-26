using System;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 테이블 저장 프로세서의 공통 처리 기반 클래스입니다.
    /// - 대상 테이블 판별
    /// - 문자열/Enum/경로 보조 유틸
    /// 을 공통화해 프로세서별 책임을 단순화합니다.
    /// </summary>
    internal abstract class TableEditorSaveProcessorBase : ITableEditorSaveProcessor
    {
        /// <summary>
        /// 처리 대상 테이블 키입니다.
        /// </summary>
        protected abstract string TargetTableKey { get; }

        /// <summary>
        /// 실행 우선순위입니다. 숫자가 낮을수록 먼저 실행됩니다.
        /// </summary>
        public abstract int Order { get; }

        /// <summary>
        /// 현재 저장 컨텍스트가 이 프로세서 대상 테이블인지 판별합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        /// <returns>대상 테이블이면 true를 반환합니다.</returns>
        public virtual bool CanProcess(TableEditorSaveContext context)
        {
            return context != null && context.IsTable(TargetTableKey);
        }

        /// <summary>
        /// 저장 전 처리 훅입니다. 필요 시 파생 클래스에서 override 합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public virtual void BeforeSave(TableEditorSaveContext context)
        {
        }

        /// <summary>
        /// 저장 후 처리 훅입니다. 필요 시 파생 클래스에서 override 합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public virtual void AfterSave(TableEditorSaveContext context)
        {
        }

        /// <summary>
        /// 행 데이터에서 헤더 값을 안전하게 꺼내고 공백을 제거합니다.
        /// </summary>
        /// <param name="row">대상 행입니다.</param>
        /// <param name="headerName">조회할 헤더명입니다.</param>
        /// <returns>정리된 문자열 값입니다. 없으면 빈 문자열을 반환합니다.</returns>
        protected static string GetTrimmedValue(TableEditorDocumentRow row, string headerName)
        {
            if (row == null || row.Values == null || string.IsNullOrWhiteSpace(headerName))
                return string.Empty;

            return row.Values.TryGetValue(headerName, out string value)
                ? (value ?? string.Empty).Trim()
                : string.Empty;
        }

        /// <summary>
        /// 문자열을 Enum으로 안전하게 변환합니다. 변환 실패 시 기본값(None/0)을 반환합니다.
        /// </summary>
        /// <typeparam name="TEnum">대상 Enum 타입입니다.</typeparam>
        /// <param name="rawValue">원본 문자열 값입니다.</param>
        /// <returns>변환된 Enum 값입니다.</returns>
        protected static TEnum ParseEnumOrDefault<TEnum>(string rawValue) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return default;

            return Enum.TryParse(rawValue.Trim(), true, out TEnum parsed)
                ? parsed
                : default;
        }

        /// <summary>
        /// 경로를 슬래시 형태로 정규화하고 양끝 공백/따옴표를 제거합니다.
        /// </summary>
        /// <param name="rawPath">원본 경로 문자열입니다.</param>
        /// <returns>정규화된 경로 문자열입니다.</returns>
        protected static string NormalizePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return string.Empty;

            string trimmed = rawPath.Trim().Trim('"');
            return ConfigAddressablePath.EnsureForwardSlashes(trimmed);
        }

        /// <summary>
        /// Uid 표시 문자열을 사용자 친화적으로 정규화합니다.
        /// </summary>
        /// <param name="uid">원본 Uid 문자열입니다.</param>
        /// <returns>표시용 Uid 문자열입니다.</returns>
        protected static string FormatUid(string uid)
        {
            return string.IsNullOrWhiteSpace(uid) ? "(빈 값)" : uid;
        }
    }
}
