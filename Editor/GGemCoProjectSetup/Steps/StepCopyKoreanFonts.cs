using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class StepCopyKoreanFonts : SetupStepBase
    {
        private const string SourceFolder = ConfigEditor.PathPackageCore+"/Samples~/Fonts";
        private const string TargetFolder = "Assets/"+ConfigDefine.NameSDK+"/Fonts";
        private const string TargetFont = "NanumGothicBold t:TMP_FontAsset";
        
        public override void Execute(EditorSetupContext ctx)
        {
            HelperFile.CopyDirectory(SourceFolder, TargetFolder, true, true);
            
            // 1) NanumGothicBold SDF 로드 (Assets/Fonts 하위 검색)
            string[] guids = AssetDatabase.FindAssets(TargetFont);
            TMP_FontAsset nanum = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(TargetFolder))
                {
                    nanum = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (nanum != null) break;
                }
            }
            if (nanum == null)
            {
                HelperLog.Error($"[{nameof(StepCopyKoreanFonts)}] {TargetFolder} 경로에서 NanumGothicBold SDF(Font Asset)을 찾지 못했습니다.", ctx);
                return;
            }

            // 2) TMP Settings 확보 (없으면 생성)
            var settings = TMP_Settings.instance;
            if (settings == null)
            {
                // Project Settings > TextMesh Pro 에서 생성된 Settings 자산이 필요
                TMP_Settings.LoadDefaultSettings(); // 존재 시 로드
                settings = TMP_Settings.instance;

                if (settings == null)
                {
                    // 예비: 기본 Settings 생성 (Resources에 저장되어야 함)
                    settings = ScriptableObject.CreateInstance<TMP_Settings>();
                    AssetDatabase.CreateAsset(settings, "Assets/Resources/TMP Settings.asset");
                    // AssetDatabase.SaveAssets();
                }
            }

            // 3) 전역 폴백 목록에 추가
            var list = TMP_Settings.fallbackFontAssets;
            if (list == null)
            {
                // 내부적으로 리스트를 Settings가 관리하므로 직렬화로 보장
                var so = new SerializedObject(settings);
                so.Update();
                var prop = so.FindProperty("m_fallbackFontAssets");
                if (prop != null) prop.arraySize = 0;
                so.ApplyModifiedProperties();
                list = TMP_Settings.fallbackFontAssets; // 갱신
            }

            if (!list.Contains(nanum))
            {
                list.Add(nanum);
                // EditorUtility.SetDirty(settings);
                HelperLog.Info($"[{nameof(StepCopyKoreanFonts)}] Added global TMP fallback: {nanum.name}", ctx);
            }
            else
            {
                HelperLog.Info($"[{nameof(StepCopyKoreanFonts)}] NanumGothicBold already in TMP global fallback list.");
            }
        }
    }
}