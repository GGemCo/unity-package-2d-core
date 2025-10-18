using UnityEngine;

namespace GGemCo2DCore
{
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
        
        [Header("시뮬레이션 툴 UI 표시")]
        public bool toolPreviewAlwaysShow;
        public bool toolPreviewHideWhenMoving;

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            volumeMaster = 0.5f;
            volumeBGM = 1.0f;
            volumeSfx = 1.0f;
        }
    }
}