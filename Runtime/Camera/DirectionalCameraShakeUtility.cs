using UnityEngine;

namespace GGemCo2DCore
{
    public enum DirectionalCameraShakeMode
    {
        PresetRaw = 0,
        FromCasterToTarget = 1,
        RecoilFromCasterToTarget = 2,
        HorizontalFromCasterToTarget = 3,
        HorizontalRecoilFromCasterToTarget = 4,
    }

    public static class DirectionalCameraShakeUtility
    {
        public static CameraShakeRequest CreateRequest(
            CameraShakePreset preset,
            Transform caster,
            Transform target,
            DirectionalCameraShakeMode mode,
            CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            if (preset == null)
            {
                return default;
            }

            if (mode == DirectionalCameraShakeMode.PresetRaw)
            {
                return preset.ToRequest(channel);
            }

            if (caster == null || target == null)
            {
                return preset.ToRequest(channel);
            }

            Vector2 direction = target.position - caster.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return preset.ToRequest(channel);
            }

            switch (mode)
            {
                case DirectionalCameraShakeMode.RecoilFromCasterToTarget:
                case DirectionalCameraShakeMode.HorizontalRecoilFromCasterToTarget:
                    direction = -direction;
                    break;
            }

            switch (mode)
            {
                case DirectionalCameraShakeMode.HorizontalFromCasterToTarget:
                case DirectionalCameraShakeMode.HorizontalRecoilFromCasterToTarget:
                    direction.y = 0f;
                    break;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = new Vector2(Mathf.Sign(target.position.x - caster.position.x), 0f);
                if (Mathf.Approximately(direction.x, 0f))
                {
                    return preset.ToRequest(channel);
                }
            }

            direction.Normalize();

            float xWeight = Mathf.Abs(direction.x);
            float yWeight = Mathf.Abs(direction.y);

            return new CameraShakeRequest
            {
                Duration = Mathf.Max(0f, preset.Duration),
                RepeatCount = Mathf.Max(1, preset.RepeatCount),
                LeftStrength = direction.x < 0f ? preset.LeftStrength * xWeight : 0f,
                RightStrength = direction.x > 0f ? preset.RightStrength * xWeight : 0f,
                DownStrength = direction.y < 0f ? preset.DownStrength * yWeight : 0f,
                UpStrength = direction.y > 0f ? preset.UpStrength * yWeight : 0f,
                Channel = channel,
                UseUnscaledTime = preset.UseUnscaledTime,
            };
        }
    }
}
