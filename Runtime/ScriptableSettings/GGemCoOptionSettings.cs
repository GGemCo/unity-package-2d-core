using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 옵션의 기본값과 모바일 전투 햅틱 프로필을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObject.Option.FileName,
        menuName = ConfigScriptableObject.Option.MenuName, order = ConfigScriptableObject.Option.Ordering)]
    public class GGemCoOptionSettings : ScriptableObject
    {
        [Header("볼륨")]
        [Tooltip("게임 전체 사운드의 기본 볼륨 크기 (0.0 ~ 1.0)")]
        public float volumeMaster;

        [Tooltip("배경 음악의 볼륨 크기 (0.0 ~ 1.0)")]
        public float volumeBGM;

        [Tooltip("UI, 전투, 환경 효과음 볼륨 크기 (0.0 ~ 1.0)")]
        public float volumeSfx;

        [Header("모바일 햅틱")]
        [Tooltip("사용자 저장값이 없을 때 적용할 모바일 햅틱 기본 활성화 여부입니다.")]
        public bool hapticEnabledByDefault = true;

        [Tooltip("일반 가드 성공 시 재생할 햅틱 프로필입니다.")]
        public MobileHapticProfile guardSuccessHaptic =
            MobileHapticProfile.Create(0.35f, 45, 0.08f);

        [Tooltip("저스트 가드 성공 시 재생할 햅틱 프로필입니다.")]
        public MobileHapticProfile justGuardSuccessHaptic =
            MobileHapticProfile.Create(0.8f, 85, 0.12f);

        [Tooltip("플레이어가 몬스터에게 확정 피해를 적용했을 때 재생할 햅틱 프로필입니다.")]
        public MobileHapticProfile monsterHitHaptic =
            MobileHapticProfile.Create(0.25f, 30, 0.05f);
        
        [Header("시뮬레이션 툴 UI 표시")]
        public bool toolPreviewAlwaysShow;
        public bool toolPreviewHideWhenMoving;

        /// <summary>
        /// 전투 이벤트 타입에 맞는 모바일 햅틱 프로필을 반환합니다.
        /// </summary>
        /// <param name="eventType">조회할 전투 햅틱 이벤트입니다.</param>
        /// <returns>이벤트별 햅틱 프로필입니다.</returns>
        public MobileHapticProfile GetHapticProfile(CombatHapticEventType eventType)
        {
            return eventType switch
            {
                CombatHapticEventType.GuardSuccess => guardSuccessHaptic,
                CombatHapticEventType.JustGuardSuccess => justGuardSuccessHaptic,
                CombatHapticEventType.MonsterHit => monsterHitHaptic,
                _ => default,
            };
        }

        /// <summary>
        /// ScriptableObject를 처음 생성할 때 기본 옵션과 햅틱 프로필을 설정합니다.
        /// </summary>
        private void Reset()
        {
            volumeMaster = 0.5f;
            volumeBGM = 1.0f;
            volumeSfx = 1.0f;
            hapticEnabledByDefault = true;
            guardSuccessHaptic = MobileHapticProfile.Create(0.35f, 45, 0.08f);
            justGuardSuccessHaptic = MobileHapticProfile.Create(0.8f, 85, 0.12f);
            monsterHitHaptic = MobileHapticProfile.Create(0.25f, 30, 0.05f);
        }
    }
}
