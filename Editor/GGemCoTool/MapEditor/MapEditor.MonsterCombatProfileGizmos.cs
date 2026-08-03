using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵에 배치된 몬스터의 전투 프로필 공간 범위를 SceneView에 표시합니다.
    /// </summary>
    public sealed partial class MapEditor
    {
        private static readonly Color MonsterDetectionRangeColor = new Color(1f, 0.85f, 0.1f, 0.95f);
        private static readonly Color MonsterDetectionExitRangeColor = new Color(1f, 0.45f, 0.1f, 0.85f);
        private static readonly Color MonsterBasicAttackRangeColor = new Color(1f, 0.15f, 0.15f, 0.95f);
        private static readonly Color MonsterPreferredRangeColor = new Color(0.1f, 0.9f, 0.85f, 0.9f);
        private static readonly Color MonsterChaseRangeColor = new Color(0.15f, 0.55f, 1f, 0.9f);
        private static readonly Color MonsterSoftLeashRangeColor = new Color(0.65f, 0.3f, 1f, 0.85f);
        private static readonly Color MonsterHardLeashRangeColor = new Color(1f, 0.2f, 0.8f, 0.9f);
        private static readonly Color MonsterReturnStopRangeColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        private static readonly Color MonsterEncounterAssistRangeColor = new Color(0.2f, 1f, 0.35f, 0.9f);
        private static readonly Color MonsterProfileWarningColor = new Color(1f, 0.2f, 0.2f, 1f);

        private readonly List<Monster> _monsterCombatProfilePreviewTargets = new List<Monster>();
        private readonly Dictionary<int, string> _monsterCombatProfileLabelCache = new Dictionary<int, string>();

        private bool _drawMonsterCombatProfileGizmos = true;
        private bool _drawAllMonsterCombatProfileGizmos;
        private bool _drawMonsterDetectionRanges = true;
        private bool _drawMonsterCombatRanges = true;
        private bool _drawMonsterLeashRanges = true;
        private bool _drawMonsterEncounterAssistRange;
        private bool _drawMonsterCombatProfileSummary = true;
        private bool _monsterCombatProfileTargetCacheDirty = true;

        /// <summary>
        /// 전투 프로필 기즈모와 TableEditor 저장 알림 콜백을 등록합니다.
        /// 도메인 리로드나 창 재활성화 시 중복 등록되지 않도록 기존 콜백을 먼저 제거합니다.
        /// </summary>
        private void RegisterMonsterCombatProfilePreview()
        {
            SceneView.duringSceneGui -= DrawMonsterCombatProfileSceneGizmos;
            SceneView.duringSceneGui += DrawMonsterCombatProfileSceneGizmos;
            Selection.selectionChanged -= OnMonsterCombatProfileSelectionChanged;
            Selection.selectionChanged += OnMonsterCombatProfileSelectionChanged;
            EditorApplication.hierarchyChanged -= OnMonsterCombatProfileHierarchyChanged;
            EditorApplication.hierarchyChanged += OnMonsterCombatProfileHierarchyChanged;
            TableEditorChangeNotifier.TableSaved -= OnTableEditorTableSaved;
            TableEditorChangeNotifier.TableSaved += OnTableEditorTableSaved;
            InvalidateMonsterCombatProfilePreview();
        }

        /// <summary>
        /// 전투 프로필 기즈모와 관련 Editor 콜백을 해제합니다.
        /// </summary>
        private void UnregisterMonsterCombatProfilePreview()
        {
            SceneView.duringSceneGui -= DrawMonsterCombatProfileSceneGizmos;
            Selection.selectionChanged -= OnMonsterCombatProfileSelectionChanged;
            EditorApplication.hierarchyChanged -= OnMonsterCombatProfileHierarchyChanged;
            TableEditorChangeNotifier.TableSaved -= OnTableEditorTableSaved;
            _monsterCombatProfilePreviewTargets.Clear();
            _monsterCombatProfileLabelCache.Clear();
            _monsterCombatProfileTargetCacheDirty = true;
        }

        /// <summary>
        /// 몬스터 전투 프로필 SceneView 표시 옵션을 그립니다.
        /// </summary>
        private void DrawMonsterCombatProfileGizmoOptions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("전투 프로필 영역", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                _drawMonsterCombatProfileGizmos = EditorGUILayout.ToggleLeft(
                    "전투 프로필 영역 표시",
                    _drawMonsterCombatProfileGizmos);

                using (new EditorGUI.DisabledScope(!_drawMonsterCombatProfileGizmos))
                {
                    _drawAllMonsterCombatProfileGizmos = EditorGUILayout.ToggleLeft(
                        "현재 맵의 모든 몬스터 표시",
                        _drawAllMonsterCombatProfileGizmos);
                    _drawMonsterDetectionRanges = EditorGUILayout.ToggleLeft(
                        "감지 및 감지 해제 범위",
                        _drawMonsterDetectionRanges);
                    _drawMonsterCombatRanges = EditorGUILayout.ToggleLeft(
                        "기본 공격 및 선호 거리",
                        _drawMonsterCombatRanges);
                    _drawMonsterLeashRanges = EditorGUILayout.ToggleLeft(
                        "추적, Leash 및 귀환 정지 범위",
                        _drawMonsterLeashRanges);
                    _drawMonsterEncounterAssistRange = EditorGUILayout.ToggleLeft(
                        "Encounter 지원 범위",
                        _drawMonsterEncounterAssistRange);
                    _drawMonsterCombatProfileSummary = EditorGUILayout.ToggleLeft(
                        "선택 몬스터 프로필 요약",
                        _drawMonsterCombatProfileSummary);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    InvalidateMonsterCombatProfilePreview();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.HelpBox(
                    "노랑 감지 / 주황 감지 해제 / 빨강 공격 / 청록 선호 / 파랑 추적 / " +
                    "보라·자홍 Leash / 회색 귀환 정지 / 초록 Encounter\n" +
                    "범위가 0인 감지/공격 값은 실제 공격 Collider를 사용하는 런타임 보정값으로 표시됩니다.",
                    MessageType.None);
            }
        }

        /// <summary>
        /// 전투 프로필 테이블과 SceneView 표시 대상 캐시를 무효화합니다.
        /// </summary>
        private void InvalidateMonsterCombatProfilePreview()
        {
            _monsterCombatProfileTargetCacheDirty = true;
            _monsterCombatProfileLabelCache.Clear();
        }

        /// <summary>
        /// TableEditor에서 monster_combat_profile 저장이 완료되면 MapEditor의 테이블과 기즈모를 즉시 갱신합니다.
        /// </summary>
        /// <param name="tableKey">저장된 테이블의 고유 키입니다.</param>
        private void OnTableEditorTableSaved(string tableKey)
        {
            if (!string.Equals(
                    tableKey,
                    ConfigAddressableTable.MonsterCombatProfile,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _tableMonsterCombatProfile = TableLoaderManager.LoadMonsterCombatProfileTable(forceReload: true);
            RebuildPopupCaches();
            InvalidateMonsterCombatProfilePreview();
            Repaint();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Selection 변경 시 선택 몬스터 기즈모를 다시 그립니다.
        /// </summary>
        private void OnMonsterCombatProfileSelectionChanged()
        {
            _monsterCombatProfileTargetCacheDirty = true;
            Repaint();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Hierarchy 변경 시 현재 맵의 몬스터 목록 캐시를 무효화합니다.
        /// </summary>
        private void OnMonsterCombatProfileHierarchyChanged()
        {
            InvalidateMonsterCombatProfilePreview();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// 현재 표시 설정에 맞는 몬스터의 전투 프로필 공간 범위를 SceneView에 그립니다.
        /// </summary>
        /// <param name="sceneView">현재 렌더링 중인 SceneView입니다.</param>
        private void DrawMonsterCombatProfileSceneGizmos(SceneView sceneView)
        {
            if (!IsMonsterPlacementTabActive ||
                !_drawMonsterCombatProfileGizmos ||
                _defaultMap == null ||
                _tableMonster == null ||
                _tableMonsterCombatProfile == null)
            {
                return;
            }

            CollectMonsterCombatProfilePreviewTargets();
            if (_monsterCombatProfilePreviewTargets.Count <= 0)
            {
                return;
            }

            Color previousColor = Handles.color;
            CompareFunction previousZTest = Handles.zTest;

            try
            {
                Handles.zTest = CompareFunction.Always;
                for (int i = 0; i < _monsterCombatProfilePreviewTargets.Count; i++)
                {
                    Monster monster = _monsterCombatProfilePreviewTargets[i];
                    if (!monster)
                    {
                        continue;
                    }

                    DrawMonsterCombatProfileGizmos(monster);
                }
            }
            finally
            {
                Handles.color = previousColor;
                Handles.zTest = previousZTest;
            }
        }

        /// <summary>
        /// 선택 표시 또는 전체 표시 정책에 따라 이번 SceneView 렌더링 대상 목록을 준비합니다.
        /// 전체 표시 목록은 Hierarchy가 바뀔 때만 다시 수집하여 반복 할당을 피합니다.
        /// </summary>
        private void CollectMonsterCombatProfilePreviewTargets()
        {
            if (!_monsterCombatProfileTargetCacheDirty)
            {
                return;
            }

            _monsterCombatProfilePreviewTargets.Clear();
            if (!_drawAllMonsterCombatProfileGizmos)
            {
                if (TryGetSelectedMonsterInCurrentMap(out Monster selectedMonster))
                {
                    _monsterCombatProfilePreviewTargets.Add(selectedMonster);
                }

                _monsterCombatProfileTargetCacheDirty = false;
                return;
            }

            Monster[] monsters = _defaultMap.GetComponentsInChildren<Monster>(true);
            for (int i = 0; i < monsters.Length; i++)
            {
                Monster monster = monsters[i];
                if (monster)
                {
                    _monsterCombatProfilePreviewTargets.Add(monster);
                }
            }

            _monsterCombatProfileTargetCacheDirty = false;
        }

        /// <summary>
        /// 단일 몬스터에 적용되는 프로필 UID와 런타임 보정 범위를 계산하여 기즈모를 그립니다.
        /// </summary>
        /// <param name="monster">범위를 표시할 맵 배치 몬스터입니다.</param>
        private void DrawMonsterCombatProfileGizmos(Monster monster)
        {
            int profileUid = ResolveMonsterCombatProfileUid(monster);
            bool isSelected = IsSelectedMonster(monster);
            if (profileUid <= 0)
            {
                if (isSelected && _drawMonsterCombatProfileSummary)
                {
                    DrawMonsterCombatProfileLabel(monster, null, profileUid, false);
                }

                return;
            }

            StruckTableMonsterCombatProfile tableData = _tableMonsterCombatProfile.GetDataByUid(profileUid);
            if (tableData == null)
            {
                if (isSelected && _drawMonsterCombatProfileSummary)
                {
                    DrawMonsterCombatProfileLabel(monster, null, profileUid, true);
                }

                return;
            }

            MonsterCombatRangeProfile rangeProfile = MonsterCombatRangeProfile.Create(
                tableData,
                monster.colliderAttackRange);
            MonsterLeashProfile leashProfile = MonsterLeashProfile.Create(tableData);
            MonsterEncounterProfile encounterProfile = MonsterEncounterProfile.Create(tableData);
            Vector3 origin = monster.transform.position;

            if (_drawMonsterDetectionRanges)
            {
                DrawAxisAlignedRange(origin, rangeProfile.DetectionRangeX, rangeProfile.DetectionRangeY, MonsterDetectionRangeColor);
                DrawAxisAlignedRange(
                    origin,
                    rangeProfile.DetectionExitRangeX,
                    rangeProfile.DetectionExitRangeY,
                    MonsterDetectionExitRangeColor);
            }

            if (_drawMonsterCombatRanges)
            {
                DrawAxisAlignedRange(origin, rangeProfile.BasicAttackRangeX, rangeProfile.BasicAttackRangeY, MonsterBasicAttackRangeColor);
                DrawPreferredRange(origin, rangeProfile.PreferredRangeMin, rangeProfile.PreferredRangeMax, rangeProfile.BasicAttackRangeY);
            }

            if (_drawMonsterLeashRanges)
            {
                DrawCircularRange(origin, rangeProfile.ChaseRange, MonsterChaseRangeColor);
                DrawCircularRange(origin, leashProfile.SoftLeashRange, MonsterSoftLeashRangeColor);
                DrawCircularRange(origin, leashProfile.HardLeashRange, MonsterHardLeashRangeColor);
                if (leashProfile.IsEnabled)
                {
                    DrawCircularRange(origin, leashProfile.ReturnStopDistance, MonsterReturnStopRangeColor);
                }
            }

            if (_drawMonsterEncounterAssistRange)
            {
                DrawCircularRange(origin, encounterProfile.AssistRadius, MonsterEncounterAssistRangeColor);
            }

            if (isSelected && _drawMonsterCombatProfileSummary)
            {
                DrawMonsterCombatProfileLabel(monster, tableData, profileUid, false);
            }
        }

        /// <summary>
        /// 배치별 Override를 우선하고, 없으면 monster 테이블 기본값을 사용하여 최종 프로필 UID를 계산합니다.
        /// </summary>
        /// <param name="monster">프로필 UID를 확인할 몬스터입니다.</param>
        /// <returns>최종 CombatProfileUid이며 0이면 프로필을 사용하지 않습니다.</returns>
        private int ResolveMonsterCombatProfileUid(Monster monster)
        {
            if (!monster)
            {
                return 0;
            }

            CharacterRegenData regenData = monster.CharacterRegenData;
            if (regenData != null && regenData.HasCombatProfileUidOverride)
            {
                return Mathf.Max(0, regenData.CombatProfileUidOverride);
            }

            return GetMonsterTableCombatProfileUid(monster.uid);
        }

        /// <summary>
        /// 대상 몬스터 또는 그 하위 오브젝트가 현재 선택되어 있는지 확인합니다.
        /// </summary>
        /// <param name="monster">선택 여부를 확인할 몬스터입니다.</param>
        /// <returns>현재 Selection이 대상 몬스터에 속하면 <see langword="true"/>입니다.</returns>
        private static bool IsSelectedMonster(Monster monster)
        {
            if (!monster || !Selection.activeGameObject)
            {
                return false;
            }

            Transform selectedTransform = Selection.activeGameObject.transform;
            return selectedTransform == monster.transform || selectedTransform.IsChildOf(monster.transform);
        }

        /// <summary>
        /// X/Y 반경을 전체 폭과 높이로 변환하여 축 정렬 사각 범위를 그립니다.
        /// </summary>
        /// <param name="origin">범위 중심의 월드 좌표입니다.</param>
        /// <param name="rangeX">X축 반경입니다.</param>
        /// <param name="rangeY">Y축 반경입니다.</param>
        /// <param name="color">기즈모 선 색상입니다.</param>
        private static void DrawAxisAlignedRange(Vector3 origin, float rangeX, float rangeY, Color color)
        {
            if (rangeX <= 0f || rangeY <= 0f)
            {
                return;
            }

            Handles.color = color;
            Handles.DrawWireCube(origin, new Vector3(rangeX * 2f, rangeY * 2f, 0f));
        }

        /// <summary>
        /// 런타임의 수평 선호 거리 판정과 동일하게 몬스터 좌우의 거리 밴드를 그립니다.
        /// </summary>
        /// <param name="origin">범위 중심의 월드 좌표입니다.</param>
        /// <param name="minimumRange">선호 거리의 최소 수평 반경입니다.</param>
        /// <param name="maximumRange">선호 거리의 최대 수평 반경입니다.</param>
        /// <param name="verticalRange">선호 거리에서 허용하는 Y축 반경입니다.</param>
        private static void DrawPreferredRange(Vector3 origin, float minimumRange, float maximumRange, float verticalRange)
        {
            float clampedMinimum = Mathf.Max(0f, minimumRange);
            float clampedMaximum = Mathf.Max(clampedMinimum, maximumRange);
            if (clampedMaximum <= 0f || verticalRange <= 0f)
            {
                return;
            }

            float bandWidth = clampedMaximum - clampedMinimum;
            if (bandWidth <= 0f)
            {
                return;
            }

            float centerOffset = clampedMinimum + bandWidth * 0.5f;
            Vector3 size = new Vector3(bandWidth, verticalRange * 2f, 0f);
            Handles.color = MonsterPreferredRangeColor;
            Handles.DrawWireCube(origin + Vector3.right * centerOffset, size);
            Handles.DrawWireCube(origin + Vector3.left * centerOffset, size);
        }

        /// <summary>
        /// 양수 반경을 가진 원형 공간 범위를 그립니다.
        /// </summary>
        /// <param name="origin">원의 중심 월드 좌표입니다.</param>
        /// <param name="radius">표시할 원의 반경입니다.</param>
        /// <param name="color">기즈모 선 색상입니다.</param>
        private static void DrawCircularRange(Vector3 origin, float radius, Color color)
        {
            if (radius <= 0f)
            {
                return;
            }

            Handles.color = color;
            Handles.DrawWireDisc(origin, Vector3.forward, radius);
        }

        /// <summary>
        /// 선택한 몬스터 위에 적용 프로필과 비공간 정책 요약을 표시합니다.
        /// 반복 SceneView 렌더링에서 문자열을 다시 만들지 않도록 인스턴스별 라벨을 캐시합니다.
        /// </summary>
        /// <param name="monster">라벨을 표시할 몬스터입니다.</param>
        /// <param name="tableData">적용된 전투 프로필 행이며 누락되었으면 null입니다.</param>
        /// <param name="profileUid">최종 적용 프로필 UID입니다.</param>
        /// <param name="isMissing">양수 UID가 테이블에 존재하지 않는지 여부입니다.</param>
        private void DrawMonsterCombatProfileLabel(
            Monster monster,
            StruckTableMonsterCombatProfile tableData,
            int profileUid,
            bool isMissing)
        {
            int instanceId = monster.GetInstanceID();
            if (!_monsterCombatProfileLabelCache.TryGetValue(instanceId, out string label))
            {
                label = BuildMonsterCombatProfileLabel(tableData, profileUid, isMissing);
                _monsterCombatProfileLabelCache[instanceId] = label;
            }

            Color previousColor = Handles.color;
            if (isMissing)
            {
                Handles.color = MonsterProfileWarningColor;
            }

            float offset = HandleUtility.GetHandleSize(monster.transform.position) * 0.35f;
            Handles.Label(monster.transform.position + Vector3.up * offset, label);
            Handles.color = previousColor;
        }

        /// <summary>
        /// 전투 프로필의 식별 정보와 영역으로 표현하기 어려운 주요 정책을 요약합니다.
        /// </summary>
        /// <param name="tableData">적용된 전투 프로필 행이며 누락되었으면 null입니다.</param>
        /// <param name="profileUid">최종 적용 프로필 UID입니다.</param>
        /// <param name="isMissing">양수 UID가 테이블에 존재하지 않는지 여부입니다.</param>
        /// <returns>SceneView에 표시할 여러 줄 요약 문자열입니다.</returns>
        private static string BuildMonsterCombatProfileLabel(
            StruckTableMonsterCombatProfile tableData,
            int profileUid,
            bool isMissing)
        {
            if (profileUid <= 0)
            {
                return "Combat Profile: 사용 안 함";
            }

            if (isMissing || tableData == null)
            {
                return $"Combat Profile: {profileUid}\n테이블 행을 찾을 수 없습니다.";
            }

            return
                $"Profile {tableData.Uid}: {tableData.Name}\n" +
                $"Threat {tableData.DetectionThreat:F2} / Switch x{tableData.TargetSwitchThreatRatio:F2}\n" +
                $"Encounter {tableData.MaxEncounterAssistCount} / Slot {tableData.AttackSlotType} {tableData.MaxConcurrentAttackers}";
        }
    }
}
