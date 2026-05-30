using System;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 대표 vfx 테이블 UID 기준으로 실제 VFX 해석 결과를 테스트하는 에디터 창입니다.
    /// </summary>
    public sealed class UseVfxDefinition : UseVfxWindowBase<StruckTableVfx>
    {
        private const string Title = "Vfx 사용툴";

        private TableVfx _tableVfx;
        private TableVfxVariant _tableVfxVariant;
        private Dictionary<int, StruckTableVfx> _tableDictionary;

        private static readonly TableRowEditorUtility.TableRowEditorField[] CachedRowEditorFields =
            TableRowEditorUtility.BuildFields<StruckTableVfx>(BuildRowEditorOptions());

        [MenuItem(ConfigEditor.NameToolUseVfx, false, (int)ConfigEditor.ToolOrdering.UseVfx)]
        public static void ShowWindow() => GetWindow<UseVfxDefinition>(Title);

        protected override string WindowTitle => Title;
        protected override string DropdownLabel => "Vfx";
        protected override string ReloadButtonLabel => "vfx / vfx_variant 재로딩";
        protected override IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields => CachedRowEditorFields;
        protected override bool UseOffsetOverrideField => true;

        private static TableRowEditorUtility.TableRowEditorBuildOptions BuildRowEditorOptions()
        {
            var options = new TableRowEditorUtility.TableRowEditorBuildOptions();
            options.ReadOnlyMembers.Add(nameof(StruckTableVfx.Uid));
            options.GroupByMemberName[nameof(StruckTableVfx.Name)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfx.Category)] = "기본";
            options.GroupByMemberName[nameof(StruckTableVfx.AssetKind)] = "직접 연결";
            options.GroupByMemberName[nameof(StruckTableVfx.ResolveMode)] = "선택 정책";
            options.GroupByMemberName[nameof(StruckTableVfx.SelectionMode)] = "선택 정책";
            options.GroupByMemberName[nameof(StruckTableVfx.NoRepeatRecentCount)] = "선택 정책";
            options.GroupByMemberName[nameof(StruckTableVfx.FallbackResourceUid)] = "Fallback";
            options.GroupByMemberName[nameof(StruckTableVfx.Enabled)] = "Flags";
            return options;
        }

        protected override void LoadTableInternal()
        {
            _tableVfx = TableLoaderManager.LoadVfxTable(forceReload: true);
            _tableVfxVariant = TableLoaderManager.LoadVfxVariantTable(forceReload: true);
            _tableDictionary = _tableVfx != null
                ? _tableVfx.GetDatas()
                : new Dictionary<int, StruckTableVfx>();
        }

        protected override IEnumerable<StruckTableVfx> EnumerateRows() => _tableDictionary?.Values;

        protected override string BuildDropdownValue(StruckTableVfx row)
            => row == null ? string.Empty : $"[Vfx] {row.Uid} - {row.Name} ({row.ResolveMode}/{row.SelectionMode})";

        protected override int GetRowUid(StruckTableVfx row) => row?.Uid ?? 0;

        protected override StruckTableVfx FindRowByUid(int uid)
            => uid > 0 && _tableDictionary != null && _tableDictionary.TryGetValue(uid, out var row) ? row : null;

        protected override StruckTableVfx GetFirstRow()
        {
            if (_tableDictionary == null)
                return null;

            foreach (var pair in _tableDictionary)
                return pair.Value;

            return null;
        }

        protected override StruckTableVfx CloneRow(StruckTableVfx row)
            => TableRowEditorUtility.CloneShallow<StruckTableVfx>(row);

        protected override void NormalizeEditingFieldValue(object target, string memberName)
        {
            base.NormalizeEditingFieldValue(target, memberName);

            var row = target as StruckTableVfx;
            if (row == null || string.IsNullOrWhiteSpace(memberName))
                return;

            switch (memberName)
            {
                case nameof(StruckTableVfx.NoRepeatRecentCount):
                    if (row.NoRepeatRecentCount < 0) row.NoRepeatRecentCount = 0;
                    break;
                case nameof(StruckTableVfx.FallbackResourceUid):
                    if (row.FallbackResourceUid < 0) row.FallbackResourceUid = 0;
                    break;
            }
        }

        protected override void AppendRowPreview(StringBuilder sb, StruckTableVfx row)
        {
            sb.AppendLine($"[Vfx] {row.Uid} - {row.Name}");
            sb.AppendLine($"- Category: {row.Category}");
            sb.AppendLine($"- AssetKind: {row.AssetKind}");
            sb.AppendLine($"- ResolveMode: {row.ResolveMode}");
            sb.AppendLine($"- SelectionMode: {row.SelectionMode}");
            sb.AppendLine($"- NoRepeatRecentCount: {row.NoRepeatRecentCount}");
            sb.AppendLine($"- FallbackResourceUid: {row.FallbackResourceUid}");
            sb.AppendLine($"- Enabled: {row.Enabled}");

            IReadOnlyList<StruckTableVfxVariant> variants = GetVariants(row.Uid);
            if (variants.Count <= 0)
            {
                sb.AppendLine("- Variants: (none)");
                return;
            }

            sb.AppendLine("- Variants:");
            for (int i = 0; i < variants.Count; i++)
            {
                StruckTableVfxVariant variant = variants[i];
                if (variant == null)
                    continue;

                sb.AppendLine($"  - {variant.Uid}: resource={variant.CandidateVfxResourceUid}, kind={variant.CandidateAssetKind}, weight={variant.Weight}, enabled={variant.Enabled}");
            }
        }

        protected override void ApplyRowToRuntime(StruckTableVfx row)
        {
            if (row == null || !Application.isPlaying || !GGemCo2DCore.TableLoaderManager.Instance)
                return;

            var runtimeRow = GGemCo2DCore.TableLoaderManager.Instance.TableVfx?.GetDataByUid(row.Uid);
            if (runtimeRow == null)
                return;

            TableRowEditorUtility.CopyMembers(row, runtimeRow, RowEditorFields);
        }

        protected override bool TrySaveTableFile(StruckTableVfx row, out string error)
        {
            error = null;
            if (row == null)
            {
                error = "저장할 Row가 없습니다.";
                return false;
            }

            return TableTextRowPatchUtility.TryPatchRowByUid(
                ConfigAddressableTable.TableVfx.Path,
                row.Uid,
                row,
                SerializeRow,
                out error);
        }

        private IReadOnlyList<StruckTableVfxVariant> GetVariants(int vfxUid)
        {
            return vfxUid > 0 && _tableVfxVariant != null
                ? _tableVfxVariant.GetVariants(vfxUid)
                : Array.Empty<StruckTableVfxVariant>();
        }

        private static string SerializeRow(StruckTableVfx row, IReadOnlyList<string> headers)
        {
            var values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "Category" => row.Category.ToString(),
                    "AssetKind" => row.AssetKind.ToString(),
                    "ResolveMode" => row.ResolveMode.ToString(),
                    "SelectionMode" => row.SelectionMode.ToString(),
                    "NoRepeatRecentCount" => row.NoRepeatRecentCount.ToString(),
                    "FallbackResourceUid" => row.FallbackResourceUid.ToString(),
                    "Enabled" => MathHelper.FormatBool(row.Enabled),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }
    }
}
