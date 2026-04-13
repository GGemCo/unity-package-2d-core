# CharacterMotionController2D

## 1. 문서 목적

이 문서는 `CharacterMotionController2D`를 설명합니다.
전진, 대시, 러시, 넉백 같은 “의도된 이동 연출”을 어떻게 공용 모션 시스템으로 처리하는지 이해하는 것이 목적입니다.

---

## 2. 역할

`CharacterMotionController2D`는 **캐릭터 공용 모션 실행기**입니다.
플레이어 입력과 직접 연결되지 않고, `MotionRequest`를 받아 실제 Rigidbody 이동을 수행합니다.

코드의 주석 기준 핵심 특징은 다음과 같습니다.

- 입력과 무관하게 플레이어/몬스터 공용으로 사용 가능
- 모션을 `MotionChannel` 단위로 분리
- 이동 계산을 `IMotionSolver`로 분리
- Rigidbody가 Kinematic이어도 `MovePosition` 기반으로 처리 가능

즉, 이 클래스는 “움직여라”라는 명령을 받아 실행하는 계층이지, “왜 움직여야 하는가”를 판단하는 계층은 아닙니다.

---

## 3. 왜 중요한가

최근 Core 구조에서 모션은 다음 시스템과 강하게 연결됩니다.

- Skill의 돌진/전진
- Crowd Control의 넉백/넉다운/넉업
- 공중 이동 연출
- 강제 위치 홀드
- 벽 충돌 처리

이 클래스가 없으면 각 시스템이 직접 Rigidbody를 만지게 되어 충돌이 많아집니다.
따라서 공통 이동 연출을 한 곳에 모아두는 기준점으로 매우 중요합니다.

---

## 4. 핵심 구조

### 채널 분리
- `_skill`
- `_crowdControl`

코드상 Crowd Control 채널이 Skill 채널보다 우선합니다.
즉, 넉백 같은 강제 제어가 들어오면 일반 스킬 모션보다 우선해서 처리됩니다.

### 주요 참조
- `Rigidbody2D rb`
- `CharacterPhysicsOverrideController physicsOverrideController`
- `CharacterHitStopController hitStopController`

모션은 단순 좌표 이동이 아니라 물리, 중력, 히트스톱과 함께 동작합니다.

### 이벤트
- `WallImpacted`

벽과 충돌했을 때 외부 시스템이 후속 처리를 할 수 있게 해 줍니다.
CC 종료, 충돌 연출, 상태 전환과 연결하기 좋습니다.

---

## 5. 주요 진입 메서드

### `TryStartMotion(in MotionRequest request)`
모션 시작의 핵심 진입점입니다.

이 메서드는 대략 다음 일을 합니다.

1. 요청이 유효한지 검사
2. 동일 채널에서 기존 모션이 재생 중인지 확인
3. 교체가 가능하면 기존 모션 정리
4. 필요 시 요청을 보정
5. 내부 상태를 시작 상태로 전환
6. 물리 상태를 모션 재생에 맞게 준비

### `CancelMotion(MotionChannel channel, int reason = 0)`
지정한 채널의 모션을 취소합니다.
Skill 취소, 피격, 상태 전환, 씬 정리에서 중요합니다.

### `IsPlaying(MotionChannel channel)`
특정 채널에서 현재 모션이 재생 중인지 확인합니다.
상태 중복 방지나 상위 계층 정책 판단에 쓰기 좋습니다.

### `TryGetMotionProgress(MotionChannel channel, out float progress01)`
현재 재생 진행도를 얻습니다.
연출 연결, 중간 상태 전환, UI 디버그에 유용합니다.

---

## 6. 내부 실행 흐름

### `FixedUpdate()`
실제 모션 틱의 중심입니다.

핵심 규칙:
- Rigidbody가 없으면 상태 정리
- HitStop 중이면 진행 중단
- CrowdControl 채널이 먼저 실행
- 그다음 Skill 채널 실행

즉, 이 클래스는 물리 프레임 기준 실행기입니다.

### `Tick(ref MotionState state, float dt)`
한 채널의 모션 상태를 한 프레임 진행시킵니다.
모션 종류에 따라
- PositionHold
- KnockDownAir
- 일반 Solver 기반
으로 분기합니다.

### 벽 충돌 처리
모션 적용 후 벽 충돌이 감지되면 종료 정책에 따라 모션을 정리합니다.
이 부분은 CC와 스킬의 충돌 감각에 직접 영향을 줍니다.

---

## 7. 연결해서 봐야 하는 클래스

### 요청/상태/규약
- `MotionRequest`
- `MotionState`
- `MotionChannel`
- `MotionKind`
- `MotionWallImpactInfo`

### Solver 계층
- `IMotionSolver`
- `MotionSolverLinearMove`
- `MotionSolverLinearMoveHold`
- `MotionSolverArcPhased`

### 연결 시스템
- `CharacterCrowdControlController`
- 스킬 이동 이벤트 계층
- `CharacterPhysicsOverrideController`
- `CharacterHitStopController`

---

## 8. 대표 런타임 흐름

### 흐름 A: 스킬 돌진
1. 상위 시스템이 `MotionRequest`를 만듭니다.
2. Skill 채널로 `TryStartMotion()`을 호출합니다.
3. Solver가 틱마다 이동량을 계산합니다.
4. 벽 충돌이나 완료 시 종료합니다.

### 흐름 B: 넉백
1. `CharacterCrowdControlController`가 CC 요청을 해석합니다.
2. CrowdControl 채널로 모션 요청을 보냅니다.
3. Skill 모션보다 우선해서 실행됩니다.
4. 종료 후 상태 복구가 이어집니다.

### 흐름 C: 히트스톱 중단
1. `CharacterHitStopController`가 활성화됩니다.
2. `FixedUpdate()`가 진행을 멈춥니다.
3. 히트스톱이 끝나면 남은 모션이 다시 진행됩니다.

---

## 9. 확장 포인트

### 새로운 이동 연출을 추가할 때
기존 `TryStartMotion()`에 특수 분기를 계속 추가하기보다, 가능하면 새로운 `IMotionSolver` 또는 요청 해석 계층으로 확장하는 편이 좋습니다.

### 우선순위 정책을 바꿀 때
채널 구조를 먼저 검토해야 합니다.
Skill과 CrowdControl 우선순위를 흐리게 만들면 상호작용 버그가 늘어납니다.

### 물리 처리 보정을 넣을 때
Rigidbody 직접 제어보다 `CharacterPhysicsOverrideController`와 함께 보는 편이 안전합니다.

---

## 10. 디버깅 체크리스트

### 모션이 시작되지 않는 경우
- `MotionRequest`의 거리/시간/높이 값이 유효한지 확인합니다.
- 같은 채널에서 이미 재생 중인데 교체가 금지된 상태인지 확인합니다.

### 모션이 중간에 끊기는 경우
- HitStop이 개입했는지 확인합니다.
- 벽 충돌로 종료된 것인지 확인합니다.
- 상위 시스템이 `CancelMotion()`을 호출했는지 확인합니다.

### CC와 Skill 이동이 충돌하는 경우
- 어떤 채널에 요청을 보냈는지 확인합니다.
- CrowdControl 우선 규칙 때문에 Skill이 멈춘 것인지 확인합니다.

### 공중 모션이 부자연스러운 경우
- `MotionKind.Arc`, `GroundSlam`, `KnockDownAir` 처리 경로를 봅니다.
- Solver와 최종 스냅 처리 위치를 함께 확인합니다.

---

## 11. 한 줄 정리

`CharacterMotionController2D`는 **스킬과 Crowd Control이 공통으로 사용하는 이동 연출 실행기이며, 채널과 Solver 구조로 복잡한 모션을 일관되게 처리하는 핵심 컨트롤러**입니다.
