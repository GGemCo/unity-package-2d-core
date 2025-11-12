using System.Collections.Generic;

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
    public class TableAffect : DefaultTable<StruckTableAffect>
    {
        public override string Key => ConfigAddressableTable.Affect;
        
        protected override void OnLoadedData(StruckTableAffect data)
        {
            if (LocalizationManager.Instance == null) return;
            data.Name = LocalizationManager.Instance.GetAffectNameByKey(data.Uid.ToString());
        }
        protected override StruckTableAffect BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableAffect
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                IconFileName = data["IconFileName"],
                Type = EnumHelper.ConvertEnum<AffectConstants.Type>(data["Type"]),
                Group = data["Group"],
                TickTime = MathHelper.ParseFloat(data["TickTime"]),
                StatusID = data["StatusID"],
                StatusSuffix = ConvertSuffixType(data["StatusSuffix"]),
                Value = MathHelper.ParseInt(data["Value"]),
                Duration = MathHelper.ParseFloat(data["Duration"]),
                EffectUid = MathHelper.ParseInt(data["EffectUid"]),
                EffectScale = MathHelper.ParseFloat(data["EffectScale"]),
                EffectSortingLayer = ConfigSortingLayer.ConvertKeys(data["EffectSortingLayer"]),
                EffectPositionYType = ConvertPositionYType(data["EffectPositionYType"]),
                EffectPositionY = MathHelper.ParseInt(data["EffectPositionY"]),
            };
        }
    }
}