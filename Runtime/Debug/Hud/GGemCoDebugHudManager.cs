using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// Debug HUD Provider를 관리하고 HUD 스냅샷을 구성하는 중앙 매니저입니다.
    /// </summary>
    public static class GGemCoDebugHudManager
    {
        private sealed class ProviderState
        {
            public ProviderState(IDebugHudProvider provider)
            {
                Provider = provider;
                Elapsed = 0f;
                HasSample = false;
            }

            public IDebugHudProvider Provider { get; }
            public float Elapsed { get; set; }
            public bool HasSample { get; set; }
        }

        private static readonly List<ProviderState> ProviderStates = new();
        private static readonly StringBuilder SnapshotBuilder = new(1024);

        private static bool _initialized;
        private static bool _snapshotDirty;
        private static string _cachedSnapshot = string.Empty;

        public static GGemCoSettings CurrentSettings { get; private set; }
        public static bool IsInitialized => _initialized;

        public static void Initialize(GGemCoSettings settings)
        {
            CurrentSettings = settings;
            _initialized = true;
            RebuildProviders();
            RefreshRootVisibility();
        }

        public static bool TryInitializeFromLoadedSettings()
        {
            AddressableLoaderSettings loader = AddressableLoaderSettings.Instance;
            if (loader == null || loader.settings == null)
            {
                return false;
            }

            Initialize(loader.settings);
            return true;
        }

        public static void Tick(float unscaledDeltaTime)
        {
            if (!_initialized || !GGemCoBuildFlags.AllowDebugFeatures)
            {
                return;
            }

            if (CurrentSettings == null || !CurrentSettings.EnableDebugHud)
            {
                RefreshRootVisibility();
                return;
            }

            bool anyProviderUpdated = false;

            foreach (ProviderState state in ProviderStates)
            {
                if (!state.Provider.IsEnabled(CurrentSettings))
                {
                    state.Elapsed = 0f;
                    state.HasSample = false;
                    continue;
                }

                float interval = Mathf.Max(0.05f, state.Provider.GetUpdateInterval(CurrentSettings));
                state.Elapsed += unscaledDeltaTime;

                if (!state.HasSample || state.Elapsed >= interval)
                {
                    state.Provider.Tick(state.Elapsed);
                    state.Elapsed = 0f;
                    state.HasSample = true;
                    anyProviderUpdated = true;
                }
            }

            if (anyProviderUpdated)
            {
                _snapshotDirty = true;
            }
        }

        public static string BuildSnapshot()
        {
            if (!_initialized || !GGemCoBuildFlags.AllowDebugFeatures || CurrentSettings == null || !CurrentSettings.EnableDebugHud)
            {
                _cachedSnapshot = string.Empty;
                _snapshotDirty = false;
                return _cachedSnapshot;
            }

            if (!_snapshotDirty)
            {
                return _cachedSnapshot;
            }

            SnapshotBuilder.Clear();
            bool wroteAny = false;

            foreach (ProviderState state in ProviderStates)
            {
                if (!state.Provider.IsEnabled(CurrentSettings) || !state.HasSample)
                {
                    continue;
                }

                if (wroteAny)
                {
                    SnapshotBuilder.AppendLine();
                    SnapshotBuilder.AppendLine();
                }

                bool appended = state.Provider.TryBuildContent(SnapshotBuilder);
                wroteAny |= appended;
            }

            _cachedSnapshot = SnapshotBuilder.ToString();
            _snapshotDirty = false;
            return _cachedSnapshot;
        }

        public static void MarkDirty()
        {
            _snapshotDirty = true;
            RefreshRootVisibility();
        }

        private static void RebuildProviders()
        {
            ProviderStates.Clear();

            if (CurrentSettings == null)
            {
                _snapshotDirty = true;
                return;
            }

            List<(int order, Type type)> providerTypes = new();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface || !typeof(IDebugHudProvider).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    DebugHudProviderAttribute attribute = type.GetCustomAttribute<DebugHudProviderAttribute>();
                    if (attribute == null)
                    {
                        continue;
                    }

                    providerTypes.Add((attribute.Order, type));
                }
            }

            providerTypes.Sort((a, b) => a.order.CompareTo(b.order));

            foreach ((int _, Type type) in providerTypes)
            {
                if (Activator.CreateInstance(type) is IDebugHudProvider provider)
                {
                    provider.Reset();
                    ProviderStates.Add(new ProviderState(provider));
                }
            }

            _snapshotDirty = true;
        }

        private static void RefreshRootVisibility()
        {
            bool shouldShow = GGemCoBuildFlags.AllowDebugFeatures && CurrentSettings != null && CurrentSettings.EnableDebugHud;
            GGemCoDebugHudRoot existingRoot = Object.FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);

            if (!shouldShow)
            {
                if (existingRoot != null)
                {
                    Object.Destroy(existingRoot.gameObject);
                }
                return;
            }

            if (existingRoot != null)
            {
                existingRoot.gameObject.SetActive(true);
                existingRoot.MarkStyleDirty();
                return;
            }

            GameObject rootObject = new GameObject("GGemCoDebugHudRoot");
            rootObject.AddComponent<GGemCoDebugHudRoot>();
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(rootObject);
            }
        }
    }
}
