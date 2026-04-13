# Runtime 기능 영역 문서 - 캐릭터와 전투

## 1. 문서 목적

이 문서는 Core Runtime에서 **캐릭터 공통 구조와 전투 처리 축**을 설명합니다.
플레이어와 몬스터가 같은 기반 위에서 어떻게 동작하는지, 그리고 피격/모션/Crowd Control/UI 연결이 어디서 이루어지는지 파악하는 것이 목적입니다.

---

## 2. 이 영역에 포함되는 주요 폴더

- `Characters/`
- `Combat/`
- `Characters/Stats/`
- `Characters/Motion/`
- `Characters/CrowdControl/`
- `Characters/Monster/`
- `Characters/Player/`
- `Characters/Targeting/`
- `Characters/ElementGauge/`
- `Characters/HitStop/`

이 영역은 Core Runtime에서 가장 자주 수정되는 축 중 하나이며, 상위 패키지의 기능도 대부분 결국 여기와 연결됩니다.

---

## 3. 대표 클래스

### 공통 캐릭터 기반
- `Characters/CharacterBase.cs`
- `Characters/CharacterBase.Lifecycle.cs`
- `Characters/CharacterBase.State.cs`
- `Characters/CharacterBase.Combat.cs`
- `Characters/CharacterBase.AnimationEvents.cs`
- `Characters/CharacterBaseController.cs`
- `Characters/CharacterStat.cs`

### 피격과 전투 처리
- `Characters/CharacterDamageController.cs`
- `Characters/CharacterHitArea.cs`
- `Characters/CharacterAttackRange.cs`
- `Combat/Hit/*`

### 모션과 제어 불가 상태
- `Characters/Motion/CharacterMotionController2D.cs`
- `Characters/Motion/MotionRequest.cs`
- `Characters/Motion/Solvers/*`
- `Characters/CrowdControl/CharacterCrowdControlController.cs`
- `Characters/CrowdControl/Handler/*`

### 플레이어/몬스터 구체 구현
- `Characters/Player/Player.cs`
- `Characters/Player/PlayerUIController.cs`
- `Characters/Monster/Monster.cs`
- `Characters/Monster/ControllerMonster.cs`
- `Characters/Monster/MonsterBrainTicker.cs`
- `Characters/Monster/MonsterBrainSelector.cs`
- `Characters/Monster/ControllerMonsterSuperArmor.cs`

### 확장성 높은 하위 시스템
- `Characters/ElementGauge/CharacterElementGaugeController.cs`
- `Characters/HitStop/CharacterHitStopController.cs`
- `Characters/AutoMove/PlayerAutoMoveController.cs`

---

## 4. 이 영역의 핵심 책임

## 4-1. 공통 캐릭터 기반 제공

`CharacterBase`는 플레이어, 몬스터, NPC가 공유하는 기반입니다.
여기에 상태, 생명주기, 전투 인터페이스, 애니메이션 이벤트 연결, 시각 표현 보조가 모입니다.

실무에서는 새로운 캐릭터 관련 기능을 추가할 때 가장 먼저 아래를 구분해야 합니다.

- 모든 캐릭터가 공유해야 하는가
- 플레이어 전용인가
- 몬스터 전용인가
- 전투 처리인가, 단순 표시인가

이 구분이 되지 않으면 기능이 `CharacterBase`에 과도하게 쌓이기 쉽습니다.

## 4-2. 스탯과 리소스의 중심 유지

`CharacterStat`은 단순 수치 저장소가 아니라, HP/MP/Stamina/보너스 자원 같은 **실제 전투 리소스의 중심 축**입니다.
상태 변화, UI 갱신, 아이템/패시브/효과 적용은 결국 이 클래스를 기준으로 모이게 됩니다.

즉, 전투 로직을 읽을 때는 “피격이 어디서 일어나는가”만 보는 것이 아니라,
**최종 수치 상태가 어디서 관리되는가**를 같이 봐야 합니다.

## 4-3. 피격과 결과 처리

`CharacterDamageController`는 피격 처리의 핵심 진입점입니다.
이 계층에서는 보통 다음 흐름이 중요합니다.

1. 공격/피격 판정이 들어온다.
2. 방어/무적/제어 불가 여부를 확인한다.
3. 실제 리소스 감소와 상태 변화를 적용한다.
4. 필요하면 HitStop, CC, UI 피드백, 사망 후속 처리를 연결한다.

즉, “데미지 수치 계산”과 “데미지를 받았을 때 어떤 후속 효과가 나는가”를 분리해서 보는 것이 중요합니다.

## 4-4. 물리성 이동과 Crowd Control 실행

`CharacterMotionController2D`와 `CharacterCrowdControlController`는 전투 감각을 결정하는 중요한 축입니다.

- Motion은 이동 자체의 실행 책임
- Crowd Control은 넉백, 넉다운, 넉업 같은 상태성 이동 연출 책임

이 둘은 서로 밀접하지만 같은 계층은 아닙니다.
새로운 모션을 추가할 때는 Motion Solver를 확장할지, CC Handler를 확장할지 먼저 구분하는 편이 좋습니다.

## 4-5. 캐릭터별 특화 책임 분리

플레이어와 몬스터는 모두 `CharacterBase` 계층을 공유하지만, 아래와 같은 전용 책임이 있습니다.

### 플레이어 쪽
- 입력과의 연결은 주로 상위 패키지(Control)에서 담당
- UI 브리지는 `PlayerUIController`가 담당
- 자동 이동, 자원 표시, 퀵슬롯 표시 같은 플레이어 경험이 중요

### 몬스터 쪽
- Brain/BT 실행과의 연결
- 슈퍼아머, 그로기, HP바 같은 전투 연출
- 타겟팅과 AI 상태 전이 연결

---

## 5. 대표 런타임 흐름

### 흐름 A: 캐릭터 생성 후 전투 준비

1. 캐릭터 프리팹이 생성됩니다.
2. `CharacterBase`와 관련 컨트롤러가 초기화됩니다.
3. `CharacterStat`이 기준 수치를 준비합니다.
4. 플레이어면 `PlayerUIController`, 몬스터면 UI/Brain 연결이 붙습니다.

### 흐름 B: 피격 발생

1. 타격 판정 또는 충돌 판정이 발생합니다.
2. `CharacterDamageController`가 유효한 피격인지 검사합니다.
3. 리소스 감소와 상태 갱신이 일어납니다.
4. 필요 시 HitStop, Crowd Control, UI 피드백, 사망 처리로 이어집니다.

### 흐름 C: Crowd Control 적용

1. CC 요청이 들어옵니다.
2. `CharacterCrowdControlController`가 타입과 런타임 데이터를 해석합니다.
3. 적절한 Handler가 선택됩니다.
4. Motion 요청과 애니메이션, 상태 제어가 함께 적용됩니다.

---

## 6. 추천 읽기 순서

1. `CharacterBase`
2. `CharacterStat`
3. `CharacterDamageController`
4. `CharacterMotionController2D`
5. `CharacterCrowdControlController`
6. `Player`, `PlayerUIController`
7. `Monster`, `ControllerMonster`, `MonsterBrainTicker`
8. `ControllerMonsterSuperArmor`
9. `CharacterElementGaugeController`
10. `CharacterHitStopController`

---

## 7. 기능 추가 시 배치 기준

## 이 영역에 넣는 것이 맞는 경우
- 캐릭터 공통 상태 변화
- HP/MP/Stamina/보너스 HP 같은 리소스 처리
- 피격 후속 처리
- CC, 넉백, 이동 연출 실행
- 플레이어/몬스터 공통 전투 규칙

## 별도 계층으로 분리하는 것이 좋은 경우
- 스킬 자체의 실행 정책
- 버프/디버프 정의와 해석
- 입력 해석
- AI 의사결정

즉, Core의 캐릭터/전투 영역은 **실행기와 공통 상태 기반**에 집중하고,
상위 패키지는 “무엇을 실행할지”를 결정하는 쪽으로 남기는 것이 좋습니다.

---

## 8. 디버깅 포인트

### 피격은 되었는데 HP가 줄지 않는 경우
- `CharacterDamageController` 진입 여부를 먼저 확인합니다.
- 무적, 방어, 상태 이상, 사망 여부 같은 차단 조건을 점검합니다.
- 실제 수치 갱신이 `CharacterStat`에 반영되는지 확인합니다.

### 넉백/넉업이 이상한 경우
- `CharacterCrowdControlController`가 어떤 Handler를 선택했는지 확인합니다.
- Motion Solver 요청값과 실제 Rigidbody/Transform 이동이 일치하는지 확인합니다.
- 애니메이션 상태 전환이 CC와 충돌하지 않는지 점검합니다.

### 플레이어/몬스터 UI가 갱신되지 않는 경우
- `PlayerUIController` 또는 몬스터 UI 브리지 계층의 구독 지점을 확인합니다.
- 리소스 값은 바뀌었는데 표시만 안 바뀌는지, 반대로 값 자체가 안 바뀌는지 분리해서 확인합니다.

### 상태가 여러 군데서 중복 처리되는 경우
- `CharacterBase`, `CharacterDamageController`, `CharacterCrowdControlController` 중 어디가 실제 소유자 책임인지 먼저 정리합니다.
- 같은 조건을 여러 컨트롤러가 동시에 판단하고 있지 않은지 확인합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **캐릭터의 공통 상태와 전투 결과를 실제로 실행하는 기반 계층**이며,
플레이어와 몬스터는 이 기반 위에 각자의 전용 책임을 얹는 구조로 이해하면 됩니다.
