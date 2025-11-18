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
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                CopyPackageResources();
            }
        }

        private const string SourceFolder = "Packages/com.ggemco.2d.core/PackageResource";
        private const string TargetFolder = "Assets/Resources/"+ConfigDefine.NameSDK;
        public void CopyPackageResources(EditorSetupContext ctx = null)
        {
            if (!Directory.Exists(SourceFolder))
            {
                HelperLog.Error($"소스 폴더가 존재하지 않습니다: {SourceFolder}", ctx);
                return;
            }

            HelperFile.CopyDirectory(SourceFolder, TargetFolder, false);
            
            AssetDatabase.Refresh();
            HelperLog.Info($"PackageResource 내의 파일을 {TargetFolder} 경로로 복사 완료했습니다.", ctx);
        }
    }
}