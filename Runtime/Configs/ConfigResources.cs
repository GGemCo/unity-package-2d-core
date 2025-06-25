using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class ResourcesAssetInfo
    {
        private readonly string _path;
        private GameObject _cachedObject;

        public ResourcesAssetInfo(string path)
        {
            _path = path;
        }

        public GameObject Load()
        {
            if (_cachedObject != null) return _cachedObject;
            _cachedObject = Resources.Load<GameObject>(_path);
            if (_cachedObject == null)
                Debug.LogWarning($"[ResourcesAssetInfo] '{_path}' 경로의 오브젝트를 찾을 수 없습니다.");
            return _cachedObject;
        }
    }
    public static class ConfigResources
    {
        /// <summary>
        /// 경로는 중복되지 않게 GGemCo 하위에 폴더를 생성해서 추가한다.
        /// Assets/Resources/UI/TextDamage.prefab
        /// Assets/GGemCo/Resources/UI/TextDamage.prefab
        /// 위와 같은 경우 UI/TextDamage 경로로 리소스를 가져올때 충돌이 발생한다.
        /// </summary>
        public static readonly ResourcesAssetInfo DropItem = new($"{ConfigDefine.NameSDK}/Item/DropItem");
        public static readonly ResourcesAssetInfo TextDropItemNameTag = new($"{ConfigDefine.NameSDK}/Item/TextDropItemNameTag");
        
        public static readonly ResourcesAssetInfo SliderMonsterHp = new($"{ConfigDefine.NameSDK}/UI/Common/SliderMonsterHp");
        public static readonly ResourcesAssetInfo TextDamage = new($"{ConfigDefine.NameSDK}/UI/Common/TextDamage");
        public static readonly ResourcesAssetInfo TextNpcNameTag = new($"{ConfigDefine.NameSDK}/UI/Common/TextNpcNameTag");
        public static readonly ResourcesAssetInfo DialogueBalloon = new($"{ConfigDefine.NameSDK}/UI/Common/UIDialogueBalloon");

        public static readonly ResourcesAssetInfo IconItem = new($"{ConfigDefine.NameSDK}/UI/Icon/IconItem");
        public static readonly ResourcesAssetInfo IconQuestInProgress = new($"{ConfigDefine.NameSDK}/UI/Icon/IconQuestInProgress");
        public static readonly ResourcesAssetInfo IconQuestReady = new($"{ConfigDefine.NameSDK}/UI/Icon/IconQuestReady");
        public static readonly ResourcesAssetInfo IconSkill = new($"{ConfigDefine.NameSDK}/UI/Icon/IconSkill");
        public static readonly ResourcesAssetInfo Slot = new($"{ConfigDefine.NameSDK}/UI/Icon/Slot");
        public static readonly ResourcesAssetInfo SlotBuff = new($"{ConfigDefine.NameSDK}/UI/Icon/SlotBuff");
        
        public static readonly ResourcesAssetInfo PopupDefault = new($"{ConfigDefine.NameSDK}/UI/Popup/PopupDefault");
        
        /// <summary>
        /// Assets/Resources/GGemCo/ 폴더에 복사할 리스트
        /// </summary>
        public static readonly List<ResourcesAssetInfo> All = new()
        {
            DropItem,
            TextDropItemNameTag,
            SliderMonsterHp,
            TextDamage,
            TextNpcNameTag,
            DialogueBalloon,
            IconItem,
            IconQuestInProgress,
            IconQuestReady,
            IconSkill,
            Slot,
            SlotBuff,
        };
    }
}