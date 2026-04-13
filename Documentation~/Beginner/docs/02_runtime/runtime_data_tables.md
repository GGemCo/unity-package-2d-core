# Runtime 기능 영역 문서 - 데이터 테이블과 로컬라이즈

## 1. 문서 목적

이 문서는 Core Runtime에서 **txt 기반 데이터 테이블을 어떻게 읽고 사용하는지** 설명합니다.
Core는 여러 시스템이 하드코딩보다 테이블과 설정 자산을 통해 동작하므로, 이 영역을 이해하는 것이 전체 구조 파악에 매우 중요합니다.

---

## 2. 이 영역에 포함되는 주요 폴더

- `TableLoader/`
- `TableLoader/Table/`
- `Localization/`

관련해서 함께 보는 폴더:
- `Configs/`
- `AddressableLoader/`
- `ScriptableSettings/`

---

## 3. 대표 클래스

### 테이블 로더 기반
- `TableLoader/TableLoaderManager.cs`
- `TableLoader/TableLoaderBase.cs`
- `TableLoader/DefaultTable.cs`
- `TableLoader/ITableParser.cs`
- `TableLoader/TableRegistry.cs`

### 대표 테이블
- `TableLoader/Table/TableMonster.cs`
- `TableLoader/Table/TableNpc.cs`
- `TableLoader/Table/TableItem.cs`
- `TableLoader/Table/TableProjectile.cs`
- `TableLoader/Table/TableQuest.cs`
- `TableLoader/Table/TableWindow.cs`
- `TableLoader/Table/TableDialogue.cs`
- `TableLoader/Table/TableStat.cs`
- `TableLoader/Table/TableState.cs`
- `TableLoader/Table/TableDamageType.cs`
- `TableLoader/Table/Vfx/*`
- `TableLoader/Table/CrowdControl/*`

### 로컬라이즈
- `Localization/LocalizationManager.cs`

---

## 4. 이 영역의 핵심 책임

## 4-1. 런타임 데이터의 공용 진입점 제공

`TableLoaderManager`는 Core Runtime 데이터의 허브입니다.
몬스터, NPC, 아이템, Projectile, 퀘스트, 윈도우, 스탯, 상태, VFX 등 다양한 시스템이 이 매니저를 통해 데이터를 가져오는 구조라면, 새 기능을 추가할 때도 **어느 테이블에서 데이터를 꺼내는지**를 먼저 맞추는 것이 중요합니다.

## 4-2. raw row와 런타임 사용 구조의 분리

테이블 구조는 보통 아래 세 층으로 이해하면 좋습니다.

1. 파일 원본
2. 파싱된 row 데이터
3. 실제 런타임 사용 방식

이 구조를 유지하면 텍스트 파일 포맷이 바뀌더라도, 런타임 사용 코드를 전부 수정하지 않아도 되는 장점이 있습니다.

## 4-3. 데이터 주도 구조 유지

Core에는 다양한 기능이 들어 있지만, 실제 규칙은 코드보다 데이터에서 오는 경우가 많습니다.
예를 들면 다음과 같습니다.

- 몬스터 기본 정보
- 아이템 정의
- 퀘스트 단계
- 윈도우 정의
- Projectile 스펙
- 스탯/상태 정의
- VFX 키
- Crowd Control 규칙

따라서 기능 확장 시 “클래스를 어디에 추가할까”보다 먼저 **새로운 데이터가 필요한가**를 판단하는 것이 좋습니다.

## 4-4. 로컬라이즈 연결

`LocalizationManager`는 이름, 설명, UI 텍스트 표시를 런타임 데이터와 연결하는 지점입니다.
테이블 값 자체가 표시용 문자열을 직접 들고 있는지, 로컬라이즈 키를 들고 있는지에 따라 설계가 달라질 수 있으므로, 이 영역은 UI/툴팁 확장과도 밀접합니다.

---

## 5. 대표 런타임 흐름

### 흐름 A: 게임 시작 시 테이블 로드

1. 로딩 단계에서 테이블 로더가 초기화됩니다.
2. `TableRegistry` 또는 매니저가 필요한 테이블을 등록합니다.
3. 각 `Table*` 클래스가 텍스트 파일을 파싱합니다.
4. `TableLoaderManager`가 다른 시스템에서 접근 가능한 형태로 보관합니다.

### 흐름 B: 런타임 데이터 조회

1. 게임 시스템이 특정 UID나 키를 기준으로 데이터를 요청합니다.
2. `TableLoaderManager`가 적절한 `Table*` 인스턴스를 통해 조회합니다.
3. 조회 결과가 캐릭터, UI, Projectile, 아이템, 맵 시스템 등에서 사용됩니다.

---

## 6. 추천 읽기 순서

1. `TableLoaderManager`
2. `TableLoaderBase`
3. `DefaultTable`, `ITableParser`
4. `TableRegistry`
5. `TableStat`, `TableState`, `TableDamageType`
6. `TableMonster`, `TableNpc`, `TableItem`
7. `TableProjectile`, `TableWindow`, `TableQuest`, `TableDialogue`
8. `LocalizationManager`

---

## 7. 새 테이블을 추가할 때의 기준

새 테이블을 추가하려면 보통 아래 흐름을 따르는 편이 좋습니다.

1. 새 데이터 row 구조를 정의한다.
2. 해당 row를 파싱하는 `Table*` 클래스를 만든다.
3. `TableLoaderManager`에서 접근 경로를 연다.
4. 필요하면 로컬라이즈 키나 설정 자산과 연결한다.
5. 런타임 시스템은 직접 파일을 읽지 않고 이 매니저를 통해 접근하게 한다.

핵심은 **데이터 접근 경로를 중앙화**하는 것입니다.

---

## 8. 디버깅 포인트

### 데이터가 적용되지 않는 경우
- 로딩 단계에서 해당 테이블이 실제로 등록/로드되었는지 확인합니다.
- 키 값이 row와 조회 코드에서 일치하는지 확인합니다.
- 파싱 예외나 기본값 대체가 조용히 일어나고 있지 않은지 확인합니다.

### 값은 나오지만 이상한 경우
- raw row 파싱 문제인지, 런타임 해석 문제인지 분리합니다.
- 숫자/enum/string 변환 규칙이 바뀌었는지 확인합니다.
- 캐시를 쓰는 경우 재로드가 제대로 반영되는지 확인합니다.

### UI 이름/설명이 비어 있는 경우
- 로컬라이즈 키 누락인지, 로컬라이즈 로더 문제인지 구분합니다.
- 테이블이 실제 표시 문자열을 들고 있는지, 키만 들고 있는지 먼저 확인합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **Core의 여러 시스템이 공통으로 사용하는 런타임 데이터 저장소와 접근 계층**이며,
코드보다 데이터를 기준으로 기능을 확장하게 만드는 기반 구조입니다.
