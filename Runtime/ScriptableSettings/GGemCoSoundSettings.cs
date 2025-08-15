using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Sound.FileName, menuName = ConfigScriptableObject.Sound.MenuName,
        order = ConfigScriptableObject.Sound.Ordering)]
    public class GGemCoSoundSettings : ScriptableObject
    {
        [Serializable]
        public class MappingButtonClickSound
        {
            public SoundConstants.UIButtonType type;
            public int soundUid;
        }
        [Tooltip("버튼 Type별로 사운드를 설정합니다.")]
        public List<MappingButtonClickSound> buttonClickSounds;

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            buttonClickSounds ??= new List<MappingButtonClickSound>();
            MappingButtonClickSound mappingButtonClickSound = new MappingButtonClickSound
            {
                type = SoundConstants.UIButtonType.Default,
                soundUid = 0
            };
            buttonClickSounds.Add(mappingButtonClickSound);
            mappingButtonClickSound = new MappingButtonClickSound
            {
                type = SoundConstants.UIButtonType.Confirm,
                soundUid = 0
            };
            buttonClickSounds.Add(mappingButtonClickSound);
            mappingButtonClickSound = new MappingButtonClickSound
            {
                type = SoundConstants.UIButtonType.Cancel,
                soundUid = 0
            };
            buttonClickSounds.Add(mappingButtonClickSound);
            mappingButtonClickSound = new MappingButtonClickSound
            {
                type = SoundConstants.UIButtonType.CloseWindow,
                soundUid = 0
            };
            buttonClickSounds.Add(mappingButtonClickSound);
        }

        public int GetSoundButtonClickUid(SoundConstants.UIButtonType buttonType)
        {
            var info = buttonClickSounds.Find(x => x.type == buttonType);
            return info?.soundUid ?? 0;
        }

        public int GetDefaultButtonClick()
        {
            return GetSoundButtonClickUid(SoundConstants.UIButtonType.Default);
        }
    }
}