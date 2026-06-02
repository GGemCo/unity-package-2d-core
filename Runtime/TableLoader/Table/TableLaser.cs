using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// laser.txt 한 줄(Row)을 런타임에서 사용하기 위한 구조입니다.
    /// </summary>
    public sealed class StruckTableLaser
    {
        public int Uid;
        public string Name;
        public int VfxUid;
        public float VfxScale = 1f;
        public LaserConstants.VfxPresentationPolicy VfxPresentationPolicy = LaserConstants.VfxPresentationPolicy.StretchToBeam;
        public Vector2 StartPosition;
        public int HitVfxUid;
        public int Count = 1;
        public float SecDelayByOne;
        public bool RotateByMoveDirection = true;
        public float MaxDistance = 10f;
        public float Duration = 0.25f;
        public float DamageStartDelay;
        public float DamageActiveDuration = -1f;
        public float DamageTickInterval;
        public bool DamageTickOnStart = true;
        public LaserConstants.BlockMode BlockMode = LaserConstants.BlockMode.StopAtGroundOrHostile;
        public LaserConstants.HitMode HitMode = LaserConstants.HitMode.FirstHitOnly;
        public LaserConstants.AimUpdateMode AimUpdateMode = LaserConstants.AimUpdateMode.Snapshot;
        public LaserConstants.RaycastDirectionMode RaycastDirectionMode = LaserConstants.RaycastDirectionMode.TowardTarget;
        public float RaycastAngleDeg;
        public LaserConstants.VfxAngleSyncMode VfxAngleSyncMode = LaserConstants.VfxAngleSyncMode.FollowRaycast;
    }

    /// <summary>
    /// laser.txt 테이블 파서입니다.
    /// - 이동체용 projectile 공통 테이블과 분리된 레이저 전용 정적 데이터를 관리합니다.
    /// </summary>
    public sealed class TableLaser : DefaultTable<StruckTableLaser>
    {
        public override string Key => ConfigAddressableTable.Laser;

        /// <summary>
        /// laser.txt의 한 줄을 런타임 레이저 데이터로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명과 값으로 구성된 행 데이터입니다.</param>
        /// <returns>파싱된 레이저 정적 데이터입니다.</returns>
        protected override StruckTableLaser BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableLaser
            {
                Uid = GetInt(reader, 0, "Uid"),
                Name = GetString(reader, string.Empty, "Name"),
                VfxUid = GetInt(reader, 0, "VfxUid", "EffectUid"),
                VfxScale = GetFloat(reader, 1f, "VfxScale", "EffectScale"),
                VfxPresentationPolicy = GetEnum(reader, LaserConstants.VfxPresentationPolicy.StretchToBeam, "VfxPresentationPolicy", "PresentationPolicy"),
                StartPosition = GetVector2(reader, Vector2.zero, "StartPosition"),
                HitVfxUid = GetInt(reader, 0, "HitVfxUid", "HitEffectUid"),
                Count = Mathf.Max(1, GetInt(reader, 1, "Count")),
                SecDelayByOne = Mathf.Max(0f, GetFloat(reader, 0f, "SecDelayByOne")),
                RotateByMoveDirection = GetBool(reader, true, "RotateByMoveDirection"),
                MaxDistance = Mathf.Max(0.01f, GetFloat(reader, 10f, "MaxDistance")),
                Duration = Mathf.Max(0f, GetFloat(reader, 0.25f, "Duration", "DurationSeconds")),
                DamageStartDelay = Mathf.Max(0f, GetFloat(reader, 0f, "DamageStartDelay", "DamageStartDelaySeconds")),
                DamageActiveDuration = NormalizeDamageActiveDuration(GetFloat(reader, -1f, "DamageActiveDuration", "DamageActiveDurationSeconds")),
                DamageTickInterval = Mathf.Max(0f, GetFloat(reader, 0f, "DamageTickInterval", "DamageTickIntervalSeconds")),
                DamageTickOnStart = GetBool(reader, true, "DamageTickOnStart"),
                BlockMode = GetEnum(reader, LaserConstants.BlockMode.StopAtGroundOrHostile, "BlockMode"),
                HitMode = GetEnum(reader, LaserConstants.HitMode.FirstHitOnly, "HitMode"),
                AimUpdateMode = GetEnum(reader, LaserConstants.AimUpdateMode.Snapshot, "AimUpdateMode"),
                RaycastDirectionMode = GetEnum(reader, LaserConstants.RaycastDirectionMode.TowardTarget, "RaycastDirectionMode"),
                RaycastAngleDeg = GetFloat(reader, 0f, "RaycastAngleDeg", "RayAngleDeg"),
                VfxAngleSyncMode = GetEnum(reader, LaserConstants.VfxAngleSyncMode.FollowRaycast, "VfxAngleSyncMode"),
            };
        }

        /// <summary>
        /// 데미지 활성 지속 시간을 테이블에서 사용하는 값으로 보정합니다.
        /// </summary>
        /// <param name="value">테이블에서 읽은 데미지 활성 지속 시간입니다.</param>
        /// <returns>0 이하이면 레이저 종료까지 유지하는 의미의 -1, 양수이면 해당 값을 반환합니다.</returns>
        private static float NormalizeDamageActiveDuration(float value)
        {
            return value <= 0f ? -1f : value;
        }

        /// <summary>
        /// 병합 또는 에디터 테스트 적용을 위해 Row를 현재 캐시에 저장합니다.
        /// </summary>
        /// <param name="row">저장할 laser Row입니다.</param>
        public void Upsert(StruckTableLaser row)
        {
            if (row == null)
                return;

            SetDataByUid(row.Uid, row);
        }


        /// <summary>
        /// 문자열 값을 정수로 변환합니다.
        /// </summary>
        private static int GetInt(TableRowReader reader, int fallback, params string[] keys)
        {
            string value = GetString(reader, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : MathHelper.ParseInt(value);
        }

        /// <summary>
        /// 문자열 값을 실수로 변환합니다.
        /// </summary>
        private static float GetFloat(TableRowReader reader, float fallback, params string[] keys)
        {
            string value = GetString(reader, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : MathHelper.ParseFloat(value);
        }

        /// <summary>
        /// 문자열 값을 bool로 변환합니다.
        /// </summary>
        private static bool GetBool(TableRowReader reader, bool fallback, params string[] keys)
        {
            string value = GetString(reader, null, keys);
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return string.Equals(value, "Y", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                   || value == "1";
        }

        /// <summary>
        /// 문자열 값을 enum으로 변환합니다.
        /// </summary>
        private static TEnum GetEnum<TEnum>(TableRowReader reader, TEnum fallback, params string[] keys)
            where TEnum : struct, Enum
        {
            string value = GetString(reader, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : EnumHelper.ConvertEnum<TEnum>(value);
        }

        /// <summary>
        /// 문자열 값을 Vector2로 변환합니다.
        /// </summary>
        private static Vector2 GetVector2(TableRowReader reader, Vector2 fallback, params string[] keys)
        {
            string value = GetString(reader, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : DefaultTable<StruckTableLaser>.ConvertVector2(value);
        }

        /// <summary>
        /// 여러 후보 키 중 첫 번째 문자열 값을 반환합니다.
        /// </summary>
        private static string GetString(TableRowReader reader, string fallback, params string[] keys)
        {
            if (keys == null)
                return fallback;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                string value = reader.String(key);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return fallback;
        }
    }
}
