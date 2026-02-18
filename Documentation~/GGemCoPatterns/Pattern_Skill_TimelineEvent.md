# Pattern_Skill_TimelineEvent (Golden: SkillLungeClip)

골든 샘플:
- GGemCo2DSkillEditor.GGemCoTool.CreateSkill.Timeline.Clips.SkillLungeClip (skill_editor.zip: GGemCoTool/CreateSkill/Timeline/Clips/SkillLungeClip.cs)

목표:
- 스킬 제작/테스트 파이프라인에서 **타임라인 이벤트를 표준 방식으로 확장**
- 데이터(Definition) / 에디터 Authoring(Clip/Track) / 런타임 실행(Executor) **3단 분리**

---

## 1) 3단 구성

1. EventDefinition
- 디자이너 입력 데이터(거리/시간/Easing 등)
- 입력 값 검증(범위, 필수 키)

2. Clip/Track(Authoring)
- 타임라인에서 이벤트를 배치/편집할 수 있는 UI
- 인스펙터 표시(툴팁/가이드 포함)

3. Runtime Executor
- 실제 동작(이동, Projectile 발사, Affect 적용 등)
- 스킬 캔슬/중단 시 정리(Release/Stop) 포함

---

## 2) 이동 이벤트(Lunge/Dash) 표준

- “속도” 입력보다 **거리 + 클립 시간** 기반을 우선
- Easing은 Core 공용 Easing을 사용
- Control의 이동/AutoMove/Wall/Guard와 충돌할 수 있으므로,
  - 토큰 기반 Suspend 또는 전용 Movement Driver를 통해 인계 지점을 단일화

---

## 3) 테스트 툴 표준

- 테스트 중 BT 비활성 등 “환경 격리” 훅 제공
- 테스트 종료 시 원상복구(대상 위치/상태/BT 재개)

---

## 4) 체크리스트

- [ ] Definition/Clip/Executor 3단 분리
- [ ] 입력 검증 및 에러 메시지 제공
- [ ] 캔슬/중단 시 정리(특히 Projectile/VFX)
- [ ] 테스트 툴에서 노출 및 즉시 검증 가능
