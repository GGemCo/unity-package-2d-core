using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Crowd Control 공통 테이블과 타입별 상세 테이블을 합쳐 만든 런타임용 데이터입니다.
    /// - crowd_control.txt에는 공통 정의만 둡니다.
    /// - 타입별 상세 값은 crowd_control_knock_back / knock_down / knock_up 테이블에서 채웁니다.
    /// </summary>
    public sealed class CrowdControlRuntimeData
    {
        public int Uid;
        public string Name;

        public CrowdControlConstants.Type Type;
        public CrowdControlConstants.DirectionType DirectionType;

        public float FixedDirectionX;
        public float FixedDirectionY;

        public float Distance;
        public Easing.EaseType EaseType;
        public float Duration;

        public float Height;
        public float KnockUpRiseTime;
        public float KnockUpAirTime;
        public float KnockUpFallTime;
        public string KnockUpRiseAnimationName;
        public string KnockUpAirAnimationName;
        public string KnockUpFallAnimationName;
        public string KnockUpLandEndAnimationName;
        public Easing.EaseType KnockUpRiseEaseType;
        public Easing.EaseType KnockUpFallEaseType;
        public CrowdControlConstants.EndYMode EndYMode;
        public float EndYOffset;
        public float EndYAbsolute;
        public float DownWaitTime;
        public float RecoverTime;

        public bool IsUseKnockbackStatus;
        public bool IsUseDontControlStatus;

        public string StaggerAnimationName;

        public bool IsStopOnWall;
        public bool IsGroundOnly;
        public bool IsAirOnly;

        public static CrowdControlRuntimeData FromShared(StruckTableCrowdControl row)
        {
            if (row == null) return null;

            return new CrowdControlRuntimeData
            {
                Uid = row.Uid,
                Name = row.Name,
                Type = row.Type,
                DirectionType = row.DirectionType,
                FixedDirectionX = row.FixedDirectionX,
                FixedDirectionY = row.FixedDirectionY,
                Distance = row.Distance,
                EaseType = row.EaseType,
                Duration = row.Duration,
                Height = 0f,
                KnockUpRiseTime = 0f,
                KnockUpAirTime = 0f,
                KnockUpFallTime = 0f,
                KnockUpRiseAnimationName = string.Empty,
                KnockUpAirAnimationName = string.Empty,
                KnockUpFallAnimationName = string.Empty,
                KnockUpLandEndAnimationName = string.Empty,
                KnockUpRiseEaseType = row.EaseType,
                KnockUpFallEaseType = row.EaseType,
                EndYMode = CrowdControlConstants.EndYMode.None,
                EndYOffset = 0f,
                EndYAbsolute = 0f,
                DownWaitTime = 0f,
                RecoverTime = 0f,
                IsUseKnockbackStatus = row.IsUseKnockbackStatus,
                IsUseDontControlStatus = row.IsUseDontControlStatus,
                StaggerAnimationName = row.StaggerAnimationName,
                IsStopOnWall = false,
                IsGroundOnly = false,
                IsAirOnly = false,
            };
        }
    }

    /// <summary>
    /// Crowd Control 공통/상세 테이블을 런타임 데이터로 합성하는 Resolver입니다.
    /// </summary>
    public static class CrowdControlRuntimeDataResolver
    {
        public static CrowdControlRuntimeData Resolve(TableLoaderManager tableLoader, int uid)
        {
            if (uid <= 0) return null;
            if (tableLoader == null || tableLoader.TableCrowdControl == null) return null;

            if (!tableLoader.TableCrowdControl.TryGetDataByUid(uid, out var row))
                return null;

            return Resolve(tableLoader, row);
        }

        public static CrowdControlRuntimeData Resolve(TableLoaderManager tableLoader, StruckTableCrowdControl row)
        {
            var runtime = CrowdControlRuntimeData.FromShared(row);
            if (runtime == null) return null;
            if (tableLoader == null) return runtime;

            switch (runtime.Type)
            {
                case CrowdControlConstants.Type.KnockBack:
                    if (tableLoader.TableCrowdControlKnockBack != null &&
                        tableLoader.TableCrowdControlKnockBack.TryGetDataByUid(runtime.Uid, out var knockBack))
                    {
                        ApplyBaseDetail(runtime, knockBack);
                        runtime.DownWaitTime = knockBack.DownWaitTime;
                    }
                    break;

                case CrowdControlConstants.Type.KnockDown:
                    if (tableLoader.TableCrowdControlKnockDown != null &&
                        tableLoader.TableCrowdControlKnockDown.TryGetDataByUid(runtime.Uid, out var knockDown))
                    {
                        ApplyBaseDetail(runtime, knockDown);
                        runtime.DownWaitTime = knockDown.DownWaitTime;
                    }
                    break;

                case CrowdControlConstants.Type.KnockUp:
                    if (tableLoader.TableCrowdControlKnockUp != null &&
                        tableLoader.TableCrowdControlKnockUp.TryGetDataByUid(runtime.Uid, out var knockUp))
                    {
                        ApplyBaseDetail(runtime, knockUp);
                        runtime.Height = knockUp.Height;
                        runtime.KnockUpRiseTime = Mathf.Max(0f, knockUp.RiseTime);
                        runtime.KnockUpAirTime = Mathf.Max(0f, knockUp.AirTime);
                        runtime.KnockUpFallTime = Mathf.Max(0f, knockUp.FallTime);
                        runtime.KnockUpRiseAnimationName = knockUp.RiseAnimationName ?? string.Empty;
                        runtime.KnockUpAirAnimationName = knockUp.AirAnimationName ?? string.Empty;
                        runtime.KnockUpFallAnimationName = knockUp.FallAnimationName ?? string.Empty;
                        runtime.KnockUpLandEndAnimationName = knockUp.LandEndAnimationName ?? string.Empty;
                        runtime.KnockUpRiseEaseType = knockUp.RiseEaseType;
                        runtime.KnockUpFallEaseType = knockUp.FallEaseType;

                        float knockUpDuration = runtime.KnockUpRiseTime + runtime.KnockUpAirTime + runtime.KnockUpFallTime;
                        if (knockUpDuration > 0f)
                            runtime.Duration = knockUpDuration;
                    }
                    break;

                case CrowdControlConstants.Type.KnockDownAir:
                    if (tableLoader.TableCrowdControlKnockDownAir != null &&
                        tableLoader.TableCrowdControlKnockDownAir.TryGetDataByUid(runtime.Uid, out var knockDownAir))
                    {
                        ApplyBaseDetail(runtime, knockDownAir);
                        runtime.Height = knockDownAir.Height;
                        runtime.KnockUpRiseTime = Mathf.Max(0f, knockDownAir.RiseTime);
                        runtime.KnockUpAirTime = Mathf.Max(0f, knockDownAir.AirTime);
                        runtime.KnockUpFallTime = Mathf.Max(0f, knockDownAir.FallTime);
                        runtime.KnockUpRiseAnimationName = knockDownAir.RiseAnimationName ?? string.Empty;
                        runtime.KnockUpAirAnimationName = knockDownAir.AirAnimationName ?? string.Empty;
                        runtime.KnockUpFallAnimationName = knockDownAir.FallAnimationName ?? string.Empty;
                        runtime.KnockUpLandEndAnimationName = knockDownAir.LandEndAnimationName ?? string.Empty;
                        runtime.KnockUpRiseEaseType = knockDownAir.RiseEaseType;
                        runtime.KnockUpFallEaseType = knockDownAir.FallEaseType;

                        float knockDownAirDuration = runtime.KnockUpRiseTime + runtime.KnockUpAirTime + runtime.KnockUpFallTime;
                        if (knockDownAirDuration > 0f)
                            runtime.Duration = knockDownAirDuration;
                    }
                    break;
            }

            return runtime;
        }

        private static void ApplyBaseDetail(CrowdControlRuntimeData runtime, StruckTableCrowdControlDetailBase detail)
        {
            if (runtime == null || detail == null) return;

            runtime.EndYMode = detail.EndYMode;
            runtime.EndYOffset = detail.EndYOffset;
            runtime.EndYAbsolute = detail.EndYAbsolute;
            runtime.RecoverTime = detail.RecoverTime;
            runtime.IsStopOnWall = detail.IsStopOnWall;
            runtime.IsGroundOnly = detail.IsGroundOnly;
            runtime.IsAirOnly = detail.IsAirOnly;
        }
    }
}
