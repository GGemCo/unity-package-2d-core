#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디버그 HUD 프로바이더 초기화와 루트 프레젠터 생성을 담당하는 매니저입니다.
    /// Settings 로드 완료 후 명시적으로 Initialize 를 호출해야 합니다.
    /// </summary>
    internal static class GGemCoDebugHudManager
    {
        private static readonly List<IDebugHudProvider> Providers = new()
        {
            new TilemapDrawCallEstimator(),
            new FpsHud(),
            new Physics2DHud(),
            new MemoryHud(),
        };

        private static GGemCoSettings _cachedSettings;
        private static bool _initialized;

        internal static IReadOnlyList<IDebugHudProvider> RegisteredProviders => Providers;
        internal static bool IsInitialized => _initialized;
        internal static GGemCoSettings Settings => _cachedSettings;

        internal static void Initialize(GGemCoSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            _cachedSettings = settings;
            _initialized = true;

            foreach (IDebugHudProvider provider in Providers)
            {
                provider.Initialize(settings);
            }

            EnsureRootState(settings);
        }

        internal static bool TryInitializeFromLoadedSettings()
        {
            if (_initialized && _cachedSettings != null)
            {
                EnsureRootState(_cachedSettings);
                return true;
            }

            GGemCoSettings settings = AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.settings
                : null;

            if (settings == null)
            {
                return false;
            }

            Initialize(settings);
            return true;
        }

        internal static void EnsureRootState(GGemCoSettings settings)
        {
            if (settings == null)
            {
                DestroyRootIfExists();
                return;
            }

            if (!DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud) || !HasAnyEnabledProvider(settings))
            {
                DestroyRootIfExists();
                return;
            }

            GGemCoDebugHudRoot existing = Object.FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.ApplySettings(settings);
                return;
            }

            GameObject rootObject = new("GGemCoDebug");
            Object.DontDestroyOnLoad(rootObject);
            GGemCoDebugHudRoot root = rootObject.AddComponent<GGemCoDebugHudRoot>();
            root.ApplySettings(settings);
        }

        internal static bool HasAnyEnabledProvider(GGemCoSettings settings)
        {
            foreach (IDebugHudProvider provider in Providers)
            {
                if (provider.IsEnabled(settings))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DestroyRootIfExists()
        {
            GGemCoDebugHudRoot existing = Object.FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(existing.gameObject);
                return;
            }

            Object.DestroyImmediate(existing.gameObject);
        }
    }
}
#endif
