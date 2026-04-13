# SaveDataManagerBase

## 1. 문서 목적

이 문서는 `SaveDataManagerBase`를 설명합니다.
세이브 슬롯, 파일 저장, 썸네일, 지연 저장, 강제 저장, SaveEnvelope 수집 흐름이 어디에서 관리되는지 이해하는 것이 목적입니다.

---

## 2. 역할

`SaveDataManagerBase`는 **세이브 시스템 공통 베이스 매니저**입니다.
개별 게임의 실제 저장 내용은 파생 클래스가 채우더라도, 저장 인프라와 흐름은 이 클래스가 제공합니다.

코드상 핵심 책임은 다음과 같습니다.

- 저장 디렉터리 초기화
- 슬롯 메타, 저장 파일, 썸네일 컨트롤러 생성
- 현재 슬롯 관리
- 지연 저장 예약
- 주기적 강제 저장
- 슬롯 삭제
- `SaveRegistry` 기반 Envelope 수집

즉, 저장 내용보다 저장 절차와 인프라를 제공하는 베이스 클래스입니다.

---

## 3. 왜 중요한가

저장 시스템 버그는 “무엇을 저장했는가”와 “언제 저장했는가” 두 축으로 나뉩니다.
`SaveDataManagerBase`는 후자의 중심입니다.

특히 아래 문제를 볼 때 중요합니다.

- 저장이 아예 트리거되지 않는다
- 저장 슬롯 번호가 잘못된다
- 자동 저장은 되는데 즉시 저장이 안 된다
- 썸네일/메타 파일이 안 생긴다
- 저장 파일은 있는데 복원 타이밍이 이상하다

---

## 4. 핵심 상태

### 컨트롤러
- `slotMetaDatController`
- `saveFileController`
- `thumbnailController`

저장 메타, 본문 파일, 썸네일을 각각 담당합니다.

### 디렉터리/슬롯
- `currentSaveSlot`
- `_maxSaveSlotCount`
- `_saveDirectory`
- `_thumbnailDirectory`

### 저장 정책
- `_saveDelay`
- `_forceSaveInterval`
- `_lastSaveTime`
- `_useSaveData`
- `_useGameTime`

설정 ScriptableObject와 연결되는 저장 정책의 핵심 상태입니다.

---

## 5. 주요 진입 메서드

### `Awake()`
`TableLoaderManager`를 확보하고 저장 디렉터리와 컨트롤러를 초기화합니다.

### `InitializeSaveDirectory()`
`GGemCoSaveSettings`와 공용 설정에서 다음 값을 읽어옵니다.

- 저장 사용 여부
- 지연 저장 시간
- 강제 저장 주기
- 썸네일 폭
- 최대 슬롯 수
- 저장/썸네일 폴더 경로

그리고 실제 디렉터리를 생성합니다.

### `InitializeController()`
슬롯 메타, 저장 파일, 썸네일 컨트롤러를 만들고 현재 슬롯을 읽은 뒤 `InitializeData()`를 호출합니다.

### `InitializeData()`
파생 클래스 확장 지점입니다.
게임별 초기 저장 데이터 준비를 여기서 붙이게 됩니다.

### `Start()`
강제 저장 타이머를 시작합니다.
`InvokeRepeating(nameof(ForceSave), ...)` 구조를 사용합니다.

### `StartSaveData()`
외부에서 저장을 요청할 때 호출하는 대표 메서드입니다.

흐름:
1. 현재 저장 컨테이너가 없으면 즉시 저장
2. 있으면 기존 예약을 취소
3. `_saveDelay` 후 `SaveData()` 예약

즉, 잦은 저장 요청을 디바운싱하는 구조입니다.

### `ForceSave()`
마지막 저장 시각과 간격을 비교해 강제 저장을 수행합니다.

### `SaveData()`
기본 클래스에서는 저장 가능 조건 검증만 수행합니다.
실제 저장 본문은 파생 클래스가 override해서 채웁니다.

### `DeleteData(int slot)`
슬롯 파일과 썸네일을 삭제하고, 메타 데이터도 갱신합니다.

### `BuildEnvelopeForSave()`
`SaveRegistry.All`을 순회하면서 각 기여자에게 `Capture(env)`를 호출합니다.
실제 저장 데이터 수집의 핵심 메서드입니다.

---

## 6. 연결해서 봐야 하는 클래스

### 설정
- `GGemCoSaveSettings`
- `AddressableLoaderSettings`

### 파일/메타/썸네일
- `SlotMetaDatController`
- `SaveFileController`
- `ThumbnailController`

### 저장 데이터 연결
- `SaveRegistry`
- `SaveEnvelope`
- `ISaveContributor`
- `SaveDataLoader`

---

## 7. 대표 런타임 흐름

### 흐름 A: 초기화
1. `Awake()`에서 설정을 읽습니다.
2. 저장/썸네일 폴더를 만듭니다.
3. 슬롯/파일/썸네일 컨트롤러를 초기화합니다.
4. 현재 슬롯 정보를 읽습니다.
5. 파생 클래스 초기 데이터를 준비합니다.

### 흐름 B: 일반 저장 요청
1. 외부 시스템이 `StartSaveData()`를 호출합니다.
2. 저장 컨테이너 존재 여부에 따라 즉시 혹은 지연 저장을 결정합니다.
3. `SaveData()`가 실행됩니다.
4. 파생 클래스가 실제 파일 쓰기를 수행합니다.

### 흐름 C: 강제 저장
1. 게임 시작 후 `InvokeRepeating`이 동작합니다.
2. `_forceSaveInterval`이 지나면 `ForceSave()`가 저장을 강제합니다.
3. 장시간 플레이 중에도 저장 누락을 줄입니다.

### 흐름 D: 실제 저장 본문 수집
1. `BuildEnvelopeForSave()`가 호출됩니다.
2. `SaveRegistry.All`을 순회합니다.
3. 각 기여자가 Envelope에 자신의 상태를 기록합니다.
4. 파생 클래스가 이 Envelope를 파일로 저장합니다.

---

## 8. 확장 포인트

### 게임별 저장 구현을 붙일 때
`SaveData()`를 override해서 파일 쓰기, 썸네일 생성, 메타 갱신을 구체화하면 됩니다.
기본 클래스는 저장 정책과 인프라만 유지하는 편이 좋습니다.

### 새로운 저장 대상 시스템을 붙일 때
`SaveDataManagerBase`를 수정하기보다 `ISaveContributor` + `SaveRegistry` 구조에 기여자를 붙이는 것이 맞습니다.

### 저장 정책을 바꿀 때
즉시 저장, 지연 저장, 강제 저장 기준은 설정에서 읽도록 유지하는 편이 운영이 쉽습니다.

---

## 9. 디버깅 체크리스트

### 저장이 아예 안 되는 경우
- `_useSaveData`가 true인지 확인합니다.
- `currentSaveSlot`이 유효 범위인지 확인합니다.
- `SaveData()`가 파생 클래스에서 실제 구현되었는지 확인합니다.

### 저장이 너무 자주 되는 경우
- `StartSaveData()`가 연속 호출되는지 확인합니다.
- `_saveDelay`가 너무 짧은지 확인합니다.

### 강제 저장이 안 되는 경우
- `Start()`가 호출되었는지 확인합니다.
- `_forceSaveInterval`이 0 또는 비정상 값인지 확인합니다.
- `_lastSaveTime` 갱신 흐름을 같이 봅니다.

### 저장 파일은 지워졌는데 UI에는 남는 경우
- `DeleteData()` 이후 슬롯 메타 갱신이 이루어지는지 확인합니다.
- 썸네일 삭제와 메타 삭제가 같이 되는지 확인합니다.

---

## 10. 한 줄 정리

`SaveDataManagerBase`는 **세이브 슬롯·파일·썸네일·저장 타이밍을 관리하고, `SaveRegistry`의 데이터를 실제 저장 흐름으로 묶는 Core 저장 시스템의 베이스 매니저**입니다.
