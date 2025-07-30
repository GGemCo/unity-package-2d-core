using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 테이블 Structure
    /// </summary>
    public class StruckTableAffect
    {
        public int Uid;
        public string Name;
        public string IconFileName;
        public AffectConstants.Type Type;
        public string Group;
        public float TickTime;
        public string StatusID;
        public ConfigCommon.SuffixType StatusSuffix;
        public int Value;
        public float Duration;
        public int EffectUid;
        public float EffectScale;
        public ConfigSortingLayer.Keys EffectSortingLayer;
        public ConfigCommon.PositionYType EffectPositionYType;
        public int EffectPositionY;
    }
    /// <summary>
    /// 어펙트 테이블
    /// </summary>
    public class TableAffect : DefaultTable
    {
        private static readonly Dictionary<string, AffectConstants.Type> MapType;

        static TableAffect()
        {
            MapType = new Dictionary<string, AffectConstants.Type>
            {
                { "Buff", AffectConstants.Type.Buff },
                { "DeBuff", AffectConstants.Type.DeBuff },
            };
        }
        private static AffectConstants.Type ConvertType(string type) => MapType.GetValueOrDefault(type, AffectConstants.Type.None);
        
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            if (LocalizationManager.Instance != null)
            {
                data["Name"] = LocalizationManager.Instance.GetAffectNameByKey(uid.ToString());   
            }
        }
        public StruckTableAffect GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableAffect
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                IconFileName = data["IconFileName"],
                Type = ConvertType(data["Type"]),
                Group = data["Group"],
                TickTime = float.Parse(data["TickTime"]),
                StatusID = data["StatusID"],
                StatusSuffix = ConvertSuffixType(data["StatusSuffix"]),
                Value = int.Parse(data["Value"]),
                Duration = float.Parse(data["Duration"]),
                EffectUid = int.Parse(data["EffectUid"]),
                EffectScale = float.Parse(data["EffectScale"]),
                EffectSortingLayer = ConfigSortingLayer.ConvertKeys(data["EffectSortingLayer"]),
                EffectPositionYType = ConvertPositionYType(data["EffectPositionYType"]),
                EffectPositionY = int.Parse(data["EffectPositionY"]),
            };
        }

        private Vector2 ConvertEffectPosition(string value)
        {
            var positions = value.Split(',');
            foreach (var position in positions)
            {
                if (position.Contains("CharacterHeight"))
                {
                    
                }
            }
            return new Vector2(float.Parse(positions[0]), float.Parse(positions[1]));
        }
    }
}