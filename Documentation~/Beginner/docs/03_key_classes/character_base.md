# CharacterBase

## 1. 문서 목적

이 문서는 Core Runtime의 공통 캐릭터 기반 클래스인 `CharacterBase`를 설명합니다.
플레이어, 몬스터, NPC가 공유하는 책임이 어디까지인지, 그리고 어떤 하위 컨트롤러와 연결되는지 이해하는 것이 목적입니다.

---

## 2. 역할

`CharacterBase`는 **캐릭터 공통 실행 축**입니다.
`CharacterStat`을 상속하면서, 실제 게임 플레이에서 필요한 캐릭터 단위 기능을 partial 구조로 묶고 있습니다.

이 클래스가 담당하는 대표 책임은 다음과 같습니다.

- 캐릭터의 공통 식별 정보와 방향/플립 상태 보관
- 애니메이션 컨트롤러, 렌더러, 충돌체, Rigidbody 연결
- 전투 상태 스트림 제공
- 피격/모션/CC/원소 게이지 같은 하위 컨트롤러 접근점 제공
- 공용 애니메이션 이벤트 중계
- 플레이어/몬스터/NPC 공통 동작의 기반 제공

즉, `CharacterBase`는 “모든 캐릭터가 최소한 공유해야 하는 런타임 표면”이라고 이해하면 됩니다.

---

## 3. 위치와 구조

**주요 위치**
- `Characters/CharacterBase.cs`
- `Characters/CharacterBase.Lifecycle.cs`
- `Characters/CharacterBase.State.cs`
- `Characters/CharacterBase.Combat.cs`
- `Characters/CharacterBase.AnimationEvents.cs`
- `Characters/CharacterBase.Presentation.cs`

이 클래스는 partial 구조를 사용하고 있기 때문에, 한 파일만 보면 책임이 다 보이지 않습니다.
처음 읽을 때는 다음 순서를 권장합니다.

1. `CharacterBase.cs`
2. `CharacterBase.Lifecycle.cs`
3. `CharacterBase.State.cs`
4. `CharacterBase.Combat.cs`
5. `CharacterBase.AnimationEvents.cs`

---

## 4. 왜 중요한가

Core에서 캐릭터 관련 기능을 추가할 때 가장 먼저 영향을 받는 클래스입니다.

특히 아래 질문에 해당하는 기능은 `CharacterBase`를 먼저 확인해야 합니다.

- 이 기능이 플레이어와 몬스터 모두에 필요한가
- 애니메이션 이벤트와 연결되는가
- 캐릭터의 전투 상태나 제어 상태를 바꾸는가
- 캐릭터가 가진 하위 컨트롤러와 연결되는가

잘못 설계하면 상위 패키지 책임이 `CharacterBase`로 들어오기 쉬우므로, 항상 “공통 기반인가, 상위 정책인가”를 구분해야 합니다.

---

## 5. 핵심 상태와 프로퍼티

### 초기화 상태
- `IsInitialized`
- `Initialized`

외부 시스템이 캐릭터를 만졌는데 아직 준비가 덜 된 상황을 구분하는 기준입니다.
로드 직후나 풀링 직후 상태 동기화에서 중요합니다.

### 캐릭터 식별 및 방향
- `type`
- `uid`, `vid`
- `defaultFacingDirection8`
- `CurrentFacing`
- `isFlip`
- `directionNormalize`

캐릭터 종류, 데이터 식별자, 방향 판정, 스프라이트 좌우 반전을 담당합니다.

### 전투 상태
- `CurrentBattleStatus`
- `IsUseSkill`

전투 상태나 스킬 사용 여부를 UI, AI, 입력 차단 같은 외부 시스템이 참고할 수 있습니다.

### 주요 런타임 참조
- `CharacterAnimationController`
- `characterRigidbody2D`
- `colliderAttackRange`
- `colliderHitArea`
- `ElementGaugeController`
- `PhysicsOverrideController`

실제 런타임 연결점은 이 참조들에 모입니다.

### 애니메이션 이벤트
- `AnimationCompleteAttack`
- `AnimationCompleteAttackEnd`
- `OnAnimationEventJump`
- `OnAnimationEventDash`
- `OnAnimationEventMotion`
- `OnAnimationEventCrowdControl`
- `OnAnimationEventGuardEnd`

캐릭터를 기준으로 애니메이션 이벤트를 다른 시스템에 전달하는 공통 창구 역할을 합니다.

---

## 6. 주요 진입 메서드

### `IsCurrentlyGrounded()`
공용 Ground Probe 규칙으로 지면 판정을 수행합니다.
Skill, CC, 점프, 낙하 관련 시스템이 같은 기준을 쓰게 만드는 중요한 메서드입니다.

### `TryProbeGroundBelow()`
캐릭터 하단의 지면을 탐색합니다.
지면 스냅, 공중 상태 종료, 낙하형 연출 보정에 자주 연결됩니다.

### `PhysicsOverrideController`
필요 시 `CharacterPhysicsOverrideController`를 찾아오거나 생성합니다.
중력 오버라이드, 물리 상태 임시 변경을 다룰 때 중요한 접근점입니다.

### `MoveTeleport()`
월드 좌표로 캐릭터를 즉시 이동시킵니다.
스폰, 복원, 컷신, 강제 위치 보정 계열에서 사용됩니다.

### `UseSkill()`
기본 클래스에서는 빈 가상 함수입니다.
즉, 스킬 자체는 `CharacterBase`의 공통 책임이 아니라 파생 클래스/상위 패키지 확장 지점이라는 뜻입니다.

---

## 7. 연결해서 봐야 하는 클래스

### 바로 아래 기반
- `CharacterStat`
- `CharacterBaseController`

### 전투/피격
- `CharacterDamageController`
- `CharacterHitArea`
- `CharacterAttackRange`

### 이동/제어
- `CharacterMotionController2D`
- `CharacterCrowdControlController`
- `CharacterPhysicsOverrideController`

### 플레이어/몬스터 구현
- `Player`
- `Monster`
- `ControllerMonster`
- `PlayerUIController`

### 애니메이션/표현
- `ICharacterAnimationController`
- `AnimationEventMediator`
- `CharacterOutlineController`

---

## 8. 대표 런타임 흐름

### 흐름 A: 캐릭터 생성
1. 프리팹이 생성됩니다.
2. `CharacterStat`가 먼저 준비됩니다.
3. `CharacterBase`가 공통 참조와 상태를 연결합니다.
4. 플레이어/몬스터 전용 컨트롤러가 후속 연결됩니다.
5. `Initialized` 이후에 외부 시스템이 안전하게 상태를 주입합니다.

### 흐름 B: 애니메이션 이벤트 발생
1. 애니메이션 이벤트가 캐릭터에 도달합니다.
2. `CharacterBase`의 이벤트 중계 지점이 호출됩니다.
3. 전투, 모션, 점프, 가드 종료 같은 관련 시스템이 구독하여 반응합니다.

### 흐름 C: 공용 캐릭터 기능 사용
1. 외부 시스템이 캐릭터를 참조합니다.
2. `CharacterBase`를 통해 지면 판정, 방향, 물리, 전투 상태를 조회합니다.
3. 세부 로직은 하위 컨트롤러로 위임됩니다.

---

## 9. 확장 포인트

### 이 클래스에 넣기 좋은 것
- 모든 캐릭터가 공유해야 하는 상태
- 캐릭터 공통 애니메이션 이벤트 중계
- 공용 물리/지면 판정 헬퍼
- 캐릭터 하위 컨트롤러 접근점

### 이 클래스에 직접 넣지 않는 것이 좋은 것
- 플레이어 입력 정책
- 몬스터 AI 판단
- 스킬 실행 세부 정책
- 버프/디버프 정의 자체

`CharacterBase`는 실행 기반이고, 무엇을 실행할지는 상위 패키지나 파생 클래스가 결정하는 구조를 유지하는 편이 좋습니다.

---

## 10. 디버깅 체크리스트

### 캐릭터가 아직 준비되지 않은 상태에서 참조되는 경우
- `IsInitialized`가 언제 true가 되는지 확인합니다.
- 풀링 후 재초기화 타이밍을 같이 봅니다.

### 방향/플립이 이상한 경우
- `CurrentFacing`, `isFlip`, `defaultFacingDirection8` 갱신 흐름을 확인합니다.
- 입력/AI가 방향을 바꾸는지, 연출 계층이 덮어쓰는지 분리해서 봅니다.

### 애니메이션 이벤트가 안 들어오는 경우
- 애니메이션 컨트롤러 연결 여부
- 이벤트 이름과 중계 지점 연결 여부
- 파생 클래스가 이벤트를 무시하고 있지 않은지 확인합니다.

### 공용 책임이 너무 커지는 경우
- 이 기능이 정말 모든 캐릭터 공통인지 다시 확인합니다.
- 파생 클래스, 별도 컨트롤러, 상위 패키지로 분리 가능한지 검토합니다.

---

## 11. 한 줄 정리

`CharacterBase`는 **Core 캐릭터 시스템의 공통 표면이며, 플레이어와 몬스터가 공유하는 런타임 책임을 묶는 중심 축**입니다.
