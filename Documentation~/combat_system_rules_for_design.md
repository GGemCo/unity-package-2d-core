# 개편 전투 시스템 규칙서

> 대상: 기획팀, 레벨 디자인팀, 전투 밸런스 담당자  
> 기준 버전: 전투 시스템 개편 1~6단계  
> 문서 목적: 몬스터의 감지, 교전, 타겟 선택, 추적, 귀환, Encounter, 동시 공격 제한 규칙을 데이터 설계 관점에서 정의합니다.

---

## 1. 시스템 목표

개편 전투 시스템은 다음 목표를 가집니다.

1. 플레이어가 여러 몬스터와 동시에 교전해도 전투 상태가 안정적으로 유지되어야 합니다.
2. 감지 범위, 공격 시작 거리, 실제 타격 영역, 추적 거리, 귀환 범위를 서로 독립적으로 조정할 수 있어야 합니다.
3. 몬스터는 여러 대상을 기억하고 Threat에 따라 현재 공격 대상을 선택할 수 있어야 합니다.
4. 몬스터가 지정된 전투 구역을 과도하게 벗어나면 안전하게 전투를 중단하고 홈으로 복귀해야 합니다.
5. 한 플레이어에게 모든 몬스터가 동시에 공격하지 않도록 근접·원거리 공격 권한을 제한할 수 있어야 합니다.
6. 보스방, 매복, 그룹 전투처럼 구역 단위로 몬스터를 활성화할 수 있어야 합니다.

---

## 2. 핵심 용어

| 용어 | 의미 |
|---|---|
| 전투 참여 목록 | 현재 플레이어와 교전 관계인 몬스터 집합입니다. |
| Threat | 몬스터가 각 전투 대상을 얼마나 우선적으로 공격해야 하는지를 나타내는 수치입니다. |
| 현재 전투 타겟 | Threat 규칙에 따라 몬스터가 현재 선택한 한 명의 대상입니다. |
| 감지 범위 | 선공 몬스터가 플레이어를 최초로 발견하는 논리 범위입니다. |
| 감지 이탈 범위 | 이미 감지한 플레이어를 놓친 것으로 판정하는 더 넓은 범위입니다. |
| 기본 공격 시작 범위 | AI가 기본 공격 애니메이션을 시작할 수 있는 거리입니다. |
| 실제 타격 영역 | 공격 애니메이션의 타격 프레임에서 피해 대상을 수집하는 Collider2D 영역입니다. |
| 선호 전투 거리 | 몬스터가 전투 중 유지하려는 최소·최대 거리입니다. |
| Chase Range | 현재 타겟 추적을 포기할 거리 제한입니다. |
| Home | 몬스터가 생성된 위치와 초기 방향입니다. |
| Soft Leash | 잠시 초과할 수 있지만 일정 시간 이상 유지되면 귀환하는 범위입니다. |
| Hard Leash | 초과 즉시 전투를 중단하고 귀환하는 절대 범위입니다. |
| Evade | Threat와 공격을 정리하고 홈 복귀를 시작하는 상태입니다. |
| Encounter | 같은 그룹 ID를 가진 몬스터를 구역 또는 동료 피격으로 함께 활성화하는 시스템입니다. |
| 공격 슬롯 | 동일 대상을 동시에 공격할 수 있는 몬스터 수를 제한하는 권한 토큰입니다. |

---

## 3. 전투 상태 규칙

### 3.1 플레이어 전투 상태

플레이어의 전투 상태는 단일 몬스터가 직접 켜거나 끄지 않습니다.

- 전투 참여 목록에 유효한 몬스터가 하나 이상 있으면 `InBattle`입니다.
- 몬스터 한 마리가 사망, 귀환 또는 이탈해도 다른 교전 몬스터가 남아 있으면 전투 상태를 유지합니다.
- 마지막 교전 몬스터가 해제되었을 때 전투 상태가 종료됩니다.
- 플레이어 사망 또는 맵 이동 시 전투 참여 목록을 전체 초기화합니다.

### 3.2 몬스터 전투 상태

몬스터는 Threat 목록에 유효한 대상이 존재하면 교전 상태입니다.

- 감지, 패트롤, 피해, 외부 도발, Encounter는 서로 독립적인 Threat 원인입니다.
- 한 원인이 제거되어도 다른 원인이 남아 있으면 교전을 유지합니다.
- 현재 타겟이 사망하면 전체 전투를 종료하지 않고 다음 Threat 대상을 선택합니다.
- 유효한 Threat 대상이 모두 제거되면 전투가 종료됩니다.

---

## 4. 전투 시작 규칙

### 4.1 선공 감지

`AttackType=AggroFirst` 몬스터만 몬스터 중심 감지 범위를 사용합니다.

1. 플레이어가 `DetectionRangeX/Y` 안으로 들어옵니다.
2. 감지 Threat가 등록됩니다.
3. 몬스터가 현재 전투 타겟을 선택합니다.
4. 플레이어의 전투 참여 목록에 해당 몬스터가 등록됩니다.

현재 감지는 축 정렬 사각형 범위로 후보를 찾으며, 시야 차단 또는 장애물 Raycast는 적용하지 않습니다.

### 4.2 후공 몬스터

`AttackType=PassiveDefense` 몬스터는 일반 감지로 먼저 공격하지 않습니다.

- 플레이어 또는 다른 전투 대상에게 실제 피해를 받으면 피해 Threat가 등록됩니다.
- 피해량이 높을수록 Threat가 크게 누적됩니다.
- 동료 지원 Encounter가 설정되어 있으면 주변 그룹 몬스터도 교전에 참여할 수 있습니다.

### 4.3 패트롤 및 Encounter 볼륨

`ObjectPatrol`은 두 방식으로 동작합니다.

- `EncounterId=0`: 기존처럼 연결된 몬스터 한 마리만 활성화합니다.
- `EncounterId>0`: 같은 Encounter ID를 가진 그룹 전체를 활성화합니다.

`ReleaseEncounterThreatOnExit` 정책:

| 값 | 플레이어가 볼륨을 나갔을 때 |
|---|---|
| `false` | Encounter Threat를 유지하며 이후 Threat·Leash 규칙이 전투 종료를 결정합니다. |
| `true` | Encounter 원인 Threat만 제거합니다. 피해·도발 등 다른 Threat가 있으면 전투는 유지됩니다. |

보스방이나 강제 전투 구역에는 일반적으로 `false`를 권장합니다.

---

## 5. Threat 및 타겟 선택 규칙

### 5.1 Threat 원인

| 원인 | 설명 | 일반적인 제거 시점 |
|---|---|---|
| DetectionRange | 선공 감지 범위에 플레이어가 존재합니다. | 감지 이탈 조건 충족 시 |
| Patrol | 기존 패트롤 볼륨으로 전투가 시작되었습니다. | 패트롤 정책에 따른 이탈 시 |
| Damage | 대상이 몬스터에게 확정 피해를 주었습니다. | 대상 제거, Evade, 전체 Threat 초기화 시 |
| External | 도발, 보스 패턴, 스크립트가 추가했습니다. | 외부 시스템이 제거하거나 Evade 시 |
| Encounter | Encounter 볼륨 또는 동료 지원으로 추가되었습니다. | Encounter 이탈 정책 또는 Evade 시 |

현재 피해 Threat는 자동으로 시간 감쇠하지 않습니다. 전투가 지속되는 동안 누적되며, Evade·대상 제거·명시적 초기화로 제거됩니다.

### 5.2 피해 Threat 계산

```text
피해 Threat = max(MinimumDamageThreat, 확정 피해량 × DamageThreatMultiplier)
```

- 방어력, 면역 등 최종 피해 판정 이후의 확정 피해량을 사용합니다.
- 작은 피해도 `MinimumDamageThreat`만큼은 Threat를 생성합니다.

### 5.3 현재 타겟 우선순위

1. 강제 지정된 타겟
2. 총 Threat가 가장 높은 타겟
3. Threat가 같으면 몬스터와 더 가까운 타겟

### 5.4 타겟 전환 안정화

`TargetSwitchThreatRatio`는 타겟이 지나치게 자주 바뀌는 것을 방지합니다.

```text
새 타겟 Threat >= 현재 타겟 Threat × TargetSwitchThreatRatio
```

예시:

- 현재 타겟 Threat: `100`
- `TargetSwitchThreatRatio`: `1.1`
- 새 타겟은 Threat가 `110` 이상이어야 전환됩니다.

도발처럼 즉시 전환해야 하는 기능은 강제 타겟 API를 사용해야 합니다.

---

## 6. 범위 책임 규칙

범위는 아래 책임에 맞게 분리하여 설정합니다.

| 범위 | 사용 목적 | 실제 피해 판정 여부 |
|---|---|---|
| DetectionRangeX/Y | 최초 선공 감지 | 아니오 |
| DetectionExitRangeX/Y | 감지 해제 히스테리시스 | 아니오 |
| BasicAttackRangeX/Y | 기본 공격 시작 가능 여부 | 아니오 |
| PreferredRangeMin/Max | 접근·후퇴 목표 거리 | 아니오 |
| Skill CastRange | 각 스킬 사용 가능 거리 | 아니오 |
| 공격 Collider2D | 공격 이벤트 시 실제 피격 대상 수집 | 예 |
| ChaseRange | 타겟 추적 포기 거리 | 아니오 |
| SoftLeashRange | 유예 가능한 홈 이탈 범위 | 아니오 |
| HardLeashRange | 즉시 귀환하는 절대 범위 | 아니오 |

### 6.1 감지 히스테리시스

감지 진입과 이탈 범위를 동일하게 설정하면 경계에서 반복 진입·이탈할 수 있습니다.

권장:

```text
DetectionExitRangeX >= DetectionRangeX
DetectionExitRangeY >= DetectionRangeY
```

런타임에서는 이탈 범위가 감지 범위보다 작으면 자동으로 감지 범위 이상으로 보정합니다.

### 6.2 기본 공격과 실제 타격 영역

- `BasicAttackRangeX/Y`: 공격을 시작할 수 있는 논리 거리입니다.
- 공격 Collider2D: 애니메이션의 실제 타격 프레임에 피해를 주는 영역입니다.

공격 시작 범위를 실제 타격 영역보다 지나치게 크게 설정하면 공격 애니메이션은 재생되지만 빗나갈 수 있습니다.

### 6.3 선호 전투 거리

- 근접 몬스터: `PreferredRangeMax`를 기본 공격 거리 안쪽으로 설정합니다.
- 원거리 몬스터: 최소 거리를 두어 플레이어가 가까우면 후퇴할 수 있게 설계합니다.
- 기본 근접 BT 프리셋은 공격 범위와 선호 거리의 교집합을 사용하여 공격 범위 밖에서 멈추는 것을 방지합니다.

### 6.4 스킬 사거리

스킬 사용 가능 여부는 `skill_monster.CastRange`와 스킬의 `TargetingMode`를 사용합니다.

- 락온 계열: 주로 수평 거리 기준
- GroundTarget: 2D 거리 기준
- Self: 대상 거리 검사 불필요

AI의 `MoveToSkillRange`는 지정된 스킬의 CastRange까지 이동합니다.

---

## 7. Home 및 Leash 규칙

### 7.1 Home 저장

몬스터가 생성되거나 풀에서 대여될 때 다음 값을 홈 정보로 저장합니다.

- 생성 위치
- 초기 좌우 방향
- 맵 UID

### 7.2 Leash 판정 거리

다음 두 거리 중 더 큰 값을 사용하여 Leash를 판정합니다.

```text
몬스터 위치 ↔ Home
현재 타겟 위치 ↔ Home
```

따라서 몬스터가 홈 근처에 남아 있더라도 타겟이 전투 구역 밖으로 크게 이탈하면 귀환할 수 있습니다.

### 7.3 Soft Leash

1. Soft 범위를 초과합니다.
2. `SoftLeashGraceSeconds` 동안 대기합니다.
3. 유예 시간 안에 범위 안으로 돌아오면 전투를 유지합니다.
4. 계속 초과하면 Evade를 시작합니다.

### 7.4 Hard Leash

Hard 범위를 초과하면 유예 없이 즉시 Evade를 시작합니다.

`SoftLeashRange`와 `HardLeashRange`가 모두 설정되었다면 Hard 값은 Soft 값보다 작을 수 없습니다. 잘못 입력하면 런타임에서 Hard 값을 Soft 이상으로 보정합니다.

### 7.5 Evade 중 처리

Evade 시작 시 다음을 수행합니다.

- 모든 Threat 제거
- 플레이어 전투 참여 목록에서 해당 몬스터 제거
- 공격 및 BT 이동 중단
- 실행 중인 몬스터 스킬 강제 취소
- 공격 슬롯 반환
- 강제 이동 및 Crowd Control 이동 정리
- 정책에 따라 Affect 제거
- 감지 센서와 일반 AI 중단
- 신규 Threat 등록 차단
- 정책에 따라 귀환 중 피해 면역

### 7.6 홈 복귀

- `ReturnMoveSpeedMultiplier`를 적용하여 홈으로 이동합니다.
- `ReturnStopDistance` 안에 들어오면 홈 도착으로 판정합니다.
- `ReturnTimeoutSeconds`를 초과하면 홈 좌표로 안전하게 보정합니다.
- 홈 도착 시 초기 방향을 복원합니다.
- `ReturnDelaySeconds` 동안 감지와 AI 재활성화를 지연할 수 있습니다.

### 7.7 자원 회복 정책

| 정책 | 동작 |
|---|---|
| None | Leash 시스템이 자원을 회복하지 않습니다. |
| OnEvadeStart | Evade 시작 즉시 자원을 회복합니다. |
| OnHomeReached | 홈 도착 시 자원을 회복합니다. |

일반 몬스터는 `OnHomeReached`, 보스는 전투 설계에 따라 `None` 또는 별도 연출과 함께 사용하는 것을 권장합니다.

---

## 8. Encounter 규칙

### 8.1 그룹 구성

Encounter 그룹은 맵 배치 데이터의 `EncounterId`로 구성합니다.

- 같은 ID를 가진 몬스터는 같은 그룹입니다.
- `EncounterId=0`은 그룹 미사용입니다.
- `monster_combat_profile`은 그룹 ID가 아니라 그룹의 Threat·지원 범위를 정의합니다.

### 8.2 그룹 볼륨 활성화

플레이어가 Encounter 볼륨에 들어오면 같은 그룹의 몬스터에게 Encounter Threat를 등록합니다.

### 8.3 동료 지원 어그로

같은 Encounter 그룹의 몬스터가 새로운 전투 대상을 얻으면 다음 규칙으로 동료를 활성화합니다.

1. `EncounterAssistRadius` 안의 그룹 멤버를 찾습니다.
2. 가까운 멤버부터 정렬합니다.
3. `MaxEncounterAssistCount`만큼만 활성화합니다.
4. 대상에게 `EncounterThreat`를 등록합니다.

`EncounterAssistRadius=0`이면 거리 제한이 없고, `MaxEncounterAssistCount=0`이면 인원 제한이 없습니다.

---

## 9. 다수 공격 슬롯 규칙

### 9.1 목적

공격 슬롯은 한 대상에게 너무 많은 몬스터가 동시에 공격하는 것을 제한합니다.

슬롯 종류:

- `None`: 제한 없음
- `Melee`: 근접 공격자 풀 사용
- `Ranged`: 원거리 공격자 풀 사용

근접과 원거리 슬롯은 서로 독립적으로 계산됩니다.

### 9.2 예약 시점

- 기본 공격: 공격 쿨다운과 사거리 조건을 만족한 뒤 예약합니다.
- 몬스터 스킬: 스킬 사용 가능 및 CastRange 조건을 만족한 뒤 예약합니다.
- 예약 실패 시 공격 또는 스킬을 시작하지 않습니다.

### 9.3 슬롯 수용량

동일 대상에 서로 다른 수용량을 가진 몬스터가 예약할 경우 현재 예약자 중 가장 엄격한 수용량이 적용됩니다.

예:

```text
몬스터 A: Melee 최대 3
몬스터 B: Melee 최대 2
현재 유효 수용량: 2
```

같은 역할의 몬스터는 가능한 한 동일한 수용량을 사용하여 예측 가능성을 높이는 것을 권장합니다.

### 9.4 예약 생명주기

- 공격·스킬 실행 중에는 예약 임대를 갱신합니다.
- 행동 종료 후 `AttackSlotPostActionHoldSeconds`만큼 추가 유지합니다.
- 갱신이 끊기면 `AttackSlotReservationSeconds` 이후 자동 반환합니다.
- 타겟 변경, 사망, Evade, 풀 반환에서는 즉시 반환합니다.

### 9.5 현재 구현 범위

공격 슬롯은 **공격 권한 토큰**입니다.

- 동시 공격자 수를 제한합니다.
- 슬롯 인덱스를 제공합니다.
- 몬스터를 플레이어 좌우의 특정 월드 좌표에 자동 배치하지는 않습니다.
- 실제 접근·후퇴는 `MoveToPreferredRange`가 담당합니다.

---

## 10. BT 기획 규칙

신규 전투 트리는 다음 표준 노드를 우선 사용합니다.

### 조건 노드

- `HasCombatTarget`
- `IsTargetInPreferredRange`
- `IsTargetTooClose`
- `IsTargetTooFar`
- `IsSkillInCastRange`
- `IsOutsideSoftLeash`
- `IsOutsideHardLeash`
- `IsReturningHome`
- `CanReserveAttackSlot`
- `HasAttackSlotReservation`

### 액션 노드

- `SelectCombatTarget`
- `MoveToPreferredRange`
- `MoveToSkillRange`
- `ReserveAttackSlot`
- `ReleaseAttackSlot`
- `BeginEvade`
- `ReleaseCombatTarget`

레거시 `HasAggroTarget`, `MoveToTarget`, `ClearAggro`는 기존 에셋 호환용입니다. 신규 트리에서는 표준 노드를 사용합니다.

---

## 11. 권장 설정 예시

### 11.1 근접 일반 몬스터

```text
DetectionRangeX=6
DetectionRangeY=3
DetectionExitRangeX=8
DetectionExitRangeY=4
BasicAttackRangeX=1.5
BasicAttackRangeY=1.2
PreferredRangeMin=0.7
PreferredRangeMax=1.4
ChaseRange=12
SoftLeashRange=14
HardLeashRange=18
TargetSwitchThreatRatio=1.1
AttackSlotType=Melee
MaxConcurrentAttackers=2
```

### 11.2 원거리 일반 몬스터

```text
DetectionRangeX=10
DetectionRangeY=4
DetectionExitRangeX=12
DetectionExitRangeY=5
BasicAttackRangeX=1.2
BasicAttackRangeY=1.5
PreferredRangeMin=4
PreferredRangeMax=7
ChaseRange=16
SoftLeashRange=18
HardLeashRange=24
AttackSlotType=Ranged
MaxConcurrentAttackers=3
```

### 11.3 보스 Encounter

```text
EncounterId=100
ReleaseEncounterThreatOnExit=false
EncounterThreat=10
EncounterAssistRadius=0
MaxEncounterAssistCount=0
SoftLeashRange=0
HardLeashRange=25
LeashRecoveryPolicy=None
InvulnerableDuringReturn=Y
AttackSlotType=None
```

보스전에서 Leash 시 HP를 회복해야 한다면 `OnHomeReached`를 선택합니다.

---

## 12. 밸런싱 권장 순서

1. 실제 타격 Collider와 애니메이션을 먼저 확정합니다.
2. `BasicAttackRangeX/Y`를 실제 타격이 안정적으로 적중하는 값으로 설정합니다.
3. `PreferredRangeMin/Max`로 전투 위치를 조정합니다.
4. 감지와 이탈 범위를 설정합니다.
5. `ChaseRange`, Soft/Hard Leash 순서로 전투 구역을 제한합니다.
6. Threat 배율과 전환 비율을 조정합니다.
7. Encounter 지원 인원과 범위를 조정합니다.
8. 마지막으로 동시 공격 슬롯 수를 조정합니다.

---

## 13. QA 체크리스트

### 전투 참여

- [ ] 세 몬스터와 교전 중 한 마리가 죽어도 전투 상태가 유지되는가?
- [ ] 마지막 몬스터가 귀환·사망했을 때 전투 상태가 종료되는가?
- [ ] 맵 이동 및 플레이어 사망 시 참여 목록이 초기화되는가?

### 감지 및 Threat

- [ ] AggroFirst 몬스터만 논리 감지로 선공하는가?
- [ ] 감지 범위 경계에서 어그로가 빠르게 반복되지 않는가?
- [ ] 현재 타겟 사망 시 다음 Threat 대상으로 전환하는가?
- [ ] `TargetSwitchThreatRatio`에 따라 타겟 전환이 안정적인가?

### 거리 및 Leash

- [ ] 기본 공격 시작 범위와 실제 타격 Collider가 의도대로 맞는가?
- [ ] 원거리 몬스터가 선호 거리에서 접근·후퇴하는가?
- [ ] Soft 범위 복귀 시 Evade가 취소되는가?
- [ ] Hard 범위 초과 시 즉시 귀환하는가?
- [ ] 귀환 중 재감지 또는 재공격하지 않는가?
- [ ] 귀환 제한 시간 초과 시 홈으로 복구되는가?

### Encounter

- [ ] 같은 Encounter ID 몬스터가 함께 활성화되는가?
- [ ] 지원 반경과 최대 지원 인원이 적용되는가?
- [ ] 볼륨 이탈 정책이 Encounter Threat만 제거하는가?

### 공격 슬롯

- [ ] 근접·원거리 동시 공격 수가 각각 제한되는가?
- [ ] 공격 취소, 사망, 귀환 시 슬롯이 반환되는가?
- [ ] 슬롯이 가득 찬 몬스터가 공격을 시작하지 않는가?
- [ ] 임대 시간 이후 고착된 슬롯이 자동 반환되는가?

---

## 14. 현재 제한 사항

- 감지 시스템은 현재 장애물 시야 판정을 하지 않습니다.
- Threat는 시간에 따라 자동 감쇠하지 않습니다.
- 공격 슬롯은 물리적 포메이션 좌표를 배정하지 않습니다.
- `EncounterId`는 몬스터 전투 프로필이 아니라 맵의 Patrol 배치 데이터에서 설정합니다.
- Soft/Hard Leash는 원형 2D 거리 기준이며 플랫폼 경로 탐색 자체를 해결하지 않습니다.
