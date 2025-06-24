using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 노드 연결 관리
    /// </summary>
    public class ConnectionHandler
    {
        private readonly DialogueEditorWindowWindow _editorWindowWindow;

        public ConnectionHandler(DialogueEditorWindowWindow windowWindow)
        {
            _editorWindowWindow = windowWindow;
        }

        public void DrawConnections()
        {
            Handles.color = Color.white;
            if (_editorWindowWindow.nodes == null) return;

            foreach (var node in _editorWindowWindow.nodes)
            {
                // ▼ 옵션 연결 처리
                if (node.options != null)
                {
                    foreach (var option in node.options)
                    {
                        if (option == null || string.IsNullOrEmpty(option.nextNodeGuid)) continue;

                        DialogueNode targetNode = _editorWindowWindow.nodes.FirstOrDefault(n => n.guid == option.nextNodeGuid);
                        if (targetNode == null) continue;

                        Vector2 startPos = option.connectionPoint * _editorWindowWindow.zoom + _editorWindowWindow.panOffset;
                        Vector2 endPos = new Vector2(targetNode.position.x, targetNode.position.y + 30) * _editorWindowWindow.zoom + _editorWindowWindow.panOffset;

                        Handles.DrawBezier(
                            startPos,
                            endPos,
                            startPos + Vector2.right * 50f,
                            endPos + Vector2.left * 50f,
                            Color.white,
                            null,
                            3f
                        );
                    }
                }

                // ▼ 대사 텍스트 연결 처리
                if (!string.IsNullOrEmpty(node.nextNodeGuid))
                {
                    DialogueNode targetNode = _editorWindowWindow.nodes.FirstOrDefault(n => n.guid == node.nextNodeGuid);
                    if (targetNode != null)
                    {
                        Vector2 startPos = node.nodeConnectionPoint * _editorWindowWindow.zoom + _editorWindowWindow.panOffset;
                        Vector2 endPos = new Vector2(targetNode.position.x, targetNode.position.y + 30) * _editorWindowWindow.zoom + _editorWindowWindow.panOffset;

                        Handles.DrawBezier(
                            startPos,
                            endPos,
                            startPos + Vector2.right * 50f,
                            endPos + Vector2.left * 50f,
                            Color.cyan,
                            null,
                            3f
                        );
                    }
                }
            }

            // ▼ 드래그 중인 옵션 연결선
            if (_editorWindowWindow.isDraggingConnection && _editorWindowWindow.draggingFromOption != null)
            {
                Vector2 startPos = _editorWindowWindow.draggingFromOption.connectionPoint * _editorWindowWindow.zoom + _editorWindowWindow.panOffset;
                Vector2 endPos = Event.current.mousePosition;

                Handles.DrawBezier(
                    startPos,
                    endPos,
                    startPos + Vector2.right * 50f,
                    endPos + Vector2.left * 50f,
                    Color.yellow,
                    null,
                    3f
                );
            }

            // ▼ 드래그 중인 dialogueText 연결선
            if (_editorWindowWindow.isDraggingConnection && _editorWindowWindow.draggingFromDialogue != null)
            {
                Vector2 startPos = _editorWindowWindow.draggingFromDialogue.nodeConnectionPoint * _editorWindowWindow.zoom + _editorWindowWindow.panOffset;
                Vector2 endPos = Event.current.mousePosition;

                Handles.DrawBezier(
                    startPos,
                    endPos,
                    startPos + Vector2.right * 50f,
                    endPos + Vector2.left * 50f,
                    Color.cyan,
                    null,
                    3f
                );
            }

            _editorWindowWindow.Repaint();
        }
    }
}
