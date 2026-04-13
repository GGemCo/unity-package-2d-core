# Runtime 기능 영역 문서 - Addressables와 설정 자산

## 1. 문서 목적

이 문서는 Core Runtime에서 **프리팹, 사운드, VFX, 설정 ScriptableObject를 어떻게 로드하는지** 설명합니다.
테이블이 정적 데이터의 중심이라면, Addressables와 설정 자산은 **런타임에 실제 리소스를 준비하는 계층**입니다.

---

## 2. 이 영역에 포함되는 주요 폴더

- `AddressableLoader/`
- `Configs/`
- `Configs/Addressables/`
- `ScriptableSettings/`

관련해서 함께 보는 폴더:
- `Core/Loader/`
- `Scenes/`
- `Vfx/`, `Sound/`, `UI/`, `Characters/`

---

## 3. 대표 클래스

### Addressables 진입점
- `AddressableLoader/AddressableLoaderController.cs`
- `AddressableLoader/AddressableLoaderSettings.cs`
- `AddressableLoader/AddressableLoaderSettingsRegist.cs`

### 리소스 유형별 로더
- `AddressableLoader/AddressableLoaderPrefabCharacter.cs`
- `AddressableLoader/AddressableLoaderPrefabCommon.cs`
- `AddressableLoader/AddressableLoaderPrefabVfx.cs`
- `AddressableLoader/AddressableLoaderSound.cs`
- `AddressableLoader/AddressableLoaderItem.cs`
- `AddressableLoader/AddressableLoaderCutscene.cs`

### 설정/키 구성
- `Configs/`
- `ScriptableSettings/*`

---

## 4. 이 영역의 핵심 책임

## 4-1. 런타임 리소스 접근을 중앙화

게임이 커질수록 프리팹, VFX, 사운드, 설정 에셋을 여러 곳에서 직접 로드하기 시작하면 관리가 어려워집니다.
그래서 Addressables 접근은 보통 로더 계층으로 감싸는 편이 좋습니다.

이 구조의 장점은 다음과 같습니다.

- 키 규칙을 중앙에서 관리할 수 있다.
- 호출부는 Addressables 세부 구현을 몰라도 된다.
- 캐싱, 재로드, 해제 정책을 한곳에서 정리하기 쉽다.

## 4-2. 설정 자산과 실행 자산의 역할 분리

Core에는 설정 ScriptableObject와 실행용 프리팹/오디오/VFX가 함께 존재합니다.
이 둘은 성격이 다르므로 문서에서도 분리해서 이해해야 합니다.

- **설정 자산**: 숫자/정책/디버그 옵션/기준 값
- **실행 자산**: 프리팹/VFX/오디오/컷신 리소스

설정 자산은 보통 “어떻게 동작할지”를 정하고, 실행 자산은 “무엇을 보여주고 재생할지”를 정합니다.

## 4-3. 패키지 간 공유 기준 제공

상위 패키지가 Core 리소스를 쓸 때도, 보통 Addressables 키나 로더를 통해 접근하는 편이 더 안전합니다.
직접 경로를 박아 넣기보다, Core에서 **정식 접근 경로**를 제공하는 구조가 유지보수에 유리합니다.

---

## 5. 대표 런타임 흐름

### 흐름 A: 초기화 시 설정 로드

1. 로딩 단계에서 `AddressableLoaderSettings` 계층이 필요한 설정 자산을 로드합니다.
2. ScriptableObject가 공용 설정으로 등록되거나 접근 가능 상태가 됩니다.
3. 캐릭터, UI, 전투, HUD, 테스트 기능이 해당 설정을 참조합니다.

### 흐름 B: 런타임 중 리소스 로드

1. 시스템이 캐릭터 프리팹, VFX, 사운드, 아이템 리소스를 요청합니다.
2. `AddressableLoaderController` 또는 하위 로더가 키를 해석합니다.
3. 로드된 자산을 반환하거나 캐싱합니다.
4. 사용이 끝난 뒤 적절한 해제 정책을 적용합니다.

---

## 6. 추천 읽기 순서

1. `AddressableLoaderController`
2. `AddressableLoaderSettings`, `AddressableLoaderSettingsRegist`
3. `AddressableLoaderPrefabCharacter`
4. `AddressableLoaderPrefabCommon`
5. `AddressableLoaderPrefabVfx`
6. `AddressableLoaderSound`
7. `AddressableLoaderItem`, `AddressableLoaderCutscene`
8. `ScriptableSettings/*`
9. `Configs/Addressables/*`

---

## 7. 기능 추가 시 배치 기준

## 이 영역에 넣는 것이 맞는 경우
- 새 Addressables 키 기반 로더
- 공용 설정 ScriptableObject 로드 경로
- 리소스 접근 공통화
- 캐시/해제 정책 정리

## 다른 영역에 두는 것이 좋은 경우
- 리소스를 재생하는 실제 로직
- UI 표시 로직
- 캐릭터 상태 계산
- 아이템/퀘스트 룰 자체

즉, 이 영역은 **리소스를 찾아오고 준비하는 곳**이지,
리소스를 사용해서 게임 규칙을 수행하는 곳은 아닙니다.

---

## 8. 디버깅 포인트

### 자산이 로드되지 않는 경우
- 키 문자열과 실제 Addressables 등록 키가 일치하는지 확인합니다.
- 올바른 로더를 통해 호출하고 있는지 확인합니다.
- 초기화 전에 로드를 시도하고 있지 않은지 점검합니다.

### 설정값이 기본값처럼 보이는 경우
- ScriptableObject가 실제로 로드되었는지 확인합니다.
- 씬에 직렬화된 임시 값과 런타임 로드 값이 충돌하지 않는지 확인합니다.
- 로드 후 등록 과정이 누락되지 않았는지 확인합니다.

### 메모리/재사용 문제가 의심되는 경우
- 자산 로드와 해제 시점이 짝을 이루는지 확인합니다.
- 캐시가 남아 있어서 이전 설정이나 이전 리소스가 재사용되는지 점검합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **런타임이 사용할 설정 자산과 실행 자산을 안정적으로 찾아오고 준비하는 리소스 접근 계층**입니다.
