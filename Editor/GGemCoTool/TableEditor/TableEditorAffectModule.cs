using System.Collections.Generic;
using GGemCo2DAffect;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorAffectModule : ITableEditorModule
    {
        private sealed class AffectModifierEditorRow
        {
            public int AffectUid;
            public int ModifierId;
            public AffectPhase Phase;
            public ModifierKind Kind;
            public string StatId;
            public float StatValue;
            public StatValueType StatValueType;
            public StatOperation StatOperation;
            public string DamageTypeId;
            public float DamageBaseValue;
            public string ScalingStatId;
            public float ScalingCoefficient;
            public bool CanCrit;
            public bool IsDot;
            public float HealBaseValue;
            public string HealScalingStatId;
            public float HealScalingCoefficient;
            public string StateId;
            public float StateChance;
            public float StateDurationOverride;
            public int CrowdControlUid;
            public int ApplyAffectUid;
            public float ApplyAffectChance;
            public float ApplyAffectDurationOverride;
            public bool ConsumeOnProc;
        }

        public string ModuleName => "Affect";
        public string PackageName => "Affect";

        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.Affect,
                ConfigAddressableTableAffect.TableAffect.Path,
                ConfigAddressableTableAffect.Affect,
                typeof(TableAffect),
                typeof(StruckTableAffect),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffect.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.AffectModifier,
                ConfigAddressableTableAffect.TableAffectModifier.Path,
                ConfigAddressableTableAffect.AffectModifier,
                typeof(TableAffectModifier),
                typeof(AffectModifierEditorRow),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffectModifier.Path),
                ResolveReference);
        }

        private static TableEditorTableDefinition ResolveReference(string headerName)
        {
            switch (headerName)
            {
                case "AffectUid":
                case "ApplyAffectUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTableAffect.Affect);
                case "CrowdControlUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.CrowdControl);
                case "EffectUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.Effect);
                default:
                    return TableEditorRegistry.FindReferenceTable(headerName);
            }
        }
    }
}
