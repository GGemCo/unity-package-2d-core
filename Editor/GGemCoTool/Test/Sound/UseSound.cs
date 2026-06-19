using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 대표 sound UID 기준으로 사운드 해석, 실제 재생, 리소스 로드, Row 편집을 검증하는 커스텀 테스트 툴입니다.
    /// </summary>
    public sealed class UseSound : DefaultEditorWindow
    {
        private const string Title = "사운드 사용툴";
        private const float PreviewMinHeight = 140f;

        private enum SoundTypeFilter
        {
            All = -1,
            None = 0,
            Bgm = 1,
            Ambient = 2,
            Sfx = 3,
        }

        private enum ResolveModeFilter
        {
            All = -1,
            Direct = 0,
            Variant = 1,
        }

        private struct ValidationMessage
        {
            public readonly MessageType Type;
            public readonly string Message;

            public ValidationMessage(MessageType type, string message)
            {
                Type = type;
                Message = message;
            }
        }

        private static readonly TableRowEditorUtility.TableRowEditorField[] SoundRowEditorFields =
        {
            new(nameof(StruckTableSound.Uid), readOnly: true),
            new(nameof(StruckTableSound.Name), group: "기본"),
            new(nameof(StruckTableSound.Type), group: "기본"),
            new(nameof(StruckTableSound.SubType), group: "기본"),
            new(nameof(StruckTableSound.ResolveMode), group: "해석"),
            new(nameof(StruckTableSound.SelectionMode), group: "해석"),
            new(nameof(StruckTableSound.VolumeScale), group: "해석"),
            new(nameof(StruckTableSound.NoRepeatRecentCount), group: "해석"),
            new(nameof(StruckTableSound.FallbackResourceUid), group: "해석"),
            new(nameof(StruckTableSound.UseIntroScene), group: "Flags"),
        };

        private static readonly TableRowEditorUtility.TableRowEditorField[] ResourceRowEditorFields =
        {
            new(nameof(StruckTableSoundResource.Uid), readOnly: true),
            new(nameof(StruckTableSoundResource.Name), group: "기본"),
            new(nameof(StruckTableSoundResource.SoundUid), group: "기본", readOnly: true),
            new(nameof(StruckTableSoundResource.Type), group: "기본", readOnly: true),
            new(nameof(StruckTableSoundResource.SubType), group: "기본"),
            new(nameof(StruckTableSoundResource.FileName), group: "에셋"),
            new(nameof(StruckTableSoundResource.MaxPlayCount), group: "재생"),
            new(nameof(StruckTableSoundResource.Volume), group: "재생"),
            new(nameof(StruckTableSoundResource.PitchMin), group: "재생"),
            new(nameof(StruckTableSoundResource.PitchMax), group: "재생"),
            new(nameof(StruckTableSoundResource.Loop), group: "재생"),
            new(nameof(StruckTableSoundResource.FadeDuration), group: "재생"),
            new(nameof(StruckTableSoundResource.UseIntroScene), group: "Flags"),
            new(nameof(StruckTableSoundResource.PreLoad), group: "Flags"),
        };

        private static readonly TableRowEditorUtility.TableRowEditorField[] VariantRowEditorFields =
        {
            new(nameof(StruckTableSoundVariant.Uid), readOnly: true),
            new(nameof(StruckTableSoundVariant.Name), group: "기본"),
            new(nameof(StruckTableSoundVariant.SoundUid), group: "기본", readOnly: true),
            new(nameof(StruckTableSoundVariant.CandidateResourceUid), group: "후보"),
            new(nameof(StruckTableSoundVariant.Weight), group: "후보"),
            new(nameof(StruckTableSoundVariant.VolumeScale), group: "보정"),
            new(nameof(StruckTableSoundVariant.PitchMinOverride), group: "보정"),
            new(nameof(StruckTableSoundVariant.PitchMaxOverride), group: "보정"),
            new(nameof(StruckTableSoundVariant.Enabled), group: "Flags"),
        };

        private readonly List<SearchableDropdownUtility.Option<StruckTableSound>> _soundDropdownOptions = new();
        private readonly List<SearchableDropdownUtility.Option<StruckTableSoundVariant>> _variantDropdownOptions = new();
        private readonly List<ValidationMessage> _validationMessages = new();

        private TableSound _tableSound;
        private TableSoundBgm _tableSoundBgm;
        private TableSoundAmbient _tableSoundAmbient;
        private TableSoundSfx _tableSoundSfx;
        private TableSoundVariant _tableSoundVariant;

        private StruckTableSound _selectedSound;
        private StruckTableSound _cachedSound;
        private StruckTableSound _editingSound;

        private StruckTableSoundResource _selectedResource;
        private StruckTableSoundResource _cachedResource;
        private StruckTableSoundResource _editingResource;

        private StruckTableSoundVariant _selectedVariant;
        private StruckTableSoundVariant _cachedVariant;
        private StruckTableSoundVariant _editingVariant;

        private SoundTypeFilter _typeFilter = SoundTypeFilter.All;
        private ResolveModeFilter _resolveModeFilter = ResolveModeFilter.All;
        private bool _foldSoundRow = true;
        private bool _foldResourceRow = true;
        private bool _foldVariantRow = true;
        private bool _foldValidation = true;
        private int _repeatPlayCount = 1;
        private float _repeatPlayDelay = 0.05f;
        private string _lastReloadMessage = string.Empty;
        private string _lastClipLoadMessage = string.Empty;
        private string _lastSimulationMessage = string.Empty;
        private Vector2 _scroll;
        private Vector2 _previewScroll;
        private Vector2 _variantListScroll;

        [MenuItem(ConfigEditor.NameToolUseSound, false, (int)ConfigEditor.ToolOrdering.UseSound)]
        public static void ShowWindow() => GetWindow<UseSound>(Title);

        /// <summary>
        /// 윈도우 활성화 시 에디터 테이블을 로드하고 선택/편집 캐시를 초기화합니다.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadAllTables(preserveSelection: true);
        }

        /// <summary>
        /// 사운드 테스트 툴의 전체 IMGUI 화면을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollScope.scrollPosition;
                EditorGUILayout.Space(6);

                DrawStatusSection();
                EditorGUILayout.Space(6);

                DrawSoundSelectionSection();
                EditorGUILayout.Space(6);

                DrawSoundRowSection();
                EditorGUILayout.Space(6);

                DrawResourceRowSection();
                EditorGUILayout.Space(6);

                DrawVariantSection();
                EditorGUILayout.Space(6);

                DrawResolvePreviewSection();
                EditorGUILayout.Space(6);

                DrawValidationSection();
                EditorGUILayout.Space(6);

                DrawRuntimeControlSection();
                EditorGUILayout.Space(6);

                DrawReloadSection();
                EditorGUILayout.Space(20);
            }
        }

        /// <summary>
        /// Play Mode, SceneGame, SoundManager, TableLoaderManager, AddressableLoaderSound 상태를 표시합니다.
        /// </summary>
        private void DrawStatusSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("실행 조건", EditorStyles.boldLabel);
                DrawStatusLine("Play Mode", Application.isPlaying);
                DrawStatusLine("SceneGame.Instance", SceneGame.Instance != null);
                DrawStatusLine("SoundManager", SceneGame.Instance != null && SceneGame.Instance.soundManager != null);
                DrawStatusLine("Runtime TableLoaderManager", GGemCo2DCore.TableLoaderManager.Instance != null);
                DrawStatusLine("AddressableLoaderSound", AddressableLoaderSound.Instance != null);

                if (!Application.isPlaying)
                    EditorGUILayout.HelpBox("실제 재생과 Addressables AudioClip 로드 검증은 Play Mode의 Game 씬에서 실행해주세요.", MessageType.Info);
            }
        }

        /// <summary>
        /// 개별 런타임 의존성 상태를 한 줄로 표시합니다.
        /// </summary>
        /// <param name="label">상태 이름입니다.</param>
        /// <param name="ok">정상 여부입니다.</param>
        private static void DrawStatusLine(string label, bool ok)
        {
            EditorGUILayout.LabelField(label, ok ? "OK" : "Missing");
        }

        /// <summary>
        /// 대표 sound UID 선택과 필터 UI를 그립니다.
        /// </summary>
        private void DrawSoundSelectionSection()
        {
            EditorGUILayout.LabelField("대표 sound 선택", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUI.BeginChangeCheck();
                _typeFilter = (SoundTypeFilter)EditorGUILayout.EnumPopup("Type Filter", _typeFilter);
                _resolveModeFilter = (ResolveModeFilter)EditorGUILayout.EnumPopup("ResolveMode Filter", _resolveModeFilter);
                if (EditorGUI.EndChangeCheck())
                {
                    RebuildSoundDropdown(preserveSelection: true);
                    if (_selectedSound == null || !MatchesSoundFilter(_selectedSound))
                        SetSelectedSound(GetFirstSoundFromDropdown());
                    else
                        SetSelectedSound(_selectedSound);
                }

                if (_soundDropdownOptions.Count <= 0)
                {
                    EditorGUILayout.HelpBox("sound 테이블 Row를 불러오지 못했거나 필터 결과가 없습니다.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Sound");
                    string currentText = _selectedSound != null ? BuildSoundDropdownValue(_selectedSound) : "선택...";
                    int selectedIndex = _selectedSound != null ? _selectedSound.Uid : 0;

                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _soundDropdownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (_, option) => SetSelectedSound(option.Data),
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                EditorGUI.BeginChangeCheck();
                int newUid = EditorGUILayout.IntField("Uid", _selectedSound != null ? _selectedSound.Uid : 0);
                if (EditorGUI.EndChangeCheck())
                    SetSelectedSound(FindSoundByUid(Mathf.Max(0, newUid)));
            }
        }

        /// <summary>
        /// 대표 sound Row 편집 UI를 그립니다.
        /// </summary>
        private void DrawSoundRowSection()
        {
            EditorGUILayout.LabelField("대표 sound Row 편집", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_editingSound == null)
                {
                    EditorGUILayout.HelpBox("편집할 sound Row를 선택하세요.", MessageType.Info);
                    return;
                }

                _foldSoundRow = EditorGUILayout.Foldout(_foldSoundRow, "sound Row", true);
                if (!_foldSoundRow)
                    return;

                TableRowEditorUtility.DrawResult result = TableRowEditorUtility.DrawObjectEditor(_editingSound, SoundRowEditorFields, NormalizeSoundFieldValue);
                if (result.Changed)
                {
                    CacheResourceAndVariantForCurrentSound();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("편집값 되돌리기", GUILayout.Height(24)))
                        CacheSoundRow();

                    using (new EditorGUI.DisabledScope(_editingSound == null))
                    {
                        if (GUILayout.Button("편집값 적용", GUILayout.Height(24)))
                            CommitSoundEditingIfNeeded();
                    }
                }
            }
        }

        /// <summary>
        /// 선택된 sound에 연결된 실제 리소스 Row 편집 UI를 그립니다.
        /// </summary>
        private void DrawResourceRowSection()
        {
            EditorGUILayout.LabelField("연결 리소스 Row 편집", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_editingResource == null)
                {
                    EditorGUILayout.HelpBox("선택된 sound에 직접 연결된 sound_bgm/sound_ambient/sound_sfx Row가 없습니다.", MessageType.Info);
                    return;
                }

                _foldResourceRow = EditorGUILayout.Foldout(_foldResourceRow, $"{GetResourceTableKey(_editingResource.Type)} Row", true);
                if (!_foldResourceRow)
                    return;

                TableRowEditorUtility.DrawResult result = TableRowEditorUtility.DrawObjectEditor(_editingResource, ResourceRowEditorFields, NormalizeResourceFieldValue);

                EditorGUILayout.Space(4);
                bool useFadeDurationOverride = EditorGUILayout.Toggle(
                    "FadeDuration Override",
                    _editingResource.HasFadeDurationOverride());
                if (useFadeDurationOverride != _editingResource.HasFadeDurationOverride())
                    _editingResource.SetFadeDurationOverride(useFadeDurationOverride);

                EditorGUILayout.LabelField("Addressables Key", _editingResource.BuildAddressKey());

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("편집값 되돌리기", GUILayout.Height(24)))
                        CacheResourceRow();

                    using (new EditorGUI.DisabledScope(_editingResource == null))
                    {
                        if (GUILayout.Button("편집값 적용", GUILayout.Height(24)))
                            CommitResourceEditingIfNeeded();
                    }
                }
            }
        }

        /// <summary>
        /// 선택된 sound의 variant 후보 목록과 개별 variant Row 편집 UI를 그립니다.
        /// </summary>
        private void DrawVariantSection()
        {
            EditorGUILayout.LabelField("Variant 후보", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                RebuildVariantDropdown();

                IReadOnlyList<StruckTableSoundVariant> variants = GetVariantsForSelectedSound();
                if (variants == null || variants.Count == 0)
                {
                    EditorGUILayout.HelpBox("선택된 sound에 연결된 variant 후보가 없습니다.", MessageType.Info);
                    return;
                }

                _variantListScroll = EditorGUILayout.BeginScrollView(_variantListScroll, GUILayout.MinHeight(80f), GUILayout.MaxHeight(140f));
                for (int i = 0; i < variants.Count; i++)
                {
                    StruckTableSoundVariant variant = variants[i];
                    if (variant == null)
                        continue;

                    StruckTableSoundResource candidate = FindResourceByUid(GetCurrentSoundType(), variant.CandidateResourceUid);
                    string candidateText = candidate != null ? $"{candidate.Uid} - {candidate.Name} ({candidate.FileName})" : "(missing/silent)";
                    EditorGUILayout.LabelField($"[{variant.Uid}] Weight={variant.Weight}, Enabled={variant.Enabled}", candidateText);
                }
                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("편집 Variant");
                    string currentText = _selectedVariant != null ? BuildVariantDropdownValue(_selectedVariant) : "선택...";
                    int selectedIndex = _selectedVariant != null ? _selectedVariant.Uid : 0;
                    SearchableDropdownUtility.DrawButtonAndShow(
                        buttonText: currentText,
                        options: _variantDropdownOptions,
                        selectedIndex: selectedIndex,
                        onSelected: (_, option) => SetSelectedVariant(option.Data),
                        defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
                }

                _foldVariantRow = EditorGUILayout.Foldout(_foldVariantRow, "sound_variant Row", true);
                if (_foldVariantRow && _editingVariant != null)
                {
                    TableRowEditorUtility.DrawResult result = TableRowEditorUtility.DrawObjectEditor(_editingVariant, VariantRowEditorFields, NormalizeVariantFieldValue);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("편집값 되돌리기", GUILayout.Height(24)))
                            CacheVariantRow();

                        if (GUILayout.Button("편집값 적용", GUILayout.Height(24)))
                            CommitVariantEditingIfNeeded();
                    }
                }
            }
        }

        /// <summary>
        /// 런타임 SoundResolver 결과와 편집 중인 정적 Row 정보를 미리보기로 표시합니다.
        /// </summary>
        private void DrawResolvePreviewSection()
        {
            EditorGUILayout.LabelField("해석 결과 미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                StringBuilder sb = new StringBuilder();
                AppendStaticPreview(sb);
                AppendRuntimeResolvePreview(sb);

                _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MinHeight(PreviewMinHeight));
                EditorGUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 데이터 검증 결과를 Warning/Error 단위로 표시합니다.
        /// </summary>
        private void DrawValidationSection()
        {
            _validationMessages.Clear();
            CollectValidationMessages(_validationMessages);

            EditorGUILayout.LabelField("검증", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _foldValidation = EditorGUILayout.Foldout(_foldValidation, $"검증 결과 ({_validationMessages.Count})", true);
                if (!_foldValidation)
                    return;

                if (_validationMessages.Count == 0)
                {
                    EditorGUILayout.HelpBox("현재 선택 기준으로 발견된 문제는 없습니다.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < _validationMessages.Count; i++)
                    EditorGUILayout.HelpBox(_validationMessages[i].Message, _validationMessages[i].Type);
            }
        }

        /// <summary>
        /// 실제 재생, 로드 검증, 런타임 적용, 테이블 저장 버튼을 그립니다.
        /// </summary>
        private void DrawRuntimeControlSection()
        {
            EditorGUILayout.LabelField("실행 / 저장", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _repeatPlayCount = Mathf.Max(1, EditorGUILayout.IntField("반복 재생 Count", _repeatPlayCount));
                _repeatPlayDelay = Mathf.Max(0f, EditorGUILayout.FloatField("반복 재생 Delay", _repeatPlayDelay));

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanUseRuntimeSoundManager()))
                    {
                        if (GUILayout.Button("선택 Sound 재생", GUILayout.Height(26)))
                            PlaySelectedSound();

                        if (GUILayout.Button("반복 재생", GUILayout.Height(26)))
                            PlaySelectedSoundRepeated();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanUseRuntimeSoundManager()))
                    {
                        if (GUILayout.Button("BGM 정지", GUILayout.Height(24)))
                            SceneGame.Instance.soundManager.StopBgm();

                        if (GUILayout.Button("Ambient 전체 정지", GUILayout.Height(24)))
                            SceneGame.Instance.soundManager.StopAmbient();

                        using (new EditorGUI.DisabledScope(_editingResource == null || _editingResource.Type != SoundConstants.Type.Ambient))
                        {
                            if (GUILayout.Button("선택 Ambient 정지", GUILayout.Height(24)))
                                SceneGame.Instance.soundManager.StopAmbientByResourceUid(_editingResource.Uid);
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanUseRuntimeSoundManager()))
                    {
                        if (GUILayout.Button("런타임 Row 적용", GUILayout.Height(24)))
                            ApplyEditingToRuntime();

                        if (GUILayout.Button("SFX Pool 재초기화", GUILayout.Height(24)))
                            SceneGame.Instance.soundManager.ReinitializeSoundSfxPool();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_cachedSound == null))
                    {
                        if (GUILayout.Button("sound 저장", GUILayout.Height(24)))
                            SaveSoundTableFile();
                    }

                    using (new EditorGUI.DisabledScope(_cachedResource == null))
                    {
                        if (GUILayout.Button("resource 저장", GUILayout.Height(24)))
                            SaveResourceTableFile();
                    }

                    using (new EditorGUI.DisabledScope(_cachedVariant == null))
                    {
                        if (GUILayout.Button("variant 저장", GUILayout.Height(24)))
                            SaveVariantTableFile();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!CanLoadSelectedClip()))
                    {
                        if (GUILayout.Button("AudioClip 로드 검증", GUILayout.Height(24)))
                            LoadSelectedAudioClipAsync();
                    }

                    using (new EditorGUI.DisabledScope(!CanUseRuntimeSoundManager()))
                    {
                        if (GUILayout.Button("10회 Resolve 분포", GUILayout.Height(24)))
                            SimulateResolveDistribution(10);

                        if (GUILayout.Button("100회 Resolve 분포", GUILayout.Height(24)))
                            SimulateResolveDistribution(100);
                    }
                }

                if (!string.IsNullOrWhiteSpace(_lastClipLoadMessage))
                    EditorGUILayout.HelpBox(_lastClipLoadMessage, MessageType.Info);

                if (!string.IsNullOrWhiteSpace(_lastSimulationMessage))
                    EditorGUILayout.HelpBox(_lastSimulationMessage, MessageType.Info);
            }
        }

        /// <summary>
        /// 테이블 재로딩 UI를 그립니다.
        /// </summary>
        private void DrawReloadSection()
        {
            DrawTableReloadSection(_lastReloadMessage, "sound 관련 테이블 재로딩", () => ReloadAllTables(preserveSelection: true));
        }

        /// <summary>
        /// 에디터 테이블을 다시 로드하고 기존 선택을 가능한 유지합니다.
        /// </summary>
        /// <param name="preserveSelection">기존 sound UID 선택을 유지할지 여부입니다.</param>
        private void ReloadAllTables(bool preserveSelection)
        {
            int previousUid = preserveSelection && _selectedSound != null ? _selectedSound.Uid : 0;
            try
            {
                _tableSound = TableLoaderManager.LoadSoundTable(forceReload: true);
                _tableSoundBgm = TableLoaderManager.LoadSoundBgmTable(forceReload: true);
                _tableSoundAmbient = TableLoaderManager.LoadSoundAmbientTable(forceReload: true);
                _tableSoundSfx = TableLoaderManager.LoadSoundSfxTable(forceReload: true);
                _tableSoundVariant = TableLoaderManager.LoadSoundVariantTable(forceReload: true);

                RebuildSoundDropdown(preserveSelection: false);
                SetSelectedSound(previousUid > 0 ? FindSoundByUid(previousUid) : GetFirstSoundFromDropdown());
                _lastReloadMessage = $"테이블 재로딩 완료: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _lastReloadMessage = $"테이블 재로딩 실패: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }

        /// <summary>
        /// 필터 조건에 맞는 sound 드롭다운 옵션을 다시 구성합니다.
        /// </summary>
        /// <param name="preserveSelection">기존 선택을 유지할지 여부입니다.</param>
        private void RebuildSoundDropdown(bool preserveSelection)
        {
            int previousUid = preserveSelection && _selectedSound != null ? _selectedSound.Uid : 0;
            RebuildDropdownOptions(
                source: _tableSound?.GetDatas()?.Values,
                targetOptions: _soundDropdownOptions,
                isValidRow: row => row != null && row.Uid > 0,
                keySelector: row => row.Uid.ToString(),
                valueSelector: BuildSoundDropdownValue,
                assignSelected: row =>
                {
                    if (!preserveSelection || previousUid <= 0)
                    {
                        _selectedSound = row;
                        return;
                    }

                    _selectedSound = FindSoundByUid(previousUid) ?? row;
                },
                filter: MatchesSoundFilter);
        }

        /// <summary>
        /// 현재 필터 설정과 sound Row가 일치하는지 확인합니다.
        /// </summary>
        /// <param name="row">검사할 sound Row입니다.</param>
        /// <returns>필터를 통과하면 true입니다.</returns>
        private bool MatchesSoundFilter(StruckTableSound row)
        {
            if (row == null)
                return false;

            if (_typeFilter != SoundTypeFilter.All && row.Type != (SoundConstants.Type)_typeFilter)
                return false;

            if (_resolveModeFilter != ResolveModeFilter.All && row.ResolveMode != (SoundConstants.ResolveMode)_resolveModeFilter)
                return false;

            return true;
        }

        /// <summary>
        /// 선택된 sound에 연결된 variant 드롭다운 옵션을 다시 구성합니다.
        /// </summary>
        private void RebuildVariantDropdown()
        {
            int previousUid = _selectedVariant != null ? _selectedVariant.Uid : 0;
            _variantDropdownOptions.Clear();

            IReadOnlyList<StruckTableSoundVariant> variants = GetVariantsForSelectedSound();
            if (variants == null)
            {
                SetSelectedVariant(null);
                return;
            }

            for (int i = 0; i < variants.Count; i++)
            {
                StruckTableSoundVariant variant = variants[i];
                if (variant == null || variant.Uid <= 0)
                    continue;

                _variantDropdownOptions.Add(new SearchableDropdownUtility.Option<StruckTableSoundVariant>(
                    variant.Uid.ToString(),
                    BuildVariantDropdownValue(variant),
                    variant));
            }

            if (_variantDropdownOptions.Count == 0)
            {
                SetSelectedVariant(null);
                return;
            }

            StruckTableSoundVariant next = previousUid > 0 ? FindVariantByUid(previousUid) : _variantDropdownOptions[0].Data;
            if (next != _selectedVariant)
                SetSelectedVariant(next ?? _variantDropdownOptions[0].Data);
        }

        /// <summary>
        /// 선택한 대표 sound Row를 변경하고 관련 캐시를 갱신합니다.
        /// </summary>
        /// <param name="sound">새로 선택할 sound Row입니다.</param>
        private void SetSelectedSound(StruckTableSound sound)
        {
            _selectedSound = sound;
            CacheSoundRow();
            CacheResourceAndVariantForCurrentSound();
            Repaint();
        }

        /// <summary>
        /// 선택된 sound를 기준으로 리소스와 variant 선택/편집 캐시를 갱신합니다.
        /// </summary>
        private void CacheResourceAndVariantForCurrentSound()
        {
            _selectedResource = FindFirstResourceBySound(_editingSound ?? _selectedSound);
            CacheResourceRow();

            IReadOnlyList<StruckTableSoundVariant> variants = GetVariantsForSelectedSound();
            _selectedVariant = variants != null && variants.Count > 0 ? variants[0] : null;
            CacheVariantRow();
        }

        /// <summary>
        /// 편집 대상 variant Row를 변경합니다.
        /// </summary>
        /// <param name="variant">새로 선택할 variant Row입니다.</param>
        private void SetSelectedVariant(StruckTableSoundVariant variant)
        {
            _selectedVariant = variant;
            CacheVariantRow();
            Repaint();
        }

        /// <summary>
        /// 선택된 sound Row를 편집용 캐시로 복사합니다.
        /// </summary>
        private void CacheSoundRow()
        {
            _cachedSound = CloneSound(_selectedSound);
            _editingSound = CloneSound(_selectedSound);
            NormalizeSoundRow(_cachedSound);
            NormalizeSoundRow(_editingSound);
        }

        /// <summary>
        /// 선택된 리소스 Row를 편집용 캐시로 복사합니다.
        /// </summary>
        private void CacheResourceRow()
        {
            _cachedResource = CloneResource(_selectedResource);
            _editingResource = CloneResource(_selectedResource);
            NormalizeResourceRow(_cachedResource);
            NormalizeResourceRow(_editingResource);
        }

        /// <summary>
        /// 선택된 variant Row를 편집용 캐시로 복사합니다.
        /// </summary>
        private void CacheVariantRow()
        {
            _cachedVariant = CloneVariant(_selectedVariant);
            _editingVariant = CloneVariant(_selectedVariant);
            NormalizeVariantRow(_cachedVariant);
            NormalizeVariantRow(_editingVariant);
        }

        /// <summary>
        /// sound 편집값이 변경되어 있으면 캐시 Row에 반영합니다.
        /// </summary>
        private void CommitSoundEditingIfNeeded()
        {
            if (_editingSound == null || _cachedSound == null)
                return;

            TableRowEditorUtility.CopyMembers(_editingSound, _cachedSound, SoundRowEditorFields);
            NormalizeSoundRow(_cachedSound);
        }

        /// <summary>
        /// 리소스 편집값이 변경되어 있으면 캐시 Row에 반영합니다.
        /// </summary>
        private void CommitResourceEditingIfNeeded()
        {
            if (_editingResource == null || _cachedResource == null)
                return;

            CopyResourceMembers(_editingResource, _cachedResource);
            NormalizeResourceRow(_cachedResource);
        }

        /// <summary>
        /// variant 편집값이 변경되어 있으면 캐시 Row에 반영합니다.
        /// </summary>
        private void CommitVariantEditingIfNeeded()
        {
            if (_editingVariant == null || _cachedVariant == null)
                return;

            TableRowEditorUtility.CopyMembers(_editingVariant, _cachedVariant, VariantRowEditorFields);
            NormalizeVariantRow(_cachedVariant);
        }

        /// <summary>
        /// 편집 중인 sound 필드 값을 정규화합니다.
        /// </summary>
        /// <param name="target">편집 중인 Row입니다.</param>
        /// <param name="memberName">변경된 멤버 이름입니다.</param>
        private void NormalizeSoundFieldValue(object target, string memberName)
        {
            NormalizeSoundRow(target as StruckTableSound);
        }

        /// <summary>
        /// 편집 중인 리소스 필드 값을 정규화합니다.
        /// </summary>
        /// <param name="target">편집 중인 리소스 Row입니다.</param>
        /// <param name="memberName">변경된 멤버 이름입니다.</param>
        private void NormalizeResourceFieldValue(object target, string memberName)
        {
            StruckTableSoundResource row = target as StruckTableSoundResource;
            if (row != null && memberName == nameof(StruckTableSoundResource.FadeDuration))
                row.SetFadeDurationOverride(true);

            NormalizeResourceRow(row);
        }

        /// <summary>
        /// 편집 중인 variant 필드 값을 정규화합니다.
        /// </summary>
        /// <param name="target">편집 중인 variant Row입니다.</param>
        /// <param name="memberName">변경된 멤버 이름입니다.</param>
        private void NormalizeVariantFieldValue(object target, string memberName)
        {
            NormalizeVariantRow(target as StruckTableSoundVariant);
        }

        /// <summary>
        /// sound Row의 범위 값을 안전한 값으로 보정합니다.
        /// </summary>
        /// <param name="row">보정할 sound Row입니다.</param>
        private void NormalizeSoundRow(StruckTableSound row)
        {
            if (row == null)
                return;

            row.VolumeScale = Mathf.Max(0f, row.VolumeScale);
            row.NoRepeatRecentCount = Mathf.Max(0, row.NoRepeatRecentCount);
            row.FallbackResourceUid = Mathf.Max(0, row.FallbackResourceUid);
        }

        /// <summary>
        /// 실제 리소스 Row의 범위 값과 대표 sound 연결값을 안전하게 보정합니다.
        /// </summary>
        /// <param name="row">보정할 리소스 Row입니다.</param>
        private void NormalizeResourceRow(StruckTableSoundResource row)
        {
            if (row == null)
                return;

            if (_editingSound != null)
            {
                row.SoundUid = _editingSound.Uid;
                row.Type = _editingSound.Type;
            }

            row.MaxPlayCount = Mathf.Max(0, row.MaxPlayCount);
            row.Volume = Mathf.Max(0f, row.Volume);
            row.PitchMin = row.PitchMin <= 0f ? 1f : row.PitchMin;
            row.PitchMax = row.PitchMax <= 0f ? row.PitchMin : row.PitchMax;
            row.FadeDuration = Mathf.Max(0f, row.FadeDuration);
        }

        /// <summary>
        /// variant Row의 범위 값과 대표 sound 연결값을 안전하게 보정합니다.
        /// </summary>
        /// <param name="row">보정할 variant Row입니다.</param>
        private void NormalizeVariantRow(StruckTableSoundVariant row)
        {
            if (row == null)
                return;

            if (_editingSound != null)
                row.SoundUid = _editingSound.Uid;

            row.CandidateResourceUid = Mathf.Max(0, row.CandidateResourceUid);
            row.Weight = Mathf.Max(0, row.Weight);
            row.VolumeScale = Mathf.Max(0f, row.VolumeScale);
            row.PitchMinOverride = Mathf.Max(0f, row.PitchMinOverride);
            row.PitchMaxOverride = Mathf.Max(0f, row.PitchMaxOverride);
        }

        /// <summary>
        /// 현재 편집 캐시를 Play Mode의 런타임 테이블에 반영합니다.
        /// </summary>
        private void ApplyEditingToRuntime()
        {
            CommitSoundEditingIfNeeded();
            CommitResourceEditingIfNeeded();
            CommitVariantEditingIfNeeded();

            GGemCo2DCore.TableLoaderManager runtimeTableLoader = GGemCo2DCore.TableLoaderManager.Instance;
            if (runtimeTableLoader == null)
            {
                EditorUtility.DisplayDialog(Title, "Runtime TableLoaderManager를 찾지 못했습니다.", "OK");
                return;
            }

            ApplySoundToRuntime(runtimeTableLoader);
            ApplyResourceToRuntime(runtimeTableLoader);
            ApplyVariantToRuntime(runtimeTableLoader);
            _lastClipLoadMessage = "런타임 Row 적용 완료";
            Repaint();
        }

        /// <summary>
        /// 대표 sound 캐시를 런타임 sound 테이블에 반영합니다.
        /// </summary>
        /// <param name="runtimeTableLoader">런타임 테이블 로더입니다.</param>
        private void ApplySoundToRuntime(GGemCo2DCore.TableLoaderManager runtimeTableLoader)
        {
            if (_cachedSound == null || runtimeTableLoader?.TableSound == null)
                return;

            StruckTableSound runtimeRow = runtimeTableLoader.TableSound.GetDataByUid(_cachedSound.Uid);
            if (runtimeRow != null)
                TableRowEditorUtility.CopyMembers(_cachedSound, runtimeRow, SoundRowEditorFields);
        }

        /// <summary>
        /// 실제 리소스 캐시를 런타임 리소스 테이블에 반영하고 필요한 컨트롤러 캐시를 갱신합니다.
        /// </summary>
        /// <param name="runtimeTableLoader">런타임 테이블 로더입니다.</param>
        private void ApplyResourceToRuntime(GGemCo2DCore.TableLoaderManager runtimeTableLoader)
        {
            if (_cachedResource == null || runtimeTableLoader == null)
                return;

            StruckTableSoundResource runtimeResource = FindRuntimeResourceByUid(runtimeTableLoader, _cachedResource.Type, _cachedResource.Uid);
            if (runtimeResource == null)
                return;

            CopyResourceMembers(_cachedResource, runtimeResource);

            if (SceneGame.Instance == null || SceneGame.Instance.soundManager == null)
                return;

            if (_cachedResource.Type == SoundConstants.Type.Sfx)
                SceneGame.Instance.soundManager.ReinitializeSoundSfxPool();
            else if (_cachedResource.Type == SoundConstants.Type.Ambient)
                SceneGame.Instance.soundManager.StopAmbientByResourceUid(_cachedResource.Uid);
        }

        /// <summary>
        /// variant 캐시를 런타임 sound_variant 테이블에 반영합니다.
        /// </summary>
        /// <param name="runtimeTableLoader">런타임 테이블 로더입니다.</param>
        private void ApplyVariantToRuntime(GGemCo2DCore.TableLoaderManager runtimeTableLoader)
        {
            if (_cachedVariant == null || runtimeTableLoader?.TableSoundVariant == null)
                return;

            StruckTableSoundVariant runtimeRow = runtimeTableLoader.TableSoundVariant.GetDataByUid(_cachedVariant.Uid);
            if (runtimeRow != null)
                TableRowEditorUtility.CopyMembers(_cachedVariant, runtimeRow, VariantRowEditorFields);
        }

        /// <summary>
        /// 선택된 대표 sound UID를 실제 SoundManager 경로로 재생합니다.
        /// </summary>
        private void PlaySelectedSound()
        {
            if (!CanUseRuntimeSoundManager() || _cachedSound == null)
                return;

            CommitSoundEditingIfNeeded();
            ApplyEditingToRuntime();
            SceneGame.Instance.soundManager.PlayByUid(_cachedSound.Uid);
        }

        /// <summary>
        /// 선택된 대표 sound UID를 지정 횟수/간격으로 반복 재생합니다.
        /// </summary>
        private void PlaySelectedSoundRepeated()
        {
            if (!CanUseRuntimeSoundManager() || _cachedSound == null)
                return;

            CommitSoundEditingIfNeeded();
            ApplyEditingToRuntime();
            SceneGame.Instance.StartCoroutine(PlayRepeatedRoutine(_cachedSound.Uid, _repeatPlayCount, _repeatPlayDelay));
        }

        /// <summary>
        /// SoundManager.PlayByUid를 여러 번 호출하는 반복 재생 코루틴입니다.
        /// </summary>
        /// <param name="soundUid">반복 재생할 대표 sound UID입니다.</param>
        /// <param name="count">반복 횟수입니다.</param>
        /// <param name="delay">각 재생 사이의 지연 시간입니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator PlayRepeatedRoutine(int soundUid, int count, float delay)
        {
            for (int i = 0; i < count; i++)
            {
                if (SceneGame.Instance == null || SceneGame.Instance.soundManager == null)
                    yield break;

                SceneGame.Instance.soundManager.PlayByUid(soundUid);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }

        /// <summary>
        /// 선택된 리소스의 Addressables AudioClip 로드를 검증하고 클립 메타 정보를 표시합니다.
        /// </summary>
        private async void LoadSelectedAudioClipAsync()
        {
            if (!CanLoadSelectedClip())
                return;

            CommitResourceEditingIfNeeded();
            string key = _cachedResource.BuildAddressKey();
            _lastClipLoadMessage = $"AudioClip 로드 중... key={key}";
            Repaint();

            try
            {
                AudioClip clip = await AddressableLoaderSound.Instance.LoadAudioClipAsync(key);
                _lastClipLoadMessage = clip != null
                    ? $"AudioClip 로드 성공: key={key}, length={clip.length:0.###}, channels={clip.channels}, frequency={clip.frequency}"
                    : $"AudioClip 로드 실패: key={key}";
            }
            catch (Exception e)
            {
                _lastClipLoadMessage = $"AudioClip 로드 예외: {e.GetType().Name} - {e.Message}";
            }

            Repaint();
        }

        /// <summary>
        /// 런타임 SoundResolver를 여러 번 호출해 선택 분포를 확인합니다.
        /// </summary>
        /// <param name="count">해석 반복 횟수입니다.</param>
        private void SimulateResolveDistribution(int count)
        {
            if (!CanUseRuntimeSoundManager() || _cachedSound == null)
                return;

            Dictionary<int, int> counts = new Dictionary<int, int>();
            int silentCount = 0;
            int failedCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (!SceneGame.Instance.soundManager.TryResolveSound(_cachedSound.Uid, out ResolvedSound resolved))
                {
                    failedCount++;
                    continue;
                }

                if (!resolved.ShouldPlay)
                {
                    silentCount++;
                    continue;
                }

                counts.TryGetValue(resolved.ResourceUid, out int current);
                counts[resolved.ResourceUid] = current + 1;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Resolve {count}회 결과");
            foreach (KeyValuePair<int, int> pair in counts)
                sb.AppendLine($"- ResourceUid {pair.Key}: {pair.Value}");
            if (silentCount > 0)
                sb.AppendLine($"- Silent: {silentCount}");
            if (failedCount > 0)
                sb.AppendLine($"- Failed: {failedCount}");

            _lastSimulationMessage = sb.ToString().TrimEnd();
        }

        /// <summary>
        /// sound 테이블 파일에 현재 캐시 Row를 저장합니다.
        /// </summary>
        private void SaveSoundTableFile()
        {
            CommitSoundEditingIfNeeded();
            if (!TableTextRowPatchUtility.TryPatchRowByUid(ConfigAddressableTable.TableSound.Path, _cachedSound.Uid, _cachedSound, SerializeSoundRow, out string error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            ReloadAllTables(preserveSelection: true);
        }

        /// <summary>
        /// 실제 리소스 테이블 파일에 현재 캐시 Row를 저장합니다.
        /// </summary>
        private void SaveResourceTableFile()
        {
            CommitResourceEditingIfNeeded();
            string path = GetResourceTablePath(_cachedResource.Type);
            if (!TableTextRowPatchUtility.TryPatchRowByUid(path, _cachedResource.Uid, _cachedResource, SerializeResourceRow, out string error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            ReloadAllTables(preserveSelection: true);
        }

        /// <summary>
        /// sound_variant 테이블 파일에 현재 캐시 Row를 저장합니다.
        /// </summary>
        private void SaveVariantTableFile()
        {
            CommitVariantEditingIfNeeded();
            if (!TableTextRowPatchUtility.TryPatchRowByUid(ConfigAddressableTable.TableSoundVariant.Path, _cachedVariant.Uid, _cachedVariant, SerializeVariantRow, out string error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            ReloadAllTables(preserveSelection: true);
        }

        /// <summary>
        /// 대표 sound Row를 현재 테이블 헤더 순서에 맞춰 직렬화합니다.
        /// </summary>
        /// <param name="row">저장할 sound Row입니다.</param>
        /// <param name="headers">테이블 헤더 목록입니다.</param>
        /// <returns>탭 구분 저장 문자열입니다.</returns>
        private static string SerializeSoundRow(StruckTableSound row, IReadOnlyList<string> headers)
        {
            string[] values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "Type" => row.Type.ToString(),
                    "SoundType" => row.Type.ToString(),
                    "SubType" => row.SubType.ToString(),
                    "ResolveMode" => row.ResolveMode.ToString(),
                    "SelectionMode" => row.SelectionMode.ToString(),
                    "VolumeScale" => MathHelper.FormatFloat(row.VolumeScale),
                    "NoRepeatRecentCount" => row.NoRepeatRecentCount.ToString(),
                    "FallbackResourceUid" => row.FallbackResourceUid.ToString(),
                    "UseIntroScene" => MathHelper.FormatBool(row.UseIntroScene),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        /// <summary>
        /// 실제 리소스 Row를 현재 테이블 헤더 순서에 맞춰 직렬화합니다.
        /// </summary>
        /// <param name="row">저장할 리소스 Row입니다.</param>
        /// <param name="headers">테이블 헤더 목록입니다.</param>
        /// <returns>탭 구분 저장 문자열입니다.</returns>
        private static string SerializeResourceRow(StruckTableSoundResource row, IReadOnlyList<string> headers)
        {
            string[] values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "SoundUid" => row.SoundUid.ToString(),
                    "Type" => row.Type.ToString(),
                    "SubType" => row.SubType.ToString(),
                    "FileName" => row.FileName ?? string.Empty,
                    "MaxPlayCount" => row.MaxPlayCount.ToString(),
                    "Volume" => MathHelper.FormatFloat(row.Volume),
                    "PitchMin" => MathHelper.FormatFloat(row.PitchMin),
                    "PitchMax" => MathHelper.FormatFloat(row.PitchMax),
                    "Loop" => MathHelper.FormatBool(row.Loop),
                    "FadeDuration" => row.HasFadeDurationOverride()
                        ? MathHelper.FormatFloat(row.FadeDuration)
                        : string.Empty,
                    "UseIntroScene" => MathHelper.FormatBool(row.UseIntroScene),
                    "PreLoad" => MathHelper.FormatBool(row.PreLoad),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        /// <summary>
        /// variant Row를 현재 테이블 헤더 순서에 맞춰 직렬화합니다.
        /// </summary>
        /// <param name="row">저장할 variant Row입니다.</param>
        /// <param name="headers">테이블 헤더 목록입니다.</param>
        /// <returns>탭 구분 저장 문자열입니다.</returns>
        private static string SerializeVariantRow(StruckTableSoundVariant row, IReadOnlyList<string> headers)
        {
            string[] values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                values[i] = headers[i] switch
                {
                    "Uid" => row.Uid.ToString(),
                    "Name" => row.Name ?? string.Empty,
                    "SoundUid" => row.SoundUid.ToString(),
                    "CandidateResourceUid" => row.CandidateResourceUid.ToString(),
                    "CandidateUid" => row.CandidateResourceUid.ToString(),
                    "Weight" => row.Weight.ToString(),
                    "VolumeScale" => MathHelper.FormatFloat(row.VolumeScale),
                    "PitchMinOverride" => MathHelper.FormatFloat(row.PitchMinOverride),
                    "PitchMaxOverride" => MathHelper.FormatFloat(row.PitchMaxOverride),
                    "Enabled" => MathHelper.FormatBool(row.Enabled),
                    _ => string.Empty,
                };
            }

            return string.Join("\t", values);
        }

        /// <summary>
        /// 정적 Row 기준 미리보기 문자열을 구성합니다.
        /// </summary>
        /// <param name="sb">문자열 빌더입니다.</param>
        private void AppendStaticPreview(StringBuilder sb)
        {
            if (_editingSound == null)
            {
                sb.AppendLine("sound Row를 선택하세요.");
                return;
            }

            sb.AppendLine($"[Sound] {_editingSound.Uid} - {_editingSound.Name}");
            sb.AppendLine($"- Type/SubType: {_editingSound.Type}/{_editingSound.SubType}");
            sb.AppendLine($"- ResolveMode/SelectionMode: {_editingSound.ResolveMode}/{_editingSound.SelectionMode}");
            sb.AppendLine($"- VolumeScale: {_editingSound.VolumeScale}");
            sb.AppendLine($"- NoRepeatRecentCount: {_editingSound.NoRepeatRecentCount}");
            sb.AppendLine($"- FallbackResourceUid: {_editingSound.FallbackResourceUid}");
            sb.AppendLine($"- UseIntroScene: {_editingSound.UseIntroScene}");

            sb.AppendLine();
            if (_editingResource != null)
            {
                sb.AppendLine($"[Direct Resource] {_editingResource.Uid} - {_editingResource.Name}");
                sb.AppendLine($"- Table: {GetResourceTableKey(_editingResource.Type)}");
                sb.AppendLine($"- FileName: {_editingResource.FileName}");
                sb.AppendLine($"- Addressables Key: {_editingResource.BuildAddressKey()}");
                sb.AppendLine($"- Volume/Pitch: {_editingResource.Volume} / {_editingResource.PitchMin}~{_editingResource.PitchMax}");
                sb.AppendLine($"- Loop/Fade: {_editingResource.Loop} / {_editingResource.FadeDuration}");
                sb.AppendLine($"- UseIntroScene/PreLoad: {_editingResource.UseIntroScene} / {_editingResource.PreLoad}");
            }
            else
            {
                sb.AppendLine("[Direct Resource] 연결된 리소스 Row 없음");
            }
        }

        /// <summary>
        /// 런타임 SoundManager 해석 결과를 미리보기 문자열에 추가합니다.
        /// </summary>
        /// <param name="sb">문자열 빌더입니다.</param>
        private void AppendRuntimeResolvePreview(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("[Runtime Resolve]");
            if (!CanUseRuntimeSoundManager() || _editingSound == null)
            {
                sb.AppendLine("- Play Mode의 Game 씬에서 확인할 수 있습니다.");
                return;
            }

            if (!SceneGame.Instance.soundManager.TryResolveSound(_editingSound.Uid, out ResolvedSound resolved))
            {
                sb.AppendLine("- Resolve 실패");
                return;
            }

            sb.AppendLine($"- ShouldPlay: {resolved.ShouldPlay}");
            sb.AppendLine($"- RequestedSoundUid: {resolved.RequestedSoundUid}");
            sb.AppendLine($"- ResourceUid: {resolved.ResourceUid}");
            sb.AppendLine($"- Type: {resolved.Type}");
            sb.AppendLine($"- FileName: {resolved.FileName}");
            sb.AppendLine($"- Volume/Pitch: {resolved.Volume:0.###} / {resolved.Pitch:0.###}");
            sb.AppendLine($"- Loop/Fade: {resolved.Loop} / {resolved.FadeDuration:0.###}");
        }

        /// <summary>
        /// 현재 선택된 sound 기준의 데이터 문제를 수집합니다.
        /// </summary>
        /// <param name="target">수집 결과를 담을 목록입니다.</param>
        private void CollectValidationMessages(List<ValidationMessage> target)
        {
            if (target == null || _editingSound == null)
                return;

            if (_editingSound.Type == SoundConstants.Type.None)
                target.Add(new ValidationMessage(MessageType.Warning, "Type이 None입니다. 실제 재생 대상으로 사용할 수 없습니다."));

            if (_editingSound.VolumeScale < 0f)
                target.Add(new ValidationMessage(MessageType.Error, "VolumeScale은 0 이상이어야 합니다."));

            StruckTableSoundResource direct = FindFirstResourceBySound(_editingSound);
            IReadOnlyList<StruckTableSoundVariant> variants = GetVariantsForSelectedSound();

            if (_editingSound.ResolveMode == SoundConstants.ResolveMode.Direct && direct == null)
                target.Add(new ValidationMessage(MessageType.Error, "Direct 모드이지만 연결된 실제 리소스 Row가 없습니다."));

            if (_editingSound.ResolveMode == SoundConstants.ResolveMode.Variant)
            {
                int enabledCount = 0;
                int totalWeight = 0;
                if (variants != null)
                {
                    for (int i = 0; i < variants.Count; i++)
                    {
                        StruckTableSoundVariant variant = variants[i];
                        if (variant == null || !variant.Enabled)
                            continue;

                        enabledCount++;
                        totalWeight += Mathf.Max(0, variant.Weight);
                        if (variant.CandidateResourceUid > 0 && FindResourceByUid(_editingSound.Type, variant.CandidateResourceUid) == null)
                            target.Add(new ValidationMessage(MessageType.Error, $"variant Uid={variant.Uid} 후보 리소스를 찾지 못했습니다. CandidateResourceUid={variant.CandidateResourceUid}"));
                    }
                }

                if (enabledCount <= 0 && _editingSound.FallbackResourceUid <= 0 && direct == null)
                    target.Add(new ValidationMessage(MessageType.Error, "Variant 모드이지만 활성 후보, Fallback, Direct 대체 리소스가 모두 없습니다."));

                if (enabledCount > 0 && totalWeight <= 0 && _editingSound.SelectionMode == SoundConstants.SelectionMode.WeightedRandom)
                    target.Add(new ValidationMessage(MessageType.Warning, "WeightedRandom이지만 활성 후보의 Weight 합계가 0입니다. 동일 확률에 가깝게 동작합니다."));
            }

            if (_editingResource != null)
                ValidateResource(_editingResource, target);
        }

        /// <summary>
        /// 실제 리소스 Row의 기본 범위와 파일명을 검증합니다.
        /// </summary>
        /// <param name="resource">검증할 리소스 Row입니다.</param>
        /// <param name="target">수집 결과를 담을 목록입니다.</param>
        private static void ValidateResource(StruckTableSoundResource resource, List<ValidationMessage> target)
        {
            if (resource == null || target == null)
                return;

            if (string.IsNullOrWhiteSpace(resource.FileName))
                target.Add(new ValidationMessage(MessageType.Error, $"리소스 Uid={resource.Uid}의 FileName이 비어 있습니다."));

            if (resource.Volume < 0f)
                target.Add(new ValidationMessage(MessageType.Error, $"리소스 Uid={resource.Uid}의 Volume은 0 이상이어야 합니다."));

            if (resource.PitchMin <= 0f || resource.PitchMax <= 0f)
                target.Add(new ValidationMessage(MessageType.Warning, $"리소스 Uid={resource.Uid}의 Pitch 값은 0보다 큰 값이 권장됩니다."));

            if (resource.PitchMax < resource.PitchMin)
                target.Add(new ValidationMessage(MessageType.Warning, $"리소스 Uid={resource.Uid}의 PitchMax가 PitchMin보다 작습니다. Resolver에서는 자동 보정됩니다."));
        }

        /// <summary>
        /// sound Row를 얕은 복사합니다.
        /// </summary>
        /// <param name="row">원본 Row입니다.</param>
        /// <returns>복사된 Row입니다.</returns>
        private static StruckTableSound CloneSound(StruckTableSound row)
        {
            return TableRowEditorUtility.CloneShallow<StruckTableSound>(row);
        }

        /// <summary>
        /// 리소스 Row 타입을 보존하면서 얕은 복사합니다.
        /// </summary>
        /// <param name="row">원본 Row입니다.</param>
        /// <returns>복사된 리소스 Row입니다.</returns>
        private static StruckTableSoundResource CloneResource(StruckTableSoundResource row)
        {
            if (row == null)
                return null;

            StruckTableSoundResource clone = row.Type switch
            {
                SoundConstants.Type.Bgm => new StruckTableSoundBgm(),
                SoundConstants.Type.Ambient => new StruckTableSoundAmbient(),
                SoundConstants.Type.Sfx => new StruckTableSoundSfx(),
                _ => null,
            };

            if (clone == null)
                return null;

            CopyResourceMembers(row, clone);
            return clone;
        }

        /// <summary>
        /// variant Row를 얕은 복사합니다.
        /// </summary>
        /// <param name="row">원본 Row입니다.</param>
        /// <returns>복사된 Row입니다.</returns>
        private static StruckTableSoundVariant CloneVariant(StruckTableSoundVariant row)
        {
            return TableRowEditorUtility.CloneShallow<StruckTableSoundVariant>(row);
        }

        /// <summary>
        /// 사운드 리소스 공통 멤버를 복사합니다.
        /// </summary>
        /// <param name="source">원본 리소스 Row입니다.</param>
        /// <param name="destination">대상 리소스 Row입니다.</param>
        private static void CopyResourceMembers(StruckTableSoundResource source, StruckTableSoundResource destination)
        {
            if (source == null || destination == null)
                return;

            destination.Uid = source.Uid;
            destination.Name = source.Name;
            destination.SoundUid = source.SoundUid;
            destination.Type = source.Type;
            destination.SubType = source.SubType;
            destination.FileName = source.FileName;
            destination.MaxPlayCount = source.MaxPlayCount;
            destination.Volume = source.Volume;
            destination.PitchMin = source.PitchMin;
            destination.PitchMax = source.PitchMax;
            destination.Loop = source.Loop;
            destination.FadeDuration = source.FadeDuration;
            destination.SetFadeDurationOverride(source.HasFadeDurationOverride());
            destination.UseIntroScene = source.UseIntroScene;
            destination.PreLoad = source.PreLoad;
        }

        /// <summary>
        /// sound UID로 에디터 테이블 Row를 찾습니다.
        /// </summary>
        /// <param name="uid">대표 sound UID입니다.</param>
        /// <returns>찾은 Row입니다.</returns>
        private StruckTableSound FindSoundByUid(int uid)
        {
            return uid > 0 && _tableSound != null ? _tableSound.GetDataByUid(uid) : null;
        }

        /// <summary>
        /// 드롭다운의 첫 번째 sound Row를 반환합니다.
        /// </summary>
        /// <returns>첫 번째 Row입니다.</returns>
        private StruckTableSound GetFirstSoundFromDropdown()
        {
            return _soundDropdownOptions.Count > 0 ? _soundDropdownOptions[0].Data : null;
        }

        /// <summary>
        /// sound 드롭다운 표시 문자열을 만듭니다.
        /// </summary>
        /// <param name="row">표시할 sound Row입니다.</param>
        /// <returns>드롭다운 텍스트입니다.</returns>
        private static string BuildSoundDropdownValue(StruckTableSound row)
        {
            return row == null ? string.Empty : $"[{row.Type}/{row.ResolveMode}] {row.Uid} - {row.Name}";
        }

        /// <summary>
        /// variant 드롭다운 표시 문자열을 만듭니다.
        /// </summary>
        /// <param name="row">표시할 variant Row입니다.</param>
        /// <returns>드롭다운 텍스트입니다.</returns>
        private static string BuildVariantDropdownValue(StruckTableSoundVariant row)
        {
            return row == null ? string.Empty : $"{row.Uid} - {row.Name} / Candidate={row.CandidateResourceUid} / Weight={row.Weight}";
        }

        /// <summary>
        /// 선택된 sound 타입을 반환합니다.
        /// </summary>
        /// <returns>현재 sound 타입입니다.</returns>
        private SoundConstants.Type GetCurrentSoundType()
        {
            return (_editingSound ?? _selectedSound)?.Type ?? SoundConstants.Type.None;
        }

        /// <summary>
        /// 대표 sound Row에 직접 연결된 첫 번째 리소스 Row를 찾습니다.
        /// </summary>
        /// <param name="sound">대표 sound Row입니다.</param>
        /// <returns>연결된 리소스 Row입니다.</returns>
        private StruckTableSoundResource FindFirstResourceBySound(StruckTableSound sound)
        {
            if (sound == null)
                return null;

            return sound.Type switch
            {
                SoundConstants.Type.Bgm => _tableSoundBgm?.GetFirstBySoundUid(sound.Uid),
                SoundConstants.Type.Ambient => _tableSoundAmbient?.GetFirstBySoundUid(sound.Uid),
                SoundConstants.Type.Sfx => _tableSoundSfx?.GetFirstBySoundUid(sound.Uid),
                _ => null,
            };
        }

        /// <summary>
        /// 사운드 타입과 실제 리소스 UID로 에디터 테이블 Row를 찾습니다.
        /// </summary>
        /// <param name="type">사운드 타입입니다.</param>
        /// <param name="uid">실제 리소스 UID입니다.</param>
        /// <returns>찾은 리소스 Row입니다.</returns>
        private StruckTableSoundResource FindResourceByUid(SoundConstants.Type type, int uid)
        {
            if (uid <= 0)
                return null;

            return type switch
            {
                SoundConstants.Type.Bgm => TryGetResource(_tableSoundBgm, uid),
                SoundConstants.Type.Ambient => TryGetResource(_tableSoundAmbient, uid),
                SoundConstants.Type.Sfx => TryGetResource(_tableSoundSfx, uid),
                _ => null,
            };
        }

        /// <summary>
        /// 에디터 리소스 테이블에서 로그 없이 UID 조회를 시도합니다.
        /// </summary>
        /// <typeparam name="TResource">리소스 Row 타입입니다.</typeparam>
        /// <param name="table">조회할 테이블입니다.</param>
        /// <param name="uid">조회할 리소스 UID입니다.</param>
        /// <returns>찾은 리소스 Row입니다.</returns>
        private static StruckTableSoundResource TryGetResource<TResource>(DefaultTable<TResource> table, int uid)
            where TResource : StruckTableSoundResource
        {
            if (table == null || uid <= 0)
                return null;

            return table.TryGetDataByUid(uid, out TResource row) ? row : null;
        }

        /// <summary>
        /// 런타임 테이블에서 사운드 타입과 실제 리소스 UID로 Row를 찾습니다.
        /// </summary>
        /// <param name="runtimeTableLoader">런타임 테이블 로더입니다.</param>
        /// <param name="type">사운드 타입입니다.</param>
        /// <param name="uid">실제 리소스 UID입니다.</param>
        /// <returns>찾은 리소스 Row입니다.</returns>
        private static StruckTableSoundResource FindRuntimeResourceByUid(GGemCo2DCore.TableLoaderManager runtimeTableLoader, SoundConstants.Type type, int uid)
        {
            if (runtimeTableLoader == null || uid <= 0)
                return null;

            return type switch
            {
                SoundConstants.Type.Bgm => TryGetResource(runtimeTableLoader.TableSoundBgm, uid),
                SoundConstants.Type.Ambient => TryGetResource(runtimeTableLoader.TableSoundAmbient, uid),
                SoundConstants.Type.Sfx => TryGetResource(runtimeTableLoader.TableSoundSfx, uid),
                _ => null,
            };
        }

        /// <summary>
        /// 선택된 sound에 연결된 variant 목록을 반환합니다.
        /// </summary>
        /// <returns>variant 목록입니다.</returns>
        private IReadOnlyList<StruckTableSoundVariant> GetVariantsForSelectedSound()
        {
            int uid = (_editingSound ?? _selectedSound)?.Uid ?? 0;
            return uid > 0 ? _tableSoundVariant?.GetVariants(uid) : Array.Empty<StruckTableSoundVariant>();
        }

        /// <summary>
        /// variant UID로 에디터 테이블 Row를 찾습니다.
        /// </summary>
        /// <param name="uid">variant UID입니다.</param>
        /// <returns>찾은 Row입니다.</returns>
        private StruckTableSoundVariant FindVariantByUid(int uid)
        {
            return uid > 0 ? _tableSoundVariant?.GetDataByUid(uid) : null;
        }

        /// <summary>
        /// 사운드 타입에 맞는 실제 리소스 테이블 키를 반환합니다.
        /// </summary>
        /// <param name="type">사운드 타입입니다.</param>
        /// <returns>테이블 키입니다.</returns>
        private static string GetResourceTableKey(SoundConstants.Type type)
        {
            return type switch
            {
                SoundConstants.Type.Bgm => ConfigAddressableTable.SoundBgm,
                SoundConstants.Type.Ambient => ConfigAddressableTable.SoundAmbient,
                SoundConstants.Type.Sfx => ConfigAddressableTable.SoundSfx,
                _ => "unknown",
            };
        }

        /// <summary>
        /// 사운드 타입에 맞는 실제 리소스 테이블 경로를 반환합니다.
        /// </summary>
        /// <param name="type">사운드 타입입니다.</param>
        /// <returns>테이블 에셋 경로입니다.</returns>
        private static string GetResourceTablePath(SoundConstants.Type type)
        {
            return type switch
            {
                SoundConstants.Type.Bgm => ConfigAddressableTable.TableSoundBgm.Path,
                SoundConstants.Type.Ambient => ConfigAddressableTable.TableSoundAmbient.Path,
                SoundConstants.Type.Sfx => ConfigAddressableTable.TableSoundSfx.Path,
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Runtime SoundManager를 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>사용 가능하면 true입니다.</returns>
        private static bool CanUseRuntimeSoundManager()
        {
            return Application.isPlaying && SceneGame.Instance != null && SceneGame.Instance.soundManager != null;
        }

        /// <summary>
        /// 선택된 리소스 AudioClip을 Addressables에서 로드할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>로드 검증 가능하면 true입니다.</returns>
        private bool CanLoadSelectedClip()
        {
            return Application.isPlaying
                   && AddressableLoaderSound.Instance != null
                   && _cachedResource != null
                   && !string.IsNullOrWhiteSpace(_cachedResource.FileName);
        }
    }
}
