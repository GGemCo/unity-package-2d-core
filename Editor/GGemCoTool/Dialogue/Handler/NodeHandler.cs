using System;
using System.Linq;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 Node 추가, 삭제, 이동
    /// </summary>
    public class NodeHandler
    {
        private readonly DialogueEditorWindow editorWindow;
        private readonly Vector2 defaultNodeSize = new Vector2(250, 150);

        public NodeHandler(DialogueEditorWindow window)
        {
            editorWindow = window;
        }

        public void DrawNodes()
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * editorWindow.zoom, Vector2.zero);
            GUI.matrix = Matrix4x4.TRS(editorWindow.panOffset, Quaternion.identity, Vector3.one) * GUI.matrix;

            DialogueNode nodeToDelete = null;

            foreach (DialogueNode node in editorWindow.nodes)
            {
                DrawNode(node, ref nodeToDelete);
            }

            if (nodeToDelete != null)
            {
                DeleteNode(nodeToDelete);
            }

            GUI.matrix = oldMatrix;
        }

        private void DrawNode(DialogueNode node, ref DialogueNode nodeToDelete)
        {
            GUIStyle style = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            if (node == editorWindow.selectedNode)
            {
                style.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
                style.border = new RectOffset(12, 12, 12, 12);
            }

            // 임시 높이 계산을 위한 GUI 영역 시작 (나중에 바꿔줌)
            Rect nodeRect = new Rect(node.position, defaultNodeSize)
            {
                height = Mathf.Max(node.cachedSize.y, defaultNodeSize.y)
            };

            GUILayout.BeginArea(nodeRect, style);

            float totalHeight = 0f;

            // 연결 토글 (dialogueText 전용)
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUIStyle wrappedLabel = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField(node.dialogueText, wrappedLabel);
            totalHeight += wrappedLabel.CalcHeight(new GUIContent(node.dialogueText), defaultNodeSize.x - 20) + 10;
            GUILayout.EndVertical();
            
            bool isDialogueConnecting = (editorWindow.draggingFromDialogue == node);
            bool clickedDialogueToggle = GUILayout.Toggle(isDialogueConnecting, GUIContent.none, GUILayout.Width(20));

            if (clickedDialogueToggle && !isDialogueConnecting)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("연결 하기"), false, () =>
                {
                    editorWindow.draggingFromDialogue = node;
                    editorWindow.isDraggingConnection = true;
                });
                menu.AddItem(new GUIContent("연결 삭제"), false, () =>
                {
                    node.nextNodeGuid = null;
                    editorWindow.Repaint();
                });
                menu.ShowAsContext();
            }

            Rect dialogueToggleRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();

            node.nodeConnectionPoint = new Vector2(
                node.position.x + defaultNodeSize.x,
                node.position.y + dialogueToggleRect.y + dialogueToggleRect.height / 2
            );

            totalHeight += dialogueToggleRect.height + 5;
            
            
            if (node.options != null)
            {
                foreach (var option in node.options)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical();
                    GUILayout.Label($"▶ {option.optionText}", wrappedLabel);
                    Rect optionRect = GUILayoutUtility.GetLastRect();
                    GUILayout.EndVertical();

                    bool isConnecting = (editorWindow.draggingFromOption == option);
                    bool clicked = GUILayout.Toggle(isConnecting, GUIContent.none, GUILayout.Width(20));
                    if (clicked && !isConnecting)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("연결 하기"), false, () =>
                        {
                            editorWindow.draggingFromOption = option;
                            editorWindow.draggingFromNode = node;
                            editorWindow.isDraggingConnection = true;
                        });
                        menu.AddItem(new GUIContent("연결 삭제"), false, () =>
                        {
                            option.nextNodeGuid = null;
                            editorWindow.Repaint();
                        });
                        menu.ShowAsContext();
                    }

                    Rect toggleRect = GUILayoutUtility.GetLastRect();

                    GUILayout.EndHorizontal();

                    totalHeight += optionRect.height + 5;

                    option.connectionPoint = new Vector2(node.position.x + defaultNodeSize.x,
                        node.position.y + toggleRect.y + toggleRect.height / 2);
                }
            }

            if (GUILayout.Button("삭제하기"))
            {
                // Undo.RecordObject(this, "Delete Node");
                nodeToDelete = node; // 바로 삭제하지 않고 예약
            }
            Rect buttonRect = GUILayoutUtility.GetLastRect();
            totalHeight += buttonRect.height;

            GUILayout.EndArea();

            // 높이를 저장 (단, 최소는 defaultNodeSize.y)
            node.cachedSize = new Vector2(defaultNodeSize.x, Mathf.Max(totalHeight, defaultNodeSize.y));
        }

        
        private void DeleteNode(DialogueNode node)
        {
            if (editorWindow.nodes.Contains(node))
            {
                editorWindow.nodes.Remove(node);

                foreach (DialogueNode n in editorWindow.nodes)
                {
                    foreach (var option in n.options)
                    {
                        if (option.nextNodeGuid == node.guid)
                            option.nextNodeGuid = null;
                    }
                }
            }
        }

        public void ProcessNodeEvents(Event e)
        {
            if (editorWindow.nodes == null) return;

            Vector2 adjustedMousePosition = (e.mousePosition - editorWindow.panOffset) / editorWindow.zoom;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                editorWindow.draggingNode = GetNodeAtPoint(adjustedMousePosition);
                if (editorWindow.draggingNode != null)
                {
                    editorWindow.selectedNode = editorWindow.draggingNode;
                    Selection.activeObject = editorWindow.draggingNode;
                    editorWindow.draggingOffset = editorWindow.draggingNode.position - adjustedMousePosition;
                }
                else
                {
                    editorWindow.selectedNode = null;
                    Selection.activeObject = null;
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && editorWindow.draggingNode != null)
            {
                editorWindow.draggingNode.position = adjustedMousePosition + editorWindow.draggingOffset;
                GUI.changed = true;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                editorWindow.draggingNode = null;
            }
        }

        private DialogueNode GetNodeAtPoint(Vector2 point)
        {
            foreach (DialogueNode node in editorWindow.nodes)
            {
                Vector2 size = node.cachedSize != Vector2.zero ? node.cachedSize : defaultNodeSize;
                Rect rect = new Rect(node.position, new Vector2(defaultNodeSize.x, Mathf.Max(size.y, defaultNodeSize.y)));
                if (rect.Contains(point))
                    return node;
            }
            return null;
        }
        
        private DialogueNode FindNodeAtPosition(Vector2 pos)
        {
            return editorWindow.nodes.FirstOrDefault(node =>
            {
                Vector2 size = node.cachedSize != Vector2.zero ? node.cachedSize : defaultNodeSize;
                return new Rect(node.position, new Vector2(defaultNodeSize.x, Mathf.Max(size.y, defaultNodeSize.y))).Contains(pos);
            });
        }

        public void HandleEvents()
        {
            if (editorWindow.isDraggingConnection && Event.current.type == EventType.MouseUp)
            {
                Vector2 adjustedMousePosition =
                    (Event.current.mousePosition - editorWindow.panOffset) / editorWindow.zoom;

                DialogueNode targetNode = FindNodeAtPosition(adjustedMousePosition);
                if (targetNode != null)
                {
                    if (editorWindow.draggingFromOption != null && targetNode != editorWindow.draggingFromNode)
                    {
                        editorWindow.draggingFromOption.nextNodeGuid = targetNode.guid;
                    }
                    else if (editorWindow.draggingFromDialogue != null &&
                             editorWindow.draggingFromDialogue != targetNode)
                    {
                        editorWindow.draggingFromDialogue.nextNodeGuid = targetNode.guid;
                    }
                }

                editorWindow.draggingFromNode = null;
                editorWindow.draggingFromOption = null;
                editorWindow.draggingFromDialogue = null;
                editorWindow.isDraggingConnection = false;
                Event.current.Use();
            }
        }
        public void AddNode(Vector2 nodePosition)
        {
            DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
            node.guid = Guid.NewGuid().ToString();
            node.position = nodePosition;
            editorWindow.nodes.Add(node);
        }

        public void AddNode()
        {
            DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
            node.guid = Guid.NewGuid().ToString();
            node.position = new Vector2(editorWindow.position.width / 2, editorWindow.position.height / 2);
            editorWindow.nodes.Add(node);
        }
    }
}
