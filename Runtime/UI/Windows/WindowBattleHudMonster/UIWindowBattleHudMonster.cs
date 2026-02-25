using System.Collections.Generic;
using System.Threading.Tasks;
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
        public void UpdateInfo(Monster monster)
        {
            if (!monster) return;
            InitMonsterNameText(monster.uid);
            InitMonsterNameByImage(monster.uid);
            InitSuperArmor(monster.uid);
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
        }

        private void InitMonsterNameText(int monsterUid)
        {
            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info == null) return;
            if (!textCharacterName) return;
            textCharacterName.text = info.Name;
        }

        private void InitSuperArmor(int monsterUid)
        {
            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info == null) return;
            int superArmor = info.StatSuperArmor;
            if (superArmor <= 0) return;
            if (GcLogger.IsNull(prefabShield, $"슈퍼아머에 사용할 아이콘 프리팹이 없습니다. prefabShield : {prefabShield}")) return;

            if (superArmor > _shieldIcons.Count)
            {
                for (int i = 0; i < superArmor - _shieldIcons.Count; i++)
                {
                    var shield = Instantiate(prefabShield, containerSuperArmor.transform);
                    _shieldIcons.Add(shield);
                }
            }
            SetSuperArmor(superArmor);
        }

        public void SetSliderHp(long currentValue, long totalValue)
        {
            uiSliderFlip.SetValue(currentValue, totalValue);
        }

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