# 패키지 의존성 계약 (Dependency Contract)

작성일: 2026-02-18  
수정일: 2026-06-21

## 방향(절대 위반 금지)

```text
Core ← Quest
Core ← Affect
Core ← Control ← Skill ← AI_BT
```

Quest는 Core에서 분리된 상위 패키지입니다. Core는 Quest를 알지 않으며, Quest가 Core의 공통 런타임 포트와 레지스트리를 사용해 게임 씬에 연결됩니다.

## 허용(Allowed)

- Quest → Core
- Affect → Core
- Control → Core
- Skill → Control, Core
- AI_BT → Skill, Control, Core

## 금지(Forbidden) 예시

- Core → Quest/Control/Affect/Skill/AI_BT 참조 추가
- Quest → Control/Affect/Skill/AI_BT 참조 추가
- Control → Quest/Skill/AI_BT 참조 추가
- Skill → Quest/AI_BT 참조 추가
- AI_BT 하위 패키지 코드에 대한 역참조 추가
- Runtime 어셈블리에서 UnityEditor 참조

## 의존이 필요할 때의 표준 해법

1) Interface(포트) 를 “하위 계층”에 둔다.  
2) 구현(Adapter/Bridge) 은 “상위 계층”에 둔다.  
3) 하위는 인터페이스만 알고, 상위만 구현을 연결한다.

예:
- Core에 `IProjectileBoundaryPolicy` (interface)
- Skill/Control에서 구현체를 제공하고, Core에는 주입/등록으로 연결

Quest 분리 예:
- Core에 `IInteractionChoiceContributor`, `IMonsterRespawnSuppressionPolicy`, `ISaveContributor` 같은 포트/레지스트리를 둔다.
- Quest는 `QuestInteractionChoiceContributor`, `QuestMonsterRespawnSuppressionPolicy`, `QuestData`를 구현하고 Core 레지스트리에 등록한다.
- Core는 Quest 구현체 타입을 직접 참조하지 않는다.
