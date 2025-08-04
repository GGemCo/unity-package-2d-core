using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    [RequireComponent(typeof(Button))]
    public class ClickSoundEventBroadcaster : MonoBehaviour, IClickSoundEventTrigger
    {
        [Tooltip("사운드 고유 ID (우선순위: 이 값이 있을 경우 우선 적용)")]
        public int soundUid;

        [Tooltip("Sound Type Enum (soundId가 없을 경우 사용)")]
        public SoundConstants.UIButtonType type = SoundConstants.UIButtonType.Default;

        private void Awake()
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            ClickSoundEventDispatcher.Dispatch(this);
        }

        public int GetSoundId() => soundUid;
        public SoundConstants.UIButtonType GetSoundType() => type;
    }
}