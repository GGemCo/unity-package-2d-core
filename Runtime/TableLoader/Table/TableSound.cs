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
            SoundConstants.Type type = EnumHelper.ConvertEnum<SoundConstants.Type>(GetValue(data, "Type", GetValue(data, "SoundType", "None")));
            return new StruckTableSound
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = GetValue(data, "Name", string.Empty),
                Type = type,
                SubType = EnumHelper.ConvertEnum<SoundConstants.SubType>(GetValue(data, "SubType", "None")),
                ResolveMode = ResolveModeOrDefault(data),
                SelectionMode = EnumHelper.ConvertEnum<SoundConstants.SelectionMode>(GetValue(data, "SelectionMode", "WeightedRandom")),
                VolumeScale = MathHelper.ParseFloat(GetValue(data, "VolumeScale", "1")),
                NoRepeatRecentCount = MathHelper.ParseInt(GetValue(data, "NoRepeatRecentCount", "0")),
                FallbackResourceUid = MathHelper.ParseInt(GetValue(data, "FallbackResourceUid", "0")),
                UseIntroScene = ConvertBoolean(GetValue(data, "UseIntroScene", "N")),
            };
        }

        /// <summary>
        /// sound 테이블의 ResolveMode를 읽고 비어 있으면 Variant를 기본값으로 사용합니다.
        /// </summary>
        /// <param name="data">테이블 행 원본 값입니다.</param>
        /// <returns>적용할 사운드 해석 방식입니다.</returns>
        private static SoundConstants.ResolveMode ResolveModeOrDefault(Dictionary<string, string> data)
        {
            string raw = GetValue(data, "ResolveMode", string.Empty);
            if (!string.IsNullOrWhiteSpace(raw))
                return EnumHelper.ConvertEnum<SoundConstants.ResolveMode>(raw);

            return SoundConstants.ResolveMode.Variant;
        }

        /// <summary>
        /// 헤더가 없을 수 있는 마이그레이션 중간 테이블에서 값을 안전하게 읽습니다.
        /// </summary>
        /// <param name="data">헤더명과 값의 사전입니다.</param>
        /// <param name="key">조회할 헤더명입니다.</param>
        /// <param name="defaultValue">헤더가 없거나 값이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>조회된 값 또는 기본값입니다.</returns>
        private static string GetValue(Dictionary<string, string> data, string key, string defaultValue)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
                return defaultValue;

            return data.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : defaultValue;
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
