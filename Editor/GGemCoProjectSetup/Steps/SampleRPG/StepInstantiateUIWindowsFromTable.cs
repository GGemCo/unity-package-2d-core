#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// WindowTable을 읽어, 지정 Canvas 하위에 UIWindow 프리팹을 일괄 인스턴스합니다.
    /// - 이미 동일 이름의 오브젝트가 있으면 재사용 또는 덮어쓰기(옵션)
    /// - 프리팹 인스턴스 시 '완전 언팩'(옵션) 혹은 프리팹 링크 유지 선택
    /// - 마지막에 UIWindowManager(SetUIWindow)에 등록(옵션)
    /// - 성능: 에셋 검색은 사전 인덱싱(경로 필터), 반복 할당 최소화
    /// </summary>
    public sealed class StepInstantiateUIWindowsFromTable : SetupStepBase
    {
        [Header("Sources")]
        [Tooltip("WindowTable 로더. 기본은 TableLoaderManager.LoadWindowTable()을 리플렉션으로 호출합니다.")]
        private readonly bool _useDefaultTableLoader = true;

        [Tooltip("직접 테이블 오브젝트를 넣고 싶다면 지정(테스트/커스텀용). 우선순위: directTable > default loader")]
        private UnityEngine.Object _directTable;

        [Header("Prefab Search")]
        [Tooltip("UIWindow 프리팹 루트 경로(여러 경로 지원). 예) Assets/_GGemCo/UI/Windows, Packages/com.ggemco.2d.core/...")]
        private readonly string[] _prefabSearchPaths = new[]
        {
            $"{ConfigEditor.PathUIWindow}",
        };

        /*
        [Header("Scene Targets")]
        [Tooltip("생성할 부모 Canvas 이름. 없으면 자동 생성(옵션).")]
        private string canvasName = "Canvas";

        [Tooltip("Canvas가 없을 때 자동 생성 여부")]
        private bool autoCreateCanvasIfMissing = true;

        [Header("Instantiation Options")]
        [Tooltip("동일 이름의 GameObject가 있으면 재사용(true) / 삭제 후 재생성(false)")]
        private bool reuseIfExists = true;

        [Tooltip("Prefab 인스턴스 후 완전 언팩할지 여부")]
        private bool unpackPrefabCompletely = true;

        [Tooltip("Table.UseInGame = true 인 항목만 생성")]
        private bool onlyUseInGame = true;

        [Tooltip("Table 내 UID가 0 이하인 항목은 건너뛰기")]
        private bool skipNonPositiveUid = true;

        [Header("Registration")]
        [Tooltip("마지막에 UIWindowManager.SetUIWindow(...) 등록 실행")]
        private bool registerToUIWindowManager = true;

        [Tooltip("UIWindowManager가 없으면 자동 생성")]
        private bool autoCreateUIWindowManagerIfMissing = true;

        [Header("Dry Run")]
        [Tooltip("실제 생성/파괴 없이 로그만 확인")]
        private bool dryRun = false;
        */
        
        // 캐시 용도
        private readonly Dictionary<string, GameObject> _prefabByName = new(StringComparer.Ordinal);

        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            // Canvas 검증
            // var canvas = GameObject.Find(canvasName);
            // if (!canvas && !autoCreateCanvasIfMissing)
            // {
            //     message = $"Canvas '{canvasName}' 를 찾을 수 없습니다. (autoCreateCanvasIfMissing=false)";
            //     return false;
            // }
            //
            // // 검색 경로 검증
            // if (prefabSearchPaths == null || prefabSearchPaths.Length == 0)
            // {
            //     message = "prefabSearchPaths 가 비어 있습니다.";
            //     return false;
            // }
            //
            // // Table 확보 가능성 검증(실제 로드는 Execute에서 재확인)
            // if (!useDefaultTableLoader && directTable == null)
            // {
            //     message = "테이블 로더를 사용하지 않고 directTable도 지정하지 않았습니다.";
            //     return false;
            // }

            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var sceneEditorGame = ScriptableObject.CreateInstance<SceneEditorGame>();
            sceneEditorGame.SetupAllTestWindow(ctx);
            
            // // 1) 캔버스 확보
            // var canvas = GameObject.Find(canvasName);
            // if (!canvas && autoCreateCanvasIfMissing)
            //     canvas = CreateCanvas(canvasName);
            //
            // if (!canvas)
            // {
            //     ctx.Logger.Error($"Canvas '{canvasName}' 를 찾거나 생성하지 못했습니다.");
            //     return;
            // }
            //
            // // 2) WindowTable 로드
            // var table = ResolveWindowTable(ctx);
            // if (table == null)
            // {
            //     ctx.Logger.Warn("WindowTable 로드 실패. 스텝을 스킵합니다.");
            //     return;
            // }
            //
            // // 3) 프리팹 인덱싱(성능 최적화)
            // IndexPrefabsOnce(ctx);
            //
            // // 4) 순회 준비
            // var windows = new List<UIWindow> { null }; // index 0 = null 유지(팀 규칙)
            // int created = 0, reused = 0, skipped = 0, missingPrefab = 0, destroyed = 0;
            //
            // // Table API 가정(팀 기존 코드 기반):
            // // - table.GetDatas(): KeyValuePairs (uid -> dataRef)
            // // - table.GetDataByUid(uid): { Uid, UseInGame, PrefabName, ... }
            // var datas = SafeGetDatas(table);
            // if (datas == null)
            // {
            //     ctx.Logger.Warn("table.GetDatas() 결과가 없습니다.");
            //     return;
            // }
            //
            // foreach (var row in datas)
            // {
            //     var info = SafeGetDataByUid(table, row.Key);
            //     if (info == null)
            //     {
            //         skipped++;
            //         windows.Add(null);
            //         continue;
            //     }
            //
            //     // 필터링
            //     if (skipNonPositiveUid && !HasPositiveUid(info)) { skipped++; windows.Add(null); continue; }
            //     if (onlyUseInGame && !IsUseInGame(info))         { skipped++; windows.Add(null); continue; }
            //
            //     // 프리팹 이름
            //     var prefabName = GetPrefabName(info);
            //     if (string.IsNullOrEmpty(prefabName))
            //     {
            //         skipped++;
            //         windows.Add(null);
            //         continue;
            //     }
            //
            //     // 씬에 동일 이름 존재?
            //     var existGo = GameObject.Find(prefabName);
            //     if (existGo)
            //     {
            //         if (reuseIfExists)
            //         {
            //             reused++;
            //             windows.Add(existGo.GetComponent<UIWindow>());
            //             continue;
            //         }
            //         else
            //         {
            //             if (!dryRun)
            //                 UnityEngine.Object.DestroyImmediate(existGo);
            //             destroyed++;
            //             // 이후 새로 생성
            //         }
            //     }
            //
            //     // 프리팹 찾기
            //     var prefab = FindPrefabByName(prefabName);
            //     if (!prefab)
            //     {
            //         missingPrefab++;
            //         windows.Add(null);
            //         continue;
            //     }
            //
            //     // 인스턴스
            //     GameObject go = null;
            //     if (!dryRun)
            //     {
            //         go = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
            //         if (!go)
            //         {
            //             ctx.Logger.Warn($"Instantiate 실패: {prefabName}");
            //             windows.Add(null);
            //             continue;
            //         }
            //
            //         go.name = prefabName; // 이름 표준화
            //
            //         if (unpackPrefabCompletely)
            //             PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            //     }
            //
            //     created++;
            //     windows.Add(go ? go.GetComponent<UIWindow>() : null);
            // }
            //
            // // 5) 등록(옵션)
            // if (registerToUIWindowManager)
            // {
            //     var manager = UnityEngine.Object.FindAnyObjectByType<UIWindowManager>();
            //     if (!manager && autoCreateUIWindowManagerIfMissing && !dryRun)
            //         manager = new GameObject(nameof(UIWindowManager)).AddComponent<UIWindowManager>();
            //
            //     if (manager)
            //     {
            //         if (!dryRun)
            //             manager.SetUIWindow(windows.ToArray());
            //     }
            //     else
            //     {
            //         ctx.Logger.Warn("UIWindowManager 가 씬에 없어 등록을 건너뜁니다.");
            //     }
            // }
            //
            // // 6) 요약 로그
            // ctx.Logger.Info(
            //     $"[UIWindows] created={created}, reused={reused}, destroyed={destroyed}, " +
            //     $"missingPrefab={missingPrefab}, skipped={skipped}, totalList={windows.Count - 1}");
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return go;
        }

        private UnityEngine.Object ResolveWindowTable(EditorSetupContext ctx)
        {
            // 1) directTable 우선
            if (_directTable) return _directTable;

            if (!_useDefaultTableLoader) return null;

            // 2) 기본 로더(TableLoaderManager.LoadWindowTable) 리플렉션
            var tLoader = Type.GetType("TableLoaderManager");
            if (tLoader == null)
            {
                ctx.Logger.Warn("TableLoaderManager 타입을 찾을 수 없습니다.");
                return null;
            }
            var mLoad = tLoader.GetMethod("LoadWindowTable",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (mLoad == null)
            {
                ctx.Logger.Warn("LoadWindowTable 메서드를 찾을 수 없습니다.");
                return null;
            }
            try
            {
                var table = mLoad.Invoke(null, null) as UnityEngine.Object;
                return table;
            }
            catch (Exception ex)
            {
                ctx.Logger.Error($"LoadWindowTable 호출 중 예외: {ex.Message}");
                return null;
            }
        }

        private IEnumerable<KeyValuePair<int, object>> SafeGetDatas(UnityEngine.Object table)
        {
            // table.GetDatas() : Dictionary<int, ???> 라고 가정
            var m = table.GetType().GetMethod("GetDatas",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (m == null) return null;

            var result = m.Invoke(table, null);
            if (result is System.Collections.IDictionary dict)
            {
                var list = new List<KeyValuePair<int, object>>(dict.Count);
                foreach (System.Collections.DictionaryEntry e in dict)
                    list.Add(new KeyValuePair<int, object>((int)e.Key, e.Value));
                return list;
            }
            return null;
        }

        private object SafeGetDataByUid(UnityEngine.Object table, int uid)
        {
            // table.GetDataByUid(int)
            var m = table.GetType().GetMethod("GetDataByUid",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return m?.Invoke(table, new object[] { uid });
        }

        private static bool HasPositiveUid(object info)
        {
            var p = info.GetType().GetProperty("Uid");
            if (p == null) return true;
            var v = p.GetValue(info, null);
            return v is not int i || (i > 0);
        }

        private static bool IsUseInGame(object info)
        {
            var p = info.GetType().GetProperty("UseInGame");
            if (p == null) return true;
            var v = p.GetValue(info, null);
            return v is not bool b || b;
        }

        private static string GetPrefabName(object info)
        {
            var p = info.GetType().GetProperty("PrefabName");
            if (p == null) return null;
            var v = p.GetValue(info, null);
            return v as string;
        }

        private void IndexPrefabsOnce(EditorSetupContext ctx)
        {
            _prefabByName.Clear();

            foreach (var root in _prefabSearchPaths)
            {
                if (string.IsNullOrEmpty(root)) continue;
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!prefab) continue;
                    var name = prefab.name;
                    _prefabByName.TryAdd(name, prefab);
                }
            }

            ctx.Logger.Info($"[UIWindows] Prefab indexed: {_prefabByName.Count} (paths={string.Join(", ", _prefabSearchPaths ?? Array.Empty<string>())})");
        }

        private GameObject FindPrefabByName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            if (_prefabByName.TryGetValue(prefabName, out var go)) return go;

            // 혹시 검색 경로 밖에 있을 경우를 대비해 1회 보조 검색
            var guid = AssetDatabase.FindAssets($"t:Prefab {prefabName}").FirstOrDefault();
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab || prefab.name != prefabName) return null;
            _prefabByName[prefabName] = prefab;
            return prefab;
        }
    }
}
#endif
