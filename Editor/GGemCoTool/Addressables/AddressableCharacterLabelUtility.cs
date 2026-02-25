using System.Collections.Generic;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace GGemCo2DCoreEditor
{
    internal enum AddressableCharacterType
    {
        Monster,
        Npc
    }

    /// <summary>
    /// 맵 리젠 데이터(regen_monster / regen_npc)를 기반으로,
    /// 등장 캐릭터 프리팹(Addressables Entry)에 맵 라벨을 부여하는 공용 유틸리티입니다.
    /// - <see cref="SettingMap"/>, <see cref="SettingCharacters"/> 등 여러 툴에서 재사용할 수 있습니다.
    /// </summary>
    internal static class AddressableCharacterLabelUtility
    {
        /// <summary>
        /// 리젠(JSON) 파일을 읽고, 등장 캐릭터 프리팹 Addressables Entry에 맵 라벨을 부여합니다.
        /// </summary>
        internal static void ApplyMapLabelFromRegen(
            AddressableAssetSettings settings,
            string mapFolderName,
            string regenJsonAssetPath,
            AddressableCharacterType type,
            bool clearExistingLabel)
        {
            if (settings == null) return;
            if (string.IsNullOrEmpty(mapFolderName)) return;

            string labelName = ConfigAddressableMap.GetLabel(mapFolderName);
            if (string.IsNullOrEmpty(labelName)) return;

            if (clearExistingLabel)
            {
                RemoveMapLabelFromAllCharacters(settings, type, labelName);
            }

            string content = AssetDatabaseLoaderManager.LoadFileJson(regenJsonAssetPath);
            if (string.IsNullOrEmpty(content)) return;

            var regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
            if (regenDataList?.CharacterRegenDatas == null || regenDataList.CharacterRegenDatas.Count == 0) return;

            foreach (var regenData in regenDataList.CharacterRegenDatas)
            {
                int uid = regenData.Uid;
                if (uid <= 0) continue;

                int animationUid = GetAnimationUidByCharacterUid(uid, type);
                if (animationUid <= 0) continue;

                var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(animationUid);
                if (infoAnimation == null) continue;

                string prefabPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, useExt: true);
                if (string.IsNullOrEmpty(prefabPath)) continue;

                var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
                entry?.SetLabel(labelName, true, true);
            }
        }

        private static int GetAnimationUidByCharacterUid(int characterUid, AddressableCharacterType type)
        {
            if (type == AddressableCharacterType.Monster)
            {
                var info = TableLoaderManager.LoadMonsterTable().GetDataByUid(characterUid);
                return info?.AnimationUid ?? 0;
            }

            var npc = TableLoaderManager.LoadNpcTable().GetDataByUid(characterUid);
            return npc?.AnimationUid ?? 0;
        }

        /// <summary>
        /// 기존에 설정된 특정 맵 라벨을 몬스터/ NPC 전체에서 제거합니다.
        /// - 리젠 기준으로 라벨을 "갱신"할 때, 잔재 제거를 위해 사용합니다.
        /// </summary>
        private static void RemoveMapLabelFromAllCharacters(
            AddressableAssetSettings settings,
            AddressableCharacterType type,
            string labelName)
        {
            if (settings == null) return;
            if (string.IsNullOrEmpty(labelName)) return;

            if (type == AddressableCharacterType.Monster)
            {
                Dictionary<int, StruckTableMonster> datas = TableLoaderManager.LoadMonsterTable().GetDatas();
                foreach (var pair in datas)
                {
                    var info = pair.Value;
                    if (info == null) continue;

                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;

                    string prefabPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, useExt: true);
                    if (string.IsNullOrEmpty(prefabPath)) continue;

                    var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
                    entry?.SetLabel(labelName, false, true);
                }
            }
            else
            {
                Dictionary<int, StruckTableNpc> datas = TableLoaderManager.LoadNpcTable().GetDatas();
                foreach (var pair in datas)
                {
                    var info = pair.Value;
                    if (info == null) continue;

                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;

                    string prefabPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, useExt: true);
                    if (string.IsNullOrEmpty(prefabPath)) continue;

                    var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
                    entry?.SetLabel(labelName, false, true);
                }
            }
        }
    }
}
