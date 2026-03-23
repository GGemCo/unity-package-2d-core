using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public sealed class UseCrowdControlKnockUp : UseCrowdControlDetailWindowBase<StruckTableCrowdControlKnockUp>
    {
        private const string Title = "CrowdControl KnockUp 사용툴";
        private TableCrowdControlKnockUp _table;
        private Dictionary<int, StruckTableCrowdControlKnockUp> _tableDictionary;

        private static readonly TableRowEditorUtility.TableRowEditorField[] CachedRowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableCrowdControlKnockUp>(BuildRowEditorOptions());

        [MenuItem(ConfigEditor.NameToolUseCrowdControlKnockUp, false, (int)ConfigEditor.ToolOrdering.UseCrowdControlKnockUp)]
        public static void ShowWindow() => GetWindow<UseCrowdControlKnockUp>(Title);

        public static void OpenAndSelect(int crowdControlUid)
        {
            UseCrowdControlSelectionBridge.PendingCrowdControlUid = crowdControlUid;
            GetWindow<UseCrowdControlKnockUp>(Title).Show();
        }

        protected override string WindowTitle => Title;
        protected override string DropdownLabel => "KnockUp CrowdControl";
        protected override string ReloadButtonLabel => "crowd_control_knock_up 재로딩";
        protected override CrowdControlConstants.Type SupportedType => CrowdControlConstants.Type.KnockUp;
        protected override string DetailTableKey => ConfigAddressableTable.CrowdControlKnockUp;
        protected override string DetailTableAssetPath => ConfigAddressableTable.TableCrowdControlKnockUp.Path;
        protected override IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields => CachedRowEditorFields;

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableCrowdControlKnockUp.CrowdControlUid));
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.CrowdControlUid)] = "Reference";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.Height)] = "Motion / Timing";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.EndYMode)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.EndYOffset)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.EndYAbsolute)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.RecoverTime)] = "Recover";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.IsStopOnWall)] = "Flags";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.IsGroundOnly)] = "Flags";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockUp.IsAirOnly)] = "Flags";
            return options;
        }

        protected override Dictionary<int, StruckTableCrowdControlKnockUp> LoadDetailRows()
        {
            _table = TableLoaderManager.LoadCrowdControlKnockUpTable(forceReload: true);
            _tableDictionary = _table != null ? _table.GetDatas() : new Dictionary<int, StruckTableCrowdControlKnockUp>();
            return _tableDictionary;
        }

        protected override StruckTableCrowdControlKnockUp CloneDetailRow(StruckTableCrowdControlKnockUp row)
            => TableRowEditorUtility.CloneShallow<StruckTableCrowdControlKnockUp>(row);

        protected override StruckTableCrowdControlKnockUp CreateDetailRowFromCommon(StruckTableCrowdControl commonRow)
        {
            return new StruckTableCrowdControlKnockUp
            {
                CrowdControlUid = commonRow?.Uid ?? 0,
                Height = commonRow?.Height ?? 0f,
                EndYMode = commonRow?.EndYMode ?? CrowdControlConstants.EndYMode.None,
                EndYOffset = commonRow?.EndYOffset ?? 0f,
                EndYAbsolute = commonRow?.EndYAbsolute ?? 0f,
                RecoverTime = commonRow?.RecoverTime ?? 0f,
                IsStopOnWall = commonRow?.IsStopOnWall ?? false,
                IsGroundOnly = commonRow?.IsGroundOnly ?? false,
                IsAirOnly = commonRow?.IsAirOnly ?? false,
            };
        }

        protected override void AppendSpecificPreview(StringBuilder sb, StruckTableCrowdControlKnockUp row)
        {
            sb.AppendLine($"- Height: {row.Height}");
        }

        protected override Dictionary<int, StruckTableCrowdControlKnockUp> GetRuntimeRows(GGemCo2DCore.TableLoaderManager runtimeLoader)
            => runtimeLoader?.TableCrowdControlKnockUp?.GetDatas();

        protected override void NormalizeEditingFieldValue(object target, string memberName)
        {
            var row = target as StruckTableCrowdControlKnockUp;
            if (row == null)
                return;

            switch (memberName)
            {
                case nameof(StruckTableCrowdControlKnockUp.CrowdControlUid):
                    if (row.CrowdControlUid < 0) row.CrowdControlUid = 0;
                    break;
                case nameof(StruckTableCrowdControlKnockUp.Height):
                    if (row.Height < 0f) row.Height = 0f;
                    break;
                case nameof(StruckTableCrowdControlKnockUp.RecoverTime):
                    if (row.RecoverTime < 0f) row.RecoverTime = 0f;
                    break;
            }
        }
    }
}
