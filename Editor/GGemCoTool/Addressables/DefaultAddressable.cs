using GGemCo.Scripts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace GGemCo.Editor
{
    public class DefaultAddressable
    {
        protected string TargetGroupName = ""; // 그룹 이름

        /// <summary>
        /// Addressable 설정이 없을 경우 새로 생성
        /// </summary>
        protected AddressableAssetSettings CreateAddressableSettings()
        {
            var settings = AddressableAssetSettings.Create(
                "Assets/AddressableAssetsData", 
                "AddressableAssetSettings", 
                true, 
                true
            );

            AddressableAssetSettingsDefaultObject.Settings = settings;
            AssetDatabase.SaveAssets();
            // GcLogger.Log("새로운 Addressable 설정을 생성했습니다.");
            return settings;
        }

        /// <summary>
        /// 기본 Addressable 그룹이 없을 경우 생성
        /// </summary>
        private AddressableAssetGroup CreateDefaultGroup(AddressableAssetSettings settings)
        {
            var defaultGroup = settings.CreateGroup(
                TargetGroupName, 
                false, 
                false, 
                true, 
                settings.DefaultGroup.Schemas
            );

            settings.DefaultGroup = defaultGroup;
            // GcLogger.Log("새로운 기본 Addressable 그룹을 생성했습니다.");
            return defaultGroup;
        }
        /// <summary>
        /// 지정한 이름의 그룹이 없으면 새로 생성
        /// </summary>
        protected AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            foreach (var group in settings.groups)
            {
                if (group != null && group.Name == groupName)
                    return group;
            }

            var newGroup = settings.CreateGroup(
                groupName,
                false,
                false,
                true,
                settings.DefaultGroup.Schemas // 기존 기본 그룹의 스키마 복사
            );

            GcLogger.Log($"새로운 Addressable 그룹을 생성했습니다: {groupName}");
            return newGroup;
        }
    }
}