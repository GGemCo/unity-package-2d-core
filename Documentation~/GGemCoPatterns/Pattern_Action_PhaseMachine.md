# Pattern_Action_PhaseMachine (Golden: ActionWall)

골든 샘플:
- GGemCo2DControl.Action.Wall.ActionWall* (control.zip: Action/Wall/*)

목표:
- 단일 Action 클래스가 비대해지지 않도록, **Phase(상태) 단위로 분리**하여 유지보수/확장성을 확보
- 물리/센서/디버그/설정 데이터를 분리하여 책임을 명확히 함

---

## 1) 권장 파일 분리

- `ActionX.cs` : public API, 생명주기, Phase 스위칭 최소 로직
- `ActionXContext.cs` : 런타임 참조(Owner, Animator, Rigidbody 등) 및 공용 상태
- `ActionXSettings.cs` : 튜닝 파라미터(가능하면 ScriptableSettings 또는 Settings 클래스)
- `ActionXPhysics.cs` : Rigidbody/Physics 적용(속도/바디타입/그라비티 등)
- `ActionXPhaseSwitch.cs` : 전환 규칙 캡슐화
- `ActionXDebug*.cs` : 디버그/Gizmo/로그(기본 비활성)

---

## 2) Phase 인터페이스 표준

각 Phase는 아래 메서드를 권장:

- `Enter(ctx)`
- `Tick(ctx, input)`
- `FixedTick(ctx, input)`
- `Exit(ctx)`

규칙:
- Phase는 “단일 책임”만 수행
- 전환 조건은 `PhaseSwitch` 또는 명시적 helper로 중앙화

---

## 3) 설정/튜닝 표준

- 튜닝 값은 하드코딩 금지
- 가능한 ScriptableObject(Settings)로 외부화
- 디버그 플래그도 Settings로 제어

---

## 4) 물리 처리 표준

- Rigidbody2D 설정 변경(BodyType/GravityScale 등)은 반드시 “원복 경로”를 보장
- NonAlloc Physics 쿼리 우선 사용
- Phase 간 인계 시 속도/상태가 누락되지 않도록 Context에 기록

---

## 5) AutoMove 연동(토큰 기반)

- Action이 시작되면 AutoMove Suspend 토큰 획득
- Action이 종료되면 반드시 토큰 해제(Exit/Dispose)
- 토큰은 기능별 분리(Guard/Wall/SkillMove 등)

---

## 6) 체크리스트

- [ ] Phase별 파일 분리 완료
- [ ] Settings 분리 및 튜닝 가능
- [ ] Physics 변경 원복 보장
- [ ] AutoMove 토큰 획득/해제 짝 보장
- [ ] 디버그는 기본 Off
