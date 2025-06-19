using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// Node 확대/축소, 이동
    /// </summary>
    public class ZoomPanHandler
    {
        private readonly DialogueEditorWindow editorWindow;

        public ZoomPanHandler(DialogueEditorWindow window)
        {
            editorWindow = window;
        }

        public void SetZoom(float newZoom)
        {
            editorWindow.zoom = Mathf.Clamp(newZoom, editorWindow.zoomMin, editorWindow.zoomMax);
        }
        public void HandleZoom()
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom = editorWindow.zoom;
                editorWindow.zoom = Mathf.Clamp(editorWindow.zoom - e.delta.y * 0.01f, editorWindow.zoomMin, editorWindow.zoomMax);
        
                Vector2 mousePos = e.mousePosition;
                Vector2 delta = mousePos - editorWindow.panOffset;
                Vector2 zoomDelta = delta * (editorWindow.zoom - oldZoom);
                editorWindow.panOffset -= zoomDelta;

                e.Use();
            }
        }
        public void HandlePan()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag && (e.button == 2 || e.button == 1)) // Middle click or Right click
            {
                editorWindow.panOffset += e.delta;
                e.Use();
            }
        }
        
        public void FitViewToNodes()
        {
            if (editorWindow.nodes.Count == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // 모든 노드의 최소, 최대 x, y 값 구하기
            foreach (var node in editorWindow.nodes)
            {
                minX = Mathf.Min(minX, node.position.x);
                maxX = Mathf.Max(maxX, node.position.x);
                minY = Mathf.Min(minY, node.position.y);
                maxY = Mathf.Max(maxY, node.position.y);
            }

            // 노드 영역에 맞춰서 줌과 패닝 설정
            float width = maxX - minX;
            float height = maxY - minY;

            float aspectRatio = (float)editorWindow.position.width / editorWindow.position.height;
            float scale = Mathf.Min(editorWindow.position.width / width, editorWindow.position.height / height);
            SetZoom(scale);

            // 화면에 꽉 차게 맞추도록 패닝 계산
            editorWindow.panOffset = new Vector2((editorWindow.position.width - width * scale) / 2 - minX * scale,
                (editorWindow.position.height - height * scale) / 2 - minY * scale);
        }
    }
}
