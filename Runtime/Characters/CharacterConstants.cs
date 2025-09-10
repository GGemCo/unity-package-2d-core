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
            Push
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
            if (dir == Vector2.zero) return FacingDirection8.None;

            if (dir.x > 0f) return FacingDirection8.Right;
            if (dir.x < 0f) return FacingDirection8.Left;
            if (Mathf.Approximately(dir.x, 0f) && dir.y > 0f) return FacingDirection8.None;
            if (Mathf.Approximately(dir.x, 0f) && dir.y < 0f) return FacingDirection8.None;
            return FacingDirection8.DownRight;
        }
    }
}