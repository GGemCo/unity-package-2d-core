# `monster_combat_profile` 테이블 설명서

> 대상: 기획팀, 데이터 작업자, 개발팀  
> 기준 클래스: `TableMonsterCombatProfile`, `MonsterCombatRangeProfile`, `MonsterThreatProfile`, `MonsterLeashProfile`, `MonsterEncounterProfile`, `MonsterAttackSlotProfile`

---

## 1. 테이블 역할

`monster_combat_profile`은 몬스터의 다음 전투 정책을 한 행으로 정의합니다.

- 선공 감지 및 감지 이탈 범위
- 기본 공격 시작 거리와 선호 전투 거리
- 추적 및 Home Leash 범위
- Threat 누적 및 타겟 전환 정책
- Encounter 지원 어그로 정책
- 근접·원거리 동시 공격 슬롯 정책

`monster` 테이블의 `CombatProfileUid`가 이 테이블의 `Uid`를 참조합니다.

```text
monster.CombatProfileUid
    → monster_combat_profile.Uid
```

`CombatProfileUid=0`, 빈 값, 또는 유효하지 않은 UID이면 기존 공격 Collider를 이용한 호환 기본값을 사용하며 신규 Leash·공격 슬롯은 비활성 상태를 유지합니다.

---

## 2. 권장 헤더

```text
Uid	Memo	DetectionRangeX	DetectionRangeY	DetectionExitRangeX	DetectionExitRangeY	BasicAttackRangeX	BasicAttackRangeY	PreferredRangeMin	PreferredRangeMax	ChaseRange	SoftLeashRange	HardLeashRange	SoftLeashGraceSeconds	ReturnStopDistance	ReturnDelaySeconds	ReturnMoveSpeedMultiplier	ReturnTimeoutSeconds	LeashRecoveryPolicy	InvulnerableDuringReturn	ClearAffectsOnEvade	DetectionThreat	PatrolThreat	DamageThreatMultiplier	MinimumDamageThreat	TargetSwitchThreatRatio	MaxThreatTargets	EncounterThreat	EncounterAssistRadius	MaxEncounterAssistCount	AttackSlotType	MaxConcurrentAttackers	AttackSlotReservationSeconds	AttackSlotPostActionHoldSeconds
```

호환 목적으로 `BasicAttackRange` 컬럼이 존재하면 `BasicAttackRangeX`가 비어 있을 때 X축 값으로 읽습니다. 신규 데이터는 `BasicAttackRangeX`를 사용합니다.

---

## 3. 값 입력 형식

### 숫자

- 실수: `1`, `1.5`, `0.25`
- 정수: `0`, `2`, `16`
- 대부분의 신규 컬럼은 빈 값을 허용하며 런타임 기본값으로 정규화됩니다.

### Boolean

다음 값을 지원합니다.

```text
Y / N
true / false
1 / 0
```

### Enum

대소문자와 실제 프로젝트의 Enum 파서 규칙을 따릅니다. 문서에 기재된 정확한 이름을 권장합니다.

---

## 4. 식별 컬럼

| 컬럼 | 타입 | 필수 | 설명 |
|---|---:|---:|---|
| `Uid` | int | 예 | 프로필 고유 ID입니다. `monster.CombatProfileUid`가 참조합니다. |
| `Memo` | string | 아니오 | 데이터 작업용 설명입니다. 런타임 표시 이름도 Memo를 기준으로 생성됩니다. |

---

## 5. 감지 및 전투 거리 컬럼

| 컬럼 | 타입 | 0/빈 값 처리 | 설명 및 제약 |
|---|---:|---|---|
| `DetectionRangeX` | float | 기존 실제 공격 Collider의 X 반경 | 몬스터 중심 선공 감지 X축 반경입니다. |
| `DetectionRangeY` | float | 기존 실제 공격 Collider의 Y 반경 | 몬스터 중심 선공 감지 Y축 반경입니다. |
| `DetectionExitRangeX` | float | `DetectionRangeX` | 감지 해제 X축 반경입니다. 감지 범위보다 작으면 감지 범위 이상으로 보정됩니다. |
| `DetectionExitRangeY` | float | `DetectionRangeY` | 감지 해제 Y축 반경입니다. 감지 범위보다 작으면 감지 범위 이상으로 보정됩니다. |
| `BasicAttackRangeX` | float | 기존 실제 공격 Collider의 X 반경 | 기본 공격을 시작할 수 있는 X축 거리입니다. |
| `BasicAttackRangeY` | float | 기존 실제 공격 Collider의 Y 반경 | 기본 공격을 시작할 수 있는 Y축 거리입니다. |
| `PreferredRangeMin` | float | `0` | 선호 최소 수평 거리입니다. `0~PreferredRangeMax`로 보정됩니다. |
| `PreferredRangeMax` | float | `BasicAttackRangeX` | 선호 최대 수평 거리입니다. |
| `ChaseRange` | float | 비활성 | 몬스터와 타겟의 2D 거리가 이 값을 초과하면 추적 포기/Evade 후보가 됩니다. |
| `SoftLeashRange` | float | 비활성 | Home 기준 Soft Leash 원형 반경입니다. |
| `HardLeashRange` | float | 비활성 | Home 기준 Hard Leash 원형 반경입니다. Soft와 함께 설정하면 Soft 이상으로 보정됩니다. |

### 기존 Collider 호환값 계산

프로필 값이 비어 있을 때 실제 공격용 `CapsuleCollider2D`의 월드 크기와 오프셋으로 X/Y 반경을 계산합니다.

Collider가 없으면 X/Y 기본 호환 반경은 `1`입니다.

### 권장 관계

```text
DetectionExitRangeX >= DetectionRangeX
DetectionExitRangeY >= DetectionRangeY
PreferredRangeMin <= PreferredRangeMax
SoftLeashRange <= HardLeashRange
BasicAttackRange는 실제 타격 Collider와 공격 모션을 고려하여 설정
```

---

## 6. Leash 및 귀환 컬럼

| 컬럼 | 타입 | 실효 기본값 | 설명 및 제약 |
|---|---:|---:|---|
| `SoftLeashGraceSeconds` | float | `1.5` | Soft 범위를 초과한 뒤 Evade까지 기다리는 시간입니다. 0 이하이면 기본값을 사용합니다. |
| `ReturnStopDistance` | float | `0.1` | Home 도착으로 인정하는 거리입니다. 최종적으로 최소 `0.01` 이상입니다. |
| `ReturnDelaySeconds` | float | `0` | Home 도착 후 감지와 BT를 다시 활성화하기 전 대기 시간입니다. 음수는 0으로 보정됩니다. |
| `ReturnMoveSpeedMultiplier` | float | `1` | 귀환 이동 속도 배율입니다. 0 이하이면 기본값을 사용합니다. |
| `ReturnTimeoutSeconds` | float | `8` | 귀환 제한 시간입니다. 초과 시 Home 좌표로 보정합니다. 최종 최소값은 `0.1`입니다. |
| `LeashRecoveryPolicy` | enum | `OnHomeReached` | 자원 회복 시점입니다. |
| `InvulnerableDuringReturn` | bool | `true` | 귀환 및 재활성 대기 중 피해를 무시할지 설정합니다. |
| `ClearAffectsOnEvade` | bool | `true` | Evade 시작 시 적용 중인 Affect를 제거할지 설정합니다. |

### `LeashRecoveryPolicy`

| 값 | 설명 |
|---|---|
| `None` | Leash 시스템이 자원을 회복하지 않습니다. |
| `OnEvadeStart` | Evade 시작 즉시 자원을 회복합니다. |
| `OnHomeReached` | Home 도착 시 자원을 회복합니다. |

### Leash 활성 조건

```text
SoftLeashRange > 0 또는 HardLeashRange > 0
```

두 값이 모두 `0`이면 Home/Leash 시스템은 비활성입니다.

---

## 7. Threat 컬럼

| 컬럼 | 타입 | 실효 기본값 | 설명 및 제약 |
|---|---:|---:|---|
| `DetectionThreat` | float | `1` | 선공 감지 범위에 있는 동안 유지할 Threat입니다. 0 이하이면 기본값을 사용합니다. |
| `PatrolThreat` | float | `1` | 기존 Patrol 영역 진입으로 유지할 Threat입니다. 0 이하이면 기본값을 사용합니다. |
| `DamageThreatMultiplier` | float | `1` | 확정 피해량에 곱하는 Threat 배율입니다. 0 이하이면 기본값을 사용합니다. |
| `MinimumDamageThreat` | float | `1` | 피해 Threat의 최소 보장값입니다. 0 이하이면 기본값을 사용합니다. |
| `TargetSwitchThreatRatio` | float | `1.1` | 새 타겟으로 전환할 때 필요한 Threat 비율입니다. 양수이지만 1보다 작으면 1로 보정됩니다. |
| `MaxThreatTargets` | int | `16` | 몬스터가 기억할 최대 Threat 대상 수입니다. 유효 범위는 `1~64`입니다. |

### 피해 Threat 공식

```text
max(MinimumDamageThreat, confirmedDamage × DamageThreatMultiplier)
```

### 타겟 전환 예시

```text
현재 타겟 Threat = 200
TargetSwitchThreatRatio = 1.2
새 후보가 240 이상일 때 전환
```

### `MaxThreatTargets` 초과 시

최대 수를 초과하면 현재 타겟과 강제 타겟을 보호하면서 낮은 Threat 항목을 제거합니다.

---

## 8. Encounter 컬럼

| 컬럼 | 타입 | 실효 기본값 | 설명 및 제약 |
|---|---:|---:|---|
| `EncounterThreat` | float | `1` | Encounter 볼륨 또는 동료 지원으로 등록할 Threat입니다. 0 이하이면 기본값을 사용합니다. |
| `EncounterAssistRadius` | float | `0` | 지원 어그로 반경입니다. 0이면 거리 제한이 없습니다. |
| `MaxEncounterAssistCount` | int | `0` | 한 번에 활성화할 최대 동료 수입니다. 0이면 제한이 없고, 양수는 `1~32`로 보정됩니다. |

주의:

- Encounter 그룹 ID 자체는 이 테이블에 없습니다.
- 그룹 ID는 맵의 `PatrolData.EncounterId`에서 설정합니다.
- 이 테이블은 그룹이 활성화될 때 사용할 Threat와 지원 범위만 정의합니다.

---

## 9. 공격 슬롯 컬럼

| 컬럼 | 타입 | 실효 기본값 | 설명 및 제약 |
|---|---:|---:|---|
| `AttackSlotType` | enum | `None` | 공격 슬롯 종류입니다. |
| `MaxConcurrentAttackers` | int | Melee `2`, Ranged `3`, None `0` | 동일 대상에 같은 종류로 동시 예약할 최대 공격자 수입니다. 양수는 `1~16`으로 보정됩니다. |
| `AttackSlotReservationSeconds` | float | `4` | 갱신이 끊긴 예약을 자동 반환할 시간입니다. 최종 최소값은 `0.2`입니다. |
| `AttackSlotPostActionHoldSeconds` | float | `0.2` | 공격·스킬 종료 후 슬롯을 추가 유지할 시간입니다. `0`은 즉시 반환, 빈 값 또는 음수는 기본값입니다. |

### `AttackSlotType`

| 값 | 설명 |
|---|---|
| `None` | 동시 공격 제한을 사용하지 않습니다. |
| `Melee` | 근접 공격자 슬롯을 사용합니다. |
| `Ranged` | 원거리 공격자 슬롯을 사용합니다. |

근접과 원거리 슬롯은 서로 독립적입니다.

---

## 10. 전체 데이터 예시

### 10.1 근접 일반 몬스터

```text
Uid=1001
Memo=근접 일반
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
SoftLeashGraceSeconds=1.5
ReturnStopDistance=0.1
ReturnDelaySeconds=0.5
ReturnMoveSpeedMultiplier=1.3
ReturnTimeoutSeconds=8
LeashRecoveryPolicy=OnHomeReached
InvulnerableDuringReturn=Y
ClearAffectsOnEvade=Y
DetectionThreat=1
PatrolThreat=1
DamageThreatMultiplier=1
MinimumDamageThreat=1
TargetSwitchThreatRatio=1.1
MaxThreatTargets=16
EncounterThreat=1
EncounterAssistRadius=10
MaxEncounterAssistCount=4
AttackSlotType=Melee
MaxConcurrentAttackers=2
AttackSlotReservationSeconds=4
AttackSlotPostActionHoldSeconds=0.2
```

### 10.2 원거리 일반 몬스터

```text
Uid=1002
Memo=원거리 일반
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
SoftLeashGraceSeconds=1.5
ReturnStopDistance=0.1
ReturnDelaySeconds=0.5
ReturnMoveSpeedMultiplier=1.2
ReturnTimeoutSeconds=8
LeashRecoveryPolicy=OnHomeReached
InvulnerableDuringReturn=Y
ClearAffectsOnEvade=Y
DetectionThreat=1
PatrolThreat=1
DamageThreatMultiplier=1
MinimumDamageThreat=1
TargetSwitchThreatRatio=1.1
MaxThreatTargets=16
EncounterThreat=1
EncounterAssistRadius=14
MaxEncounterAssistCount=6
AttackSlotType=Ranged
MaxConcurrentAttackers=3
AttackSlotReservationSeconds=6
AttackSlotPostActionHoldSeconds=0.3
```

### 10.3 그룹 보스

```text
Uid=1100
Memo=Encounter 보스
DetectionRangeX=0
DetectionRangeY=0
DetectionExitRangeX=0
DetectionExitRangeY=0
BasicAttackRangeX=2.5
BasicAttackRangeY=2
PreferredRangeMin=1.2
PreferredRangeMax=2.3
ChaseRange=0
SoftLeashRange=0
HardLeashRange=25
SoftLeashGraceSeconds=0
ReturnStopDistance=0.1
ReturnDelaySeconds=2
ReturnMoveSpeedMultiplier=1.5
ReturnTimeoutSeconds=10
LeashRecoveryPolicy=None
InvulnerableDuringReturn=Y
ClearAffectsOnEvade=Y
DetectionThreat=5
PatrolThreat=10
DamageThreatMultiplier=1
MinimumDamageThreat=1
TargetSwitchThreatRatio=1.2
MaxThreatTargets=32
EncounterThreat=10
EncounterAssistRadius=0
MaxEncounterAssistCount=0
AttackSlotType=None
MaxConcurrentAttackers=0
AttackSlotReservationSeconds=0
AttackSlotPostActionHoldSeconds=
```

---

## 11. 다른 데이터와의 연결

### `monster` 테이블

필수 연결 컬럼:

```text
CombatProfileUid
```

예:

```text
monster.Uid=2001
monster.CombatProfileUid=1001
```

### `PatrolData`

Encounter 관련 배치 값:

```text
EncounterId
ReleaseEncounterThreatOnExit
```

### `skill_monster` 테이블

스킬 전투 BT는 다음 값을 별도로 사용합니다.

```text
TargetingMode
CastRange
```

`monster_combat_profile`의 선호 거리와 스킬 CastRange는 서로 다른 책임입니다.

---

## 12. 데이터 검증 체크리스트

- [ ] `Uid`가 중복되지 않는가?
- [ ] `monster.CombatProfileUid`가 존재하는 프로필을 참조하는가?
- [ ] 감지 이탈 범위가 감지 범위 이상인가?
- [ ] 기본 공격 시작 범위에서 실제 타격 Collider가 적중 가능한가?
- [ ] 선호 최소가 선호 최대보다 크지 않은가?
- [ ] Hard Leash가 Soft Leash보다 작지 않은가?
- [ ] 공격 슬롯을 사용하는 몬스터끼리 수용량이 일관적인가?
- [ ] Encounter 지원 반경 0이 무제한이라는 점을 의도했는가?
- [ ] `MaxEncounterAssistCount=0`이 무제한이라는 점을 의도했는가?
- [ ] 보스의 `LeashRecoveryPolicy`가 전투 기획과 일치하는가?
- [ ] `AttackSlotPostActionHoldSeconds=0`이 즉시 반환이라는 점을 의도했는가?
