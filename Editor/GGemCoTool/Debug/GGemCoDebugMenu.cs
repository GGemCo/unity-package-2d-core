// Menu: GGemCoTool > Debug > (각 HUD)
// Desc: 각 HUD On/Off, EditorPrefs로 상태 저장, 씬/플레이모드 진입 시 자동 반영

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GGemCo2DCore;
using GGemCo2DCoreEditor;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

[InitializeOnLoad] // 에디터 로드시 자동 초기화
public static class GGemCoDebugMenu
{
    private const string RootName = "GGemCoDebug";
    private const string PTilemap = "GGemCo.Debug.HUD.Tilemap";
    private const string PFps = "GGemCo.Debug.HUD.Fps";
    private const string PPh2D = "GGemCo.Debug.HUD.Physics2D";
    private const string PMem = "GGemCo.Debug.HUD.Memory";

    static GGemCoDebugMenu()
    {
        // 에디터 구동/도메인 리로드/씬 오픈 시 반영
        EditorApplication.delayCall += ApplyAllFromPrefs;
        EditorSceneManager.sceneOpened += (_, __) => ApplyAllFromPrefs();

        // ★ 플레이모드 진입 시 즉시 반영
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // EnteredPlayMode 직후, GameView가 활성화된 프레임에 적용
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // 즉시 1회 적용
            ApplyAllFromPrefs();

            // 다음 에디터 틱에 한 번 더 리페인트(도메인 리로드/씬 재로딩 케이스 대비)
            EditorApplication.delayCall += () =>
            {
                ApplyAllFromPrefs();
                RepaintAndDirty();
            };
        }
    }

    // ----- 메뉴 항목 -----
    [MenuItem(ConfigEditor.NameToolTilemapDrawCall, false, (int)ConfigEditor.ToolOrdering.DebugTilemapDrawCall)]
    private static void Toggle_Tilemap() => Toggle(PTilemap, Ensure<TilemapDrawCallHUD>);
    [MenuItem(ConfigEditor.NameToolTilemapDrawCall, true, (int)ConfigEditor.ToolOrdering.DebugTilemapDrawCall)]
    private static bool Validate_Tilemap() => ValidateCheck(PTilemap, ConfigEditor.NameToolTilemapDrawCall);

    [MenuItem(ConfigEditor.NameToolFps, false, (int)ConfigEditor.ToolOrdering.DebugFps)]
    private static void Toggle_Fps() => Toggle(PFps, Ensure<FpsHud>);
    [MenuItem(ConfigEditor.NameToolFps, true, (int)ConfigEditor.ToolOrdering.DebugFps)]
    private static bool Validate_Fps() => ValidateCheck(PFps, ConfigEditor.NameToolFps);

    [MenuItem(ConfigEditor.NameToolPhysics2D, false, (int)ConfigEditor.ToolOrdering.DebugPhysics2D)]
    private static void Toggle_Physics2D() => Toggle(PPh2D, Ensure<Physics2DHud>);
    [MenuItem(ConfigEditor.NameToolPhysics2D, true, (int)ConfigEditor.ToolOrdering.DebugPhysics2D)]
    private static bool Validate_Physics2D() => ValidateCheck(PPh2D, ConfigEditor.NameToolPhysics2D);

    [MenuItem(ConfigEditor.NameToolMemory, false, (int)ConfigEditor.ToolOrdering.DebugMemory)]
    private static void Toggle_Memory() => Toggle(PMem, Ensure<MemoryHud>);
    [MenuItem(ConfigEditor.NameToolMemory, true, (int)ConfigEditor.ToolOrdering.DebugMemory)]
    private static bool Validate_Memory() => ValidateCheck(PMem, ConfigEditor.NameToolMemory);

    // ----- 공통 로직 -----
    private static void Toggle<T>(string key, System.Func<GameObject, T> ensure) where T : Component
    {
        bool on = !EditorPrefs.GetBool(key, false);
        EditorPrefs.SetBool(key, on);
        Apply(key, on, ensure);
        RepaintAndDirty();
    }

    private static bool ValidateCheck(string key, string menuPath)
    {
        bool on = EditorPrefs.GetBool(key, false);
        Menu.SetChecked(menuPath, on);
        return true;
    }

    private static void ApplyAllFromPrefs()
    {
        Apply(PTilemap, EditorPrefs.GetBool(PTilemap, false), Ensure<TilemapDrawCallHUD>);
        Apply(PFps,     EditorPrefs.GetBool(PFps,     false), Ensure<FpsHud>);
        Apply(PPh2D,    EditorPrefs.GetBool(PPh2D,    false), Ensure<Physics2DHud>);
        Apply(PMem,     EditorPrefs.GetBool(PMem,     false), Ensure<MemoryHud>);
        RepaintAndDirty();
    }

    private static void Apply<T>(string key, bool on, System.Func<GameObject, T> ensure) where T : Component
    {
        var go = GetOrCreateRoot();
        var comp = go.GetComponentInChildren<T>(true);
        if (!comp && on) comp = ensure(go);
        if (comp) comp.gameObject.SetActive(on);
        // 플레이 중에는 씬 더티 표시를 하지 않음
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static GameObject GetOrCreateRoot()
    {
        // DDOL 영역 포함, 비활성 포함하여 타입으로 탐색
        var existing = Object.FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);
        if (existing != null)
            return existing.gameObject;

        // 없으면 새로 생성
        var root = new GameObject(RootName);
        // var comp = root.AddComponent<GGemCoDebugHudRoot>();

        // 플레이 중이면 즉시 DDOL 전환(씬 로드시 파괴 방지)
        if (Application.isPlaying)
            Object.DontDestroyOnLoad(root);

        return root;
    }

    private static T Ensure<T>(GameObject parent) where T : Component
    {
        var child = new GameObject(typeof(T).Name);
        child.transform.SetParent(parent.transform);
        return child.AddComponent<T>();
    }

    private static void RepaintAndDirty()
    {
        // 씬 더티 표시는 Apply 쪽에서 처리
        // GameView 즉시 리페인트
        var gvType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
        if (gvType != null)
        {
            foreach (var gv in Resources.FindObjectsOfTypeAll(gvType))
                gvType.GetMethod("Repaint")?.Invoke(gv, null);
        }
    }
}
