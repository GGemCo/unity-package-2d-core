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
        private readonly DialogueEditorWindowWindow _editorWindowWindow;
        private readonly Vector2 defaultNodeSize = new Vector2(250, 150);

        public NodeHandler(DialogueEditorWindowWindow windowWindow)
        {
            _editorWindowWindow = windowWindow;
        }

        public void DrawNodes()
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(Vector2.one * _editorWindowWindow.zoom, Vector2.zero);
            GUI.matrix = Matrix4x4.TRS(_editorWindowWindow.panOffset, Quaternion.identity, Vector3.one) * GUI.matrix;

            DialogueNode nodeToDelete = null;

            foreach (DialogueNode node in _editorWindowWindow.nodes)
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

            if (node == _editorWindowWindow.selectedNode)
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
            
            bool isDialogueConnecting = (_editorWindowWindow.draggingFromDialogue == node);
            bool clickedDialogueToggle = GUILayout.Toggle(isDialogueConnecting, GUIContent.none, GUILayout.Width(20));

            if (clickedDialogueToggle && !isDialogueConnecting)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("연결 하기"), false, () =>
                {
                    _editorWindowWindow.draggingFromDialogue = node;
                    _editorWindowWindow.isDraggingConnection = true;
                });
                menu.AddItem(new GUIContent("연결 삭제"), false, () =>
                {
                    node.nextNodeGuid = null;
                    _editorWindowWindow.Repaint();
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

                    bool isConnecting = (_editorWindowWindow.draggingFromOption == option);
                    bool clicked = GUILayout.Toggle(isConnecting, GUIContent.none, GUILayout.Width(20));
                    if (clicked && !isConnecting)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("연결 하기"), false, () =>
                        {
                            _editorWindowWindow.draggingFromOption = option;
                            _editorWindowWindow.draggingFromNode = node;
                            _editorWindowWindow.isDraggingConnection = true;
                        });
                        menu.AddItem(new GUIContent("연결 삭제"), false, () =>
                        {
                            option.nextNodeGuid = null;
                            _editorWindowWindow.Repaint();
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
            if (_editorWindowWindow.nodes.Contains(node))
            {
                _editorWindowWindow.nodes.Remove(node);

                foreach (DialogueNode n in _editorWindowWindow.nodes)
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
            if (_editorWindowWindow.nodes == null) return;

            Vector2 adjustedMousePosition = (e.mousePosition - _editorWindowWindow.panOffset) / _editorWindowWindow.zoom;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _editorWindowWindow.draggingNode = GetNodeAtPoint(adjustedMousePosition);
                if (_editorWindowWindow.draggingNode != null)
                {
                    _editorWindowWindow.selectedNode = _editorWindowWindow.draggingNode;
                    Selection.activeObject = _editorWindowWindow.draggingNode;
                    _editorWindowWindow.draggingOffset = _editorWindowWindow.draggingNode.position - adjustedMousePosition;
                }
                else
                {
                    _editorWindowWindow.selectedNode = null;
                    Selection.activeObject = null;
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && _editorWindowWindow.draggingNode != null)
            {
                _editorWindowWindow.draggingNode.position = adjustedMousePosition + _editorWindowWindow.draggingOffset;
                GUI.changed = true;
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _editorWindowWindow.draggingNode = null;
            }
        }

        private DialogueNode GetNodeAtPoint(Vector2 point)
        {
            foreach (DialogueNode node in _editorWindowWindow.nodes)
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
            return _editorWindowWindow.nodes.FirstOrDefault(node =>
            {
                Vector2 size = node.cachedSize != Vector2.zero ? node.cachedSize : defaultNodeSize;
                return new Rect(node.position, new Vector2(defaultNodeSize.x, Mathf.Max(size.y, defaultNodeSize.y))).Contains(pos);
            });
        }

        public void HandleEvents()
        {
            if (_editorWindowWindow.isDraggingConnection && Event.current.type == EventType.MouseUp)
            {
                Vector2 adjustedMousePosition =
                    (Event.current.mousePosition - _editorWindowWindow.panOffset) / _editorWindowWindow.zoom;

                DialogueNode targetNode = FindNodeAtPosition(adjustedMousePosition);
                if (targetNode != null)
                {
                    if (_editorWindowWindow.draggingFromOption != null && targetNode != _editorWindowWindow.draggingFromNode)
                    {
                        _editorWindowWindow.draggingFromOption.nextNodeGuid = targetNode.guid;
                    }
                    else if (_editorWindowWindow.draggingFromDialogue != null &&
                             _editorWindowWindow.draggingFromDialogue != targetNode)
                    {
                        _editorWindowWindow.draggingFromDialogue.nextNodeGuid = targetNode.guid;
                    }
                }

                _editorWindowWindow.draggingFromNode = null;
                _editorWindowWindow.draggingFromOption = null;
                _editorWindowWindow.draggingFromDialogue = null;
                _editorWindowWindow.isDraggingConnection = false;
                Event.current.Use();
            }
        }
        public void AddNode(Vector2 nodePosition)
        {
            DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
            node.guid = Guid.NewGuid().ToString();
            node.position = nodePosition;
            _editorWindowWindow.nodes.Add(node);
        }

        public void AddNode()
        {
            DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
            node.guid = Guid.NewGuid().ToString();
            node.position = new Vector2(_editorWindowWindow.position.width / 2, _editorWindowWindow.position.height / 2);
            _editorWindowWindow.nodes.Add(node);
        }
    }
}
