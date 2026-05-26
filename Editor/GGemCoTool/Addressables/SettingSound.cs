using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 사운드 Addressables 동기화 실행 옵션입니다.
    /// - 수동 실행 시에는 확인/완료 다이얼로그를 켜고,
    /// - 자동 실행(예: 테이블 저장 후) 시에는 다이얼로그 없이 동작하도록 제어합니다.
    /// </summary>
    public sealed class SettingSoundOptions
    {
        public bool ShowConfirmDialog = true;
        public bool ShowCompletedDialog = true;
        public bool SaveAssets = true;
        public EditorSetupContext Context;
    }

    public class SettingSound : DefaultAddressable
    {
        private const string Title = "사운드 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingSound(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = $"{ConfigAddressableGroupName.Sound}";
        }

        public void OnGUI()
        {
            if (!File.Exists($"{ConfigAddressableTable.TableSound.Path}"))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Sound} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth),
                        GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(Title, "사운드 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.",
                            "OK");
                    }
                }
            }
        }

        /// <summary>
        /// 수동 버튼 실행 진입점입니다.
        /// 내부 동작은 <see cref="SyncFromTable"/>를 호출해 공통 처리합니다.
        /// </summary>
        /// <param name="ctx">프로젝트 셋업 컨텍스트입니다. null이면 일반 에디터 다이얼로그 모드로 실행됩니다.</param>
        public void Setup(EditorSetupContext ctx = null)
        {
            SyncFromTable(new SettingSoundOptions
            {
                Context = ctx,
                ShowConfirmDialog = ctx == null,
                ShowCompletedDialog = ctx == null,
                SaveAssets = true,
            });
        }

        /// <summary>
        /// sound 테이블을 기준으로 Addressables 사운드 그룹을 재구성합니다.
        /// 기존 엔트리를 비우고, 테이블 행을 다시 순회해 엔트리/라벨을 등록합니다.
        /// </summary>
        /// <param name="options">동기화 옵션입니다. null이면 기본 옵션으로 실행됩니다.</param>
        public static void SyncFromTable(SettingSoundOptions options = null)
        {
            options ??= new SettingSoundOptions();
            EditorSetupContext ctx = options.Context;

            if (options.ShowConfirmDialog)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result)
                    return;
            }

            Dictionary<int, StruckTableSound> dictionary = TableLoaderManager.LoadSoundTable(true).GetDatas();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = new SettingSound(null).CreateAddressableSettings();
            }

            SettingSound helper = new SettingSound(null);
            AddressableAssetGroup group = helper.GetOrCreateGroup(settings, helper.targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{helper.targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            ClearGroupEntries(settings, group);

            foreach (KeyValuePair<int, StruckTableSound> outerPair in dictionary)
            {
                StruckTableSound info = outerPair.Value;
                if (info == null || info.Uid <= 0)
                    continue;

                string key = $"{ConfigAddressableGroupName.Sound}_{info.FileName}";
                string assetPath = ResolveAssetPath(info);
                string label = ConfigAddressableLabel.Sound;

                AddressableAssetEntry entry = helper.Add(settings, group, key, assetPath, label);
                entry?.SetLabel(ConfigAddressableLabel.SoundIntro, info.UseIntroScene, true);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

            if (options.SaveAssets)
                AssetDatabase.SaveAssets();

            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 사운드 설정 완료", ctx);
            }
            else if (options.ShowCompletedDialog)
            {
                EditorUtility.DisplayDialog(Title, "[Addressable] 사운드 설정 완료", "OK");
            }
        }

        /// <summary>
        /// 사운드 테이블 행의 FileName 값을 Addressables가 읽을 수 있는 실제 에셋 경로로 해석합니다.
        /// </summary>
        /// <param name="info">사운드 테이블 행 데이터입니다.</param>
        /// <returns>Assets 기준 정규화된 에셋 경로를 반환합니다.</returns>
        private static string ResolveAssetPath(StruckTableSound info)
        {
            string rawFileName = info?.FileName ?? string.Empty;
            string normalizedFileName = NormalizePath(rawFileName);

            // 이미 Assets 절대(프로젝트 기준) 경로로 입력된 경우 그대로 사용
            if (Path.IsPathRooted(normalizedFileName) ||
                normalizedFileName.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return normalizedFileName;

            // DataAddressable 루트 기준 상대 경로(Sounds/...)로 입력된 경우
            if (normalizedFileName.StartsWith("Sounds/", StringComparison.OrdinalIgnoreCase))
                return ConfigAddressablePath.Combine(ConfigAddressablePath.Root, normalizedFileName);

            // 기본 규칙: Type/SubType 기반 폴더 + FileName
            string basePath = ConfigAddressablePath.BuildSoundPath(info.Type, info.SubType);
            if (string.IsNullOrWhiteSpace(basePath))
                basePath = ConfigAddressablePath.Sounds;

            return ConfigAddressablePath.Combine(basePath, normalizedFileName);
        }

        /// <summary>
        /// 경로 문자열을 슬래시 기준으로 정규화하고 양끝 공백/따옴표를 제거합니다.
        /// </summary>
        /// <param name="path">원본 경로 문자열입니다.</param>
        /// <returns>정규화된 경로 문자열입니다.</returns>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string trimmed = path.Trim().Trim('"');
            return ConfigAddressablePath.EnsureForwardSlashes(trimmed);
        }
    }
}
