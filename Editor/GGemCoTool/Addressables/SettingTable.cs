using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 테이블 등록하기
    /// </summary>
    public class SettingTable : DefaultAddressable
    {
        private const string Title = "테이블 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingTable(AddressableEditor addressableEditorWindow = null)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = ConfigAddressableGroupName.Table;
        }
        public void OnGUI()
        {
            if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
            {
                try
                {
                    Setup();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog(Title, "데이터 테이블 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                }
            }
        }
        
        /// <summary>
        /// Core 테이블 원본과 런타임 테이블 팩을 Addressables에 등록합니다.
        /// </summary>
        /// <param name="ctx">자동 설정 실행 컨텍스트입니다. null이면 완료 다이얼로그를 표시합니다.</param>
        public void Setup(EditorSetupContext ctx = null)
        {
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);

            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            RegisterRuntimeTablePack(settings, group, ctx);

            foreach (var addressableAssetInfo in ConfigAddressableTable.All)
            {
                if (ShouldSkipMissingOptionalProjectileTable(addressableAssetInfo))
                    continue;

                Add(settings, group, addressableAssetInfo.Key, addressableAssetInfo.Path, ConfigAddressableLabel.Table);
                // Debug.Log($"Addressable 키 값 설정: {keyName}");
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

            if (ctx != null)
            {
                HelperLog.Info($"Addressable 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");    
            }
        }

        /// <summary>
        /// Core 개별 테이블 txt를 런타임 팩으로 생성하고 Addressables에 등록합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">등록 대상 Table 그룹입니다.</param>
        /// <param name="ctx">자동 설정 실행 컨텍스트입니다.</param>
        private void RegisterRuntimeTablePack(AddressableAssetSettings settings, AddressableAssetGroup group, EditorSetupContext ctx)
        {
            AddressableAssetInfo pack = ConfigAddressableTablePack.Core;
            bool built = RuntimeTablePackBuilder.Build(
                ConfigAddressableTablePack.PackageCore,
                pack,
                ConfigAddressableTable.All,
                ctx);

            if (!built)
            {
                HelperLog.Warn("Core 런타임 테이블 팩 생성에 실패했습니다. 개별 테이블 등록은 계속 진행합니다.", ctx);
                return;
            }

            Add(settings, group, pack.Key, pack.Path, ConfigAddressableLabel.TablePack);
        }

        /// <summary>
        /// Projectile 마이그레이션용 선택 테이블이 아직 생성되지 않았는지 확인합니다.
        /// - legacy projectile과 분리 테이블(linear/arc/path)을 함께 지원하므로 일부 파일이 없어도 Addressables 등록을 건너뜁니다.
        /// </summary>
        /// <param name="info">검사할 테이블 Addressables 정보입니다.</param>
        /// <returns>선택 Projectile 테이블이 없어서 건너뛰어야 하면 true를 반환합니다.</returns>
        private static bool ShouldSkipMissingOptionalProjectileTable(AddressableAssetInfo info)
        {
            if (info == null)
                return false;

            bool isProjectileTable =
                info.Key == ConfigAddressableTable.TableProjectile.Key ||
                info.Key == ConfigAddressableTable.TableProjectileLinear.Key ||
                info.Key == ConfigAddressableTable.TableProjectileArc.Key ||
                info.Key == ConfigAddressableTable.TableProjectilePath.Key;

            return isProjectileTable && string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(info.Path));
        }
    }
}
