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
        public float TickInterval;
        public bool TickOnSpawn = true;
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
            return new StruckTableLaser
            {
                Uid = GetInt(data, 0, "Uid"),
                Name = GetString(data, string.Empty, "Name"),
                VfxUid = GetInt(data, 0, "VfxUid", "EffectUid"),
                VfxScale = GetFloat(data, 1f, "VfxScale", "EffectScale"),
                VfxPresentationPolicy = GetEnum(data, LaserConstants.VfxPresentationPolicy.StretchToBeam, "VfxPresentationPolicy", "PresentationPolicy"),
                StartPosition = GetVector2(data, Vector2.zero, "StartPosition"),
                HitVfxUid = GetInt(data, 0, "HitVfxUid", "HitEffectUid"),
                Count = Mathf.Max(1, GetInt(data, 1, "Count")),
                SecDelayByOne = Mathf.Max(0f, GetFloat(data, 0f, "SecDelayByOne")),
                RotateByMoveDirection = GetBool(data, true, "RotateByMoveDirection"),
                MaxDistance = Mathf.Max(0.01f, GetFloat(data, 10f, "MaxDistance")),
                Duration = Mathf.Max(0f, GetFloat(data, 0.25f, "Duration", "DurationSeconds")),
                TickInterval = Mathf.Max(0f, GetFloat(data, 0f, "TickInterval", "TickIntervalSeconds")),
                TickOnSpawn = GetBool(data, true, "TickOnSpawn"),
                BlockMode = GetEnum(data, LaserConstants.BlockMode.StopAtGroundOrHostile, "BlockMode"),
                HitMode = GetEnum(data, LaserConstants.HitMode.FirstHitOnly, "HitMode"),
                AimUpdateMode = GetEnum(data, LaserConstants.AimUpdateMode.Snapshot, "AimUpdateMode"),
                RaycastDirectionMode = GetEnum(data, LaserConstants.RaycastDirectionMode.TowardTarget, "RaycastDirectionMode"),
                RaycastAngleDeg = GetFloat(data, 0f, "RaycastAngleDeg", "RayAngleDeg"),
                VfxAngleSyncMode = GetEnum(data, LaserConstants.VfxAngleSyncMode.FollowRaycast, "VfxAngleSyncMode"),
            };
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
        private static int GetInt(Dictionary<string, string> data, int fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : MathHelper.ParseInt(value);
        }

        /// <summary>
        /// 문자열 값을 실수로 변환합니다.
        /// </summary>
        private static float GetFloat(Dictionary<string, string> data, float fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : MathHelper.ParseFloat(value);
        }

        /// <summary>
        /// 문자열 값을 bool로 변환합니다.
        /// </summary>
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
        /// 문자열 값을 enum으로 변환합니다.
        /// </summary>
        private static TEnum GetEnum<TEnum>(Dictionary<string, string> data, TEnum fallback, params string[] keys)
            where TEnum : struct, Enum
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : EnumHelper.ConvertEnum<TEnum>(value);
        }

        /// <summary>
        /// 문자열 값을 Vector2로 변환합니다.
        /// </summary>
        private static Vector2 GetVector2(Dictionary<string, string> data, Vector2 fallback, params string[] keys)
        {
            string value = GetString(data, null, keys);
            return string.IsNullOrWhiteSpace(value) ? fallback : DefaultTable<StruckTableLaser>.ConvertVector2(value);
        }

        /// <summary>
        /// 여러 후보 키 중 첫 번째 문자열 값을 반환합니다.
        /// </summary>
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
    }
}
