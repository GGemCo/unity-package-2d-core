using System;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class CharacterWhiteOverlayData
    {
        [Header("Target")]
        [Tooltip("캐릭터 대상 참조 정보입니다. Fixed는 직접 타입/uid를, RuntimeOverride는 런타임 키를 사용합니다.")]
        public CutsceneCharacterReference target = new CutsceneCharacterReference();

        [HideInInspector] public CharacterConstants.Type characterType;
        [HideInInspector] public int characterUid;

        [Header("Overlay")]
        public Color color = Color.white;
        [Range(0f, 1f)] public float fromStrength = 0f;
        [Range(0f, 1f)] public float toStrength = 1f;
        [Tooltip("클립 종료 후 오버레이를 제거할지 여부입니다.")]
        public bool restoreOnStop = true;
        [Tooltip("SpriteRenderer 목록을 다시 수집할지 여부입니다.")]
        public bool refreshTargetsOnTrigger = false;
        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
        [Tooltip("강도 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
    }
}
