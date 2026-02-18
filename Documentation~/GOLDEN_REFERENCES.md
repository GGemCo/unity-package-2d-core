# GOLDEN_REFERENCES.md (GGemCo Golden Samples)

작성일: 2026-02-18

목적:
- AI가 **새로운 구조를 발명하지 않고**
  기존 GGemCo 스타일을 자동 복제하도록 강제

---

## 1. Editor Tool Golden

Golden Sample:
- UseProjectile (Core Editor Tool)

적용 상황:
- UseX 계열 툴 추가
- 테이블 편집/테스트 툴
- 대상 선택 + 즉시 실행 툴

반드시 복제할 것:
- UI 배치 순서 (대상 → 데이터 → 실행 → 저장)
- Undo/Redo 패턴
- Reload/Export 흐름
- 캐시 갱신 방식

---

## 2. Action Phase Machine Golden

Golden Sample:
- ActionWall (Control)

적용 상황:
- 새로운 액션(ActionGuard, ActionDash 등)
- 벽/점프/이동 상태 머신

반드시 복제할 것:
- Phase 클래스 분리
- Enter / Tick / FixedTick / Exit 구조
- Context + Settings 분리
- Physics 원복 패턴

---

## 3. Skill Timeline Golden

Golden Sample:
- SkillLungeClip (Skill Editor)

적용 상황:
- 신규 Timeline 이벤트 추가
- 이동/발사체/이펙트 이벤트

반드시 복제할 것:
- Definition / Clip / Runtime Executor 3단 구조
- 디자이너 파라미터 구조
- 테스트 툴 연동 방식

---

## 4. Table 변경 Golden

Golden Reference:
- Core TableLoader + UseProjectile Tool 흐름

적용 상황:
- 테이블 컬럼 추가/삭제
- 데이터 구조 확장

반드시 수행:
- Runtime Struct
- Parser
- Loader
- Editor Tool
- Export
- Sample Data

---

## 5. AutoMove Suspend Golden

Golden Reference:
- AutoMoveAdapter + PlayerAutoMoveController

적용 상황:
- 이동 제어가 필요한 Action/Skill

반드시 복제할 것:
- 토큰 기반 Suspend/Resume
- Exit에서 해제 보장
- 우선순위 명시

---

## 6. BT Golden

Golden Reference:
- MonsterBtRunner + RuntimeBlackboard

적용 상황:
- BT 노드 추가/수정

반드시 복제할 것:
- Node 책임 분리
- Blackboard 사용
- 외부 시스템 표준 진입점 호출

---

## 7. 요청 시 사용 문장 (추천)

Editor Tool:
- "UseProjectile 골든 구조를 복제하고 차이점만 적용"

Action:
- "ActionWall 골든 구조를 복제하고 Phase만 추가"

Timeline:
- "SkillLungeClip 골든 구조를 복제하고 Definition/Clip/Executor만 추가"

---

## 8. 핵심 원칙

AI는:
- 새로운 패턴을 만들지 않는다.
- Golden 구조를 복제한다.
- 차이점만 구현한다.
