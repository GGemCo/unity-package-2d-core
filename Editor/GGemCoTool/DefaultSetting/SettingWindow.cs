using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 인트로 씬 설정 툴
    /// </summary>
    public class SettingWindow : DefaultSceneEditor
    {
        private const string Title = "윈도우 셋팅하기";
        private TableWindow _tableWindow;
        private readonly Dictionary<int, UIWindow> _windowDict = new Dictionary<int, UIWindow>();
        
        [MenuItem(ConfigEditor.NameToolSettingWindow, false, (int)ConfigEditor.ToolOrdering.SettingWindow)]
        public static void ShowWindow()
        {
            GetWindow<SettingWindow>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _tableWindow = TableLoaderManager.LoadWindowTable();

            UIWindowManager uiWindowManager = CompatObjectFind.FindFirst<UIWindowManager>(true);
            if (uiWindowManager == null)
            {
                HelperLog.Error($"[{nameof(SceneEditorGame)}] {nameof(UIWindowManager)} 오브젝트 셋팅 실패: {nameof(UIWindowManager)} 를 생성하지 못 했습니다.");
                return;
            }

            RebuildWindowDict(uiWindowManager);
        }

        private void RebuildWindowDict(UIWindowManager uiWindowManager)
        {
            _windowDict.Clear();
            if (uiWindowManager == null || uiWindowManager.windowKeys == null)
            {
                return;
            }

            foreach (var windowKey in uiWindowManager.windowKeys)
            {
                if (windowKey == null || windowKey.uid <= 0 || windowKey.uiWindow == null)
                {
                    continue;
                }

                _windowDict[windowKey.uid] = windowKey.uiWindow;
            }
        }

        private static Dictionary<string, UIWindow> BuildSceneWindowNameMap()
        {
            var result = new Dictionary<string, UIWindow>(System.StringComparer.Ordinal);
            var windows = CompatObjectFind.FindAll<UIWindow>(true);
            if (windows == null)
            {
                return result;
            }

            foreach (var window in windows)
            {
                if (window == null)
                {
                    continue;
                }

                string windowName = window.gameObject.name;
                if (string.IsNullOrWhiteSpace(windowName))
                {
                    continue;
                }

                if (result.ContainsKey(windowName))
                {
                    Debug.LogWarning($"중복된 UIWindow 이름을 찾았습니다. name:{windowName}");
                    continue;
                }

                result.Add(windowName, window);
            }

            return result;
        }

        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNameGame))
            {
                EditorGUILayout.HelpBox($"게임 씬을 불러와 주세요.", MessageType.Error);
            }
            else
            {
                HelperEditorUI.OnGUITitle(Title);

                if (GUILayout.Button(Title))
                {
                    AddWindows();
                }
            }
        }

        private void AddWindows()
        {
            UIWindowManager uiWindowManager = CompatObjectFind.FindFirst<UIWindowManager>(true);
            if (uiWindowManager == null)
            {
                HelperLog.Error($"[{nameof(SceneEditorGame)}] {nameof(UIWindowManager)} 오브젝트 셋팅 실패: {nameof(UIWindowManager)} 를 생성하지 못 했습니다.");
                return;
            }

            if (_tableWindow == null)
            {
                _tableWindow = TableLoaderManager.LoadWindowTable();
                if (_tableWindow == null)
                {
                    HelperLog.Error($"[{nameof(SceneEditorGame)}] {nameof(TableWindow)} 로드 실패");
                    return;
                }
            }

            RebuildWindowDict(uiWindowManager);
            var sceneWindowMap = BuildSceneWindowNameMap();
            var mapDict = _tableWindow.GetDatas();
            if (mapDict == null)
            {
                return;
            }

            int addedOrUpdatedCount = 0;
            int skippedCount = 0;

            Undo.RecordObject(uiWindowManager, "Sync UIWindowManager Window Keys");

            foreach (var kv in mapDict)
            {
                var info = kv.Value;
                if (info == null || info.Uid <= 0)
                {
                    continue;
                }

                if (_windowDict.TryGetValue(info.Uid, out var existingWindow) && existingWindow != null)
                {
                    skippedCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(info.PrefabName))
                {
                    skippedCount++;
                    continue;
                }

                if (!sceneWindowMap.TryGetValue(info.PrefabName, out UIWindow uiWindow) || uiWindow == null)
                {
                    skippedCount++;
                    continue;
                }

                bool changed = uiWindowManager.UpsertWindowKey(info.Uid, uiWindow);
                if (!changed)
                {
                    skippedCount++;
                    continue;
                }

                _windowDict[info.Uid] = uiWindow;
                addedOrUpdatedCount++;
            }

            if (addedOrUpdatedCount > 0)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(uiWindowManager);
                EditorUtility.SetDirty(uiWindowManager);
                EditorSceneManager.MarkSceneDirty(uiWindowManager.gameObject.scene);
            }

            Debug.Log($"UIWindowManager windowKeys 동기화 완료. addedOrUpdated:{addedOrUpdatedCount}, skipped:{skippedCount}");
        }
    }
}