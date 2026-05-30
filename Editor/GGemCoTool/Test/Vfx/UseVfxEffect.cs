using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class UseVfxEffect : UseVfxWindowBase<StruckTableVfxEffect>
    {
        private const string Title = "Vfx Effect 사용툴";

        private TableVfxEffect _tableVfxEffect;
        private Dictionary<int, StruckTableVfxEffect> _tableDictionary;

        private static readonly TableRowEditorUtility.TableRowEditorField[] CachedRowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableVfxEffect>(BuildRowEditorOptions());

        [MenuItem(ConfigEditor.NameToolUseVfxEffect, false, (int)ConfigEditor.ToolOrdering.UseVfxEffect)]
        public static void ShowWindow() => GetWindow<UseVfxEffect>(Title);

        protected override string WindowTitle => Title;
        protected override string DropdownLabel => "Vfx Effect";
        protected override string ReloadButtonLabel => "vfx_effect 재로딩";
        protected override IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields => CachedRowEditorFields;
        protected override bool UseOffsetOverrideField => true;

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableVfxEffect.Uid));
            options.GroupByMemberName[nameof(StruckTableVfxEffect.Name)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.VfxUid)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.Category)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.EffectType)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.PrefabPath)] = "에셋";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.AnimationController)] = "에셋";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.Width)] = "표현";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.Height)] = "표현";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.ColliderSize)] = "표현";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.NeedRotation)] = "표현";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.Color)] = "표현";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.DefaultDirection)] = "표현";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.LifecycleType)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.AttachType)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.FollowMode)] = "Spawn Policy";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.PoolPrewarmCount)] = "Pool";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.PoolMaxSize)] = "Pool";
            options.GroupByMemberName[nameof(StruckTableVfxEffect.UseUnscaledTime)] = "Pool";
            return options;
        }

        protected override void LoadTableInternal()
        {
            _tableVfxEffect = TableLoaderManager.LoadVfxEffectTable(forceReload: true);
            _tableDictionary = _tableVfxEffect != null
                ? _tableVfxEffect.GetDatas()
                : new Dictionary<int, StruckTableVfxEffect>();
        }

        protected override IEnumerable<StruckTableVfxEffect> EnumerateRows() => _tableDictionary?.Values;
        protected override string BuildDropdownValue(StruckTableVfxEffect row)
            => row == null ? string.Empty : $"[Effect] {row.Uid} - {row.Name} ({row.Category}/{row.EffectType})";
        protected override int GetRowUid(StruckTableVfxEffect row) => row?.Uid ?? 0;
        protected override StruckTableVfxEffect FindRowByUid(int uid)
            => uid > 0 && _tableDictionary != null && _tableDictionary.TryGetValue(uid, out var row) ? row : null;
        protected override StruckTableVfxEffect GetFirstRow()
        {
            if (_tableDictionary == null)
                return null;

            foreach (var pair in _tableDictionary)
                return pair.Value;

            return null;
        }

        protected override StruckTableVfxEffect CloneRow(StruckTableVfxEffect row)
            => TableRowEditorUtility.CloneShallow<StruckTableVfxEffect>(row);

        protected override void NormalizeEditingFieldValue(object target, string memberName)
        {
            base.NormalizeEditingFieldValue(target, memberName);

            var row = target as StruckTableVfxEffect;
            if (row == null || string.IsNullOrWhiteSpace(memberName))
                return;

            switch (memberName)
            {
                case nameof(StruckTableVfxEffect.Width):
                    if (row.Width < 0) row.Width = 0;
                    break;
                case nameof(StruckTableVfxEffect.Height):
                    if (row.Height < 0) row.Height = 0;
                    break;
                case nameof(StruckTableVfxEffect.ColliderSize):
                    row.ColliderSize = new Vector2(Mathf.Max(0f, row.ColliderSize.x), Mathf.Max(0f, row.ColliderSize.y));
                    break;
                case nameof(StruckTableVfxEffect.PoolPrewarmCount):
                    if (row.PoolPrewarmCount < 0) row.PoolPrewarmCount = 0;
                    break;
                case nameof(StruckTableVfxEffect.PoolMaxSize):
                    if (row.PoolMaxSize < 0) row.PoolMaxSize = 0;
                    break;
            }
        }

        protected override void AppendRowPreview(StringBuilder sb, StruckTableVfxEffect row)
        {
            sb.AppendLine($"[Effect] {row.Uid} - {row.Name}");
            sb.AppendLine($"- VfxUid: {row.VfxUid}");
            sb.AppendLine($"- Category: {row.Category}");
            sb.AppendLine($"- EffectType: {row.EffectType}");
            sb.AppendLine($"- PrefabPath: {row.PrefabPath}");
            sb.AppendLine($"- AnimationController: {row.AnimationController}");
            sb.AppendLine($"- Width/Height: {row.Width}/{row.Height}");
            sb.AppendLine($"- ColliderSize: {row.ColliderSize}");
            sb.AppendLine($"- NeedRotation: {row.NeedRotation}");
            sb.AppendLine($"- Color: {row.Color}");
            sb.AppendLine($"- DefaultDirection: {row.DefaultDirection}");
            sb.AppendLine($"- LifecycleType: {row.LifecycleType}");
            sb.AppendLine($"- AttachType: {row.AttachType}");
            sb.AppendLine($"- FollowMode: {row.FollowMode}");
            sb.AppendLine($"- PoolPrewarmCount: {row.PoolPrewarmCount}");
            sb.AppendLine($"- PoolMaxSize: {row.PoolMaxSize}");
            sb.AppendLine($"- UseUnscaledTime: {row.UseUnscaledTime}");
        }


        protected override bool UseDefaultUiCanvasParent(StruckTableVfxEffect row)
            => row != null && row.Category == VfxConstants.Category.UI;

        protected override bool UseDefaultUiSorting(StruckTableVfxEffect row)
            => row != null && row.Category == VfxConstants.Category.UI;

        protected override void ApplyRowToRuntime(StruckTableVfxEffect row)
        {
            if (row == null || !Application.isPlaying || !GGemCo2DCore.TableLoaderManager.Instance)
                return;

            var runtimeRow = GGemCo2DCore.TableLoaderManager.Instance.TableVfxEffect?.GetDataByUid(row.Uid);
            if (runtimeRow == null)
                return;

            TableRowEditorUtility.CopyMembers(row, runtimeRow, RowEditorFields);
        }

        protected override bool TrySaveTableFile(StruckTableVfxEffect row, out string error)
        {
            error = null;
            if (row == null)
            {
                error = "저장할 Row가 없습니다.";
                return false;
            }

            return TableTextRowPatchUtility.TryPatchRowByUid(
                ConfigAddressableTable.TableVfxEffect.Path,
                row.Uid,
                row,
                SerializeRow,
                out error);
        }

        private static string SerializeRow(StruckTableVfxEffect row, IReadOnlyList<string> headers)
        {
            var values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "VfxUid" => row.VfxUid.ToString(),
                    "Category" => row.Category.ToString(),
                    "EffectType" => row.EffectType.ToString(),
                    "PrefabPath" => row.PrefabPath ?? string.Empty,
                    "AnimationController" => row.AnimationController.ToString(),
                    "Width" => row.Width.ToString(),
                    "Height" => row.Height.ToString(),
                    "ColliderSize" => MathHelper.FormatVector2(row.ColliderSize),
                    "NeedRotation" => MathHelper.FormatBool(row.NeedRotation),
                    "Color" => row.Color ?? string.Empty,
                    "DefaultDirection" => row.DefaultDirection.ToString(),
                    "LifecycleType" => row.LifecycleType.ToString(),
                    "AttachType" => row.AttachType.ToString(),
                    "FollowMode" => row.FollowMode.ToString(),
                    "PoolPrewarmCount" => row.PoolPrewarmCount.ToString(),
                    "PoolMaxSize" => row.PoolMaxSize.ToString(),
                    "UseUnscaledTime" => MathHelper.FormatBool(row.UseUnscaledTime),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }
    }
}
