using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingDefaultScene
    {
        private const string Title = "Scene 추가하기";
        // 새로 생성 시 기본 템플릿으로 빈 씬을 만든다
        private const bool CreateEmptySceneIfMissing = true;

        public void OnGUI()
        {
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                CreateDefaultScene();
            }
        }

        public void CreateDefaultScene(EditorSetupContext ctx = null)
        {
            // 1) 씬 보장(없으면 생성)
            var preIntro = EnsureScene(ConfigDefine.SceneNamePreIntro, ctx);
            var loading  = EnsureScene(ConfigDefine.SceneNameLoading, ctx);
            var intro    = EnsureScene(ConfigDefine.SceneNameIntro, ctx);
            var game     = EnsureScene(ConfigDefine.SceneNameGame, ctx);

            // Step_CreateDefaultScenes.Execute(...)
            if (ctx != null)
            {
                ctx.SetShared(ConfigDefine.SceneNamePreIntro, preIntro);
                ctx.SetShared(ConfigDefine.SceneNameLoading, loading);
                ctx.SetShared(ConfigDefine.SceneNameIntro, intro);
                ctx.SetShared(ConfigDefine.SceneNameGame, game);
            }

            if (ctx == null)
            {
                AssetDatabase.SaveAssets();
            }

            // 3) 빌드세팅에 등록(중복은 스킵)
            AddToBuildSettings(preIntro, ctx);
            AddToBuildSettings(loading,  ctx);
            AddToBuildSettings(intro,    ctx);
            AddToBuildSettings(game,     ctx);
        }
        
        private SceneAsset EnsureScene(string key, EditorSetupContext ctx = null)
        {
            string path = ConfigDefine.PathSceneAsset.GetValueOrDefault(key);
            if (string.IsNullOrEmpty(path))
            {
                HelperLog.Error($"[Scene] 경로가 잘 못 되었습니다. {key} @ {path}");
                return null;
            }
            if (File.Exists(path))
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                HelperLog.Info($"[Scene] 이미 존재하는 씬입니다. {key} @ {path}");
                return scene;
            }

            if (!path.EndsWith(".unity"))
            {
                HelperLog.Error($"[Scene] 파일 확장자가 잘 못 되었습니다. 경로: {path}");
                return null;
            }

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());

            if (!File.Exists(path) && CreateEmptySceneIfMissing)
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(newScene, path))
                {
                    HelperLog.Error($"[Scene] 저장 실패: {path}");
                }
                else
                {
                    HelperLog.Info($"[Scene] 생성 완료: {key} @ {path}");
                }
                AssetDatabase.Refresh();
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        private static void AddToBuildSettings(SceneAsset scene, EditorSetupContext ctx = null)
        {
            if (scene == null) return;
            var path = AssetDatabase.GetAssetPath(scene);
            var list = EditorBuildSettings.scenes?.ToList() ?? new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (list.Exists(s => s.path == path))
                return;

            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            HelperLog.Info($"[BuildScenes] 씬 추가: {path}");
        }
    }
}