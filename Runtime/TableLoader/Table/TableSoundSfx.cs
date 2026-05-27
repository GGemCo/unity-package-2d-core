using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableSoundSfx : StruckTableSoundResource
    {
        public StruckTableSoundSfx()
        {
            Type = SoundConstants.Type.Sfx;
        }
    }

    public class TableSoundSfx : DefaultTable<StruckTableSoundSfx>
    {
        public override string Key => ConfigAddressableTable.SoundSfx;

        protected override StruckTableSoundSfx BuildRow(Dictionary<string, string> data)
        {
            return TableSoundResourceParser.BuildResourceRow<StruckTableSoundSfx>(data, SoundConstants.Type.Sfx);
        }

        /// <summary>
        /// 대표 sound UID에 연결된 첫 번째 SFX 리소스 행을 찾습니다.
        /// </summary>
        /// <param name="soundUid">대표 sound UID입니다.</param>
        /// <returns>연결된 SFX 리소스 행입니다. 없으면 null입니다.</returns>
        public StruckTableSoundSfx GetFirstBySoundUid(int soundUid)
        {
            foreach (KeyValuePair<int, StruckTableSoundSfx> pair in GetDatas())
            {
                if (pair.Value != null && pair.Value.SoundUid == soundUid)
                    return pair.Value;
            }

            return null;
        }
    }
}
