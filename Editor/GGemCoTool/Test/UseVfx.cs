using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class UseVfx : DefaultEditorWindow
    {
        private const string Title = "Vfx 사용툴";
        private TableVfx _tableVfx;
        private int _selectedIndex;
        private float _scale = 1f;
        private float _duration;
        private string _color = string.Empty;
        private bool _followScenePlayer;
        private bool _useUiSorting;

        private readonly List<string> _names = new List<string>();
        private readonly List<int> _uids = new List<int>();
        private Dictionary<int, StruckTableVfx> _tableDictionary;

        [MenuItem(ConfigEditor.NameToolUseVfx, false, (int)ConfigEditor.ToolOrdering.UseVfx)]
        public static void ShowWindow()
        {
            GetWindow<UseVfx>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndex = 0;
            _tableVfx = TableLoaderManager.LoadVfxTable();
            _tableDictionary = _tableVfx.GetDatas();
            LoadTableInfoData();
        }

        private void OnGUI()
        {
            if (_selectedIndex >= _names.Count)
                _selectedIndex = 0;

            _selectedIndex = EditorGUILayout.Popup("VFX 선택", _selectedIndex, _names.ToArray());
            _scale = EditorGUILayout.FloatField("Scale", _scale);
            _duration = EditorGUILayout.FloatField("Duration", _duration);
            _color = EditorGUILayout.TextField("Color(Hex)", _color);
            _followScenePlayer = EditorGUILayout.Toggle("Follow Player", _followScenePlayer);
            _useUiSorting = EditorGUILayout.Toggle("Force UI Sorting", _useUiSorting);

            if (GUILayout.Button("VFX 사용"))
                CreateVfx();
        }

        private void CreateVfx()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            int uid = _uids[_selectedIndex];
            var info = _tableDictionary.GetValueOrDefault(uid);
            if (info == null)
                return;

            var request = new VfxSpawnRequest
            {
                VfxUid = uid,
                DurationOverride = _duration,
                ScaleOverride = _scale,
                ColorOverride = _color,
                ForceUiCanvasParent = info.Category == VfxConstants.Category.UI,
                SortingLayerOverride = _useUiSorting ? (ConfigSortingLayer.Keys?)ConfigSortingLayer.Keys.UI : null,
            };

            var playerObject = SceneGame.Instance.player;
            var player = playerObject != null ? playerObject.GetComponent<CharacterBase>() : null;
            if (player != null)
            {
                request.Owner = player;
                if (_followScenePlayer)
                    request.FollowTarget = player;
            }

            var vfx = SceneGame.Instance.VfxManager.CreateVfx(request);
            if (vfx == null)
                return;

            if (info.Category == VfxConstants.Category.UI)
            {
                vfx.transform.SetParent(SceneGame.Instance.canvasUI.transform);
                vfx.transform.localPosition = Vector3.zero;
            }
            else if (!_followScenePlayer)
            {
                vfx.transform.position = SceneGame.Instance.cameraManager.GetPositionCenter();
            }

            if (_useUiSorting)
                vfx.SetSortingLayer(ConfigSortingLayer.Keys.UI);
        }

        private void LoadTableInfoData()
        {
            _names.Clear();
            _uids.Clear();

            foreach (var kvp in _tableDictionary)
            {
                var info = kvp.Value;
                if (info.Uid <= 0) continue;

                _names.Add($"{info.Uid} - {info.Name} [{info.PlaybackType}]");
                _uids.Add(info.Uid);
            }
            _selectedIndex = 0;
        }
    }
}
