using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public static class CharacterConstants
    {
        public enum Type
        {
            None,
            Player,
            Monster,
            Npc
        }

        public const int SortingOrderTop = 32767;
        public const int SortingOrderBottom = -32768;
        public enum FacingDirection8
        {
            None = 0,
            Right = 1,
            UpRight = 2,
            Up = 3,
            UpLeft = 4,
            Left = 5,
            DownLeft = 6,
            Down = 7,
            DownRight = 8
        }
        /// <summary>
        /// 캐릭터 상태
        /// </summary>
        public enum CharacterStatus
        {
            None,
            /// <summary>
            /// 기본 상태
            /// </summary>
            Idle,
            /// <summary>
            /// 움직이는 중
            /// </summary>
            Run,
            /// <summary>
            /// 공격 중
            /// </summary>
            Attack,
            AttackComboWait,
            /// <summary>
            /// 죽음
            /// </summary>
            Dead,
            /// <summary>
            /// 움직이지 못함
            /// </summary>
            DontMove,
            /// <summary>
            /// 조작/입력 기반 제어를 할 수 없음(그로기/기절/컷씬 등)
            /// - 이동/공격/스킬/상호작용 등, 플레이어/AI 제어가 차단되는 상태
            /// </summary>
            DontControl,
            /// <summary>
            /// 움직이지 못함
            /// </summary>
            CastingSkill,
            UseSkill,
            MoveForce,
            Damage,
            Jump,
            Knockback,
            Dash,
            Climb,
            Push,
            SimulationTool
        }
        
        /// <summary>
        /// 전투 상태 (동작 상태(CharacterStatus)와 별도의 축)
        /// - UI(전투 시작/종료), BGM, 이펙트, 카메라 등 '전투 여부'에 반응하는 시스템을 위해 사용합니다.
        /// - 이동/공격/점프 같은 동작 상태와 결합하지 않기 위해 별도 enum으로 관리합니다.
        /// </summary>
        public enum BattleStatus
        {
            None = 0,
            InBattle = 1,
        }

        public enum CharacterSubStatus
        {
            None        = 0,
            PickUp      = 1 << 0,   // 들고 있는 상태
            NoGravity   = 1 << 1,
        }

        /// <summary>
        /// 캐릭터 등급
        /// </summary>
        public enum Grade
        {
            None,
            Common,
            Boss
        }
        /// <summary>
        /// 캐릭터 정렬
        /// </summary>
        public enum CharacterSortingOrder
        {
            Normal,
            AlwaysOnTop,
            AlwaysOnBottom,
            Fixed
        }
        public enum AttackType
        {
            None,
            PassiveDefense, // 후공
            AggroFirst // 선공
        }

        public static FacingDirection8 ToFacingDirection8(Vector2 dir)
        {
            var facingDirection8 = FacingDirection8.None;
            if (dir == Vector2.zero)
                return facingDirection8;

            // 벡터를 정규화 (길이에 영향받지 않게)
            dir.Normalize();

            // 기준(오른쪽, 즉 0도)을 기준으로 각도 계산 (-180 ~ 180)
            float angle = Vector2.SignedAngle(Vector2.right, dir);

            // 각도를 0~360도로 변환
            if (angle < 0) angle += 360f;

            // 8방향 (45도 단위)
            // 22.5도씩 구간 나눔
            if (angle >= 337.5f || angle < 22.5f)
                facingDirection8 = FacingDirection8.Right;
            else if (angle >= 22.5f && angle < 67.5f)
                facingDirection8 = FacingDirection8.UpRight;
            else if (angle >= 67.5f && angle < 112.5f)
                facingDirection8 = FacingDirection8.Up;
            else if (angle >= 112.5f && angle < 157.5f)
                facingDirection8 = FacingDirection8.UpLeft;
            else if (angle >= 157.5f && angle < 202.5f)
                facingDirection8 = FacingDirection8.Left;
            else if (angle >= 202.5f && angle < 247.5f)
                facingDirection8 = FacingDirection8.DownLeft;
            else if (angle >= 247.5f && angle < 292.5f)
                facingDirection8 = FacingDirection8.Down;
            else if (angle >= 292.5f && angle < 337.5f)
                facingDirection8 = FacingDirection8.DownRight;
            else
                facingDirection8 = FacingDirection8.None;

            // GcLogger.Log($"direction: {facingDirection8}");
            return facingDirection8;
        }

        /// <summary>
        /// 사망 이유
        /// </summary>
        public enum DieReasonType
        {
            None,
            Battle, // 전투 
            EndTilemapY // 맵 Y좌표를 벗어 났을 때
        }

        public static Vector2 FacingToVector2(FacingDirection8 facing)
        {
            return facing switch
            {
                FacingDirection8.Right => Vector2.right,
                FacingDirection8.UpRight => new Vector2(1, 1).normalized,
                FacingDirection8.Up => Vector2.up,
                FacingDirection8.UpLeft => new Vector2(-1, 1).normalized,
                FacingDirection8.Left => Vector2.left,
                FacingDirection8.DownLeft => new Vector2(-1, -1).normalized,
                FacingDirection8.Down => Vector2.down,
                FacingDirection8.DownRight => new Vector2(1, -1).normalized,
                _ => Vector2.right
            };
        }
        
        /// <summary>
        /// 피격 리액션의 강도(종류)
        /// - Flinch: 짧은 움찔(경직)
        /// - Stagger: 비교적 큰 경직(행동 취소)
        /// - Knockdown: 다운
        /// </summary>
        public enum HitReactionType : byte
        {
            None = 0,
            Flinch = 1,
            Stagger = 2,
            Knockdown = 3,
        }

        /// <summary>
        /// 스택이 0이 되어 리액션이 발생했을 때 스택을 어떻게 처리할지
        /// </summary>
        public enum StaggerBreakResetMode : byte
        {
            /// <summary>브레이크 후 0으로 유지(회복으로 다시 쌓임)</summary>
            KeepZero = 0,

            /// <summary>브레이크 후 Max로 리셋(보스/패턴 유지에 유리)</summary>
            ResetToMax = 1,
        }
        /// <summary>
        /// Player 클레스에서 subscribe 를 위해 사용중
        /// </summary>
        public enum IndexPlayerInfo
        {
            None,
            Atk,
            Def,
            Hp,
            Mp,
            Stamina,
            MoveSpeed,
            AttackSpeed,
            CriticalDamage,
            CriticalProbability,
            RegistFire,
            RegistCold,
            RegistLightning,
            RegistPoison,
        }
        /// <summary>
        /// 스탯 포인트 투자 대상 집합
        /// - 1차 적용 범위: 공격력/방어력/체력/마력/스테미나
        /// - UI/로직에서 "투자 가능한 항목"을 명시적으로 통제하기 위해 HashSet으로 관리합니다.
        /// </summary>
        public static readonly HashSet<IndexPlayerInfo> PlayerStatPointTargets = new HashSet<IndexPlayerInfo>
        {
            IndexPlayerInfo.Atk,
            IndexPlayerInfo.Def,
            IndexPlayerInfo.Hp,
            IndexPlayerInfo.Mp,
            IndexPlayerInfo.Stamina,
        };

        /// <summary>
        /// 해당 PlayerInfo 라인이 스탯 포인트 투자 대상인지 여부
        /// </summary>
        public static bool IsStatPointTarget(IndexPlayerInfo idx) => PlayerStatPointTargets.Contains(idx);
    }
}