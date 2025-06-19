using System.Linq;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 노드 연결 관리
    /// </summary>
    public class ConnectionHandler
    {
        private readonly DialogueEditorWindow editorWindow;

        public ConnectionHandler(DialogueEditorWindow window)
        {
            editorWindow = window;
        }

        public void DrawConnections()
        {
            Handles.color = Color.white;
            if (editorWindow.nodes == null) return;

            foreach (var node in editorWindow.nodes)
            {
                // ▼ 옵션 연결 처리
                if (node.options != null)
                {
                    foreach (var option in node.options)
                    {
                        if (option == null || string.IsNullOrEmpty(option.nextNodeGuid)) continue;

                        DialogueNode targetNode = editorWindow.nodes.FirstOrDefault(n => n.guid == option.nextNodeGuid);
                        if (targetNode == null) continue;

                        Vector2 startPos = option.connectionPoint * editorWindow.zoom + editorWindow.panOffset;
                        Vector2 endPos = new Vector2(targetNode.position.x, targetNode.position.y + 30) * editorWindow.zoom + editorWindow.panOffset;

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
                    DialogueNode targetNode = editorWindow.nodes.FirstOrDefault(n => n.guid == node.nextNodeGuid);
                    if (targetNode != null)
                    {
                        Vector2 startPos = node.nodeConnectionPoint * editorWindow.zoom + editorWindow.panOffset;
                        Vector2 endPos = new Vector2(targetNode.position.x, targetNode.position.y + 30) * editorWindow.zoom + editorWindow.panOffset;

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
            if (editorWindow.isDraggingConnection && editorWindow.draggingFromOption != null)
            {
                Vector2 startPos = editorWindow.draggingFromOption.connectionPoint * editorWindow.zoom + editorWindow.panOffset;
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
            if (editorWindow.isDraggingConnection && editorWindow.draggingFromDialogue != null)
            {
                Vector2 startPos = editorWindow.draggingFromDialogue.nodeConnectionPoint * editorWindow.zoom + editorWindow.panOffset;
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

            editorWindow.Repaint();
        }
    }
}
