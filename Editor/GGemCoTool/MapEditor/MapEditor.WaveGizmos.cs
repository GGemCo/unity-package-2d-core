using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치툴의 웨이브 스폰 포인트와 몬스터 예상 위치를 SceneView에 표시합니다.
    /// </summary>
    public sealed partial class MapEditor
    {
        private bool _drawWaveSceneGizmos = true;
        private bool _drawWaveMonsterPreviewGizmos = true;

        private static readonly Color WaveSpawnPointColor = new Color(0.1f, 0.8f, 1f, 0.9f);
        private static readonly Color WaveSelectedSpawnPointColor = new Color(1f, 0.85f, 0.1f, 1f);
        private static readonly Color WaveStartPointColor = new Color(0.2f, 1f, 0.35f, 1f);
        private static readonly Color WaveMonsterPreviewColor = new Color(1f, 0.35f, 0.25f, 0.85f);
        private static readonly Color WaveSelectedGroupMonsterPreviewColor = new Color(1f, 0.15f, 0.85f, 0.95f);

        /// <summary>
        /// SceneView 웨이브 기즈모 콜백을 등록합니다.
        /// 에디터 윈도우가 다시 활성화되어도 중복 등록되지 않도록 기존 콜백을 먼저 제거합니다.
        /// </summary>
        private void RegisterWaveSceneGizmos()
        {
            SceneView.duringSceneGui -= DrawWaveSceneGizmos;
            SceneView.duringSceneGui += DrawWaveSceneGizmos;
        }

        /// <summary>
        /// SceneView 웨이브 기즈모 콜백을 해제합니다.
        /// 맵 배치툴 창이 닫힌 뒤에도 기즈모가 남는 문제를 방지합니다.
        /// </summary>
        private void UnregisterWaveSceneGizmos()
        {
            SceneView.duringSceneGui -= DrawWaveSceneGizmos;
        }

        /// <summary>
        /// 웨이브 편집 UI에서 SceneView 기즈모 표시 옵션을 그립니다.
        /// </summary>
        private void DrawWaveGizmoOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("디버그 / 기즈모", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                _drawWaveSceneGizmos = EditorGUILayout.Toggle("씬 기즈모 표시", _drawWaveSceneGizmos);
                using (new EditorGUI.DisabledScope(!_drawWaveSceneGizmos))
                {
                    _drawWaveMonsterPreviewGizmos = EditorGUILayout.Toggle("몬스터 예상 위치 표시", _drawWaveMonsterPreviewGizmos);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }
        }

        /// <summary>
        /// 현재 선택된 웨이브 시나리오의 스폰 포인트와 몬스터 예상 위치를 SceneView에 그립니다.
        /// </summary>
        /// <param name="sceneView">현재 렌더링 중인 SceneView입니다.</param>
        private void DrawWaveSceneGizmos(SceneView sceneView)
        {
            if (!IsWavePlacementTabActive ||
                !_drawWaveSceneGizmos ||
                _waveExporter?.DataList?.WaveScenarios == null)
            {
                return;
            }

            MapWaveScenarioData scenario = ResolveWaveGizmoScenario();
            if (scenario == null)
            {
                return;
            }

            Color previousColor = Handles.color;
            UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;

            try
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                DrawWaveSpawnPointGizmos(scenario);

                if (_drawWaveMonsterPreviewGizmos)
                {
                    DrawWaveMonsterPreviewGizmos(scenario);
                }
            }
            finally
            {
                Handles.color = previousColor;
                Handles.zTest = previousZTest;
            }
        }

        /// <summary>
        /// 기즈모로 표시할 웨이브 시나리오를 계산합니다.
        /// 현재 선택된 시나리오가 유효하면 우선 사용하고, 없으면 첫 번째 시나리오를 사용합니다.
        /// </summary>
        /// <returns>기즈모 표시 대상 시나리오입니다.</returns>
        private MapWaveScenarioData ResolveWaveGizmoScenario()
        {
            MapWaveScenarioData selectedScenario = _waveExporter.FindScenario(_selectedWaveScenarioUid);
            if (selectedScenario != null)
            {
                return selectedScenario;
            }

            List<MapWaveScenarioData> scenarios = _waveExporter.DataList.WaveScenarios;
            return scenarios != null && scenarios.Count > 0 ? scenarios[0] : null;
        }

        /// <summary>
        /// 시나리오의 스폰 포인트 위치와 랜덤 반경을 SceneView에 표시합니다.
        /// </summary>
        /// <param name="scenario">표시할 웨이브 시나리오입니다.</param>
        private void DrawWaveSpawnPointGizmos(MapWaveScenarioData scenario)
        {
            if (scenario?.SpawnPoints == null)
            {
                return;
            }

            for (int i = 0; i < scenario.SpawnPoints.Count; i++)
            {
                MapWaveSpawnPointData point = scenario.SpawnPoints[i];
                if (point == null || point.PointId <= 0)
                {
                    continue;
                }

                Vector3 position = new Vector3(point.x, point.y, point.z);
                bool isSelected = point.PointId == _selectedWaveSpawnPointId;
                bool isStartPoint = point.PointId == scenario.StartPointId;

                Handles.color = isSelected
                    ? WaveSelectedSpawnPointColor
                    : isStartPoint ? WaveStartPointColor : WaveSpawnPointColor;

                float handleSize = HandleUtility.GetHandleSize(position) * (isSelected ? 0.18f : 0.12f);
                Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize, EventType.Repaint);
                Handles.DrawWireDisc(position, Vector3.forward, handleSize * 2f);

                if (point.RandomRadius > 0f)
                {
                    Handles.DrawWireDisc(position, Vector3.forward, point.RandomRadius);
                }

                string label = isStartPoint
                    ? $"Wave Start / Point {point.PointId}"
                    : $"Wave Point {point.PointId}";
                Handles.Label(position + Vector3.up * (handleSize * 1.6f), label);
            }
        }

        /// <summary>
        /// 그룹별 몬스터 스폰 예상 위치를 SceneView에 표시합니다.
        /// 실제 런타임에서는 랜덤 반경과 생성 순서가 적용되므로, 기즈모는 기준 위치 확인용으로만 사용합니다.
        /// </summary>
        /// <param name="scenario">표시할 웨이브 시나리오입니다.</param>
        private void DrawWaveMonsterPreviewGizmos(MapWaveScenarioData scenario)
        {
            if (scenario?.Groups == null)
            {
                return;
            }

            for (int groupIndex = 0; groupIndex < scenario.Groups.Count; groupIndex++)
            {
                MapWaveGroupData group = scenario.Groups[groupIndex];
                if (group?.Monsters == null)
                {
                    continue;
                }

                for (int monsterIndex = 0; monsterIndex < group.Monsters.Count; monsterIndex++)
                {
                    MapWaveMonsterSpawnData monsterSpawn = group.Monsters[monsterIndex];
                    if (monsterSpawn == null || monsterSpawn.MonsterUid <= 0)
                    {
                        continue;
                    }

                    MapWaveSpawnPointData point = _waveExporter.FindSpawnPoint(scenario, monsterSpawn.SpawnPointId);
                    if (point == null)
                    {
                        continue;
                    }

                    Vector3 position = new Vector3(
                        point.x + monsterSpawn.OffsetX,
                        point.y + monsterSpawn.OffsetY,
                        point.z + monsterSpawn.OffsetZ);

                    bool isSelectedGroup = group.GroupUid == _selectedWaveGroupUid;
                    Handles.color = isSelectedGroup ? WaveSelectedGroupMonsterPreviewColor : WaveMonsterPreviewColor;

                    float handleSize = HandleUtility.GetHandleSize(position) * 0.1f;
                    Handles.DrawWireCube(position, Vector3.one * handleSize);
                    Handles.Label(
                        position + Vector3.down * (handleSize * 1.8f),
                        $"G{group.GroupUid} / M{monsterSpawn.MonsterUid} x{Mathf.Max(1, monsterSpawn.Count)}");
                }
            }
        }
    }
}
