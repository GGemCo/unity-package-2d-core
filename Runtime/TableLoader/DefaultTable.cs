using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// (선택) UID/Name 패턴을 갖는 Row가 IUidName을 구현하면,
    /// 툴링/디버깅 시 공통 표기 규칙을 활용할 수 있습니다.
    /// </summary>
    public interface IUidName
    {
        /// <summary>테이블 고유 키(정수)</summary>
        int Uid { get; }
        /// <summary>표시용 이름</summary>
        string Name { get; }
    }

    /// <summary>
    /// 탭( \t ) 구분 텍스트 테이블을 파싱해 행( <typeparamref name="TRow"/> )을 캐시하는 기본 베이스 클래스.
    /// - 상속 클래스는 <see cref="BuildRow"/> 만 구현하면 됩니다.
    /// - 로드 순서는 <see cref="PreLoad"/> → <see cref="LoadData"/> → <see cref="OnLoadedData"/>.
    /// - 조회는 <see cref="GetDataByUid(int)"/>, <see cref="TryGetDataByUid(int, out TRow)"/>, <see cref="GetAll"/> 제공.
    /// </summary>
    /// <typeparam name="TRow">한 줄(레코드)을 표현하는 DTO 타입</typeparam>
    public abstract class DefaultTable<TRow> : ITableParser<TRow> where TRow : class
    {
        /// <summary>
        /// Addressables 테이블 키. 파생 클래스에서 올바른 키를 노출해야 합니다.
        /// </summary>
        public virtual string Key => ConfigAddressableTable.None;

        /// <summary>
        /// UID → Row 캐시. 테이블 로드 이후 모든 조회는 이 사전을 통해 수행됩니다.
        /// </summary>
        private readonly Dictionary<int, TRow> _table = new Dictionary<int, TRow>();

        /// <summary>
        /// 텍스트(탭 구분) 콘텐츠를 파싱하여 내부 캐시에 적재합니다.
        /// 첫 번째 라인은 헤더이며, 이후부터 데이터로 간주합니다.
        /// </summary>
        /// <param name="content">탭( \t ) 기준의 텍스트 테이블(헤더 포함)</param>
        public virtual void LoadData(string content)
        {
            PreLoad();

            if (string.IsNullOrWhiteSpace(content))
            {
                GcLogger.LogWarning($"[Table] Empty content: {GetType().Name}");
                return;
            }

            var lines = content.Split('\n');
            if (lines.Length == 0)
            {
                GcLogger.LogWarning($"[Table] No lines: {GetType().Name}");
                return;
            }

            var headers = lines[0].Trim().Split('\t');
            if (headers.Length == 0)
            {
                GcLogger.LogError($"[Table] No headers: {GetType().Name}");
                return;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("#"))
                    continue;

                var values = rawLine.Split('\t');
                if (values.Length < headers.Length)
                {
                    GcLogger.LogError($"[Table] 열 개수 불일치 line={i}: {rawLine}");
                    // 부족한 칸은 빈 문자열로 보정
                    Array.Resize(ref values, headers.Length);
                }

                // 헤더 → 값 사전 구성(필드명 기준 접근)
                var data = new Dictionary<string, string>(headers.Length);
                for (int j = 0; j < headers.Length; j++)
                {
                    var raw = values[j] ?? string.Empty;
                    // \n 이스케이프 복원 및 "None"/"NONE" 보정
                    data[headers[j].Trim()] = CheckNone(raw.Trim().Replace(@"\n", "\n"));
                }

                // 첫 번째 컬럼을 UID로 간주
                if (!int.TryParse(values[0], out var uid))
                {
                    GcLogger.LogError($"[Table] 잘못된 Uid at line={i}: '{values[0]}'");
                    continue;
                }

                try
                {
                    var row = BuildRow(data);
                    _table[uid] = row;
                    OnLoadedData(row); // (예: Localization 후처리 등)
                }
                catch (Exception e)
                {
                    GcLogger.LogError($"[Table] BuildRow failed at line={i} uid={uid} table={GetType().Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 로드 직전 훅. 상속 클래스가 필요 시 캐시 초기화, 전처리 등을 수행할 수 있습니다.
        /// </summary>
        protected virtual void PreLoad()
        {
            // 기본 구현: NOP
        }

        /// <summary>
        /// 로드 완료 직후의 후처리 훅. (예: 2차 인덱싱, 지역화 키 보정 등)
        /// </summary>
        /// <param name="row">방금 적재된 행</param>
        protected virtual void OnLoadedData(TRow row)
        {
            // 기본 구현: NOP
        }

        /// <summary>
        /// 헤더/값 사전을 받아 한 행을 강타입 DTO(<typeparamref name="TRow"/>)로 생성합니다.
        /// 필수 컬럼 검증/형변환/무결성 체크를 이 메서드에서 끝내는 것이 좋습니다.
        /// </summary>
        /// <param name="data">헤더명 → 값</param>
        /// <returns>생성된 DTO</returns>
        protected abstract TRow BuildRow(Dictionary<string, string> data);

        /// <summary>
        /// 내부 캐시에서 UID로 행을 조회합니다. 없으면 경고 로그 후 null 반환.
        /// </summary>
        /// <param name="uid">행 UID</param>
        /// <returns>행 또는 null</returns>
        private TRow GetRow(int uid)
        {
            if (!_table.TryGetValue(uid, out var row))
            {
                GcLogger.LogWarning($"[Table] Row not found: {GetType().Name}, uid={uid}");
                return null;
            }
            return row;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // ITableParser<TRow> 구현부 (강타입 접근)
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// UID로 강타입 행을 반환합니다. 없으면 null.
        /// </summary>
        public virtual TRow GetDataByUid(int uid) => GetRow(uid);

        /// <summary>
        /// UID로 강타입 행을 시도 조회합니다.
        /// </summary>
        public virtual bool TryGetDataByUid(int uid, out TRow row)
            => _table.TryGetValue(uid, out row);

        /// <summary>
        /// 전체 캐시를 읽기 전용 사전으로 노출합니다.
        /// </summary>
        public virtual IReadOnlyDictionary<int, TRow> GetAll() => _table;

        // ─────────────────────────────────────────────────────────────────────────────
        // 조회/유틸리티
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 내부 캐시를 직접 반환합니다. (에디터/디버그용)
        /// </summary>
        public Dictionary<int, TRow> GetDatas() => _table;

        /// <summary>
        /// 파생 테이블이 외부 테이블의 Row를 현재 캐시에 병합할 때 사용합니다.
        /// - 동일 UID가 이미 있으면 새 Row로 교체합니다.
        /// - 에디터용 병합 테이블처럼 원본 파일을 직접 파싱하지 않는 경우에만 사용합니다.
        /// </summary>
        /// <param name="uid">등록할 Row의 UID입니다.</param>
        /// <param name="row">캐시에 저장할 Row 데이터입니다.</param>
        protected void SetDataByUid(int uid, TRow row)
        {
            if (uid <= 0 || row == null)
                return;

            _table[uid] = row;
        }

        /// <summary>
        /// 적재된 행의 개수(캐시 크기)를 반환합니다.
        /// </summary>
        public int GetCount() => _table.Count;

        /// <summary>
        /// "a,b,c" 형태의 문자열을 int 배열로 변환합니다. "0"은 빈 배열 처리.
        /// </summary>
        protected static int[] ConvertIntArray(string value)
        {
            if (value == "0" || string.IsNullOrEmpty(CheckNone(value))) return Array.Empty<int>();
            string[] values = value.Split(',');
            int[] intArray = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
                intArray[i] = int.Parse(values[i]);
            return intArray;
        }

        /// <summary>
        /// "None"/"NONE" → 빈 문자열로 보정합니다.
        /// </summary>
        private static string CheckNone(string value)
            => (value == "None" || value == "NONE") ? string.Empty : value;

        /// <summary>
        /// "x,y" 문자열을 <see cref="Vector2"/> 로 변환합니다. 비어있으면 <see cref="Vector2.zero"/>.
        /// </summary>
        protected static Vector2 ConvertVector2(string value)
        {
            if (string.IsNullOrWhiteSpace(CheckNone(value))) return Vector2.zero;
            var parts = value.Split(',');
            var x = MathHelper.ParseFloat(parts.Length > 0 ? parts[0] : "0");
            var y = MathHelper.ParseFloat(parts.Length > 1 ? parts[1] : "0");
            return new Vector2(x, y);
        }

        /// <summary>
        /// "Y" → true, 그 외 false.
        /// </summary>
        protected static bool ConvertBoolean(string value) => value == "Y";

        /// <summary>
        /// 접미 규칙 문자열을 <see cref="ConfigCommon.SuffixType"/> 으로 변환합니다.
        /// </summary>
        protected static ConfigCommon.SuffixType ConvertSuffixType(string value)
            => EnumHelper.ConvertEnum<ConfigCommon.SuffixType>(value);

        /// <summary>문자열을 통화 타입으로 변환합니다.</summary>
        protected static CurrencyConstants.Type ConvertCurrencyType(string value)
            => EnumHelper.ConvertEnum<CurrencyConstants.Type>(value);

        /// <summary>문자열을 8방향 페이싱으로 변환합니다.</summary>
        protected static CharacterConstants.FacingDirection8 ConvertFacing(string value)
            => EnumHelper.ConvertEnum<CharacterConstants.FacingDirection8>(value);

        /// <summary>문자열을 애니메이션 컨트롤러 타입으로 변환합니다.</summary>
        protected static ConfigCommon.AnimationController ConvertAnimationController(string value)
            => EnumHelper.ConvertEnum<ConfigCommon.AnimationController>(value);

        /// <summary>문자열을 Y축 포지션 타입으로 변환합니다.</summary>
        protected static ConfigCommon.PositionYType ConvertPositionYType(string value)
            => EnumHelper.ConvertEnum<ConfigCommon.PositionYType>(value);
        
        /// <summary>
        /// .NET의 string.GetHashCode()는 프로세스/런타임에 따라 값이 달라질 수 있으므로(랜덤 시드),
        /// 테이블 토큰을 정수 ID로 변환할 때는 안정적인 해시를 사용한다.
        /// </summary>
        protected static int StableHash32(string s)
        {
            unchecked
            {
                // FNV-1a 32-bit
                const uint offset = 2166136261u;
                const uint prime = 16777619u;

                uint hash = offset;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= prime;
                }
                // int로 캐스팅(부호 포함). HashSet에는 동일성만 중요하므로 문제 없음.
                return (int)hash;
            }
        }
    }
}
