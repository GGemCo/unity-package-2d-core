using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 시스템이 참조하는 대표 sound 테이블 행입니다.
    /// 실제 AudioClip은 sound_bgm/sound_ambient/sound_sfx 테이블을 통해 해석됩니다.
    /// </summary>
    public class StruckTableSound : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public SoundConstants.Type Type;
        public SoundConstants.SubType SubType;
        public SoundConstants.ResolveMode ResolveMode;
        public SoundConstants.SelectionMode SelectionMode;
        public float VolumeScale;
        public int NoRepeatRecentCount;
        public int FallbackResourceUid;
        public bool UseIntroScene;
    }

    public class TableSound : DefaultTable<StruckTableSound>
    {
        public override string Key => ConfigAddressableTable.Sound;
        
        protected override StruckTableSound BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            SoundConstants.Type type = reader.Enum<SoundConstants.Type>("Type", reader.Enum<SoundConstants.Type>("SoundType"));
            return new StruckTableSound
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name", string.Empty),
                Type = type,
                SubType = reader.Enum<SoundConstants.SubType>("SubType"),
                ResolveMode = reader.Enum<SoundConstants.ResolveMode>("ResolveMode"),
                SelectionMode = reader.Enum<SoundConstants.SelectionMode>("SelectionMode", SoundConstants.SelectionMode.WeightedRandom),
                VolumeScale = reader.Float("VolumeScale", 1f),
                NoRepeatRecentCount = reader.Int("NoRepeatRecentCount", 0),
                FallbackResourceUid = reader.Int("FallbackResourceUid", 0),
                UseIntroScene = reader.BoolYN("UseIntroScene"),
            };
        }

        /// <summary>
        /// Intro 씬에서 자동 재생할 대표 BGM sound UID를 반환합니다.
        /// </summary>
        /// <returns>UseIntroScene이 true인 BGM 대표 sound UID입니다. 없으면 0입니다.</returns>
        public int GetBgmIntro()
        {
            var datas = GetDatas();
            foreach (var data in datas)
            {
                var info = data.Value;
                if (info.UseIntroScene && info.Type == SoundConstants.Type.Bgm) return info.Uid;
            }

            return 0;
        }
    }
}
