using System.Collections.Generic;
using UnityEngine;

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
    }
    public class TableSound : DefaultTable
    {
        private static readonly Dictionary<string, SoundConstants.Type> MapType;
        private static readonly Dictionary<string, SoundConstants.SubType> MapTypeSub;

        static TableSound()
        {
            MapType = new Dictionary<string, SoundConstants.Type>
            {
                { "Bgm", SoundConstants.Type.Bgm },
                { "Sfx", SoundConstants.Type.Sfx },
            };
            MapTypeSub = new Dictionary<string, SoundConstants.SubType>
            {
                { "Player", SoundConstants.SubType.Player },
                { "UI", SoundConstants.SubType.UI },
                { "Skill", SoundConstants.SubType.Skill }
            };
        }
        private static SoundConstants.Type ConvertType(string type) => MapType.GetValueOrDefault(type, SoundConstants.Type.None);
        private static SoundConstants.SubType ConvertTypeSub(string typeSub) => MapTypeSub.GetValueOrDefault(typeSub, SoundConstants.SubType.None);

        public StruckTableSound GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableSound
            {
                Uid = int.Parse(data["Uid"]),
                Type = ConvertType(data["Type"]),
                SubType = ConvertTypeSub(data["SubType"]),
                FileName = data["FileName"],
                MaxPlayCount = int.Parse(data["MaxPlayCount"]),
                Volume = float.Parse(data["Volume"]),
            };
        }
    }
}