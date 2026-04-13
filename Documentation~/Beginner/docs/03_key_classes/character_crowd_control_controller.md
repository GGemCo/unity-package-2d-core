# CharacterCrowdControlController

## 1. 문서 목적

이 문서는 `CharacterCrowdControlController`를 설명합니다.
넉백, 넉다운, 넉업 같은 Crowd Control을 어떻게 공통 규칙으로 실행하는지 이해하는 것이 목적입니다.

---

## 2. 역할

`CharacterCrowdControlController`는 **Crowd Control 실행 조정자**입니다.
단순히 캐릭터를 밀어내는 것이 아니라, 다음 책임을 함께 묶어서 처리합니다.

- CC 데이터 해석
- 시작 가능 상태 판정
- 방향 결정
- 애니메이션 재생
- 모션 요청 생성
- 착지/종료 처리
- 벽 충돌 후속 처리
- 시퀀스형 CC 큐 처리

즉, 이 클래스는 “CC 결과를 캐릭터 위에 일관되게 적용하는 오케스트레이터”입니다.

---

## 3. 왜 중요한가

전투 감각은 단순 데미지 수치보다 CC 품질에 크게 좌우됩니다.
`CharacterCrowdControlController`는 다음 계층이 만나는 지점이기 때문에 중요합니다.

- 테이블 기반 Crowd Control 데이터
- 캐릭터 상태 제어
- 애니메이션 전환
- 실제 이동 실행
- 공중/착지 처리
- 몬스터/플레이어 공용 적용 규칙

특히 최근 구조에서는 이동을 직접 하지 않고 `ICharacterMotionController`에 위임하려는 방향이 명확하기 때문에, 모션과 상태를 연결하는 허브로서 중요성이 큽니다.

---

## 4. 핵심 상태

### 현재 활성 상태
- `_activeCrowdControl`
- `_activeSource`
- `_isActive`

현재 캐릭터에 적용 중인 CC 1건을 관리합니다.
코드상 정책은 “한 번에 1개”에 가깝고, 새 CC가 오면 기존 것을 강제로 중단하고 교체하는 흐름입니다.

### 핸들러 맵
- `_handlers`

타입별 세부 구현을 `ICrowdControlHandler`로 분리합니다.
즉, `CharacterCrowdControlController`가 모든 넉백/넉다운 계산을 직접 들고 있지 않습니다.

### 큐 처리 상태
- `_queuedCrowdControls`
- `_isSequenceRunning`
- `_sequenceSource`
- `_sequenceTarget`

즉시 교체만 있는 것이 아니라, 연속 CC 처리용 큐 개념도 갖고 있습니다.

### 애니메이션 상태
- `_currentStaggerAnimationName`
- `_currentPhaseAnimationName`
- `_currentAirborneAnimationPhase`

CC가 단순 위치 이동이 아니라 애니메이션 상태 기계와 결합되어 있음을 보여줍니다.

---

## 5. 주요 진입 메서드

### `ApplyCrowdControl(StruckTableCrowdControl crowdControl, GameObject source, bool isEndCharacterStop = false)`
테이블 행 데이터를 받아 런타임 데이터로 바꾼 뒤 CC를 적용합니다.
테이블 기반 게임플레이와 실제 런타임 실행을 이어주는 대표 진입점입니다.

### `ApplyCrowdControl(CrowdControlRuntimeData crowdControl, GameObject source)`
이미 해석된 런타임 데이터를 바로 적용합니다.
상위 시스템이 데이터를 캐싱하거나 가공한 뒤 넘기기 좋습니다.

### `ApplyCrowdControlInternal(...)`
실제 적용 중심 메서드입니다.
이 메서드 안에서 다음이 결정됩니다.

1. 시작 가능 상태인지
2. 기존 CC를 끊을지
3. 방향을 어떻게 잡을지
4. 애니메이션을 어떻게 시작할지
5. 실제 이동을 모션 컨트롤러에 맡길지
6. 실패 시 스냅 이동으로 대체할지

### `TryBuildMotionRequest(...)`
CC 데이터를 `MotionRequest`로 바꾸는 메서드입니다.
핸들러가 있으면 핸들러에게 위임하고, 없으면 기본 선형 이동 요청을 만듭니다.

---

## 6. 연결해서 봐야 하는 클래스

### 런타임 데이터
- `CrowdControlRuntimeData`
- `CrowdControlRuntimeDataResolver`
- `StruckTableCrowdControl`

### 핸들러 계층
- `ICrowdControlHandler`
- `CrowdControlHandlerKnockBack`
- `CrowdControlHandlerKnockDown`
- `CrowdControlHandlerKnockUp`
- `CrowdControlHandlerKnockDownAir`

### 실제 이동과 물리
- `ICharacterMotionController`
- `CharacterMotionController2D`
- `MotionRequest`

### 캐릭터 기반
- `CharacterBase`
- `Rigidbody2D`
- 애니메이션 컨트롤러 계층

---

## 7. 대표 런타임 흐름

### 흐름 A: 테이블 기반 넉백 적용
1. 상위 시스템이 `StruckTableCrowdControl`을 넘깁니다.
2. 런타임 데이터로 변환합니다.
3. 시작 가능 상태인지 확인합니다.
4. 방향과 끝 위치를 계산합니다.
5. 핸들러가 `MotionRequest`를 구성합니다.
6. 모션 컨트롤러가 실제 이동을 수행합니다.
7. 종료 시 애니메이션/상태 정리를 수행합니다.

### 흐름 B: 모션 컨트롤러가 없을 때
1. CC는 적용 요청을 받습니다.
2. 모션 컨트롤러가 없거나 시작에 실패합니다.
3. 위치 스냅 방식으로 최소한의 결과를 보장합니다.
4. 종료 애니메이션과 정리 로직을 수행합니다.

### 흐름 C: 공중형 CC 착지
1. 공중 상태 CC가 진행됩니다.
2. Update에서 핸들러가 런타임 상태를 갱신합니다.
3. 착지 가능 조건이 되면 착지 단계로 전환합니다.
4. 최종 스냅과 종료 애니메이션을 수행합니다.

---

## 8. 확장 포인트

### 새로운 CC 타입을 추가할 때
이 클래스에 타입별 분기를 늘리기보다, 새 `ICrowdControlHandler`를 추가하고 핸들러 맵에 연결하는 방식이 적합합니다.

### 지면/공중 시작 조건을 바꿀 때
CC 시작 허용 조건은 매우 민감합니다.
범용 if 문을 늘리기보다, 시작 상태 판정 메서드를 정책화하는 편이 좋습니다.

### 종료 후 행동을 늘릴 때
종료 시 `Character.Stop()` 같은 후속 정책을 무분별하게 섞지 않고, 데이터 플래그나 후처리 훅으로 분리하는 편이 안전합니다.

---

## 9. 디버깅 체크리스트

### CC가 적용되지 않는 경우
- 시작 가능 상태 제한(`IsGroundOnly`, `IsAirOnly`)에 걸렸는지 확인합니다.
- 현재 다른 CC가 सक्रिय 중이고 교체가 제대로 되는지 확인합니다.

### 넉백 방향이 이상한 경우
- `source` 기준 방향 계산을 확인합니다.
- travel direction과 최종 end position 계산이 같은 기준인지 확인합니다.

### 공중형 CC가 부자연스러운 경우
- 착지 트리거 거리와 최종 스냅 거리를 같이 확인합니다.
- `CharacterMotionController2D`의 공중 모션 처리와 충돌하는지 확인합니다.

### 벽에 부딪힌 뒤 상태가 꼬이는 경우
- `WallImpacted` 이후 종료 처리 흐름을 확인합니다.
- 모션 종료와 애니메이션 종료가 둘 다 처리되는지 확인합니다.

---

## 10. 한 줄 정리

`CharacterCrowdControlController`는 **Crowd Control 데이터를 받아 방향, 애니메이션, 이동, 종료 상태를 하나의 흐름으로 조정하는 Core 전투 제어 컨트롤러**입니다.
