using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core가 Affect 패키지를 직접 참조하지 않으면서 런타임 어펙트 기능을 호출하기 위한 reflection 브리지입니다.
    /// </summary>
    /// <remarks>
    /// Core Runtime의 의존성 방향을 유지하기 위해 Affect 타입은 문자열 FullName으로 탐색합니다.
    /// Affect 패키지가 설치되지 않은 경우 대부분의 메서드는 아무 작업도 하지 않고 안전하게 반환합니다.
    /// </remarks>
    internal static class AffectRuntimeBridge
    {
        private const string TypeNameAffectComponent = "GGemCo2DAffect.AffectComponent";
        private const string TypeNameAffectApplyContext = "GGemCo2DAffect.AffectApplyContext";
        private const string TypeNameCoreTargetAdapter = "GGemCo2DAffect.CoreAffectTargetAdapter";
        private const string TypeNamePlayerAffectUiPresenter = "GGemCo2DAffect.PlayerAffectUiPresenter";
        private const string TypeNamePlayerAffectHudVisualStatePresenter = "GGemCo2DAffect.PlayerAffectHudVisualStatePresenter";
        private const string TypeNameAddressableLoaderAffect = "GGemCo2DAffect.AddressableLoaderAffect";

        private static readonly Dictionary<string, Type> STypeCache = new(StringComparer.Ordinal);

        /// <summary>
        /// 현재 로드된 Assembly에서 지정한 FullName의 타입을 찾고 결과를 캐시합니다.
        /// </summary>
        /// <param name="fullName">namespace를 포함한 타입 FullName입니다.</param>
        /// <returns>찾은 타입입니다. 없으면 null입니다.</returns>
        private static Type ResolveType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;
            if (STypeCache.TryGetValue(fullName, out Type cached)) return cached;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = null;
                try
                {
                    type = assemblies[i].GetType(fullName, throwOnError: false);
                }
                catch
                {
                    // 일부 동적 Assembly는 타입 탐색 중 예외가 날 수 있으므로 다음 Assembly를 계속 확인합니다.
                }

                if (type != null)
                {
                    STypeCache[fullName] = type;
                    return type;
                }
            }

            STypeCache[fullName] = null;
            return null;
        }

        /// <summary>
        /// 지정한 GameObject에서 컴포넌트를 찾거나 없으면 추가합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="type">추가할 Component 타입입니다.</param>
        /// <returns>조회 또는 추가된 Component입니다.</returns>
        private static Component GetOrAddComponent(GameObject go, Type type)
        {
            if (go == null || type == null) return null;
            Component comp = go.GetComponent(type);
            return comp != null ? comp : go.AddComponent(type);
        }

        /// <summary>
        /// Affect 패키지 런타임이 현재 프로젝트에 로드되어 있는지 확인합니다.
        /// </summary>
        /// <returns>AffectComponent 타입을 찾을 수 있으면 true입니다.</returns>
        internal static bool HasAffectRuntime()
        {
            return ResolveType(TypeNameAffectComponent) != null;
        }

        /// <summary>
        /// Core 대상 어댑터와 AffectComponent를 대상 GameObject에 준비합니다.
        /// </summary>
        /// <param name="go">어펙트 시스템을 연결할 GameObject입니다.</param>
        public static void EnsureAffectSystem(GameObject go)
        {
            if (go == null) return;

            Type adapterType = ResolveType(TypeNameCoreTargetAdapter);
            Type affectCompType = ResolveType(TypeNameAffectComponent);
            if (adapterType == null || affectCompType == null)
            {
                return;
            }

            GetOrAddComponent(go, adapterType);
            GetOrAddComponent(go, affectCompType);
        }

        /// <summary>
        /// 대상 GameObject에 적용된 모든 어펙트를 제거합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        public static void RemoveAll(GameObject go)
        {
            Component affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            MethodInfo method = affectComp.GetType().GetMethod("RemoveAll", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(affectComp, null);
        }

        /// <summary>
        /// 대상 GameObject에서 특정 어펙트를 제거합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="affectUid">제거할 어펙트 UID입니다.</param>
        public static void RemoveAffect(GameObject go, int affectUid)
        {
            if (affectUid <= 0) return;
            Component affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            MethodInfo method = affectComp.GetType().GetMethod("RemoveAffect", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(affectComp, new object[] { affectUid });
        }

        /// <summary>
        /// 대상 GameObject에 어펙트를 적용합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="affectUid">적용할 어펙트 UID입니다.</param>
        /// <param name="durationOverrideSeconds">0보다 크면 지속 시간을 덮어쓸 값입니다.</param>
        public static void ApplyAffect(GameObject go, int affectUid, float durationOverrideSeconds)
        {
            ApplyAffect(go, affectUid, null, durationOverrideSeconds);
        }

        /// <summary>
        /// 출처와 지속 시간 보정 정보를 포함해 대상 GameObject에 어펙트를 적용합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="affectUid">적용할 어펙트 UID입니다.</param>
        /// <param name="source">어펙트 출처 GameObject입니다.</param>
        /// <param name="durationOverrideSeconds">0보다 크면 지속 시간을 덮어쓸 값입니다.</param>
        /// <param name="durationBonusSeconds">0보다 크면 지속 시간에 더할 값입니다.</param>
        /// <param name="healHpBonus">힐 Modifier 최종 회복량에 더할 HP 값입니다.</param>
        /// <param name="healHpMultiplier">힐 Modifier 최종 회복량에 곱할 배율입니다.</param>
        internal static void ApplyAffect(
            GameObject go,
            int affectUid,
            GameObject source,
            float durationOverrideSeconds,
            float durationBonusSeconds = 0f,
            long healHpBonus = 0L,
            float healHpMultiplier = 1f)
        {
            if (affectUid <= 0 || go == null) return;

            EnsureAffectSystem(go);

            Component affectComp = GetAffectComponent(go);
            if (affectComp == null) return;

            MethodInfo method = affectComp.GetType().GetMethod("ApplyAffect", BindingFlags.Instance | BindingFlags.Public);
            if (method == null) return;

            object context = CreateApplyContext(
                source,
                durationOverrideSeconds,
                durationBonusSeconds,
                healHpBonus,
                healHpMultiplier);
            method.Invoke(affectComp, new[] { (object)affectUid, context });
        }

        /// <summary>
        /// stateId가 숫자형 어펙트 UID인 경우 해당 어펙트를 적용합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="stateId">상태 식별자입니다.</param>
        /// <param name="durationOverrideSeconds">0보다 크면 지속 시간을 덮어쓸 값입니다.</param>
        public static void ApplyState(GameObject go, string stateId, float durationOverrideSeconds)
        {
            if (string.IsNullOrWhiteSpace(stateId)) return;
            if (int.TryParse(stateId, out int affectUid) && affectUid > 0)
            {
                ApplyAffect(go, affectUid, durationOverrideSeconds);
            }
        }

        /// <summary>
        /// Affect 패키지 로더를 통해 어펙트 아이콘 Sprite를 조회합니다.
        /// </summary>
        /// <param name="iconKey">조회할 아이콘 키입니다.</param>
        /// <returns>조회된 Sprite입니다. 로더나 키가 없으면 null입니다.</returns>
        public static Sprite TryLoadIconSprite(string iconKey)
        {
            if (string.IsNullOrWhiteSpace(iconKey)) return null;

            Type loaderType = ResolveType(TypeNameAddressableLoaderAffect);
            if (loaderType == null) return null;

            PropertyInfo instanceProp = loaderType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            object instance = instanceProp?.GetValue(null);
            if (instance == null) return null;

            MethodInfo method = loaderType.GetMethod("GetImageIconByName", BindingFlags.Instance | BindingFlags.Public);
            return method?.Invoke(instance, new object[] { iconKey }) as Sprite;
        }

        /// <summary>
        /// 플레이어의 AffectComponent와 PlayerBuffInfo 윈도우를 Affect 패키지 프리젠터에 연결합니다.
        /// </summary>
        /// <remarks>
        /// Core는 Affect 패키지의 구체 UI 타입을 직접 참조하지 않기 위해
        /// 윈도우를 <see cref="UIWindow"/>로만 받고, 실제 Bind 메서드는 reflection으로 호출합니다.
        /// </remarks>
        /// <param name="player">버프 상태를 보유한 플레이어 컴포넌트입니다.</param>
        /// <param name="view">PlayerBuffInfo 윈도우 인스턴스입니다.</param>
        public static void TryBindPlayerBuffInfo(Component player, UIWindow view)
        {
            if (player == null || view == null) return;

            Type presenterType = ResolveType(TypeNamePlayerAffectUiPresenter);
            Type affectCompType = ResolveType(TypeNameAffectComponent);
            if (presenterType == null || affectCompType == null) return;

            Component presenter = player.GetComponent(presenterType) as Component;
            if (presenter == null)
            {
                presenter = player.gameObject.AddComponent(presenterType);
            }

            Component affectComp = player.GetComponent(affectCompType);
            if (affectComp == null) return;

            MethodInfo bind = presenterType.GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public);
            if (bind == null) return;

            // Reflection Invoke는 선택 파라미터 기본값을 자동으로 채우지 않으므로 syncInterval까지 전달합니다.
            bind.Invoke(presenter, new object[] { affectComp, view, 0.10f });
        }

        /// <summary>
        /// 플레이어 Affect 상태를 HUD 시각 상태 수신자에 자동 바인딩합니다.
        /// </summary>
        /// <param name="player">플레이어 컴포넌트입니다.</param>
        /// <param name="receiver">HUD 시각 상태를 받을 객체입니다.</param>
        public static void TryBindPlayerHudAffectState(Player player, IAffectHudVisualStateReceiver receiver)
        {
            if (player == null || receiver == null) return;

            Type presenterType = ResolveType(TypeNamePlayerAffectHudVisualStatePresenter);
            Type affectCompType = ResolveType(TypeNameAffectComponent);
            if (presenterType == null || affectCompType == null)
            {
                return;
            }

            EnsureAffectSystem(player.gameObject);

            Component presenter = player.gameObject.GetComponent(presenterType) as Component;
            if (presenter == null)
            {
                presenter = player.gameObject.AddComponent(presenterType);
            }

            Component affectComp = player.GetComponent(affectCompType);
            if (affectComp == null) return;

            MethodInfo bind = presenterType.GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public);
            if (bind == null) return;

            bind.Invoke(presenter, new object[] { affectComp, receiver, 0.10f });
        }

        /// <summary>
        /// 공격자가 대상을 타격했음을 Affect 런타임에 알립니다.
        /// </summary>
        /// <param name="attacker">공격자 GameObject입니다.</param>
        /// <param name="hitTarget">피격 대상 GameObject입니다.</param>
        public static void NotifyOnHit(GameObject attacker, GameObject hitTarget)
        {
            if (attacker == null || hitTarget == null) return;

            EnsureAffectSystem(attacker);

            Component affectComp = GetAffectComponent(attacker);
            if (affectComp == null) return;

            MethodInfo method = affectComp.GetType().GetMethod("NotifyHit", BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(affectComp, new object[] { hitTarget });
        }

        /// <summary>
        /// 대상 GameObject에 특정 어펙트가 적용되어 있는지 확인합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="affectUid">확인할 어펙트 UID입니다.</param>
        /// <returns>어펙트가 적용되어 있으면 true입니다.</returns>
        public static bool HasAffect(GameObject go, int affectUid)
        {
            if (go == null || affectUid <= 0)
            {
                return false;
            }

            EnsureAffectSystem(go);
            return HasAffectInternal(go, affectUid);
        }

        /// <summary>
        /// 컴포넌트 자동 추가 없이 대상 GameObject의 기존 어펙트 보유 여부만 확인합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="affectUid">확인할 어펙트 UID입니다.</param>
        /// <returns>기존 AffectComponent에 어펙트가 있으면 true입니다.</returns>
        public static bool HasAttachedAffect(GameObject go, int affectUid)
        {
            if (go == null || affectUid <= 0)
            {
                return false;
            }

            return HasAffectInternal(go, affectUid);
        }

        /// <summary>
        /// AffectComponent의 HasAffect API를 reflection으로 호출합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <param name="affectUid">확인할 어펙트 UID입니다.</param>
        /// <returns>어펙트가 적용되어 있으면 true입니다.</returns>
        private static bool HasAffectInternal(GameObject go, int affectUid)
        {
            Component affectComp = GetAffectComponent(go);
            if (affectComp == null)
            {
                return false;
            }

            MethodInfo method = affectComp.GetType().GetMethod("HasAffect", BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                return false;
            }

            try
            {
                object result = method.Invoke(affectComp, new object[] { affectUid });
                return result is bool has && has;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 대상 GameObject에서 AffectComponent를 조회합니다.
        /// </summary>
        /// <param name="go">대상 GameObject입니다.</param>
        /// <returns>조회된 AffectComponent입니다.</returns>
        private static Component GetAffectComponent(GameObject go)
        {
            if (go == null) return null;
            Type type = ResolveType(TypeNameAffectComponent);
            return type != null ? go.GetComponent(type) : null;
        }

        /// <summary>
        /// 어펙트 적용에 필요한 선택 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="source">어펙트 출처 GameObject입니다.</param>
        /// <param name="durationOverrideSeconds">0보다 크면 지속 시간을 덮어쓸 값입니다.</param>
        /// <param name="durationBonusSeconds">0보다 크면 지속 시간에 더할 값입니다.</param>
        /// <param name="healHpBonus">힐 Modifier 최종 회복량에 더할 HP 값입니다.</param>
        /// <param name="healHpMultiplier">힐 Modifier 최종 회복량에 곱할 배율입니다.</param>
        /// <returns>생성된 AffectApplyContext입니다. 추가 정보가 없거나 타입이 없으면 null입니다.</returns>
        private static object CreateApplyContext(
            GameObject source,
            float durationOverrideSeconds,
            float durationBonusSeconds,
            long healHpBonus = 0L,
            float healHpMultiplier = 1f)
        {
            if (source == null &&
                durationOverrideSeconds <= 0f &&
                durationBonusSeconds <= 0f &&
                healHpBonus <= 0L &&
                Mathf.Approximately(healHpMultiplier, 1f))
            {
                return null;
            }

            Type contextType = ResolveType(TypeNameAffectApplyContext);
            if (contextType == null)
            {
                return null;
            }

            object context = Activator.CreateInstance(contextType);
            SetMemberValue(contextType, context, "Source", source);

            if (durationOverrideSeconds > 0f)
            {
                SetMemberValue(contextType, context, "DurationOverride", durationOverrideSeconds);
            }

            if (durationBonusSeconds > 0f)
            {
                SetMemberValue(contextType, context, "DurationBonusSeconds", durationBonusSeconds);
            }

            if (healHpBonus > 0L)
            {
                SetMemberValue(contextType, context, "HealHpBonus", healHpBonus);
            }

            if (healHpMultiplier > 0f && !Mathf.Approximately(healHpMultiplier, 1f))
            {
                SetMemberValue(contextType, context, "HealHpMultiplier", healHpMultiplier);
            }

            return context;
        }

        /// <summary>
        /// reflection 대상 객체의 public 프로퍼티 또는 필드에 값을 설정합니다.
        /// </summary>
        /// <param name="type">대상 객체 타입입니다.</param>
        /// <param name="target">대상 객체입니다.</param>
        /// <param name="memberName">설정할 멤버 이름입니다.</param>
        /// <param name="value">설정할 값입니다.</param>
        private static void SetMemberValue(Type type, object target, string memberName, object value)
        {
            PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
                return;
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
