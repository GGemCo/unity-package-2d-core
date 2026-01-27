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
        
        [System.Flags]
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
    }
}