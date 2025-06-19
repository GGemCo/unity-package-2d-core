using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 저장 데이터 관리 툴
    /// </summary>
    public class SaveDataEditor : EditorWindow
    {
        [MenuItem(ConfigEditor.NameToolOpenSaveDataFolder, false, (int)ConfigEditor.ToolOrdering.OpenSaveDataFolder)]
        public static void ShowWindow()
        {
            GetWindow<SaveDataEditor>(ConfigEditor.NameToolOpenSaveDataFolder);
        }

        private void OnEnable()
        {
        }

        private void OnGUI()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("게임 데이터 관리저장 폴더 열기"))
            {
                OpenGameDataFolder();
            }
            GUILayout.Space(20);
            if (GUILayout.Button("게임 데이터 모두 지우기"))
            {
                RevemoAllGameDataFolder();
            }
        }

        private void RevemoAllGameDataFolder()
        {
            bool result = EditorUtility.DisplayDialog("지우기", "현재 저장된 데이터가 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
            if (!result) return;
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static void OpenGameDataFolder()
        {
            string path = Application.persistentDataPath;

            if (Directory.Exists(path))
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true
                });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                Process.Start("open", path);
#endif
            }
            else
            {
                UnityEngine.Debug.LogError($"폴더를 찾을 수 없습니다: {path}");
            }
        }

    }
}
