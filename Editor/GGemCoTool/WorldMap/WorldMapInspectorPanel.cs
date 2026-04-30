using System;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 선택된 월드맵 노드 또는 연결선의 상세 속성을 편집하는 우측 패널입니다.
    /// </summary>
    internal sealed class WorldMapInspectorPanel
    {
        /// <summary>
        /// 우측 인스펙터 패널을 그립니다.
        /// </summary>
        /// <param name="asset">편집 중인 월드맵 그래프 에셋입니다.</param>
        /// <param name="state">현재 선택 상태입니다.</param>
        /// <param name="mapOptions">TableMap 검색 옵션 제공자입니다.</param>
        /// <param name="onChanged">데이터 변경 콜백입니다.</param>
        /// <param name="onDeleteSelected">선택 항목 삭제 콜백입니다.</param>
        /// <param name="onRenameNode">노드 ID 변경 콜백입니다.</param>
        /// <param name="onSetStartNode">시작 노드 지정 콜백입니다.</param>
        /// <param name="onStartLinking">선택 노드에서 연결 생성 모드로 진입하는 콜백입니다.</param>
        public void Draw(
            WorldMapGraphAsset asset,
            WorldMapSelectionState state,
            WorldMapTableMapOptionProvider mapOptions,
            Action onChanged,
            Action onDeleteSelected,
            Action<string, string> onRenameNode,
            Action<string> onSetStartNode,
            Action<string> onStartLinking)
        {
            if (asset == null)
            {
                EditorGUILayout.HelpBox("월드맵 그래프 에셋을 선택해주세요.", MessageType.Info);
                return;
            }

            if (state.IsLinking)
            {
                EditorGUILayout.HelpBox("연결 생성 모드입니다. 캔버스에서 도착 노드를 클릭하세요.", MessageType.Info);
                if (GUILayout.Button("연결 생성 취소", GUILayout.Height(24)))
                {
                    state.CancelLinking();
                }

                GUILayout.Space(8f);
            }

            WorldMapNodeData selectedNode = asset.FindNode(state.SelectedNodeId);
            if (selectedNode != null)
            {
                DrawNodeInspector(asset, selectedNode, mapOptions, onChanged, onDeleteSelected, onRenameNode, onSetStartNode, onStartLinking);
                return;
            }

            WorldMapEdgeData selectedEdge = asset.FindEdge(state.SelectedEdgeId);
            if (selectedEdge != null)
            {
                DrawEdgeInspector(asset, selectedEdge, onChanged, onDeleteSelected);
                return;
            }

            EditorGUILayout.HelpBox("노드 또는 연결선을 선택하면 상세 속성이 표시됩니다.", MessageType.Info);
        }

        /// <summary>
        /// 선택된 노드의 상세 속성 편집 UI를 그립니다.
        /// </summary>
        /// <param name="asset">편집 중인 월드맵 그래프 에셋입니다.</param>
        /// <param name="node">선택된 노드입니다.</param>
        /// <param name="mapOptions">TableMap 검색 옵션 제공자입니다.</param>
        /// <param name="onChanged">데이터 변경 콜백입니다.</param>
        /// <param name="onDeleteSelected">선택 항목 삭제 콜백입니다.</param>
        /// <param name="onRenameNode">노드 ID 변경 콜백입니다.</param>
        /// <param name="onSetStartNode">시작 노드 지정 콜백입니다.</param>
        /// <param name="onStartLinking">선택 노드에서 연결 생성 모드로 진입하는 콜백입니다.</param>
        private static void DrawNodeInspector(
            WorldMapGraphAsset asset,
            WorldMapNodeData node,
            WorldMapTableMapOptionProvider mapOptions,
            Action onChanged,
            Action onDeleteSelected,
            Action<string, string> onRenameNode,
            Action<string> onSetStartNode,
            Action<string> onStartLinking)
        {
            EditorGUILayout.LabelField("선택 노드", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string newNodeId = EditorGUILayout.DelayedTextField("Node ID", node.nodeId);
            if (EditorGUI.EndChangeCheck())
            {
                onRenameNode?.Invoke(node.nodeId, newNodeId);
            }

            int selectedMapIndex = mapOptions.FindIndexByUid(node.mapUid);
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                "Map UID",
                mapOptions.Options,
                selectedMapIndex,
                (_, option) =>
                {
                    Undo.RecordObject(asset, "월드맵 노드 맵 변경");
                    node.mapUid = option.Data;
                    EditorUtility.SetDirty(asset);
                    onChanged?.Invoke();
                },
                noneText: "(맵 선택)",
                disabled: mapOptions.Options.Count == 0);

            EditorGUI.BeginChangeCheck();
            Sprite oldIconSprite = node.iconSprite;
            string titleOverride = EditorGUILayout.TextField("Title Override", node.titleOverride);
            Sprite iconSprite = (Sprite)EditorGUILayout.ObjectField("Icon Sprite", node.iconSprite, typeof(Sprite), false);
            string iconAddress = EditorGUILayout.TextField("Icon Address", node.iconAddress);
            WorldMapNodeType nodeType = (WorldMapNodeType)EditorGUILayout.EnumPopup("Node Type", node.nodeType);
            bool visibleByDefault = EditorGUILayout.Toggle("Visible By Default", node.visibleByDefault);
            bool inactiveByDefault = EditorGUILayout.Toggle("Inactive By Default", node.inactiveByDefault);
            string unlockConditionKey = EditorGUILayout.TextField("Unlock Condition", node.unlockConditionKey);
            Vector2 normalizedPosition = EditorGUILayout.Vector2Field("Normalized Position", node.normalizedPosition);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "월드맵 노드 편집");
                node.titleOverride = titleOverride;
                node.iconSprite = iconSprite;
                node.iconAddress = iconAddress;
                if (iconSprite != null && iconSprite != oldIconSprite)
                {
                    node.iconAddress = ConfigAddressableWorldMap.GetNodeIconKey(asset.graphId, node.nodeId);
                }
                node.nodeType = nodeType;
                node.visibleByDefault = visibleByDefault;
                node.inactiveByDefault = inactiveByDefault;
                node.unlockConditionKey = unlockConditionKey;
                node.normalizedPosition = new Vector2(Mathf.Clamp01(normalizedPosition.x), Mathf.Clamp01(normalizedPosition.y));
                if (node.nodeType == WorldMapNodeType.Start)
                {
                    asset.startNodeId = node.nodeId;
                }

                EditorUtility.SetDirty(asset);
                onChanged?.Invoke();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("시작 노드로 설정", GUILayout.Height(26)))
                {
                    onSetStartNode?.Invoke(node.nodeId);
                }

                if (GUILayout.Button("이 노드에서 연결", GUILayout.Height(26)))
                {
                    onStartLinking?.Invoke(node.nodeId);
                }
            }

            EditorGUILayout.HelpBox("연결 생성 후 중앙 캔버스에서 도착 노드를 클릭하면 edge가 생성됩니다.", MessageType.None);

            GUILayout.Space(8f);
            if (GUILayout.Button("선택 노드 삭제", GUILayout.Height(28)))
            {
                onDeleteSelected?.Invoke();
            }
        }

        /// <summary>
        /// 선택된 연결선의 상세 속성 편집 UI를 그립니다.
        /// </summary>
        /// <param name="asset">편집 중인 월드맵 그래프 에셋입니다.</param>
        /// <param name="edge">선택된 연결선입니다.</param>
        /// <param name="onChanged">데이터 변경 콜백입니다.</param>
        /// <param name="onDeleteSelected">선택 항목 삭제 콜백입니다.</param>
        private static void DrawEdgeInspector(
            WorldMapGraphAsset asset,
            WorldMapEdgeData edge,
            Action onChanged,
            Action onDeleteSelected)
        {
            EditorGUILayout.LabelField("선택 연결선", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string oldEdgeId = edge.edgeId;
            string edgeId = EditorGUILayout.DelayedTextField("Edge ID", edge.edgeId);
            EditorGUILayout.LabelField("From", edge.fromNodeId);
            EditorGUILayout.LabelField("To", edge.toNodeId);
            bool bidirectional = EditorGUILayout.Toggle("Bidirectional", edge.bidirectional);
            WorldMapEdgeType edgeType = (WorldMapEdgeType)EditorGUILayout.EnumPopup("Edge Type", edge.edgeType);
            Sprite oldEdgeSprite = edge.edgeSprite;
            Sprite edgeSprite = (Sprite)EditorGUILayout.ObjectField("Edge Sprite", edge.edgeSprite, typeof(Sprite), false);
            string edgeSpriteAddress = EditorGUILayout.TextField("Edge Sprite Address", edge.edgeSpriteAddress);
            string unlockConditionKey = EditorGUILayout.TextField("Unlock Condition", edge.unlockConditionKey);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "월드맵 연결선 편집");
                edge.edgeId = edgeId;
                edge.bidirectional = bidirectional;
                edge.edgeType = edgeType;
                edge.edgeSprite = edgeSprite;
                edge.edgeSpriteAddress = edgeSpriteAddress;
                if (edgeSprite != null && (edgeSprite != oldEdgeSprite || edgeId != oldEdgeId))
                {
                    edge.edgeSpriteAddress = ConfigAddressableWorldMap.GetEdgeSpriteKey(asset.graphId, edge.edgeId);
                }
                edge.unlockConditionKey = unlockConditionKey;
                EditorUtility.SetDirty(asset);
                onChanged?.Invoke();
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("선택 연결선 삭제", GUILayout.Height(28)))
            {
                onDeleteSelected?.Invoke();
            }
        }
    }
}
