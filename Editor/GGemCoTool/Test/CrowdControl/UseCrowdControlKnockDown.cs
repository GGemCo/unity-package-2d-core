using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public sealed class UseCrowdControlKnockDown : UseCrowdControlDetailWindowBase<StruckTableCrowdControlKnockDown>
    {
        private const string Title = "CrowdControl KnockDown 사용툴";
        private TableCrowdControlKnockDown _table;
        private Dictionary<int, StruckTableCrowdControlKnockDown> _tableDictionary;

        private static readonly TableRowEditorUtility.TableRowEditorField[] CachedRowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableCrowdControlKnockDown>(BuildRowEditorOptions());

        [MenuItem(ConfigEditor.NameToolUseCrowdControlKnockDown, false, (int)ConfigEditor.ToolOrdering.UseCrowdControlKnockDown)]
        public static void ShowWindow() => GetWindow<UseCrowdControlKnockDown>(Title);

        public static void OpenAndSelect(int crowdControlUid)
        {
            UseCrowdControlSelectionBridge.PendingCrowdControlUid = crowdControlUid;
            GetWindow<UseCrowdControlKnockDown>(Title).Show();
        }

        protected override string WindowTitle => Title;
        protected override string DropdownLabel => "KnockDown CrowdControl";
        protected override string ReloadButtonLabel => "crowd_control_knock_down 재로딩";
        protected override CrowdControlConstants.Type SupportedType => CrowdControlConstants.Type.KnockDown;
        protected override string DetailTableKey => ConfigAddressableTable.CrowdControlKnockDown;
        protected override string DetailTableAssetPath => ConfigAddressableTable.TableCrowdControlKnockDown.Path;
        protected override IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields => CachedRowEditorFields;

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableCrowdControlKnockDown.CrowdControlUid));
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.CrowdControlUid)] = "Reference";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.DownWaitTime)] = "Motion / Timing";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.EndYMode)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.EndYOffset)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.EndYAbsolute)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.RecoverTime)] = "Recover";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.IsStopOnWall)] = "Flags";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.IsGroundOnly)] = "Flags";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockDown.IsAirOnly)] = "Flags";
            return options;
        }

        protected override Dictionary<int, StruckTableCrowdControlKnockDown> LoadDetailRows()
        {
            _table = TableLoaderManager.LoadCrowdControlKnockDownTable(forceReload: true);
            _tableDictionary = _table != null ? _table.GetDatas() : new Dictionary<int, StruckTableCrowdControlKnockDown>();
            return _tableDictionary;
        }

        protected override StruckTableCrowdControlKnockDown CloneDetailRow(StruckTableCrowdControlKnockDown row)
            => TableRowEditorUtility.CloneShallow<StruckTableCrowdControlKnockDown>(row);

        protected override StruckTableCrowdControlKnockDown CreateDetailRowFromCommon(StruckTableCrowdControl commonRow)
        {
            return new StruckTableCrowdControlKnockDown
            {
                CrowdControlUid = commonRow?.Uid ?? 0,
                DownWaitTime = commonRow?.DownWaitTime ?? 0f,
                EndYMode = commonRow?.EndYMode ?? CrowdControlConstants.EndYMode.None,
                EndYOffset = commonRow?.EndYOffset ?? 0f,
                EndYAbsolute = commonRow?.EndYAbsolute ?? 0f,
                RecoverTime = commonRow?.RecoverTime ?? 0f,
                IsStopOnWall = commonRow?.IsStopOnWall ?? false,
                IsGroundOnly = commonRow?.IsGroundOnly ?? false,
                IsAirOnly = commonRow?.IsAirOnly ?? false,
            };
        }

        protected override void AppendSpecificPreview(StringBuilder sb, StruckTableCrowdControlKnockDown row)
        {
            sb.AppendLine($"- DownWaitTime: {row.DownWaitTime}");
        }

        protected override Dictionary<int, StruckTableCrowdControlKnockDown> GetRuntimeRows(GGemCo2DCore.TableLoaderManager runtimeLoader)
            => runtimeLoader?.TableCrowdControlKnockDown?.GetDatas();

        protected override void NormalizeEditingFieldValue(object target, string memberName)
        {
            var row = target as StruckTableCrowdControlKnockDown;
            if (row == null)
                return;

            switch (memberName)
            {
                case nameof(StruckTableCrowdControlKnockDown.CrowdControlUid):
                    if (row.CrowdControlUid < 0) row.CrowdControlUid = 0;
                    break;
                case nameof(StruckTableCrowdControlKnockDown.DownWaitTime):
                    if (row.DownWaitTime < 0f) row.DownWaitTime = 0f;
                    break;
                case nameof(StruckTableCrowdControlKnockDown.RecoverTime):
                    if (row.RecoverTime < 0f) row.RecoverTime = 0f;
                    break;
            }
        }
    }
}
