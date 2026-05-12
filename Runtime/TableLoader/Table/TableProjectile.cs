using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타겟 직선 이동 이후에 이어서 실행할 이동 세그먼트 데이터입니다.
    /// - Direction: 이동 방향입니다. 실제 해석 기준은 SegmentDirectionMode를 따릅니다.
    /// - Speed: 해당 세그먼트의 이동 속도입니다.
    /// - Distance: 해당 세그먼트에서 이동할 거리입니다.
    /// </summary>
    [Serializable]
    public struct ProjectileMoveSegment
    {
        public Vector2 Direction;
        public float Speed;
        public float Distance;

        /// <summary>
        /// 이동 세그먼트 값을 생성합니다.
        /// </summary>
        /// <param name="direction">세그먼트 방향입니다.</param>
        /// <param name="speed">세그먼트 속도입니다.</param>
        /// <param name="distance">세그먼트 이동 거리입니다.</param>
        public ProjectileMoveSegment(Vector2 direction, float speed, float distance)
        {
            Direction = direction;
            Speed = speed;
            Distance = distance;
        }
    }

    /// <summary>
    /// Projectile 런타임에서 사용하는 최종 병합 데이터입니다.
    /// - projectile.txt 공통 Row에 projectile_linear/arc/path 상세 Row를 UID 기준으로 덧입힌 결과입니다.
    /// - 런타임 생성 코드는 이 타입 하나만 바라보도록 유지합니다.
    /// </summary>
    public class StruckTableProjectile
    {
        public int Uid;
        public ProjectileConstants.Type Type;
        public string Name;
        public int VfxUid;
        public float VfxScale;
        public int MoveSpeed;
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
        public ProjectileConstants.DamageApplyMode DamageApplyMode;

        /// <summary>
        /// 이동 방향을 기준으로 발사체 Transform을 자동 회전할지 여부입니다.
        /// - 컬럼이 없는 기존 데이터는 true로 보정하여 기존 동작을 유지합니다.
        /// </summary>
        public bool RotateByMoveDirection = true;

        // ---- Linear detail ----
        public ProjectileConstants.BoundaryMode BoundaryMode;
        public float BoundaryPadding;
        public int BounceMaxCount;
        public float BounceSpeedMultiplier;

        // ---- Arc detail ----
        public int ArcHeightMin;
        public int ArcHeightMax;

        // ---- Path detail ----
        public float TickDamageInterval;
        public bool TickOnSpawn;
        public ProjectileConstants.PathCoordinateMode PathCoordinateMode;
        public Vector2[] PathPoints = Array.Empty<Vector2>();
        public float PathDuration;

        // ---- LinearThenSegments detail ----
        public ProjectileConstants.SegmentDirectionMode SegmentDirectionMode;
        public ProjectileConstants.SegmentRelativeAxesMode SegmentRelativeAxesMode;
        public ProjectileMoveSegment[] MoveSegments = Array.Empty<ProjectileMoveSegment>();
    }

    /// <summary>
    /// projectile_linear.txt Row 구조입니다.
    /// - 공통 정보는 projectile.txt에서 관리하고, 직선형 이동 옵션만 보관합니다.
    /// </summary>
    public sealed class StruckTableProjectileLinear
    {
        public int Uid;
        public ProjectileConstants.BoundaryMode BoundaryMode;
        public float BoundaryPadding;
        public int BounceMaxCount;
        public float BounceSpeedMultiplier;
    }

    /// <summary>
    /// projectile_arc.txt Row 구조입니다.
    /// - 공통 정보는 projectile.txt에서 관리하고, 포물선 이동 옵션만 보관합니다.
    /// </summary>
    public sealed class StruckTableProjectileArc
    {
        public int Uid;
        public int ArcHeightMin;
        public int ArcHeightMax;
    }

    /// <summary>
    /// projectile_path.txt Row 구조입니다.
    /// - 공통 정보는 projectile.txt에서 관리하고, 경로 이동/주기 데미지 옵션만 보관합니다.
    /// </summary>
    public sealed class StruckTableProjectilePath
    {
        public int Uid;
        public float TickDamageInterval;
        public bool TickOnSpawn;
        public ProjectileConstants.PathCoordinateMode PathCoordinateMode;
        public Vector2[] PathPoints = Array.Empty<Vector2>();
        public float PathDuration;
    }

    /// <summary>
    /// projectile_linear_then_segments.txt Row 구조입니다.
    /// - 공통 정보는 projectile.txt에서 관리하고, 타겟 직선 이동 이후의 세그먼트 이동 옵션만 보관합니다.
    /// </summary>
    public sealed class StruckTableProjectileLinearThenSegments
    {
        public int Uid;
        public ProjectileConstants.SegmentDirectionMode SegmentDirectionMode;
        public ProjectileConstants.SegmentRelativeAxesMode SegmentRelativeAxesMode;
        public ProjectileMoveSegment[] MoveSegments = Array.Empty<ProjectileMoveSegment>();
    }

    /// <summary>
    /// Projectile 계열 테이블 파서의 공통 유틸리티입니다.
    /// - 공통/상세 테이블의 컬럼 누락을 기본값으로 보정합니다.
    /// - 레거시 컬럼명(EffectUid/EffectScale/HitEffectUid)도 함께 지원합니다.
    /// </summary>
    /// <typeparam name="TRow">파싱할 Row 타입입니다.</typeparam>
    public abstract class ProjectileTableBase<TRow> : DefaultTable<TRow> where TRow : class
    {
        /// <summary>
        /// 여러 후보 컬럼명 중 첫 번째로 발견된 문자열 값을 반환합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <param name="fallback">값이 없을 때 사용할 기본값입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>컬럼 값 또는 기본값입니다.</returns>
        protected static string GetString(Dictionary<string, string> data, string fallback, params string[] keys)
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
        protected static int GetInt(Dictionary<string, string> data, int fallback, params string[] keys)
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
        protected static float GetFloat(Dictionary<string, string> data, float fallback, params string[] keys)
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
        protected static bool GetBool(Dictionary<string, string> data, bool fallback, params string[] keys)
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
        protected static TEnum GetEnum<TEnum>(Dictionary<string, string> data, TEnum fallback, params string[] keys)
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
        protected static Vector2 GetVector2(Dictionary<string, string> data, Vector2 fallback, params string[] keys)
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
        protected static Vector2[] GetVector2Array(Dictionary<string, string> data, string key)
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

        /// <summary>
        /// 이동 세그먼트 컬럼을 <see cref="ProjectileMoveSegment"/> 배열로 변환합니다.
        /// - 세그먼트 구분자는 "|" 또는 ";"를 사용합니다.
        /// - 각 세그먼트는 "dirX,dirY,speed,distance" 형식으로 작성합니다.
        /// </summary>
        /// <param name="data">헤더명과 값으로 구성된 Row 데이터입니다.</param>
        /// <param name="keys">확인할 컬럼명 목록입니다.</param>
        /// <returns>파싱된 이동 세그먼트 배열입니다.</returns>
        protected static ProjectileMoveSegment[] GetMoveSegments(Dictionary<string, string> data, params string[] keys)
        {
            string value = GetString(data, null, keys);
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<ProjectileMoveSegment>();

            string[] tokens = value.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return Array.Empty<ProjectileMoveSegment>();

            var segments = new ProjectileMoveSegment[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
                segments[i] = ConvertMoveSegment(tokens[i].Trim());

            return segments;
        }

        /// <summary>
        /// "dirX,dirY,speed,distance" 문자열을 이동 세그먼트 데이터로 변환합니다.
        /// </summary>
        /// <param name="value">파싱할 세그먼트 문자열입니다.</param>
        /// <returns>파싱된 이동 세그먼트입니다.</returns>
        private static ProjectileMoveSegment ConvertMoveSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            string[] parts = value.Split(',');
            float dirX = MathHelper.ParseFloat(parts.Length > 0 ? parts[0] : "0");
            float dirY = MathHelper.ParseFloat(parts.Length > 1 ? parts[1] : "0");
            float speed = MathHelper.ParseFloat(parts.Length > 2 ? parts[2] : "0");
            float distance = MathHelper.ParseFloat(parts.Length > 3 ? parts[3] : "0");

            return new ProjectileMoveSegment(new Vector2(dirX, dirY), speed, distance);
        }
    }

    /// <summary>
    /// projectile.txt 공통 테이블 파서입니다.
    /// - 신규 구조에서는 공통 컬럼만 관리합니다.
    /// - 기존 데이터 호환을 위해 상세 컬럼이 남아 있으면 기본 상세값으로 함께 읽습니다.
    /// </summary>
    public class TableProjectile : ProjectileTableBase<StruckTableProjectile>
    {
        public override string Key => ConfigAddressableTable.Projectile;

        /// <summary>
        /// projectile.txt의 공통 Row를 파싱합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <returns>공통 Projectile Row입니다.</returns>
        protected override StruckTableProjectile BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableProjectile
            {
                Uid = GetInt(data, 0, "Uid"),
                Type = GetEnum(data, ProjectileConstants.Type.Default, "Type"),
                Name = GetString(data, string.Empty, "Name"),
                VfxUid = GetInt(data, 0, "VfxUid", "EffectUid"),
                VfxScale = GetFloat(data, 1f, "VfxScale", "EffectScale"),
                MoveSpeed = GetInt(data, 0, "MoveSpeed"),
                StartPosition = GetVector2(data, Vector2.zero, "StartPosition"),
                ColliderSize = GetVector2(data, Vector2.zero, "ColliderSize"),
                ColliderOffset = GetVector2(data, Vector2.zero, "ColliderOffset"),
                HitVfxUid = GetInt(data, 0, "HitVfxUid", "HitEffectUid"),
                TargetType = GetEnum(data, ProjectileConstants.TargetType.None, "TargetType"),
                TargetPositionRangeX = GetInt(data, 0, "TargetPositionRangeX"),
                Count = GetInt(data, 1, "Count"),
                SecDelayByOne = GetFloat(data, 0f, "SecDelayByOne"),
                DamageApplyMode = GetEnum(data, ProjectileConstants.DamageApplyMode.OnHit, "DamageApplyMode"),
                RotateByMoveDirection = GetBool(data, true, "RotateByMoveDirection"),
            };
        }

        /// <summary>
        /// 병합 또는 에디터 갱신을 위해 Row를 현재 캐시에 저장합니다.
        /// </summary>
        /// <param name="row">저장할 공통 Projectile Row입니다.</param>
        public void Upsert(StruckTableProjectile row)
        {
            if (row == null)
                return;

            SetDataByUid(row.Uid, row);
        }

        /// <summary>
        /// 다른 Projectile 공통 Row를 현재 캐시에 병합합니다.
        /// </summary>
        /// <param name="source">병합할 공통 Row 사전입니다.</param>
        public void MergeFrom(IReadOnlyDictionary<int, StruckTableProjectile> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<int, StruckTableProjectile> pair in source)
                SetDataByUid(pair.Key, pair.Value);
        }

        /// <summary>
        /// linear 상세 Row를 UID 기준으로 병합합니다.
        /// </summary>
        /// <param name="source">병합할 linear 상세 Row 사전입니다.</param>
        public void MergeLinearDetails(IReadOnlyDictionary<int, StruckTableProjectileLinear> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<int, StruckTableProjectileLinear> pair in source)
            {
                if (!TryGetDataByUid(pair.Key, out StruckTableProjectile row) || row == null)
                    continue;

                ApplyLinear(row, pair.Value);
            }
        }

        /// <summary>
        /// arc 상세 Row를 UID 기준으로 병합합니다.
        /// </summary>
        /// <param name="source">병합할 arc 상세 Row 사전입니다.</param>
        public void MergeArcDetails(IReadOnlyDictionary<int, StruckTableProjectileArc> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<int, StruckTableProjectileArc> pair in source)
            {
                if (!TryGetDataByUid(pair.Key, out StruckTableProjectile row) || row == null)
                    continue;

                ApplyArc(row, pair.Value);
            }
        }

        /// <summary>
        /// path 상세 Row를 UID 기준으로 병합합니다.
        /// </summary>
        /// <param name="source">병합할 path 상세 Row 사전입니다.</param>
        public void MergePathDetails(IReadOnlyDictionary<int, StruckTableProjectilePath> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<int, StruckTableProjectilePath> pair in source)
            {
                if (!TryGetDataByUid(pair.Key, out StruckTableProjectile row) || row == null)
                    continue;

                ApplyPath(row, pair.Value);
            }
        }

        /// <summary>
        /// linear_then_segments 상세 Row를 UID 기준으로 병합합니다.
        /// </summary>
        /// <param name="source">병합할 linear_then_segments 상세 Row 사전입니다.</param>
        public void MergeLinearThenSegmentsDetails(IReadOnlyDictionary<int, StruckTableProjectileLinearThenSegments> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<int, StruckTableProjectileLinearThenSegments> pair in source)
            {
                if (!TryGetDataByUid(pair.Key, out StruckTableProjectile row) || row == null)
                    continue;

                ApplyLinearThenSegments(row, pair.Value);
            }
        }

        /// <summary>
        /// 공통 Row를 복제한 뒤 타입별 상세 Row를 덧입혀 최종 런타임 데이터를 만듭니다.
        /// </summary>
        /// <param name="source">복제할 공통 Projectile Row입니다.</param>
        /// <param name="linear">linear 상세 Row입니다.</param>
        /// <param name="arc">arc 상세 Row입니다.</param>
        /// <param name="path">path 상세 Row입니다.</param>
        /// <returns>상세값이 병합된 Projectile 런타임 데이터입니다.</returns>
        public static StruckTableProjectile CreateMerged(
            StruckTableProjectile source,
            StruckTableProjectileLinear linear,
            StruckTableProjectileArc arc,
            StruckTableProjectilePath path,
            StruckTableProjectileLinearThenSegments linearThenSegments)
        {
            if (source == null)
                return null;

            StruckTableProjectile row = Clone(source);
            ApplyLinear(row, linear);
            ApplyArc(row, arc);
            ApplyPath(row, path);
            ApplyLinearThenSegments(row, linearThenSegments);
            return row;
        }

        /// <summary>
        /// 공통 Row를 복제합니다.
        /// </summary>
        /// <param name="source">복제할 Row입니다.</param>
        /// <returns>복제된 Row입니다.</returns>
        private static StruckTableProjectile Clone(StruckTableProjectile source)
        {
            return new StruckTableProjectile
            {
                Uid = source.Uid,
                Type = source.Type,
                Name = source.Name,
                VfxUid = source.VfxUid,
                VfxScale = source.VfxScale,
                MoveSpeed = source.MoveSpeed,
                StartPosition = source.StartPosition,
                ColliderSize = source.ColliderSize,
                ColliderOffset = source.ColliderOffset,
                HitVfxUid = source.HitVfxUid,
                TargetType = source.TargetType,
                TargetPositionRangeX = source.TargetPositionRangeX,
                Count = source.Count,
                SecDelayByOne = source.SecDelayByOne,
                DamageApplyMode = source.DamageApplyMode,
                RotateByMoveDirection = source.RotateByMoveDirection,
                BoundaryMode = source.BoundaryMode,
                BoundaryPadding = source.BoundaryPadding,
                BounceMaxCount = source.BounceMaxCount,
                BounceSpeedMultiplier = source.BounceSpeedMultiplier,
                ArcHeightMin = source.ArcHeightMin,
                ArcHeightMax = source.ArcHeightMax,
                TickDamageInterval = source.TickDamageInterval,
                TickOnSpawn = source.TickOnSpawn,
                PathCoordinateMode = source.PathCoordinateMode,
                PathPoints = source.PathPoints != null ? (Vector2[])source.PathPoints.Clone() : Array.Empty<Vector2>(),
                PathDuration = source.PathDuration,
                SegmentDirectionMode = source.SegmentDirectionMode,
                SegmentRelativeAxesMode = source.SegmentRelativeAxesMode,
                MoveSegments = source.MoveSegments != null ? (ProjectileMoveSegment[])source.MoveSegments.Clone() : Array.Empty<ProjectileMoveSegment>(),
            };
        }

        /// <summary>
        /// linear 상세값을 최종 Row에 덧입힙니다.
        /// </summary>
        /// <param name="target">상세값을 받을 Row입니다.</param>
        /// <param name="source">linear 상세 Row입니다.</param>
        private static void ApplyLinear(StruckTableProjectile target, StruckTableProjectileLinear source)
        {
            if (target == null || source == null)
                return;

            if (!ShouldApplyDetail(target.Type, ProjectileConstants.Type.Linear))
                return;

            target.BoundaryMode = source.BoundaryMode;
            target.BoundaryPadding = source.BoundaryPadding;
            target.BounceMaxCount = source.BounceMaxCount;
            target.BounceSpeedMultiplier = source.BounceSpeedMultiplier;
        }

        /// <summary>
        /// arc 상세값을 최종 Row에 덧입힙니다.
        /// </summary>
        /// <param name="target">상세값을 받을 Row입니다.</param>
        /// <param name="source">arc 상세 Row입니다.</param>
        private static void ApplyArc(StruckTableProjectile target, StruckTableProjectileArc source)
        {
            if (target == null || source == null)
                return;

            if (!ShouldApplyDetail(target.Type, ProjectileConstants.Type.Arc))
                return;

            target.ArcHeightMin = source.ArcHeightMin;
            target.ArcHeightMax = source.ArcHeightMax;
        }

        /// <summary>
        /// path 상세값을 최종 Row에 덧입힙니다.
        /// </summary>
        /// <param name="target">상세값을 받을 Row입니다.</param>
        /// <param name="source">path 상세 Row입니다.</param>
        private static void ApplyPath(StruckTableProjectile target, StruckTableProjectilePath source)
        {
            if (target == null || source == null)
                return;

            if (!ShouldApplyDetail(target.Type, ProjectileConstants.Type.Path))
                return;

            target.TickDamageInterval = source.TickDamageInterval;
            target.TickOnSpawn = source.TickOnSpawn;
            target.PathCoordinateMode = source.PathCoordinateMode;
            target.PathPoints = source.PathPoints != null ? (Vector2[])source.PathPoints.Clone() : Array.Empty<Vector2>();
            target.PathDuration = source.PathDuration;
        }

        /// <summary>
        /// linear_then_segments 상세값을 최종 Row에 병합합니다.
        /// </summary>
        /// <param name="target">상세값을 받을 Row입니다.</param>
        /// <param name="source">linear_then_segments 상세 Row입니다.</param>
        private static void ApplyLinearThenSegments(StruckTableProjectile target, StruckTableProjectileLinearThenSegments source)
        {
            if (target == null || source == null)
                return;

            if (!ShouldApplyDetail(target.Type, ProjectileConstants.Type.LinearThenSegments))
                return;

            target.SegmentDirectionMode = source.SegmentDirectionMode;
            target.SegmentRelativeAxesMode = source.SegmentRelativeAxesMode;
            target.MoveSegments = source.MoveSegments != null
                ? (ProjectileMoveSegment[])source.MoveSegments.Clone()
                : Array.Empty<ProjectileMoveSegment>();
        }

        /// <summary>
        /// 공용 Row의 Type과 상세 테이블 타입이 병합 가능한지 확인합니다.
        /// - Type이 명시되어 있으면 같은 타입의 상세 Row만 적용합니다.
        /// - Type이 Default이면 레거시 데이터 호환을 위해 발견된 상세 Row를 허용합니다.
        /// </summary>
        /// <param name="rowType">공용 Projectile Row의 타입입니다.</param>
        /// <param name="detailType">상세 테이블이 표현하는 타입입니다.</param>
        /// <returns>상세값을 적용할 수 있으면 true를 반환합니다.</returns>
        private static bool ShouldApplyDetail(ProjectileConstants.Type rowType, ProjectileConstants.Type detailType)
        {
            return rowType == ProjectileConstants.Type.Default || rowType == detailType;
        }
    }

    /// <summary>
    /// projectile_linear.txt 상세 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectileLinear : ProjectileTableBase<StruckTableProjectileLinear>
    {
        public override string Key => ConfigAddressableTable.ProjectileLinear;

        /// <summary>
        /// projectile_linear.txt의 상세 Row를 파싱합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <returns>linear 상세 Row입니다.</returns>
        protected override StruckTableProjectileLinear BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableProjectileLinear
            {
                Uid = GetInt(data, 0, "Uid"),
                BoundaryMode = GetEnum(data, ProjectileConstants.BoundaryMode.Destroy, "BoundaryMode"),
                BoundaryPadding = GetFloat(data, 0f, "BoundaryPadding"),
                BounceMaxCount = GetInt(data, 0, "BounceMaxCount"),
                BounceSpeedMultiplier = GetFloat(data, 1f, "BounceSpeedMultiplier"),
            };
        }

        /// <summary>
        /// 에디터 테스트 갱신을 위해 상세 Row를 현재 캐시에 저장합니다.
        /// </summary>
        /// <param name="row">저장할 linear 상세 Row입니다.</param>
        public void Upsert(StruckTableProjectileLinear row)
        {
            if (row == null)
                return;

            SetDataByUid(row.Uid, row);
        }
    }

    /// <summary>
    /// projectile_arc.txt 상세 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectileArc : ProjectileTableBase<StruckTableProjectileArc>
    {
        public override string Key => ConfigAddressableTable.ProjectileArc;

        /// <summary>
        /// projectile_arc.txt의 상세 Row를 파싱합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <returns>arc 상세 Row입니다.</returns>
        protected override StruckTableProjectileArc BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableProjectileArc
            {
                Uid = GetInt(data, 0, "Uid"),
                ArcHeightMin = GetInt(data, 0, "ArcHeightMin"),
                ArcHeightMax = GetInt(data, 0, "ArcHeightMax"),
            };
        }

        /// <summary>
        /// 에디터 테스트 갱신을 위해 상세 Row를 현재 캐시에 저장합니다.
        /// </summary>
        /// <param name="row">저장할 arc 상세 Row입니다.</param>
        public void Upsert(StruckTableProjectileArc row)
        {
            if (row == null)
                return;

            SetDataByUid(row.Uid, row);
        }
    }

    /// <summary>
    /// projectile_path.txt 상세 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectilePath : ProjectileTableBase<StruckTableProjectilePath>
    {
        public override string Key => ConfigAddressableTable.ProjectilePath;

        /// <summary>
        /// projectile_path.txt의 상세 Row를 파싱합니다.
        /// </summary>
        /// <param name="data">헤더명 → 값 사전입니다.</param>
        /// <returns>path 상세 Row입니다.</returns>
        protected override StruckTableProjectilePath BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableProjectilePath
            {
                Uid = GetInt(data, 0, "Uid"),
                TickDamageInterval = GetFloat(data, 0f, "TickDamageInterval"),
                TickOnSpawn = GetBool(data, false, "TickOnSpawn"),
                PathCoordinateMode = GetEnum(data, ProjectileConstants.PathCoordinateMode.StartRelative, "PathCoordinateMode"),
                PathPoints = GetVector2Array(data, "PathPoints"),
                PathDuration = GetFloat(data, 0f, "PathDuration"),
            };
        }

        /// <summary>
        /// 에디터 테스트 갱신을 위해 상세 Row를 현재 캐시에 저장합니다.
        /// </summary>
        /// <param name="row">저장할 path 상세 Row입니다.</param>
        public void Upsert(StruckTableProjectilePath row)
        {
            if (row == null)
                return;

            SetDataByUid(row.Uid, row);
        }
    }

    /// <summary>
    /// projectile_linear_then_segments.txt 상세 테이블 파서입니다.
    /// </summary>
    public sealed class TableProjectileLinearThenSegments : ProjectileTableBase<StruckTableProjectileLinearThenSegments>
    {
        public override string Key => ConfigAddressableTable.ProjectileLinearThenSegments;

        /// <summary>
        /// projectile_linear_then_segments.txt의 상세 Row를 파싱합니다.
        /// </summary>
        /// <param name="data">헤더명과 값으로 구성된 Row 데이터입니다.</param>
        /// <returns>linear_then_segments 상세 Row입니다.</returns>
        protected override StruckTableProjectileLinearThenSegments BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableProjectileLinearThenSegments
            {
                Uid = GetInt(data, 0, "Uid"),
                SegmentDirectionMode = GetEnum(data, ProjectileConstants.SegmentDirectionMode.World, "SegmentDirectionMode"),
                SegmentRelativeAxesMode = GetEnum(data, ProjectileConstants.SegmentRelativeAxesMode.Full2D, "SegmentRelativeAxesMode"),
                MoveSegments = GetMoveSegments(data, "MoveSegments", "Segments"),
            };
        }

        /// <summary>
        /// 에디터 테스트 값 갱신을 위해 상세 Row를 현재 캐시에 저장합니다.
        /// </summary>
        /// <param name="row">저장할 linear_then_segments 상세 Row입니다.</param>
        public void Upsert(StruckTableProjectileLinearThenSegments row)
        {
            if (row == null)
                return;

            SetDataByUid(row.Uid, row);
        }
    }
}
