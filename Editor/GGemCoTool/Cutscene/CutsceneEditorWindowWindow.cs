using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    public class CutsceneEditorWindowWindow : DefaultEditorWindow
    {
        private const string Title = "연출툴";
        private CutsceneData _data;
        private ReorderableList _list;
        private const string ImportFolder = "Assets/_test";

        private TextAsset _selectedJson;
        
        private TableCutscene _tableCutscene;
        private int _selectedCutsceneIndex;
        
        private readonly List<string> _cutsceneMemos = new List<string>();
        private readonly Dictionary<int, StruckTableCutscene> _cutsceneInfos = new Dictionary<int, StruckTableCutscene>(); 
        
        [MenuItem(ConfigEditor.NameToolCutscene, false, (int)ConfigEditor.ToolOrdering.Cutscene)]
        static void Open() => GetWindow<CutsceneEditorWindowWindow>(Title);

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedCutsceneIndex = 0;
            
            _tableCutscene = TableLoaderManager.LoadCutsceneTable();
            LoadCutsceneInfoData();
        }
        /// <summary>
        /// npc 정보 불러오기
        /// </summary>
        private void LoadCutsceneInfoData()
        {
            Dictionary<int, Dictionary<string, string>> npcDictionary = _tableCutscene.GetDatas();
             
            _cutsceneMemos.Clear();
            _cutsceneInfos.Clear();
            int index = 0;
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in npcDictionary)
            {
                var info = _tableCutscene.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _cutsceneMemos.Add($"{info.Uid} - {info.Memo}");
                _cutsceneInfos.TryAdd(index, info);
                index++;
            }
            _selectedCutsceneIndex = 0; // 추가
        }

        private void OnGUI()
        {
            // 방어 코드 추가
            if (_cutsceneMemos.Count == 0)
            {
                EditorGUILayout.LabelField("등록된 연출이 없습니다.");
                return;
            }

            if (_selectedCutsceneIndex >= _cutsceneMemos.Count)
            {
                _selectedCutsceneIndex = 0;
            }
            _selectedCutsceneIndex = EditorGUILayout.Popup("연출 선택", _selectedCutsceneIndex, _cutsceneMemos.ToArray());
            if (GUILayout.Button("연출 플레이"))
            {
                if (!SceneGame.Instance)
                {
                    EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                    return;
                }
                var info = _cutsceneInfos.GetValueOrDefault(_selectedCutsceneIndex);
                SceneGame.Instance.CutsceneManager.PlayCutscene(info.Uid);
            }
            
            GUILayout.Space(20);
            GUILayout.Label("JSON -> Timeline 생성", EditorStyles.boldLabel);
            _selectedJson = (TextAsset)EditorGUILayout.ObjectField("JSON 파일", _selectedJson, typeof(TextAsset), false);

            if (GUILayout.Button("JSON으로부터 타임라인 생성"))
            {
                if (_selectedJson)
                    ImportJsonToTimeline(_selectedJson);
                else
                    Debug.LogWarning("JSON 파일을 선택해주세요.");
            }
        }
        private void ImportJsonToTimeline(TextAsset jsonAsset)
        {
            CutsceneData cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(jsonAsset.text);
            string assetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(jsonAsset));

            if (!Directory.Exists(ImportFolder))
                Directory.CreateDirectory(ImportFolder);

            string timelinePath = Path.Combine(ImportFolder, $"{assetName}.playable");
            var timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timelineAsset, timelinePath);

            if (cutsceneData == null || cutsceneData.events == null)
            {
                Debug.LogError("Json 파싱 실패 또는 이벤트 없음");
                return;
            }

            // 트랙 맵 (EventType -> Track)
            Dictionary<CutsceneEventType, TrackAsset> trackMap = new Dictionary<CutsceneEventType, TrackAsset>();

            foreach (var evt in cutsceneData.events)
            {
                // 트랙이 이미 있으면 재사용, 없으면 새로 생성
                if (!trackMap.TryGetValue(evt.type, out var track))
                {
                    track = timelineAsset.CreateTrack<CutsceneEventTrack>(null, $"{evt.type} Track");
                    trackMap[evt.type] = track;
                }

                // 클립 생성 및 설정
                var clip = track.CreateClip<CutsceneEventClip>();
                clip.start = evt.time;
                clip.duration = evt.duration > 0 ? evt.duration : 1.0; // 최소 duration 보장

                var asset = clip.asset as CutsceneEventClip;
                if (!asset) continue;
                asset.SetEvent(evt);
            }

            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.SaveAssets();
            // TimelineEditorUtility.SelectTimelineAsset(timeline);

            Debug.Log($"Timeline 생성 완료: {timelinePath}");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Timeline 생성 완료", "OK");
        }
    }
}