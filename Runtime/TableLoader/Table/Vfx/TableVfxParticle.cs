using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableVfxParticle
    {
        public int Uid;
        public string Name;
        public int VfxUid;
        public string PrefabPath;
        public VfxConstants.LifecycleType LifecycleType;
        public VfxConstants.AttachType AttachType;
        public VfxConstants.FollowMode FollowMode;
        public VfxConstants.FollowAnchorMode FollowAnchorMode;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool Loop;
        public bool UseUnscaledTime;
    }
    public class TableVfxParticle : DefaultTable<StruckTableVfxParticle>
    {
        public override string Key => ConfigAddressableTable.VfxParticle;

        protected override StruckTableVfxParticle BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableVfxParticle
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                VfxUid = reader.Int("VfxUid", 0),
                PrefabPath = reader.String("PrefabPath"),
                LifecycleType = ParseLifecycleType(reader.String("LifecycleType")),
                AttachType = ParseAttachType(reader.String("AttachType")),
                FollowMode = ParseFollowMode(reader.String("FollowMode")),
                FollowAnchorMode = ParseFollowAnchorMode(reader.String("FollowAnchorMode")),
                PoolPrewarmCount = reader.Int("PoolPrewarmCount"),
                PoolMaxSize = reader.Int("PoolMaxSize"),
                Loop = reader.BoolYN("Loop"),
                UseUnscaledTime = reader.BoolYN("UseUnscaledTime"),
            };
        }


        /// <summary>
        /// 대표 VFX UID에 직접 연결된 첫 번째 Particle 리소스를 반환합니다.
        /// </summary>
        /// <param name="vfxUid">대표 VFX UID입니다.</param>
        /// <returns>연결된 Particle 행입니다. 없으면 null입니다.</returns>
        public StruckTableVfxParticle GetFirstByVfxUid(int vfxUid)
        {
            foreach (KeyValuePair<int, StruckTableVfxParticle> pair in GetDatas())
            {
                if (pair.Value == null)
                    continue;

                if (pair.Value.VfxUid == vfxUid)
                    return pair.Value;
            }

            return null;
        }

        private static VfxConstants.LifecycleType ParseLifecycleType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.LifecycleType.AutoRelease
                : EnumHelper.ConvertEnum<VfxConstants.LifecycleType>(value);
        }

        private static VfxConstants.AttachType ParseAttachType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.AttachType.World
                : EnumHelper.ConvertEnum<VfxConstants.AttachType>(value);
        }

        private static VfxConstants.FollowMode ParseFollowMode(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.FollowMode.None
                : EnumHelper.ConvertEnum<VfxConstants.FollowMode>(value);
        }

        /// <summary>
        /// Follow 위치 기준 정책을 테이블 문자열에서 변환합니다.
        /// </summary>
        /// <param name="value">vfx_particle.FollowAnchorMode 값입니다.</param>
        /// <returns>런타임 Follow 위치 기준 정책입니다.</returns>
        private static VfxConstants.FollowAnchorMode ParseFollowAnchorMode(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.FollowAnchorMode.FollowTargetOrigin
                : EnumHelper.ConvertEnum<VfxConstants.FollowAnchorMode>(value);
        }
    }
}
