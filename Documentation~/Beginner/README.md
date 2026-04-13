# Core 패키지 문서 시작점

## 문서 목적

이 문서는 **Core 패키지를 처음 보는 개발자**가 빠르게 구조를 이해하고,
어디서부터 코드를 읽어야 하는지 판단할 수 있도록 만든 입문용 문서입니다.

Core는 프로젝트의 공통 런타임 기반 패키지입니다.
캐릭터, 스탯, 피격, UI, 저장/복원, 테이블, Addressables, VFX/사운드 같은
여러 시스템이 이 패키지에 모여 있으며, Control / Skill / Affect / AI BT 같은 상위 패키지들이 Core를 기반으로 동작합니다.

---

## 이 문서를 먼저 읽어야 하는 사람

- 프로젝트에 새로 합류한 Unity 프로그래머
- Core 패키지를 수정해야 하는데 시작 지점을 모르는 개발자
- Control / Skill / Affect / AI BT 를 보기 전에 하위 계층을 먼저 이해하고 싶은 개발자
- 기능 추가 전에 Core의 책임 범위를 먼저 파악하고 싶은 개발자

---

## 먼저 알아야 할 핵심 원칙

### 1. Core는 하위 계층입니다

Core는 공통 기반 패키지이며, 상위 패키지에 의존하지 않습니다.
의존 방향은 아래처럼 유지하는 것이 기준입니다.

```text
Core
↑
Control
↑
Skill
↑
AI_BT
```

즉, Core 안에서 Control / Skill / AI BT 구현 세부사항을 직접 참조하는 방향은 피해야 합니다.
의존이 필요하면 인터페이스를 Core에 두고, 상위 패키지에서 구현을 연결하는 방식이 권장됩니다.

### 2. Runtime과 Editor는 분리해서 이해해야 합니다

Core는 크게 두 영역으로 나뉩니다.

- **Runtime**: 실제 게임 플레이 중 동작하는 코드
- **Editor**: 테이블 편집, 테스트 툴, 프로젝트 셋업, 에셋 생성 같은 개발 도구

문서를 읽을 때도 먼저 Runtime 구조를 이해하고, 그다음 Editor 툴이 Runtime을 어떻게 보조하는지 보는 순서가 좋습니다.

### 3. Core는 “기능 모음”이 아니라 “흐름의 바닥층”입니다

Core는 단순 유틸리티 집합이 아닙니다.
게임 시작 시 로딩이 어떻게 이루어지는지,
캐릭터가 어떻게 초기화되는지,
UI가 어떤 기준으로 데이터를 표시하는지,
저장/복원이 어떤 방식으로 연결되는지 같은 **전체 흐름의 공통 축**을 제공합니다.

---

## 문서 읽기 순서

### 1단계: 패키지 관점 이해

1. `README.md`
2. `docs/01_overview/core_architecture.md`
3. `docs/01_overview/core_reading_order.md`

### 2단계: 실제 코드 진입점 이해

아래 클래스부터 읽는 것을 권장합니다.

1. `Core/GameLoaderManager.cs`
2. `TableLoader/TableLoaderManager.cs`
3. `Scenes/SceneGame.cs`
4. `Characters/CharacterBase.cs`
5. `Characters/CharacterStat.cs`
6. `UI/Core/Base/UIWindowManager.cs`
7. `SaveData/Support/SaveRegistry.cs`

### 3단계: 관심 분야별 심화

- 전투/캐릭터: `Characters/`
- UI/HUD: `UI/`
- 데이터/테이블: `TableLoader/`
- 저장/복원: `SaveData/`
- 리소스 로딩: `AddressableLoader/`, `Configs/Addressables/`
- 연출/VFX/Projectile: `Vfx/`, `Projectile/`, `Animation/`, `Cutscene/`

---

## Core Runtime를 한 문장으로 정리하면

Core Runtime는 **게임 시작과 로딩, 캐릭터 공통 로직, UI, 데이터 테이블, 저장/복원, 전역 피드백 시스템을 제공하는 기반 런타임 계층**입니다.

실제 업로드된 Runtime 소스에서도 `UI`, `Characters`, `TableLoader`, `SaveData`, `Cutscene`, `Maps`, `Items` 폴더 비중이 크며,
이 구조는 Core가 단순 전투 모듈이 아니라 **게임 전반의 공통 프레임워크**라는 점을 보여줍니다.

---

## 추천 문서 활용 방식

이 문서는 “설명서”라기보다 “길잡이”로 사용하는 것이 좋습니다.

- 기능을 추가하기 전에는 **어느 계층 책임인지 판단하는 기준**으로 사용합니다.
- 버그를 수정할 때는 **어느 진입점부터 역추적할지 정하는 기준**으로 사용합니다.
- 새 팀원이 들어오면 **온보딩 순서**로 사용합니다.

---

## 작성 기준

이 문서는 다음 자료를 바탕으로 정리되었습니다.

- `core_runtime.zip`의 Runtime 코드 구조
- 프로젝트 내부 문서 `ARCHITECTURE.md`
- 프로젝트 내부 문서 `PACKAGE_DEPENDENCY.md`
- 기존 Core 패키지 핵심 클래스 정리 문서

즉, 단순 개념 설명이 아니라 **현재 프로젝트 기준 구조와 책임**을 기준으로 작성되었습니다.

---

## 다음에 이어질 문서 방향

이후 문서는 다음 순서로 확장하는 것을 권장합니다.

1. Runtime 기능 영역 문서
2. Editor 기능 영역 문서
3. 핵심 클래스 상세 문서
4. 확장 레시피 문서
5. 디버깅 체크리스트 문서

이 순서를 따르면 문서가 커져도 유지보수가 쉽고, 패키지 구조가 바뀌어도 영향 범위를 추적하기 쉽습니다.
