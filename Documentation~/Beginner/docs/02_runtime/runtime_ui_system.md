# Runtime 기능 영역 문서 - UI 시스템

## 1. 문서 목적

이 문서는 Core Runtime의 **UGUI 기반 UI 시스템**이 어떤 구조로 움직이는지 설명합니다.
특히 윈도우 관리, HUD 연결, 아이콘/슬롯, 드래그 앤 드롭, 플로팅 텍스트, 팝업과 시스템 메시지의 책임 구분을 정리하는 데 목적이 있습니다.

---

## 2. 이 영역에 포함되는 주요 폴더

- `UI/`
- `Popup/`
- `SystemMessage/`

세부적으로는 다음 하위 구조가 중요합니다.

- `UI/Core/Base/` : 윈도우 기반 구조
- `UI/Core/Icon/` : 슬롯/아이콘/쿨타임 구조
- `UI/Core/DragDrop/` : 드래그 앤 드롭 구조
- `UI/Core/Effects/` : UI 이펙트 구조
- `UI/Core/FloatingText/` : 전투 피드백 텍스트 구조
- `UI/Windows/` : 실제 화면별 윈도우
- `UI/Elements/` : 비교적 작은 표시 단위

---

## 3. 대표 클래스

### 윈도우 기반 구조
- `UI/Core/Base/UIWindowManager.cs`
- `UI/Core/Base/UIWindow.cs`
- `UI/Core/Base/UIWindowBase.cs`

### 공통 UI 보조 기능
- `UI/Core/Icon/UIIcon.cs`
- `UI/Core/Icon/UISlot.cs`
- `UI/Core/Icon/IconPoolManager.cs`
- `UI/Core/Icon/UICoolTimeHandler.cs`
- `UI/Core/DragDrop/*`
- `UI/Core/Effects/UIEffectService.cs`

### 전투 피드백
- `UI/Core/FloatingText/UIFloatingTextManager.cs`
- `Popup/PopupManager.cs`
- `SystemMessage/SystemMessageManager.cs`

### 게임 상태와 UI 연결
- `Characters/Player/PlayerUIController.cs`
- `UI/Windows/WindowHud/*`
- `UI/Windows/Common/UIWindowPlayerBuffInfo.cs`
- `UI/AffectUI/PlayerAffectUiPresenter.cs`

---

## 4. 이 영역의 핵심 책임

## 4-1. 윈도우의 생명주기 관리

`UIWindowManager`는 Runtime UI 구조의 중심입니다.
개별 화면은 `UIWindow` 계층으로 만들어지더라도, 실제 열기/닫기/정렬/초기화 기준은 `UIWindowManager`를 통해 통일하는 편이 유지보수에 유리합니다.

이 구조를 유지하면 아래 장점이 있습니다.

- 개별 윈도우가 서로를 직접 생성하지 않아도 된다.
- UI 공통 규칙을 한 곳에서 맞출 수 있다.
- 씬 진입과 윈도우 로딩 관계를 추적하기 쉽다.

## 4-2. 표시와 계산의 역할 분리

UI는 값을 계산하는 곳이 아니라, 보통 **이미 계산된 상태를 표시하는 곳**이어야 합니다.
이 프로젝트 구조에서는 `PlayerUIController`, 각 Presenter, HUD 브리지 클래스가 그 중간 역할을 담당합니다.

즉, 다음 구분이 중요합니다.

- 값 계산: `CharacterStat`, 게임 로직 계층
- UI 연결: `PlayerUIController`, Presenter, Receiver 인터페이스
- 화면 표시: `UIWindow*`, `UIElement*`, `UISlider*`

이 원칙이 흔들리면 UI 윈도우 안에 게임 로직이 쌓이기 쉽습니다.

## 4-3. 아이콘/슬롯/드래그 앤 드롭 표준화

Core UI는 단순 이미지 표시보다 **슬롯 중심 UI** 비중이 큽니다.
장비창, 인벤토리, 퀵슬롯, 상점, 버프 아이콘 같은 기능은 공통 슬롯/아이콘 구조 위에서 움직이는 편이 좋습니다.

이때 중요한 구조가 다음입니다.

- `UIIcon`
- `UISlot`
- `ISetIconHandler`
- `IDragDropStrategy`
- `IconPoolManager`

이 구조를 이해하면 개별 윈도우 구현을 매번 처음부터 만들지 않고, 공통 규칙을 재사용할 수 있습니다.

## 4-4. 플레이어 피드백 계층 분리

즉시 피드백은 크게 세 종류로 나눠서 보는 것이 좋습니다.

- **HUD 변화**: HP/MP/Stamina/버프 상태
- **짧은 피드백**: 플로팅 텍스트, 쿨타임, 흔들림, UI 이펙트
- **메시지 계층**: 팝업, 시스템 메시지

이 세 가지를 한 클래스에 몰아넣기보다, `UIFloatingTextManager`, `PopupManager`, `SystemMessageManager`, `UIEffectService`처럼 나누어 두는 구조가 좋습니다.

---

## 5. 대표 런타임 흐름

### 흐름 A: 씬 진입 후 UI 준비

1. `SceneGame`이 UI 관련 공용 객체를 준비합니다.
2. `UIWindowManager`가 윈도우 생성과 정렬 구조를 관리합니다.
3. 플레이어가 생성되면 `PlayerUIController`가 HUD 연결을 시작합니다.
4. 필요 시 버프 UI, 퀵슬롯 UI, 팝업 UI가 개별적으로 활성화됩니다.

### 흐름 B: 플레이어 상태 갱신

1. 리소스 값이나 상태 값이 변경됩니다.
2. `PlayerUIController`나 Presenter가 변경 내용을 수신합니다.
3. 해당 HUD 윈도우/슬라이더/아이콘이 표시를 갱신합니다.
4. 필요하면 UI 이펙트나 플로팅 텍스트가 함께 실행됩니다.

### 흐름 C: 인벤토리/슬롯 상호작용

1. 슬롯에 아이콘이 빌드됩니다.
2. 사용자가 드래그/드롭 또는 클릭을 수행합니다.
3. `IDragDropStrategy`와 `ISetIconHandler`가 동작을 결정합니다.
4. 결과가 실제 게임 데이터나 UI 상태에 반영됩니다.

---

## 6. 추천 읽기 순서

1. `UIWindowManager`
2. `UIWindow`, `UIWindowBase`
3. `PlayerUIController`
4. `UIFloatingTextManager`
5. `PopupManager`, `SystemMessageManager`
6. `UIIcon`, `UISlot`, `IconPoolManager`
7. `IDragDropStrategy`, `UIClickDragHandler`, `UIDragHandler`
8. `UIEffectService`
9. 관심 있는 `UI/Windows/*`

---

## 7. 기능 추가 시 배치 기준

## 이 영역에 넣는 것이 맞는 경우
- 윈도우 열기/닫기 규칙
- HUD 표시와 UI 값 반영
- 슬롯/아이콘/쿨타임/드래그 앤 드롭 규칙
- 플로팅 텍스트, 팝업, 시스템 메시지
- UI 전용 시각 효과

## 다른 계층에 두는 것이 좋은 경우
- 실제 전투 계산
- 아이템 사용 결과 계산
- 퀘스트 완료 조건 계산
- 스킬 사용 가능 여부 계산 자체

UI는 결과를 보여주는 계층으로 유지할수록 코드가 읽기 쉬워집니다.

---

## 8. 디버깅 포인트

### UI가 열리지 않는 경우
- `UIWindowManager`에 해당 윈도우가 등록되었는지 확인합니다.
- 윈도우 테이블 또는 프리팹 참조가 올바른지 확인합니다.
- 씬에서 UI 루트 캔버스가 준비되었는지 확인합니다.

### 값은 바뀌는데 HUD가 갱신되지 않는 경우
- `PlayerUIController` 또는 Presenter의 구독이 연결되어 있는지 확인합니다.
- UI 표시 함수가 초기화 전에 호출되는지 확인합니다.
- 값 계산 쪽 문제인지, 표시 쪽 문제인지 분리해서 확인합니다.

### 드래그 앤 드롭이 이상한 경우
- `IDragDropStrategy` 선택이 올바른지 확인합니다.
- 아이콘 풀 재사용 중 이전 상태가 남아 있지 않은지 확인합니다.
- 슬롯 데이터와 실제 표시 데이터가 분리되어 유지되는지 확인합니다.

### 시스템 메시지가 중복되거나 누락되는 경우
- `PopupManager`와 `SystemMessageManager`의 용도가 섞여 있지 않은지 확인합니다.
- 한 이벤트에서 같은 메시지를 여러 계층이 동시에 띄우고 있지 않은지 확인합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **게임 상태를 화면에 일관된 규칙으로 보여주고, 윈도우와 슬롯 중심 UI를 관리하는 표시 계층**으로 이해하면 됩니다.
