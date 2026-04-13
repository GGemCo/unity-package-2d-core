# UIWindowManager

## 1. 문서 목적

이 문서는 `UIWindowManager`를 설명합니다.
게임 내 UGUI 기반 윈도우들을 어떻게 초기화하고, UID로 찾아서 표시/숨김 제어하는지 이해하는 것이 목적입니다.

---

## 2. 역할

`UIWindowManager`는 **UI 윈도우 전역 관리자**입니다.
개별 윈도우가 자신의 표시 로직을 갖고 있더라도, 윈도우 목록과 테이블 정보를 묶어 관리하는 상위 허브가 필요하며 그 역할을 이 클래스가 맡습니다.

코드상 핵심 책임은 다음과 같습니다.

- 윈도우 프리팹 배열 관리
- Window 테이블과 실제 윈도우 연결
- 오버/선택용 아이콘 시각 요소 생성
- UID 기반 윈도우 조회
- 표시/숨김 제어
- 현재 윈도우 가시성 상태 캡처/복원
- 아이콘 제거 같은 공용 보조 기능 제공

---

## 3. 왜 중요한가

UI 버그는 개별 `UIWindow`만 봐서는 원인이 안 나오는 경우가 많습니다.
특히 아래 상황에서 `UIWindowManager`를 먼저 봐야 합니다.

- 윈도우가 생성은 되어 있는데 관리되지 않는다
- 특정 WindowUid가 null을 반환한다
- UseInGame 설정이 적용되지 않는다
- 일괄 숨김/복원이 제대로 되지 않는다
- 씬 시작 시 윈도우 순서가 틀린다

이 클래스는 Window 테이블과 실제 프리팹 인스턴스를 연결하는 기준점이기 때문입니다.

---

## 4. 핵심 상태

### 오버/선택 시각 요소
- `prefabIconOver`
- `prefabIconSelected`
- `_imageIconOver`
- `_imageIconSelected`

윈도우 혹은 아이콘 선택/오버 상태를 공통 이미지로 표시하는 구조입니다.

### 관리 대상 윈도우
- `uiWindows`

실제 관리 대상 윈도우 배열입니다.
UID 기반 접근을 위한 핵심 저장소입니다.

### 테이블 캐시
- `_struckTableWindows`

`TableWindow`에서 읽은 정보를 UID별로 캐싱합니다.
실제 사용 시점에는 이 캐시가 “관리 대상인가”를 판별하는 기준이 됩니다.

---

## 5. 주요 진입 메서드

### `SetUIWindow(UIWindow[] prefabs)`
외부에서 관리 대상 윈도우 배열을 주입할 수 있습니다.

### `InitializationTableInfo()`
초기화 핵심 메서드입니다.

이 메서드는 대략 다음 일을 합니다.

1. `TableLoaderManager.Instance.TableWindow`를 가져옵니다.
2. 테이블 데이터를 Ordering 기준으로 정렬합니다.
3. 각 UID에 대응하는 실제 `UIWindow`를 찾습니다.
4. `UseInGame` 여부를 적용합니다.
5. `SetTableWindow(info)`로 테이블 정보를 연결합니다.
6. sibling index를 정렬 순서에 맞게 설정합니다.

### `ShowWindow(UIWindowConstants.WindowUid uid, bool show)`
UID 기준으로 윈도우를 열고 닫는 대표 메서드입니다.
외부 시스템은 직접 GameObject를 건드리기보다 이 메서드를 경유하는 편이 좋습니다.

### `GetUIWindowByUid<T>(UIWindowConstants.WindowUid windowUid)`
UID 기준으로 관리 중인 윈도우를 찾아 반환합니다.
관리 대상이 아니거나 `UseInGame`이 꺼져 있으면 null을 반환합니다.

### `RemoveIcon(...)`
특정 윈도우 슬롯에서 아이콘을 제거합니다.
드래그 앤 드롭이나 장착 UI와 연결될 가능성이 큽니다.

### `CaptureVisibilityState(...)` / `RestoreVisibilityState(...)`
현재 윈도우 표시 상태를 저장/복원합니다.
일시적으로 UI를 닫았다가 되돌리는 연출이나 상태 전환에 유용합니다.

### `SetWindowsVisible(...)`
복수 윈도우를 일괄 표시/숨김 처리합니다.

---

## 6. 연결해서 봐야 하는 클래스

### 개별 윈도우 계층
- `UIWindow`
- `UIWindowBase`
- 각 개별 UI 윈도우 구현체

### 데이터 계층
- `TableLoaderManager`
- `TableWindow`
- `StruckTableWindow`

### 아이콘 계층
- `UIIcon`
- 아이콘 풀/드래그 앤 드롭 관련 클래스

### 씬 연결
- `SceneGame`
- `canvasUI`

---

## 7. 대표 런타임 흐름

### 흐름 A: 씬 시작 시 초기화
1. `Awake()`에서 테이블 캐시를 비웁니다.
2. `InitializationTableInfo()`가 실행됩니다.
3. Window 테이블과 실제 윈도우 배열을 연결합니다.
4. `Start()`에서 오버/선택 이미지를 만듭니다.

### 흐름 B: 윈도우 표시 요청
1. 다른 시스템이 `WindowUid`로 열기/닫기를 요청합니다.
2. `ShowWindow()`가 관리 중인 윈도우를 찾습니다.
3. 실제 `UIWindow.Show(show)`를 호출합니다.

### 흐름 C: 상태 보존 후 복원
1. 특정 상황에서 윈도우 가시성 상태를 캡처합니다.
2. UI를 일괄로 닫거나 전환합니다.
3. 필요 시 저장한 상태를 복원합니다.

---

## 8. 확장 포인트

### 새 윈도우를 추가할 때
- `TableWindow`에 UID와 표시 설정을 추가합니다.
- `uiWindows` 배열에 실제 윈도우를 연결합니다.
- `UseInGame`, `Ordering`을 적절히 설정합니다.

### 윈도우 표시 정책을 바꿀 때
개별 윈도우 안에 분산 구현하기보다, 공통 표시/복원 정책은 `UIWindowManager`에서 다루는 편이 좋습니다.

### 선택/오버 연출을 바꿀 때
공통 시각 피드백은 `_imageIconOver`, `_imageIconSelected` 계층을 먼저 검토하는 것이 좋습니다.

---

## 9. 디버깅 체크리스트

### `GetUIWindowByUid()`가 null을 반환하는 경우
- `uiWindows` 배열 길이가 UID를 커버하는지 확인합니다.
- `TableWindow`에 해당 UID가 있는지 확인합니다.
- `UseInGame`이 꺼져 있지 않은지 확인합니다.

### 윈도우 순서가 틀린 경우
- `Ordering` 값이 올바른지 확인합니다.
- sibling index 적용이 예상대로 되었는지 확인합니다.

### 특정 윈도우만 안 보이는 경우
- 관리 대상 배열에 프리팹이 빠졌는지 확인합니다.
- `SetTableWindow(info)`가 호출되는지 확인합니다.
- `Show(false)` 상태로 시작하는 의도인지 확인합니다.

### 상태 복원이 안 되는 경우
- `CaptureVisibilityState()` 호출 시점에 올바른 UID 목록을 넘겼는지 확인합니다.
- 복원 대상이 현재 관리 중인 윈도우인지 확인합니다.

---

## 10. 한 줄 정리

`UIWindowManager`는 **WindowUid와 Window 테이블을 기준으로 게임 내 주요 UI 윈도우를 초기화·조회·표시·복원하는 UGUI 전역 관리 허브**입니다.
