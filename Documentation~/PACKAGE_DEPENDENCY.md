# 패키지 의존성 계약 (Dependency Contract)

작성일: 2026-02-18

## 방향(절대 위반 금지)

Core  
⬆  
Control  
⬆  
Skill  
⬆  
AI_BT

## 허용(Allowed)

- Control → Core
- Skill → Control, Core
- AI_BT → Skill, Control, Core

## 금지(Forbidden) 예시

- Core → Control/Skill/AI_BT 참조 추가
- Control → Skill/AI_BT 참조 추가
- Skill → AI_BT 참조 추가
- Runtime 어셈블리에서 UnityEditor 참조

## 의존이 필요할 때의 표준 해법

1) Interface(포트) 를 “하위 계층”에 둔다.  
2) 구현(Adapter/Bridge) 은 “상위 계층”에 둔다.  
3) 하위는 인터페이스만 알고, 상위만 구현을 연결한다.

예:
- Core에 `IProjectileBoundaryPolicy` (interface)
- Skill/Control에서 구현체를 제공하고, Core에는 주입/등록으로 연결
