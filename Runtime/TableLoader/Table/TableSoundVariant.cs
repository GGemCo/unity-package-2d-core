using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대표 sound UID가 Variant 방식으로 재생될 때 사용할 후보 리소스와 확률 보정값입니다.
    /// </summary>
    public class StruckTableSoundVariant : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int SoundUid;
        public int CandidateResourceUid;
        public int Weight;
        public float VolumeScale;
        public float PitchMinOverride;
        public float PitchMaxOverride;
        public bool Enabled;
    }

    public class TableSoundVariant : DefaultTable<StruckTableSoundVariant>
    {
        private readonly Dictionary<int, List<StruckTableSoundVariant>> _variantsBySoundUid = new Dictionary<int, List<StruckTableSoundVariant>>();

        public override string Key => ConfigAddressableTable.SoundVariant;

        protected override void PreLoad()
        {
            _variantsBySoundUid.Clear();
        }

        protected override StruckTableSoundVariant BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableSoundVariant
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name", string.Empty),
                SoundUid = reader.Int("SoundUid", 0),
                CandidateResourceUid = reader.Int("CandidateResourceUid", reader.Int("CandidateUid", 0)),
                Weight = reader.Int("Weight", 1),
                VolumeScale = reader.Float("VolumeScale", 1f),
                PitchMinOverride = reader.Float("PitchMinOverride", 0f),
                PitchMaxOverride = reader.Float("PitchMaxOverride", 0f),
                Enabled = reader.BoolLoose("Enabled", true),
            };
        }

        protected override void OnLoadedData(StruckTableSoundVariant row)
        {
            if (row == null || row.SoundUid <= 0)
                return;

            if (!_variantsBySoundUid.TryGetValue(row.SoundUid, out List<StruckTableSoundVariant> variants))
            {
                variants = new List<StruckTableSoundVariant>();
                _variantsBySoundUid[row.SoundUid] = variants;
            }

            variants.Add(row);
        }

        /// <summary>
        /// 대표 sound UID에 연결된 후보 목록을 반환합니다.
        /// </summary>
        /// <param name="soundUid">대표 sound UID입니다.</param>
        /// <returns>후보 목록입니다. 없으면 빈 목록입니다.</returns>
        public IReadOnlyList<StruckTableSoundVariant> GetVariants(int soundUid)
        {
            return _variantsBySoundUid.TryGetValue(soundUid, out List<StruckTableSoundVariant> variants)
                ? variants
                : System.Array.Empty<StruckTableSoundVariant>();
        }
    }
}
