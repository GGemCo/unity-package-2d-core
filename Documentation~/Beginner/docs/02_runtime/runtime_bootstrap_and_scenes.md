# Runtime 기능 영역 문서 - 부트스트랩과 씬 진입

## 1. 문서 목적

이 문서는 Core Runtime에서 **게임이 시작되고 실제 플레이 씬으로 진입하기까지의 공통 흐름**을 설명합니다.
처음 보는 개발자가 아래 질문에 답할 수 있도록 구성했습니다.

- 게임 시작 시 어떤 클래스가 먼저 동작하는가
- 로딩 단계는 어디에서 등록되고 실행되는가
- 씬 진입 후 공용 매니저는 어디에서 연결되는가
- 기능을 추가할 때 로딩 단계와 씬 책임을 어디에 두어야 하는가

---

## 2. 이 영역에 포함되는 주요 폴더

- `Core/`
- `Core/Loader/`
- `Scenes/`

관련해서 자주 함께 보게 되는 폴더:
- `AddressableLoader/`
- `TableLoader/`
- `SaveData/`
- `ScriptableSettings/`

즉, 이 영역은 다른 시스템을 직접 구현하는 곳이라기보다 **다른 시스템을 올바른 순서로 준비시키는 진입점**입니다.

---

## 3. 대표 클래스

### 게임 시작 흐름
- `Core/GameLoaderManager.cs`
- `Core/Loader/IGameLoadStep.cs`
- `Core/Loader/GameLoadStep.cs`
- `Core/Loader/GameLoadStepBase.cs`
- `Core/Loader/TableLoadStep.cs`
- `Core/Loader/LocalizationLoadStep.cs`
- `Core/Loader/SaveDataLoadStep.cs`

### 씬 진입 흐름
- `Scenes/ScenePreIntro.cs`
- `Scenes/SceneLoading.cs`
- `Scenes/SceneIntro.cs`
- `Scenes/SceneGame.cs`
- `Core/SceneManager.cs`

### 보조 공용 클래스
- `Core/GameTimeManager.cs`
- `Core/PlayerPrefsManager.cs`
- `Core/UnityMainThreadDispatcher.cs`

---

## 4. 이 영역의 핵심 책임

## 4-1. 로딩 순서 표준화

`GameLoaderManager`와 `IGameLoadStep` 계층은 테이블, 로컬라이즈, 세이브, 설정, Addressables 같은 공용 초기화 단계를 **일관된 순서**로 실행하기 위한 구조입니다.

이 구조의 핵심 목적은 다음과 같습니다.

- 각 패키지가 필요한 초기화 단계를 독립적으로 등록할 수 있게 한다.
- 진행률 표시와 완료 시점을 중앙에서 관리한다.
- 초기화 순서가 어긋나서 생기는 버그를 줄인다.

## 4-2. 씬별 역할 분리

`ScenePreIntro`, `SceneLoading`, `SceneIntro`, `SceneGame`은 이름 그대로 동일한 책임을 갖지 않습니다.

- **PreIntro**: 게임 시작 직후의 준비 단계
- **Loading**: 실제 초기화 진행과 대기 UI 단계
- **Intro**: 타이틀/시작 화면 성격의 단계
- **Game**: 실제 플레이 시스템이 연결되는 메인 단계

따라서 “게임이 잘 안 뜬다”는 문제는 단순히 `SceneGame` 하나만 보는 것이 아니라, **어느 씬에서 어느 준비가 끝나야 다음 단계로 넘어가는가**를 같이 봐야 합니다.

## 4-3. 전역 공용 객체 연결

`SceneGame`은 실제 플레이 씬의 허브 역할을 합니다.
카메라, UI, 팝업, 시스템 메시지, 데미지 텍스트, 맵, 캐릭터, VFX 같은 공용 오브젝트 참조가 이 축에 모일 가능성이 높습니다.

즉, 이 영역은 “게임 로직”보다 **게임 로직이 붙을 무대**를 준비합니다.

---

## 5. 추천 읽기 순서

1. `GameLoaderManager`
2. `IGameLoadStep`, `GameLoadStepBase`
3. `TableLoadStep`, `LocalizationLoadStep`, `SaveDataLoadStep`
4. `SceneManager`
5. `ScenePreIntro`, `SceneLoading`, `SceneIntro`
6. `SceneGame`
7. `GameTimeManager`, `UnityMainThreadDispatcher`

이 순서로 보면 “무엇을 로드하는가”보다 먼저 **언제, 어떤 순서로, 누가 연결하는가**를 이해할 수 있습니다.

---

## 6. 대표 런타임 흐름

### 흐름 A: 게임 시작

1. 시작 씬에서 `GameLoaderManager`가 준비됩니다.
2. 각 시스템이 `IGameLoadStep` 형태로 로딩 작업을 등록합니다.
3. 테이블, 로컬라이즈, 세이브, 설정 같은 공통 데이터가 순차적으로 준비됩니다.
4. 로딩이 끝나면 게임 진입에 필요한 씬 전환이 진행됩니다.

### 흐름 B: 플레이 씬 진입

1. `SceneGame`이 씬 기준 공용 오브젝트를 연결합니다.
2. UI, 카메라, 맵, 캐릭터, 팝업, 피드백 시스템이 준비됩니다.
3. 다른 패키지(Control / Skill / Affect / AI BT)가 Core 진입점을 기준으로 자신의 연결을 올립니다.

---

## 7. 기능 추가 시 배치 기준

## 이 영역에 넣는 것이 맞는 경우

- 초기화 순서가 중요한 기능
- 로딩 단계 등록이 필요한 기능
- 특정 씬에서만 존재해야 하는 공용 허브
- 다른 시스템 여러 개를 연결만 하는 조정자 클래스

## 이 영역에 넣지 않는 것이 좋은 경우

- 캐릭터 개별 로직
- UI 값 계산 자체
- 아이템/퀘스트의 세부 규칙
- VFX/사운드 재생의 구체 구현

즉, 이 영역은 **오케스트레이션**에 집중해야 하며, 세부 도메인 로직은 각 도메인 폴더에 남기는 편이 좋습니다.

---

## 8. 디버깅 포인트

### 로딩이 끝나지 않는 경우
- `GameLoaderManager`에 등록된 스텝 수와 완료 수를 확인합니다.
- 특정 `GameLoadStep`이 예외 없이 종료되는지 확인합니다.
- Addressables, 테이블, 세이브 중 어느 단계에서 멈추는지 로그를 분리해서 봅니다.

### 게임 씬에 들어왔는데 일부 시스템이 비정상인 경우
- `SceneGame`에서 참조하는 공용 객체가 모두 연결되었는지 확인합니다.
- 해당 시스템이 로딩 단계에서 준비되어야 하는 자산을 놓치지 않았는지 확인합니다.
- 씬 진입 전에 필요한 설정 ScriptableObject가 로드되었는지 확인합니다.

### 씬 전환 후 상태가 꼬이는 경우
- `SceneManager`를 통해 전환되는 흐름과 직접 로드되는 흐름이 섞여 있지 않은지 확인합니다.
- 씬 기반 싱글톤이나 정적 캐시가 정리되는 시점을 점검합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **게임을 시작시키고, 필요한 시스템을 순서대로 준비하고, 플레이 씬에서 공용 객체를 묶는 진입점 계층**으로 이해하면 가장 정확합니다.
