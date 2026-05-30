using System;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public abstract class UseVfxWindowBase<TRow> : DefaultEditorWindow where TRow : class, new()
    {
        [Header("Override")]
        private bool _followOwner;
        private bool _followTarget;
        private bool _spawnAtScreenCenter = true;
        private bool _forceUiCanvasParent;
        private bool _useUiSorting;
        private float _scale = 1f;
        private float _duration;
        private string _color = string.Empty;
        private float _positionY;
        private Vector3 _positionOffset = Vector3.zero;
        private ConfigCommon.PositionYType _positionYType = ConfigCommon.PositionYType.None;
        private bool _overrideLifecycleType;
        private VfxConstants.LifecycleType _lifecycleTypeOverride = VfxConstants.LifecycleType.AutoRelease;
        private bool _overrideAttachType;
        private VfxConstants.AttachType _attachTypeOverride = VfxConstants.AttachType.World;
        private bool _overrideFollowMode;
        private VfxConstants.FollowMode _followModeOverride = VfxConstants.FollowMode.None;
        private bool _overrideSortingOrder;
        private int _sortingOrderOverride;

        private readonly List<SearchableDropdownUtility.Option<TRow>> _dropDownOptions = new();
        private readonly Vector2 _minPreviewScroll = new Vector2(0f, 120f);

        private TRow _selectedRow;
        private TRow _cachedRow;
        private TRow _editingRow;
        private bool _editingDirty;
        private bool _foldRowEdit = true;
        private string _lastReloadMessage = string.Empty;
        private Vector2 _scroll;
        private Vector2 _previewScroll;

        protected abstract string WindowTitle { get; }
        protected abstract string DropdownLabel { get; }
        protected abstract string ReloadButtonLabel { get; }
        protected abstract IReadOnlyList<TableRowEditorUtility.TableRowEditorField> RowEditorFields { get; }
        protected virtual bool UseOffsetOverrideField => false;

        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadAllTables(preserveSelection: true);
            CacheSelectedRow();
        }

        protected override void OnSelectedCharacterChanged(CharacterBase character)
        {
            Repaint();
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollScope.scrollPosition;
                EditorGUILayout.Space(6);

                DrawPlayModeGate();
                EditorGUILayout.Space(6);

                DrawTargetSection();
                EditorGUILayout.Space(6);

                DrawTableSection();
                EditorGUILayout.Space(6);

                DrawBottomButtons();
                EditorGUILayout.Space(6);

                DrawRowEditorSection();
                EditorGUILayout.Space(6);

                DrawOverrideSection();
                EditorGUILayout.Space(6);

                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        private void DrawTargetSection()
        {
            DrawCharacterSelectionSection(WindowTitle, "Owner 캐릭터");
        }

        private void DrawTableSection()
        {
            EditorGUILayout.LabelField("테이블 선택", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_dropDownOptions.Count <= 0)
                {
                    EditorGUILayout.HelpBox("테이블 Row를 불러오지 못했습니다. 테이블 경로/설정을 확인해주세요.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(DropdownLabel);

                    string currentText = _selectedRow != null ? BuildDropdownValue(_selectedRow) : "선택...";
                    int selectedIndex = _selectedRow != null ? GetRowUid(_selectedRow) : 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _dropDownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (_, opt) =>
                        {
                            _selectedRow = opt.Data;
                            CacheSelectedRow();
                            Repaint();
                        },
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                EditorGUI.BeginChangeCheck();
                int newUid = EditorGUILayout.IntField("Uid", _selectedRow != null ? GetRowUid(_selectedRow) : 0);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedRow = FindRowByUid(Mathf.Max(0, newUid));
                    CacheSelectedRow();
                }
            }
        }

        private void DrawRowEditorSection()
        {
            EditorGUILayout.LabelField("테이블 Row 편집", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_editingRow == null)
                {
                    EditorGUILayout.HelpBox("편집할 Row를 선택하세요.", MessageType.Info);
                    return;
                }

                _foldRowEdit = EditorGUILayout.Foldout(_foldRowEdit, "Row 편집", true);
                if (!_foldRowEdit)
                    return;

                var result = TableRowEditorUtility.DrawObjectEditor(_editingRow, RowEditorFields, NormalizeEditingFieldValue);
                if (result.Changed)
                    _editingDirty = true;

                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_cachedRow == null))
                    {
                        if (GUILayout.Button("편집값 되돌리기", GUILayout.Height(24)))
                            CacheSelectedRow();
                    }

                    using (new EditorGUI.DisabledScope(_editingRow == null))
                    {
                        if (GUILayout.Button("편집값 적용", GUILayout.Height(24)))
                        {
                            ApplyEditingToCachedRow();
                            _editingDirty = false;
                        }
                    }
                }
                
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanApplyToRuntime()))
                    {
                        if (GUILayout.Button("인게임 테이블 적용", GUILayout.Height(24)))
                        {
                            CommitEditingIfNeeded();
                            ApplyRowToRuntime(_cachedRow);
                        }
                    }

                    using (new EditorGUI.DisabledScope(_cachedRow == null))
                    {
                        if (GUILayout.Button("테이블 파일 저장", GUILayout.Height(24)))
                        {
                            CommitEditingIfNeeded();
                            TrySaveTable();
                        }
                    }
                }
            }
        }

        private void DrawOverrideSection()
        {
            EditorGUILayout.LabelField("Override 프로퍼티", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _scale = Mathf.Max(0f, EditorGUILayout.FloatField("Scale", _scale));
                _duration = Mathf.Max(0f, EditorGUILayout.FloatField("Duration", _duration));
                _color = EditorGUILayout.TextField("Color(Hex)", _color ?? string.Empty);
                _positionY = EditorGUILayout.FloatField("PositionY", _positionY);
                _positionYType = (ConfigCommon.PositionYType)EditorGUILayout.EnumPopup("PositionYType", _positionYType);

                EditorGUILayout.Space(4);
                _followOwner = EditorGUILayout.ToggleLeft("Follow Owner", _followOwner);
                _followTarget = EditorGUILayout.ToggleLeft("Follow Target", _followTarget);
                if (UseOffsetOverrideField)
                    DrawFollowOffsetField();
                _spawnAtScreenCenter = EditorGUILayout.ToggleLeft("Spawn At Screen Center", _spawnAtScreenCenter);
                _forceUiCanvasParent = EditorGUILayout.ToggleLeft("Force UI Canvas Parent", _forceUiCanvasParent);
                _useUiSorting = EditorGUILayout.ToggleLeft("Force UI Sorting", _useUiSorting);

                EditorGUILayout.Space(6);
                _overrideLifecycleType = EditorGUILayout.ToggleLeft("LifecycleType Override 사용", _overrideLifecycleType);
                if (_overrideLifecycleType)
                    _lifecycleTypeOverride = (VfxConstants.LifecycleType)EditorGUILayout.EnumPopup("LifecycleType", _lifecycleTypeOverride);

                _overrideAttachType = EditorGUILayout.ToggleLeft("AttachType Override 사용", _overrideAttachType);
                if (_overrideAttachType)
                    _attachTypeOverride = (VfxConstants.AttachType)EditorGUILayout.EnumPopup("AttachType", _attachTypeOverride);

                _overrideFollowMode = EditorGUILayout.ToggleLeft("FollowMode Override 사용", _overrideFollowMode);
                if (_overrideFollowMode)
                    _followModeOverride = (VfxConstants.FollowMode)EditorGUILayout.EnumPopup("FollowMode", _followModeOverride);

                _overrideSortingOrder = EditorGUILayout.ToggleLeft("SortingOrder Override 사용", _overrideSortingOrder);
                if (_overrideSortingOrder)
                    _sortingOrderOverride = EditorGUILayout.IntField("SortingOrder", _sortingOrderOverride);
            }
        }

        /// <summary>
        /// Follow Target 기준으로 적용할 VFX 위치 오프셋을 입력합니다.
        /// Follow 대상이 없더라도 실제 스폰 기준 좌표에 동일한 월드 오프셋이 더해집니다.
        /// </summary>
        private void DrawFollowOffsetField()
        {
            _positionOffset = EditorGUILayout.Vector3Field("Offset", _positionOffset);
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.LabelField("실행 / 저장", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUI.DisabledScope(!CanExecute()))
                {
                    if (GUILayout.Button("VFX 실행", GUILayout.Height(28)))
                    {
                        CommitEditingIfNeeded();
                        ExecuteVfx();
                    }
                }
            }
        }

        private void DrawReloadSection()
        {
            DrawTableReloadSection(_lastReloadMessage, ReloadButtonLabel, () => ReloadAllTables(preserveSelection: true));
        }

        private void ReloadAllTables(bool preserveSelection)
        {
            int previousUid = preserveSelection && _selectedRow != null ? GetRowUid(_selectedRow) : 0;
            try
            {
                LoadTableInternal();
                RebuildDropdown();
                _selectedRow = previousUid > 0 ? FindRowByUid(previousUid) : GetFirstRow();
                CacheSelectedRow();
                _lastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _lastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }

        private void RebuildDropdown()
        {
            RebuildDropdownOptions(
                source: EnumerateRows(),
                targetOptions: _dropDownOptions,
                isValidRow: row => row != null && GetRowUid(row) > 0,
                keySelector: row => GetRowUid(row).ToString(),
                valueSelector: BuildDropdownValue,
                assignSelected: row => _selectedRow = row);
        }

        private void CacheSelectedRow()
        {
            _cachedRow = CloneRow(_selectedRow);
            _editingRow = CloneRow(_selectedRow);
            NormalizeEditingRow();
            _editingDirty = false;
        }

        private bool ApplyEditingToCachedRow()
        {
            if (_cachedRow == null || _editingRow == null)
                return false;

            TableRowEditorUtility.CopyMembers(_editingRow, _cachedRow, RowEditorFields);
            NormalizeCachedRow();
            return true;
        }

        private void CommitEditingIfNeeded()
        {
            if (!_editingDirty)
                return;

            if (ApplyEditingToCachedRow())
                _editingDirty = false;
        }

        private bool CanExecute()
        {
            return Application.isPlaying && SceneGame.Instance != null && _cachedRow != null && GetRowUid(_cachedRow) > 0;
        }

        private bool CanApplyToRuntime()
        {
            return Application.isPlaying && _cachedRow != null && GetRowUid(_cachedRow) > 0;
        }

        private void ExecuteVfx()
        {
            if (!CanExecute())
            {
                EditorUtility.DisplayDialog(WindowTitle, "Play Mode의 Game 씬에서만 실행할 수 있습니다.", "OK");
                return;
            }

            ApplyRowToRuntime(_cachedRow);

            var request = new VfxSpawnRequest
            {
                VfxUid = GetRowUid(_cachedRow),
                Owner = selectedCharacter,
                Target = null,
                FollowTarget = _followOwner ? selectedCharacter : null,
                ForceUiCanvasParent = _forceUiCanvasParent || UseDefaultUiCanvasParent(_cachedRow),
                DurationOverride = _duration,
                ScaleOverride = _scale,
                ColorOverride = _color,
                SortingLayerOverride = _useUiSorting || UseDefaultUiSorting(_cachedRow) ? ConfigSortingLayer.Keys.UI : null,
                SortingOrderOverride = _overrideSortingOrder ? _sortingOrderOverride : null,
                PositionY = _positionY,
                PositionYType = _positionYType,
                LifecycleTypeOverride = _overrideLifecycleType ? _lifecycleTypeOverride : null,
                AttachTypeOverride = _overrideAttachType ? _attachTypeOverride : null,
                FollowModeOverride = _overrideFollowMode ? _followModeOverride : null,
            };

            // Offset은 Follow Target/Owner/월드 스폰 어디서 시작하든 동일하게 월드 좌표에 더해집니다.
            if (UseOffsetOverrideField)
                request.PositionOffset = _positionOffset;

            if (_spawnAtScreenCenter && SceneGame.Instance.cameraManager != null)
                request.WorldPosition = SceneGame.Instance.cameraManager.GetPositionCenter();

            var vfx = SceneGame.Instance.VfxManager.CreateVfx(request);
            if (vfx == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "VFX 생성에 실패했습니다. PrefabPath / Addressables / 런타임 상태를 확인해주세요.", "OK");
                return;
            }
        }

        private void TrySaveTable()
        {
            if (!TrySaveTableFile(_cachedRow, out var error))
            {
                EditorUtility.DisplayDialog(WindowTitle, error, "OK");
                return;
            }

            _lastReloadMessage = $"테이블 저장 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            ReloadAllTables(preserveSelection: true);
        }

        protected virtual void NormalizeEditingFieldValue(object target, string memberName)
        {
        }

        private void NormalizeEditingRow()
        {
            if (_editingRow == null)
                return;

            foreach (var field in RowEditorFields)
                NormalizeEditingFieldValue(_editingRow, field.MemberName);
        }

        private void NormalizeCachedRow()
        {
            if (_cachedRow == null)
                return;

            foreach (var field in RowEditorFields)
                NormalizeEditingFieldValue(_cachedRow, field.MemberName);
        }

        protected abstract void LoadTableInternal();
        protected abstract IEnumerable<TRow> EnumerateRows();
        protected abstract string BuildDropdownValue(TRow row);
        protected abstract int GetRowUid(TRow row);
        protected abstract TRow FindRowByUid(int uid);
        protected abstract TRow GetFirstRow();
        protected abstract TRow CloneRow(TRow row);
        protected abstract void AppendRowPreview(StringBuilder sb, TRow row);
        protected virtual bool UseDefaultUiCanvasParent(TRow row) => false;
        protected virtual bool UseDefaultUiSorting(TRow row) => false;
        protected abstract void ApplyRowToRuntime(TRow row);
        protected abstract bool TrySaveTableFile(TRow row, out string error);
    }
}
