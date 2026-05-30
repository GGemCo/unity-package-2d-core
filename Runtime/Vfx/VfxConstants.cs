using UnityEngine;

namespace GGemCo2DCore
{
    public static class VfxConstants
    {
        public enum AssetKind
        {
            None,
            Effect,
            Particle,
        }

        public enum ResolveMode
        {
            [Tooltip("연결된 실제 VFX 리소스 테이블 행 1개를 직접 생성합니다.")]
            Direct,
            [Tooltip("vfx_variant 테이블의 후보 목록 중 하나를 선택해 생성합니다.")]
            Variant
        }

        public enum SelectionMode
        {
            [Tooltip("가중치를 동일하게 보고 후보 중 하나를 무작위로 선택합니다.")]
            RandomEqual,
            [Tooltip("vfx_variant.Weight 값을 기준으로 후보 중 하나를 선택합니다.")]
            WeightedRandom,
            [Tooltip("등록된 후보 순서대로 하나씩 선택합니다.")]
            Sequence,
            [Tooltip("최근 선택 후보를 가능한 한 피하면서 무작위로 선택합니다.")]
            ShuffleBag
        }

        public enum Category
        {
            None,
            Common,
            Skill,
            Player,
            Monster,
            UI,
            Etc
        }

        public enum EffectType
        {
            None,
            Default,
            Laser
        }

        public enum PlaybackType
        {
            Auto,
            SpriteSequence,
            SpineSequence,
            ParticleSystem,
            Laser,
        }

        public enum LifecycleType
        {
            AutoRelease,
            Duration,
            ManualRelease,
        }

        public enum AttachType
        {
            World,
            Owner,
            Target,
            UI,
        }

        public enum FollowMode
        {
            None,
            Position,
            PositionAndFlip,
        }

        /// <summary>
        /// VFX가 Follow 대상을 따라갈 때 기준 위치를 계산하는 방식입니다.
        /// </summary>
        public enum FollowAnchorMode
        {
            /// <summary>
            /// 매 프레임 Follow 대상의 현재 Transform 위치를 기준으로 위치를 다시 계산합니다.
            /// 기존 Follow 동작과 동일한 기본값입니다.
            /// </summary>
            FollowTargetOrigin = 0,

            /// <summary>
            /// 최초 스폰 위치와 Follow 대상 위치의 월드 오프셋을 저장한 뒤,
            /// 이후에는 저장된 오프셋을 유지하며 Follow 대상을 따라갑니다.
            /// </summary>
            SpawnPosition = 1,
        }
    }
}
