using System.Collections.Generic;
using System.Linq;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 생성툴
    /// </summary>
    public class DialogueEditorWindow : EditorWindow
    {
        public List<DialogueNode> nodes = new List<DialogueNode>();
        public DialogueNode selectedNode;

        private Vector2 offset;
        public DialogueNode draggingNode;
        public Vector2 draggingOffset;
        
        // 연결선 드래그
        public DialogueNode draggingFromDialogue;
        public DialogueNode draggingFromNode;
        public DialogueOption draggingFromOption;
        private Vector2 draggingPosition;
        public bool isDraggingConnection;
        
        // 줌 인,아웃
        public float zoom = 1.0f;
        public Vector2 zoomOrigin = Vector2.zero;
        public Vector2 panOffset = Vector2.zero;
        public float zoomMin = 0.5f;
        public float zoomMax = 2.0f;

        public ZoomPanHandler ZoomPanHandler;
        public NodeHandler NodeHandler;
        public FileHandler FileHandler;
        
        private ConnectionHandler connectionHandler;
        private ToolbarHandler toolbarHandler;
        
        [MenuItem(ConfigEditor.NameToolCreateDialogue, false, (int)ConfigEditor.ToolOrdering.CreateDialogue)]
        static void OpenWindow()
        {
            GetWindow<DialogueEditorWindow>("대사 생성툴");
        }

        private void OnEnable()
        {
            ZoomPanHandler = new ZoomPanHandler(this);
            NodeHandler = new NodeHandler(this);
            connectionHandler = new ConnectionHandler(this);
            FileHandler = new FileHandler(this);
            toolbarHandler = new ToolbarHandler(this);
        }

        private void OnGUI()
        {
            // DrawGrid(20, 0.2f, Color.gray);
            // DrawGrid(100, 0.4f, Color.gray);
            ZoomPanHandler?.HandleZoom();
            ZoomPanHandler?.HandlePan();
            
            GUILayout.BeginHorizontal();

            toolbarHandler?.DrawToolbar();

            // 오른쪽 메인 에디터 영역
            GUILayout.BeginVertical();
            
            NodeHandler?.DrawNodes();
            
            connectionHandler?.DrawConnections();
            NodeHandler?.HandleEvents(); // 추가

            ProcessEvents(Event.current);
            NodeHandler?.ProcessNodeEvents(Event.current);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            
            if (GUI.changed) Repaint();
        }
        
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

            for (int i = 0; i < widthDivs; i++)
                Handles.DrawLine(new Vector3(gridSpacing * i, 0, 0) + newOffset,
                    new Vector3(gridSpacing * i, position.height, 0f) + newOffset);

            for (int j = 0; j < heightDivs; j++)
                Handles.DrawLine(new Vector3(0, gridSpacing * j, 0) + newOffset,
                    new Vector3(position.width, gridSpacing * j, 0f) + newOffset);

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void ProcessEvents(Event e)
        {
            // if (e.type == EventType.MouseDown && e.button == 1)
            // {
            //     ShowContextMenu(e.mousePosition);
            // }
        }
        
        private void AutoLayout()
        {
            float startX = 50f;
            float startY = 50f;
            float nodeSpacingX = 400f;
            float nodeSpacingY = 200f;

            Dictionary<string, int> levels = new Dictionary<string, int>();

            foreach (var node in nodes)
                levels[node.guid] = GetDepth(node);

            var sortedNodes = nodes.OrderBy(n => levels[n.guid]).ToList();
            float currentY = startY;
            int preLevel = 0;
            foreach (var node in sortedNodes)
            {
                if (preLevel != levels[node.guid])
                {
                    currentY = startY;
                }
                node.position = new Vector2(startX + levels[node.guid] * nodeSpacingX, currentY);
                currentY += nodeSpacingY;
                preLevel = levels[node.guid];
            }
        }
        private int GetDepth(DialogueNode node)
        {
            int depth = 0;
            DialogueNode current = node;
            while (true)
            {
                var parent = nodes.FirstOrDefault(n => n.options.Any(o => o.nextNodeGuid == current.guid));
                if (parent == null) break;
                current = parent;
                depth++;
            }
            return depth;
        }
    }
}