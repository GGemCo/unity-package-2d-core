using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// <para>Localization 매니저</para>
    /// </summary>
    public class LocalizationManager : LocalizationManagerBase
    {
        public static LocalizationManager Instance;
        // 사용자 언어 테이블 존재 여부
        private readonly Dictionary<string, bool> _userTableExistsMap = new();

        protected override void Awake()
        {
            base.Awake();
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

        }

        /// <summary>
        /// 사용자 언어 테이블 존재 체크
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator CheckUserTablesExist()
        {
            foreach (string baseTable in LocalizationConstants.Tables.All)
            {
                string userTableName = $"{baseTable}_User";
                var handle = stringDatabase.GetTableAsync(userTableName, LocalizationSettings.SelectedLocale);
                yield return handle;

                bool exists = false;

                if (handle.IsValid())
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    {
                        exists = true;
                        // GcLogger.Log($"table: {userTableName} / exist: true");
                    }
                    else
                    {
                        // GcLogger.Log($"table: {userTableName} / exist: false");
                    }
                }
                else
                {
                    GcLogger.LogWarning($"Invalid handle for table: {userTableName}");
                }

                _userTableExistsMap[baseTable] = exists;

                // handle이 Release 가능한 경우라면 아래 코드도 추가
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        /// <summary>
        /// UI 에서 사용하는 공용 단어
        /// </summary>
        public string GetCommonUIByKey(string key) => GetString(LocalizationConstants.Tables.CommonUI, key);

        /// <summary>
        /// 시스템 메시지
        /// </summary>
        public string GetSystemByKey(string key) => GetString(LocalizationConstants.Tables.System, key);

        /// <summary>
        /// Scene (인트로, 로딩, 게임)
        /// </summary>
        public string GetSceneByKey(string key) => GetString(LocalizationConstants.Tables.Scene, key);
        /// <summary>
        /// 아이템 정보 윈도우
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetUIWindowItemInfoByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemInfo, key);
        /// <summary>
        /// 인게임에서 사용하는 공용 단어
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetCommonGameByKey(string key) => GetString(LocalizationConstants.Tables.CommonGame, key);
        
        /// <summary>
        /// 데이터 테이블별 단어 가져오기
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetStatusNameByKey(string key) => GetString(LocalizationConstants.Tables.StatusName, key);
        public string GetUIWindowSkillInfoByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowSkillInfo, key);
        public string GetUIWindowSkillByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowSkill, key);
        public string GetUIWindowItemUpgradeByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemUpgrade, key);
        public string GetUIWindowItemCraftByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemCraft, key);
        public string GetUIWindowQuestRewardByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowQuestReward, key);
        public string GetItemNameByKey(string key) => GetString(LocalizationConstants.Tables.ItemName, key);
        public string GetItemDescriptionByKey(string key) => GetString(LocalizationConstants.Tables.ItemDescription, key);
        public string GetMapNameByKey(string key) => GetString(LocalizationConstants.Tables.MapName, key);
        public string GetSkillNameByKey(string key) => GetString(LocalizationConstants.Tables.SkillName, key);
        public string GetNpcNameByKey(string key) => GetString(LocalizationConstants.Tables.NpcName, key);
        public string GetMonsterNameByKey(string key) => GetString(LocalizationConstants.Tables.MonsterName, key);
        public string GetUIWindowTitleByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowTitle, key);
        public string GetAffectNameByKey(string key) => GetString(LocalizationConstants.Tables.AffectName, key);
        public string GetUIWindowOptionByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowOption, key);

        public string GetInteractionByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowInteractionDialogue, key);
    }
}
