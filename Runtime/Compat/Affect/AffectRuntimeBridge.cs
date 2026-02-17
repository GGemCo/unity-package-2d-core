using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core가 Affect 패키지를 직접 참조하지 않으면서,
    /// 런타임에 AffectComponent/AddressableLoaderAffect 등 '옵션 기능'을 사용할 수 있도록
    /// Reflection 기반으로 연결해주는 브리지.
    /// </summary>
    internal static class AffectRuntimeBridge
    {
        // Type FullName (namespace 포함)
        private const string TypeNameAffectComponent = "GGemCo2DAffect.AffectComponent";
        private const string TypeNameAffectApplyContext = "GGemCo2DAffect.AffectApplyContext";
        private const string TypeNameCoreTargetAdapter = "GGemCo2DAffect.CoreAffectTargetAdapter";
        private const string TypeNamePlayerAffectUiPresenter = "GGemCo2DAffect.PlayerAffectUiPresenter";
        private const string TypeNameAddressableLoaderAffect = "GGemCo2DAffect.AddressableLoaderAffect";

        private static readonly Dictionary<string, Type> STypeCache = new(StringComparer.Ordinal);

        private static Type ResolveType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;
            if (STypeCache.TryGetValue(fullName, out var cached)) return cached;

            // Type.GetType는 어셈블리 한정자가 없으면 실패할 수 있으므로,
            // 로드된 모든 Assembly에서 찾는다.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type t = null;
                try
                {
                    t = assemblies[i].GetType(fullName, throwOnError: false);
                }
                catch
                {
                    // 일부 동적 Assembly는 GetType에서 예외가 날 수 있다.
                }

                if (t != null)
                {
                    STypeCache[fullName] = t;
                    return t;
                }
            }

            STypeCache[fullName] = null;
            return null;
        }

        private static Component GetOrAddComponent(GameObject go, Type type)
        {
            if (go == null || type == null) return null;
            var comp = go.GetComponent(type);
            if (comp != null) return comp;
            return go.AddComponent(type);
        }

        /// <summary>
        /// Affect 패키지 런타임이 설치되어 있는지 여부.
        /// - Core는 Affect를 직접 참조하지 않으므로 Reflection으로 확인합니다.
        /// </summary>
        internal static bool HasAffectRuntime()
        {
            // 핵심 타입 중 하나라도 있으면 설치된 것으로 본다.
            return ResolveType(TypeNameAffectComponent) != null;
        }

        /// <summary>
        /// Affect 런타임 시스템(타겟 어댑터 + AffectComponent)을 자동 부착한다.
        /// Affect가 설치되지 않았다면 아무 일도 하지 않는다.
        /// </summary>
        public static void EnsureAffectSystem(GameObject go)
        {
            if (go == null) return;

            var adapterType = ResolveType(TypeNameCoreTargetAdapter);
            var affectCompType = ResolveType(TypeNameAffectComponent);

            // Affect 미설치
            if (adapterType == null || affectCompType == null)
                return;

            GetOrAddComponent(go, adapterType);
            GetOrAddComponent(go, affectCompType);
        }

        public static void RemoveAll(GameObject go)
        {
            var affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            var method = affectComp.GetType().GetMethod("RemoveAll", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(affectComp, null);
        }

        public static void RemoveAffect(GameObject go, int affectUid)
        {
            if (affectUid <= 0) return;
            var affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            var method = affectComp.GetType().GetMethod("RemoveAffect", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(affectComp, new object[] { affectUid });
        }

        public static void ApplyAffect(GameObject go, int affectUid, float durationOverrideSeconds)
        {
            if (affectUid <= 0) return;
            if (go == null) return;

            EnsureAffectSystem(go);

            var affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            // ApplyAffect(int, AffectApplyContext)
            var method = affectComp.GetType().GetMethod("ApplyAffect", BindingFlags.Instance | BindingFlags.Public);
            if (method == null) return;

            object ctx = null;
            if (durationOverrideSeconds > 0f)
            {
                var ctxType = ResolveType(TypeNameAffectApplyContext);
                if (ctxType != null)
                {
                    ctx = Activator.CreateInstance(ctxType);

                    // AffectApplyContext.DurationOverride 프로퍼티/필드 모두 대응
                    var prop = ctxType.GetProperty("DurationOverride", BindingFlags.Instance | BindingFlags.Public);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(ctx, durationOverrideSeconds);
                    }
                    else
                    {
                        var field = ctxType.GetField("DurationOverride", BindingFlags.Instance | BindingFlags.Public);
                        field?.SetValue(ctx, durationOverrideSeconds);
                    }
                }
            }

            // context는 null 허용
            method.Invoke(affectComp, new[] { (object)affectUid, ctx });
        }

        internal static void ApplyAffect(GameObject go, int affectUid, GameObject source, float durationOverrideSeconds)
        {
            if (affectUid <= 0) return;
            if (go == null) return;

            EnsureAffectSystem(go);

            var affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            // ApplyAffect(int, AffectApplyContext)
            var method = affectComp.GetType().GetMethod("ApplyAffect", BindingFlags.Instance | BindingFlags.Public);
            if (method == null) return;

            object ctx = null;

            // Source 또는 DurationOverride 중 하나라도 있으면 컨텍스트를 생성한다.
            if (source != null || durationOverrideSeconds > 0f)
            {
                var ctxType = ResolveType(TypeNameAffectApplyContext);
                if (ctxType != null)
                {
                    ctx = Activator.CreateInstance(ctxType);

                    // AffectApplyContext.Source (public field) 대응
                    var srcField = ctxType.GetField("Source", BindingFlags.Instance | BindingFlags.Public);
                    srcField?.SetValue(ctx, source);

                    // AffectApplyContext.DurationOverride 프로퍼티/필드 모두 대응
                    if (durationOverrideSeconds > 0f)
                    {
                        var prop = ctxType.GetProperty("DurationOverride", BindingFlags.Instance | BindingFlags.Public);
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(ctx, durationOverrideSeconds);
                        }
                        else
                        {
                            var field = ctxType.GetField("DurationOverride", BindingFlags.Instance | BindingFlags.Public);
                            field?.SetValue(ctx, durationOverrideSeconds);
                        }
                    }
                }
            }

            // context는 null 허용
            method.Invoke(affectComp, new[] { (object)affectUid, ctx });
        }


        /// <summary>
        /// Optional hook: apply a state option.
        /// - If stateId is an integer, it is treated as an Affect UID and forwarded.
        /// - Otherwise, do nothing (your Affect runtime can implement a mapping externally).
        /// </summary>
        public static void ApplyState(GameObject go, string stateId, float durationOverrideSeconds)
        {
            if (string.IsNullOrWhiteSpace(stateId)) return;
            if (int.TryParse(stateId, out var affectUid) && affectUid > 0)
            {
                ApplyAffect(go, affectUid, durationOverrideSeconds);
            }
        }

        public static Sprite TryLoadIconSprite(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey)) return null;

            var loaderType = ResolveType(TypeNameAddressableLoaderAffect);
            if (loaderType == null) return null;

            // AddressableLoaderAffect.Instance
            var instanceProp = loaderType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            var instance = instanceProp?.GetValue(null);
            if (instance == null) return null;

            var method = loaderType.GetMethod("GetImageIconByName", BindingFlags.Instance | BindingFlags.Public);
            if (method == null) return null;

            return method.Invoke(instance, new object[] { iconKey }) as Sprite;
        }

        public static void TryBindPlayerBuffInfo(Component player, UIWindowPlayerBuffInfo view)
        {
            if (player == null || view == null) return;

            var presenterType = ResolveType(TypeNamePlayerAffectUiPresenter);
            var affectCompType = ResolveType(TypeNameAffectComponent);
            if (presenterType == null || affectCompType == null) return;

            // presenter 부착
            var presenter = player.GetComponent(presenterType) as Component;
            if (presenter == null)
                presenter = player.gameObject.AddComponent(presenterType);

            var affectComp = player.GetComponent(affectCompType);
            if (affectComp == null) return;

            // Bind(AffectComponent, UIWindowPlayerBuffInfo)
            // C# 호출에서는 옵션 파라미터가 자동으로 채워지지만, Reflection Invoke는 자동으로 기본값을 채우지 않습니다.
            var bind = presenterType.GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public);
            if (bind == null) return;

            // todo. 정리 필요. 0.1f
            bind.Invoke(presenter, new object[] { affectComp, view, 0.10f });
        }

        private static Component GetAffectComponent(GameObject go)
        {
            if (go == null) return null;
            var t = ResolveType(TypeNameAffectComponent);
            if (t == null) return null;
            return go.GetComponent(t);
        }
    }
}