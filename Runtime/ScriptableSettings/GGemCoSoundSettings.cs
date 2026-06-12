using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Sound.FileName, menuName = ConfigScriptableObject.Sound.MenuName,
        order = ConfigScriptableObject.Sound.Ordering)]
    public class GGemCoSoundSettings : ScriptableObject
    {
        private const int DefaultPreloadConcurrentRequestCount = 3;
        private const int MaxPreloadConcurrentRequestCount = 8;

        [Serializable]
        public class MappingButtonClickSound
        {
            public SoundConstants.UIButtonType type;
            public int soundUid;
        }
        [Tooltip("버튼 Type별로 사운드를 설정합니다.")]
        public List<MappingButtonClickSound> buttonClickSounds;

        [Header("사운드 로딩")]
        [Tooltip("PreLoad 사운드를 동시에 요청할 최대 개수입니다. 너무 큰 값은 로딩 중 CPU와 메모리 사용량을 높일 수 있습니다.")]
        [SerializeField, Range(1, MaxPreloadConcurrentRequestCount)]
        private int preloadConcurrentRequestCount = DefaultPreloadConcurrentRequestCount;

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

        /// <summary>
        /// PreLoad 사운드의 동시 로드 요청 개수를 반환합니다.
        /// 기존 설정 에셋에서 값이 비어 있거나 잘못된 경우에는 기본값을 사용합니다.
        /// </summary>
        /// <returns>동시에 요청할 최대 AudioClip 개수입니다.</returns>
        public int GetPreloadConcurrentRequestCount()
        {
            int resolvedCount = preloadConcurrentRequestCount > 0
                ? preloadConcurrentRequestCount
                : DefaultPreloadConcurrentRequestCount;
            return Mathf.Clamp(resolvedCount, 1, MaxPreloadConcurrentRequestCount);
        }

        /// <summary>
        /// 버튼 종류에 연결된 대표 사운드 UID를 반환합니다.
        /// </summary>
        /// <param name="buttonType">조회할 UI 버튼 종류입니다.</param>
        /// <returns>연결된 대표 사운드 UID이며, 설정이 없으면 0입니다.</returns>
        public int GetSoundButtonClickUid(SoundConstants.UIButtonType buttonType)
        {
            var info = buttonClickSounds.Find(x => x.type == buttonType);
            return info?.soundUid ?? 0;
        }

        /// <summary>
        /// 기본 버튼 클릭 사운드 UID를 반환합니다.
        /// </summary>
        /// <returns>기본 버튼 클릭 사운드 UID입니다.</returns>
        public int GetDefaultButtonClick()
        {
            return GetSoundButtonClickUid(SoundConstants.UIButtonType.Default);
        }
    }
}