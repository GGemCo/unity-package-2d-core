using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 서비스용 Settings와 작업자별 개발용 Settings 프로파일을 관리하는 EditorWindow입니다.
    /// </summary>
    public class LocalSettingsProfileWindow : EditorWindow
    {
        private const string WindowTitle = "Settings 프로파일";
        private Vector2 _scrollPosition;
        private readonly List<string> _messages = new List<string>();

        /// <summary>
        /// Settings 프로파일 관리 창을 엽니다.
        /// </summary>
        [MenuItem("GGemCoTool/개발툴/Settings 프로파일", priority = (int)ConfigEditor.ToolOrdering.Development)]
        public static void Open()
        {
            LocalSettingsProfileWindow window = GetWindow<LocalSettingsProfileWindow>(WindowTitle);
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        /// <summary>
        /// Settings 프로파일 선택 및 개발용 에셋 관리 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Settings 프로파일", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Service는 Addressables에 등록된 서비스용 Settings를 사용합니다. Development는 현재 작업자 로컬 Settings가 있으면 우선 사용하고, 없으면 서비스용으로 fallback 합니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            SettingsProfileKind profile = (SettingsProfileKind)EditorGUILayout.EnumPopup("현재 프로파일", SettingsProfileEditorPrefs.CurrentProfile);
            string workerName = EditorGUILayout.TextField("작업자 이름", SettingsProfileEditorPrefs.WorkerName);
            if (EditorGUI.EndChangeCheck())
            {
                SettingsProfileEditorPrefs.CurrentProfile = profile;
                SettingsProfileEditorPrefs.WorkerName = workerName;
                Repaint();
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("개발용 Settings 폴더", SettingsProfileEditorPrefs.GetCurrentWorkerRoot());
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("개발용 Settings 생성/갱신", GUILayout.Height(28f)))
                {
                    CloneAddressableSettingsToLocal(overwriteExisting: true);
                }

                if (GUILayout.Button("폴더 열기", GUILayout.Height(28f), GUILayout.Width(110f)))
                {
                    OpenCurrentWorkerFolder();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Addressables 등록 여부 검사", GUILayout.Height(24f)))
                {
                    ValidateLocalSettingsAreNotAddressable();
                }

                if (GUILayout.Button("메시지 지우기", GUILayout.Height(24f), GUILayout.Width(110f)))
                {
                    _messages.Clear();
                }
            }

            EditorGUILayout.Space(8f);
            DrawMessages();
        }

        /// <summary>
        /// Addressables에 등록된 서비스용 Settings 에셋을 현재 작업자 로컬 폴더로 복제합니다.
        /// </summary>
        /// <param name="overwriteExisting">기존 개발용 Settings가 있을 때 서비스용 값으로 덮어쓸지 여부입니다.</param>
        private void CloneAddressableSettingsToLocal(bool overwriteExisting)
        {
            _messages.Clear();
            AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (!addressableSettings)
            {
                AddMessage("Addressable 설정을 찾을 수 없습니다.", MessageType.Error);
                return;
            }

            SettingsProfileEditorPrefs.EnsureCurrentWorkerDirectory();

            int created = 0;
            int updated = 0;
            int skipped = 0;

            foreach (AddressableAssetEntry entry in EnumerateSettingsEntries(addressableSettings))
            {
                ScriptableObject source = LoadSourceSettings(entry);
                if (!source)
                {
                    skipped++;
                    continue;
                }

                string destinationPath = SettingsProfileEditorPrefs.GetDevelopmentAssetPath(entry.address);
                ScriptableObject destination = AssetDatabase.LoadAssetAtPath<ScriptableObject>(destinationPath);
                if (destination)
                {
                    if (!overwriteExisting)
                    {
                        skipped++;
                        continue;
                    }

                    if (destination.GetType() != source.GetType())
                    {
                        AddMessage($"타입이 달라 갱신하지 않았습니다. key={entry.address}, source={source.GetType().Name}, local={destination.GetType().Name}", MessageType.Warning);
                        skipped++;
                        continue;
                    }

                    Undo.RecordObject(destination, "Update Local Development Settings");
                    EditorUtility.CopySerialized(source, destination);
                    destination.name = Path.GetFileNameWithoutExtension(destinationPath);
                    EditorUtility.SetDirty(destination);
                    updated++;
                    continue;
                }

                ScriptableObject clone = Object.Instantiate(source);
                clone.name = Path.GetFileNameWithoutExtension(destinationPath);
                AssetDatabase.CreateAsset(clone, destinationPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddMessage($"개발용 Settings 생성/갱신 완료: 생성 {created}개, 갱신 {updated}개, 건너뜀 {skipped}개", MessageType.Info);
        }

        /// <summary>
        /// Addressables에 등록된 서비스용 Settings 후보 엔트리를 순회합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <returns>Settings ScriptableObject로 판단되는 Addressable 엔트리 목록입니다.</returns>
        private static IEnumerable<AddressableAssetEntry> EnumerateSettingsEntries(AddressableAssetSettings settings)
        {
            if (settings == null)
                yield break;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.address))
                        continue;

                    if (!entry.address.StartsWith(ConfigDefine.NameSDK + "_"))
                        continue;

                    string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(assetPath) || !assetPath.Contains("/Settings/"))
                        continue;

                    if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) == null)
                        continue;

                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Addressables 엔트리에서 서비스용 Settings ScriptableObject를 로드합니다.
        /// </summary>
        /// <param name="entry">서비스용 Settings Addressables 엔트리입니다.</param>
        /// <returns>로드된 ScriptableObject입니다.</returns>
        private ScriptableObject LoadSourceSettings(AddressableAssetEntry entry)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            ScriptableObject source = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (!source)
            {
                AddMessage($"서비스용 Settings를 찾을 수 없습니다. key={entry.address}, path={assetPath}", MessageType.Warning);
            }

            return source;
        }

        /// <summary>
        /// 현재 작업자 로컬 Settings가 실수로 Addressables에 등록되어 있는지 검사합니다.
        /// </summary>
        private void ValidateLocalSettingsAreNotAddressable()
        {
            _messages.Clear();
            AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (!addressableSettings)
            {
                AddMessage("Addressable 설정을 찾을 수 없습니다.", MessageType.Error);
                return;
            }

            string root = SettingsProfileEditorPrefs.GetCurrentWorkerRoot();
            if (!AssetDatabase.IsValidFolder(root))
            {
                AddMessage("개발용 Settings 폴더가 없습니다.", MessageType.Warning);
                return;
            }

            int checkedCount = 0;
            int registeredCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { root });
            foreach (string guid in guids)
            {
                checkedCount++;
                AddressableAssetEntry entry = addressableSettings.FindAssetEntry(guid);
                if (entry == null)
                    continue;

                registeredCount++;
                AddMessage($"개발용 Settings가 Addressables에 등록되어 있습니다. address={entry.address}, path={AssetDatabase.GUIDToAssetPath(guid)}", MessageType.Error);
            }

            if (registeredCount == 0)
            {
                AddMessage($"검사 완료: 개발용 Settings {checkedCount}개 모두 Addressables에 등록되어 있지 않습니다.", MessageType.Info);
            }
        }

        /// <summary>
        /// 현재 작업자 개발용 Settings 폴더를 프로젝트 창에서 선택합니다.
        /// </summary>
        private void OpenCurrentWorkerFolder()
        {
            SettingsProfileEditorPrefs.EnsureCurrentWorkerDirectory();
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(SettingsProfileEditorPrefs.GetCurrentWorkerRoot());
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        /// <summary>
        /// 창 하단에 작업 결과 메시지 목록을 그립니다.
        /// </summary>
        private void DrawMessages()
        {
            EditorGUILayout.LabelField("작업 로그", EditorStyles.boldLabel);
            using (EditorGUILayout.ScrollViewScope scope = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.MinHeight(120f)))
            {
                _scrollPosition = scope.scrollPosition;
                if (_messages.Count == 0)
                {
                    EditorGUILayout.HelpBox("표시할 메시지가 없습니다.", MessageType.None);
                    return;
                }

                foreach (string message in _messages)
                {
                    EditorGUILayout.HelpBox(message, MessageType.None);
                }
            }
        }

        /// <summary>
        /// 작업 로그에 메시지를 추가하고 콘솔에도 같은 내용을 출력합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        /// <param name="messageType">메시지 종류입니다.</param>
        private void AddMessage(string message, MessageType messageType)
        {
            _messages.Add(message);
            switch (messageType)
            {
                case MessageType.Error:
                    Debug.LogError(message);
                    break;
                case MessageType.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }
}
