using System;
using System.Collections.Generic;
using GGemCo2DAffect;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 테이블 Structure 
    /// - 적용 로직은 com.ggemco.2d.affect(AffectComponent)가 담당한다.
    /// - Core는 테이블 로드 및 UI/툴링 표시에 필요한 정보만 유지한다.
    /// </summary>
    public sealed class StruckTableAffect
    {
        public int Uid;

        // Display
        public string Name;
        public string Memo;
        public string IconKey;

        // Runtime
        public DispelType DispelType;
        public string GroupId;
        public float BaseDuration;
        public float TickInterval;
        public StackPolicy StackPolicy;
        public int MaxStacks;
        public RefreshPolicy RefreshPolicy;
        public string Tags;
        public int VfxUid;
        public float VfxScale;
        public float VfxOffsetY;
        public float ApplyChance;

        public bool HasTick => TickInterval > 0f;
    }

    /// <summary>
    /// 어펙트 테이블 (A안/신규)
    /// </summary>
    public sealed class TableAffect : DefaultTable<StruckTableAffect>
    {
        public override string Key => ConfigAddressableTable.Affect;

        protected override void OnLoadedData(StruckTableAffect data)
        {
            if (data == null) return;

            // 기존 방식과의 호환: 로컬라이징 키가 비어있으면 uid 문자열을 사용한다.
            if (LocalizationManager.Instance != null)
            {
                data.Name = LocalizationManager.Instance.GetAffectNameByKey($"{data.Uid}");
            }
            else
            {
                data.Name = $"{data.Memo}";
            }
        }

        protected override StruckTableAffect BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableAffect
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data.GetValueOrDefault("Memo"),
                Memo = data.GetValueOrDefault("Memo"),
                IconKey = data.GetValueOrDefault("IconKey"),
                DispelType = EnumHelper.ConvertEnum<DispelType>(data.GetValueOrDefault("DispelType")),
                GroupId = data.GetValueOrDefault("GroupId"),
                BaseDuration = MathHelper.ParseFloat(data.GetValueOrDefault("BaseDuration")),
                TickInterval = MathHelper.ParseFloat(data.GetValueOrDefault("TickInterval")),
                StackPolicy = EnumHelper.ConvertEnum<StackPolicy>(data.GetValueOrDefault("StackPolicy")),
                MaxStacks = MathHelper.ParseInt(data.GetValueOrDefault("MaxStacks")),
                RefreshPolicy = EnumHelper.ConvertEnum<RefreshPolicy>(data.GetValueOrDefault("RefreshPolicy")),
                Tags = data.GetValueOrDefault("Tags"),
                VfxUid = MathHelper.ParseInt(data.GetValueOrDefault("VfxUid")),
                VfxScale = MathHelper.ParseFloat(data.GetValueOrDefault("VfxScale")),
                VfxOffsetY = MathHelper.ParseFloat(data.GetValueOrDefault("VfxOffsetY")),
                ApplyChance = MathHelper.ParseFloat(data.GetValueOrDefault("ApplyChance"))
            };
        }
    }
}
