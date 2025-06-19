namespace GGemCo.Scripts
{
    public class CharacterConstants
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
        public enum CharacterFacing
        {
            Right,
            Left
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
            MoveForce
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

    }
}