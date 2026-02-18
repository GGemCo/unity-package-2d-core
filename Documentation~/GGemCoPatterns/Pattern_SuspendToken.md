# Pattern_SuspendToken (Golden: AutoMoveAdapter + PlayerAutoMoveController)

골든 샘플:
- GGemCo2DControl.Core.Input.AutoMoveAdapter (control.zip: Core/Input/AutoMoveAdapter.cs) + GGemCo2DCore.Characters.AutoMove.PlayerAutoMoveController (core.zip: Characters/AutoMove/PlayerAutoMoveController.cs)

목표:
- AutoMove/입력/액션/스킬/벽/가드 등 다양한 기능이 “이동 중지”를 요청할 때,
  **토큰 기반으로 충돌 없이** Suspend/Resume 되게 한다.

---

## 1) 토큰 정책(표준)

- 토큰은 기능별로 분리
  - 예: Guard, Wall, SkillMove, Interaction, HitStun …
- Suspend는 “누적”될 수 있고, Resume는 “해당 토큰”만 해제한다.
- 모든 토큰이 해제되어야 AutoMove가 재개된다.

---

## 2) 우선순위(권장)

- Wall > SkillMove > Guard > Attack > Interaction > 기타

우선순위는 “동시 발생 시 어떤 상태를 유지할지”를 결정하는 규칙이며,
코드로 표현(정렬/enum order)하고 문서화한다.

---

## 3) 구현 체크리스트

- [ ] Suspend 요청은 Adapter 단일 진입점에서 수행
- [ ] Exit/Dispose에서 토큰 해제 보장(try/finally 권장)
- [ ] 디버그: 현재 활성 토큰 목록 표시(옵션)
