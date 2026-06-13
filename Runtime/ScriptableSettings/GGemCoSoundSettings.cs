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

        [Header("사운드 진단")]
        [Tooltip("맵 사운드 범위의 로드 시간과 개발 빌드 메모리 추정값을 로그로 출력합니다.")]
        [SerializeField]
        private bool enableMapScopeProfiling;

        [Tooltip("이 시간 이상 걸린 맵 사운드 범위 로드는 경고로 출력합니다.")]
        [SerializeField, Min(0f)]
        private float slowMapScopeLoadThresholdSeconds = 0.25f;

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
        /// 맵 사운드 범위 로드 시간과 메모리 진단 로그를 출력할지 여부를 반환합니다.
        /// </summary>
        /// <returns>진단 로그가 활성화되어 있으면 true입니다.</returns>
        public bool IsMapScopeProfilingEnabled()
        {
            return enableMapScopeProfiling;
        }

        /// <summary>
        /// 느린 맵 사운드 범위 로드로 판단할 기준 시간을 반환합니다.
        /// </summary>
        /// <returns>0 이상으로 보정된 초 단위 기준 시간입니다.</returns>
        public float GetSlowMapScopeLoadThresholdSeconds()
        {
            return Mathf.Max(0f, slowMapScopeLoadThresholdSeconds);
        }

        /// <summary>
        /// 버튼 종류에 연결된 대표 사운드 UID를 반환합니다.
        /// </summary>
        /// <param name="buttonType">조회할 UI 버튼 종류입니다.</param>
        /// <returns>연결된 대표 사운드 UID이며, 설정이 없으면 0입니다.</returns>
        public int GetSoundButtonClickUid(SoundConstants.UIButtonType buttonType)
        {
            MappingButtonClickSound info = buttonClickSounds?.Find(x => x != null && x.type == buttonType);
            return info?.soundUid ?? 0;
        }

        /// <summary>
        /// 게임 시작 시 전역 UI 공용 범위로 미리 로드할 버튼 사운드 UID를 반환합니다.
        /// 같은 UID와 0 이하 값은 제거합니다.
        /// </summary>
        /// <returns>중복이 제거된 대표 sound UID 목록입니다.</returns>
        public IReadOnlyList<int> GetCommonUiSoundUids()
        {
            List<int> result = new List<int>();
            HashSet<int> registered = new HashSet<int>();
            if (buttonClickSounds == null)
                return result;

            for (int i = 0; i < buttonClickSounds.Count; i++)
            {
                MappingButtonClickSound mapping = buttonClickSounds[i];
                if (mapping == null || mapping.soundUid <= 0 || !registered.Add(mapping.soundUid))
                    continue;

                result.Add(mapping.soundUid);
            }

            return result;
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
