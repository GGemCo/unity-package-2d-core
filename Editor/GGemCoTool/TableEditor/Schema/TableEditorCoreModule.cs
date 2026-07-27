using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorCoreModule : ITableEditorModule
    {
        public string ModuleName => "Core";
        public string PackageName => "Core";

        /// <summary>
        /// Core 런타임 어셈블리에서 테이블 파서를 찾아 TableEditor 정의 목록을 생성합니다.
        /// </summary>
        /// <returns>TableEditor에 표시할 Core 테이블 정의 목록입니다.</returns>
        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            List<AddressableAssetInfo> infos = ConfigAddressableTable.All;
            Type defaultTableType = typeof(DefaultTable<>);
            Type runtimeAssemblyType = typeof(DefaultTable<>);

            foreach (Type type in runtimeAssemblyType.Assembly.GetTypes())
            {
                if (type.IsAbstract)
                    continue;

                if (!TryGetDefaultTableBaseType(type, defaultTableType, out Type tableBaseType))
                    continue;

                object tableInstance;
                try
                {
                    tableInstance = Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                if (!(tableInstance is ITableParser parser))
                    continue;

                string key = parser.Key;
                if (string.IsNullOrWhiteSpace(key) || string.Equals(key, ConfigAddressableTable.None, StringComparison.OrdinalIgnoreCase))
                    continue;

                AddressableAssetInfo addressable = infos.FirstOrDefault(a => string.Equals(a.Etc1, key, StringComparison.OrdinalIgnoreCase));
                if (addressable == null)
                    continue;

                Func<string, MemberInfo, string> resolveColumnTooltip = null;
                if (string.Equals(key, ConfigAddressableTable.MonsterCombatProfile, StringComparison.OrdinalIgnoreCase))
                    resolveColumnTooltip = ResolveMonsterCombatProfileColumnTooltip;

                yield return TableEditorDefinitionFactory.Create(
                    ModuleName,
                    PackageName,
                    key,
                    addressable.Path,
                    key,
                    type,
                    tableBaseType.GetGenericArguments()[0],
                    TableEditorDefinitionFactory.CreateDefaultReloadAction(addressable.Path),
                    ResolveReference,
                    resolveColumnTooltip);
            }
        }

        /// <summary>
        /// 테이블 타입의 상속 체인에서 DefaultTable&lt;T&gt; 기반 타입을 찾습니다.
        /// </summary>
        /// <param name="type">검사할 테이블 타입입니다.</param>
        /// <param name="defaultTableType">비교 기준인 DefaultTable 타입입니다.</param>
        /// <param name="tableBaseType">찾은 DefaultTable&lt;T&gt; 기반 타입입니다.</param>
        /// <returns>DefaultTable&lt;T&gt; 기반 타입을 찾으면 true를 반환합니다.</returns>
        private static bool TryGetDefaultTableBaseType(Type type, Type defaultTableType, out Type tableBaseType)
        {
            tableBaseType = null;
            for (Type current = type.BaseType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                    continue;

                if (current.GetGenericTypeDefinition() != defaultTableType)
                    continue;

                tableBaseType = current;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 컬럼 헤더명을 기준으로 참조 가능한 테이블 정의를 찾습니다.
        /// </summary>
        /// <param name="headerName">참조 컬럼 헤더명입니다.</param>
        /// <returns>참조 테이블 정의입니다. 찾지 못하면 null을 반환합니다.</returns>
        private static TableEditorTableDefinition ResolveReference(string headerName)
        {
            return TableEditorRegistry.FindReferenceTable(headerName);
        }

        /// <summary>
        /// monster_combat_profile 테이블 컬럼의 용도, 단위 및 런타임 기본값을 설명하는 Tooltip을 반환합니다.
        /// </summary>
        /// <param name="headerName">설명을 찾을 테이블 컬럼 헤더명입니다.</param>
        /// <param name="memberInfo">컬럼과 연결된 Row 멤버입니다. 현재 설명은 헤더명을 기준으로 결정합니다.</param>
        /// <returns>등록된 컬럼 설명이며, 알 수 없는 컬럼이면 기본 Tooltip을 사용하도록 <see langword="null"/>을 반환합니다.</returns>
        private static string ResolveMonsterCombatProfileColumnTooltip(string headerName, MemberInfo memberInfo)
        {
            // 테이블에 새 컬럼이 추가되더라도 null을 반환하면 공통 형식/참조 안내로 fallback됩니다.
            switch (headerName)
            {
                case "Uid":
                    return "몬스터 전투 프로필을 식별하는 고유 UID입니다.";
                case "Name":
                    return "테이블 에디터에서 프로필을 구분하기 위한 이름입니다.";
                case "Memo":
                    return "프로필의 설계 의도나 사용 대상을 기록하는 디자이너 메모입니다.";

                case "DetectionRangeX":
                    return "몬스터 중심에서 플레이어를 선공 감지할 X축 반경입니다. 0 이하면 기존 공격 판정 Collider의 X축 범위를 사용합니다.";
                case "DetectionRangeY":
                    return "몬스터 중심에서 플레이어를 선공 감지할 Y축 반경입니다. 0 이하면 기존 공격 판정 Collider의 Y축 범위를 사용합니다.";
                case "DetectionExitRangeX":
                    return "감지한 플레이어가 벗어났다고 판정할 X축 반경입니다. 감지 반경보다 작거나 0 이하면 DetectionRangeX를 사용합니다.";
                case "DetectionExitRangeY":
                    return "감지한 플레이어가 벗어났다고 판정할 Y축 반경입니다. 감지 반경보다 작거나 0 이하면 DetectionRangeY를 사용합니다.";
                case "BasicAttackRangeX":
                    return "기본 공격을 시작할 수 있는 X축 거리입니다. 0 이하면 기존 공격 판정 Collider의 X축 범위를 사용합니다.";
                case "BasicAttackRangeY":
                    return "기본 공격을 시작할 수 있는 Y축 거리입니다. 0 이하면 기존 공격 판정 Collider의 Y축 범위를 사용합니다.";
                case "PreferredRangeMin":
                    return "몬스터가 유지하려는 최소 전투 거리입니다. 0 이상 PreferredRangeMax 이하로 보정됩니다.";
                case "PreferredRangeMax":
                    return "몬스터가 유지하려는 최대 전투 거리입니다. 0 이하면 BasicAttackRangeX를 사용합니다.";
                case "ChaseRange":
                    return "타겟 추적을 포기할 2D 거리입니다. 0 이하면 별도의 추적 거리 제한을 사용하지 않습니다.";

                case "SoftLeashRange":
                    return "홈 위치에서 이 거리보다 멀어지면 유예 시간 후 Evade를 시작합니다. 0 이하면 소프트 Leash를 사용하지 않습니다.";
                case "HardLeashRange":
                    return "홈 위치에서 이 거리보다 멀어지면 즉시 Evade를 시작합니다. 0 이하면 하드 Leash를 사용하지 않습니다.";
                case "SoftLeashGraceSeconds":
                    return "소프트 Leash 범위를 벗어난 뒤 Evade를 시작하기 전 유예 시간(초)입니다. 0 이하면 기본값 1.5초를 사용합니다.";
                case "ReturnStopDistance":
                    return "홈 위치에 도착한 것으로 판정할 거리입니다. 0 이하면 기본값 0.1을 사용합니다.";
                case "ReturnDelaySeconds":
                    return "홈 도착 후 감지와 AI를 다시 활성화하기 전 대기 시간(초)입니다. 음수 값은 0으로 보정됩니다.";
                case "ReturnMoveSpeedMultiplier":
                    return "홈 복귀 이동에 적용할 이동 속도 배율입니다. 0 이하면 기본값 1을 사용합니다.";
                case "ReturnTimeoutSeconds":
                    return "홈 복귀 이동의 제한 시간(초)입니다. 초과하면 홈 위치로 보정하며, 0 이하면 기본값 8초를 사용합니다.";
                case "LeashRecoveryPolicy":
                    return "Evade 중 자원을 회복할 시점입니다. None, OnEvadeStart, OnHomeReached 중에서 선택합니다.";
                case "InvulnerableDuringReturn":
                    return "활성화하면 홈 복귀 및 재활성 대기 중에 받는 피해를 무시합니다.";
                case "ClearAffectsOnEvade":
                    return "활성화하면 Evade를 시작할 때 현재 적용 중인 Affect를 모두 제거합니다.";

                case "DetectionTargetRetentionPolicy":
                    return "감지 범위를 벗어난 뒤 전투 타겟을 유지하는 정책입니다. DistanceBased는 거리 정책을 따르고, UntilCombatReleased는 명시적인 전투 종료까지 유지합니다.";
                case "DetectionThreat":
                    return "감지 범위에 들어온 대상에게 등록할 기본 Threat입니다. 0 이하면 기본값 1을 사용합니다.";
                case "DamageThreatMultiplier":
                    return "확정 피해량을 Threat로 변환할 때 곱하는 배율입니다. 0 이하면 기본값 1을 사용합니다.";
                case "MinimumDamageThreat":
                    return "피해량이 작더라도 보장할 최소 피해 Threat입니다. 0 이하면 기본값 1을 사용합니다.";
                case "TargetSwitchThreatRatio":
                    return "새 후보로 타겟을 전환할 때 필요한 현재 타겟 대비 Threat 비율입니다. 1은 더 높으면 즉시 전환하고, 1.1은 10% 이상 높아야 전환합니다. 0 이하면 1.1을 사용합니다.";
                case "MaxThreatTargets":
                    return "몬스터 한 개체가 동시에 기억할 최대 Threat 대상 수입니다. 0 이하면 기본값 16을 사용하며, 최대 64로 제한됩니다.";

                case "EncounterThreat":
                    return "Encounter 볼륨 또는 동료 지원으로 대상을 등록할 때 부여하는 Threat입니다. 0 이하면 기본값 1을 사용합니다.";
                case "EncounterAssistRadius":
                    return "같은 Encounter 그룹의 동료에게 지원 어그로를 전달할 최대 거리입니다. 0 이하면 거리 제한을 사용하지 않습니다.";
                case "MaxEncounterAssistCount":
                    return "한 번의 지원 요청으로 활성화할 최대 Encounter 동료 수입니다. 0 이하면 제한하지 않으며, 최대 32로 제한됩니다.";

                case "AttackSlotType":
                    return "동일 타겟에 대한 동시 공격 수를 제한할 슬롯 종류입니다. None은 비활성, Melee는 근접, Ranged는 원거리 슬롯을 사용합니다.";
                case "MaxConcurrentAttackers":
                    return "동일 타겟에 동시에 공격을 예약할 수 있는 최대 몬스터 수입니다. 0 이하면 Melee는 2, Ranged는 3을 사용하며, 최대 16으로 제한됩니다.";
                case "AttackSlotReservationSeconds":
                    return "갱신되지 않은 공격 슬롯 예약을 자동 반환할 시간(초)입니다. 0 이하면 기본값 4초를 사용합니다.";
                case "AttackSlotPostActionHoldSeconds":
                    return "공격 또는 스킬 종료 후 슬롯을 추가로 유지할 시간(초)입니다. 0은 즉시 반환하고, 음수이면 기본값 0.2초를 사용합니다.";
                default:
                    return null;
            }
        }
    }
}
