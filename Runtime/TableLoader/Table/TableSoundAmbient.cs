using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableSoundAmbient : StruckTableSoundResource
    {
        public StruckTableSoundAmbient()
        {
            Type = SoundConstants.Type.Ambient;
        }
    }

    public class TableSoundAmbient : DefaultTable<StruckTableSoundAmbient>
    {
        public override string Key => ConfigAddressableTable.SoundAmbient;

        protected override StruckTableSoundAmbient BuildRow(Dictionary<string, string> data)
        {
            return TableSoundResourceParser.BuildResourceRow<StruckTableSoundAmbient>(data, SoundConstants.Type.Ambient);
        }

        /// <summary>
        /// 대표 sound UID에 연결된 첫 번째 Ambient 리소스 행을 찾습니다.
        /// </summary>
        /// <param name="soundUid">대표 sound UID입니다.</param>
        /// <returns>연결된 Ambient 리소스 행입니다. 없으면 null입니다.</returns>
        public StruckTableSoundAmbient GetFirstBySoundUid(int soundUid)
        {
            foreach (KeyValuePair<int, StruckTableSoundAmbient> pair in GetDatas())
            {
                if (pair.Value != null && pair.Value.SoundUid == soundUid)
                    return pair.Value;
            }

            return null;
        }
    }
}
