# GameLoaderManager

## 1. 문서 목적

이 문서는 `GameLoaderManager`를 설명합니다.
게임 시작 시 여러 패키지의 로딩 단계를 어떤 규칙으로 등록하고 실행하는지 이해하는 것이 목적입니다.

---

## 2. 역할

`GameLoaderManager`는 **모듈식 로딩 시퀀스 관리자**입니다.
각 패키지는 `IGameLoadStep` 구현체를 등록하고, `GameLoaderManager`는 그것을 정렬된 순서대로 실행합니다.

코드 주석에 드러나는 핵심 책임은 다음과 같습니다.

- 로딩 Step 등록
- Step 중복 방지
- 로딩 순서 정렬
- 진행률 집계 및 UI 반영
- 전체 로딩 완료 처리
- SceneLoading 씬에서 필요한 기본 로더 생성/연결

즉, 이 클래스는 Core와 상위 패키지들이 함께 쓰는 “로딩 오케스트레이터”입니다.

---

## 3. 왜 중요한가

프로젝트 전체 초기화 순서를 추적할 때 가장 먼저 봐야 하는 클래스 중 하나입니다.
특히 아래 문제가 생기면 거의 항상 이 클래스를 확인해야 합니다.

- 특정 패키지 로더가 아예 실행되지 않는다
- 로딩 완료 전에 다른 시스템이 먼저 접근한다
- 로딩 퍼센트 표시가 이상하다
- Loading 씬과 Game 씬의 연결 타이밍이 맞지 않는다

GameLoader를 이해하면 Addressables, Localization, TableLoader, SaveData 초기화 흐름도 자연스럽게 따라갈 수 있습니다.

---

## 4. 핵심 상태

### 싱글톤
- `Instance`

씬을 넘나드는 로딩 관리자 역할을 하므로 `DontDestroyOnLoad`로 유지됩니다.

### 이벤트
- `BeforeLoadStart`
- `BeforeLoadStartInLoadingScene`

로딩 시작 직전에 외부 패키지가 개입할 수 있는 훅입니다.
특정 패키지가 로딩 단계 주입을 늦게 하거나, Loading 씬에서만 별도 처리를 해야 할 때 중요합니다.

### 진행률 관련
- `_steps`
- `_stepProgress`
- `_progressBasePerStep`
- `_progressTotal`
- `_textLoadingPercent`

단순 총합 퍼센트가 아니라, Step별 진행률을 집계해 전체 퍼센트로 환산합니다.

### 로딩 상태
- `_isLoadComplete`
- `_isStarted`

중복 시작, 로딩 중 재등록 같은 오류를 막는 핵심 상태입니다.

---

## 5. 주요 진입 메서드

### `Register(IGameLoadStep step)`
외부 패키지가 로딩 Step을 등록하는 공식 진입점입니다.

이 메서드의 중요한 의미는 다음과 같습니다.

- 로딩 시작 전까지만 등록 가능
- `Id` 중복 금지
- 패키지별 독립 Step 주입 가능

즉, Core가 모든 패키지 로딩을 직접 알 필요 없이, 패키지가 Step을 추가하는 구조입니다.

### `StartLoading(IEnumerable<string> allowedIds = null)`
실제 로딩 시작 메서드입니다.

대략 다음 순서로 동작합니다.

1. 시작 중복 방지
2. 시작 전 이벤트 호출
3. 대상 Step 필터링 및 Order 정렬
4. 진행률 초기화
5. 코루틴 시작

### `LoadSequenceCoroutine(List<IGameLoadStep> steps)`
각 Step의 `Run()` 코루틴을 순서대로 실행합니다.
중간중간 진행률을 갱신하고, 마지막에 완료 보정을 수행합니다.

### `UpdateLoadingProgress(IGameLoadStep step, bool forceComplete = false)`
Step 진행률을 전체 퍼센트로 환산하고, 필요하면 로컬라이즈된 로딩 문구를 UI에 반영합니다.

### `OnLoadComplete()`
로딩 종료 후 내부 상태를 정리합니다.
다음 로딩 시퀀스를 위해 Step 목록과 진행률 캐시를 비웁니다.

### `StartLoadingInSceneLoading()`
Loading 씬 기준 기본 로더들을 확보하고 Core 기본 Step들을 등록합니다.
이 메서드는 프로젝트의 실제 초기화 진입점 성격이 강합니다.

---

## 6. 연결해서 봐야 하는 클래스

### Step 규약
- `IGameLoadStep`
- `GameLoadStepBase`
- `TableLoadStep`
- `LocalizationLoadStep`
- `AddressableTaskStep`

### 로더와 매니저
- `TableLoaderManager`
- `LocalizationManager`
- `SaveDataLoader`
- `AddressableLoaderPrefabCommon`
- `AddressableLoaderPrefabVfx`
- `AddressableLoaderItem`
- `AddressableLoaderSound`

### 씬 연결
- `SceneGame`
- Loading 씬 관련 클래스

---

## 7. 대표 런타임 흐름

### 흐름 A: Loading 씬 진입
1. `GameLoaderManager`가 준비됩니다.
2. `StartLoadingInSceneLoading()`이 호출됩니다.
3. 필요한 로더가 씬에서 검색되거나 생성됩니다.
4. Core 기본 Step이 등록됩니다.
5. 상위 패키지도 추가 Step을 등록합니다.

### 흐름 B: 실제 로딩
1. `StartLoading()`이 호출됩니다.
2. `BeforeLoadStart` 이벤트가 실행됩니다.
3. Step이 Order 기준으로 정렬됩니다.
4. Step별 `Run()`이 순서대로 실행됩니다.
5. 진행률이 UI에 반영됩니다.
6. 완료 후 내부 상태가 정리됩니다.

### 흐름 C: 부분 로딩
1. `allowedIds`가 전달됩니다.
2. 지정된 Step만 골라 실행합니다.
3. 전체 재로딩이 아닌 부분 로딩 흐름으로 활용할 수 있습니다.

---

## 8. 확장 포인트

### 새 패키지 로더를 붙일 때
기존 로더 클래스에 억지로 코드를 추가하기보다, 새 `IGameLoadStep` 구현을 만들고 등록하는 방식이 가장 깔끔합니다.

### 로딩 UI를 바꿀 때
진행률 집계 로직과 표시 로직을 분리해서 보는 편이 좋습니다.
현재 구조에서는 `_textLoadingPercent`가 최소 연결 지점입니다.

### 로딩 순서를 세밀하게 조정할 때
Step의 `Order`와 `Id`를 통해 제어하는 편이 좋습니다.
패키지 간 직접 참조로 순서를 고정하면 결합이 커집니다.

---

## 9. 디버깅 체크리스트

### 어떤 로더가 실행되지 않는 경우
- `Register()`가 로딩 시작 전에 호출되었는지 확인합니다.
- Step `Id`가 중복되어 무시되지 않았는지 확인합니다.

### 로딩이 끝나지 않는 경우
- 특정 Step의 `Run()`이 종료되지 않는지 확인합니다.
- `GetProgress()`가 1까지 도달하는지만 보지 말고 코루틴 종료 자체를 확인합니다.

### 퍼센트가 이상한 경우
- Step 수가 예상과 다른지 확인합니다.
- `InitializeProgress()`가 올바르게 호출되었는지 확인합니다.
- 특정 Step 진행률이 갱신되지 않는지 확인합니다.

### Loading 씬에서는 되는데 Game 씬에서는 안 되는 경우
- `StartLoadingInSceneLoading()`이 실제로 호출되었는지 확인합니다.
- 필요한 로더가 `CompatObjectFind`로 확보되는지 확인합니다.

---

## 10. 한 줄 정리

`GameLoaderManager`는 **패키지별 로딩 Step을 등록·정렬·실행·집계하는 Core의 공용 초기화 오케스트레이터**입니다.
