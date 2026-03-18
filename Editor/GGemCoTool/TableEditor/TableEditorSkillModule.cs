using System.Collections.Generic;
using GGemCo2DSkill;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorSkillModule : ITableEditorModule
    {
        public string ModuleName => "Skill";
        public string PackageName => "Skill";

        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableSkill.Skill,
                ConfigAddressableTableSkill.TableSkill.Path,
                ConfigAddressableTableSkill.Skill,
                typeof(TableSkill),
                typeof(StruckTableSkill),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableSkill.TableSkill.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableSkill.SkillMonster,
                ConfigAddressableTableSkill.TableSkillMonster.Path,
                ConfigAddressableTableSkill.SkillMonster,
                typeof(TableSkillMonster),
                typeof(StruckTableSkillMonster),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableSkill.TableSkillMonster.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableSkill.SkillPassive,
                ConfigAddressableTableSkill.TableSkillPassive.Path,
                ConfigAddressableTableSkill.SkillPassive,
                typeof(TableSkillPassive),
                typeof(StruckTableSkillPassive),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableSkill.TableSkillPassive.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableSkill.SkillPassiveOption,
                ConfigAddressableTableSkill.TableSkillPassiveOption.Path,
                ConfigAddressableTableSkill.SkillPassiveOption,
                typeof(TableSkillPassiveOption),
                typeof(StruckTableSkillPassiveOption),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableSkill.TableSkillPassiveOption.Path),
                ResolveReference);
        }

        private static TableEditorTableDefinition ResolveReference(string headerName)
        {
            switch (headerName)
            {
                case "OptionGroupUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTableSkill.SkillPassiveOption);
                case "ApplyAffectUid":
                case "AffectUid":
                    return TableEditorRegistry.FindByKey("affect");
                default:
                    return TableEditorRegistry.FindReferenceTable(headerName);
            }
        }
    }
}
