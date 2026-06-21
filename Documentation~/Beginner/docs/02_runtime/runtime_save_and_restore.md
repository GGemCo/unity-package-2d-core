# Runtime 기능 영역 문서 - 저장과 복원

## 1. 문서 목적

이 문서는 Core Runtime의 **세이브/로드 구조와 복원 흐름**을 설명합니다.
게임 상태가 저장 파일에 어떻게 모이고, 로딩 시 어떤 기준으로 각 시스템에 다시 적용되는지 이해하는 것이 목적입니다.

---

## 2. 이 영역에 포함되는 주요 폴더

- `SaveData/`
- `SaveData/Base/`
- `SaveData/Data/`
- `SaveData/Support/`

관련해서 함께 보는 폴더:
- `Characters/Player/`
- `Items/`
- `Quest/`
- `UI/`
- `Core/Loader/`

---

## 3. 대표 클래스

### 저장 매니저 계층
- `SaveData/Base/SaveDataManagerBase.cs`
- `SaveData/SaveDataManager.cs`
- `SaveData/Base/SaveDataLoaderBase.cs`
- `SaveData/SaveDataLoader.cs`

### 저장 파일 구조
- `SaveData/Support/SaveEnvelope.cs`
- `SaveData/Support/SaveFileController.cs`
- `SaveData/Support/SlotMetaDatController.cs`
- `SaveData/Support/ThumbnailController.cs`

### 저장 참여 구조
- `SaveData/Support/ISaveContributor.cs`
- `SaveData/Support/SaveRegistry.cs`

### 실제 저장 데이터
- `SaveData/Data/PlayerData.cs`
- `SaveData/Data/InventoryData.cs`
- `SaveData/Data/EquipData.cs`
- `SaveData/Data/QuickSlotData.cs`
- `SaveData/Data/GameTimeData.cs`
- `SaveData/Data/StashData.cs`
- 기타 `SaveData/Data/*`

---

## 4. 이 영역의 핵심 책임

## 4-1. 저장 책임의 중앙화

`SaveDataManagerBase`와 `SaveDataManager`는 세이브 시스템의 중심입니다.
개별 시스템이 파일을 직접 만지는 대신, 매니저를 통해 저장 요청과 로드 요청을 모으는 구조가 더 안전합니다.

이 구조의 장점은 다음과 같습니다.

- 슬롯 관리와 파일 경로가 일관된다.
- 저장 시점을 중앙에서 통제할 수 있다.
- 저장 형식을 한 번에 관리할 수 있다.

## 4-2. 저장 데이터와 런타임 객체의 분리

저장 구조는 보통 아래 두 층을 분리해서 보는 것이 좋습니다.

- **저장 데이터 객체**: 직렬화 가능한 순수 데이터
- **런타임 객체**: 실제 씬과 오브젝트를 가진 동작 객체

이 분리가 잘 되어 있어야 세이브 포맷이 안정적이고, 씬 구조가 바뀌어도 저장 파일 호환성을 유지하기 쉽습니다.

## 4-3. 늦게 생성된 객체까지 복원 가능하게 만들기

`SaveRegistry`는 Core 저장 구조에서 매우 중요한 포인트입니다.
씬 로딩 순서상 어떤 객체는 저장 파일을 읽은 뒤 나중에 생성될 수 있습니다.
이때도 복원에서 빠지지 않게 하려면, 저장 참여자 등록과 지연 복원 구조가 필요합니다.

즉, `SaveRegistry`는 단순 리스트가 아니라 **초기화 순서 문제를 줄이는 장치**로 이해해야 합니다.

---

## 5. 대표 런타임 흐름

### 흐름 A: 저장

1. 저장 시점이 결정됩니다.
2. `SaveDataManager`가 저장 참여자 또는 데이터 매니저를 모읍니다.
3. 각 시스템이 현재 상태를 `Data/*` 객체로 내보냅니다.
4. `SaveEnvelope`에 묶어 파일로 기록합니다.

### 흐름 B: 로드

1. 슬롯 또는 파일이 선택됩니다.
2. 저장 파일을 읽어 `SaveEnvelope`를 복원합니다.
3. `SaveDataManager`와 `SaveRegistry`가 각 시스템에 데이터를 전달합니다.
4. 이미 살아 있는 객체는 즉시 복원하고, 늦게 생성되는 객체는 등록 시점에 후속 복원을 수행합니다.

---

## 6. 추천 읽기 순서

1. `SaveDataManagerBase`
2. `SaveDataManager`
3. `SaveEnvelope`
4. `ISaveContributor`
5. `SaveRegistry`
6. `SaveFileController`
7. `PlayerData`, `InventoryData`, `EquipData`
8. `SaveDataLoader`

---

## 7. 새 저장 항목을 추가할 때의 기준

새로운 시스템을 저장 대상에 포함하려면 보통 아래 흐름을 따르는 것이 좋습니다.

1. 저장할 순수 데이터 객체를 만든다.
2. 런타임 시스템에서 그 데이터를 내보내고 다시 적용하는 경로를 만든다.
3. `ISaveContributor` 또는 매니저 계층에 참여시킨다.
4. 로드 시 객체 생성 순서에 영향을 받는다면 `SaveRegistry` 기반 복원까지 고려한다.

핵심은 “씬 오브젝트를 저장한다”가 아니라 **현재 상태를 저장용 데이터로 바꾼다**는 관점입니다.

---

## 8. 디버깅 포인트

### 저장은 되는데 다시 로드하면 값이 사라지는 경우
- 해당 상태가 실제 `Data/*` 객체에 포함되었는지 확인합니다.
- 저장 시점에는 값이 있었는지, 로드 후 적용 시점에서 덮어써지는지 구분합니다.

### 로드 직후 일부 시스템만 초기값으로 돌아가는 경우
- 해당 객체가 로드 시점보다 늦게 생성되는지 확인합니다.
- `SaveRegistry` 기반 늦은 복원이 제대로 작동하는지 확인합니다.
- 복원 후 초기화 코드가 다시 값을 덮어쓰고 있지 않은지 점검합니다.

### 슬롯/파일이 꼬이는 경우
- `SaveFileController`와 슬롯 메타 관리가 같은 기준 경로를 쓰는지 확인합니다.
- 썸네일/메타데이터와 실제 저장 파일이 같은 슬롯 ID를 바라보는지 확인합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **게임 상태를 파일로 안전하게 내보내고, 초기화 순서가 달라도 다시 적용할 수 있게 만드는 복원 기반 계층**입니다.
