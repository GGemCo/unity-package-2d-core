using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingResource
    {
        private const string Title = "필수 Resource 추가하기";

        public void OnGUI()
        {
            Common.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                CopyPackageResources();
            }
        }

        private const string SourceFolder = "Packages/com.ggemco.2d.core/PackageResource";
        private const string TargetFolder = "Assets/Resources/"+ConfigDefine.NameSDK;
        private void CopyPackageResources()
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogError($"소스 폴더가 존재하지 않습니다: {SourceFolder}");
                return;
            }

            CopyDirectory(SourceFolder, TargetFolder);
            AssetDatabase.Refresh();
            Debug.Log($"PackageResource 내의 파일을 {TargetFolder} 경로로 복사 완료했습니다.");
        }
        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                if (file.EndsWith(".meta")) continue;

                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(directory, targetSubDir);
            }
        }
    }
}