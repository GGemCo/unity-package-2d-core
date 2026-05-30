using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class UseVfxParticle : UseVfxWindowBase<StruckTableVfxParticle>
    {
        private const string Title = "Vfx Particle 사용툴";

        private TableVfxParticle _tableVfxParticle;
        private Dictionary<int, StruckTableVfxParticle> _tableDictionary;

        private static readonly TableRowEditorUtility.TableRowEditorField[] CachedRowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableVfxParticle>(BuildRowEditorOptions());

        [MenuItem(ConfigEditor.NameToolUseVfxParticle, false, (int)ConfigEditor.ToolOrdering.UseVfxParticle)]
        public static void ShowWindow() => GetWindow<UseVfxParticle>(Title);

        protected override string WindowTitle => Title;
        protected override string DropdownLabel => "Vfx Particle";
        protected override string ReloadButtonLabel => "vfx_particle 재로딩";
        protected override IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields => CachedRowEditorFields;

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableVfxParticle.Uid));
            options.GroupByMemberName[nameof(StruckTableVfxParticle.Name)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.VfxUid)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.PrefabPath)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.LifecycleType)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.AttachType)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.FollowMode)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.FollowAnchorMode)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.PoolPrewarmCount)] = "Pool";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.PoolMaxSize)] = "Pool";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.Loop)] = "Pool";
            options.GroupByMemberName[nameof(StruckTableVfxParticle.UseUnscaledTime)] = "Pool";
            return options;
        }

        protected override void LoadTableInternal()
        {
            _tableVfxParticle = TableLoaderManager.LoadVfxParticleTable(forceReload: true);
            _tableDictionary = _tableVfxParticle != null
                ? _tableVfxParticle.GetDatas()
                : new Dictionary<int, StruckTableVfxParticle>();
        }

        protected override IEnumerable<StruckTableVfxParticle> EnumerateRows() => _tableDictionary?.Values;
        protected override string BuildDropdownValue(StruckTableVfxParticle row)
            => row == null ? string.Empty : $"[Particle] {row.Uid} - {row.Name} ({row.LifecycleType}/{row.AttachType})";
        protected override int GetRowUid(StruckTableVfxParticle row) => row?.Uid ?? 0;
        protected override StruckTableVfxParticle FindRowByUid(int uid)
            => uid > 0 && _tableDictionary != null && _tableDictionary.TryGetValue(uid, out var row) ? row : null;
        protected override StruckTableVfxParticle GetFirstRow()
        {
            if (_tableDictionary == null)
                return null;

            foreach (var pair in _tableDictionary)
                return pair.Value;

            return null;
        }

        protected override StruckTableVfxParticle CloneRow(StruckTableVfxParticle row)
            => TableRowEditorUtility.CloneShallow<StruckTableVfxParticle>(row);

        protected override void NormalizeEditingFieldValue(object target, string memberName)
        {
            base.NormalizeEditingFieldValue(target, memberName);

            var row = target as StruckTableVfxParticle;
            if (row == null || string.IsNullOrWhiteSpace(memberName))
                return;

            switch (memberName)
            {
                case nameof(StruckTableVfxParticle.PoolPrewarmCount):
                    if (row.PoolPrewarmCount < 0) row.PoolPrewarmCount = 0;
                    break;
                case nameof(StruckTableVfxParticle.PoolMaxSize):
                    if (row.PoolMaxSize < 0) row.PoolMaxSize = 0;
                    break;
            }
        }

        protected override void AppendRowPreview(StringBuilder sb, StruckTableVfxParticle row)
        {
            sb.AppendLine($"[Particle] {row.Uid} - {row.Name}");
            sb.AppendLine($"- VfxUid: {row.VfxUid}");
            sb.AppendLine($"- PrefabPath: {row.PrefabPath}");
            sb.AppendLine($"- LifecycleType: {row.LifecycleType}");
            sb.AppendLine($"- AttachType: {row.AttachType}");
            sb.AppendLine($"- FollowMode: {row.FollowMode}");
            sb.AppendLine($"- FollowAnchorMode: {row.FollowAnchorMode}");
            sb.AppendLine($"- PoolPrewarmCount: {row.PoolPrewarmCount}");
            sb.AppendLine($"- PoolMaxSize: {row.PoolMaxSize}");
            sb.AppendLine($"- Loop: {row.Loop}");
            sb.AppendLine($"- UseUnscaledTime: {row.UseUnscaledTime}");
        }

        protected override void ApplyRowToRuntime(StruckTableVfxParticle row)
        {
            if (row == null || !Application.isPlaying || !GGemCo2DCore.TableLoaderManager.Instance)
                return;

            var runtimeRow = GGemCo2DCore.TableLoaderManager.Instance.TableVfxParticle?.GetDataByUid(row.Uid);
            if (runtimeRow == null)
                return;

            TableRowEditorUtility.CopyMembers(row, runtimeRow, RowEditorFields);
        }

        protected override bool TrySaveTableFile(StruckTableVfxParticle row, out string error)
        {
            error = null;
            if (row == null)
            {
                error = "저장할 Row가 없습니다.";
                return false;
            }

            return TableTextRowPatchUtility.TryPatchRowByUid(
                ConfigAddressableTable.TableVfxParticle.Path,
                row.Uid,
                row,
                SerializeRow,
                out error);
        }

        private static string SerializeRow(StruckTableVfxParticle row, IReadOnlyList<string> headers)
        {
            var values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "VfxUid" => row.VfxUid.ToString(),
                    "PrefabPath" => row.PrefabPath ?? string.Empty,
                    "LifecycleType" => row.LifecycleType.ToString(),
                    "AttachType" => row.AttachType.ToString(),
                    "FollowMode" => row.FollowMode.ToString(),
                    "FollowAnchorMode" => row.FollowAnchorMode.ToString(),
                    "PoolPrewarmCount" => row.PoolPrewarmCount.ToString(),
                    "PoolMaxSize" => row.PoolMaxSize.ToString(),
                    "Loop" => MathHelper.FormatBool(row.Loop),
                    "UseUnscaledTime" => MathHelper.FormatBool(row.UseUnscaledTime),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }
    }
}
