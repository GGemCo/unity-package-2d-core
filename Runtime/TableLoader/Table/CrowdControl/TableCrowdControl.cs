using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CrowdControl 공통 테이블의 한 행(row) 데이터입니다.
    /// </summary>
    public sealed class StruckTableCrowdControl
    {
        public int Uid;
        public string Name;

        public CrowdControlConstants.Type Type;
        public CrowdControlConstants.DirectionType DirectionType;

        public float FixedDirectionX;
        public float FixedDirectionY;

        /// <summary>
        /// CrowdControlDuration 동안 이동할 총 거리(유닛)입니다.
        /// 방향은 <see cref="DirectionType"/> 규칙으로 결정됩니다.
        /// </summary>
        public float Distance;

        /// <summary>
        /// <see cref="Duration"/> 동안의 이동 보간(Easing) 타입입니다.
        /// </summary>
        public Easing.EaseType EaseType;

        public float Duration;

        /// <summary>
        /// CrowdControl 대상 캐릭터의 위치를 카메라 화면 안쪽으로 보정하는 정책입니다.
        /// </summary>
        public CrowdControlConstants.EndViewportPolicy EndViewportPolicy;

        /// <summary>
        /// 화면 경계 보정을 CrowdControl 종료 시점에만 적용할지, 이동 중에도 적용할지 결정합니다.
        /// </summary>
        public CrowdControlConstants.ViewportConstraintPhase ViewportConstraintPhase;

        /// <summary>
        /// 화면 경계 보정을 적용할 축입니다.
        /// </summary>
        public CrowdControlConstants.EndViewportClampAxis EndViewportClampAxis;

        /// <summary>
        /// 캐릭터 Collider와 화면 경계 사이에 확보할 월드 단위 여백입니다.
        /// </summary>
        public float EndViewportPadding;

        public bool IsUseKnockbackStatus;
        public bool IsUseDontControlStatus;

        public string StaggerAnimationName;

    }

    /// <summary>
    /// CrowdControl 정의 테이블 파서입니다. (crowd_control.txt)
    /// </summary>
    public sealed class TableCrowdControl : DefaultTable<StruckTableCrowdControl>
    {
        public override string Key => ConfigAddressableTable.CrowdControl;

        protected override StruckTableCrowdControl BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            // 필수
            var row = new StruckTableCrowdControl
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),

                Type = reader.Enum<CrowdControlConstants.Type>("Type"),
                DirectionType = reader.Enum<CrowdControlConstants.DirectionType>("DirectionType"),

                FixedDirectionX = reader.Float("FixedDirectionX"),
                FixedDirectionY = reader.Float("FixedDirectionY"),

                Distance = reader.Float("Distance"),
                EaseType = reader.Enum<Easing.EaseType>("EaseType"),

                Duration = reader.Float("Duration"),

                // 기존 테이블에 신규 컬럼이 아직 추가되지 않은 경우에도
                // 플레이어가 일반 맵 화면 밖으로 밀려나지 않도록 안전한 기본 정책을 사용합니다.
                EndViewportPolicy = reader.Enum(
                    "EndViewportPolicy",
                    CrowdControlConstants.EndViewportPolicy.ClampPlayerExceptFreeCameraFollow),
                ViewportConstraintPhase = reader.Enum(
                    "ViewportConstraintPhase",
                    CrowdControlConstants.ViewportConstraintPhase.EndOnly),
                EndViewportClampAxis = reader.Enum(
                    "EndViewportClampAxis",
                    CrowdControlConstants.EndViewportClampAxis.Horizontal),
                EndViewportPadding = Mathf.Max(0f, reader.Float("EndViewportPadding", 0.3f)),

                IsUseKnockbackStatus = reader.BoolYN("IsUseKnockbackStatus"),
                IsUseDontControlStatus = reader.BoolYN("IsUseDontControlStatus"),

                StaggerAnimationName = reader.String("StaggerAnimationName"),
            };

            // 유효성 최소 보정
            if (row.Uid <= 0) return null;

            // 누락 시 기본값 보정
            if (string.IsNullOrWhiteSpace(reader.String("EaseType")))
                row.EaseType = Easing.EaseType.Linear;

            return row;
        }
    }
}
