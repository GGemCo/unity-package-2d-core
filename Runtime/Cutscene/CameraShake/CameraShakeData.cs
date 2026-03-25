using System;

namespace GGemCo2DCore
{
    [Serializable]
    public class CameraShakeData
    {
        public float duration;
        public float shakeIntensity;
        public float leftStrength;
        public float rightStrength;
        public float downStrength;
        public float upStrength;
        public int repeatCount = 3;
        public bool useUnscaledTime;

        public float GetLeftStrength()
        {
            return leftStrength > 0f ? leftStrength : shakeIntensity;
        }

        public float GetRightStrength()
        {
            return rightStrength > 0f ? rightStrength : shakeIntensity;
        }

        public float GetDownStrength()
        {
            return downStrength > 0f ? downStrength : shakeIntensity;
        }

        public float GetUpStrength()
        {
            return upStrength > 0f ? upStrength : shakeIntensity;
        }

        public int GetRepeatCount()
        {
            return repeatCount > 0 ? repeatCount : 3;
        }
    }
}
