# 6. 연출 시스템(Cutscene / Direction System) 추가 정리

## 6-1. 역할

연출 시스템은 **게임 플레이 흐름 위에 배치되는 상위 콘텐츠 실행 계층**입니다.

주요 목적은 다음과 같습니다.

- 컷신(Cutscene) 재생
- 화면 전환(Screen Fade)
- 오버레이 텍스트(Overlay Text)
- 캐릭터 강조/비강조(예: White Overlay)
- 카메라, UI, 이펙트, 애니메이션을 포함한 연출 이벤트의 시간축 실행
- JSON 기반 연출 데이터의 저장 / 불러오기 / 재생

즉, 연출 시스템은 전투나 이동 자체를 직접 소유하는 시스템이 아니라,  
**기존 Core / Control / Skill 계층의 기능을 조합해서 특정 시점에 연출적으로 실행하는 오케스트레이션 계층**으로 보는 것이 적절합니다.

---

## 6-2. 계층 관점에서의 위치

연출 시스템은 Core의 하위가 아니라 **Core 위에서 동작하는 상위 시스템**으로 취급하는 것이 좋습니다.

이유는 다음과 같습니다.

- Core는 캐릭터, UI, 로딩, 저장, VFX, 공통 런타임 기반을 제공하는 하위 계층입니다.
- 연출 시스템은 그 기반을 사용해 “언제 무엇을 보여줄지”를 조합하는 콘텐츠 계층입니다.
- 따라서 Core가 연출 시스템을 참조하면 의존성 방향이 뒤집히기 쉽습니다.

정리하면 다음 원칙이 적절합니다.

- 연출 Runtime은 Core / 필요 시 Control 을 참조한다.
- Core는 연출 전용 타입이나 Editor 툴을 직접 참조하지 않는다.
- 연출 전용 데이터 정의, 타임라인, 실행기, 에디터는 별도 패키지/폴더에서 관리한다.

---

## 6-3. Runtime 책임 분리 권장안

연출 시스템 Runtime은 아래처럼 나누는 것이 유지보수에 유리합니다.

### `CutsceneData`
**역할**
연출 1건의 루트 데이터입니다.  
연출 이름, 식별자, 타임라인 목록, 메타데이터를 보유합니다.

**책임**
- 연출 단위의 루트 컨테이너
- 여러 타임라인/트랙의 묶음
- 저장/로드 대상의 최상위 모델

---

### `CutsceneTimeline`
**역할**
시간축 기반 이벤트 목록을 보관하는 데이터입니다.

**책임**
- 시작 시간 / 길이 / 트랙 정보 보관
- 여러 클립(Event Clip)의 순서 관리
- 런타임 재생 시 평가 대상 제공

---

### `CutsceneEventDefinition`
**역할**
실제 연출 1개를 설명하는 직렬화 가능한 이벤트 정의입니다.

**예시**
- ScreenFade
- OverlayText
- CharacterWhiteOverlay
- CameraShake
- Wait
- PlayAnimation
- MoveCharacter

**책임**
- 이벤트 종류 구분
- 이벤트별 파라미터 정의
- JSON 직렬화 대상 모델 제공

---

### `CutsceneRunner`
**역할**
CutsceneData를 실제 런타임에서 재생하는 실행기입니다.

**책임**
- 타임라인 시간 진행
- 이벤트 시작/업데이트/종료 제어
- 중단 / 스킵 / 완료 처리
- 연출 재생 상태 관리

---

### `CutsceneEventExecutor`
**역할**
이벤트 타입별 실제 실행 로직을 담당하는 실행 계층입니다.

**책임**
- ScreenFade 실행
- OverlayText 표시
- 캐릭터 강조 머티리얼/오버레이 적용
- Core 시스템 호출 연결

**권장 이유**
이벤트 데이터와 실행 코드를 분리하면,
- JSON 구조는 안정적으로 유지되고
- 실제 런타임 구현은 교체/확장이 쉬워집니다.

---

## 6-4. Core와의 연결 지점

연출 시스템은 Core의 기존 공통 시스템을 직접 재사용하는 것이 좋습니다.

### 캐릭터 이동 / 위치 연출
`CharacterMotionController2D`를 통해 처리합니다.

연출 이동은 별도 이동 시스템을 새로 만들기보다,
기존 Motion 계층을 재사용하는 편이 일관성과 디버깅 측면에서 유리합니다.

---

### 화면 효과 / 강조 효과
- `VfxManager`
- 캐릭터 Overlay 전용 컴포넌트
- UI Fade 전용 Presenter / Controller

기존 전역 VFX/표현 계층을 재사용하고,
연출 시스템은 “언제 요청할지”만 결정하는 편이 좋습니다.

---

### UI 연출
- Overlay Text
- Letter Box
- Screen Fade
- Skip UI
- Dialogue 연계 UI

이 부분은 SceneGame 또는 UIWindowManager 기반의 공용 UI 루트를 활용하되,
연출 전용 Presenter를 따로 두는 구조가 좋습니다.

---

## 6-5. Editor 책임 분리 권장안

연출 시스템 Editor는 아래처럼 나누는 편이 좋습니다.

### `CutsceneEditorWindow`
**역할**
연출 데이터 전체를 편집하는 메인 EditorWindow입니다.

**책임**
- 연출 선택
- 타임라인/트랙 표시
- 클립 추가/삭제/복제
- JSON Import / Export
- 미리보기 / 테스트 실행

---

### `CutsceneEventClipEditor`
**역할**
클립 단위의 편집 UI를 담당합니다.

**책임**
- 시간, 길이, 이벤트 타입 편집
- 각 이벤트 정의를 Inspector 형태로 표시
- Undo/Redo 대응

---

### `CutsceneEventDrawer`
**역할**
이벤트 종류별 상세 필드 렌더링을 담당하는 Drawer 계층입니다.

**책임**
- ScreenFade 전용 필드
- OverlayText 전용 필드
- CharacterWhiteOverlay 전용 필드
- 공통 필드/개별 필드 분기

**권장 이유**
이 계층을 두면 `CutsceneEditorWindow`가 비대해지는 것을 막을 수 있습니다.

---

### `CutsceneTimelineJsonUtility`
**역할**
연출 JSON 직렬화/역직렬화 전용 유틸리티입니다.

**책임**
- JSON 저장/로드
- 버전 호환 처리
- Unity 타입(Color 등) 안전 직렬화 보조

**주의**
Unity 직렬화 타입(Color, Vector, Gradient 등)은
직접 JSON 변환 시 순환 참조/형식 차이 문제가 생기기 쉬우므로,
전용 DTO 또는 안전 변환 계층을 두는 것이 좋습니다.

---

## 6-6. Editor UX 권장 사항

연출 툴은 이벤트 종류와 참조 대상이 많아지기 쉬우므로,
기존 Core Editor 방향과 맞춰 아래 기준을 권장합니다.

- 이벤트 타입 선택은 검색 가능한 드롭다운 사용
- 캐릭터/타임라인/프리셋 선택도 검색 가능한 드롭다운 사용
- 상세 필드 편집은 Drawer 계층으로 분리
- Undo/Redo를 기본 지원
- JSON Export 전에 Validate 수행
- 잘못된 참조는 즉시 경고 표시

특히 연출 선택, 이벤트 타입 선택, 참조 리소스 선택은  
`SearchableDropdownUtility` 기반으로 통일하면 툴 사용성이 크게 좋아집니다.

---

## 6-7. 데이터 설계 원칙

연출 시스템 데이터는 다음 기준을 지키는 것이 좋습니다.

### 1) 실행 로직보다 데이터가 먼저
이벤트는 “무엇을 할지”를 데이터로 정의하고,
실행 로직은 별도 Executor가 담당합니다.

### 2) Unity Object 직접 참조 최소화
JSON 저장이 필요한 데이터는
직접적인 Scene Object 참조보다
UID / Key / Addressable Key 기반으로 보관하는 편이 안전합니다.

### 3) 이벤트 단위를 작게 유지
하나의 이벤트가 너무 많은 기능을 가지면
에디터도 복잡해지고 런타임 분기도 비대해집니다.

예)
- `ScreenFade`
- `OverlayText`
- `CharacterHighlight`
- `Wait`
- `PlaySfx`

처럼 잘게 나누는 편이 좋습니다.

### 4) 공통 필드와 개별 필드 분리
모든 이벤트가 공통으로 가지는 값
- StartTime
- Duration
- Enabled
- Comment

과 이벤트 고유 필드를 분리하면
Editor와 JSON 구조가 단순해집니다.

---

## 6-8. 추천 문서화 우선순위

연출 시스템 문서를 별도로 만들 경우 아래 순서를 추천합니다.

### Runtime 1순위
- `CutsceneData`
- `CutsceneTimeline`
- `CutsceneRunner`
- `CutsceneEventExecutor`

### Runtime 2순위
- Overlay / Fade / Highlight 관련 개별 Executor
- 연출 트리거 컴포넌트
- Skip / Cancel / AutoPlay 정책

### Editor 1순위
- `CutsceneEditorWindow`
- `CutsceneEventClipEditor`
- `CutsceneEventDrawer`
- `CutsceneTimelineJsonUtility`

### Editor 2순위
- 연출 미리보기 툴
- 연출 Validation 유틸리티
- 프리셋/템플릿 생성 툴

---

## 6-9. 한 문장 요약

연출 시스템은 **Core의 공통 기능을 시간축 기반으로 조합해 컷신, 화면 효과, UI 오버레이, 캐릭터 표현을 실행하는 상위 콘텐츠 오케스트레이션 계층**으로 정의하는 것이 가장 적절합니다.