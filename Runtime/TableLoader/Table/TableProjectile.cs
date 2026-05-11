using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Projectile 계열 테이블이 공통으로 사용하는 Row 구조입니다.
    /// - projectile_linear, projectile_arc, projectile_path 테이블은 이 구조로 파싱됩니다.
    /// - 기존 projectile 테이블도 마이그레이션 호환을 위해 같은 구조를 유지합니다.
    /// </summary>
    public class StruckTableProjectile
    {
        public int Uid;
        public ProjectileConstants.Type Type;
        public string Name;
        public int VfxUid;
        public float VfxScale;
        public int MoveSpeed;
        public int ArcHeightMin;
        public int ArcHeightMax;
        public Vector2 StartPosition;
        public Vector2 ColliderSize;

        /// <summary>
        /// Projectile 로컬 좌표 기준 Collider 중심 오프셋입니다.
        /// - (0,0)이면 기존 중심 기준 동작과 동일합니다.
        /// </summary>
        public Vector2 ColliderOffset;

        public int HitVfxUid;
        public ProjectileConstants.TargetType TargetType;
        public int TargetPositionRangeX;
        public int Count;
        public float SecDelayByOne;

        // ---- Boundary (Camera view) ----
        public ProjectileConstants.BoundaryMode BoundaryMode;
        public float BoundaryPadding;
        public int BounceMaxCount;
        public float BounceSpeedMultiplier;

        // ---- Damage policy ----
        public ProjectileConstants.DamageApplyMode DamageApplyMode;
        public float TickDamageInterval;
        public bool TickOnSpawn;

        // ---- Path movement ----
        public ProjectileConstants.PathCoordinateMode PathCoordinateMode;
        public Vector2[] PathPoints = Array.Empty<Vector2>();
        public float PathDuration;
    }

    /// <summary>
    /// Projectile 분리 테이블의 공통 파서입니다.
    /// - 각 하위 테이블은 파일명만 다르고 동일한 Row 구조를 공유합니다.
    /// - 테이블별 기본 Type을 주입하므로 Type 컬럼을 생략해도 안전하게 동작합니다.
    /// </summary>
    public abstract class TableProjectileBase : DefaultTable<StruckTableProjectile>
    {
        /// <summary>
        /// 현재 테이블이 기본으로 부여할 Projectile 타입입니다.
        /// </summary>
        protected abstract ProjectileConstants.Type DefaultProjectileType { get; }

        /// <summary>
        /// 헤더/값 사전에서 Projectile Row를 생성합니다.
        /// - 공통 컬럼은 모든 분리 테이블에서 읽고, 없는 컬럼은 기본값으로 보정합니다.
        /// - legacy 컬럼명(EffectUid/EffectScale/HitEffectUid)도 함께 지원합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <returns>파싱된 Projectile Row입니다.</returns>
        protected override StruckTableProjectile BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableProjectile
            {
                Uid = GetInt(data, 0, "Uid"),
                Type = GetEnum(data, DefaultProjectileType, "Type"),
                Name = GetString(data, string.Empty, "Name"),
                VfxUid = GetInt(data, 0, "VfxUid", "EffectUid"),
                VfxScale = GetFloat(data, 1f, "VfxScale", "EffectScale"),
                MoveSpeed = GetInt(data, 0, "MoveSpeed"),
                ArcHeightMin = GetInt(data, 0, "ArcHeightMin"),
                ArcHeightMax = GetInt(data, 0, "ArcHeightMax"),
                StartPosition = GetVector2(data, Vector2.zero, "StartPosition"),
                ColliderSize = GetVector2(data, Vector2.zero, "ColliderSize"),
                ColliderOffset = GetVector2(data, Vector2.zero, "ColliderOffset"),
                HitVfxUid = GetInt(data, 0, "HitVfxUid", "HitEffectUid"),
                TargetType = GetEnum(data, ProjectileConstants.TargetType.None, "TargetType"),
                TargetPositionRangeX = GetInt(data, 0, "TargetPositionRangeX"),
                Count = GetInt(data, 1, "Count"),
                SecDelayByOne = GetFloat(data, 0f, "SecDelayByOne"),
                BoundaryMode = GetEnum(data, ProjectileConstants.BoundaryMode.Destroy, "BoundaryMode"),
                BoundaryPadding = GetFloat(data, 0f, "BoundaryPadding"),
                BounceMaxCount = GetInt(data, 0, "BounceMaxCount"),
                BounceSpeedMultiplier = GetFloat(data, 1f, "BounceSpeedMultiplier"),
                DamageApplyMode = GetEnum(data, ProjectileConstants.DamageApplyMode.OnHitDestroy, "DamageApplyMode"),
                TickDamageInterval = GetFloat(data, 0f, "TickDamageInterval"),
                TickOnSpawn = GetBool(data, false, "TickOnSpawn"),
                PathCoordinateMode = GetEnum(data, ProjectileConstants.PathCoordinateMode.StartRelative, "PathCoordinateMode"),
                PathPoints = GetVector2Array(data, "PathPoints"),
                PathDuration = GetFloat(data, 0f, "PathDuration"),
            };
        }

        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째로 발견된 문자열 값을 반환합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>컬럼 값 또는 기본값입니다.</returns>
        private static string GetString(Dictionary<string, string> data, string fallback, params string[] keys)
        {
            if (data == null || keys == null)
                return fallback;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (data.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return fallback;
        }

        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째 값을 정수로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>정수 값 또는 기본값입니다.</returns>
        private static int GetInt(Dictionary<string, string> data, int fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : MathHelper.ParseInt(value);
        }

        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째 값을 실수로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>실수 값 또는 기본값입니다.</returns>
        private static float GetFloat(Dictionary<string, string> data, float fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : MathHelper.ParseFloat(value);
        }

        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째 값을 bool로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>bool 값 또는 기본값입니다.</returns>
        private static bool GetBool(Dictionary<string, string> data, bool fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                   || value == "1";
        }

        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째 값을 enum으로 변환합니다.
        /// </summary>
        /// <typeparam name="TEnum">변환할 enum 타입입니다.</typeparam>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>enum 값 또는 기본값입니다.</returns>
        private static TEnum GetEnum<TEnum>(Dictionary<string, string> data, TEnum fallback, params string[] keys)
            where TEnum : struct, Enum
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : EnumHelper.ConvertEnum<TEnum>(value);
        }

        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째 값을 Vector2로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>Vector2 값 또는 기본값입니다.</returns>
        private static Vector2 GetVector2(Dictionary<string, string> data, Vector2 fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : ConvertVector2(value);
        }

        /// <summary>
        /// PathPoints 컬럼을 Vector2 배열로 변환합니다.
        /// - 구분자는 "|" 또는 ";"를 사용할 수 있습니다. 예: "0,0|120,40|240,0".
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="key">PathPoints 컬럼명입니다.</param>
        /// <returns>파싱된 경로 점 배열입니다.</returns>
        private static Vector2[] GetVector2Array(Dictionary<string, string> data, string key)
        {
            string value = GetString(data, null, key);
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<Vector2>();

            string[] tokens = value.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return Array.Empty<Vector2>();

            var points = new Vector2[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                points[i] = ConvertVector2(tokens[i].Trim());

            return points;
        }
    }

    /// <summary>
    /// 기존 projectile 테이블 파서입니다.
    /// - 새 분리 테이블로 이전하는 동안 레거시 데이터를 읽기 위해 유지합니다.
    /// </summary>
    public class TableProjectile : TableProjectileBase
    {
        public override string Key => ConfigAddressableTable.Projectile;
        protected override ProjectileConstants.Type DefaultProjectileType => ProjectileConstants.Type.Default;

        /// <summary>
        /// 다른 Projectile 테이블의 Row를 현재 캐시에 병합합니다.
        /// - 동일 UID가 있으면 나중에 병합된 Row가 우선합니다.
        /// - 에디터 드롭다운처럼 분리 테이블 전체를 하나로 보여줄 때 사용합니다.
        /// </summary>
        /// <param name="source">병합할 Row 사전입니다.</param>
        public void MergeFrom(IReadOnlyDictionary<int, StruckTableProjectile> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<int, StruckTableProjectile> pair in source)
                SetDataByUid(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// projectile_linear 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectileLinear : TableProjectileBase
    {
        public override string Key => ConfigAddressableTable.ProjectileLinear;
        protected override ProjectileConstants.Type DefaultProjectileType => ProjectileConstants.Type.Linear;
    }

    /// <summary>
    /// projectile_arc 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectileArc : TableProjectileBase
    {
        public override string Key => ConfigAddressableTable.ProjectileArc;
        protected override ProjectileConstants.Type DefaultProjectileType => ProjectileConstants.Type.Arc;
    }

    /// <summary>
    /// projectile_path 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectilePath : TableProjectileBase
    {
        public override string Key => ConfigAddressableTable.ProjectilePath;
        protected override ProjectileConstants.Type DefaultProjectileType => ProjectileConstants.Type.Path;
    }
}
