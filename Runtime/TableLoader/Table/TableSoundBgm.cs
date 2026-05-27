using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableSoundBgm : StruckTableSoundResource
    {
        public StruckTableSoundBgm()
        {
            Type = SoundConstants.Type.Bgm;
        }
    }

    public class TableSoundBgm : DefaultTable<StruckTableSoundBgm>
    {
        public override string Key => ConfigAddressableTable.SoundBgm;

        protected override StruckTableSoundBgm BuildRow(Dictionary<string, string> data)
        {
            return TableSoundResourceParser.BuildResourceRow<StruckTableSoundBgm>(data, SoundConstants.Type.Bgm);
        }

        /// <summary>
        /// 대표 sound UID에 연결된 첫 번째 BGM 리소스 행을 찾습니다.
        /// </summary>
        /// <param name="soundUid">대표 sound UID입니다.</param>
        /// <returns>연결된 BGM 리소스 행입니다. 없으면 null입니다.</returns>
        public StruckTableSoundBgm GetFirstBySoundUid(int soundUid)
        {
            foreach (KeyValuePair<int, StruckTableSoundBgm> pair in GetDatas())
            {
                if (pair.Value != null && pair.Value.SoundUid == soundUid)
                    return pair.Value;
            }

            return null;
        }
    }
}
