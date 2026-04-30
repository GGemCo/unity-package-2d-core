using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 그래프를 편집하고 JSON으로 export하는 메인 EditorWindow입니다.
    /// </summary>
    public sealed class WorldMapEditorWindow : EditorWindow
    {
        private const string Title = "월드맵 그래프 에디터";
        private const float LeftPanelWidth = 320f;
        private const float RightPanelWidth = 340f;

        private readonly WorldMapSelectionState _selectionState = new WorldMapSelectionState();
        private readonly WorldMapTableMapOptionProvider _mapOptions = new WorldMapTableMapOptionProvider();
        private readonly WorldMapCanvasView _canvasView = new WorldMapCanvasView();
        private readonly WorldMapInspectorPanel _inspectorPanel = new WorldMapInspectorPanel();

        private WorldMapGraphAsset _asset;
        private WorldMapValidationReport _lastReport;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private int _selectedAddMapUid;
        
        // -------------------------
        // EditorPrefs Keys
        // -------------------------
        /// <summary>이 툴에서 사용하는 EditorPrefs 키 접두사입니다.</summary>
        private const string PrefKeyPrefix = "GGemCo.WorldMapEditorWindow.";

        private const string KeyAssetName     = PrefKeyPrefix + "AssetName ";

        /// <summary>
        /// 월드맵 그래프 에디터 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditor.NameToolWorldMapGraph, false, (int)ConfigEditor.ToolOrdering.WorldMapGraph)]
        public static void ShowWindow()
        {
            GetWindow<WorldMapEditorWindow>(Title);
        }

        /// <summary>
        /// 창이 활성화될 때 TableMap 옵션과 초기 선택 에셋을 준비합니다.
        /// </summary>
        private void OnEnable()
        {
            _mapOptions.Reload();
            _asset = Selection.activeObject as WorldMapGraphAsset;
            
            LoadPrefs();
            EnsureSelectedAddMapUid();
            RunValidation();
        }

        /// <summary>
        /// 월드맵 그래프 에디터 전체 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            if (_asset == null)
            {
                DrawEmptyState();
                return;
            }

            _asset.EnsureDefaults();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftPanel();
                DrawCanvasPanel();
                DrawRightPanel();
            }
        }

        /// <summary>
        /// 상단 툴바를 그리고 주요 명령을 처리합니다.
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                WorldMapGraphAsset selectedAsset = (WorldMapGraphAsset)EditorGUILayout.ObjectField(
                    _asset,
                    typeof(WorldMapGraphAsset),
                    false,
                    GUILayout.Width(240f));

                if (selectedAsset != _asset)
                {
                    _asset = selectedAsset;
                    _selectionState.ClearSelection();
                    RunValidation();
                    // UI 변경 시 즉시 저장(선택 사항이지만, “입력/선택 저장” 만족을 위해 안전하게 적용)
                    if (GUI.changed)
                        SavePrefs();
                }

                if (GUILayout.Button("새 GraphAsset", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                {
                    CreateGraphAsset();
                }

                if (GUILayout.Button("테이블 새로고침", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    _mapOptions.Reload();
                    EnsureSelectedAddMapUid();
                    RunValidation();
                }

                using (new EditorGUI.DisabledScope(_asset == null))
                {
                    if (GUILayout.Button("검증", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                    {
                        RunValidation();
                    }

                    if (GUILayout.Button("JSON Export", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                    {
                        ExportSelectedGraph();
                    }

                    if (GUILayout.Button("JSON Import", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                    {
                        ImportJsonIntoSelectedGraph();
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("보기 초기화", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                {
                    _selectionState.ResetView();
                    Repaint();
                }
            }
        }

        /// <summary>
        /// 에셋이 선택되지 않은 상태의 안내 UI를 그립니다.
        /// </summary>
        private void DrawEmptyState()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(
                "월드맵 GraphAsset을 선택하거나 새로 생성해주세요.\n" +
                "이 에셋이 편집 원본이며, 저장 시 런타임용 JSON으로 export됩니다.",
                MessageType.Info);

            if (GUILayout.Button("새 WorldMapGraphAsset 생성", GUILayout.Height(32f)))
            {
                CreateGraphAsset();
            }
        }

        /// <summary>
        /// 좌측 그래프 설정, 노드 목록, 연결선 목록, 검증 결과 패널을 그립니다.
        /// </summary>
        private void DrawLeftPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPanelWidth), GUILayout.ExpandHeight(true)))
            {
                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                DrawGraphSettings();
                GUILayout.Space(8f);
                DrawAddNodeSection();
                GUILayout.Space(8f);
                DrawNodeList();
                GUILayout.Space(8f);
                DrawEdgeList();
                GUILayout.Space(8f);
                DrawValidationResults();
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 중앙 캔버스 패널을 그리고 노드/연결선 편집 입력을 처리합니다.
        /// </summary>
        private void DrawCanvasPanel()
        {
            Rect canvasRect = GUILayoutUtility.GetRect(
                240f,
                10000f,
                240f,
                10000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            _canvasView.Draw(
                canvasRect,
                _asset,
                _selectionState,
                _mapOptions.TableMap,
                node => _selectionState.SelectNode(node.nodeId),
                edge => _selectionState.SelectEdge(edge.edgeId),
                CreateEdge,
                OnGraphChanged);
        }

        /// <summary>
        /// 우측 선택 상세 인스펙터 패널을 그립니다.
        /// </summary>
        private void DrawRightPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(RightPanelWidth), GUILayout.ExpandHeight(true)))
            {
                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                _inspectorPanel.Draw(
                    _asset,
                    _selectionState,
                    _mapOptions,
                    OnGraphChanged,
                    DeleteSelected,
                    RenameNode,
                    SetStartNode,
                    StartLinking);
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 그래프 공통 속성 편집 UI를 그립니다.
        /// </summary>
        private void DrawGraphSettings()
        {
            EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                Sprite oldBackgroundSprite = _asset.backgroundSprite;
                string graphId = EditorGUILayout.TextField("Graph ID", _asset.graphId);
                Sprite backgroundSprite = (Sprite)EditorGUILayout.ObjectField(
                    "Background Sprite",
                    _asset.backgroundSprite,
                    typeof(Sprite),
                    false);
                string backgroundAddress = EditorGUILayout.TextField("Background Address", _asset.backgroundAddress);
                Vector2 referenceResolution = EditorGUILayout.Vector2Field("Reference Resolution", _asset.referenceResolution);
                string startNodeId = EditorGUILayout.TextField("Start Node ID", _asset.startNodeId);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_asset, "월드맵 그래프 설정 변경");
                    _asset.graphId = graphId;
                    _asset.backgroundSprite = backgroundSprite;
                    _asset.backgroundAddress = backgroundAddress;
                    if (backgroundSprite != null && backgroundSprite != oldBackgroundSprite)
                    {
                        _asset.backgroundAddress = ConfigAddressableWorldMap.GetBackgroundKey(graphId);
                    }
                    _asset.referenceResolution = referenceResolution;
                    _asset.startNodeId = startNodeId;
                    OnGraphChanged();
                }

                EditorGUILayout.LabelField("Export Path", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(ConfigAddressableWorldMap.GetAssetPath(_asset.graphId), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("Addressable Key", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(ConfigAddressableWorldMap.GetKey(_asset.graphId), EditorStyles.wordWrappedMiniLabel);
            }
        }

        /// <summary>
        /// TableMap 검색 후 노드를 추가하는 UI를 그립니다.
        /// </summary>
        private void DrawAddNodeSection()
        {
            EditorGUILayout.LabelField("Add Node", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int selectedIndex = _mapOptions.FindIndexByUid(_selectedAddMapUid);
                SearchableDropdownUtility.DrawLabeledFieldAndShow(
                    "Map",
                    _mapOptions.Options,
                    selectedIndex,
                    (_, option) => _selectedAddMapUid = option.Data,
                    noneText: "(맵 선택)",
                    disabled: _mapOptions.Options.Count == 0);

                using (new EditorGUI.DisabledScope(_selectedAddMapUid <= 0))
                {
                    if (GUILayout.Button("선택 맵 노드 추가", GUILayout.Height(28f)))
                    {
                        AddNode(_selectedAddMapUid);
                    }
                }
            }
        }

        /// <summary>
        /// 현재 그래프의 노드 목록 UI를 그립니다.
        /// </summary>
        private void DrawNodeList()
        {
            EditorGUILayout.LabelField("Nodes", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_asset.nodes.Count == 0)
                {
                    EditorGUILayout.HelpBox("노드가 없습니다.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < _asset.nodes.Count; i++)
                {
                    WorldMapNodeData node = _asset.nodes[i];
                    if (node == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool selected = node.nodeId == _selectionState.SelectedNodeId;
                        if (GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(18f)) != selected)
                        {
                            _selectionState.SelectNode(node.nodeId);
                            Repaint();
                        }

                        string label = node.nodeId + " (" + _mapOptions.GetDisplayName(node.mapUid) + ")";
                        if (GUILayout.Button(label, EditorStyles.label))
                        {
                            _selectionState.SelectNode(node.nodeId);
                            Repaint();
                        }

                        if (GUILayout.Button("연결", GUILayout.Width(44f)))
                        {
                            StartLinking(node.nodeId);
                        }

                        if (GUILayout.Button("X", GUILayout.Width(24f)))
                        {
                            _selectionState.SelectNode(node.nodeId);
                            DeleteSelected();
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 현재 그래프의 연결선 목록 UI를 그립니다.
        /// </summary>
        private void DrawEdgeList()
        {
            EditorGUILayout.LabelField("Edges", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_asset.edges.Count == 0)
                {
                    EditorGUILayout.HelpBox("연결선이 없습니다.", MessageType.Info);
                    return;
                }

                for (int i = 0; i < _asset.edges.Count; i++)
                {
                    WorldMapEdgeData edge = _asset.edges[i];
                    if (edge == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool selected = edge.edgeId == _selectionState.SelectedEdgeId;
                        if (GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(18f)) != selected)
                        {
                            _selectionState.SelectEdge(edge.edgeId);
                            Repaint();
                        }

                        string label = edge.fromNodeId + (edge.bidirectional ? " <-> " : " -> ") + edge.toNodeId;
                        if (GUILayout.Button(label, EditorStyles.label))
                        {
                            _selectionState.SelectEdge(edge.edgeId);
                            Repaint();
                        }

                        if (GUILayout.Button("X", GUILayout.Width(24f)))
                        {
                            _selectionState.SelectEdge(edge.edgeId);
                            DeleteSelected();
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 마지막 검증 결과를 좌측 패널에 표시합니다.
        /// </summary>
        private void DrawValidationResults()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_lastReport == null)
                {
                    RunValidation();
                }

                for (int i = 0; i < _lastReport.Messages.Count; i++)
                {
                    WorldMapValidationMessage message = _lastReport.Messages[i];
                    string text = string.IsNullOrWhiteSpace(message.TargetId)
                        ? message.Message
                        : "[" + message.TargetId + "] " + message.Message;
                    EditorGUILayout.HelpBox(text, message.ToMessageType());
                }
            }
        }

        /// <summary>
        /// 새 월드맵 그래프 에셋을 프로젝트에 생성하고 선택합니다.
        /// </summary>
        private void CreateGraphAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "WorldMapGraphAsset 생성",
                "WorldMapGraph",
                "asset",
                "월드맵 그래프 에셋을 저장할 위치를 선택하세요.");

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            WorldMapGraphAsset graphAsset = CreateInstance<WorldMapGraphAsset>();
            graphAsset.EnsureDefaults();
            AssetDatabase.CreateAsset(graphAsset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _asset = graphAsset;
            Selection.activeObject = graphAsset;
            RunValidation();
        }

        /// <summary>
        /// 선택한 TableMap UID로 새 노드를 추가합니다.
        /// </summary>
        /// <param name="mapUid">추가할 노드가 참조할 TableMap UID입니다.</param>
        private void AddNode(int mapUid)
        {
            if (_asset == null || mapUid <= 0)
            {
                return;
            }

            Undo.RecordObject(_asset, "월드맵 노드 추가");
            _asset.EnsureDefaults();

            WorldMapNodeData node = new WorldMapNodeData
            {
                nodeId = _asset.CreateUniqueNodeId(mapUid),
                mapUid = mapUid,
                normalizedPosition = new Vector2(0.5f, 0.5f),
                nodeType = _asset.nodes.Count == 0 ? WorldMapNodeType.Start : WorldMapNodeType.Normal,
                visibleByDefault = true,
                inactiveByDefault = false,
            };

            _asset.nodes.Add(node);
            if (_asset.nodes.Count == 1)
            {
                _asset.startNodeId = node.nodeId;
            }

            _selectionState.SelectNode(node.nodeId);
            OnGraphChanged();
        }

        /// <summary>
        /// 선택 상태에 따라 노드 또는 연결선을 삭제합니다.
        /// </summary>
        private void DeleteSelected()
        {
            if (_asset == null)
            {
                return;
            }

            WorldMapNodeData node = _asset.FindNode(_selectionState.SelectedNodeId);
            if (node != null)
            {
                DeleteNode(node.nodeId);
                return;
            }

            WorldMapEdgeData edge = _asset.FindEdge(_selectionState.SelectedEdgeId);
            if (edge != null)
            {
                Undo.RecordObject(_asset, "월드맵 연결선 삭제");
                _asset.edges.Remove(edge);
                _selectionState.ClearSelection();
                OnGraphChanged();
            }
        }

        /// <summary>
        /// 지정한 노드와 해당 노드를 참조하는 연결선을 삭제합니다.
        /// </summary>
        /// <param name="nodeId">삭제할 노드 ID입니다.</param>
        private void DeleteNode(string nodeId)
        {
            WorldMapNodeData node = _asset.FindNode(nodeId);
            if (node == null)
            {
                return;
            }

            Undo.RecordObject(_asset, "월드맵 노드 삭제");
            _asset.nodes.Remove(node);
            _asset.edges.RemoveAll(edge => edge != null && (edge.fromNodeId == nodeId || edge.toNodeId == nodeId));

            if (_asset.startNodeId == nodeId)
            {
                _asset.startNodeId = _asset.nodes.Count > 0 ? _asset.nodes[0].nodeId : string.Empty;
            }

            _selectionState.ClearSelection();
            OnGraphChanged();
        }

        /// <summary>
        /// 지정한 노드에서 시작하는 연결 생성 모드로 전환합니다.
        /// </summary>
        /// <param name="nodeId">연결 시작 노드 ID입니다.</param>
        private void StartLinking(string nodeId)
        {
            if (_asset == null || _asset.FindNode(nodeId) == null)
            {
                return;
            }

            _selectionState.StartLinking(nodeId);
            Repaint();
        }

        /// <summary>
        /// 두 노드 사이에 새 연결선을 추가합니다.
        /// </summary>
        /// <param name="fromNodeId">출발 노드 ID입니다.</param>
        /// <param name="toNodeId">도착 노드 ID입니다.</param>
        private void CreateEdge(string fromNodeId, string toNodeId)
        {
            if (_asset == null || string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId))
            {
                return;
            }

            if (fromNodeId == toNodeId)
            {
                EditorUtility.DisplayDialog(Title, "같은 노드로는 연결할 수 없습니다.", "OK");
                return;
            }

            if (HasBlockingDuplicateEdge(fromNodeId, toNodeId))
            {
                EditorUtility.DisplayDialog(Title, "동일한 방향의 연결선이 이미 있습니다.", "OK");
                _selectionState.CancelLinking();
                return;
            }

            Undo.RecordObject(_asset, "월드맵 연결선 추가");
            WorldMapEdgeData edge = new WorldMapEdgeData
            {
                edgeId = _asset.CreateUniqueEdgeId(fromNodeId, toNodeId),
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                bidirectional = false,
                edgeType = WorldMapEdgeType.Normal,
            };

            _asset.edges.Add(edge);
            _selectionState.CancelLinking();
            _selectionState.SelectEdge(edge.edgeId);
            OnGraphChanged();
        }

        /// <summary>
        /// 새 연결선을 추가할 때 막아야 하는 중복 연결선이 있는지 확인합니다.
        /// </summary>
        /// <param name="fromNodeId">출발 노드 ID입니다.</param>
        /// <param name="toNodeId">도착 노드 ID입니다.</param>
        /// <returns>중복으로 막아야 하면 true입니다.</returns>
        private bool HasBlockingDuplicateEdge(string fromNodeId, string toNodeId)
        {
            for (int i = 0; i < _asset.edges.Count; i++)
            {
                WorldMapEdgeData edge = _asset.edges[i];
                if (edge == null)
                {
                    continue;
                }

                bool sameDirection = edge.fromNodeId == fromNodeId && edge.toNodeId == toNodeId;
                bool coveredByBidirectional =
                    edge.bidirectional &&
                    ((edge.fromNodeId == fromNodeId && edge.toNodeId == toNodeId) ||
                     (edge.fromNodeId == toNodeId && edge.toNodeId == fromNodeId));

                if (sameDirection || coveredByBidirectional)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 노드 ID를 변경하고 기존 edge/start 참조를 함께 갱신합니다.
        /// </summary>
        /// <param name="oldNodeId">기존 노드 ID입니다.</param>
        /// <param name="newNodeId">새 노드 ID입니다.</param>
        private void RenameNode(string oldNodeId, string newNodeId)
        {
            if (_asset == null || oldNodeId == newNodeId)
            {
                return;
            }

            newNodeId = newNodeId != null ? newNodeId.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(newNodeId))
            {
                EditorUtility.DisplayDialog(Title, "nodeId는 비울 수 없습니다.", "OK");
                return;
            }

            WorldMapNodeData node = _asset.FindNode(oldNodeId);
            if (node == null)
            {
                return;
            }

            if (_asset.FindNode(newNodeId) != null)
            {
                EditorUtility.DisplayDialog(Title, "이미 사용 중인 nodeId입니다.", "OK");
                return;
            }

            Undo.RecordObject(_asset, "월드맵 노드 ID 변경");
            node.nodeId = newNodeId;
            if (node.iconSprite != null)
            {
                node.iconAddress = ConfigAddressableWorldMap.GetNodeIconKey(_asset.graphId, newNodeId);
            }

            for (int i = 0; i < _asset.edges.Count; i++)
            {
                WorldMapEdgeData edge = _asset.edges[i];
                if (edge == null)
                {
                    continue;
                }

                if (edge.fromNodeId == oldNodeId)
                {
                    edge.fromNodeId = newNodeId;
                }

                if (edge.toNodeId == oldNodeId)
                {
                    edge.toNodeId = newNodeId;
                }
            }

            if (_asset.startNodeId == oldNodeId)
            {
                _asset.startNodeId = newNodeId;
            }

            _selectionState.SelectNode(newNodeId);
            OnGraphChanged();
        }

        /// <summary>
        /// 지정한 노드를 시작 노드로 설정하고 기존 Start 타입을 정리합니다.
        /// </summary>
        /// <param name="nodeId">시작 노드로 설정할 노드 ID입니다.</param>
        private void SetStartNode(string nodeId)
        {
            WorldMapNodeData selectedNode = _asset != null ? _asset.FindNode(nodeId) : null;
            if (selectedNode == null)
            {
                return;
            }

            Undo.RecordObject(_asset, "월드맵 시작 노드 설정");
            for (int i = 0; i < _asset.nodes.Count; i++)
            {
                WorldMapNodeData node = _asset.nodes[i];
                if (node != null && node.nodeType == WorldMapNodeType.Start)
                {
                    node.nodeType = WorldMapNodeType.Normal;
                }
            }

            selectedNode.nodeType = WorldMapNodeType.Start;
            _asset.startNodeId = selectedNode.nodeId;
            OnGraphChanged();
        }

        /// <summary>
        /// 현재 선택된 그래프 에셋을 JSON으로 export합니다.
        /// </summary>
        private void ExportSelectedGraph()
        {
            if (_asset == null)
            {
                return;
            }

            RunValidation();
            if (_lastReport != null && _lastReport.HasErrors)
            {
                EditorUtility.DisplayDialog(Title, "검증 오류가 있어 JSON을 저장하지 않았습니다.", "OK");
                return;
            }

            string assetPath = ConfigAddressableWorldMap.GetAssetPath(_asset.graphId);
            if (!WorldMapJsonExporter.TryExport(_asset, assetPath, out string error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            EditorUtility.DisplayDialog(Title, "월드맵 JSON 저장 완료\n" + assetPath, "OK");
        }

        /// <summary>
        /// JSON 파일을 선택해 현재 그래프 에셋에 가져옵니다.
        /// </summary>
        private void ImportJsonIntoSelectedGraph()
        {
            if (_asset == null)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(Title, "현재 GraphAsset 내용을 선택한 JSON으로 덮어씁니다. 계속할까요?", "가져오기", "취소"))
            {
                return;
            }

            string path = EditorUtility.OpenFilePanel("월드맵 JSON 선택", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!WorldMapJsonImporter.TryImportIntoAsset(_asset, path, out string error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            _selectionState.ClearSelection();
            RunValidation();
            EditorUtility.DisplayDialog(Title, "월드맵 JSON 가져오기 완료", "OK");
        }

        /// <summary>
        /// 그래프 데이터가 변경되었을 때 Dirty 처리와 검증 갱신을 수행합니다.
        /// </summary>
        private void OnGraphChanged()
        {
            if (_asset == null)
            {
                return;
            }

            _asset.EnsureDefaults();
            EditorUtility.SetDirty(_asset);
            RunValidation();
            Repaint();
        }

        /// <summary>
        /// 현재 그래프 에셋의 검증 결과를 갱신합니다.
        /// </summary>
        private void RunValidation()
        {
            _lastReport = _asset != null
                ? WorldMapValidator.Validate(_asset, _mapOptions.TableMap)
                : null;
        }

        /// <summary>
        /// 노드 추가 드롭다운의 선택값이 비어 있을 때 첫 번째 맵으로 보정합니다.
        /// </summary>
        private void EnsureSelectedAddMapUid()
        {
            if (_mapOptions.FindIndexByUid(_selectedAddMapUid) >= 0)
            {
                return;
            }

            _selectedAddMapUid = _mapOptions.Options.Count > 0 ? _mapOptions.Options[0].Data : 0;
        }
        
        // -------------------------
        // Prefs Save/Load
        // -------------------------
        /// <summary>
        /// 현재 UI 입력값(폴더, 프리팹명, FPS, 스프라이트 목록 등)을 EditorPrefs에 저장합니다.
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetString(KeyAssetName, GetFolderPath(_asset));
        }
        
        /// <summary>
        /// EditorPrefs에서 이전 입력값(폴더, 프리팹명, FPS, 스프라이트 목록 등)을 복원합니다.
        /// </summary>
        private void LoadPrefs()
        {
            var prefabFolderPath = EditorPrefs.GetString(KeyAssetName, "Assets");
            _asset = AssetDatabase.LoadAssetAtPath<WorldMapGraphAsset>(prefabFolderPath);
        }
        
        /// <summary>
        /// 폴더 에셋으로부터 유효한 프로젝트 폴더 경로를 얻습니다(유효하지 않으면 Assets로 대체).
        /// </summary>
        /// <param name="folderAsset">폴더로 사용될 DefaultAsset 입니다.</param>
        /// <returns>유효한 폴더 경로(기본값: Assets)입니다.</returns>
        private static string GetFolderPath(WorldMapGraphAsset? folderAsset)
        {
            if (folderAsset == null) return "Assets";
            var path = AssetDatabase.GetAssetPath(folderAsset);
            if (string.IsNullOrEmpty(path))
                return "Assets";
            return path;
        }
    }
}
