# Core 패키지 추천 읽기 순서

## 1. 이 문서의 목적

Core 패키지는 범위가 넓습니다.
UI만 있는 것도 아니고, 전투만 있는 것도 아니며,
캐릭터, 저장, 테이블, 연출, 로딩, 설정이 모두 함께 들어 있습니다.

그래서 처음부터 파일 이름 순서대로 읽으면 금방 길을 잃기 쉽습니다.
이 문서는 **어떤 목적을 가진 개발자가 어떤 순서로 Core를 읽으면 좋은지**를 안내합니다.

---

## 2. 가장 추천하는 기본 순서

Core를 처음 읽는다면 아래 순서를 권장합니다.

### 1단계: 전체 진입점 파악

1. `Core/GameLoaderManager.cs`
2. `Scenes/SceneGame.cs`
3. `TableLoader/TableLoaderManager.cs`
4. `AddressableLoader/AddressableLoaderController.cs`

이 4개를 먼저 보면 아래가 정리됩니다.

- 게임이 언제 시작되는가
- 공용 매니저가 어디에 모이는가
- 데이터가 어디서 오는가
- 에셋 로딩이 어떤 방식으로 이루어지는가

즉, **게임 시작과 공용 기반**을 먼저 이해하게 됩니다.

### 2단계: 캐릭터 중심축 파악

5. `Characters/CharacterBase.cs`
6. `Characters/CharacterBase.Lifecycle.cs`
7. `Characters/CharacterBase.State.cs`
8. `Characters/CharacterStat.cs`
9. `Characters/Stats/CharacterStat.Resources.cs`
10. `Characters/Stats/CharacterStat.BonusHp.cs`

이 구간을 보면 아래가 정리됩니다.

- 캐릭터가 어떤 공통 상태를 가지는가
- 초기화 시점이 어떻게 나뉘는가
- 상태/리소스/보너스 HP가 어디서 관리되는가
- 플레이어와 몬스터가 어떤 공통 기반 위에 놓이는가

### 3단계: 전투 처리 축 파악

11. `Characters/CharacterDamageController.cs`
12. `Characters/Motion/CharacterMotionController2D.cs`
13. `Characters/CrowdControl/CharacterCrowdControlController.cs`
14. `Animation/Animation2dController.cs`
15. `Animation/AnimationEventMediator.cs`

이 구간을 보면 아래가 정리됩니다.

- 피격이 어떤 흐름으로 들어오는가
- 이동 연출과 물리성 움직임이 어디서 처리되는가
- Crowd Control이 어디에서 실행되는가
- 애니메이션 이벤트가 런타임과 어떻게 연결되는가

### 4단계: UI 축 파악

16. `UI/Core/Base/UIWindowManager.cs`
17. `UI/Core/Base/UIWindow.cs`
18. `Characters/Player/PlayerUIController.cs`
19. `Popup/PopupManager.cs`
20. `SystemMessage/SystemMessageManager.cs`

이 구간을 보면 아래가 정리됩니다.

- 윈도우가 어떤 기준으로 초기화되는가
- HUD와 플레이어 데이터는 어떻게 연결되는가
- 팝업과 메시지는 어느 계층 책임인가

### 5단계: 저장/복원 축 파악

21. `SaveData/Base/SaveDataManagerBase.cs`
22. `SaveData/SaveDataManager.cs`
23. `SaveData/Support/SaveRegistry.cs`
24. `SaveData/Support/SaveEnvelope.cs`
25. `SaveData/Data/PlayerData.cs`

이 구간을 보면 아래가 정리됩니다.

- 저장 책임이 어디에 있는가
- 실제 데이터 객체는 어떤 식으로 나뉘는가
- 복원 순서 문제를 어떻게 해결하는가

---

## 3. 목적별 추천 읽기 순서

## A. 캐릭터/전투를 먼저 보고 싶은 개발자

아래 순서를 권장합니다.

1. `CharacterBase`
2. `CharacterStat`
3. `CharacterDamageController`
4. `CharacterMotionController2D`
5. `CharacterCrowdControlController`
6. `Player`
7. `Monster`
8. `ControllerMonsterSuperArmor`
9. `PlayerUIController`

이 순서는 액션 게임 플레이 감각을 먼저 이해하려는 개발자에게 적합합니다.

## B. UI/HUD를 먼저 보고 싶은 개발자

아래 순서를 권장합니다.

1. `SceneGame`
2. `UIWindowManager`
3. `UIWindow`
4. `PlayerUIController`
5. `UIFloatingTextManager`
6. `PopupManager`
7. `SystemMessageManager`
8. 관심 있는 `UI/Windows/*`

이 순서는 HUD, 인벤토리, 팝업, 플레이어 상태 표시를 먼저 파악하려는 개발자에게 적합합니다.

## C. 데이터/테이블 구조를 먼저 보고 싶은 개발자

아래 순서를 권장합니다.

1. `TableLoaderManager`
2. `TableLoaderBase`
3. `TableRegistry`
4. `TableLoader/Table/*`
5. `ConfigAddressableTable*` 계열
6. `ScriptableSettings/*`

이 순서는 데이터 주도 구조와 확장 포인트를 먼저 이해하려는 개발자에게 적합합니다.

## D. 저장/복원을 먼저 보고 싶은 개발자

아래 순서를 권장합니다.

1. `SaveDataManagerBase`
2. `SaveDataManager`
3. `SaveRegistry`
4. `ISaveContributor`
5. `SaveEnvelope`
6. `PlayerData`, `InventoryData`, `QuickSlotData`

이 순서는 초기화 순서 이슈나 세이브 연동 문제를 먼저 해결해야 하는 개발자에게 적합합니다.

---

## 4. 첫날 온보딩 기준 추천 읽기 플랜

### 30분 플랜

아래만 읽어도 Core의 윤곽은 잡힙니다.

1. `README.md`
2. `core_architecture.md`
3. `GameLoaderManager`
4. `SceneGame`
5. `CharacterBase`
6. `UIWindowManager`
7. `SaveRegistry`

### 반나절 플랜

아래까지 보면 실제 수정 포인트 판단이 가능해집니다.

1. 30분 플랜 전체
2. `TableLoaderManager`
3. `CharacterStat`
4. `CharacterDamageController`
5. `CharacterMotionController2D`
6. `PlayerUIController`
7. `SaveDataManagerBase`

### 하루 플랜

아래까지 보면 Core 전체의 주요 책임 분리가 어느 정도 보입니다.

1. 반나절 플랜 전체
2. `CharacterCrowdControlController`
3. `Animation2dController`
4. `AnimationEventMediator`
5. `VfxManager`
6. `ProjectileController`
7. `PopupManager`
8. `SystemMessageManager`
9. 관심 있는 `UI/Windows/*`
10. 관심 있는 `TableLoader/Table/*`

---

## 5. 클래스별로 무엇을 확인해야 하는가

## `GameLoaderManager`

볼 것:
- 어떤 로드 스텝을 등록하는가
- 등록 순서와 중복 체크는 어떻게 하는가
- 진행률 계산은 어떤 방식인가

목표:
- 프로젝트 시작 시점의 초기화 흐름을 이해한다.

## `SceneGame`

볼 것:
- 어떤 전역 매니저를 참조하는가
- 어떤 공용 컨테이너를 씬에서 보관하는가
- 어떤 시스템이 씬 중심으로 연결되는가

목표:
- 게임 씬 기준 공용 객체 허브를 이해한다.

## `TableLoaderManager`

볼 것:
- 어떤 테이블이 등록되는가
- 어떤 데이터가 Core 책임인지
- 새 데이터 테이블을 어디에 추가해야 하는가

목표:
- 데이터 진입점을 이해한다.

## `CharacterBase`

볼 것:
- partial 분리 기준
- 초기화 완료 시점
- 상태/전투/표현/애니메이션 이벤트 연결 구조

목표:
- 캐릭터 공통 기반의 경계를 이해한다.

## `CharacterStat`

볼 것:
- 어떤 리소스를 들고 있는가
- 계산/캐시/보정 구조가 어떻게 나뉘는가
- 보너스 HP와 일반 리소스 흐름이 어떻게 구분되는가

목표:
- 전투 데이터 중심축을 이해한다.

## `UIWindowManager`

볼 것:
- 윈도우가 테이블과 어떻게 연결되는가
- show/hide, 아이콘, sibling index 구조가 어떻게 되는가

목표:
- Core UI 공통 구조를 이해한다.

## `SaveRegistry`

볼 것:
- 등록 시점과 복원 시점 연결 방식
- pending restore 개념

목표:
- 초기화 순서 이슈를 완화하는 저장 구조를 이해한다.

---

## 6. 읽을 때 함께 체크하면 좋은 질문

각 클래스를 읽을 때 아래 질문을 반복하면 이해 속도가 빨라집니다.

1. 이 클래스는 **진입점**인가, **실행기**인가, **데이터 보관소**인가, **브리지**인가?
2. 이 클래스는 누가 생성하고 누가 호출하는가?
3. 상태를 직접 소유하는가, 아니면 다른 시스템의 상태를 반영만 하는가?
4. 여기서 기능을 추가하는 것이 맞는가, 아니면 더 아래/위 계층이 맞는가?
5. 이 클래스와 항상 같이 봐야 하는 파일은 무엇인가?

---

## 7. 디버깅 관점 추천 역추적 순서

## UI가 갱신되지 않을 때

1. `PlayerUIController`
2. 해당 `UIWindow*`
3. `UIWindowManager`
4. `SceneGame`
5. 필요한 경우 `CharacterStat`

## 데이터가 적용되지 않을 때

1. `TableLoaderManager`
2. 해당 `Table*`
3. `AddressableLoader*`
4. 해당 Manager / Controller

## 저장이 복원되지 않을 때

1. `SaveRegistry`
2. `ISaveContributor` 구현체
3. `SaveDataManagerBase`
4. 실제 `*Data` 클래스

## 전투 상태가 이상할 때

1. `CharacterBase`
2. `CharacterStat`
3. `CharacterDamageController`
4. `CharacterMotionController2D`
5. `CharacterCrowdControlController`
6. `AnimationEventMediator`

---

## 8. Core를 읽을 때 자주 하는 실수

### 1. UI부터 보고 전체 구조를 추측하는 것

Core는 UI 패키지가 아닙니다.
UI만 보면 전체 구조가 왜 그렇게 생겼는지 이해하기 어렵습니다.
먼저 `GameLoaderManager`, `SceneGame`, `TableLoaderManager`를 보는 편이 더 좋습니다.

### 2. Player만 보고 Character 공통 구조를 건너뛰는 것

플레이어를 수정하더라도 대부분의 기반 책임은 `CharacterBase`, `CharacterStat`에 있습니다.
공통 기반을 먼저 이해하지 않으면 수정 위치 판단이 자주 어긋납니다.

### 3. 저장 문제를 SaveDataManager만 보고 끝내는 것

초기화 순서가 걸린 문제는 `SaveRegistry`와 `ISaveContributor`까지 봐야 정확히 이해됩니다.

### 4. 테이블 문제를 UI 문제로 착각하는 것

표시가 안 되는 문제는 UI가 아니라 `TableLoaderManager`나 `TableWindow` 데이터 문제일 때가 많습니다.

---

## 9. 한 줄 요약

Core 패키지는 **게임 시작 → 데이터 준비 → 캐릭터 초기화 → UI 반영 → 저장/복원** 흐름으로 읽는 것이 가장 이해하기 쉽습니다.
