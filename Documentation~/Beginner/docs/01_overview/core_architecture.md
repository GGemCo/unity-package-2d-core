# Core 패키지 아키텍처 개요

## 1. 이 문서의 목적

이 문서는 Core 패키지의 역할과 구조를 **아키텍처 관점**에서 설명합니다.
개별 클래스의 구현 세부보다, 아래 질문에 답하는 것을 목표로 합니다.

- Core는 프로젝트에서 어떤 위치에 있는가
- Runtime과 Editor는 어떤 책임으로 나뉘는가
- 실제 코드 폴더는 어떤 시스템 축으로 구성되어 있는가
- 기능을 추가할 때 어떤 위치에 넣는 것이 맞는가

---

## 2. Core의 위치

Core는 프로젝트의 **공통 하위 계층**입니다.

상위 패키지들이 Core를 기반으로 동작하며, Core는 상위 패키지의 구현 세부를 직접 알지 않는 방향을 유지해야 합니다.

```text
Core
↑
Control
↑
Skill
↑
AI_BT
```

이 방향은 코드 설계뿐 아니라 문서 구조에도 그대로 반영되어야 합니다.
즉, Core 문서는 “공통 기반을 제공하는 방식”을 설명하고,
상위 패키지 문서는 “Core를 어떻게 활용하는지”를 설명하는 구조가 가장 자연스럽습니다.

---

## 3. Runtime / Editor 책임 분리

## Runtime

Runtime는 실제 게임 플레이 중 실행되는 코드입니다.

주요 책임은 다음과 같습니다.

- 캐릭터 공통 로직
- 스탯과 리소스 계산
- 피격, 상태, 모션, Crowd Control
- UI 표시 및 HUD 업데이트
- 데이터 테이블 접근
- 저장/복원
- Addressables 기반 리소스 로딩
- VFX, 사운드, Projectile, Cutscene 같은 전역 피드백 시스템
- 맵, 아이템, 퀘스트, 대화 같은 공통 게임 기능

## Editor

Editor는 개발 생산성을 높이는 도구 계층입니다.

주요 책임은 다음과 같습니다.

- 테이블 편집
- 테스트 툴
- 프로젝트 셋업 자동화
- 생성기 기반 에셋 제작 지원
- Addressables / 설정 자산 구성 보조

이 분리를 지키면 Runtime은 플레이 코드에 집중할 수 있고,
Editor는 유지보수성과 제작 파이프라인 품질을 높이는 역할을 안정적으로 수행할 수 있습니다.

---

## 4. 실제 Runtime 구조를 보는 방법

업로드된 Runtime 소스 구조를 보면 Core는 대략 아래 축으로 읽는 것이 좋습니다.

## 4-1. 게임 시작과 씬 진입 축

- `Core/`
- `Scenes/`
- `AddressableLoader/`
- `Configs/`
- `ScriptableSettings/`

이 축은 **게임 시작 시 무엇이 먼저 준비되는가**를 설명합니다.

대표 진입점은 아래 클래스입니다.

- `GameLoaderManager`
- `SceneGame`
- `AddressableLoaderController`
- `ConfigScriptableObject`

특히 `GameLoaderManager`는 각 패키지의 로딩 스텝을 등록하고 실행 순서를 조정하는 로더 허브입니다.
`SceneGame`은 게임 씬에서 카메라, 캔버스, UI 매니저, 팝업, 사운드, 맵, 캐릭터, VFX 같은 공용 객체를 묶는 씬 진입점 역할을 합니다.

## 4-2. 캐릭터와 전투 축

- `Characters/`
- `Combat/`
- `Animation/`
- `Camera/`

이 축은 **캐릭터가 실제로 어떻게 살아 움직이는가**를 설명합니다.

대표 구조는 다음과 같습니다.

- `CharacterBase`: 캐릭터 공통 기반
- `CharacterStat`: 스탯 / 리소스 중심축
- `CharacterDamageController`: 피격 처리 축
- `CharacterMotionController2D`: 이동/모션 실행 축
- `CharacterCrowdControlController`: 넉백/넉업/넉다운 같은 CC 실행 축
- `Player`, `Monster`, `Npc`: 캐릭터 종류별 구체 구현
- `PlayerUIController`, `MonsterUIController`: 캐릭터 상태를 UI로 브리지하는 계층

이 구조 덕분에 플레이어와 몬스터는 공통 캐릭터 기반 위에서 차이를 확장하는 방향으로 설계할 수 있습니다.

## 4-3. UI 축

- `UI/`
- `Popup/`
- `SystemMessage/`

이 축은 **데이터를 어떻게 화면에 보여주는가**를 담당합니다.

대표 진입점은 아래와 같습니다.

- `UIWindowManager`
- `UIWindow`
- `UIFloatingTextManager`
- `PopupManager`
- `SystemMessageManager`

실제 `UIWindowManager`는 `TableWindow` 데이터를 읽어 윈도우 정보를 초기화하고,
씬의 메인 Canvas 아래에서 각 윈도우를 정렬/제어하는 구조를 가집니다.
따라서 UI 문제를 볼 때는 단순히 개별 프리팹보다 먼저 **윈도우 매니저와 테이블 연결 구조**를 확인하는 것이 효율적입니다.

## 4-4. 데이터와 설정 축

- `TableLoader/`
- `Configs/`
- `ScriptableSettings/`
- `Localization/`

이 축은 **런타임이 어떤 데이터를 기준으로 동작하는가**를 담당합니다.

대표 구조는 다음과 같습니다.

- `TableLoaderManager`: Core 테이블 접근 허브
- `TableRegistry`: 테이블 등록 집합
- 각 `Table*`: 실제 txt 기반 데이터 로더
- `GGemCo*Settings`: ScriptableObject 기반 설정 자산

`TableLoaderManager`는 몬스터, NPC, 아이템, 윈도우, 스탯, 상태, VFX, Projectile, 퀘스트, 대사 등 매우 많은 데이터를 보관합니다.
즉, Core의 많은 시스템은 코드 하드코딩보다 **테이블과 설정 자산을 조합하는 방식**으로 움직입니다.

## 4-5. 저장/복원 축

- `SaveData/`

이 축은 **게임 상태를 보존하고 다시 불러오는 방법**을 담당합니다.

대표 구조는 다음과 같습니다.

- `SaveDataManager`
- `SaveDataManagerBase`
- `SaveRegistry`
- `ISaveContributor`
- `SaveEnvelope`
- `SaveFileController`
- `PlayerData`, `InventoryData`, `QuickSlotData` 등 실제 저장 데이터 객체

특히 `SaveRegistry`는 늦게 등록된 객체까지 복원 흐름에 합류할 수 있게 하는 핵심 장치입니다.
초기화 순서 때문에 저장 복원이 꼬이는 문제를 줄이는 데 중요한 구조입니다.

## 4-6. 피드백 / 리소스 실행 축

- `Vfx/`
- `Sound/`
- `Projectile/`
- `Cutscene/`

이 축은 **전투와 연출의 체감 품질**을 담당합니다.

대표 구조는 다음과 같습니다.

- `VfxManager`
- `SoundManager`
- `ProjectileController`
- `CutsceneManager`

이 영역은 다른 패키지와 자주 연결되지만, 실제 리소스 생성과 실행 책임은 Core에 두는 편이 일관성이 좋습니다.
특히 Projectile과 VFX는 Skill 패키지와 강하게 연결되더라도, **실행기 자체는 Core에 두고 상위 패키지는 요청만 전달하는 구조**가 유지보수에 유리합니다.

## 4-7. 게임 공통 기능 축

- `Maps/`
- `Items/`
- `Quest/`
- `Dialogue/`
- `Interaction/`
- `Currency/`

이 축은 프로젝트 전반에서 반복 사용하는 공통 도메인 기능입니다.
Core가 이미 단순 전투 패키지를 넘어 **게임 프레임워크**에 가깝다는 점을 보여주는 영역입니다.

---

## 5. 대표 런타임 흐름

Core를 처음 이해할 때는 아래 흐름으로 보면 좋습니다.

### 흐름 1: 게임 시작

1. 로딩 씬 또는 초기 씬에서 `GameLoaderManager` 준비
2. 각 패키지가 `IGameLoadStep` 구현체를 등록
3. 테이블, 로컬라이즈, 세이브, 설정, Addressables 준비
4. 게임 씬 진입 후 `SceneGame`이 공용 오브젝트를 연결

### 흐름 2: 캐릭터 생성과 플레이 시작

1. `Player` 또는 `Monster` 생성
2. `CharacterBase` / `CharacterStat` 초기화
3. 전용 컨트롤러와 UI 브리지 연결
4. 애니메이션, 피격, 모션, 상태 처리 활성화

### 흐름 3: UI 갱신

1. 캐릭터 상태나 게임 데이터가 변경됨
2. 해당 매니저 또는 Presenter가 UI 갱신 요청 수행
3. `UIWindowManager` 또는 개별 UIWindow / UIElement가 화면 반영

### 흐름 4: 저장/복원

1. 저장 기여자가 `SaveRegistry`에 등록
2. 저장 시 `SaveEnvelope`로 데이터 수집
3. 복원 시 `ApplyRestore`로 등록 객체에 일괄 적용
4. 늦게 생성된 객체도 pending restore를 통해 복원 가능

---

## 6. Core에 기능을 추가할 때의 기준

아래 기준이 중요합니다.

### Core에 넣어도 되는 것

- 여러 패키지에서 공통으로 쓰는 기능
- 플레이어/몬스터/NPC 모두에 적용되는 기반 로직
- 게임 전체가 공유하는 UI / 로딩 / 저장 / 테이블 / 전역 피드백 구조
- 상위 패키지가 호출할 수 있는 공용 서비스나 인터페이스

### Core에 넣지 않는 것이 좋은 것

- 특정 스킬 전용 로직
- 특정 AI 트리 전용 규칙
- Control / Skill / Affect / AI BT 세부 정책에 강하게 종속되는 기능
- 상위 패키지 구조를 직접 아는 분기 코드

### 권장 확장 방식

- 큰 클래스에 기능을 계속 누적하기보다 작은 서비스/핸들러/모듈로 분리
- 상위 계층 의존이 필요하면 인터페이스를 Core에 두고 구현은 상위 계층에 배치
- 테이블 / ScriptableObject / Addressables 키를 통해 데이터 중심으로 확장
- 저장/복원은 `ISaveContributor`와 `SaveRegistry` 흐름에 맞춰 추가

---

## 7. Unity 관점에서 이 구조가 가지는 의미

Core 문서를 작성할 때 Unity의 기본 설계 원칙과 맞물리는 지점을 함께 설명하는 것이 좋습니다.

- **Assembly Definition**: 패키지 코드와 Editor 코드를 분리하고 의존성을 명시적으로 관리하는 기준점입니다.
- **ScriptableObject**: 공통 설정과 패키지 전역 데이터를 에셋으로 분리해 공유하기 좋습니다.
- **Serialization**: 테이블/설정/저장 데이터 구조를 설계할 때 직렬화 규칙을 고려해야 유지보수가 쉬워집니다.
- **Addressables**: 프리팹, VFX, 사운드, 설정 자산을 느슨하게 연결하고 로딩 정책을 표준화하는 데 적합합니다.

따라서 Core 문서는 단순히 클래스 설명에 머물지 않고,
**왜 이런 구조가 Unity 프로젝트에서 유효한지**까지 함께 설명하는 편이 장기적으로 더 가치가 있습니다.

---

## 8. 문서화 우선순위 제안

아키텍처 문서를 읽은 뒤에는 다음 순서로 상세 문서를 만드는 것이 좋습니다.

### Runtime 1순위

- `CharacterStat`
- `CharacterBase`
- `CharacterMotionController2D`
- `CharacterCrowdControlController`
- `TableLoaderManager`
- `GameLoaderManager`
- `UIWindowManager`
- `VfxManager`
- `ProjectileController`
- `SaveRegistry`
- `SaveDataManagerBase`

### Runtime 2순위

- `PlayerUIController`
- `CharacterDamageController`
- `SceneGame`
- `PopupManager`
- `SystemMessageManager`
- `CutsceneManager`
- `MapManager`
- `QuestManager`
- `ItemManager`

---

## 9. 한 줄 요약

Core 패키지는 **게임 시작과 로딩, 캐릭터 기반 로직, UI, 데이터 테이블, 저장/복원, 피드백 시스템을 묶는 공통 런타임 프레임워크**로 이해하는 것이 가장 적절합니다.
