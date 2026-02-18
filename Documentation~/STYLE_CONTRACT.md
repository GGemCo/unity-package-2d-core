# STYLE_CONTRACT.md (GGemCo Project Standard)

작성일: 2026-02-18

목적:
- AI가 GGemCo 코드 스타일을 **자동으로 복제**하도록 만드는 최소 규칙 집합
- 구조 일관성 유지
- 요청 재작업 감소
- 패키지 경계 보호

---

## 1. 우선순위 (Important)

코드 생성/수정 시 다음 순서를 반드시 따른다:

1) PACKAGE_DEPENDENCY.md
2) Docs/GGemCoPatterns/*
3) 각 패키지 CONVENTIONS.md
4) ARCHITECTURE.md
5) CHANGE_PLAYBOOK.md

---

## 2. 파일/구조 규칙

- Runtime / Editor는 반드시 분리한다.
- Runtime 어셈블리에서 `UnityEditor` 참조 금지.
- 클래스는 단일 책임 원칙을 우선한다.
- 대형 클래스 수정 대신:
  - Handler / Adapter / Bridge / Executor 추가를 우선한다.

권장 파일 크기(가이드):
- 50~200 lines: 이상적
- 300~500 lines: 허용
- 700+ lines: 분리 검토

---

## 3. 골든 패턴 복제 규칙 (매우 중요)

새 구조를 만들지 않는다.
아래 골든 샘플 구조를 복제한 뒤 차이점만 적용한다:

- Editor Tool → UseProjectile
- Action Phase Machine → ActionWall
- Skill Timeline Event → SkillLungeClip

복제 우선 순위:
1) 파일 분리 구조
2) 책임 분리 방식
3) 데이터 흐름
4) API 형태

---

## 4. Runtime 스타일 규칙

- Update / FixedUpdate에서 GC 할당 최소화
- 가능하면 NonAlloc API 사용
- LINQ는 hot path에서 사용 금지
- Physics 쿼리는 NonAlloc 우선
- 로그는 Debug flag로 제어

---

## 5. 데이터 중심 설계

- 게임 규칙 값은 하드코딩하지 않는다.
- Table 또는 ScriptableObject Settings로 외부화한다.
- 테이블 변경 시:
  - Runtime parser
  - Loader
  - Editor Tool
  - Export
  모두 함께 갱신한다.

---

## 6. Editor Tool 스타일 규칙

- 대상 선택 UI는 상단 배치
- Undo/Redo 지원
- Reload / Export 버튼 제공
- 런타임 즉시 테스트 가능
- ReadOnly 모드 최소화 (즉시 수정 + 테스트 중심)

---

## 7. AutoMove / Suspend Token

- Suspend는 토큰 기반으로만 처리
- 획득/해제는 반드시 짝을 맞춘다.
- Exit/Dispose 경로에서 해제 보장

---

## 8. Timeline 이벤트 규칙

- Definition / Clip / Runtime Executor 3단 구조 유지
- 디자이너 친화:
  - 거리 + 시간 + Easing 우선
- 캔슬/중단 시 정리 코드 포함

---

## 9. AI 산출물 규칙

- 변경/추가 파일만 제공
- 변경 요약 포함
- 테스트 시나리오(최대 3개) 포함
- 기존 구조를 재설계하지 않는다.

---

## 10. 금지(Banned Moves)

- 패키지 dependency 방향 변경
- Core에서 상위 패키지 참조 추가
- Runtime ↔ Editor 혼합
- 기존 대형 클래스 전체 리라이트
- 이유 없는 리팩토링

---

## 11. Self Check (AI 내부 체크)

코드 생성 전에 스스로 확인:

1) Dependency Contract 위반 없는가?
2) Golden Pattern을 복제했는가?
3) Runtime/Editor 분리 유지되는가?
4) 변경 파일 수 최소화했는가?
5) 테스트 시나리오 포함했는가?
