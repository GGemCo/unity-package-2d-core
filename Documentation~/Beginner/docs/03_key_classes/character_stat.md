# CharacterStat

## 1. 문서 목적

이 문서는 Core Runtime의 스탯/리소스 중심 클래스인 `CharacterStat`를 설명합니다.
전투 수치, 리소스, 보너스 HP, Provider 기반 스탯 계산 구조를 어떻게 이해하면 좋은지 정리하는 것이 목적입니다.

---

## 2. 역할

`CharacterStat`는 **캐릭터 수치 시스템의 Facade**입니다.
단순히 숫자를 보관하는 컴포넌트가 아니라, 여러 Modifier Provider를 모아 최종 스탯을 계산하고, 결과를 Reactive 스트림으로 외부에 발행합니다.

코드의 설명대로 핵심 구조는 다음과 같습니다.

- 베이스 수치 보관
- Provider(장비/영구/패시브/아이템/런타임 임시 HP) 수집
- `StatCalculator`를 통한 최종값 산출
- 현재 리소스와 최대 리소스의 동기화
- UI와 다른 시스템에 `BehaviorSubject`로 변화 통지

즉, `CharacterStat`는 “수치를 직접 들고 있는 객체”이면서 동시에 “수치 변경 파이프라인의 조정자”입니다.

---

## 3. 왜 중요한가

Core 전투 시스템에서 값의 진실한 기준은 대부분 이 클래스에 있습니다.

예를 들어 아래 질문은 거의 모두 `CharacterStat`까지 내려가서 확인해야 합니다.

- 현재 HP/MP/Stamina 최대치는 왜 이렇게 계산되었는가
- 패시브와 장비 보정이 어디서 합산되는가
- 보너스 HP와 Temp HP가 어떤 순서로 반영되는가
- UI가 구독해야 하는 값은 무엇인가
- 저장/복원 뒤 현재값과 최대값의 동기화가 왜 달라졌는가

이 클래스 문서를 이해하면 HP 계열, 스탯 포인트, 패시브, 아이템, UI 동기화 대부분의 출발점을 잡을 수 있습니다.

---

## 4. 구조상 중요한 포인트

### 4-1. Provider 기반 구조

코드상 Provider는 다음처럼 분리되어 있습니다.

- `EquipmentOptionModifierProvider`
- `PersistentModifierProvider`
- `PassiveSkillModifierProvider`
- `ItemBonusModifierProvider`
- `RuntimeTempHpModifierProvider`

이 구조의 장점은 “왜 값이 바뀌었는지”를 출처별로 추적하기 쉽다는 점입니다.
새로운 스탯 기여 요인이 생겨도, 기존 계산 메서드에 분기문을 계속 추가하지 않고 Provider를 늘리는 방식으로 확장할 수 있습니다.

### 4-2. 계산과 보관의 분리

- 계산 규칙: `StatCalculator`
- 기여 요인: Provider들
- 최종 공개값: `CharacterStat`

즉, 계산 엔진과 공개 인터페이스를 분리한 구조입니다.

### 4-3. Reactive 발행 구조

최종값은 `BehaviorSubject`로 제공됩니다.
대표적으로 다음 값들이 공개됩니다.

- `TotalAtk`
- `TotalDef`
- `TotalHp`
- `TotalMp`
- `TotalStamina`
- `TotalSuperArmor`
- `TotalMoveSpeed`
- `TotalAttackSpeed`
- `TotalCriticalDamage`
- `TotalCriticalProbability`
- `TotalRegistFire/Cold/Lightning/Poison`

따라서 UI나 하위 시스템은 직접 수치를 폴링하기보다 이 스트림을 구독하는 방식이 더 적합합니다.

---

## 5. 주요 상태와 타입

### 베이스 값
- `BaseHp`
- BaseAtk/Def/Mp/Stamina/SuperArmor 등

최종값 계산의 출발점입니다.

### 최종 캐시 값
- `_totalAtk`, `_totalDef`, `_totalHp`, `_totalHpTemp` 등

마지막 계산 결과를 내부 캐시로 보관합니다.
이 캐시가 실제 발행값과 언제 동기화되는지 이해하는 것이 중요합니다.

### 스냅샷 구조체
- `CharacterTotals`

UI 미리보기, 시뮬레이션, 임시 계산 결과 전달에 적합한 읽기 전용 스냅샷입니다.

### 자동 리소스 보정 제어
- `_suppressAutoResourceSyncCount`
- `SuppressAutoResourceSync()`

최대치가 바뀔 때 현재값을 자동으로 따라가게 하는 보정이 항상 바람직한 것은 아닙니다.
직접 현재값을 맞춰야 하는 특수 구간에서 이 스코프를 잠깐 비활성화할 수 있습니다.

### 배치 업데이트
- `_batchUpdateCount`
- `_batchPublishPending`

로드/리빌드/장착 변경처럼 값이 연쇄적으로 많이 바뀌는 구간에서 이벤트 폭발을 줄이기 위한 장치입니다.

---

## 6. 주요 진입 메서드와 흐름

### `Awake()`
Provider를 생성하고 변경 이벤트를 연결합니다.
`CharacterStat`가 단순 수치 저장소가 아니라 초기화가 필요한 파이프라인 객체라는 점이 여기서 드러납니다.

### Provider 변경 이벤트 처리
Provider 중 하나라도 바뀌면 재계산 흐름으로 이어집니다.
즉, 외부에서 직접 총합을 만지기보다 Provider를 갱신하는 방향이 구조상 맞습니다.

### `SuppressAutoResourceSync()`
최대 리소스가 바뀌어도 현재값 자동 보정을 잠시 막아야 할 때 사용합니다.
예를 들어 스탯 포인트 재분배나 저장값 복원 시점에 유용합니다.

---

## 7. 연결해서 봐야 하는 클래스

### 계산과 Modifier
- `StatCalculator`
- `IStatModifierProvider`
- `ICharacterStatModule`

### Provider 구현
- `EquipmentOptionModifierProvider`
- `PersistentModifierProvider`
- `PassiveSkillModifierProvider`
- `ItemBonusModifierProvider`
- `RuntimeTempHpModifierProvider`

### 캐릭터와 UI
- `CharacterBase`
- `PlayerUIController`
- HP/MP/Stamina 관련 HUD 클래스

### 저장/복원
- `SaveRegistry`
- `SaveDataManagerBase`
- 플레이어 저장 데이터 계층

---

## 8. 대표 런타임 흐름

### 흐름 A: 초기 계산
1. `Awake()`에서 Provider가 만들어집니다.
2. 베이스 수치와 Provider 기여치가 준비됩니다.
3. 최종 스탯이 계산됩니다.
4. `BehaviorSubject`를 통해 외부에 공개됩니다.

### 흐름 B: 장비/패시브/아이템 변경
1. 해당 Provider가 갱신됩니다.
2. `CharacterStat`가 변경 이벤트를 받습니다.
3. 최종 합산을 다시 수행합니다.
4. 필요하면 현재 리소스 보정과 UI 갱신이 이어집니다.

### 흐름 C: 저장/복원 후 재동기화
1. 저장값 또는 외부 정책이 적용됩니다.
2. 최대 리소스와 현재 리소스가 다시 정렬됩니다.
3. 필요 시 자동 리소스 보정을 억제하면서 직접 현재값을 맞춥니다.

---

## 9. 확장 포인트

### 새 수치 기여 요인을 추가할 때
`CharacterStat` 본문에 분기를 늘리기보다 Provider를 하나 더 만드는 방식이 권장됩니다.

### 새 리소스 동기화 정책이 필요할 때
현재값 자동 보정 로직을 무턱대고 바꾸기보다, 스코프 억제와 정책 메서드 분리 방식으로 확장하는 편이 안전합니다.

### UI 미리보기 기능을 추가할 때
실제 상태를 건드리지 않고 `CharacterTotals` 기반 스냅샷 계산으로 풀 수 있는지 먼저 검토하는 것이 좋습니다.

---

## 10. 디버깅 체크리스트

### 최종 스탯이 예상과 다른 경우
- 어떤 Provider가 값을 더하고 있는지 먼저 확인합니다.
- 베이스 값이 잘못됐는지, Provider 기여가 잘못됐는지 분리합니다.

### 최대 HP는 바뀌는데 현재 HP가 이상한 경우
- 자동 리소스 동기화가 작동했는지 확인합니다.
- `SuppressAutoResourceSync()`가 걸린 구간이 있는지 확인합니다.

### UI는 안 바뀌는데 값은 바뀐 경우
- `BehaviorSubject` 발행 시점이 누락되지 않았는지 확인합니다.
- UI가 올바른 스트림을 구독하는지 확인합니다.

### 저장/복원 후 수치가 꼬이는 경우
- 복원 순서상 Provider 재구성이 먼저인지, 현재값 적용이 먼저인지 확인합니다.
- 영구/아이템/패시브 보정이 중복 적용되지 않는지 확인합니다.

---

## 11. 한 줄 정리

`CharacterStat`는 **Core 캐릭터 수치 시스템의 중심이며, 여러 Modifier 출처를 모아 최종 전투 리소스와 스탯을 계산하고 발행하는 Facade**입니다.
