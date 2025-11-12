using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableSound
    {
        public int Uid;
        public SoundConstants.Type Type;
        public SoundConstants.SubType SubType;
        public string FileName;
        public int MaxPlayCount;
        public float Volume;
        public bool UseIntroScene;
    }
    public class TableSound : DefaultTable<StruckTableSound>
    {
        public override string Key => ConfigAddressableTable.Sound;
        
        protected override StruckTableSound BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableSound
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Type = EnumHelper.ConvertEnum<SoundConstants.Type>(data["Type"]),
                SubType = EnumHelper.ConvertEnum<SoundConstants.SubType>(data["SubType"]),
                FileName = data["FileName"],
                MaxPlayCount = MathHelper.ParseInt(data["MaxPlayCount"]),
                Volume = MathHelper.ParseFloat(data["Volume"]),
                UseIntroScene = ConvertBoolean(data["UseIntroScene"]),
            };
        }

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