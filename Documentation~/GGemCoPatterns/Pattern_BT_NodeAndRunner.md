# Pattern_BT_NodeAndRunner (Golden: MonsterBtRunner + RuntimeBlackboard)

골든 샘플:
- GGemCo2DAiBt.BT.MonsterBtRunner / RuntimeBlackboard (bt.zip: BT/MonsterBtRunner.cs, BT/RuntimeBlackboard.cs)

목표:
- BT 노드 확장 시 런타임 성능/안정성을 유지하고,
  외부 시스템(Control/Skill/Affect)을 안전하게 호출한다.

---

## 1) Node 책임 분리

- Condition: 읽기만(검사)
- Action: 실행(요청) + 진행/완료 관리
- Decorator/Composite: 흐름 제어

---

## 2) Blackboard 규칙

- 키는 Schema로 중앙화
- 타입 안정성 확보(기본값/Nullable 명확화)
- Tick 중 할당 최소화

---

## 3) 외부 시스템 호출 규칙

- 이동은 Control, 스킬은 Skill, 상태효과는 Affect의 “표준 진입점” 호출
- BT에서 데미지 계산/스탯 수정 같은 게임 규칙 직접 구현 금지

---

## 4) 체크리스트

- [ ] Tick 할당 없음(또는 최소)
- [ ] 중단 시 정리(타이머/코루틴/요청)
- [ ] Debug 표시 옵션 제공
