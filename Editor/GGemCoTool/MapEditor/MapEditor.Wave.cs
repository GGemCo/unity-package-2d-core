using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치툴의 웨이브 스폰 편집 UI를 담당하는 partial 클래스입니다.
    /// </summary>
    public sealed partial class MapEditor
    {
        private bool _foldWave = true;
        private int _selectedWaveScenarioUid;
        private int _selectedWaveSpawnPointId;
        private int _selectedWaveGroupUid;
        private int _selectedWaveMonsterSpawnIndex = -1;

        /// <summary>
        /// 맵을 새로 불러올 때 웨이브 편집 선택 상태를 초기화합니다.
        /// 기존 맵의 선택 UID가 다음 맵 데이터에 잘못 적용되지 않도록 분리합니다.
        /// </summary>
        private void ResetWaveEditorSelection()
        {
            _selectedWaveScenarioUid = 0;
            _selectedWaveSpawnPointId = 0;
            _selectedWaveGroupUid = 0;
            _selectedWaveMonsterSpawnIndex = -1;
        }

        /// <summary>
        /// 웨이브 시나리오, 스폰 포인트, 그룹, 그룹 몬스터 편집 섹션을 그립니다.
        /// 이 섹션에서 수정한 데이터는 맵 저장 시 wave_spawn.json으로 함께 내보냅니다.
        /// </summary>
        private void DrawWaveSection()
        {
            _foldWave = EditorGUILayout.Foldout(_foldWave, "6) 웨이브 스폰 편집", true);
            if (!_foldWave) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_defaultMap == null || GetSelectedMapUid() <= 0)
                {
                    EditorGUILayout.HelpBox("맵을 먼저 불러온 뒤 웨이브 스폰 데이터를 편집해주세요.", MessageType.Info);
                    return;
                }

                EditorGUILayout.HelpBox(
                    "웨이브 데이터는 현재 맵 폴더의 wave_spawn.json으로 저장됩니다.\n" +
                    "스폰 포인트는 씬에서 선택한 오브젝트 위치를 기준으로 추가할 수 있습니다.",
                    MessageType.Info);

                DrawWaveScenarioToolbar();

                MapWaveScenarioData scenario = DrawWaveScenarioSelector();
                if (scenario == null)
                {
                    EditorGUILayout.HelpBox("웨이브 시나리오가 없습니다. '시나리오 추가' 버튼으로 새 시나리오를 생성해주세요.", MessageType.Info);
                    return;
                }

                DrawWaveScenarioEditor(scenario);
                HelperEditorUI.GUILine();
                DrawWaveSpawnPointEditor(scenario);
                HelperEditorUI.GUILine();
                DrawWaveGroupEditor(scenario);
            }
        }

        /// <summary>
        /// 웨이브 시나리오 추가/삭제 버튼 영역을 그립니다.
        /// </summary>
        private void DrawWaveScenarioToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("시나리오 추가", GUILayout.Height(24)))
                {
                    MapWaveScenarioData scenario = _waveExporter.AddScenario(GetSelectedMapUid());
                    if (scenario != null)
                    {
                        _selectedWaveScenarioUid = scenario.ScenarioUid;
                        _selectedWaveSpawnPointId = 0;
                        _selectedWaveGroupUid = 0;
                        _selectedWaveMonsterSpawnIndex = -1;
                    }
                }

                using (new EditorGUI.DisabledScope(_selectedWaveScenarioUid <= 0))
                {
                    if (GUILayout.Button("선택 시나리오 삭제", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog("웨이브 시나리오 삭제", "선택한 웨이브 시나리오를 삭제할까요?", "삭제", "취소"))
                        {
                            _waveExporter.RemoveScenario(_selectedWaveScenarioUid);
                            ResetWaveEditorSelection();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 현재 편집할 웨이브 시나리오를 선택하는 드롭다운을 그립니다.
        /// </summary>
        /// <returns>선택된 웨이브 시나리오 데이터입니다.</returns>
        private MapWaveScenarioData DrawWaveScenarioSelector()
        {
            List<MapWaveScenarioData> scenarios = _waveExporter.DataList.WaveScenarios;
            if (scenarios == null || scenarios.Count == 0)
            {
                return null;
            }

            if (_selectedWaveScenarioUid <= 0 || _waveExporter.FindScenario(_selectedWaveScenarioUid) == null)
            {
                _selectedWaveScenarioUid = scenarios[0]?.ScenarioUid ?? 0;
            }

            List<SearchableDropdownUtility.Option<int>> options = BuildWaveScenarioOptions(scenarios);
            int selectedIndex = FindOptionIndexByUid(options, _selectedWaveScenarioUid);
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                "시나리오 선택",
                options,
                selectedIndex,
                (_, option) =>
                {
                    _selectedWaveScenarioUid = option.Data;
                    _selectedWaveSpawnPointId = 0;
                    _selectedWaveGroupUid = 0;
                    _selectedWaveMonsterSpawnIndex = -1;
                },
                noneText: "(시나리오 선택)");

            return _waveExporter.FindScenario(_selectedWaveScenarioUid);
        }

        /// <summary>
        /// 웨이브 시나리오의 기본 실행 정책 필드를 그립니다.
        /// </summary>
        /// <param name="scenario">편집할 웨이브 시나리오입니다.</param>
        private void DrawWaveScenarioEditor(MapWaveScenarioData scenario)
        {
            EditorGUILayout.LabelField("시나리오 기본 설정", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("ScenarioUid", scenario.ScenarioUid);
                    EditorGUILayout.IntField("MapUid", GetSelectedMapUid());
                }

                scenario.Memo = EditorGUILayout.TextField("메모", scenario.Memo ?? string.Empty);
                scenario.AutoStart = EditorGUILayout.Toggle("맵 로드 후 자동 시작", scenario.AutoStart);
                scenario.StartDelaySeconds = Mathf.Max(0f, EditorGUILayout.FloatField("시작 지연 시간", scenario.StartDelaySeconds));
                scenario.SuppressNormalMonsterRespawnWhileRunning = EditorGUILayout.Toggle(
                    "진행 중 일반 리젠 억제",
                    scenario.SuppressNormalMonsterRespawnWhileRunning);
                scenario.StartPointId = DrawSpawnPointIdPopup("시작 기준 포인트", scenario, scenario.StartPointId);
            }
        }

        /// <summary>
        /// 선택 시나리오의 스폰 포인트 목록과 위치 편집 UI를 그립니다.
        /// </summary>
        /// <param name="scenario">편집할 웨이브 시나리오입니다.</param>
        private void DrawWaveSpawnPointEditor(MapWaveScenarioData scenario)
        {
            EditorGUILayout.LabelField("스폰 포인트", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택 위치로 포인트 추가", GUILayout.Height(24)))
                {
                    MapWaveSpawnPointData point = _waveExporter.AddSpawnPoint(
                        scenario,
                        _waveExporter.ResolveDefaultSpawnPointPosition());
                    if (point != null)
                    {
                        _selectedWaveSpawnPointId = point.PointId;
                    }
                }

                using (new EditorGUI.DisabledScope(_selectedWaveSpawnPointId <= 0))
                {
                    if (GUILayout.Button("선택 위치로 갱신", GUILayout.Height(24)))
                    {
                        MapWaveSpawnPointData point = _waveExporter.FindSpawnPoint(scenario, _selectedWaveSpawnPointId);
                        if (point != null)
                        {
                            Vector3 position = _waveExporter.ResolveDefaultSpawnPointPosition();
                            point.x = position.x;
                            point.y = position.y;
                            point.z = position.z;
                        }
                    }
                }
            }

            MapWaveSpawnPointData selectedPoint = DrawWaveSpawnPointSelector(scenario);
            if (selectedPoint == null)
            {
                EditorGUILayout.HelpBox("스폰 포인트가 없습니다. 먼저 포인트를 추가해주세요.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("PointId", selectedPoint.PointId);
                }

                selectedPoint.x = EditorGUILayout.FloatField("X", selectedPoint.x);
                selectedPoint.y = EditorGUILayout.FloatField("Y", selectedPoint.y);
                selectedPoint.z = EditorGUILayout.FloatField("Z", selectedPoint.z);
                selectedPoint.RandomRadius = Mathf.Max(0f, EditorGUILayout.FloatField("랜덤 반경", selectedPoint.RandomRadius));
                selectedPoint.MapVisibilityPolicy = DrawMapVisibilityPolicyField(
                    "기본 표시 정책",
                    selectedPoint.MapVisibilityPolicy);

                if (GUILayout.Button("선택 스폰 포인트 삭제", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("스폰 포인트 삭제", "선택한 스폰 포인트를 삭제할까요?", "삭제", "취소"))
                    {
                        _waveExporter.RemoveSpawnPoint(scenario, selectedPoint.PointId);
                        _selectedWaveSpawnPointId = 0;
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        /// <summary>
        /// 현재 시나리오의 스폰 포인트 선택 드롭다운을 그립니다.
        /// </summary>
        /// <param name="scenario">편집할 웨이브 시나리오입니다.</param>
        /// <returns>선택된 스폰 포인트 데이터입니다.</returns>
        private MapWaveSpawnPointData DrawWaveSpawnPointSelector(MapWaveScenarioData scenario)
        {
            if (scenario?.SpawnPoints == null || scenario.SpawnPoints.Count == 0)
            {
                return null;
            }

            if (_selectedWaveSpawnPointId <= 0 || _waveExporter.FindSpawnPoint(scenario, _selectedWaveSpawnPointId) == null)
            {
                _selectedWaveSpawnPointId = scenario.SpawnPoints[0]?.PointId ?? 0;
            }

            List<SearchableDropdownUtility.Option<int>> options = BuildWaveSpawnPointOptions(scenario);
            int selectedIndex = FindOptionIndexByUid(options, _selectedWaveSpawnPointId);
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                "포인트 선택",
                options,
                selectedIndex,
                (_, option) => _selectedWaveSpawnPointId = option.Data,
                noneText: "(포인트 선택)");

            return _waveExporter.FindSpawnPoint(scenario, _selectedWaveSpawnPointId);
        }

        /// <summary>
        /// 선택 시나리오의 웨이브 그룹 목록과 그룹 내부 몬스터 스폰 UI를 그립니다.
        /// </summary>
        /// <param name="scenario">편집할 웨이브 시나리오입니다.</param>
        private void DrawWaveGroupEditor(MapWaveScenarioData scenario)
        {
            EditorGUILayout.LabelField("웨이브 그룹", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("그룹 추가", GUILayout.Height(24)))
                {
                    MapWaveGroupData group = _waveExporter.AddGroup(scenario);
                    if (group != null)
                    {
                        _selectedWaveGroupUid = group.GroupUid;
                        _selectedWaveMonsterSpawnIndex = -1;
                    }
                }

                using (new EditorGUI.DisabledScope(_selectedWaveGroupUid <= 0))
                {
                    if (GUILayout.Button("선택 그룹 삭제", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog("웨이브 그룹 삭제", "선택한 웨이브 그룹을 삭제할까요?", "삭제", "취소"))
                        {
                            _waveExporter.RemoveGroup(scenario, _selectedWaveGroupUid);
                            _selectedWaveGroupUid = 0;
                            _selectedWaveMonsterSpawnIndex = -1;
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            MapWaveGroupData selectedGroup = DrawWaveGroupSelector(scenario);
            if (selectedGroup == null)
            {
                EditorGUILayout.HelpBox("웨이브 그룹이 없습니다. 먼저 그룹을 추가해주세요.", MessageType.Info);
                return;
            }

            DrawWaveGroupFields(scenario, selectedGroup);
            DrawWaveGroupMonsterList(scenario, selectedGroup);
        }

        /// <summary>
        /// 현재 시나리오의 그룹 선택 드롭다운을 그립니다.
        /// </summary>
        /// <param name="scenario">편집할 웨이브 시나리오입니다.</param>
        /// <returns>선택된 웨이브 그룹 데이터입니다.</returns>
        private MapWaveGroupData DrawWaveGroupSelector(MapWaveScenarioData scenario)
        {
            if (scenario?.Groups == null || scenario.Groups.Count == 0)
            {
                return null;
            }

            if (_selectedWaveGroupUid <= 0 || _waveExporter.FindGroup(scenario, _selectedWaveGroupUid) == null)
            {
                _selectedWaveGroupUid = scenario.Groups[0]?.GroupUid ?? 0;
            }

            List<SearchableDropdownUtility.Option<int>> options = BuildWaveGroupOptions(scenario);
            int selectedIndex = FindOptionIndexByUid(options, _selectedWaveGroupUid);
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                "그룹 선택",
                options,
                selectedIndex,
                (_, option) =>
                {
                    _selectedWaveGroupUid = option.Data;
                    _selectedWaveMonsterSpawnIndex = -1;
                },
                noneText: "(그룹 선택)");

            return _waveExporter.FindGroup(scenario, _selectedWaveGroupUid);
        }

        /// <summary>
        /// 웨이브 그룹의 진행 정책 필드를 그립니다.
        /// </summary>
        /// <param name="scenario">소속 시나리오입니다.</param>
        /// <param name="group">편집할 웨이브 그룹입니다.</param>
        private void DrawWaveGroupFields(MapWaveScenarioData scenario, MapWaveGroupData group)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("GroupUid", group.GroupUid);
                }

                group.Order = Mathf.Max(1, EditorGUILayout.IntField("실행 순서", group.Order));
                group.RepeatCount = EditorGUILayout.IntField("반복 횟수(-1 무한)", group.RepeatCount);
                if (group.RepeatCount == 0 || group.RepeatCount < -1)
                {
                    group.RepeatCount = 1;
                }

                group.NextPolicy = (WaveNextPolicy)EditorGUILayout.EnumPopup("다음 그룹 정책", group.NextPolicy);
                group.NextAfterSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("시간 전환 기준", group.NextAfterSeconds));
                group.NextDelaySeconds = Mathf.Max(0f, EditorGUILayout.FloatField("다음 그룹 지연", group.NextDelaySeconds));
                group.NextGroupUid = DrawGroupUidPopup("명시 다음 그룹", scenario, group.NextGroupUid, group.GroupUid);
            }
        }

        /// <summary>
        /// 웨이브 그룹 안의 몬스터 스폰 목록을 그립니다.
        /// </summary>
        /// <param name="scenario">소속 시나리오입니다.</param>
        /// <param name="group">편집할 웨이브 그룹입니다.</param>
        private void DrawWaveGroupMonsterList(MapWaveScenarioData scenario, MapWaveGroupData group)
        {
            EditorGUILayout.LabelField("그룹 몬스터", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                int selectedMonsterIndex = FindOptionIndexByUid(_monsterOptions, _selectedMonsterUid);
                SearchableDropdownUtility.DrawLabeledFieldAndShow(
                    "추가할 몬스터",
                    _monsterOptions,
                    selectedMonsterIndex,
                    (_, option) => _selectedMonsterUid = option.Data,
                    noneText: "(몬스터 선택)");

                using (new EditorGUI.DisabledScope(_selectedMonsterUid <= 0))
                {
                    if (GUILayout.Button("그룹에 추가", GUILayout.Width(100), GUILayout.Height(22)))
                    {
                        int spawnPointId = ResolveDefaultMonsterSpawnPointId(scenario);
                        MapWaveMonsterSpawnData addedMonster = _waveExporter.AddMonsterSpawn(
                            group,
                            _selectedMonsterUid,
                            spawnPointId);
                        if (addedMonster != null && group.Monsters != null)
                        {
                            _selectedWaveMonsterSpawnIndex = group.Monsters.Count - 1;
                        }
                    }
                }
            }

            if (group.Monsters == null || group.Monsters.Count == 0)
            {
                EditorGUILayout.HelpBox("이 그룹에 등록된 몬스터가 없습니다.", MessageType.Info);
                return;
            }

            for (int i = 0; i < group.Monsters.Count; i++)
            {
                MapWaveMonsterSpawnData monsterSpawn = group.Monsters[i];
                if (monsterSpawn == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawWaveMonsterSpawnHeader(group, i, monsterSpawn);
                    DrawWaveMonsterSpawnFields(scenario, monsterSpawn);
                }
            }
        }

        /// <summary>
        /// 몬스터 스폰 데이터 행의 헤더와 삭제 버튼을 그립니다.
        /// </summary>
        /// <param name="group">소속 웨이브 그룹입니다.</param>
        /// <param name="index">그룹 안의 몬스터 스폰 인덱스입니다.</param>
        /// <param name="monsterSpawn">표시할 몬스터 스폰 데이터입니다.</param>
        private void DrawWaveMonsterSpawnHeader(
            MapWaveGroupData group,
            int index,
            MapWaveMonsterSpawnData monsterSpawn)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"#{index + 1} MonsterUid:{monsterSpawn.MonsterUid}", EditorStyles.boldLabel);
                if (GUILayout.Button("삭제", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("그룹 몬스터 삭제", "선택한 몬스터 스폰 데이터를 삭제할까요?", "삭제", "취소"))
                    {
                        _waveExporter.RemoveMonsterSpawn(group, index);
                        _selectedWaveMonsterSpawnIndex = -1;
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        /// <summary>
        /// 몬스터 스폰 데이터의 상세 필드를 그립니다.
        /// </summary>
        /// <param name="scenario">소속 시나리오입니다.</param>
        /// <param name="monsterSpawn">편집할 몬스터 스폰 데이터입니다.</param>
        private void DrawWaveMonsterSpawnFields(
            MapWaveScenarioData scenario,
            MapWaveMonsterSpawnData monsterSpawn)
        {
            int monsterIndex = FindOptionIndexByUid(_monsterOptions, monsterSpawn.MonsterUid);
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                "몬스터",
                _monsterOptions,
                monsterIndex,
                (_, option) => monsterSpawn.MonsterUid = option.Data,
                noneText: "(몬스터 선택)");

            monsterSpawn.SpawnPointId = DrawSpawnPointIdPopup("스폰 포인트", scenario, monsterSpawn.SpawnPointId);
            monsterSpawn.Count = Mathf.Max(1, EditorGUILayout.IntField("생성 수", monsterSpawn.Count));
            monsterSpawn.SpawnIntervalSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("스폰 간격", monsterSpawn.SpawnIntervalSeconds));
            monsterSpawn.OffsetX = EditorGUILayout.FloatField("Offset X", monsterSpawn.OffsetX);
            monsterSpawn.OffsetY = EditorGUILayout.FloatField("Offset Y", monsterSpawn.OffsetY);
            monsterSpawn.OffsetZ = EditorGUILayout.FloatField("Offset Z", monsterSpawn.OffsetZ);
            monsterSpawn.IsFlip = EditorGUILayout.Toggle("좌우 반전", monsterSpawn.IsFlip);
            monsterSpawn.DefaultVisible = EditorGUILayout.Toggle("기본 보임", monsterSpawn.DefaultVisible);
            monsterSpawn.MoveStep = Mathf.Max(0f, EditorGUILayout.FloatField("MoveStep(0 기본값)", monsterSpawn.MoveStep));
            monsterSpawn.MoveSpeed = Mathf.Max(0f, EditorGUILayout.FloatField("MoveSpeed(0 기본값)", monsterSpawn.MoveSpeed));
            monsterSpawn.CanMoveX = EditorGUILayout.Toggle("X축 이동 가능", monsterSpawn.CanMoveX);
            monsterSpawn.CanMoveY = EditorGUILayout.Toggle("Y축 이동 가능", monsterSpawn.CanMoveY);
            monsterSpawn.MapVisibilityPolicy = DrawMapVisibilityPolicyField("맵 표시 정책", monsterSpawn.MapVisibilityPolicy);
        }

        /// <summary>
        /// 스폰 포인트 ID를 선택하는 드롭다운을 그리고 선택 결과를 반환합니다.
        /// </summary>
        /// <param name="label">필드 라벨입니다.</param>
        /// <param name="scenario">스폰 포인트 목록을 가진 시나리오입니다.</param>
        /// <param name="currentPointId">현재 선택된 포인트 ID입니다.</param>
        /// <returns>선택된 스폰 포인트 ID입니다.</returns>
        private static int DrawSpawnPointIdPopup(
            string label,
            MapWaveScenarioData scenario,
            int currentPointId)
        {
            if (scenario?.SpawnPoints == null || scenario.SpawnPoints.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(label, 0);
                }

                return 0;
            }

            List<int> ids = new List<int>();
            List<string> labels = new List<string>();
            for (int i = 0; i < scenario.SpawnPoints.Count; i++)
            {
                MapWaveSpawnPointData point = scenario.SpawnPoints[i];
                if (point == null || point.PointId <= 0)
                {
                    continue;
                }

                ids.Add(point.PointId);
                labels.Add($"Point {point.PointId} ({point.x:F1}, {point.y:F1}, {point.z:F1})");
            }

            if (ids.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(label, 0);
                }

                return 0;
            }

            int selectedIndex = ids.IndexOf(currentPointId);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(label, selectedIndex, labels.ToArray());
            nextIndex = Mathf.Clamp(nextIndex, 0, ids.Count - 1);
            return ids[nextIndex];
        }

        /// <summary>
        /// 다음 그룹 UID를 선택하는 드롭다운을 그리고 선택 결과를 반환합니다.
        /// 0은 Order 기준 다음 그룹을 의미합니다.
        /// </summary>
        /// <param name="label">필드 라벨입니다.</param>
        /// <param name="scenario">그룹 목록을 가진 시나리오입니다.</param>
        /// <param name="currentGroupUid">현재 선택된 다음 그룹 UID입니다.</param>
        /// <param name="selfGroupUid">현재 편집 중인 그룹 UID입니다.</param>
        /// <returns>선택된 다음 그룹 UID입니다.</returns>
        private static int DrawGroupUidPopup(
            string label,
            MapWaveScenarioData scenario,
            int currentGroupUid,
            int selfGroupUid)
        {
            List<int> ids = new List<int> { 0 };
            List<string> labels = new List<string> { "Order 기준 다음 그룹" };

            if (scenario?.Groups != null)
            {
                for (int i = 0; i < scenario.Groups.Count; i++)
                {
                    MapWaveGroupData group = scenario.Groups[i];
                    if (group == null || group.GroupUid <= 0 || group.GroupUid == selfGroupUid)
                    {
                        continue;
                    }

                    ids.Add(group.GroupUid);
                    labels.Add($"Group {group.GroupUid} / Order {group.Order}");
                }
            }

            int selectedIndex = ids.IndexOf(currentGroupUid);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(label, selectedIndex, labels.ToArray());
            nextIndex = Mathf.Clamp(nextIndex, 0, ids.Count - 1);
            return ids[nextIndex];
        }

        /// <summary>
        /// 그룹 몬스터를 추가할 때 사용할 기본 스폰 포인트 ID를 계산합니다.
        /// 현재 선택된 포인트가 유효하면 그것을 사용하고, 없으면 첫 번째 포인트를 사용합니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <returns>기본 스폰 포인트 ID입니다.</returns>
        private int ResolveDefaultMonsterSpawnPointId(MapWaveScenarioData scenario)
        {
            if (scenario?.SpawnPoints == null || scenario.SpawnPoints.Count == 0)
            {
                return 0;
            }

            if (_selectedWaveSpawnPointId > 0 && _waveExporter.FindSpawnPoint(scenario, _selectedWaveSpawnPointId) != null)
            {
                return _selectedWaveSpawnPointId;
            }

            return scenario.SpawnPoints[0].PointId;
        }

        /// <summary>
        /// 시나리오 선택 드롭다운 옵션을 생성합니다.
        /// </summary>
        /// <param name="scenarios">웨이브 시나리오 목록입니다.</param>
        /// <returns>검색 가능한 드롭다운 옵션 목록입니다.</returns>
        private static List<SearchableDropdownUtility.Option<int>> BuildWaveScenarioOptions(
            IReadOnlyList<MapWaveScenarioData> scenarios)
        {
            List<SearchableDropdownUtility.Option<int>> options = new List<SearchableDropdownUtility.Option<int>>();
            if (scenarios == null)
            {
                return options;
            }

            for (int i = 0; i < scenarios.Count; i++)
            {
                MapWaveScenarioData scenario = scenarios[i];
                if (scenario == null || scenario.ScenarioUid <= 0)
                {
                    continue;
                }

                string label = string.IsNullOrEmpty(scenario.Memo)
                    ? $"Scenario {scenario.ScenarioUid}"
                    : scenario.Memo;
                options.Add(new SearchableDropdownUtility.Option<int>(scenario.ScenarioUid.ToString(), label, scenario.ScenarioUid));
            }

            return options;
        }

        /// <summary>
        /// 스폰 포인트 선택 드롭다운 옵션을 생성합니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <returns>검색 가능한 드롭다운 옵션 목록입니다.</returns>
        private static List<SearchableDropdownUtility.Option<int>> BuildWaveSpawnPointOptions(MapWaveScenarioData scenario)
        {
            List<SearchableDropdownUtility.Option<int>> options = new List<SearchableDropdownUtility.Option<int>>();
            if (scenario?.SpawnPoints == null)
            {
                return options;
            }

            for (int i = 0; i < scenario.SpawnPoints.Count; i++)
            {
                MapWaveSpawnPointData point = scenario.SpawnPoints[i];
                if (point == null || point.PointId <= 0)
                {
                    continue;
                }

                string label = $"Point {point.PointId} ({point.x:F1}, {point.y:F1}, {point.z:F1})";
                options.Add(new SearchableDropdownUtility.Option<int>(point.PointId.ToString(), label, point.PointId));
            }

            return options;
        }

        /// <summary>
        /// 웨이브 그룹 선택 드롭다운 옵션을 생성합니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <returns>검색 가능한 드롭다운 옵션 목록입니다.</returns>
        private static List<SearchableDropdownUtility.Option<int>> BuildWaveGroupOptions(MapWaveScenarioData scenario)
        {
            List<SearchableDropdownUtility.Option<int>> options = new List<SearchableDropdownUtility.Option<int>>();
            if (scenario?.Groups == null)
            {
                return options;
            }

            for (int i = 0; i < scenario.Groups.Count; i++)
            {
                MapWaveGroupData group = scenario.Groups[i];
                if (group == null || group.GroupUid <= 0)
                {
                    continue;
                }

                string label = $"Group {group.GroupUid} / Order {group.Order}";
                options.Add(new SearchableDropdownUtility.Option<int>(group.GroupUid.ToString(), label, group.GroupUid));
            }

            return options;
        }

    }
}
