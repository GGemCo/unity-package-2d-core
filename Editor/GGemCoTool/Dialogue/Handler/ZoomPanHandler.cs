using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Node 확대/축소, 이동
    /// </summary>
    public class ZoomPanHandler
    {
        private readonly DialogueEditorWindowWindow _editorWindowWindow;

        public ZoomPanHandler(DialogueEditorWindowWindow windowWindow)
        {
            _editorWindowWindow = windowWindow;
        }

        public void SetZoom(float newZoom)
        {
            _editorWindowWindow.zoom = Mathf.Clamp(newZoom, _editorWindowWindow.zoomMin, _editorWindowWindow.zoomMax);
        }
        public void HandleZoom()
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom = _editorWindowWindow.zoom;
                _editorWindowWindow.zoom = Mathf.Clamp(_editorWindowWindow.zoom - e.delta.y * 0.01f, _editorWindowWindow.zoomMin, _editorWindowWindow.zoomMax);
        
                Vector2 mousePos = e.mousePosition;
                Vector2 delta = mousePos - _editorWindowWindow.panOffset;
                Vector2 zoomDelta = delta * (_editorWindowWindow.zoom - oldZoom);
                _editorWindowWindow.panOffset -= zoomDelta;

                e.Use();
            }
        }
        public void HandlePan()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag && (e.button == 2 || e.button == 1)) // Middle click or Right click
            {
                _editorWindowWindow.panOffset += e.delta;
                e.Use();
            }
        }
        
        public void FitViewToNodes()
        {
            if (_editorWindowWindow.nodes.Count == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // 모든 노드의 최소, 최대 x, y 값 구하기
            foreach (var node in _editorWindowWindow.nodes)
            {
                minX = Mathf.Min(minX, node.position.x);
                maxX = Mathf.Max(maxX, node.position.x);
                minY = Mathf.Min(minY, node.position.y);
                maxY = Mathf.Max(maxY, node.position.y);
            }

            // 노드 영역에 맞춰서 줌과 패닝 설정
            float width = maxX - minX;
            float height = maxY - minY;

            float aspectRatio = (float)_editorWindowWindow.position.width / _editorWindowWindow.position.height;
            float scale = Mathf.Min(_editorWindowWindow.position.width / width, _editorWindowWindow.position.height / height);
            SetZoom(scale);

            // 화면에 꽉 차게 맞추도록 패닝 계산
            _editorWindowWindow.panOffset = new Vector2((_editorWindowWindow.position.width - width * scale) / 2 - minX * scale,
                (_editorWindowWindow.position.height - height * scale) / 2 - minY * scale);
        }
    }
}
