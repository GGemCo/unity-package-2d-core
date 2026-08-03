using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치 작업을 기능별 탭으로 나누고 탭별 UI 상태를 관리합니다.
    /// </summary>
    public sealed partial class MapEditor
    {
        private const string SelectedTabSessionKey = "GGemCo2DCoreEditor.MapEditor.SelectedTab";

        private static readonly string[] MapEditorTabLabels =
        {
            "NPC",
            "몬스터",
            "웨이브",
            "워프",
            "확장",
        };

        private static readonly string[] MapEditorTabTitles =
        {
            "NPC 배치",
            "단일 몬스터 배치",
            "웨이브 스폰 편집",
            "워프 배치",
            "프로젝트 확장",
        };

        private readonly Vector2[] _tabScrollPositions = new Vector2[MapEditorTabLabels.Length];
        private MapEditorTab _selectedTab;

        private enum MapEditorTab
        {
            Npc = 0,
            Monster = 1,
            Wave = 2,
            Warp = 3,
            Extension = 4,
        }

        /// <summary>
        /// 현재 선택된 맵 배치 탭이 단일 몬스터 탭인지 여부입니다.
        /// SceneView 전투 프로필 기즈모의 표시 범위를 탭과 동기화할 때 사용합니다.
        /// </summary>
        private bool IsMonsterPlacementTabActive => _selectedTab == MapEditorTab.Monster;

        /// <summary>
        /// 현재 선택된 맵 배치 탭이 웨이브 탭인지 여부입니다.
        /// SceneView 웨이브 기즈모의 표시 범위를 탭과 동기화할 때 사용합니다.
        /// </summary>
        private bool IsWavePlacementTabActive => _selectedTab == MapEditorTab.Wave;

        /// <summary>
        /// 현재 Editor 세션에 저장된 마지막 활성 탭을 복원합니다.
        /// 저장된 값이 유효하지 않으면 NPC 탭을 기본값으로 사용합니다.
        /// </summary>
        private void RestoreSelectedMapEditorTab()
        {
            int savedIndex = SessionState.GetInt(
                SelectedTabSessionKey,
                (int)MapEditorTab.Npc);
            _selectedTab = savedIndex >= 0 && savedIndex < MapEditorTabLabels.Length
                ? (MapEditorTab)savedIndex
                : MapEditorTab.Npc;
        }

        /// <summary>
        /// 맵 배치 기능 탭 버튼을 그리고 선택 변경을 Editor 세션에 보관합니다.
        /// </summary>
        private void DrawMapEditorTabs()
        {
            int selectedIndex = GUILayout.Toolbar(
                (int)_selectedTab,
                MapEditorTabLabels,
                EditorStyles.toolbarButton,
                GUILayout.Height(24f));
            if (selectedIndex < 0 ||
                selectedIndex >= MapEditorTabLabels.Length ||
                selectedIndex == (int)_selectedTab)
            {
                return;
            }

            _selectedTab = (MapEditorTab)selectedIndex;
            SessionState.SetInt(SelectedTabSessionKey, selectedIndex);

            // 탭별 SceneView 보조 표시가 즉시 전환되도록 모든 SceneView를 다시 그립니다.
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 활성 탭의 독립 스크롤 위치를 유지하면서 해당 배치 UI만 그립니다.
        /// </summary>
        private void DrawSelectedMapEditorTab()
        {
            int tabIndex = (int)_selectedTab;
            Vector2 scrollPosition = _tabScrollPositions[tabIndex];
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            try
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    MapEditorTabTitles[tabIndex],
                    EditorStyles.boldLabel);
                EditorGUILayout.Space(4f);
                DrawSelectedMapEditorTabContent();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
                _tabScrollPositions[tabIndex] = scrollPosition;
            }
        }

        /// <summary>
        /// 현재 탭에 속하는 Core 또는 외부 확장 편집 패널을 선택적으로 그립니다.
        /// 탭은 UI 표시만 제어하며 맵 전체 데이터의 Load와 Export 수명주기는 변경하지 않습니다.
        /// </summary>
        private void DrawSelectedMapEditorTabContent()
        {
            switch (_selectedTab)
            {
                case MapEditorTab.Npc:
                    DrawNpcSection();
                    GUILayout.Space(12f);
                    DrawNpcEditSection();
                    break;

                case MapEditorTab.Monster:
                    DrawMonsterSection();
                    GUILayout.Space(12f);
                    DrawMonsterEditSection();
                    break;

                case MapEditorTab.Wave:
                    DrawWaveSection();
                    break;

                case MapEditorTab.Warp:
                    DrawWarpSection();
                    break;

                case MapEditorTab.Extension:
                    DrawExtensionSections();
                    break;
            }
        }
    }
}
