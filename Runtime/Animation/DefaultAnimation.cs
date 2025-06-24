using UnityEngine;

namespace GGemCo2DCore
{
    public class StruckChangeSlotImage
    {
        public string SlotName;
        public string AttachmentName;
        public Sprite Sprite;

        public StruckChangeSlotImage(string slotName, string attachmentName, Sprite sprite)
        {
            SlotName = slotName;
            AttachmentName = attachmentName;
            Sprite = sprite;
        }
    }
    public class StruckAddAnimation
    {
        public readonly string AnimationName;
        public readonly bool Loop;
        public readonly float Delay;
        public readonly float TimeScale;

        public StruckAddAnimation(string animationName, bool loop, float delay, float timeScale)
        {
            AnimationName = animationName;
            Loop = loop;
            Delay = delay;
            TimeScale = timeScale;
        }
    }
    public class DefaultAnimation
    {
    }
}