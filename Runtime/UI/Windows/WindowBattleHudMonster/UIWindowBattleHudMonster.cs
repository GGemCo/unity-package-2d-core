using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowBattleHudMonster : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("생명력 Slider")]
        public UISliderFlip uiSliderFlip;
        [Header("슈퍼아머")]
        [Tooltip("슈퍼 아머 아이콘이 들어갈 오브젝트")]
        public GameObject containerSuperArmor;
        [Tooltip("슈퍼 아머 아이콘 프리팹")]
        public GameObject prefabShield;

        public TMP_Text textCharacterName;
        public Image imageCharacterName;
        private List<GameObject> _shieldIcons;
        private AddressableLoaderCharacterImageName _addressableLoaderCharacterImageName;
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.BattleHudMonster;
            base.Awake();
            _shieldIcons = new List<GameObject>();
            _addressableLoaderCharacterImageName = AddressableLoaderCharacterImageName.Instance;
        }
        /// <summary>
        /// 전투 HUD에 표시할 몬스터 정보를 갱신합니다.
        /// </summary>
        /// <param name="monster">표시할 몬스터입니다.</param>
        /// <param name="showSuperArmor">Super Armor 아이콘 표시 여부입니다.</param>
        public void UpdateInfo(Monster monster, bool showSuperArmor)
        {
            if (!monster) return;
            InitMonsterNameText(monster.uid);
            InitMonsterNameByImage(monster.uid);
            InitSuperArmor(monster, showSuperArmor);
        }

        private void InitMonsterNameByImage(int monsterUid)
        {
            if (!imageCharacterName) return;
            
            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info == null) return;

            if (string.IsNullOrEmpty(info.ImageThumbnailFileName)) return;
            string key = $"{ConfigAddressableKey.CharacterImageNameMonster}_{info.ImageThumbnailFileName}";
            Sprite sprite = _addressableLoaderCharacterImageName.GetImageNameByKey(key);
            if (GcLogger.IsNull(sprite, $"[CharacterLoader] Failed to load character sprite. key={key}")) return;
            imageCharacterName.sprite = sprite;
            imageCharacterName.SetNativeSize();
        }

        private void InitMonsterNameText(int monsterUid)
        {
            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info == null) return;
            if (!textCharacterName) return;
            textCharacterName.text = info.Name;
        }

        /// <summary>
        /// Battle HUD에 표시할 Super Armor 아이콘을 초기화합니다.
        /// </summary>
        /// <param name="monster">표시할 몬스터입니다.</param>
        /// <param name="showSuperArmor">Super Armor 표시 여부입니다.</param>
        private void InitSuperArmor(Monster monster, bool showSuperArmor)
        {
            if (containerSuperArmor != null)
            {
                containerSuperArmor.SetActive(showSuperArmor);
            }

            if (!showSuperArmor)
            {
                SetSuperArmor(0);
                return;
            }

            int maxSuperArmor = Mathf.Max(monster.TotalSuperArmor.Value, monster.CurrentSuperArmor.Value);
            if (maxSuperArmor <= 0)
            {
                SetSuperArmor(0);
                return;
            }

            if (GcLogger.IsNull(containerSuperArmor, $"슈퍼아머 아이콘을 배치할 컨테이너가 없습니다. containerSuperArmor : {containerSuperArmor}")) return;
            if (GcLogger.IsNull(prefabShield, $"슈퍼아머에 사용할 아이콘 프리팹이 없습니다. prefabShield : {prefabShield}")) return;

            EnsureShieldIconCount(maxSuperArmor);
            SetSuperArmor(monster.CurrentSuperArmor.Value);
        }

        /// <summary>
        /// 필요한 Super Armor 아이콘 개수만큼 풀을 확장합니다.
        /// </summary>
        /// <param name="count">필요한 아이콘 개수입니다.</param>
        private void EnsureShieldIconCount(int count)
        {
            if (count <= _shieldIcons.Count) return;

            for (int i = _shieldIcons.Count; i < count; i++)
            {
                var shield = Instantiate(prefabShield, containerSuperArmor.transform);
                _shieldIcons.Add(shield);
            }
        }

        /// <summary>
        /// Battle HUD HP 슬라이더 값을 갱신합니다.
        /// </summary>
        /// <param name="currentValue">현재 HP 값입니다.</param>
        /// <param name="totalValue">최대 HP 값입니다.</param>
        public void SetSliderHp(long currentValue, long totalValue)
        {
            uiSliderFlip.SetValue(currentValue, totalValue);
        }

        /// <summary>
        /// Battle HUD Super Armor 아이콘 활성 상태를 현재 값에 맞춰 갱신합니다.
        /// </summary>
        /// <param name="value">현재 Super Armor 값입니다.</param>
        public void SetSuperArmor(int value)
        {
            if (value <= 0)
            {
                foreach (var shieldIcon in _shieldIcons)
                {
                    shieldIcon.SetActive(false);
                }
                return;
            }
            int index = 0;
            foreach (var shieldIcon in _shieldIcons)
            {
                shieldIcon.SetActive(index < value);
                index++;
            }
        }
    }
}