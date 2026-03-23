using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public sealed class UseCrowdControlKnockBack : UseCrowdControlDetailWindowBase<StruckTableCrowdControlKnockBack>
    {
        private const string Title = "CrowdControl KnockBack 사용툴";
        private TableCrowdControlKnockBack _table;
        private Dictionary<int, StruckTableCrowdControlKnockBack> _tableDictionary;

        private static readonly TableRowEditorUtility.TableRowEditorField[] CachedRowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableCrowdControlKnockBack>(BuildRowEditorOptions());

        [MenuItem(ConfigEditor.NameToolUseCrowdControlKnockBack, false, (int)ConfigEditor.ToolOrdering.UseCrowdControlKnockBack)]
        public static void ShowWindow() => GetWindow<UseCrowdControlKnockBack>(Title);

        public static void OpenAndSelect(int crowdControlUid)
        {
            UseCrowdControlSelectionBridge.PendingCrowdControlUid = crowdControlUid;
            GetWindow<UseCrowdControlKnockBack>(Title).Show();
        }

        protected override string WindowTitle => Title;
        protected override string DropdownLabel => "KnockBack CrowdControl";
        protected override string ReloadButtonLabel => "crowd_control_knock_back 재로딩";
        protected override CrowdControlConstants.Type SupportedType => CrowdControlConstants.Type.KnockBack;
        protected override string DetailTableKey => ConfigAddressableTable.CrowdControlKnockBack;
        protected override string DetailTableAssetPath => ConfigAddressableTable.TableCrowdControlKnockBack.Path;
        protected override IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields => CachedRowEditorFields;

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableCrowdControlKnockBack.CrowdControlUid));
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.CrowdControlUid)] = "Reference";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.DownWaitTime)] = "Motion / Timing";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.EndYMode)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.EndYOffset)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.EndYAbsolute)] = "End Position";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.RecoverTime)] = "Recover";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.IsStopOnWall)] = "Flags";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.IsGroundOnly)] = "Flags";
            options.GroupByMemberName[nameof(StruckTableCrowdControlKnockBack.IsAirOnly)] = "Flags";
            return options;
        }

        protected override Dictionary<int, StruckTableCrowdControlKnockBack> LoadDetailRows()
        {
            _table = TableLoaderManager.LoadCrowdControlKnockBackTable(forceReload: true);
            _tableDictionary = _table != null ? _table.GetDatas() : new Dictionary<int, StruckTableCrowdControlKnockBack>();
            return _tableDictionary;
        }

        protected override StruckTableCrowdControlKnockBack CloneDetailRow(StruckTableCrowdControlKnockBack row)
            => TableRowEditorUtility.CloneShallow<StruckTableCrowdControlKnockBack>(row);

        protected override StruckTableCrowdControlKnockBack CreateDetailRowFromCommon(StruckTableCrowdControl commonRow)
        {
            return new StruckTableCrowdControlKnockBack
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

        protected override void AppendSpecificPreview(StringBuilder sb, StruckTableCrowdControlKnockBack row)
        {
            sb.AppendLine($"- DownWaitTime: {row.DownWaitTime}");
        }

        protected override Dictionary<int, StruckTableCrowdControlKnockBack> GetRuntimeRows(GGemCo2DCore.TableLoaderManager runtimeLoader)
            => runtimeLoader?.TableCrowdControlKnockBack?.GetDatas();

        protected override void NormalizeEditingFieldValue(object target, string memberName)
        {
            var row = target as StruckTableCrowdControlKnockBack;
            if (row == null)
                return;

            switch (memberName)
            {
                case nameof(StruckTableCrowdControlKnockBack.CrowdControlUid):
                    if (row.CrowdControlUid < 0) row.CrowdControlUid = 0;
                    break;
                case nameof(StruckTableCrowdControlKnockBack.DownWaitTime):
                    if (row.DownWaitTime < 0f) row.DownWaitTime = 0f;
                    break;
                case nameof(StruckTableCrowdControlKnockBack.RecoverTime):
                    if (row.RecoverTime < 0f) row.RecoverTime = 0f;
                    break;
            }
        }
    }
}
